using System;
using System.Collections.Generic;
using System.IO;
using TopSpeed.Audio;
using TopSpeed.Core;
using TopSpeed.Input;
using TopSpeed.Localization;
using TopSpeed.Speech;
using TS.Audio;
using TopSpeed.Input.Devices.Controller;

namespace TopSpeed.Menu
{
    internal sealed partial class MenuScreen : IDisposable
    {
        private const string DefaultNavigateSound = "menu_navigate.wav";
        private const string DefaultWrapSound = "menu_wrap.wav";
        private const string DefaultActivateSound = "menu_enter.wav";
        private const string DefaultEdgeSound = "menu_edge.wav";
        private const string MissingPathSentinel = "\0";
        private const int NoSelection = -1;

        private readonly List<MenuItem> _items;
        private readonly List<MenuView> _views;
        private readonly string _defaultViewId;
        private readonly AudioManager _audio;
        private readonly SpeechService _speech;
        private readonly Func<bool> _usageHintsEnabled;
        private readonly Func<bool> _autoFocusFirstItemEnabled;
        private readonly string _defaultMenuSoundRoot;
        private readonly string _legacySoundRoot;
        private readonly string _musicRoot;

        private bool _initialized;
        private int _index;
        private int _viewIndex;
        private Source? _music;
        private SoundAsset? _musicAsset;
        private float _musicVolume;
        private float _musicCurrentVolume;
        private SoundAsset? _navigateSound;
        private SoundAsset? _wrapSound;
        private SoundAsset? _activateSound;
        private SoundAsset? _edgeSound;
        private State _prevController;
        private State _controllerCenter;
        private bool _hasPrevController;
        private bool _hasControllerCenter;
        private bool _justEntered = true;
        private bool _ignoreHeldInput;
        private bool _autoFocusPending;
        private bool _suppressAutoFocus;
        private bool _waitForTitleSpeechBeforeAutoFocus;
        private int _hintToken;
        private bool _disposed;
        private string? _menuSoundPresetRoot;
        private bool _menuSoundsSilenced;
        private bool _titlePending;
        private int _activeActionIndex = NoSelection;
        private string? _openingAnnouncementOverride;
        private int? _pendingFocusIndex;
        private readonly Dictionary<string, string> _menuSoundPathCache =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        private string? _cachedMusicFile;
        private string? _cachedMusicPath;

        private const int MusicFadeStepMs = 50;
        private int _musicFadeToken;

        public string Id { get; }
        public IReadOnlyList<MenuItem> Items => _items;
        public bool WrapNavigation { get; set; } = true;
        public bool MenuNavigatePanning { get; set; }
        public bool NumberActivationEnabled { get; set; }
        public bool ForceNumberActivation { get; set; }
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
        public Func<CloseEvent, bool>? OnClose { get; set; }

        public int ScreenCount => _views.Count;
        internal bool HasMusic => !string.IsNullOrWhiteSpace(MusicFile);
        internal bool IsMusicPlaying => _music != null && _music.IsPlaying;
        internal void CancelPendingHint() => CancelHint();
        internal bool TryHandleClose(in CloseEvent e)
        {
            if (ActiveView.Spec.OnClose?.Invoke(e) == true)
                return true;

            return OnClose?.Invoke(e) == true;
        }
        internal string ActiveViewId => ActiveView.Id;

        public MenuScreen(
            string id,
            IEnumerable<MenuItem> items,
            AudioManager audio,
            SpeechService speech,
            string? title = null,
            Func<string>? titleProvider = null,
            Func<bool>? usageHintsEnabled = null,
            Func<bool>? autoFocusFirstItemEnabled = null,
            ScreenSpec? spec = null)
        {
            Id = id;
            _audio = audio;
            _speech = speech;
            _usageHintsEnabled = usageHintsEnabled ?? (() => false);
            _autoFocusFirstItemEnabled = autoFocusFirstItemEnabled ?? (() => true);
            _items = new List<MenuItem>();
            _views = new List<MenuView>();
            _defaultViewId = $"{id}:main";
            var defaultView = new MenuView(_defaultViewId, items, title, titleProvider, spec);
            _views.Add(defaultView);
            LoadActiveViewItems();
            _defaultMenuSoundRoot = Path.Combine(AssetPaths.SoundsRoot, "En", "Menu");
            _legacySoundRoot = Path.Combine(AssetPaths.SoundsRoot, "Legacy");
            _musicRoot = Path.Combine(AssetPaths.SoundsRoot, "En", "Music");
            _musicVolume = 0.0f;
        }

