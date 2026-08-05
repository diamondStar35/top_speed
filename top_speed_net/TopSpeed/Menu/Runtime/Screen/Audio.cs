using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using TopSpeed.Audio;
using TopSpeed.Core;
using TS.Audio;

namespace TopSpeed.Menu
{
    internal sealed partial class MenuScreen
    {
        public void FadeOutMusic(int durationMs)
        {
            if (_music == null || !_music.IsPlaying)
                return;

            StartMusicFade(_musicCurrentVolume, 0f, durationMs, stopOnEnd: true);
        }

        public void ApplyExternalMusicVolume(float volume)
        {
            _musicVolume = Math.Max(0f, Math.Min(1f, volume));
            if (_music == null)
                return;

            if (_music.IsPlaying)
                _music.SetVolume(_musicVolume);

            _musicCurrentVolume = _music.IsPlaying ? _musicVolume : 0f;
        }

        public void FadeInMusic(int durationMs)
        {
            if (!HasMusic)
                return;

            if (_musicAsset == null)
            {
                var themePath = ResolveMusicPath();
                if (string.IsNullOrWhiteSpace(themePath))
                    return;
                _musicAsset = _audio.LoadAsset(themePath!, streamFromDisk: false);
            }

            if (_music == null)
            {
                _music = _audio.CreateLoopingSource(_musicAsset, AudioEngineOptions.MusicBusName, useHrtf: false);
                ApplyMusicVolume(0f);
            }

            if (_music.IsPlaying)
            {
                if (_musicCurrentVolume >= _musicVolume)
                {
                    ApplyMusicVolume(_musicVolume);
                    return;
                }
                StartMusicFade(_musicCurrentVolume, _musicVolume, durationMs, stopOnEnd: false);
                return;
            }

            ApplyMusicVolume(0f);
            _music.Play(loop: true);
            StartMusicFade(0f, _musicVolume, durationMs, stopOnEnd: false);
        }

        private void SetMusicVolume(float volume)
        {
            _musicVolume = Math.Max(0f, Math.Min(1f, volume));
            if (_music != null)
            {
                Interlocked.Increment(ref _musicFadeToken);
                ApplyMusicVolume(_musicVolume);
            }
            MusicVolumeChanged?.Invoke(_musicVolume);
        }

        private void StartMusicFade(float startVolume, float targetVolume, int durationMs, bool stopOnEnd)
        {
            if (_music == null)
                return;

            var token = Interlocked.Increment(ref _musicFadeToken);
            ApplyMusicVolume(startVolume);
            var steps = Math.Max(1, durationMs / MusicFadeStepMs);
            var delayMs = Math.Max(1, durationMs / steps);

            Task.Run(async () =>
            {
                for (var i = 1; i <= steps; i++)
                {
                    if (token != Volatile.Read(ref _musicFadeToken))
                        return;

                    var t = i / (float)steps;
                    var volume = startVolume + (targetVolume - startVolume) * t;
                    ApplyMusicVolume(volume);
                    await Task.Delay(delayMs).ConfigureAwait(false);
                }

                if (token != Volatile.Read(ref _musicFadeToken))
                    return;

                if (stopOnEnd)
                {
                    _music?.Stop();
                    _music?.Dispose();
                    _music = null;
                    ApplyMusicVolume(0f);
                }
            });
        }

        private void ApplyMusicVolume(float volume)
        {
            _musicCurrentVolume = volume;
            _music?.SetVolume(volume);
        }

        private SoundAsset? LoadDefaultSound(string? fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
                return null;
            if (_menuSoundsSilenced)
                return null;
            var resolvedPath = ResolveMenuSoundPath(fileName);
            if (string.IsNullOrWhiteSpace(resolvedPath))
                return null;
            return _audio.LoadAsset(resolvedPath!, streamFromDisk: false);
        }

        private void PlaySfx(SoundAsset? sound)
        {
            if (sound == null)
                return;
            _audio.PlayOneShot(sound, AudioEngineOptions.UiBusName, useHrtf: false);
        }

        private void PlayNavigateSound()
        {
            if (_navigateSound == null)
                return;
            _audio.PlayOneShot(
                _navigateSound,
                AudioEngineOptions.UiBusName,
                configure: handle => handle.SetPan(MenuNavigatePanning ? CalculateNavigatePan() : 0f),
                useHrtf: false);
        }

