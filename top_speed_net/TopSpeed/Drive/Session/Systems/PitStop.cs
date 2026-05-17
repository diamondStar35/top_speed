using System;
using TopSpeed.Data;
using TopSpeed.Input;
using TopSpeed.Localization;
using TopSpeed.Tracks;
using TopSpeed.Vehicles;
using TopSpeed.Vehicles.Control;
using AudioSource = TS.Audio.Source;

namespace TopSpeed.Drive.Session.Systems
{
    internal sealed class PitStop : Subsystem
    {
        public enum PitPhase { Inactive, EnteringLane, InService, ExitingLane }

        private const float PitLaneSpeedKmh = 56.33f;
        private const float EnteringLaneDurationSeconds = 15f;
        private const float ExitingLaneDurationSeconds = 15f;
        private const float RefuelDurationSeconds = 8f;
        private const float TiresDurationSeconds = 12f;
        private const float BothDurationSeconds = 16f;
        private const int RefuelChoiceId = 1;
        private const int TiresChoiceId = 2;
        private const int BothChoiceId = 3;
        private const float FuelSoundLeadSeconds = 0.5f;
        private const float FuelStopTimeSeconds = 7f;
        private const float SecondCanTimeSeconds = 8.5f;
        private const float LeftTiresTimeSeconds = 8f;
        private const float CanLiters = 12f * 3.78541178f;
        private const float FillRateLitersPerSecond = CanLiters / FuelStopTimeSeconds;
        private const float ListenerLerpRate = 4.0f;
        private const float OffTrackMoveSeconds = 3.0f;
        private const float ExitStartMoveSeconds = 14.0f;
        private const float ListenerFollowDelaySeconds = 0.5f;

        private readonly DriveInput _input;
        private readonly ICar _car;
        private readonly Track _track;
        private readonly Func<bool> _isStarted;
        private readonly Func<bool> _isFinished;
        private readonly AudioSource? _soundLetsPit;
        private readonly AudioSource? _soundRightTires;
        private readonly AudioSource? _soundLeftTires;
        private readonly AudioSource? _soundFuelingUp;
        private readonly AudioSource? _soundExitPitRoad;
        private readonly Action _setPitting;
        private readonly Action _setRacing;
        private readonly Action<string> _speakText;
        private readonly Action<AudioSource?> _queueSound;
        private readonly Action<AudioSource?, float> _queueSoundDelayed;
        private readonly PitLaneController _pitController;

        private PitPhase _pitPhase;
        private bool _pitThisLap;
        private float _timer;
        private bool _choiceReceived;
        private int _choiceId;
        private bool _workStarted;
        private float _workTimer;
        private float _workDuration;
        private float _prevPositionY;
        private bool _prevInitialized;
        private float _pitEntryDist;
        private float _pitExitDist;
        private bool _hasPitLane;
        private float _pitEntryX;
        private float _pitEntryY;
        private bool _audioPhase0;
        private bool _audioPhase1;
        private bool _audioPhase7;
        private bool _audioPhase8;
        private bool _audioPhase9;
        private float _canFuelRemainingLiters;
        private bool _carFilledInCan1;
        private float _pitRoadCenterX;
        private Track.Road _pitEntryRoad;

        // Off-track pit lane state (used when !_hasPitLane)
        // Note: listener forward=+Z, so right=-X, left=+X. We go off the +X (audio-left) side.
        private float _trackPitEdge;
        private float _carHalfWidth;
        private float _offTrackTargetX;
        private bool _offTrackReached;
        private float _listenerCurrentX;

        public PitPhase Phase => _pitPhase;
        public bool PitThisLap => _pitThisLap;
        public bool NeedsChoice { get; private set; }
        public bool IsActive => _pitPhase != PitPhase.Inactive;
        public bool IsGhosted => _pitPhase != PitPhase.Inactive;
        public float? ListenerXOverride { get; private set; }

