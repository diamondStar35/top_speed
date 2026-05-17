using System;
using System.IO;
using TopSpeed.Audio;
using TopSpeed.Core;
using TopSpeed.Core.Settings;
using TopSpeed.Input;
using TS.Audio;
namespace TopSpeed.Drive.Panels
{
    internal sealed partial class RadioVehiclePanel
    {
        private void SaveRadioSettings()
        {
            try
            {
                new SettingsManager().Save(_settings);
            }
            catch
            {
            }
        }

        private void AdjustVolume(int deltaPercent, string feedbackSound)
        {
            var previous = _radio.VolumePercent;
            var target = previous + deltaPercent;
            if (target < 0)
                target = 0;
            else if (target > 100)
                target = 100;

            _radio.SetVolumePercent(target);
            if (target != previous)
                _announce(target + "%");
            PlayFeedback(feedbackSound);
        }

        private void PlayFeedback(string fileName)
        {
            var sound = GetFeedbackSound(fileName);
            if (sound == null)
                return;

            try
            {
                _audio.PlayOneShot(sound, AudioEngineOptions.UiBusName, configure: handle =>
                {
                    handle.SetVolumePercent(_settings, AudioVolumeCategory.OnlineServerEvents, 100);
                });
            }
            catch
            {
            }
        }

        private SoundAsset? GetFeedbackSound(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
                return null;

            ref var cache = ref _volumeDownSound;
            if (string.Equals(fileName, "volume_up.ogg", StringComparison.OrdinalIgnoreCase))
                cache = ref _volumeUpSound;

            if (cache != null)
                return cache;

            var path = Path.Combine(AssetPaths.SoundsRoot, "network", fileName);
            if (!_audio.TryResolvePath(path, out var fullPath))
                return null;

            try
            {
                cache = _audio.LoadAsset(fullPath, streamFromDisk: false);
                return cache;
            }
            catch
            {
                return null;
            }
        }
    }
}


