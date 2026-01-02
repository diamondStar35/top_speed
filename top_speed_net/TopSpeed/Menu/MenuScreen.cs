using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using SharpDX.DirectInput;
using TopSpeed.Audio;
using TopSpeed.Core;
using TopSpeed.Input;
using TopSpeed.Speech;
using TS.Audio;

namespace TopSpeed.Menu
{
    internal sealed class MenuScreen : IDisposable
    {
        private const string DefaultNavigateSound = "menu_navigate.wav";
        private const string DefaultWrapSound = "menu_wrap.wav";
        private const string DefaultActivateSound = "menu_enter.wav";
        private const int JoystickThreshold = 50;
        private const int NoSelection = -1;
        private readonly List<MenuItem> _items;
        private readonly AudioManager _audio;
        private readonly SpeechService _speech;
        private readonly string _menuSoundRoot;
        private readonly string _legacySoundRoot;
        private readonly string _musicRoot;
        private bool _initialized;
        private int _index;
        private AudioSourceHandle? _music;
        private float _musicVolume;
        private AudioSourceHandle? _navigateSound;
        private AudioSourceHandle? _wrapSound;
        private AudioSourceHandle? _activateSound;
        private JoystickStateSnapshot _prevJoystick;
        private JoystickStateSnapshot _joystickCenter;
        private bool _hasPrevJoystick;
        private bool _hasJoystickCenter;

        public string Id { get; }
        public IReadOnlyList<MenuItem> Items => _items;
        public bool WrapNavigation { get; set; } = true;
        public string? MusicFile { get; set; }
        public string? NavigateSoundFile { get; set; } = DefaultNavigateSound;
        public string? WrapSoundFile { get; set; } = DefaultWrapSound;
        public string? ActivateSoundFile { get; set; } = DefaultActivateSound;
        public float MusicVolume
        {
            get => _musicVolume;
            set => _musicVolume = Math.Max(0f, Math.Min(1f, value));
        }
        public Action<float>? MusicVolumeChanged { get; set; }
        internal bool HasMusic => !string.IsNullOrWhiteSpace(MusicFile);
        internal bool IsMusicPlaying => _music != null && _music.IsPlaying;

        public MenuScreen(string id, IEnumerable<MenuItem> items, AudioManager audio, SpeechService speech, string? title = null)
        {
            Id = id;
            _audio = audio;
            _speech = speech;
            _items = new List<MenuItem>(items);
            _menuSoundRoot = Path.Combine(AssetPaths.SoundsRoot, "En", "Menu");
            _legacySoundRoot = Path.Combine(AssetPaths.SoundsRoot, "Legacy");
            _musicRoot = Path.Combine(AssetPaths.SoundsRoot, "En", "Music");
            _musicVolume = 0.0f;
            Title = title ?? id;
        }

        public string Title { get; }

        public void Initialize()
        {
            if (_initialized)
                return;

            _navigateSound = LoadDefaultSound(NavigateSoundFile);
            _wrapSound = LoadDefaultSound(WrapSoundFile);
            _activateSound = LoadDefaultSound(ActivateSoundFile);

            if (!string.IsNullOrWhiteSpace(MusicFile))
            {
                var themePath = Path.Combine(_musicRoot, MusicFile!);
                if (File.Exists(themePath))
                {
                    _music = _audio.CreateLoopingSource(themePath);
                    _music.SetVolume(_musicVolume);
                    _music.Play(loop: true);
                }
            }

            _initialized = true;
        }

