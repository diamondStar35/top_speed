using System;
using TopSpeed.Audio;
using TopSpeed.Data;
using TopSpeed.Input;
using TopSpeed.Physics.Powertrain;
using TopSpeed.Tracks;
using TS.Audio;

namespace TopSpeed.Vehicles
{
    internal partial class Car
    {
        private void UpdateEngineFreq()
        {
            UpdateEngineFreqManual();
            SyncEngineLoopPlayback();
        }

        private void UpdateEngineFreqManual()
        {
            _frequency = EnginePitch.FromRpm(
                _engine.Rpm,
                _engine.StallRpm,
                _engine.IdleRpm,
                _engine.RevLimiter,
                _idleFreq,
                _topFreq,
                _pitchCurveExponent);

            if (_frequency == _prevFrequency)
                return;

            _soundEngine.SetFrequency(_frequency);
            if (_soundThrottle != null)
            {
                if ((int)_throttleVolume != (int)_prevThrottleVolume)
                {
                    SetPlayerEngineVolumePercent(_soundThrottle, (int)_throttleVolume);
                    _prevThrottleVolume = _throttleVolume;
                }
                _soundThrottle.SetFrequency(_frequency);
            }
            _prevFrequency = _frequency;
        }

        private void UpdateSoundRoad()
        {
            _audioFlow.UpdateRoad(
                _surface,
                _speed,
                ref _surfaceFrequency,
                ref _prevSurfaceFrequency,
                _soundAsphalt,
                _soundGravel,
                _soundWater,
                _soundSand,
                _soundSnow);
        }

        private void StopSurfaceLoops()
        {
            _soundAsphalt.Stop();
            _soundGravel.Stop();
            _soundWater.Stop();
            _soundSand.Stop();
            _soundSnow.Stop();
        }

        private void SetEngineRotationState(EngineRotationState nextState)
        {
            if (_engineRotationState == nextState)
                return;

            var previousState = _engineRotationState;
            _engineRotationState = nextState;

            if (previousState == EngineRotationState.Stopped
                && nextState != EngineRotationState.Stopped
                && _combustionState != EngineCombustionState.Starting)
            {
                if (!_soundEngine.IsPlaying)
                {
                    _soundEngine.SeekToStart();
                    _soundEngine.SetFrequency(_frequency > 0 ? _frequency : _idleFreq);
                    _soundEngine.Play(loop: true);
                }
            }
            else if (previousState != EngineRotationState.Stopped
                && nextState == EngineRotationState.Stopped
                && _soundEngine.IsPlaying)
            {
                _soundEngine.Stop(EngineShutdownFadeSeconds);
            }
        }

        private void UpdateEngineRotationState(EngineCouplingMode couplingMode, float rawCoupledDriveRpm)
        {
            if (_engine.Rpm <= 1f)
            {
                SetEngineRotationState(EngineRotationState.Stopped);
                return;
            }

            var drivelineDriven = _combustionState == EngineCombustionState.Off
                && couplingMode != EngineCouplingMode.Disengaged
                && !IsNeutralGear()
                && _drivelineCouplingFactor > 0.05f
                && rawCoupledDriveRpm > _engine.StallRpm;
            SetEngineRotationState(drivelineDriven
                ? EngineRotationState.DrivelineDriven
                : EngineRotationState.FreeSpinning);
        }

        private void SyncEngineLoopPlayback()
        {
            var shouldPlay = _engineRotationState != EngineRotationState.Stopped
                && _combustionState != EngineCombustionState.Starting;
            if (shouldPlay)
            {
                if (!_soundEngine.IsPlaying)
                {
                    _soundEngine.SeekToStart();
                    _soundEngine.Play(loop: true);
                }
            }
            else if (_soundEngine.IsPlaying)
            {
                _soundEngine.Stop(EngineShutdownFadeSeconds);
            }
        }

        private void SwitchSurfaceSound(TrackSurface surface)
        {
            switch (surface)
            {
                case TrackSurface.Gravel:
                    _soundGravel.SetFrequency(Math.Min(_surfaceFrequency, MaxSurfaceFreq));
                    _soundGravel.Play(loop: true);
                    break;
                case TrackSurface.Water:
                    _soundWater.SetFrequency(Math.Min(_surfaceFrequency, MaxSurfaceFreq));
                    _soundWater.Play(loop: true);
                    break;
                case TrackSurface.Sand:
                    _soundSand.SetFrequency((int)(_surfaceFrequency / 2.5f));
                    _soundSand.Play(loop: true);
                    break;
                case TrackSurface.Snow:
                    _soundSnow.SetFrequency(Math.Min(_surfaceFrequency, MaxSurfaceFreq));
                    _soundSnow.Play(loop: true);
                    break;
                case TrackSurface.Asphalt:
                    _soundAsphalt.SetFrequency(Math.Min(_surfaceFrequency, MaxSurfaceFreq));
                    _soundAsphalt.Play(loop: true);
                    break;
            }
        }

        private void ApplyPan(int pan)
        {
            _audioFlow.ApplyPan(
                _surface,
                pan,
                _soundHorn,
                _soundBrake,
                _soundBackfire,
                _soundWipers,
                _soundAsphalt,
                _soundGravel,
                _soundWater,
                _soundSand,
                _soundSnow);
        }

        private int CalculatePan(float relPos)
        {
            return _audioFlow.CalculatePan(relPos);
        }

        private void RefreshCategoryVolumes(bool force = false)
        {
            _audioFlow.RefreshVolumes(
                _settings,
                force,
                (int)Math.Round(_throttleVolume),
                _soundEngine,
                _soundStart,
                _soundThrottle,
                _soundHorn,
                _soundBrake,
                _soundMiniCrash,
                _soundBump,
                _soundBadSwitch,
                _soundFuelWarning,
                _soundWipers,
                _soundCrash,
                _soundBackfire,
                _soundCrashVariants,
                _soundBackfireVariants,
                _soundAsphalt,
                _soundGravel,
                _soundWater,
                _soundSand,
                _soundSnow,
                ref _lastPlayerEngineVolumePercent,
                ref _lastPlayerEventsVolumePercent,
                ref _lastSurfaceLoopVolumePercent);
            SetPlayerEventVolumePercent(_soundStop, 100);
        }

        private void SetPlayerEngineVolumePercent(Source? sound, int percent)
        {
            sound.SetVolumePercent(_settings, AudioVolumeCategory.PlayerVehicleEngine, percent);
        }

        private void SetPlayerEventVolumePercent(Source? sound, int percent)
        {
            sound.SetVolumePercent(_settings, AudioVolumeCategory.PlayerVehicleEvents, percent);
        }

        private void SetSurfaceLoopVolumePercent(Source? sound, int percent)
        {
            sound.SetVolumePercent(_settings, AudioVolumeCategory.SurfaceLoops, percent);
        }
    }
}

