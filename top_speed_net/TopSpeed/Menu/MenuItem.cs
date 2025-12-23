using System;
using TS.Audio;

namespace TopSpeed.Menu
{
    internal sealed class MenuItem
    {
        private readonly string _text;
        private readonly Func<string>? _textProvider;

        public string Text => _text;
        public MenuAction Action { get; }
        public string? NextMenuId { get; }
        public string? SoundFile { get; }
        public AudioSourceHandle? Sound { get; set; }
        public Action? OnActivate { get; }

        public MenuItem(
            string text,
            MenuAction action,
            string? soundFile,
            string? nextMenuId = null,
            Action? onActivate = null)
        {
            _text = text;
            _textProvider = null;
            Action = action;
            SoundFile = soundFile;
            NextMenuId = nextMenuId;
            OnActivate = onActivate;
        }

        public MenuItem(
            Func<string> textProvider,
            MenuAction action,
            string? soundFile,
            string? nextMenuId = null,
            Action? onActivate = null)
        {
            _text = string.Empty;
            _textProvider = textProvider ?? throw new ArgumentNullException(nameof(textProvider));
            Action = action;
            SoundFile = soundFile;
            NextMenuId = nextMenuId;
            OnActivate = onActivate;
        }

        public string GetDisplayText()
        {
            return _textProvider?.Invoke() ?? _text;
        }
    }
}