        public PitStop(
            string name,
            int order,
            DriveInput input,
            ICar car,
            Track track,
            Func<bool> isStarted,
            Func<bool> isFinished,
            AudioSource? soundLetsPit,
            AudioSource? soundRightTires,
            AudioSource? soundLeftTires,
            AudioSource? soundFuelingUp,
            AudioSource? soundExitPitRoad,
            Action setPitting,
            Action setRacing,
            Action<string> speakText,
            Action<AudioSource?> queueSound,
            Action<AudioSource?, float> queueSoundDelayed)
            : base(name, order)
        {
            _input = input ?? throw new ArgumentNullException(nameof(input));
            _car = car ?? throw new ArgumentNullException(nameof(car));
            _track = track ?? throw new ArgumentNullException(nameof(track));
            _isStarted = isStarted ?? throw new ArgumentNullException(nameof(isStarted));
            _isFinished = isFinished ?? throw new ArgumentNullException(nameof(isFinished));
            _soundLetsPit = soundLetsPit;
            _soundRightTires = soundRightTires;
            _soundLeftTires = soundLeftTires;
            _soundFuelingUp = soundFuelingUp;
            _soundExitPitRoad = soundExitPitRoad;
            _setPitting = setPitting ?? throw new ArgumentNullException(nameof(setPitting));
            _setRacing = setRacing ?? throw new ArgumentNullException(nameof(setRacing));
            _speakText = speakText ?? throw new ArgumentNullException(nameof(speakText));
            _queueSound = queueSound ?? throw new ArgumentNullException(nameof(queueSound));
            _queueSoundDelayed = queueSoundDelayed ?? throw new ArgumentNullException(nameof(queueSoundDelayed));
            _pitController = new PitLaneController(car.GetGearForSpeedKmh);

            _hasPitLane = track.TryGetPitPointDistance(SegmentPitPoint.PitEntry, out _pitEntryDist);
            if (!_hasPitLane)
                _pitEntryDist = 0f;
            if (!track.TryGetPitPointDistance(SegmentPitPoint.PitExit, out _pitExitDist))
                _pitExitDist = _pitEntryDist;
        }

        public void SetChoice(int choiceId)
        {
            _choiceId = choiceId;
            _choiceReceived = true;
            NeedsChoice = false;
        }

        public void Reset()
        {
            if (_pitPhase != PitPhase.Inactive)
            {
                _car.SetOverrideController(null);
                if (!_isFinished())
                    _setRacing();
            }
            _pitPhase = PitPhase.Inactive;
            _pitThisLap = false;
            _timer = 0f;
            _choiceReceived = false;
            _workStarted = false;
            NeedsChoice = false;
            _prevInitialized = false;
            _pitController.SteerTargetX = null;
            ListenerXOverride = null;
            _offTrackReached = false;
            _canFuelRemainingLiters = 0f;
            _carFilledInCan1 = false;
        }

        public override void Update(SessionContext context, float elapsed)
        {
            if (!_isStarted() || _isFinished())
            {
                if (_pitPhase != PitPhase.Inactive)
                    CancelPitStop();
                _prevInitialized = false;
                return;
            }

            if (_pitPhase != PitPhase.Inactive
                && (_car.State == CarState.Crashing || _car.State == CarState.Crashed))
            {
                CancelPitStop();
                _prevInitialized = false;
                return;
            }

            switch (_pitPhase)
            {
                case PitPhase.Inactive:
                    UpdateInactive();
                    break;
                case PitPhase.EnteringLane:
                    UpdateEnteringLane(elapsed);
                    break;
                case PitPhase.InService:
                    UpdateInService(elapsed);
                    break;
                case PitPhase.ExitingLane:
                    UpdateExitingLane(elapsed);
                    break;
            }

            _prevPositionY = _car.PositionY;
            _prevInitialized = true;
        }

        private void UpdateInactive()
        {
            if (_input.Intents.IsTriggered(DriveIntent.Pit) && !_pitThisLap)
            {
                _pitThisLap = true;
                _queueSound(_soundLetsPit);
                if (_soundLetsPit == null)
                    _speakText(LocalizationService.Mark("Pitting this time"));
            }

            if (!_pitThisLap || !_prevInitialized)
                return;

            var lapDist = _track.Length;
            if (lapDist <= 0f)
                return;

            var posY = _car.PositionY;
            var lapStart = (float)Math.Floor(posY / lapDist) * lapDist;
            var entryAbsY = lapStart + _pitEntryDist;
            if (_prevPositionY < entryAbsY && posY >= entryAbsY)
                EnterPitLane();
        }

