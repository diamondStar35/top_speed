using System;
using System.Collections.Generic;
using TopSpeed.Audio;
using TopSpeed.Input;
using TopSpeed.Speech;

namespace TopSpeed.Menu
{
    internal sealed class MenuManager : IDisposable
    {
        private const int DefaultFadeMs = 1000;
        private readonly Dictionary<string, MenuScreen> _screens;
        private readonly Stack<MenuScreen> _stack;
        private readonly AudioManager _audio;
        private readonly SpeechService _speech;
        private bool _menuMusicSuspended;

        public MenuManager(AudioManager audio, SpeechService speech)
        {
            _audio = audio;
            _speech = speech;
            _screens = new Dictionary<string, MenuScreen>(StringComparer.Ordinal);
            _stack = new Stack<MenuScreen>();
        }

        public void Register(MenuScreen screen)
        {
            if (!_screens.ContainsKey(screen.Id))
                _screens.Add(screen.Id, screen);
        }

        public void UpdateItems(string id, IEnumerable<MenuItem> items)
        {
            var screen = GetScreen(id);
            screen.ReplaceItems(items);
        }

        public void ShowRoot(string id)
        {
            _stack.Clear();
            var screen = GetScreen(id);
            screen.ResetSelection();
            screen.Initialize();
            _stack.Push(screen);
            screen.AnnounceTitle();
        }

        public void Push(string id)
        {
            var screen = GetScreen(id);
            screen.ResetSelection();
            screen.Initialize();
            _stack.Push(screen);
            screen.AnnounceTitle();
        }

        public void ReplaceTop(string id)
        {
            if (_stack.Count == 0)
            {
                ShowRoot(id);
                return;
            }

            _stack.Pop();
            var screen = GetScreen(id);
            screen.ResetSelection();
            screen.Initialize();
            _stack.Push(screen);
            screen.AnnounceTitle();
        }

        public void PopToPrevious()
        {
            if (_stack.Count <= 1)
                return;

            _stack.Pop();
            _stack.Peek().AnnounceTitle();
        }

        public bool HasActiveMenu => _stack.Count > 0;
        public bool CanPop => _stack.Count > 1;
        public string? CurrentId => _stack.Count > 0 ? _stack.Peek().Id : null;

        public MenuAction Update(InputManager input)
        {
            if (_stack.Count == 0)
                return MenuAction.None;

            var current = _stack.Peek();
            var result = current.Update(input);

            if (result.BackRequested)
            {
                if (_stack.Count > 1)
                {
                    _stack.Pop();
                    _stack.Peek().AnnounceTitle();
                    return MenuAction.None;
                }

                return MenuAction.Exit;
            }

            if (result.ActivatedItem != null)
            {
                var item = result.ActivatedItem;
                var stackCount = _stack.Count;
                _speech.Purge();
                var announcement = item.ActivateAndGetAnnouncement();
                var stackChanged = _stack.Count != stackCount || _stack.Peek() != current;
                if (item.Action == MenuAction.Back)
                {
                    if (_stack.Count > 1)
                    {
                        _stack.Pop();
                        _stack.Peek().AnnounceTitle();
                        return MenuAction.None;
                    }
                    return MenuAction.Exit;
                }
                if (!string.IsNullOrWhiteSpace(item.NextMenuId))
                {
                    Push(item.NextMenuId!);
                    return MenuAction.None;
                }
                if (!stackChanged && !item.SuppressPostActivateAnnouncement && !string.IsNullOrWhiteSpace(announcement))
                {
                    _speech.Speak(announcement);
                }
                return item.Action;
            }

            return MenuAction.None;
        }

        private MenuScreen GetScreen(string id)
        {
            if (!_screens.TryGetValue(id, out var screen))
                throw new InvalidOperationException($"Menu not registered: {id}");
            return screen;
        }

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

        public MenuScreen CreateMenu(string id, IEnumerable<MenuItem> items, string? title = null, Func<string>? titleProvider = null)
        {
            return new MenuScreen(id, items, _audio, _speech, title, titleProvider);
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