        public MenuUpdateResult Update(InputManager input)
        {
            if (_items.Count == 0)
                return MenuUpdateResult.None;

            var moveUp = input.WasPressed(Key.Up);
            var moveDown = input.WasPressed(Key.Down);
            var moveHome = input.WasPressed(Key.Home);
            var moveEnd = input.WasPressed(Key.End);
            var activate = input.WasPressed(Key.Return) || input.WasPressed(Key.NumberPadEnter);
            var back = input.WasPressed(Key.Escape);

            if (input.TryGetJoystickState(out var joystick))
            {
                if (!_hasJoystickCenter && IsNearCenter(joystick))
                {
                    _joystickCenter = joystick;
                    _hasJoystickCenter = true;
                }

                var previous = _hasPrevJoystick ? _prevJoystick : _joystickCenter;
                moveUp |= WasJoystickUpPressed(joystick, previous);
                moveDown |= WasJoystickDownPressed(joystick, previous);
                activate |= WasJoystickActivatePressed(joystick, previous);
                back |= WasJoystickBackPressed(joystick, previous);
                _prevJoystick = joystick;
                _hasPrevJoystick = true;
            }
            else
            {
                _hasPrevJoystick = false;
            }

            if (_index == NoSelection)
            {
                if (moveDown)
                {
                    MoveToIndex(0);
                }
                else if (moveUp)
                {
                    MoveToIndex(_items.Count - 1);
                }
                else if (moveHome)
                {
                    MoveToIndex(0);
                }
                else if (moveEnd)
                {
                    MoveToIndex(_items.Count - 1);
                }
            }
            else
            {
                if (moveUp)
                {
                    MoveSelectionAndAnnounce(-1);
                }
                else if (moveDown)
                {
                    MoveSelectionAndAnnounce(1);
                }
                else if (moveHome)
                {
                    MoveToIndex(0);
                }
                else if (moveEnd)
                {
                    MoveToIndex(_items.Count - 1);
                }
            }

            if (input.WasPressed(Key.PageUp))
            {
                SetMusicVolume(_musicVolume + 0.05f);
            }
            else if (input.WasPressed(Key.PageDown))
            {
                SetMusicVolume(_musicVolume - 0.05f);
            }

            if (activate)
            {
                if (_index == NoSelection)
                    return MenuUpdateResult.None;
                PlaySfx(_activateSound);
                return MenuUpdateResult.Activated(_items[_index]);
            }

            if (back)
                return MenuUpdateResult.Back;

            return MenuUpdateResult.None;
        }

        public void ResetSelection()
        {
            _index = NoSelection;
        }

        public void ReplaceItems(IEnumerable<MenuItem> items)
        {
            _items.Clear();
            _items.AddRange(items);
            _index = NoSelection;
        }

        private void MoveSelectionAndAnnounce(int delta)
        {
            var moved = MoveSelection(delta, out var wrapped);
            if (moved)
            {
                if (wrapped)
                {
                    PlaySfx(_navigateSound);
                    PlaySfx(_wrapSound);
                }
                else
                {
                    PlaySfx(_navigateSound);
                }
                AnnounceCurrent();
            }
            else if (wrapped)
            {
                PlaySfx(_wrapSound);
            }
        }

        private void MoveToIndex(int targetIndex)
        {
            if (targetIndex < 0 || targetIndex >= _items.Count)
                return;
            if (_index == NoSelection)
            {
                _index = targetIndex;
                PlaySfx(_navigateSound);
                AnnounceCurrent();
                return;
            }
            if (targetIndex == _index)
            {
                PlaySfx(_wrapSound);
                return;
            }
            _index = targetIndex;
            PlaySfx(_navigateSound);
            AnnounceCurrent();
        }

        private bool MoveSelection(int delta, out bool wrapped)
        {
            wrapped = false;
            if (_items.Count == 0)
                return false;
            if (_index == NoSelection)
            {
                _index = delta >= 0 ? 0 : _items.Count - 1;
                return true;
            }
            var previous = _index;
            if (WrapNavigation)
            {
                var next = _index + delta;
                if (next < 0 || next >= _items.Count)
                    wrapped = true;
                _index = (next + _items.Count) % _items.Count;
                return _index != previous;
            }

            _index = Math.Max(0, Math.Min(_items.Count - 1, _index + delta));
            if (_index == previous)
                wrapped = true;
            return _index != previous;
        }

        public void AnnounceSelection()
        {
            AnnounceCurrent();
        }

        private void AnnounceCurrent()
        {
            if (_index == NoSelection)
                return;
            var item = _items[_index];
            _speech.Speak(item.GetDisplayText(), interrupt: true);
        }