        private void EnterPitLane()
        {
            _pitPhase = PitPhase.EnteringLane;
            _pitThisLap = false;
            _timer = 0f;
            _choiceReceived = false;
            _choiceId = -1;
            _workStarted = false;
            _workTimer = 0f;
            _workDuration = 0f;
            NeedsChoice = true;

            _audioPhase0 = false;
            _audioPhase1 = false;
            _audioPhase7 = false;
            _audioPhase8 = false;
            _audioPhase9 = false;
            _canFuelRemainingLiters = 0f;
            _carFilledInCan1 = false;

            _pitEntryX = _car.PositionX;
            _pitEntryY = _car.PositionY;
            var road = _track.RoadAtPosition(_pitEntryY);
            _pitRoadCenterX = (road.Left + road.Right) * 0.5f;

            if (!_hasPitLane)
            {
                _pitEntryRoad = road;
                _trackPitEdge = Math.Min(road.Left, road.Right);
                _carHalfWidth = _car.WidthM / 2f;
                _offTrackTargetX = _trackPitEdge - _carHalfWidth;
                _offTrackReached = false;
                _listenerCurrentX = _pitRoadCenterX;
                ListenerXOverride = _pitRoadCenterX;
                _pitController.SteerTargetX = null; // X is managed via SetPosition
            }
            else
            {
                ListenerXOverride = null;
                _pitController.SteerTargetX = _pitRoadCenterX;
            }

            if (!_car.CombustionActive)
                _car.RestartFromStall();
            _setPitting();
            _car.SetFirstGear();
            _pitController.BrakeMode = false;
            _car.SetOverrideController(_pitController);
        }

        private void UpdateEnteringLane(float elapsed)
        {
            if (!_hasPitLane)
            {
                var xProgress = Math.Min(1f, _timer / OffTrackMoveSeconds);
                var carX = _pitEntryX + (_offTrackTargetX - _pitEntryX) * xProgress;
                _car.SetPosition(carX, _pitEntryY);
                EvaluateWithWideRoad();
                if (!_offTrackReached && xProgress >= 1f)
                {
                    _offTrackReached = true;
                    _listenerCurrentX = _pitRoadCenterX;
                }
                UpdateEnteringLaneListener(elapsed);
            }

            _timer += elapsed;
            if (_timer < EnteringLaneDurationSeconds)
                return;

            if (!_pitController.BrakeMode)
            {
                _pitController.BrakeMode = true;
                _car.BrakeSound();
            }

            if (_car.SpeedKmh > 0.5f)
                return;

            _car.StopMotionImmediately();
            _pitPhase = PitPhase.InService;
            _timer = 0f;
        }

        private void UpdateEnteringLaneListener(float elapsed)
        {
            if (!_offTrackReached)
            {
                // Listener stays at track center while car moves off-track to the left.
                // Player hears the car panning left in stereo.
                ListenerXOverride = _pitRoadCenterX;
            }
            else
            {
                // Car is now off-track; lerp listener toward it so it re-centers in stereo.
                LerpListenerToward(_offTrackTargetX, elapsed);
            }
        }

        private void UpdateInService(float elapsed)
        {
            if (!_hasPitLane)
            {
                _car.SetPosition(_offTrackTargetX, _pitEntryY);
                EvaluateWithWideRoad();
                LerpListenerToward(_offTrackTargetX, elapsed);
            }

            if (_workStarted)
            {
                _workTimer += elapsed;
                UpdateWorkAudio(elapsed);
                if (_workTimer >= _workDuration)
                    BeginExitingLane();
                return;
            }

            if (!_choiceReceived)
                return;

            _workStarted = true;
            _workTimer = 0f;
            _workDuration = GetWorkDuration(_choiceId);
        }

