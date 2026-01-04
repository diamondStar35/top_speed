using System;

namespace TopSpeed.Menu
{
    internal class MenuItem
    {
        private readonly string _text;
        private readonly Func<string>? _textProvider;

        public string Text => _text;
        public MenuAction Action { get; }
        public string? NextMenuId { get; }
        public Action? OnActivate { get; }
        public bool SuppressPostActivateAnnouncement { get; }

        public MenuItem(
            string text,
            MenuAction action,
            string? nextMenuId = null,
            Action? onActivate = null,
            bool suppressPostActivateAnnouncement = false)
        {
            _text = text;
            _textProvider = null;
            Action = action;
            NextMenuId = nextMenuId;
            OnActivate = onActivate;
            SuppressPostActivateAnnouncement = suppressPostActivateAnnouncement;
        }

        public MenuItem(
            Func<string> textProvider,
            MenuAction action,
            string? nextMenuId = null,
            Action? onActivate = null,
            bool suppressPostActivateAnnouncement = false)
        {
            _text = string.Empty;
            _textProvider = textProvider ?? throw new ArgumentNullException(nameof(textProvider));
            Action = action;
            NextMenuId = nextMenuId;
            OnActivate = onActivate;
            SuppressPostActivateAnnouncement = suppressPostActivateAnnouncement;
        }

        public virtual string GetDisplayText()
        {
            return _textProvider?.Invoke() ?? _text;
        }

        public virtual string? ActivateAndGetAnnouncement()
        {
            OnActivate?.Invoke();
            return null;
        }

        protected string GetBaseText()
        {
            return _textProvider?.Invoke() ?? _text;
        }
    }
}
