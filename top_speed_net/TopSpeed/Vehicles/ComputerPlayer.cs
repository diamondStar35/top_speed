using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using TopSpeed.Audio;
using TopSpeed.Common;
using TopSpeed.Core;
using TopSpeed.Data;
using TopSpeed.Input;
using TopSpeed.Protocol;
using TopSpeed.Tracks;
using TS.Audio;

namespace TopSpeed.Vehicles
{
    internal sealed class ComputerPlayer : IDisposable
    {
        private const float CallLength = 30.0f;
        private const float BaseLateralSpeed = 7.0f;
        private const float StabilitySpeedRef = 45.0f;
        private const float AutoShiftHysteresis = 0.05f;
        private const float AutoShiftCooldownSeconds = 0.15f;

        private readonly AudioManager _audio;
        private readonly Track _track;
        private readonly RaceSettings _settings;
        private readonly Func<float> _currentTime;
        private readonly Func<bool> _started;
        private readonly Action<string>? _debugSpeak;
        private readonly int _playerNumber;
        private readonly int _vehicleIndex;

        private readonly List<BotEvent> _events;
        private readonly string _legacyRoot;

        private ComputerState _state;
        private TrackSurface _surface;
        private int _gear;
        private float _speed;
        private float _positionX;
        private float _positionY;
        private int _switchingGear;
        private float _autoShiftCooldown;
        private float _trackLength;

        private float _surfaceTractionFactor;
        private float _deceleration;
        private float _topSpeed;
        private float _massKg;
        private float _drivetrainEfficiency;
        private float _engineBrakingTorqueNm;
        private float _tireGripCoefficient;
        private float _brakeStrength;
        private float _wheelRadiusM;
        private float _engineBraking;
        private float _idleRpm;
        private float _revLimiter;
        private float _finalDriveRatio;
        private float _powerFactor;
        private float _peakTorqueNm;
        private float _peakTorqueRpm;
        private float _idleTorqueNm;
        private float _redlineTorqueNm;
        private float _dragCoefficient;
        private float _frontalAreaM2;
        private float _rollingResistanceCoefficient;
        private float _launchRpm;
        private float _lastDriveRpm;
        private float _lateralGripCoefficient;
        private float _highSpeedStability;
        private float _wheelbaseM;
        private float _maxSteerDeg;
        private float _widthM;
        private float _lengthM;
        private int _idleFreq;
        private int _topFreq;
        private int _shiftFreq;
        private int _gears;
        private float _steering;
        private int _steeringFactor;

        private int _random;
        private int _prevFrequency;
        private int _frequency;
        private int _prevBrakeFrequency;
        private int _brakeFrequency;
        private float _laneWidth;
        private float _relPos;
        private float _nextRelPos;
        private float _diffX;
        private float _diffY;
        private int _currentSteering;
        private int _currentThrottle;
        private int _currentBrake;
        private float _currentSurfaceTractionFactor;
        private float _currentDeceleration;
        private float _speedDiff;
        private float _thrust;
        private int _difficulty;
        private bool _finished;
        private bool _horning;
        private bool _backfirePlayedAuto;
        private bool _networkBackfireActive;
        private int _frame;
        private Vector3 _lastAudioPosition;
        private bool _audioInitialized;
        private float _lastAudioUpdateTime;

        private AudioSourceHandle _soundEngine;
        private AudioSourceHandle _soundHorn;
        private AudioSourceHandle _soundStart;
        private AudioSourceHandle _soundCrash;
        private AudioSourceHandle _soundBrake;
        private AudioSourceHandle _soundMiniCrash;
        private AudioSourceHandle _soundBump;
        private AudioSourceHandle? _soundBackfire;

        private EngineModel _engine;

