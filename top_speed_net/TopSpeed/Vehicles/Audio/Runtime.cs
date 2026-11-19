using TopSpeed.Data;

namespace TopSpeed.Vehicles
{
    internal partial class Car
    {
        public virtual void BrakeSound()
        {
            _surfaceVolume.Apply(_settings, _raceAudio, _speed, braking: true);
            if (_surface == TrackSurface.Asphalt)
            {
                if (!_soundBrake.IsPlaying)
                    _soundBrake.Play(loop: true);
            }
            else if (_soundBrake.IsPlaying)
                _soundBrake.Stop();
        }

        public virtual void BrakeCurveSound()
        {
            if (_soundBrake.IsPlaying)
                _soundBrake.Stop();
            _surfaceVolume.Apply(_settings, _raceAudio, _speed, braking: false);
        }

        public virtual bool Backfiring() => AnyBackfirePlaying();

        public virtual void Pause()
        {
            if (_soundStop != null && _soundStop.IsPlaying)
                _soundStop.Stop();
            _audioFlow.Pause(
                _surface,
                _soundEngine,
                _soundThrottle,
                _soundBrake,
                _soundHorn,
                _soundFuelWarning,
                _soundWipers,
                _soundAsphalt,
                _soundGravel,
                _soundWater,
                _soundSand,
                _soundSnow,
                StopResetBackfireVariants);
        }

        public virtual void Unpause()
        {
            var resumeEngine = _engineRotationState != EngineRotationState.Stopped
                && _combustionState != EngineCombustionState.Starting;
            var resumeThrottle = resumeEngine
                && _combustionState == EngineCombustionState.On
                && _soundThrottle != null
                && _currentThrottle > 0;
            var resumeWipers = _soundWipers != null
                && _hasWipers == 1
                && _combustionState == EngineCombustionState.On;
            var resumeSurfaceLoops = _speed > 0f;

            _audioFlow.Unpause(
                _surface,
                resumeEngine,
                resumeThrottle,
                resumeWipers,
                resumeSurfaceLoops,
                _soundEngine,
                _soundThrottle,
                _soundWipers,
                _soundAsphalt,
                _soundGravel,
                _soundWater,
                _soundSand,
                _soundSnow);
        }
    }
}

