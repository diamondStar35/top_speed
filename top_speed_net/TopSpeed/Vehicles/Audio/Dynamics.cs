using System;
using TopSpeed.Audio;
using TopSpeed.Input;
using TopSpeed.Data;
using TopSpeed.Input.Devices.Vibration;

namespace TopSpeed.Vehicles
{
    internal partial class Car
    {
        private void UpdateThrottleLoopAudio(float elapsed)
        {
            if (_soundThrottle == null)
                return;

            if (_combustionState == EngineCombustionState.On && _soundEngine.IsPlaying)
            {
                var throttlePercent = Math.Max(0f, Math.Min(100f, _currentThrottle));
                var throttleRatio = throttlePercent / 100f;
                var speedFloor = _topSpeed > 0f ? (_speed * 95f / _topSpeed) : 0f;
                if (speedFloor < 0f)
                    speedFloor = 0f;
                if (speedFloor > 95f)
                    speedFloor = 95f;

                if (throttlePercent > 0f)
                {
                    if (!_soundThrottle.IsPlaying)
                    {
                        var startVolume = Math.Max(speedFloor, 20f + (80f * throttleRatio));
                        _throttleVolume = startVolume;
                        SetPlayerEngineVolumePercent(_soundThrottle, (int)_throttleVolume);
                        _prevThrottleVolume = _throttleVolume;
                        _soundThrottle.Play(loop: true);
                    }
                    else
                    {
                        var targetVolume = Math.Max(speedFloor, 20f + (80f * throttleRatio));
                        var blend = Math.Min(1f, elapsed * 10f);
                        _throttleVolume += (targetVolume - _throttleVolume) * blend;
                        if (_throttleVolume > 100.0f)
                            _throttleVolume = 100.0f;
                        if (_throttleVolume < 0.0f)
                            _throttleVolume = 0.0f;
                        if ((int)_throttleVolume != (int)_prevThrottleVolume)
                        {
                            SetPlayerEngineVolumePercent(_soundThrottle, (int)_throttleVolume);
                            _prevThrottleVolume = _throttleVolume;
                        }
                    }
                }
                else
                {
                    var blend = Math.Min(1f, elapsed * 6f);
                    _throttleVolume += (speedFloor - _throttleVolume) * blend;
                    if ((int)_throttleVolume != (int)_prevThrottleVolume)
                    {
                        SetPlayerEngineVolumePercent(_soundThrottle, (int)_throttleVolume);
                        _prevThrottleVolume = _throttleVolume;
                    }
                }
            }
            else if (_soundThrottle.IsPlaying)
            {
                _soundThrottle.Stop();
            }
        }

        private void UpdateBrakeAndSteeringOutput()
        {
            var brakingInput = Math.Max(0, -_currentBrake);
            var isBraking = brakingInput >= 15 && _speed > 0;
            var speedRatio = NormalizeSpeedByTopSpeed(_speed, 1f);
            if (isBraking)
            {
                BrakeSound();
                if (_thrust < -50)
                {
                    _vibration?.Gain(VibrationEffectType.Spring, (int)(50.0f * speedRatio));
                    _currentSteering = (_currentSteering * 2) / 3;
                }
            }
            else if (_currentSteering != 0 && _speed > _topSpeed / 2)
            {
                BrakeCurveSound();
            }
            else
            {
                if (_soundBrake.IsPlaying)
                    _soundBrake.Stop();
                SetSurfaceLoopVolumePercent(_soundAsphalt, 90);
                SetSurfaceLoopVolumePercent(_soundGravel, 90);
                SetSurfaceLoopVolumePercent(_soundWater, 90);
                SetSurfaceLoopVolumePercent(_soundSand, 90);
                SetSurfaceLoopVolumePercent(_soundSnow, 90);
            }
        }

        private void UpdateFrameAudioAndFeedback()
        {
            // Keep engine pitch tightly synced with RPM, even between batched audio updates.
            UpdateEngineFreq();

            if (_frame % 4 != 0)
                return;

            _frame = 0;
            var speedRatio = NormalizeSpeedByTopSpeed(_speed, 1f);
            _brakeFrequency = (int)(11025 + (22050 * speedRatio));
            if (_brakeFrequency != _prevBrakeFrequency)
            {
                _soundBrake.SetFrequency(_brakeFrequency);
                _prevBrakeFrequency = _brakeFrequency;
            }

            var brakePercent = Math.Max(0f, Math.Min(100f, -_currentBrake));
            var speedVolume = _speed <= 50.0f ? (100f - (50f - _speed)) : 100f;
            var inputVolume = brakePercent <= 0f ? 0f : (25f + (75f * (brakePercent / 100f)));
            var brakeVolume = (int)Math.Round(speedVolume * (inputVolume / 100f));
            SetPlayerEventVolumePercent(_soundBrake, brakeVolume);

            UpdateSoundRoad();

            if (_vibration == null)
                return;

            if (_surface == TrackSurface.Gravel)
                _vibration.Gain(VibrationEffectType.Gravel, (int)(speedRatio * 10000));
            else
                _vibration.Gain(VibrationEffectType.Gravel, 0);

            if (_speed == 0)
                _vibration.Gain(VibrationEffectType.Spring, 10000);
            else
                _vibration.Gain(VibrationEffectType.Spring, (int)(10000 * speedRatio));

            var lowSpeedLimit = Math.Max(1f, _topSpeed / 10f);
            var throttleRatio = Math.Max(0f, Math.Min(100f, _currentThrottle)) / 100f;
            var coupledIdle = _combustionState == EngineCombustionState.On
                && !IsNeutralGear()
                && _drivelineCouplingFactor > 0.20f;
            if (coupledIdle && _speed < lowSpeedLimit)
            {
                var speedFactor = 1f - Math.Max(0f, Math.Min(1f, _speed / lowSpeedLimit));
                var throttleFactor = 1f - Math.Max(0f, Math.Min(1f, throttleRatio * 1.5f));
                _vibration.Gain(VibrationEffectType.Engine, (int)Math.Round(10000f * speedFactor * throttleFactor));
            }
            else
            {
                _vibration.Gain(VibrationEffectType.Engine, 0);
            }
        }

        private void EnsureSurfaceLoopPlaying()
        {
            if (_speed <= 0f)
            {
                StopSurfaceLoops();
                return;
            }

            switch (_surface)
            {
                case TrackSurface.Asphalt:
                    EnsureSurfaceLoop(_soundAsphalt, Math.Min(_surfaceFrequency, MaxSurfaceFreq));
                    break;
                case TrackSurface.Gravel:
                    EnsureSurfaceLoop(_soundGravel, Math.Min(_surfaceFrequency, MaxSurfaceFreq));
                    break;
                case TrackSurface.Water:
                    EnsureSurfaceLoop(_soundWater, Math.Min(_surfaceFrequency, MaxSurfaceFreq));
                    break;
                case TrackSurface.Sand:
                    EnsureSurfaceLoop(_soundSand, (int)(_surfaceFrequency / 2.5f));
                    break;
                case TrackSurface.Snow:
                    EnsureSurfaceLoop(_soundSnow, Math.Min(_surfaceFrequency, MaxSurfaceFreq));
                    break;
            }
        }

        private static void EnsureSurfaceLoop(TS.Audio.Source sound, int frequency)
        {
            if (sound.IsPlaying)
                return;
            sound.SetFrequency(frequency);
            sound.Play(loop: true);
        }
    }
}

