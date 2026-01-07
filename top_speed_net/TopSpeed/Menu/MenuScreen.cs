using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
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
        private const string DefaultEdgeSound = "menu_edge.wav";
        private const int JoystickThreshold = 50;
        private const int NoSelection = -1;
        private readonly List<MenuItem> _items;
        private readonly AudioManager _audio;
        private readonly SpeechService _speech;
        private readonly Func<bool> _usageHintsEnabled;
        private readonly string _defaultMenuSoundRoot;
        private readonly string _legacySoundRoot;
        private readonly string _musicRoot;
        private readonly string _title;
        private readonly Func<string>? _titleProvider;
        private bool _initialized;
        private int _index;
        private AudioSourceHandle? _music;
        private float _musicVolume;
        private float _musicCurrentVolume;
        private AudioSourceHandle? _navigateSound;
        private AudioSourceHandle? _wrapSound;
        private AudioSourceHandle? _activateSound;
        private AudioSourceHandle? _edgeSound;
        private JoystickStateSnapshot _prevJoystick;
        private JoystickStateSnapshot _joystickCenter;
        private bool _hasPrevJoystick;
        private bool _hasJoystickCenter;
        private bool _justEntered = true;
        private bool _ignoreHeldInput;
        private bool _autoFocusPending;
        private int _hintToken;
        private bool _disposed;
        private string? _menuSoundPresetRoot;

        private const int MusicFadeStepMs = 50;
        private int _musicFadeToken;

        public string Id { get; }
        public IReadOnlyList<MenuItem> Items => _items;
        public bool WrapNavigation { get; set; } = true;
        public bool MenuNavigatePanning { get; set; }
        public string? MusicFile { get; set; }
        public string? NavigateSoundFile { get; set; } = DefaultNavigateSound;
        public string? WrapSoundFile { get; set; } = DefaultWrapSound;
        public string? ActivateSoundFile { get; set; } = DefaultActivateSound;
        public string? EdgeSoundFile { get; set; } = DefaultEdgeSound;
        public float MusicVolume
        {
            get => _musicVolume;
            set => _musicVolume = Math.Max(0f, Math.Min(1f, value));
        }
        public Action<float>? MusicVolumeChanged { get; set; }
        internal bool HasMusic => !string.IsNullOrWhiteSpace(MusicFile);
        internal bool IsMusicPlaying => _music != null && _music.IsPlaying;
        internal void CancelPendingHint() => CancelHint();

        public MenuScreen(string id, IEnumerable<MenuItem> items, AudioManager audio, SpeechService speech, string? title = null, Func<string>? titleProvider = null, Func<bool>? usageHintsEnabled = null)
        {
            Id = id;
            _audio = audio;
            _speech = speech;
            _usageHintsEnabled = usageHintsEnabled ?? (() => false);
            _items = new List<MenuItem>(items);
            _defaultMenuSoundRoot = Path.Combine(AssetPaths.SoundsRoot, "En", "Menu");
            _legacySoundRoot = Path.Combine(AssetPaths.SoundsRoot, "Legacy");
            _musicRoot = Path.Combine(AssetPaths.SoundsRoot, "En", "Music");
            _musicVolume = 0.0f;
            _title = title ?? string.Empty;
            _titleProvider = titleProvider;
        }

        public string Title => _titleProvider?.Invoke() ?? _title;

        public void Initialize()
        {
            if (_initialized)
                return;

            _navigateSound = LoadDefaultSound(NavigateSoundFile);
            _wrapSound = LoadDefaultSound(WrapSoundFile);
            _activateSound = LoadDefaultSound(ActivateSoundFile);
            _edgeSound = LoadDefaultSound(EdgeSoundFile);

            if (!string.IsNullOrWhiteSpace(MusicFile))
            {
                var themePath = Path.Combine(_musicRoot, MusicFile!);
                if (File.Exists(themePath))
                {
                    _music = _audio.CreateLoopingSource(themePath);
                    ApplyMusicVolume(0f);
                }
            }

            _initialized = true;
        }

        public void SetMenuSoundPreset(string? preset)
        {
            var root = ResolveMenuSoundPresetRoot(preset);
            if (string.Equals(_menuSoundPresetRoot, root, StringComparison.OrdinalIgnoreCase))
                return;
            _menuSoundPresetRoot = root;
            if (_initialized)
                ReloadMenuSounds();
        }

        public MenuUpdateResult Update(InputManager input)
        {
            if (_items.Count == 0)
                return MenuUpdateResult.None;

            var moveUp = input.WasPressed(Key.Up);
            var moveDown = input.WasPressed(Key.Down);
            var moveHome = input.WasPressed(Key.Home);
            var moveEnd = input.WasPressed(Key.End);
            var moveLeft = input.WasPressed(Key.Left);
            var moveRight = input.WasPressed(Key.Right);
            var pageUp = input.WasPressed(Key.PageUp);
            var pageDown = input.WasPressed(Key.PageDown);
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

            if (input.ShouldIgnoreMenuBack())
                return MenuUpdateResult.None;

            if (_ignoreHeldInput)
            {
                if (input.IsMenuBackHeld())
                {
                    input.LatchMenuBack();
                    _ignoreHeldInput = false;
                    _autoFocusPending = false;
                    return MenuUpdateResult.Back;
                }
                if (moveUp)
                {
                    _ignoreHeldInput = false;
                    _autoFocusPending = false;
                    MoveToIndex(_items.Count - 1);
                    return MenuUpdateResult.None;
                }
                if (moveDown)
                {
                    _ignoreHeldInput = false;
                    _autoFocusPending = false;
                    MoveToIndex(0);
                    return MenuUpdateResult.None;
                }
                if (moveHome)
                {
                    _ignoreHeldInput = false;
                    _autoFocusPending = false;
                    MoveToIndex(0);
                    return MenuUpdateResult.None;
                }
                if (moveEnd)
                {
                    _ignoreHeldInput = false;
                    _autoFocusPending = false;
                    MoveToIndex(_items.Count - 1);
                    return MenuUpdateResult.None;
                }
                if (activate || back)
                {
                    _ignoreHeldInput = false;
                }
                else if (input.IsAnyMenuInputHeld())
                {
                    return MenuUpdateResult.None;
                }
                else
                {
                    _ignoreHeldInput = false;
                    input.ResetState();
                }
            }

            if (_index != NoSelection)
            {
                var adjustment = GetAdjustmentAction(moveLeft, moveRight, pageUp, pageDown, moveHome, moveEnd);
                if (adjustment.HasValue)
                {
                    var item = _items[_index];
                    if (item.Adjust(adjustment.Value, out var announcement))
                    {
                        PlayNavigateSound();
                        var safeAnnouncement = announcement;
                        if (!string.IsNullOrWhiteSpace(safeAnnouncement))
                        {
                            _speech.Purge();
                            _speech.Speak(safeAnnouncement!);
                            CancelHint();
                        }
                        return MenuUpdateResult.None;
                    }
                }
            }

            if (_index == NoSelection)
            {
                if (moveDown)
                {
                    MoveToIndex(0);
                    _autoFocusPending = false;
                }
                else if (moveUp)
                {
                    MoveToIndex(_items.Count - 1);
                    _autoFocusPending = false;
                }
                else if (moveHome)
                {
                    MoveToIndex(0);
                    _autoFocusPending = false;
                }
                else if (moveEnd)
                {
                    MoveToIndex(_items.Count - 1);
                    _autoFocusPending = false;
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

            if (pageUp)
            {
                SetMusicVolume(_musicVolume + 0.05f);
            }
            else if (pageDown)
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
            {
                input.LatchMenuBack();
                return MenuUpdateResult.Back;
            }

            if (_index == NoSelection && _autoFocusPending)
            {
                FocusFirstItem();
                _autoFocusPending = false;
            }

            return MenuUpdateResult.None;
        }

        public void ResetSelection()
        {
            _index = NoSelection;
            _justEntered = true;
            _autoFocusPending = true;
            CancelHint();
        }

        public void ReplaceItems(IEnumerable<MenuItem> items)
        {
            _items.Clear();
            _items.AddRange(items);
            _index = NoSelection;
            _justEntered = true;
            _autoFocusPending = true;
            CancelHint();
        }

        private void MoveSelectionAndAnnounce(int delta)
        {
            var moved = MoveSelection(delta, out var wrapped, out var edgeReached);
            if (moved)
            {
                if (wrapped)
                {
                    PlayNavigateSound();
                    PlaySfx(_wrapSound);
                }
                else
                {
                    PlayNavigateSound();
                }
                AnnounceCurrent(!_justEntered);
                _justEntered = false;
            }
            else if (wrapped)
            {
                PlaySfx(_wrapSound);
            }
            else if (edgeReached)
            {
                PlaySfx(_edgeSound);
            }
        }

        private void MoveToIndex(int targetIndex)
        {
            if (targetIndex < 0 || targetIndex >= _items.Count)
                return;
            if (_index == NoSelection)
            {
                _index = targetIndex;
                PlayNavigateSound();
                AnnounceCurrent(!_justEntered);
                _justEntered = false;
                return;
            }
            if (targetIndex == _index)
            {
                PlaySfx(WrapNavigation ? _wrapSound : _edgeSound);
                return;
            }
            _index = targetIndex;
            PlayNavigateSound();
            AnnounceCurrent(!_justEntered);
            _justEntered = false;
        }

        private bool MoveSelection(int delta, out bool wrapped, out bool edgeReached)
        {
            wrapped = false;
            edgeReached = false;
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

            var nextIndex = _index + delta;
            if (nextIndex < 0 || nextIndex >= _items.Count)
            {
                edgeReached = true;
                return false;
            }
            _index = nextIndex;
            return _index != previous;
        }

        public void AnnounceSelection()
        {
            AnnounceCurrent(!_justEntered);
            _justEntered = false;
        }

        private void AnnounceCurrent(bool purge)
        {
            if (_index == NoSelection)
                return;
            var item = _items[_index];
            if (purge)
                _speech.Purge();
            var displayText = item.GetDisplayText();
            _speech.Speak(displayText);
            ScheduleHint(item, _index, displayText);
        }

        public void AnnounceTitle()
        {
            _justEntered = true;
            _ignoreHeldInput = true;
            _speech.Purge();
            CancelHint();
            if (!string.IsNullOrWhiteSpace(Title))
                _speech.Speak(Title, SpeechService.SpeakFlag.Interruptable);

            _index = NoSelection;
            _autoFocusPending = true;
        }

        private void FocusFirstItem()
        {
            if (_items.Count == 0)
                return;
            _index = 0;
            PlayNavigateSound();
            AnnounceCurrent(purge: false);
            _justEntered = false;
        }

        public void FadeOutMusic(int durationMs)
        {
            if (_music == null || !_music.IsPlaying)
                return;

            StartMusicFade(_musicCurrentVolume, 0f, durationMs, stopOnEnd: true);
        }

        public void FadeInMusic(int durationMs)
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
                    ApplyMusicVolume(0f);
                }
            });
        }

        private void ApplyMusicVolume(float volume)
        {
            _musicCurrentVolume = volume;
            _music?.SetVolume(volume);
        }

        private AudioSourceHandle? LoadDefaultSound(string? fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
                return null;
            if (!string.IsNullOrWhiteSpace(_menuSoundPresetRoot))
            {
                var presetPath = Path.Combine(_menuSoundPresetRoot, fileName);
                if (File.Exists(presetPath))
                    return _audio.CreateSource(presetPath, streamFromDisk: true);
            }
            var enRoot = Path.Combine(AssetPaths.SoundsRoot, "En");
            var enPath = Path.Combine(enRoot, fileName);
            if (File.Exists(enPath))
                return _audio.CreateSource(enPath, streamFromDisk: true);
            var legacyPath = Path.Combine(_legacySoundRoot, fileName);
            if (File.Exists(legacyPath))
                return _audio.CreateSource(legacyPath, streamFromDisk: true);
            var menuPath = Path.Combine(_defaultMenuSoundRoot, fileName);
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

        private void PlayNavigateSound()
        {
            if (_navigateSound == null)
                return;
            _navigateSound.Stop();
            _navigateSound.SeekToStart();
            _navigateSound.SetPan(MenuNavigatePanning ? CalculateNavigatePan() : 0f);
            _navigateSound.Play(loop: false);
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
            _navigateSound?.Dispose();
            _wrapSound?.Dispose();
            _activateSound?.Dispose();
            _edgeSound?.Dispose();
            _navigateSound = LoadDefaultSound(NavigateSoundFile);
            _wrapSound = LoadDefaultSound(WrapSoundFile);
            _activateSound = LoadDefaultSound(ActivateSoundFile);
            _edgeSound = LoadDefaultSound(EdgeSoundFile);
        }

        private static string? ResolveMenuSoundPresetRoot(string? preset)
        {
            var trimmed = preset?.Trim();
            if (string.IsNullOrWhiteSpace(trimmed))
                return null;
            return Path.Combine(AssetPaths.SoundsRoot, "menu", trimmed);
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

        private static MenuAdjustAction? GetAdjustmentAction(bool moveLeft, bool moveRight, bool pageUp, bool pageDown, bool moveHome, bool moveEnd)
        {
            if (moveLeft)
                return MenuAdjustAction.Decrease;
            if (moveRight)
                return MenuAdjustAction.Increase;
            if (pageUp)
                return MenuAdjustAction.PageIncrease;
            if (pageDown)
                return MenuAdjustAction.PageDecrease;
            if (moveHome)
                return MenuAdjustAction.ToMaximum;
            if (moveEnd)
                return MenuAdjustAction.ToMinimum;
            return null;
        }

        private void ScheduleHint(MenuItem item, int index, string displayText)
        {
            CancelHint();
            if (!_usageHintsEnabled())
                return;
            if (string.IsNullOrWhiteSpace(item.Hint))
                return;

            var token = Volatile.Read(ref _hintToken);
            var delayMs = CalculateHintDelay(displayText);
            Task.Run(async () =>
            {
                await Task.Delay(delayMs).ConfigureAwait(false);
                if (token != Volatile.Read(ref _hintToken))
                    return;
                if (_disposed || _index != index)
                    return;
                if (!_usageHintsEnabled() || string.IsNullOrWhiteSpace(item.Hint))
                    return;
                _speech.Speak(item.Hint!, SpeechService.SpeakFlag.Interruptable);
            });
        }

        private int CalculateHintDelay(string displayText)
        {
            var words = CountWords(displayText);
            var rateMs = _speech.ScreenReaderRateMs;
            var baseDelay = rateMs > 0f ? words * rateMs : 0f;
            var totalDelay = baseDelay + 1000f;
            return (int)Math.Max(0, Math.Ceiling(totalDelay));
        }

        private static int CountWords(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return 0;
            return text.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).Length;
        }

        private void CancelHint()
        {
            Interlocked.Increment(ref _hintToken);
        }


        public void Dispose()
        {
            _disposed = true;
            CancelHint();
            _navigateSound?.Dispose();
            _wrapSound?.Dispose();
            _activateSound?.Dispose();
            _edgeSound?.Dispose();
            _music?.Dispose();
            _music = null;
            Interlocked.Increment(ref _musicFadeToken);
        }
    }
}
