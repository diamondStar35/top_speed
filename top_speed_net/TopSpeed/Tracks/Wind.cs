using System;
using TopSpeed.Data;
using TopSpeed.Physics.Powertrain;

namespace TopSpeed.Tracks
{
    internal sealed partial class Track
    {
        public ResistanceEnvironment GetResistanceEnvironment()
        {
            return _activeWeatherProfile.ToResistanceEnvironment();
        }

        public TrackWeatherProfile GetActiveWeatherProfile()
        {
            return _activeWeatherProfile;
        }

        private void InitializeWeatherRuntime()
        {
            _lastWeatherUpdateUtc = DateTime.UtcNow;
            _activeWeatherProfile = ResolveWeatherProfile(0);
            _weatherTransitionFrom = _activeWeatherProfile;
            _weatherTransitionTo = _activeWeatherProfile;
            _weatherTransitionElapsedSeconds = 0f;
            _weatherTransitionSeconds = 0f;
            EnsureWeatherLoopsPlaying();
            ApplyWeatherAudio();
        }

        private void UpdateWeather(int segmentIndex)
        {
            var now = DateTime.UtcNow;
            var deltaSeconds = (float)(now - _lastWeatherUpdateUtc).TotalSeconds;
            if (deltaSeconds < 0f)
                deltaSeconds = 0f;
            if (deltaSeconds > 0.5f)
                deltaSeconds = 0.5f;
            _lastWeatherUpdateUtc = now;

            if (segmentIndex >= 0)
            {
                var targetProfile = ResolveWeatherProfile(segmentIndex);
                var targetId = targetProfile.Id;
                if (!string.Equals(_weatherTransitionTo.Id, targetId, StringComparison.OrdinalIgnoreCase))
                {
                    _weatherTransitionFrom = _activeWeatherProfile;
                    _weatherTransitionTo = targetProfile;
                    _weatherTransitionElapsedSeconds = 0f;
                    _weatherTransitionSeconds = ResolveWeatherTransitionSeconds(segmentIndex);
                    if (_weatherTransitionSeconds <= 0f)
                        _activeWeatherProfile = _weatherTransitionTo;
                }
            }

            if (_weatherTransitionSeconds > 0f &&
                !string.Equals(_weatherTransitionFrom.Id, _weatherTransitionTo.Id, StringComparison.OrdinalIgnoreCase))
            {
                _weatherTransitionElapsedSeconds += deltaSeconds;
                var t = _weatherTransitionElapsedSeconds / _weatherTransitionSeconds;
                if (t >= 1f)
                {
                    _activeWeatherProfile = _weatherTransitionTo;
                    _weatherTransitionSeconds = 0f;
                    _weatherTransitionElapsedSeconds = 0f;
                }
                else
                {
                    _activeWeatherProfile = TrackWeatherProfile.Blend(_weatherTransitionFrom, _weatherTransitionTo, t);
                }
            }
            else if (_weatherTransitionSeconds <= 0f)
            {
                _activeWeatherProfile = _weatherTransitionTo;
            }

            ApplyWeatherAudio();
        }

        private TrackWeatherProfile ResolveWeatherProfile(int segmentIndex)
        {
            string? profileId = null;
            if (segmentIndex >= 0 && segmentIndex < _segmentCount)
                profileId = _definition[segmentIndex].WeatherProfileId;

            if (!string.IsNullOrWhiteSpace(profileId) &&
                _weatherProfiles.TryGetValue(profileId!, out var profile))
            {
                return profile;
            }

            if (_weatherProfiles.TryGetValue(_defaultWeatherProfileId, out var defaultProfile))
                return defaultProfile;

            return TrackWeatherProfile.CreatePreset(TrackWeatherProfile.DefaultProfileId, _defaultWeatherKind);
        }

        private float ResolveWeatherTransitionSeconds(int segmentIndex)
        {
            if (segmentIndex < 0 || segmentIndex >= _segmentCount)
                return 0f;
            var seconds = _definition[segmentIndex].WeatherTransitionSeconds;
            return seconds < 0f ? 0f : seconds;
        }

        private void EnsureWeatherLoopsPlaying()
        {
            if (_soundRain != null && !_soundRain.IsPlaying)
                _soundRain.Play(loop: true);
            if (_soundWind != null && !_soundWind.IsPlaying)
                _soundWind.Play(loop: true);
            if (_soundStorm != null && !_soundStorm.IsPlaying)
                _soundStorm.Play(loop: true);
        }

        private void ApplyWeatherAudio()
        {
            ApplyWeatherVolume(_soundRain, _activeWeatherProfile.RainGain);
            ApplyWeatherVolume(_soundWind, _activeWeatherProfile.WindGain);
            ApplyWeatherVolume(_soundStorm, _activeWeatherProfile.StormGain);
        }

        private void ApplyWeatherVolume(TS.Audio.Source? handle, float gain)
        {
            handle?.SetVolume(Clamp01(gain) * _ambientVolumeScale);
        }

        private static float Clamp01(float value)
        {
            if (value < 0f)
                return 0f;
            if (value > 1f)
                return 1f;
            return value;
        }
    }
}