        public ComputerPlayer(
            AudioManager audio,
            Track track,
            RaceSettings settings,
            int vehicleIndex,
            int playerNumber,
            Func<float> currentTime,
            Func<bool> started,
            Action<string>? debugSpeak = null)
        {
            _audio = audio;
            _track = track;
            _settings = settings;
            _playerNumber = playerNumber;
            _vehicleIndex = vehicleIndex;
            _currentTime = currentTime;
            _started = started;
            _debugSpeak = debugSpeak;
            _events = new List<BotEvent>();
            _legacyRoot = Path.Combine(AssetPaths.SoundsRoot, "Legacy");

            _surface = TrackSurface.Asphalt;
            _gear = 1;
            _state = ComputerState.Stopped;
            _switchingGear = 0;
            _horning = false;
            _difficulty = (int)settings.Difficulty;
            _prevFrequency = 0;
            _prevBrakeFrequency = 0;
            _brakeFrequency = 0;
            _laneWidth = 0;
            _relPos = 0f;
            _nextRelPos = 0f;
            _diffX = 0;
            _diffY = 0;
            _currentSteering = 0;
            _currentThrottle = 0;
            _currentBrake = 0;
            _currentSurfaceTractionFactor = 0;
            _currentDeceleration = 0;
            _speedDiff = 0;
            _thrust = 0;
            _speed = 0;
            _frame = 1;
            _finished = false;
            _random = Algorithm.RandomInt(100);
            _networkBackfireActive = false;

            var definition = VehicleLoader.LoadOfficial(vehicleIndex, track.Weather);
            _surfaceTractionFactor = definition.SurfaceTractionFactor;
            _deceleration = definition.Deceleration;
            _topSpeed = definition.TopSpeed;
            _massKg = Math.Max(1f, definition.MassKg);
            _drivetrainEfficiency = Math.Max(0.1f, Math.Min(1.0f, definition.DrivetrainEfficiency));
            _engineBrakingTorqueNm = Math.Max(0f, definition.EngineBrakingTorqueNm);
            _tireGripCoefficient = Math.Max(0.1f, definition.TireGripCoefficient);
            _brakeStrength = Math.Max(0.1f, definition.BrakeStrength);
            _wheelRadiusM = Math.Max(0.01f, definition.TireCircumferenceM / (2.0f * (float)Math.PI));
            _engineBraking = Math.Max(0.05f, Math.Min(1.0f, definition.EngineBraking));
            _idleRpm = definition.IdleRpm;
            _revLimiter = definition.RevLimiter;
            _finalDriveRatio = definition.FinalDriveRatio;
            _powerFactor = Math.Max(0.1f, definition.PowerFactor);
            _peakTorqueNm = Math.Max(0f, definition.PeakTorqueNm);
            _peakTorqueRpm = Math.Max(_idleRpm + 100f, definition.PeakTorqueRpm);
            _idleTorqueNm = Math.Max(0f, definition.IdleTorqueNm);
            _redlineTorqueNm = Math.Max(0f, definition.RedlineTorqueNm);
            _dragCoefficient = Math.Max(0.01f, definition.DragCoefficient);
            _frontalAreaM2 = Math.Max(0.1f, definition.FrontalAreaM2);
            _rollingResistanceCoefficient = Math.Max(0.001f, definition.RollingResistanceCoefficient);
            _launchRpm = Math.Max(_idleRpm, Math.Min(_revLimiter, definition.LaunchRpm));
            _lateralGripCoefficient = Math.Max(0.1f, definition.LateralGripCoefficient);
            _highSpeedStability = Math.Max(0f, Math.Min(1.0f, definition.HighSpeedStability));
            _wheelbaseM = Math.Max(0.5f, definition.WheelbaseM);
            _maxSteerDeg = Math.Max(5f, Math.Min(60f, definition.MaxSteerDeg));
            _widthM = Math.Max(0.5f, definition.WidthM);
            _lengthM = Math.Max(0.5f, definition.LengthM);
            _idleFreq = definition.IdleFreq;
            _topFreq = definition.TopFreq;
            _shiftFreq = definition.ShiftFreq;
            _gears = definition.Gears;
            _steering = definition.Steering;
            _steeringFactor = definition.SteeringFactor;
            _frequency = _idleFreq;

            _engine = new EngineModel(
                definition.IdleRpm,
                definition.MaxRpm,
                definition.RevLimiter,
                definition.AutoShiftRpm,
                definition.EngineBraking,
                definition.TopSpeed,
                definition.FinalDriveRatio,
                definition.TireCircumferenceM,
                definition.Gears,
                definition.GearRatios);

            _soundEngine = CreateRequiredSound(definition.GetSoundPath(VehicleAction.Engine), "engine", looped: true);
            _soundStart = CreateRequiredSound(definition.GetSoundPath(VehicleAction.Start), "start");
            _soundHorn = CreateRequiredSound(definition.GetSoundPath(VehicleAction.Horn), "horn", looped: true);
            _soundCrash = CreateRequiredSound(definition.GetSoundPath(VehicleAction.Crash), "crash");
            _soundBrake = CreateRequiredSound(definition.GetSoundPath(VehicleAction.Brake), "brake", looped: true);
            _soundEngine.SetDopplerFactor(1f);
            _soundHorn.SetDopplerFactor(0f);
            _soundBrake.SetDopplerFactor(0f);
            _soundMiniCrash = CreateRequiredSound(Path.Combine(_legacyRoot, "crashshort.wav"), "mini crash");
            _soundBump = CreateRequiredSound(Path.Combine(_legacyRoot, "bump.wav"), "bump");
            _soundCrash.SetDopplerFactor(0f);
            _soundMiniCrash.SetDopplerFactor(0f);
            _soundBump.SetDopplerFactor(0f);
            _soundBackfire = TryCreateSound(definition.GetSoundPath(VehicleAction.Backfire));
        }

        public ComputerState State => _state;
        public float PositionX => _positionX;
        public float PositionY => _positionY;
        public float Speed => _speed;
        public int PlayerNumber => _playerNumber;
        public int VehicleIndex => _vehicleIndex;
        public bool Finished => _finished;
        public void SetFinished(bool value) => _finished = value;
        public float WidthM => _widthM;
        public float LengthM => _lengthM;

        public void Initialize(float positionX, float positionY, float trackLength)
        {
            _positionX = positionX;
            _positionY = positionY;
            _trackLength = trackLength;
            _laneWidth = _track.LaneWidth;
            _audioInitialized = false;
            _lastAudioPosition = new Vector3(positionX, 0f, positionY);
            _lastAudioUpdateTime = 0f;
        }

        public void FinalizePlayer()
        {
            _soundEngine.Stop();
        }

        public void PendingStart(float baseDelay)
        {
            float difficultyDelay;
            var randomValue = Algorithm.RandomInt(100) / 100f;

            switch (_difficulty)
            {
                case 2: // Hard
                    difficultyDelay = 0.1f + (randomValue * 0.4f);
                    break;
                case 1: // Normal
                    difficultyDelay = 1.0f + (randomValue * 1.5f);
                    break;
                case 0: // Easy
                default:
                    difficultyDelay = 2.5f + (randomValue * 2.5f);
                    break;
            }

            var startTime = baseDelay + difficultyDelay;
            PushEvent(BotEventType.CarComputerStart, startTime);
        }

