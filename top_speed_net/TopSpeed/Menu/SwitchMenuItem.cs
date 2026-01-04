using System;

namespace TopSpeed.Menu
{
    internal sealed class SwitchMenuItem : MenuItem
    {
        private readonly Func<bool> _getValue;
        private readonly Action<bool> _setValue;
        private readonly Action<bool>? _onChanged;
        private readonly string _valueOn;
        private readonly string _valueOff;

        public SwitchMenuItem(
            string text,
            string valueOn,
            string valueOff,
            Func<bool> getValue,
            Action<bool> setValue,
            Action<bool>? onChanged = null,
            MenuAction action = MenuAction.None,
            string? nextMenuId = null,
            Action? onActivate = null,
            bool suppressPostActivateAnnouncement = false)
            : base(text, action, nextMenuId, onActivate, suppressPostActivateAnnouncement)
        {
            if (string.IsNullOrWhiteSpace(valueOn))
                throw new ArgumentException("valueOn must be provided.", nameof(valueOn));
            if (string.IsNullOrWhiteSpace(valueOff))
                throw new ArgumentException("valueOff must be provided.", nameof(valueOff));

            _valueOn = valueOn;
            _valueOff = valueOff;
            _getValue = getValue ?? throw new ArgumentNullException(nameof(getValue));
            _setValue = setValue ?? throw new ArgumentNullException(nameof(setValue));
            _onChanged = onChanged;
        }

        public override string GetDisplayText()
        {
            return $"{GetBaseText()}; switch {GetValueLabel(_getValue())}";
        }

        public override string? ActivateAndGetAnnouncement()
        {
            var newValue = !_getValue();
            _setValue(newValue);
            _onChanged?.Invoke(newValue);
            base.ActivateAndGetAnnouncement();
            return GetValueLabel(newValue);
        }

        private string GetValueLabel(bool value)
        {
            return value ? _valueOn : _valueOff;
        }
    }
}
