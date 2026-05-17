using System;
using TopSpeed.Audio;
using TopSpeed.Data;
using TopSpeed.Input;
using TopSpeed.Vehicles.Events;
using TopSpeed.Input.Devices.Vibration;

namespace TopSpeed.Vehicles
{
    internal partial class Car
    {
        public virtual void Initialize(float positionX = 0, float positionY = 0)
        {
            _positionX = positionX;
            _positionY = Math.Max(0f, positionY);
            _lateralVelocityMps = 0f;
            _yawRateRad = 0f;
            _laneWidth = _track.LaneWidth * 2;
            _stickReleased = true;
            _audioInitialized = false;
            _lastAudioX = positionX;
            _lastAudioY = _positionY;
            _lastAudioElapsed = 0f;
            _vibration?.PlayEffect(VibrationEffectType.Spring);
        }

        public virtual void SetPosition(float positionX, float positionY)
        {
            _positionX = positionX;
            _positionY = Math.Max(0f, positionY);
            _lateralVelocityMps = 0f;
            _yawRateRad = 0f;
        }

        public virtual void FinalizeCar()
        {
            _soundEngine.Stop();
            _soundThrottle?.Stop();
            _soundStop?.Stop();
            _soundFuelWarning.Stop();
            _vibration?.StopEffect(VibrationEffectType.Spring);
        }

        public virtual void Start()
        {
            if (_combustionState == EngineCombustionState.Starting)
                return;
            if (!CanStartEngineWithFuel())
            {
                PlayFuelStartBlockedCue();
                return;
            }
            if (_engineRotationState != EngineRotationState.Stopped)
            {
                ResumeCombustion();
                return;
            }

            var delay = Math.Max(0f, _soundStart.LengthSeconds - 0.1f);
            PushEvent(EventType.CarStart, delay);
            _soundStop?.Stop();
            _soundStart.Restart(loop: false);
            _speed = 0;
            _lateralVelocityMps = 0f;
            _yawRateRad = 0f;
            _engine.ResetForEngineStart();
            _prevFrequency = _idleFreq;
            _frequency = _idleFreq;
            _prevBrakeFrequency = 0;
            _brakeFrequency = 0;
            _prevSurfaceFrequency = 0;
            _surfaceFrequency = 0;
            _switchingGear = 0;
            _autoShiftCooldown = 0f;
            _throttleVolume = 0.0f;
            ClearStallState();
            _soundAsphalt.SetFrequency(_surfaceFrequency);
            _soundGravel.SetFrequency(_surfaceFrequency);
            _soundWater.SetFrequency(_surfaceFrequency);
            _soundSand.SetFrequency(_surfaceFrequency);
            _soundSnow.SetFrequency(_surfaceFrequency);
            RefreshCategoryVolumes(force: true);
            _stickReleased = true;
            _combustionState = EngineCombustionState.Starting;
            SetState(CarState.Starting);
            _listener?.OnStart();
            _vibration?.PlayEffect(VibrationEffectType.Start);
            _vibration?.PlayEffect(VibrationEffectType.Engine);
        }

        public virtual void RestartFromStall()
        {
            if (_combustionState == EngineCombustionState.Starting)
                return;
            if (!CanStartEngineWithFuel())
            {
                PlayFuelStartBlockedCue();
                return;
            }
            if (_engineRotationState != EngineRotationState.Stopped)
            {
                ResumeCombustion();
                return;
            }

            var delay = Math.Max(0f, _soundStart.LengthSeconds - 0.1f);
            PushEvent(EventType.CarStart, delay);
            _soundStop?.Stop();
            _soundStart.Restart(loop: false);
            _switchingGear = 0;
            _autoShiftCooldown = 0f;
            _throttleVolume = 0.0f;
            RefreshCategoryVolumes(force: true);
            _combustionState = EngineCombustionState.Starting;
            SetState(CarState.Starting);
            _vibration?.PlayEffect(VibrationEffectType.Start);
            _vibration?.PlayEffect(VibrationEffectType.Engine);
        }

        /// <summary>
        /// Restarts the car after a crash, preserving distance traveled.
        /// </summary>
        public virtual void RestartAfterCrash()
        {
            if (_combustionState == EngineCombustionState.Starting)
                return;
            if (!CanStartEngineWithFuel())
            {
                PlayFuelStartBlockedCue();
                return;
            }

            var delay = Math.Max(0f, _soundStart.LengthSeconds - 0.1f);
            PushEvent(EventType.CarStart, delay);
            _soundStop?.Stop();
            _soundStart.Restart(loop: false);
            _speed = 0;
            _lateralVelocityMps = 0f;
            _yawRateRad = 0f;
            _engine.ResetForEngineStart();
            _prevFrequency = _idleFreq;
            _frequency = _idleFreq;
            _prevBrakeFrequency = 0;
            _brakeFrequency = 0;
            _prevSurfaceFrequency = 0;
            _surfaceFrequency = 0;
            _switchingGear = 0;
            _autoShiftCooldown = 0f;
            _throttleVolume = 0.0f;
            ClearStallState();
            _soundAsphalt.SetFrequency(_surfaceFrequency);
            _soundGravel.SetFrequency(_surfaceFrequency);
            _soundWater.SetFrequency(_surfaceFrequency);
            _soundSand.SetFrequency(_surfaceFrequency);
            _soundSnow.SetFrequency(_surfaceFrequency);
            RefreshCategoryVolumes(force: true);
            _stickReleased = true;
            _combustionState = EngineCombustionState.Starting;
            SetState(CarState.Starting);
            _listener?.OnStart();
            _vibration?.PlayEffect(VibrationEffectType.Start);
            _vibration?.PlayEffect(VibrationEffectType.Engine);
        }