        public void Start()
        {
            var delay = Math.Max(0f, _soundStart.GetLengthSeconds() - 0.1f);
            PushEvent(BotEventType.CarStart, delay);
            _soundStart.Play(loop: false);
            _speed = 0;
            _prevFrequency = _idleFreq;
            _frequency = _idleFreq;
            _prevBrakeFrequency = 0;
            _brakeFrequency = 0;
            _switchingGear = 0;
            _state = ComputerState.Starting;
        }

        public void Crash(float newPosition)
        {
            _speed = 0;
            _soundCrash.Play(loop: false);
            _soundEngine.Stop();
            _soundEngine.SeekToStart();
            _soundEngine.SetPanPercent(0);
            _soundBrake.Stop();
            _soundBrake.SeekToStart();
            _soundHorn.Stop();
            _gear = 1;
            _positionX = newPosition;
            _state = ComputerState.Crashing;
            PushEvent(BotEventType.CarRestart, _soundCrash.GetLengthSeconds() + 1.25f);
        }

        public void MiniCrash(float newPosition)
        {
            _speed /= 4;
            _positionX = newPosition;
            _soundMiniCrash.Play(loop: false);
        }

        public void Bump(float bumpX, float bumpY, float bumpSpeed)
        {
            if (bumpY != 0)
            {
                _speed -= bumpSpeed;
                _positionY += bumpY;
            }

            if (bumpX > 0)
            {
                _positionX += 2 * bumpX;
                _speed -= _speed / 5;
            }
            else if (bumpX < 0)
            {
                _positionX += 2 * bumpX;
                _speed -= _speed / 5;
            }

            if (_speed < 0)
                _speed = 0;
            _soundBump.Play(loop: false);
            Horn();
        }

        public void Stop()
        {
            _state = ComputerState.Stopping;
        }

        public void Quiet()
        {
            _soundBrake.Stop();
            _soundHorn.Stop();
            _soundEngine.SetVolumePercent(80);
            if (_soundBackfire != null)
                _soundBackfire.SetVolumePercent(80);
        }

