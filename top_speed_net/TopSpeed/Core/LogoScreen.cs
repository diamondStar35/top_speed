using System;
using System.IO;
using SharpDX.DirectInput;
using TopSpeed.Audio;
using TopSpeed.Input;
using TS.Audio;

namespace TopSpeed.Core
{
    internal sealed class LogoScreen : IDisposable
    {
        private const float FadeDurationSeconds = 0.8f;
        private readonly AudioManager _audio;
        private AudioSourceHandle? _logo;
        private bool _fading;
        private float _fadeProgress;

        public LogoScreen(AudioManager audio)
        {
            _audio = audio;
        }

        public void Start()
        {
            var logoPath = Path.Combine(AssetPaths.SoundsRoot, "Legacy", "pitd_logo.wav");
            if (File.Exists(logoPath))
            {
                _logo = _audio.CreateSource(logoPath, streamFromDisk: true);
                _logo.SetVolume(1.0f);
                _logo.Play(loop: false);
            }
        }

        public bool Update(InputManager input, float deltaSeconds)
        {
            if (_logo == null)
                return true;

            if (!_fading && !_logo.IsPlaying)
                return true;

            if (!_fading && (input.WasPressed(Key.Return) || input.WasPressed(Key.NumberPadEnter)))
            {
                _fading = true;
                _fadeProgress = 0f;
            }

            if (_fading)
            {
                _fadeProgress += Math.Max(0f, deltaSeconds);
                var volume = 1.0f - Math.Min(1.0f, _fadeProgress / FadeDurationSeconds);
                _logo.SetVolume(volume);
                if (_fadeProgress >= FadeDurationSeconds)
                {
                    _logo.Stop();
                    return true;
                }
            }

            return false;
        }

        public void Dispose()
        {
            if (_logo != null)
            {
                _logo.Stop();
                _logo.Dispose();
                _logo = null;
            }
        }
    }
}