        private void UpdateWorkAudio(float elapsed)
        {
            // Right tires start immediately
            if (!_audioPhase0)
            {
                _audioPhase0 = true;
                if (_choiceId == TiresChoiceId || _choiceId == BothChoiceId)
                    _soundRightTires?.Play(loop: false);
            }

            // Can 1: fueling sound starts after 0.5s lead
            if (!_audioPhase1 && _workTimer >= FuelSoundLeadSeconds
                && (_choiceId == RefuelChoiceId || _choiceId == BothChoiceId))
            {
                _audioPhase1 = true;
                _canFuelRemainingLiters = CanLiters;
                _soundFuelingUp?.Play(loop: true);
            }

            // Can 1: add fuel during dump window
            if (_audioPhase1 && !_audioPhase7
                && (_choiceId == RefuelChoiceId || _choiceId == BothChoiceId))
                AddFuelFromCan(elapsed);

            // Can 1: early finish — tank full or can empty before scheduled end
            if (_audioPhase1 && !_audioPhase7 && _canFuelRemainingLiters <= 0f
                && (_choiceId == RefuelChoiceId || _choiceId == BothChoiceId))
            {
                _audioPhase7 = true;
                _soundFuelingUp?.Stop();
                if (_choiceId == RefuelChoiceId)
                    _workDuration = _workTimer + FuelSoundLeadSeconds;
                else if (_car.FuelLitersRemaining >= _car.FuelTankCapacityLiters - 0.01f)
                {
                    _carFilledInCan1 = true;
                    _workDuration = Math.Max(TiresDurationSeconds, _workTimer + FuelSoundLeadSeconds);
                }
            }

            // Can 1: sound stops after 7s dump (0.5s trail before next event)
            if (!_audioPhase7 && _workTimer >= FuelSoundLeadSeconds + FuelStopTimeSeconds
                && (_choiceId == RefuelChoiceId || _choiceId == BothChoiceId))
            {
                _audioPhase7 = true;
                _soundFuelingUp?.Stop();
                if (_choiceId == BothChoiceId
                    && _car.FuelLitersRemaining >= _car.FuelTankCapacityLiters - 0.01f)
                {
                    _carFilledInCan1 = true;
                    _workDuration = TiresDurationSeconds;
                }
            }

            // Left tires start at 8s
            if (!_audioPhase9 && _workTimer >= LeftTiresTimeSeconds
                && (_choiceId == TiresChoiceId || _choiceId == BothChoiceId))
            {
                _audioPhase9 = true;
                _soundLeftTires?.Play(loop: false);
            }

            // Can 2: fueling sound starts at 8.5s (skip if car was filled by can 1)
            if (!_audioPhase8 && !_carFilledInCan1 && _workTimer >= SecondCanTimeSeconds
                && _choiceId == BothChoiceId)
            {
                _audioPhase8 = true;
                _canFuelRemainingLiters = CanLiters;
                _soundFuelingUp?.Play(loop: true);
            }

            // Can 2: add fuel during dump window
            if (_audioPhase8 && _workTimer < SecondCanTimeSeconds + FuelStopTimeSeconds
                && _choiceId == BothChoiceId)
                AddFuelFromCan(elapsed);

            // Can 2: sound stops after 7s dump (0.5s trail before service ends)
            if (_audioPhase8 && _workTimer >= SecondCanTimeSeconds + FuelStopTimeSeconds
                && _choiceId == BothChoiceId)
                _soundFuelingUp?.Stop();
        }

        private void AddFuelFromCan(float elapsed)
        {
            if (_canFuelRemainingLiters <= 0f)
                return;
            var spaceInTank = _car.FuelTankCapacityLiters - _car.FuelLitersRemaining;
            if (spaceInTank <= 0f)
            {
                _canFuelRemainingLiters = 0f;
                return;
            }
            var toAdd = Math.Min(FillRateLitersPerSecond * elapsed,
                        Math.Min(_canFuelRemainingLiters, spaceInTank));
            _car.AddFuelLiters(toAdd);
            _canFuelRemainingLiters -= toAdd;
            // If tank is now full, mark can done so the early-exit check fires.
            // Without this, the idle engine burns a tiny amount each frame, keeping
            // a small spaceInTank that we top up forever, never draining the can.
            if (_car.FuelLitersRemaining >= _car.FuelTankCapacityLiters - 0.01f)
                _canFuelRemainingLiters = 0f;
        }