        public void Run(float elapsed, float playerX, float playerY)
        {
            _diffX = _positionX - playerX;
            _diffY = _positionY - playerY;
            _diffY = ((_diffY % _trackLength) + _trackLength) % _trackLength;
            if (_diffY > _trackLength / 2)
                _diffY = (_diffY - _trackLength) % _trackLength;

            if (!_horning && _diffY < -100.0f)
            {
                if (Algorithm.RandomInt(2500) == 1)
                {
                    var duration = Algorithm.RandomInt(80);
                    _horning = true;
                    PushEvent(BotEventType.StopHorn, 0.2f + (duration / 80.0f));
                }
            }

            UpdateSpatialAudio(playerX, playerY, _trackLength, elapsed);

            if (_state == ComputerState.Running && _started())
            {
                AI();

                _currentSurfaceTractionFactor = _surfaceTractionFactor;
                _currentDeceleration = _deceleration;
                _speedDiff = 0;
                switch (_surface)
                {
                    case TrackSurface.Gravel:
                        _currentSurfaceTractionFactor = (_currentSurfaceTractionFactor * 2) / 3;
                        _currentDeceleration = (_currentDeceleration * 2) / 3;
                        break;
                    case TrackSurface.Water:
                        _currentSurfaceTractionFactor = (_currentSurfaceTractionFactor * 3) / 5;
                        _currentDeceleration = (_currentDeceleration * 3) / 5;
                        break;
                    case TrackSurface.Sand:
                        _currentSurfaceTractionFactor = _currentSurfaceTractionFactor / 2;
                        _currentDeceleration = (_currentDeceleration * 3) / 2;
                        break;
                    case TrackSurface.Snow:
                        _currentDeceleration = _currentDeceleration / 2;
                        break;
                }

                if (_currentThrottle == 0)
                {
                    _thrust = _currentBrake;
                    if (_currentBrake != 0)
                    {
                        if (_surface == TrackSurface.Asphalt && !_soundBrake.IsPlaying)
                            _soundBrake.Play(loop: true);
                        else if (_surface != TrackSurface.Asphalt)
                            _soundBrake.Stop();
                    }
                }
                else if (_currentBrake == 0)
                {
                    _thrust = _currentThrottle;
                    if (_soundBrake.IsPlaying)
                        _soundBrake.Stop();
                }
                else if (-_currentBrake > _currentThrottle)
                {
                    _thrust = _currentBrake;
                }

                var speedMpsCurrent = _speed / 3.6f;
                var throttle = Math.Max(0f, Math.Min(100f, _currentThrottle)) / 100f;
                var surfaceTractionMod = _surfaceTractionFactor > 0f
                    ? _currentSurfaceTractionFactor / _surfaceTractionFactor
                    : 1.0f;
                var longitudinalGripFactor = 1.0f;

                if (_thrust > 10)
                {
                    var steeringCommandAccel = (_currentSteering / 100.0f) * _steering;
                    if (steeringCommandAccel > 1.0f)
                        steeringCommandAccel = 1.0f;
                    else if (steeringCommandAccel < -1.0f)
                        steeringCommandAccel = -1.0f;
                    var steerRadAccel = (float)(Math.PI / 180.0) * (_maxSteerDeg * steeringCommandAccel);
                    var curvatureAccel = (float)Math.Tan(steerRadAccel) / _wheelbaseM;
                    var desiredLatAccel = curvatureAccel * speedMpsCurrent * speedMpsCurrent;
                    var desiredLatAccelAbs = Math.Abs(desiredLatAccel);
                    var grip = _tireGripCoefficient * surfaceTractionMod * _lateralGripCoefficient;
                    var maxLatAccel = grip * 9.80665f;
                    var lateralRatio = maxLatAccel > 0f ? Math.Min(1.0f, desiredLatAccelAbs / maxLatAccel) : 0f;
                    longitudinalGripFactor = (float)Math.Sqrt(Math.Max(0.0, 1.0 - (lateralRatio * lateralRatio)));
                    var driveRpm = CalculateDriveRpm(speedMpsCurrent, throttle);
                    var engineTorque = CalculateEngineTorqueNm(driveRpm) * throttle * _powerFactor;
                    var gearRatio = _engine.GetGearRatio(_gear);
                    var wheelTorque = engineTorque * gearRatio * _finalDriveRatio * _drivetrainEfficiency;
                    var wheelForce = wheelTorque / _wheelRadiusM;
                    var tractionLimit = _tireGripCoefficient * surfaceTractionMod * _massKg * 9.80665f;
                    if (wheelForce > tractionLimit)
                        wheelForce = tractionLimit;
                    wheelForce *= (float)longitudinalGripFactor;

                    var dragForce = 0.5f * 1.225f * _dragCoefficient * _frontalAreaM2 * speedMpsCurrent * speedMpsCurrent;
                    var rollingForce = _rollingResistanceCoefficient * _massKg * 9.80665f;
                    var netForce = wheelForce - dragForce - rollingForce;
                    var accelMps2 = netForce / _massKg;
                    var newSpeedMps = speedMpsCurrent + (accelMps2 * elapsed);
                    if (newSpeedMps < 0f)
                        newSpeedMps = 0f;
                    _speedDiff = (newSpeedMps - speedMpsCurrent) * 3.6f;
                    _lastDriveRpm = CalculateDriveRpm(newSpeedMps, throttle);
                }
                else
                {
                    var surfaceDecelMod = _deceleration > 0f ? _currentDeceleration / _deceleration : 1.0f;
                    var brakeInput = Math.Max(0f, Math.Min(100f, -_currentBrake)) / 100f;
                    var brakeDecel = CalculateBrakeDecel(brakeInput, surfaceDecelMod);
                    var engineBrakeDecel = CalculateEngineBrakingDecel(surfaceDecelMod);
                    var totalDecel = _thrust < -10 ? (brakeDecel + engineBrakeDecel) : engineBrakeDecel;
                    _speedDiff = -totalDecel * elapsed;
                    _lastDriveRpm = 0f;
                }

                _speed += _speedDiff;
                if (_speed > _topSpeed)
                    _speed = _topSpeed;
                if (_speed < 0)
                    _speed = 0;

                UpdateAutomaticGear(elapsed, _speed / 3.6f, throttle, surfaceTractionMod, longitudinalGripFactor);
                _engine.SyncFromSpeed(_speed, _gear, elapsed, _currentThrottle);
                if (_lastDriveRpm > 0f && _lastDriveRpm > _engine.Rpm)
                    _engine.OverrideRpm(_lastDriveRpm);
                if (_thrust < -50 && _speed > 0)
                    _currentSteering = _currentSteering * 2 / 3;

                var speedMps = _speed / 3.6f;
                _positionY += (speedMps * elapsed);
                var surfaceMultiplier = _surface == TrackSurface.Snow ? 1.44f : 1.0f;
                var steeringCommandLat = (_currentSteering / 100.0f) * _steering;
                if (steeringCommandLat > 1.0f)
                    steeringCommandLat = 1.0f;
                else if (steeringCommandLat < -1.0f)
                    steeringCommandLat = -1.0f;
                var steerRadLat = (float)(Math.PI / 180.0) * (_maxSteerDeg * steeringCommandLat);
                var curvatureLat = (float)Math.Tan(steerRadLat) / _wheelbaseM;
                var surfaceTractionModLat = _surfaceTractionFactor > 0f ? _currentSurfaceTractionFactor / _surfaceTractionFactor : 1.0f;
                var gripLat = _tireGripCoefficient * surfaceTractionModLat * _lateralGripCoefficient;
                var maxLatAccelLat = gripLat * 9.80665f;
                var desiredLatAccelLat = curvatureLat * speedMps * speedMps;
                var massFactor = (float)Math.Sqrt(1500f / _massKg);
                if (massFactor > 3.0f)
                    massFactor = 3.0f;
                var stabilityScale = 1.0f - (_highSpeedStability * (speedMps / StabilitySpeedRef) * massFactor);
                if (stabilityScale < 0.2f)
                    stabilityScale = 0.2f;
                else if (stabilityScale > 1.0f)
                    stabilityScale = 1.0f;
                var responseTime = BaseLateralSpeed / 20.0f;
                var maxLatSpeed = maxLatAccelLat * responseTime * stabilityScale;
                var desiredLatSpeed = desiredLatAccelLat * responseTime;
                if (desiredLatSpeed > maxLatSpeed)
                    desiredLatSpeed = maxLatSpeed;
                else if (desiredLatSpeed < -maxLatSpeed)
                    desiredLatSpeed = -maxLatSpeed;
                var lateralSpeed = desiredLatSpeed * surfaceMultiplier;
                _positionX += (lateralSpeed * elapsed);

                if (_frame % 4 == 0)
                {
                    _frame = 0;
                    _brakeFrequency = (int)(11025 + 22050 * _speed / _topSpeed);
                    if (_brakeFrequency != _prevBrakeFrequency)
                    {
                        _soundBrake.SetFrequency(_brakeFrequency);
                        _prevBrakeFrequency = _brakeFrequency;
                    }
                    UpdateEngineFreq();
                }

                var road = _track.RoadComputer(_positionY);
                if (!_finished)
                    Evaluate(road);
            }
            else if (_state == ComputerState.Stopping)
            {
                _speed -= (elapsed * 100 * _deceleration);
                if (_speed < 0)
                    _speed = 0;
                if (_frame % 4 == 0)
                {
                    _frame = 0;
                    UpdateEngineFreq();
                }
                _frame++;
            }

            if (_horning && _state == ComputerState.Running)
            {
                if (!_soundHorn.IsPlaying)
                    _soundHorn.Play(loop: true);
            }
            else
            {
                if (_soundHorn.IsPlaying)
                    _soundHorn.Stop();
            }

            for (var i = _events.Count - 1; i >= 0; i--)
            {
                var e = _events[i];
                if (e.Time < _currentTime())
                {
                    _events.RemoveAt(i);
                    switch (e.Type)
                    {
                        case BotEventType.CarStart:
                            if (!_started())
                            {
                                PushEvent(BotEventType.CarStart, 0.25f);
                                break;
                            }
                            _debugSpeak?.Invoke($"Debug: bot {_playerNumber + 1} engine start.");
                            _soundEngine.SetFrequency(_idleFreq);
                            _soundEngine.Play(loop: true);
                            _state = ComputerState.Running;
                            break;
                        case BotEventType.CarComputerStart:
                            if (!_started())
                            {
                                PushEvent(BotEventType.CarComputerStart, 0.25f);
                                break;
                            }
                            _debugSpeak?.Invoke($"Debug: bot {_playerNumber + 1} start trigger.");
                            Start();
                            break;
                        case BotEventType.CarRestart:
                            if (!_started())
                            {
                                PushEvent(BotEventType.CarRestart, 0.25f);
                                break;
                            }
                            _debugSpeak?.Invoke($"Debug: bot {_playerNumber + 1} restart trigger.");
                            Start();
                            break;
                        case BotEventType.InGear:
                            _switchingGear = 0;
                            break;
                        case BotEventType.StopHorn:
                            _horning = false;
                            break;
                        case BotEventType.StartHorn:
                            _horning = true;
                            break;
                    }
                }
            }
        }

