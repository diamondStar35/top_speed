using System;
using System.Collections.Generic;
using Key = TopSpeed.Input.InputKey;
using TopSpeed.Audio;
using TopSpeed.Input;
using TopSpeed.Shortcuts;
using TopSpeed.Speech;

namespace TopSpeed.Menu
{
    internal sealed partial class MenuManager : IDisposable
    {
        private const int DefaultFadeMs = 1000;
        private readonly Dictionary<string, MenuScreen> _screens;
        private readonly ShortcutCatalog _shortcutCatalog;
        private readonly Stack<MenuScreen> _stack;
        private readonly AudioManager _audio;
        private readonly SpeechService _speech;
        private readonly Func<bool> _usageHintsEnabled;
        private bool _wrapNavigation = true;
        private bool _menuNavigatePanning;
        private bool _menuDigitActivation;
        private bool _menuAutoFocus = true;
        private string? _menuSoundPreset;
        private bool _menuMusicSuspended;

        public MenuManager(AudioManager audio, SpeechService speech, Func<bool>? usageHintsEnabled = null)
        {
            _audio = audio;
            _speech = speech;
            _usageHintsEnabled = usageHintsEnabled ?? (() => false);
            _screens = new Dictionary<string, MenuScreen>(StringComparer.Ordinal);
            _shortcutCatalog = new ShortcutCatalog();
            _stack = new Stack<MenuScreen>();
        }

        public void Register(MenuScreen screen)
        {
            if (!_screens.ContainsKey(screen.Id))
                _screens.Add(screen.Id, screen);
        }

        public void UpdateItems(string id, IEnumerable<MenuItem> items, bool preserveSelection = false)
        {
            var screen = GetScreen(id);
            screen.ReplaceItems(items, preserveSelection);
        }

        public void UpdateItems(string id, string screenId, IEnumerable<MenuItem> items, bool preserveSelection = false)
        {
            var screen = GetScreen(id);
            if (screen.UpdateScreenItems(screenId, items, preserveSelection))
                return;

            throw new InvalidOperationException($"Screen '{screenId}' is not registered for menu '{id}'.");
        }

        public void SetScreens(string id, IEnumerable<MenuView> screens, string? initialScreenId = null)
        {
            var screen = GetScreen(id);
            screen.SetScreens(screens, initialScreenId);
        }

        public void RegisterShortcutAction(
            string actionId,
            string displayName,
            string description,
            Key key,
            ShortcutModifiers modifiers,
            Action onTrigger,
            Func<bool>? canExecute = null,
            GestureIntent? gestureIntent = null)
        {
            if (gestureIntent.HasValue && MenuInputBindings.IsReservedGesture(gestureIntent.Value))
                throw new InvalidOperationException($"Gesture intent '{gestureIntent.Value}' is reserved for core menu actions.");

            _shortcutCatalog.RegisterAction(actionId, displayName, description, key, modifiers, onTrigger, canExecute, gestureIntent);
        }

        public void SetShortcutBinding(string actionId, Key key)
        {
            SetShortcutBinding(actionId, key, ShortcutModifiers.None);
        }

        public void SetShortcutBinding(string actionId, Key key, ShortcutModifiers modifiers)
        {
            _shortcutCatalog.SetBinding(actionId, key, modifiers);
        }

        public void SetGlobalShortcutActions(IEnumerable<string>? actionIds)
        {
            _shortcutCatalog.SetGlobalActions(actionIds);
        }

        public void SetScopeShortcutActions(string scopeId, IEnumerable<string>? actionIds, string? scopeName = null)
        {
            _shortcutCatalog.SetScopeActions(scopeId, actionIds, scopeName);
        }

        public void SetMenuShortcutScopes(string id, IEnumerable<string>? scopeIds)
        {
            var screen = GetScreen(id);
            _shortcutCatalog.SetMenuScopes(screen.Id, scopeIds);
        }

        public void SetMenuShortcutActions(string id, IEnumerable<string>? actionIds, string? menuName = null)
        {
            var screen = GetScreen(id);
            var resolvedMenuName = string.IsNullOrWhiteSpace(menuName) ? screen.Title : menuName;
            _shortcutCatalog.SetMenuActions(screen.Id, actionIds, resolvedMenuName);
        }

        public void SetViewShortcutActions(string viewId, IEnumerable<string>? actionIds, string? viewName = null)
        {
            _shortcutCatalog.SetViewActions(viewId, actionIds, viewName);
        }

        public IReadOnlyList<ShortcutGroup> GetShortcutGroups()
        {
            return _shortcutCatalog.GetGroups();
        }

        public IReadOnlyList<ShortcutBinding> GetShortcutBindings(string groupId)
        {
            return _shortcutCatalog.GetGroupBindings(groupId);
        }

        public bool TryGetShortcutBinding(string actionId, out ShortcutBinding binding)
        {
            return _shortcutCatalog.TryGetBinding(actionId, out binding);
        }

        public bool IsShortcutKeyInUse(string groupId, Key key, string ignoredActionId)
        {
            return IsShortcutBindingInUse(groupId, key, ShortcutModifiers.None, ignoredActionId);
        }

        public bool IsShortcutBindingInUse(string groupId, Key key, ShortcutModifiers modifiers, string ignoredActionId)
        {
            return _shortcutCatalog.IsBindingInUseInGroup(groupId, key, modifiers, ignoredActionId);
        }

        public void ResetShortcutBindings()
        {
            _shortcutCatalog.ResetBindings();
        }

        public bool ResetShortcutBindingsInGroup(string groupId)
        {
            return _shortcutCatalog.ResetBindingsInGroup(groupId);
        }

        public bool TryTriggerShortcutAction(string actionId, IInputService input)
        {
            if (!_shortcutCatalog.TryResolveTriggeredActionById(input, actionId, out var action))
                return false;

            action.Trigger();
            return true;
        }

        public void SetClose(string id, Func<CloseEvent, bool>? onClose)
        {
            var screen = GetScreen(id);
            screen.OnClose = onClose;
        }

        public bool IsWrapNavigationEnabled => _wrapNavigation;

    }
}