        public string Title => ActiveView.DisplayTitle;

        public void SetScreens(IEnumerable<MenuView>? screens, string? initialScreenId = null)
        {
            var fallbackItems = _views.Count > 0
                ? _views[0].Items
                : _items;
            var fallbackTitle = _views.Count > 0 ? _views[0].Title : string.Empty;
            var fallbackTitleProvider = _views.Count > 0 ? _views[0].TitleProvider : null;
            var fallbackSpec = _views.Count > 0 ? _views[0].Spec : ScreenSpec.None;
            _views.Clear();
            if (screens != null)
            {
                foreach (var screen in screens)
                {
                    if (screen == null)
                        continue;
                    if (_views.Exists(existing => string.Equals(existing.Id, screen.Id, StringComparison.Ordinal)))
                        continue;
                    _views.Add(screen);
                }
            }

            if (_views.Count == 0)
                _views.Add(new MenuView(_defaultViewId, fallbackItems, fallbackTitle, fallbackTitleProvider, fallbackSpec));

            _viewIndex = ResolveScreenIndex(initialScreenId);
            LoadActiveViewItems();
            ResetSelection();
        }

        public bool UpdateScreenItems(string screenId, IEnumerable<MenuItem> items, bool preserveSelection = false)
        {
            if (string.IsNullOrWhiteSpace(screenId))
                return false;

            var index = FindScreenIndex(screenId);
            if (index < 0)
                return false;

            _views[index].ReplaceItems(items);
            if (index == _viewIndex)
                RefreshActiveViewItems(preserveSelection);

            return true;
        }

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
                var themePath = ResolveMusicPath();
                if (!string.IsNullOrWhiteSpace(themePath))
                {
                    _musicAsset = _audio.LoadAsset(themePath!, streamFromDisk: false);
                }
            }

            _initialized = true;
        }

        public void SetMenuSoundPreset(string? preset)
        {
            var silenced = MenuSoundPresets.IsOff(preset);
            var root = silenced ? null : ResolveMenuSoundPresetRoot(preset);
            if (silenced == _menuSoundsSilenced
                && string.Equals(_menuSoundPresetRoot, root, StringComparison.OrdinalIgnoreCase))
                return;
            _menuSoundsSilenced = silenced;
            _menuSoundPresetRoot = root;
            _menuSoundPathCache.Clear();
            if (_initialized)
                ReloadMenuSounds();
        }

        private MenuView ActiveView => _views[_viewIndex];
        private MenuView PrimaryView => _views[0];

        private void QueueAutoFocusFirstItem(bool force = false)
        {
            _autoFocusPending = force || _autoFocusFirstItemEnabled();
            if (!_autoFocusPending)
                _pendingFocusIndex = null;
        }

        private void ClearAutoFocusPending()
        {
            _autoFocusPending = false;
            _waitForTitleSpeechBeforeAutoFocus = false;
        }

        private int ResolveScreenIndex(string? screenId)
        {
            if (string.IsNullOrWhiteSpace(screenId))
                return 0;

            var index = FindScreenIndex(screenId!);
            return index >= 0 ? index : 0;
        }

        private int FindScreenIndex(string screenId)
        {
            for (var i = 0; i < _views.Count; i++)
            {
                if (string.Equals(_views[i].Id, screenId, StringComparison.Ordinal))
                    return i;
            }

            return -1;
        }

        private void LoadActiveViewItems()
        {
            _items.Clear();
            var source = ActiveView.Items;
            for (var i = 0; i < source.Count; i++)
            {
                var item = source[i];
                if (item == null || item.IsHidden)
                    continue;
                _items.Add(item);
            }

            var flags = ActiveView.Spec.Flags;
            if ((flags & ScreenFlags.Back) != 0)
                _items.Add(new MenuItem(LocalizationService.Mark("Go back"), MenuAction.Back));

            if ((flags & ScreenFlags.Close) != 0)
            {
                var closeText = string.IsNullOrWhiteSpace(ActiveView.Spec.CloseText)
                    ? LocalizationService.Mark("Close")
                    : ActiveView.Spec.CloseText!;
                _items.Add(new MenuItem(closeText, MenuAction.None, flags: MenuItemFlags.Close));
            }
        }
    }
}