        public void ApplyNetworkState(
            float positionX,
            float positionY,
            float speed,
            int frequency,
            bool engineRunning,
            bool braking,
            bool horning,
            bool backfiring,
            float playerX,
            float playerY,
            float trackLength)
        {
            _positionX = positionX;
            _positionY = positionY;
            _speed = speed;
            _trackLength = trackLength;
            _state = ComputerState.Running;

            _diffX = _positionX - playerX;
            _diffY = _positionY - playerY;
            _diffY = ((_diffY % _trackLength) + _trackLength) % _trackLength;
            if (_diffY > _trackLength / 2)
                _diffY = (_diffY - _trackLength) % _trackLength;

            var elapsed = 0f;
            var now = _currentTime();
            if (_audioInitialized)
            {
                elapsed = now - _lastAudioUpdateTime;
                if (elapsed < 0f)
                    elapsed = 0f;
            }
            _lastAudioUpdateTime = now;
            UpdateSpatialAudio(playerX, playerY, _trackLength, elapsed);

            if (engineRunning)
            {
                if (!_soundEngine.IsPlaying)
                    _soundEngine.Play(loop: true);
                var targetFrequency = frequency > 0 ? frequency : _idleFreq;
                if (_prevFrequency != targetFrequency)
                {
                    _soundEngine.SetFrequency(targetFrequency);
                    _prevFrequency = targetFrequency;
                }
            }
            else if (_soundEngine.IsPlaying)
            {
                _soundEngine.Stop();
            }

            if (braking)
            {
                if (!_soundBrake.IsPlaying)
                    _soundBrake.Play(loop: true);
                var targetBrakeFrequency = (int)(11025 + 22050 * _speed / _topSpeed);
                if (_prevBrakeFrequency != targetBrakeFrequency)
                {
                    _soundBrake.SetFrequency(targetBrakeFrequency);
                    _prevBrakeFrequency = targetBrakeFrequency;
                }
            }
            else if (_soundBrake.IsPlaying)
            {
                _soundBrake.Stop();
            }

            if (horning)
            {
                if (!_soundHorn.IsPlaying)
                    _soundHorn.Play(loop: true);
            }
            else if (_soundHorn.IsPlaying)
            {
                _soundHorn.Stop();
            }

            if (backfiring && !_networkBackfireActive && _soundBackfire != null)
            {
                _soundBackfire.Stop();
                _soundBackfire.SeekToStart();
                _soundBackfire.Play(loop: false);
            }
            _networkBackfireActive = backfiring;
        }

        public void Evaluate(Track.Road road)
        {
            if (_state == ComputerState.Running && _started())
            {
                if (_frame % 4 == 0)
                {
                    _relPos = (_positionX - road.Left) / (_laneWidth * 2.0f);
                    if (_relPos < 0 || _relPos > 1)
                    {
                        var fullCrash = _gear > 1 || _speed >= 50.0f;
                        if (fullCrash)
                            Crash((road.Right + road.Left) / 2);
                        else
                            MiniCrash((road.Right + road.Left) / 2);
                    }
                }
            }

            _surface = road.Surface;
            _frame++;
        }

