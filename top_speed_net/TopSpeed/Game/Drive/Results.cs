using System;
using System.IO;
using TopSpeed.Audio;
using TopSpeed.Core;
using TopSpeed.Input;
using TopSpeed.Drive;
using TS.Audio;

namespace TopSpeed.Game
{
    internal sealed partial class Game
    {
        private void ShowRaceResultDialog(DriveResultSummary summary, Action? onClosed = null)
        {
            _resultShow.Show(summary, onClosed);
        }

        private void PlayRaceWinSound()
        {
            var audio = _audio as AudioManager;
            if (audio == null)
                return;

            if (_raceWinSound == null)
            {
                if (!TryLoadRaceWinSound(out var handle))
                    return;
                _raceWinSound = handle;
            }

            try
            {
                var handle = _raceWinSound;
                if (handle == null)
                    return;
                audio.PlayOneShot(handle, AudioEngineOptions.UiBusName, configure: sound =>
                {
                    sound.SetVolumePercent(_settings, AudioVolumeCategory.OnlineServerEvents, 100);
                });
            }
            catch
            {
            }
        }

        private bool TryLoadRaceWinSound(out SoundAsset? handle)
        {
            handle = null;
            var audio = _audio as AudioManager;
            if (audio == null)
                return false;

            var path = Path.Combine(AssetPaths.SoundsRoot, "network", "win.ogg");
            if (!audio.TryResolvePath(path, out var fullPath))
                return false;

            try
            {
                handle = audio.LoadAsset(fullPath, streamFromDisk: false);
                return handle != null;
            }
            catch
            {
                return false;
            }
        }
    }
}