        private void BeginExitingLane()
        {
            _soundFuelingUp?.Stop();
            _car.SetFirstGear();
            _pitPhase = PitPhase.ExitingLane;
            _timer = 0f;
            _pitController.BrakeMode = false;
        }

        private void UpdateExitingLane(float elapsed)
        {
            if (!_hasPitLane)
            {
                if (_timer < ExitStartMoveSeconds)
                {
                    // 0–14s: car held off-track, listener tracks car position.
                    _car.SetPosition(_offTrackTargetX, _pitEntryY);
                    _listenerCurrentX = _offTrackTargetX;
                    ListenerXOverride = _offTrackTargetX;
                }
                else
                {
                    // 14–15s: car moves back onto the track.
                    var xProgress = Math.Min(1f, (_timer - ExitStartMoveSeconds) /
                        (ExitingLaneDurationSeconds - ExitStartMoveSeconds));
                    var carX = _offTrackTargetX + (_pitEntryX - _offTrackTargetX) * xProgress;
                    _car.SetPosition(carX, _pitEntryY);

                    // Listener follows car after a 0.5s delay so car is heard panning right first.
                    if (_timer >= ExitStartMoveSeconds + ListenerFollowDelaySeconds)
                    {
                        _listenerCurrentX = carX;
                        ListenerXOverride = carX;
                    }
                    else
                    {
                        ListenerXOverride = _offTrackTargetX;
                    }
                }
                EvaluateWithWideRoad();
            }

            _timer += elapsed;

            if (_timer < ExitingLaneDurationSeconds)
                return;
            CompletePitExit();
        }

        private void CompletePitExit()
        {
            var lapDist = _track.Length;
            var lapStart = lapDist > 0f
                ? (float)Math.Floor(_pitEntryY / lapDist) * lapDist
                : 0f;
            _car.PrepareEngineForPitExit(PitLaneSpeedKmh);
            _car.SetPosition(_pitEntryX, lapStart + _pitExitDist);
            _car.SetOverrideController(null);
            _soundExitPitRoad?.Play(loop: false);
            ListenerXOverride = null;
            _pitController.SteerTargetX = null;
            _setRacing();
            _pitPhase = PitPhase.Inactive;
            NeedsChoice = false;
        }

        private void CancelPitStop()
        {
            _soundFuelingUp?.Stop();
            _soundRightTires?.Stop();
            _soundLeftTires?.Stop();
            _pitController.SteerTargetX = null;
            ListenerXOverride = null;
            _offTrackReached = false;
            _car.SetOverrideController(null);
            if (!_isFinished())
                _setRacing();
            _pitPhase = PitPhase.Inactive;
            _timer = 0f;
            _choiceReceived = false;
            _workStarted = false;
            NeedsChoice = false;
        }

        private void EvaluateWithWideRoad()
        {
            var wideRoad = _pitEntryRoad;
            wideRoad.Left = _pitEntryRoad.Left - 1000f;
            wideRoad.Right = _pitEntryRoad.Right + 1000f;
            _car.Evaluate(wideRoad);
        }

        private void LerpListenerToward(float targetX, float elapsed)
        {
            var blend = Math.Min(1f, elapsed * ListenerLerpRate);
            _listenerCurrentX += (targetX - _listenerCurrentX) * blend;
            if (Math.Abs(_listenerCurrentX - targetX) < 0.1f)
                _listenerCurrentX = targetX;
            ListenerXOverride = _listenerCurrentX;
        }

        private static float GetWorkDuration(int choiceId)
        {
            return choiceId switch
            {
                RefuelChoiceId => RefuelDurationSeconds,
                TiresChoiceId => TiresDurationSeconds,
                BothChoiceId => BothDurationSeconds,
                _ => RefuelDurationSeconds
            };
        }
    }
}