        public void Pause()
        {
            if (_state == ComputerState.Starting)
                _soundStart.Stop();
            else if (_state == ComputerState.Running || _state == ComputerState.Stopping)
                _soundEngine.Stop();
            if (_soundBrake.IsPlaying)
                _soundBrake.Stop();
            if (_soundHorn.IsPlaying)
                _soundHorn.Stop();
            if (_soundBackfire != null && _soundBackfire.IsPlaying)
            {
                _soundBackfire.Stop();
                _soundBackfire.SeekToStart();
            }
            if (_soundCrash.IsPlaying)
            {
                _soundCrash.Stop();
                _soundCrash.SeekToStart();
            }
        }

        public void Unpause()
        {
            if (_state == ComputerState.Starting)
                _soundStart.Play(loop: false);
            else if (_state == ComputerState.Running || _state == ComputerState.Stopping)
                _soundEngine.Play(loop: true);
        }

        public void Dispose()
        {
            _soundEngine.Dispose();
            _soundHorn.Dispose();
            _soundStart.Dispose();
            _soundCrash.Dispose();
            _soundBrake.Dispose();
            _soundMiniCrash.Dispose();
            _soundBump.Dispose();
            _soundBackfire?.Dispose();
        }

        private void AI()
        {
            var road = _track.RoadComputer(_positionY);
            _relPos = (_positionX - road.Left) / (_laneWidth * 2.0f);
            var nextRoad = _track.RoadComputer(_positionY + CallLength);
            _nextRelPos = (_positionX - nextRoad.Left) / (_laneWidth * 2.0f);
            _currentThrottle = 100;
            _currentSteering = 0;

            if (road.Type == TrackType.HairpinLeft || nextRoad.Type == TrackType.HairpinLeft)
            {
                switch (_difficulty)
                {
                    case 0:
                        if (_relPos > 0.65f)
                            _currentSteering = -100;
                        break;
                    case 1:
                        if (_relPos > 0.55f)
                            _currentSteering = -100;
                        _currentThrottle = 66;
                        break;
                    case 2:
                        if (_relPos > 0.55f)
                            _currentSteering = -100;
                        _currentThrottle = 33;
                        break;
                }
            }
            else if (road.Type == TrackType.HairpinRight || nextRoad.Type == TrackType.HairpinRight)
            {
                switch (_difficulty)
                {
                    case 0:
                        if (_relPos < 0.35f)
                            _currentSteering = 100;
                        break;
                    case 1:
                        if (_relPos < 0.45f)
                            _currentSteering = 100;
                        _currentThrottle = 66;
                        break;
                    case 2:
                        if (_relPos < 0.45f)
                            _currentSteering = 100;
                        _currentThrottle = 33;
                        break;
                }
            }
            else if (_relPos < 0.40f)
            {
                if (_relPos > 0.2f)
                {
                    switch (_difficulty)
                    {
                        case 0:
                            _currentSteering = 100 - _random / 5;
                            break;
                        case 1:
                            _currentSteering = 100 - _random / 10;
                            break;
                        case 2:
                            _currentSteering = 100 - _random / 25;
                            break;
                    }
                }
                else
                {
                    switch (_difficulty)
                    {
                        case 0:
                            _currentSteering = 100 - _random / 10;
                            break;
                        case 1:
                            _currentSteering = 100 - _random / 20;
                            _currentThrottle = 75;
                            break;
                        case 2:
                            _currentSteering = 100;
                            _currentThrottle = 50;
                            break;
                    }
                }
            }
            else if (_relPos > 0.6f)
            {
                if (_relPos < 0.8f)
                {
                    switch (_difficulty)
                    {
                        case 0:
                            _currentSteering = -100 + _random / 5;
                            break;
                        case 1:
                            _currentSteering = -100 + _random / 10;
                            break;
                        case 2:
                            _currentSteering = -100 + _random / 25;
                            break;
                    }
                }
                else
                {
                    switch (_difficulty)
                    {
                        case 0:
                            _currentSteering = -100 + _random / 10;
                            break;
                        case 1:
                            _currentSteering = -100 + _random / 20;
                            _currentThrottle = 75;
                            break;
                        case 2:
                            _currentSteering = -100;
                            _currentThrottle = 50;
                            break;
                    }
                }
            }
        }

        private void Horn()
        {
            var duration = Algorithm.RandomInt(80);
            PushEvent(BotEventType.StartHorn, 0.3f);
            PushEvent(BotEventType.StopHorn, 0.5f + duration / 80.0f);
        }

        private void PushEvent(BotEventType type, float time)
        {
            _events.Add(new BotEvent { Type = type, Time = _currentTime() + time });
        }

        private void UpdateEngineFreq()
        {
            var gearForSound = _engine.GetGearForSpeedKmh(_speed);
            var gearRange = _engine.GetGearRangeKmh(gearForSound);
            var gearMin = _engine.GetGearMinSpeedKmh(gearForSound);

            if (gearForSound == 1)
            {
                var gearSpeed = gearRange <= 0f ? 0f : Math.Min(1.0f, _speed / gearRange);
                _frequency = (int)(gearSpeed * (_topFreq - _idleFreq)) + _idleFreq;
            }
            else
            {
                var gearSpeed = (_speed - gearMin) / (float)gearRange;
                if (gearSpeed < 0.07f)
                {
                    _frequency = (int)(((0.07f - gearSpeed) / 0.07f) * (_topFreq - _shiftFreq) + _shiftFreq);
                    if (_soundBackfire != null)
                    {
                        if (!_backfirePlayedAuto)
                        {
                            if (Algorithm.RandomInt(5) == 1 && !_soundBackfire.IsPlaying)
                                _soundBackfire.Play(loop: false);
                        }
                        _backfirePlayedAuto = true;
                    }
                }
                else
                {
                    _frequency = (int)(gearSpeed * (_topFreq - _shiftFreq) + _shiftFreq);
                    if (_soundBackfire != null && _backfirePlayedAuto)
                        _backfirePlayedAuto = false;
                }
            }

            if (_switchingGear != 0)
                _frequency = (_frequency + _prevFrequency * 2) / 3;

            if (_frequency != _prevFrequency)
            {
                _soundEngine.SetFrequency(_frequency);
                _prevFrequency = _frequency;
            }
        }

