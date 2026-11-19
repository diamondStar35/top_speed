using System;
using TopSpeed.Audio;
using TopSpeed.Drive.Session.Audio;
using TopSpeed.Input;

namespace TopSpeed.Vehicles.Audio
{
    internal sealed class SurfaceVolume
    {
        private const float StraightVolume = 0.9f;
        private const float SteeringBoost = 0.05f;
        private const float SmoothingSeconds = 0.05f;
        private float _volume = StraightVolume;

        public void Update(int steering, float elapsed)
        {
            if (!float.IsFinite(elapsed) || elapsed <= 0f)
                return;

            var steeringRatio = Math.Min(100f, Math.Abs((float)steering)) / 100f;
            var target = StraightVolume + SteeringBoost * steeringRatio;
            // Time-based smoothing gives binary and progressive controls the same transition.
            var blend = 1f - MathF.Exp(-elapsed / SmoothingSeconds);
            _volume += (target - _volume) * blend;
        }

        public void Reset() => _volume = StraightVolume;

        public void Apply(DriveSettings settings, PlayerVehicleAudio audio, float speed, bool braking)
        {
            var looseVolume = braking
                ? Math.Max(_volume, StraightVolume + Math.Clamp(speed / 500f, 0f, 0.1f))
                : _volume;
            audio.Asphalt.SetVolumeUnit(settings, AudioVolumeCategory.SurfaceLoops, _volume);
            audio.Gravel.SetVolumeUnit(settings, AudioVolumeCategory.SurfaceLoops, looseVolume);
            audio.Water.SetVolumeUnit(settings, AudioVolumeCategory.SurfaceLoops, looseVolume);
            audio.Sand.SetVolumeUnit(settings, AudioVolumeCategory.SurfaceLoops, looseVolume);
            audio.Snow.SetVolumeUnit(settings, AudioVolumeCategory.SurfaceLoops, looseVolume);
        }
    }
}