        internal void PlayMenuCue(MenuFeedbackCue cue)
        {
            switch (cue)
            {
                case MenuFeedbackCue.Navigate:
                    PlayNavigateSound();
                    break;
                case MenuFeedbackCue.Wrap:
                    PlaySfx(_wrapSound);
                    break;
                case MenuFeedbackCue.Edge:
                    PlaySfx(_edgeSound);
                    break;
            }
        }

        private float CalculateNavigatePan()
        {
            if (_index < 0)
                return 0f;
            var count = _items.Count;
            if (count <= 1)
                return 0f;
            return -1f + (2f * _index / (count - 1f));
        }

        private void ReloadMenuSounds()
        {
            ReleaseMenuSound(ref _navigateSound);
            ReleaseMenuSound(ref _wrapSound);
            ReleaseMenuSound(ref _activateSound);
            ReleaseMenuSound(ref _edgeSound);
            _navigateSound = LoadDefaultSound(NavigateSoundFile);
            _wrapSound = LoadDefaultSound(WrapSoundFile);
            _activateSound = LoadDefaultSound(ActivateSoundFile);
            _edgeSound = LoadDefaultSound(EdgeSoundFile);
        }

        private string? ResolveMenuSoundPath(string? fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
                return null;
            var key = fileName!;
            if (_menuSoundPathCache.TryGetValue(key, out var cached))
                return cached == MissingPathSentinel ? null : cached;

            string? resolved = null;
            if (!string.IsNullOrWhiteSpace(_menuSoundPresetRoot))
            {
                var presetPath = Path.Combine(_menuSoundPresetRoot, fileName);
                if (_audio.TryResolvePath(presetPath, out var fullPath))
                    resolved = fullPath;
            }

            if (resolved == null)
            {
                var enPath = Path.Combine(AssetPaths.SoundsRoot, "En", fileName);
                if (_audio.TryResolvePath(enPath, out var fullPath))
                    resolved = fullPath;
            }

            if (resolved == null)
            {
                var legacyPath = Path.Combine(_legacySoundRoot, fileName);
                if (_audio.TryResolvePath(legacyPath, out var fullPath))
                    resolved = fullPath;
            }

            if (resolved == null)
            {
                var menuPath = Path.Combine(_defaultMenuSoundRoot, fileName);
                if (_audio.TryResolvePath(menuPath, out var fullPath))
                    resolved = fullPath;
            }

            _menuSoundPathCache[key] = resolved ?? MissingPathSentinel;
            return resolved;
        }

        private string? ResolveMusicPath()
        {
            var musicFile = MusicFile;
            if (string.IsNullOrWhiteSpace(musicFile))
                return null;

            if (string.Equals(_cachedMusicFile, musicFile, StringComparison.OrdinalIgnoreCase))
                return _cachedMusicPath == MissingPathSentinel ? null : _cachedMusicPath;

            _cachedMusicFile = musicFile;
            _cachedMusicPath = MissingPathSentinel;
            var themePath = Path.Combine(_musicRoot, musicFile);
            if (_audio.TryResolvePath(themePath, out var fullPath))
                _cachedMusicPath = fullPath;

            return _cachedMusicPath == MissingPathSentinel ? null : _cachedMusicPath;
        }

        private static string? ResolveMenuSoundPresetRoot(string? preset)
        {
            var trimmed = preset?.Trim();
            if (string.IsNullOrWhiteSpace(trimmed))
                return null;
            return Path.Combine(AssetPaths.SoundsRoot, "menu", trimmed);
        }

        private static void ReleaseMenuSound(ref SoundAsset? sound)
        {
            sound = null;
        }

        public void Dispose()
        {
            _disposed = true;
            CancelHint();
            ReleaseMenuSound(ref _navigateSound);
            ReleaseMenuSound(ref _wrapSound);
            ReleaseMenuSound(ref _activateSound);
            ReleaseMenuSound(ref _edgeSound);
            _musicAsset = null;
            _music?.Stop();
            _music?.Dispose();
            _music = null;
            _menuSoundPathCache.Clear();
            Interlocked.Increment(ref _musicFadeToken);
        }
    }
}