        private int CalculateAcceleration()
        {
            var gearRange = _engine.GetGearRangeKmh(_gear);
            var gearMin = _engine.GetGearMinSpeedKmh(_gear);
            var gearCenter = gearMin + (gearRange * 0.18f);
            _speedDiff = _speed - gearCenter;
            var relSpeedDiff = _speedDiff / (gearRange * 1.0f);
            if (Math.Abs(relSpeedDiff) < 1.9f)
            {
                var acceleration = (int)(100 * (0.5f + Math.Cos(relSpeedDiff * Math.PI * 0.5f)));
                return acceleration < 5 ? 5 : acceleration;
            }

            var minAcceleration = (int)(100 * (0.5f + Math.Cos(0.95f * Math.PI)));
            return minAcceleration < 5 ? 5 : minAcceleration;
        }

        private float CalculateDriveRpm(float speedMps, float throttle)
        {
            var wheelCircumference = _wheelRadiusM * 2.0f * (float)Math.PI;
            var gearRatio = _engine.GetGearRatio(_gear);
            var speedBasedRpm = wheelCircumference > 0f
                ? (speedMps / wheelCircumference) * 60f * gearRatio * _finalDriveRatio
                : 0f;
            var launchTarget = _idleRpm + (throttle * (_launchRpm - _idleRpm));
            var rpm = Math.Max(speedBasedRpm, launchTarget);
            if (rpm < _idleRpm)
                rpm = _idleRpm;
            if (rpm > _revLimiter)
                rpm = _revLimiter;
            return rpm;
        }

        private void UpdateAutomaticGear(float elapsed, float speedMps, float throttle, float surfaceTractionMod, float longitudinalGripFactor)
        {
            if (_gears <= 1)
                return;

            if (_autoShiftCooldown > 0f)
            {
                _autoShiftCooldown -= elapsed;
                return;
            }

            var currentAccel = ComputeNetAccelForGear(_gear, speedMps, throttle, surfaceTractionMod, longitudinalGripFactor);
            var bestGear = _gear;
            var bestAccel = currentAccel;

            if (_gear < _gears)
            {
                var upAccel = ComputeNetAccelForGear(_gear + 1, speedMps, throttle, surfaceTractionMod, longitudinalGripFactor);
                if (upAccel > bestAccel)
                {
                    bestAccel = upAccel;
                    bestGear = _gear + 1;
                }
            }

            if (_gear > 1)
            {
                var downAccel = ComputeNetAccelForGear(_gear - 1, speedMps, throttle, surfaceTractionMod, longitudinalGripFactor);
                if (downAccel > bestAccel)
                {
                    bestAccel = downAccel;
                    bestGear = _gear - 1;
                }
            }

            var currentRpm = SpeedToRpm(speedMps, _gear);
            if (_gear < _gears && currentRpm >= _revLimiter * 0.995f)
            {
                ShiftAutomaticGear(_gear + 1);
                return;
            }

            var shiftRpm = _idleRpm + (_revLimiter - _idleRpm) * 0.35f;
            if (_gear > 1 && currentRpm < shiftRpm)
            {
                ShiftAutomaticGear(_gear - 1);
                return;
            }

            if (bestGear != _gear && bestAccel > currentAccel * (1f + AutoShiftHysteresis))
                ShiftAutomaticGear(bestGear);
        }

        private void ShiftAutomaticGear(int newGear)
        {
            if (newGear == _gear)
                return;
            _switchingGear = newGear > _gear ? 1 : -1;
            _gear = newGear;
            PushEvent(BotEventType.InGear, 0.2f);
            _autoShiftCooldown = AutoShiftCooldownSeconds;
        }

        private float ComputeNetAccelForGear(int gear, float speedMps, float throttle, float surfaceTractionMod, float longitudinalGripFactor)
        {
            var rpm = SpeedToRpm(speedMps, gear);
            if (rpm <= 0f)
                return float.NegativeInfinity;
            if (rpm > _revLimiter && gear < _gears)
                return float.NegativeInfinity;

            var engineTorque = CalculateEngineTorqueNm(rpm) * throttle * _powerFactor;
            var gearRatio = _engine.GetGearRatio(gear);
            var wheelTorque = engineTorque * gearRatio * _finalDriveRatio * _drivetrainEfficiency;
            var wheelForce = wheelTorque / _wheelRadiusM;
            var tractionLimit = _tireGripCoefficient * surfaceTractionMod * _massKg * 9.80665f;
            if (wheelForce > tractionLimit)
                wheelForce = tractionLimit;
            wheelForce *= longitudinalGripFactor;

            var dragForce = 0.5f * 1.225f * _dragCoefficient * _frontalAreaM2 * speedMps * speedMps;
            var rollingForce = _rollingResistanceCoefficient * _massKg * 9.80665f;
            var netForce = wheelForce - dragForce - rollingForce;
            return netForce / _massKg;
        }

