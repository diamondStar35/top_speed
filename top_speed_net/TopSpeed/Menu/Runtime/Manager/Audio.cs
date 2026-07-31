using System;
using System.Collections.Generic;

namespace TopSpeed.Menu
{
    internal sealed partial class MenuManager
    {
        public void Dispose()
        {
            foreach (var screen in _screens.Values)
                screen.Dispose();
            _stack.Clear();
        }

        public void FadeOutMenuMusic(int durationMs = DefaultFadeMs)
        {
            var screen = FindScreenWithPlayingMusic();
            if (screen == null)
                return;

            screen.FadeOutMusic(durationMs);
            _menuMusicSuspended = true;
        }

        public void FadeInMenuMusic(int durationMs = DefaultFadeMs, bool force = false)
        {
            if (!_menuMusicSuspended && !force)
                return;

            var screen = FindScreenWithMusic();
            if (screen == null)
                return;

            screen.FadeInMusic(durationMs);
            _menuMusicSuspended = false;
        }

        public void SetWrapNavigation(bool enabled)
        {
            _wrapNavigation = enabled;
            foreach (var screen in _screens.Values)
                screen.WrapNavigation = enabled;
        }

        public void SetMenuSoundPreset(string? preset)
        {
            _menuSoundPreset = preset;
            foreach (var screen in _screens.Values)
                screen.SetMenuSoundPreset(preset);
        }

        public void SetMenuNavigatePanning(bool enabled)
        {
            _menuNavigatePanning = enabled;
            foreach (var screen in _screens.Values)
                screen.MenuNavigatePanning = enabled;
        }

        public void SetMenuDigitActivation(bool enabled)
        {
            _menuDigitActivation = enabled;
            foreach (var screen in _screens.Values)
                screen.NumberActivationEnabled = enabled;
        }

        public void SetForceNumberActivation(string menuId, bool forced)
        {
            if (string.IsNullOrWhiteSpace(menuId))
                return;
            if (_screens.TryGetValue(menuId, out var screen))
                screen.ForceNumberActivation = forced;
        }

        public void SetMenuAutoFocus(bool enabled)
        {
            _menuAutoFocus = enabled;
        }

        public void SetMenuMusicVolume(float volume)
        {
            foreach (var screen in _screens.Values)
                screen.ApplyExternalMusicVolume(volume);
        }

        public MenuScreen CreateMenu(string id, IEnumerable<MenuItem> items, string? title = null, Func<string>? titleProvider = null, ScreenSpec? spec = null)
        {
            var screen = new MenuScreen(id, items, _audio, _speech, title, titleProvider, _usageHintsEnabled, () => _menuAutoFocus, spec)
            {
                WrapNavigation = _wrapNavigation,
                MenuNavigatePanning = _menuNavigatePanning,
                NumberActivationEnabled = _menuDigitActivation
            };
            screen.SetMenuSoundPreset(_menuSoundPreset);
            return screen;
        }

        private MenuScreen? FindScreenWithPlayingMusic()
        {
            foreach (var screen in _stack)
            {
                if (screen.IsMusicPlaying)
                    return screen;
            }

            return null;
        }

        private MenuScreen? FindScreenWithMusic()
        {
            foreach (var screen in _stack)
            {
                if (screen.HasMusic)
                    return screen;
            }

            return null;
        }
    }
}