        public virtual void ShutdownEngine()
        {
            if (_combustionState == EngineCombustionState.Off)
                return;

            _currentThrottle = 0;
            _currentBrake = 0;
            _thrust = 0;
            _switchingGear = 0;
            _autoShiftCooldown = 0f;
            _throttleVolume = 0f;
            _prevThrottleVolume = 0f;
            _combustionState = EngineCombustionState.Off;
            _soundStart.Stop();
            _soundStop?.Restart(loop: false);
            _soundThrottle?.Stop();
            _soundFuelWarning.Stop();
            _vibration?.StopEffect(VibrationEffectType.Engine);
        }

        public virtual void StopMotionImmediately()
        {
            _speed = 0f;
            _speedDiff = 0f;
            _lateralVelocityMps = 0f;
            _yawRateRad = 0f;
            _currentThrottle = 0;
            _currentBrake = 0;
            _thrust = 0;
            _brakeFrequency = 0;
            _prevBrakeFrequency = 0;
            _surfaceFrequency = 0;
            _prevSurfaceFrequency = 0;
            _soundBrake.Stop();
            StopSurfaceLoops();
            _vibration?.StopEffect(VibrationEffectType.CurbLeft);
            _vibration?.StopEffect(VibrationEffectType.CurbRight);
        }

        public virtual void Crash()
        {
            _speed = 0;
            _lateralVelocityMps = 0f;
            _yawRateRad = 0f;
            _engine.ResetForCrash();
            _throttleVolume = 0.0f;
            _soundCrash = SelectRandomCrashHandle();
            _soundCrash.Restart(loop: false);
            _soundStop?.Stop();
            _soundEngine.Stop();
            _soundEngine.SeekToStart();
            if (_soundThrottle != null)
            {
                _soundThrottle.Stop();
                _soundThrottle.SeekToStart();
            }
            _soundStart.SetPanPercent(0);
            switch (_surface)
            {
                case TrackSurface.Asphalt:
                    _soundAsphalt.Stop();
                    _soundAsphalt.SetPanPercent(0);
                    SetSurfaceLoopVolumePercent(_soundAsphalt, 90);
                    break;
                case TrackSurface.Gravel:
                    _soundGravel.Stop();
                    _soundGravel.SetPanPercent(0);
                    SetSurfaceLoopVolumePercent(_soundGravel, 90);
                    break;
                case TrackSurface.Water:
                    _soundWater.Stop();
                    _soundWater.SetPanPercent(0);
                    SetSurfaceLoopVolumePercent(_soundWater, 90);
                    break;
                case TrackSurface.Sand:
                    _soundSand.Stop();
                    _soundSand.SetPanPercent(0);
                    SetSurfaceLoopVolumePercent(_soundSand, 90);
                    break;
                case TrackSurface.Snow:
                    _soundSnow.Stop();
                    _soundSnow.SetPanPercent(0);
                    SetSurfaceLoopVolumePercent(_soundSnow, 90);
                    break;
            }
            _soundBrake.Stop();
            _soundBrake.SeekToStart();
            _soundBrake.SetPanPercent(0);
            _soundFuelWarning.Stop();
            _soundFuelWarning.SeekToStart();
            if (_hasWipers == 1 && _soundWipers != null)
            {
                _soundWipers.Stop();
                _soundWipers.SeekToStart();
                _soundWipers.SetPanPercent(0);
            }
            _soundHorn.Stop();
            _soundHorn.SeekToStart();
            _soundHorn.SetPanPercent(0);
            _gear = NeutralGear;
            _switchingGear = 0;
            _autoShiftCooldown = 0f;
            _combustionState = EngineCombustionState.Off;
            SetEngineRotationState(EngineRotationState.Stopped);
            ClearStallState();
            SetState(CarState.Crashing);
            PushEvent(EventType.CrashComplete, _soundCrash.LengthSeconds + 1.25f);
            _listener?.OnCrash();
            _vibration?.StopEffect(VibrationEffectType.Engine);
            _vibration?.PlayEffect(VibrationEffectType.Crash);
            PushEvent(EventType.StopVibration, CrashVibrationSeconds, VibrationEffectType.Crash);
            _vibration?.StopEffect(VibrationEffectType.CurbLeft);
            _vibration?.StopEffect(VibrationEffectType.CurbRight);
        }

