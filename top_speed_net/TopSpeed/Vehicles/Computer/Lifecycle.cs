using System;
using TopSpeed.Audio;
using TopSpeed.Common;

namespace TopSpeed.Vehicles
{
    internal sealed partial class ComputerPlayer
    {
        public void Initialize(float positionX, float positionY, float trackLength)
        {
            _positionX = positionX;
            _positionY = Math.Max(0f, positionY);
            _lateralVelocityMps = 0f;
            _yawRateRad = 0f;
            _tireWearFraction = 0f;
            _tireTemperatureC = float.NaN;
            _tireTreadTemperatureC = float.NaN;
            _tireCarcassTemperatureC = float.NaN;
            _tireSmoothedInputs = default;
            _surfaceTemperatureC = float.NaN;
            _driverState = default;
            _drivingPreviewRefreshSeconds = 0f;
            _trackLength = trackLength;
            _remoteNetInit = false;
            _remoteTargetX = _positionX;
            _remoteTargetY = _positionY;
            _remoteTargetSpeed = _speed;
            _audioInitialized = false;
            _lastAudioPosition = new System.Numerics.Vector3(positionX, 0f, _positionY);
            _lastAudioUpdateTime = 0f;
        }

        public void FinalizePlayer()
        {
            _soundEngine.Stop();
            _radio.PauseForGame();
            _liveRadio.Stop(0);
        }

        public void PendingStart(float baseDelay)
        {
            float difficultyDelay;
            var randomValue = Algorithm.RandomInt(100) / 100f;

            switch (_difficulty)
            {
                case 2:
                    difficultyDelay = 0.1f + (randomValue * 0.4f);
                    break;
                case 1:
                    difficultyDelay = 1.0f + (randomValue * 1.5f);
                    break;
                case 0:
                default:
                    difficultyDelay = 2.5f + (randomValue * 2.5f);
                    break;
            }

            var startTime = baseDelay + difficultyDelay;
            PushEvent(BotEventType.CarComputerStart, startTime);
        }

        public void Start()
        {
            var delay = Math.Max(0f, _soundStart.LengthSeconds - 0.1f);
            PushEvent(BotEventType.CarStart, delay);
            _soundStart.Play(loop: false);
            _speed = 0;
            _lateralVelocityMps = 0f;
            _yawRateRad = 0f;
            _prevFrequency = _idleFreq;
            _frequency = _idleFreq;
            _prevBrakeFrequency = 0;
            _brakeFrequency = 0;
            _state = ComputerState.Starting;
        }

        public void Crash(float newPosition, bool scheduleRestart = true)
        {
            var crashRoad = _track.RoadComputer(_positionY);
            var crashRoadCenterX = (crashRoad.Left + crashRoad.Right) * 0.5f;
            _crashLateralFromCenter = _positionX - crashRoadCenterX;
            _crashLateralAnchored = true;

            _speed = 0;
            _lateralVelocityMps = 0f;
            _yawRateRad = 0f;
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
            _driverState = default;
            if (scheduleRestart)
                PushEvent(BotEventType.CarRestart, _soundCrash.LengthSeconds + 1.25f);
        }

        public void MiniCrash(float newPosition)
        {
            _speed /= 4;
            _lateralVelocityMps = 0f;
            _yawRateRad = 0f;
            _positionX = newPosition;
            Bots.BotDrivingPlanner.NotifyContact(ref _driverState);
            _soundMiniCrash.Play(loop: false);
        }

        public void Bump(float bumpX, float bumpY, float speedDeltaKph)
        {
            if (bumpY != 0f)
            {
                _positionY += bumpY;
                if (_positionY < 0f)
                    _positionY = 0f;
            }

            if (bumpX > 0f)
            {
                _positionX += 2 * bumpX;
            }
            else if (bumpX < 0f)
            {
                _positionX += 2 * bumpX;
            }

            _speed += speedDeltaKph;
            if (_speed < 0)
                _speed = 0;
            _lateralVelocityMps = 0f;
            _yawRateRad = 0f;
            Bots.BotDrivingPlanner.NotifyContact(ref _driverState);
            if (!_soundBump.IsPlaying)
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
            SetOtherEngineVolumePercent(_soundEngine, 80);
            if (_soundBackfire != null)
                SetOtherEventVolumePercent(_soundBackfire, 80);
        }

        public void Pause()
        {
            _radio.PauseForGame();
            _liveRadio.PauseForGame();
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
            _radio.ResumeFromGame();
            _liveRadio.ResumeFromGame();
            if (_state == ComputerState.Starting)
                _soundStart.Play(loop: false);
            else if (_state == ComputerState.Running || _state == ComputerState.Stopping)
                _soundEngine.Play(loop: true);
        }

        public void Dispose()
        {
            _raceAudio.Dispose();
            _radio.Dispose();
            _liveRadio.Dispose();
        }
    }
}