        public void AnnounceTitle()
        {
            if (string.IsNullOrWhiteSpace(Title))
                return;

            _speech.Speak(Title, interrupt: true);
        }

        public void FadeOutMusic()
        {
            if (_music == null || !_music.IsPlaying)
                return;

            var current = _musicVolume;
            for (var i = 0; i < 10; i++)
            {
                current -= _musicVolume / 10f;
                if (current < 0) current = 0;
                _music.SetVolume(current);
                Thread.Sleep(50);
            }
            _music.Stop();
        }

        public void FadeInMusic()
        {
            if (!HasMusic)
                return;

            if (_music == null)
            {
                var themePath = Path.Combine(_musicRoot, MusicFile!);
                if (!File.Exists(themePath))
                    return;
                _music = _audio.CreateLoopingSource(themePath);
            }

            if (_music.IsPlaying)
            {
                _music.SetVolume(_musicVolume);
                return;
            }

            var targetVolume = _musicVolume;
            _music.SetVolume(0f);
            _music.Play(loop: true);

            const int steps = 10;
            for (var i = 0; i < steps; i++)
            {
                var volume = targetVolume * ((i + 1) / (float)steps);
                _music.SetVolume(volume);
                Thread.Sleep(50);
            }
        }

        private void SetMusicVolume(float volume)
        {
            _musicVolume = Math.Max(0f, Math.Min(1f, volume));
            if (_music != null)
                _music.SetVolume(_musicVolume);
            MusicVolumeChanged?.Invoke(_musicVolume);
        }

        private AudioSourceHandle? LoadDefaultSound(string? fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
                return null;
            var enRoot = Path.Combine(AssetPaths.SoundsRoot, "En");
            var enPath = Path.Combine(enRoot, fileName);
            if (File.Exists(enPath))
                return _audio.CreateSource(enPath, streamFromDisk: true);
            var legacyPath = Path.Combine(_legacySoundRoot, fileName);
            if (File.Exists(legacyPath))
                return _audio.CreateSource(legacyPath, streamFromDisk: true);
            var menuPath = Path.Combine(_menuSoundRoot, fileName);
            if (File.Exists(menuPath))
                return _audio.CreateSource(menuPath, streamFromDisk: true);     
            return null;
        }

        private static void PlaySfx(AudioSourceHandle? sound)
        {
            if (sound == null)
                return;
            sound.Stop();
            sound.SeekToStart();
            sound.Play(loop: false);
        }

        private static bool IsNearCenter(JoystickStateSnapshot state)
        {
            return Math.Abs(state.X) <= JoystickThreshold && Math.Abs(state.Y) <= JoystickThreshold;
        }

        private static bool WasJoystickUpPressed(JoystickStateSnapshot current, JoystickStateSnapshot previous)
        {
            var currentUp = current.Y < -JoystickThreshold || current.Pov1;
            var previousUp = previous.Y < -JoystickThreshold || previous.Pov1;
            return currentUp && !previousUp;
        }

        private static bool WasJoystickDownPressed(JoystickStateSnapshot current, JoystickStateSnapshot previous)
        {
            var currentDown = current.Y > JoystickThreshold || current.Pov3;
            var previousDown = previous.Y > JoystickThreshold || previous.Pov3;
            return currentDown && !previousDown;
        }

        private static bool WasJoystickActivatePressed(JoystickStateSnapshot current, JoystickStateSnapshot previous)
        {
            var currentRight = current.X > JoystickThreshold || current.Pov2;
            var previousRight = previous.X > JoystickThreshold || previous.Pov2;
            if (currentRight && !previousRight)
                return true;
            return current.B1 && !previous.B1;
        }

        private static bool WasJoystickBackPressed(JoystickStateSnapshot current, JoystickStateSnapshot previous)
        {
            var currentLeft = current.X < -JoystickThreshold || current.Pov4;
            var previousLeft = previous.X < -JoystickThreshold || previous.Pov4;
            return currentLeft && !previousLeft;
        }

        public void Dispose()
        {
            _navigateSound?.Dispose();
            _wrapSound?.Dispose();
            _activateSound?.Dispose();
            _music?.Dispose();
        }
    }
}