        private float SpeedToRpm(float speedMps, int gear)
        {
            var wheelCircumference = _wheelRadiusM * 2.0f * (float)Math.PI;
            if (wheelCircumference <= 0f)
                return 0f;
            var gearRatio = _engine.GetGearRatio(gear);
            return (speedMps / wheelCircumference) * 60f * gearRatio * _finalDriveRatio;
        }

        private float CalculateEngineTorqueNm(float rpm)
        {
            if (_peakTorqueNm <= 0f)
                return 0f;
            var clampedRpm = Math.Max(_idleRpm, Math.Min(_revLimiter, rpm));
            if (clampedRpm <= _peakTorqueRpm)
            {
                var denom = _peakTorqueRpm - _idleRpm;
                var t = denom > 0f ? (clampedRpm - _idleRpm) / denom : 0f;
                return SmoothStep(_idleTorqueNm, _peakTorqueNm, t);
            }
            else
            {
                var denom = _revLimiter - _peakTorqueRpm;
                var t = denom > 0f ? (clampedRpm - _peakTorqueRpm) / denom : 0f;
                return SmoothStep(_peakTorqueNm, _redlineTorqueNm, t);
            }
        }

        private static float SmoothStep(float a, float b, float t)
        {
            var clamped = Math.Max(0f, Math.Min(1f, t));
            clamped = clamped * clamped * (3f - 2f * clamped);
            return a + (b - a) * clamped;
        }

        private float CalculateBrakeDecel(float brakeInput, float surfaceDecelMod)
        {
            if (brakeInput <= 0f)
                return 0f;
            var grip = Math.Max(0.1f, _tireGripCoefficient * surfaceDecelMod);
            var decelMps2 = brakeInput * _brakeStrength * grip * 9.80665f;
            return decelMps2 * 3.6f;
        }

        private float CalculateEngineBrakingDecel(float surfaceDecelMod)
        {
            if (_engineBrakingTorqueNm <= 0f || _massKg <= 0f || _wheelRadiusM <= 0f)
                return 0f;
            var rpmRange = _revLimiter - _idleRpm;
            if (rpmRange <= 0f)
                return 0f;
            var rpmFactor = (_engine.Rpm - _idleRpm) / rpmRange;
            if (rpmFactor <= 0f)
                return 0f;
            rpmFactor = Math.Max(0f, Math.Min(1f, rpmFactor));
            var gearRatio = _engine.GetGearRatio(_gear);
            var drivelineTorque = _engineBrakingTorqueNm * _engineBraking * rpmFactor;
            var wheelTorque = drivelineTorque * gearRatio * _finalDriveRatio * _drivetrainEfficiency;
            var wheelForce = wheelTorque / _wheelRadiusM;
            var decelMps2 = (wheelForce / _massKg) * surfaceDecelMod;
            return Math.Max(0f, decelMps2 * 3.6f);
        }

        private void UpdateSpatialAudio(float listenerX, float listenerY, float trackLength, float elapsed)
        {
            var dx = _positionX - listenerX;
            var dz = AudioWorld.WrapDelta(_positionY - listenerY, trackLength);
            var worldX = listenerX + dx;
            var worldZ = listenerY + dz;

            var position = AudioWorld.Position(worldX, worldZ);

            var velocity = Vector3.Zero;
            if (_audioInitialized && elapsed > 0f)
            {
                var velUnits = new Vector3((worldX - _lastAudioPosition.X) / elapsed, 0f, (worldZ - _lastAudioPosition.Z) / elapsed);
                velocity = AudioWorld.ToMeters(velUnits);
            }
            _lastAudioPosition = new Vector3(worldX, 0f, worldZ);
            _audioInitialized = true;

            SetSpatial(_soundEngine, position, velocity);
            SetSpatial(_soundStart, position, velocity);
            SetSpatial(_soundHorn, position, velocity);
            SetSpatial(_soundCrash, position, velocity);
            SetSpatial(_soundBrake, position, velocity);
            SetSpatial(_soundBackfire, position, velocity);
            SetSpatial(_soundBump, position, velocity);
            SetSpatial(_soundMiniCrash, position, velocity);
        }

        private static void SetSpatial(AudioSourceHandle? sound, Vector3 position, Vector3 velocity)
        {
            if (sound == null)
                return;
            sound.SetPosition(position);
            sound.SetVelocity(velocity);
        }

        private AudioSourceHandle CreateRequiredSound(string? path, string label, bool looped = false)
        {
            if (string.IsNullOrWhiteSpace(path))
                throw new InvalidOperationException($"Sound path not provided for {label}.");
            var resolved = path!.Trim();
            if (!File.Exists(resolved))
                throw new FileNotFoundException("Sound file not found.", resolved);
            return looped
                ? _audio.CreateLoopingSource(resolved, useHrtf: true)
                : _audio.CreateSource(resolved, streamFromDisk: true, useHrtf: true);
        }

        private AudioSourceHandle? TryCreateSound(string? path, bool looped = false)
        {
            if (string.IsNullOrWhiteSpace(path))
                return null;
            var resolved = path!.Trim();
            if (!File.Exists(resolved))
                return null;
            return looped
                ? _audio.CreateLoopingSource(resolved, useHrtf: true)
                : _audio.CreateSource(resolved, streamFromDisk: true, useHrtf: true);
        }

        private sealed class BotEvent
        {
            public float Time { get; set; }
            public BotEventType Type { get; set; }
        }

        private enum BotEventType
        {
            CarStart,
            CarComputerStart,
            CarRestart,
            InGear,
            StopHorn,
            StartHorn
        }

        internal enum ComputerState
        {
            Stopped,
            Starting,
            Running,
            Crashing,
            Stopping
        }
    }
}