        public virtual void MiniCrash(float newPosition)
        {
            _speed /= 4;
            _lateralVelocityMps = 0f;
            _yawRateRad = 0f;
            if (_positionX < newPosition)
                _vibration?.PlayEffect(VibrationEffectType.BumpLeft);
            if (_positionX > newPosition)
                _vibration?.PlayEffect(VibrationEffectType.BumpRight);
            PushEvent(EventType.StopBumpVibration, BumpVibrationSeconds);

            _positionX = newPosition;
            _throttleVolume = 0.0f;
            _soundMiniCrash.SeekToStart();
            _soundMiniCrash.Play(loop: false);
        }

        public virtual void Bump(float bumpX, float bumpY, float speedDeltaKph)
        {
            if (bumpY != 0f)
            {
                var currentLapStart = GetLapStartPosition(_positionY);
                _positionY += bumpY;
                if (_positionY < currentLapStart)
                    _positionY = currentLapStart;
                if (_positionY < 0f)
                    _positionY = 0f;
            }

            if (bumpX > 0f)
            {
                _positionX += 2 * bumpX;
                _vibration?.PlayEffect(VibrationEffectType.BumpLeft);
            }
            else if (bumpX < 0f)
            {
                _positionX += 2 * bumpX;
                _vibration?.PlayEffect(VibrationEffectType.BumpRight);
            }

            _speed += speedDeltaKph;
            if (_speed < 0)
                _speed = 0;
            _lateralVelocityMps = 0f;
            _yawRateRad = 0f;
            if (!_soundBump.IsPlaying)
                _soundBump.Play(loop: false);
            PushEvent(EventType.StopBumpVibration, BumpVibrationSeconds);
        }

        public virtual void Stop()
        {
            _soundBrake.Stop();
            _soundFuelWarning.Stop();
            _soundWipers?.Stop();
            StopSurfaceLoops();
            _vibration?.StopEffect(VibrationEffectType.CurbLeft);
            _vibration?.StopEffect(VibrationEffectType.CurbRight);
            if (_combustionState != EngineCombustionState.Off || _engineRotationState != EngineRotationState.Stopped)
            {
                _combustionState = EngineCombustionState.Off;
                _soundThrottle?.Stop();
                _soundStop?.Restart(loop: false);
                _vibration?.StopEffect(VibrationEffectType.Engine);
                SetState(CarState.Stopping);
            }
            else
            {
                SetState(CarState.Stopped);
            }
        }

        public virtual void Quiet()
        {
            _soundBrake.Stop();
            _soundFuelWarning.Stop();
            SetPlayerEngineVolumePercent(_soundEngine, 90);
            _soundThrottle?.Stop();
            for (var i = 0; i < _soundBackfireVariants.Length; i++)
                SetPlayerEventVolumePercent(_soundBackfireVariants[i], 90);
            SetSurfaceLoopVolumePercent(_soundAsphalt, 90);
            SetSurfaceLoopVolumePercent(_soundGravel, 90);
            SetSurfaceLoopVolumePercent(_soundWater, 90);
            SetSurfaceLoopVolumePercent(_soundSand, 90);
            SetSurfaceLoopVolumePercent(_soundSnow, 90);
            _vibration?.StopEffect(VibrationEffectType.CurbLeft);
            _vibration?.StopEffect(VibrationEffectType.CurbRight);
            _vibration?.StopEffect(VibrationEffectType.Engine);
        }

        public virtual void Dispose()
        {
            StopAllVibrations();
            _raceAudio.Dispose();
        }

        private void StopAllVibrations()
        {
            if (_vibration == null)
                return;
            foreach (VibrationEffectType effect in Enum.GetValues(typeof(VibrationEffectType)))
                _vibration.StopEffect(effect);
        }

        private void CompleteStop()
        {
            _speed = 0f;
            _speedDiff = 0f;
            _engine.StopEngine();
            SetEngineRotationState(EngineRotationState.Stopped);
            _soundFuelWarning.Stop();
            StopSurfaceLoops();
            SetState(CarState.Stopped);
        }

        private void ResumeCombustion()
        {
            if (!CanStartEngineWithFuel())
            {
                PlayFuelStartBlockedCue();
                return;
            }

            _soundStop?.Stop();
            _soundStart.Stop();
            _switchingGear = 0;
            _autoShiftCooldown = 0f;
            _throttleVolume = 0.0f;
            _prevThrottleVolume = 0.0f;
            ClearStallState();
            _combustionState = EngineCombustionState.On;
            SetState(CarState.Running);
            _listener?.OnStart();
            _vibration?.PlayEffect(VibrationEffectType.Engine);
        }
    }
}

