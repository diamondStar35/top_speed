using System;
using TopSpeed.Localization;
using TopSpeed.Runtime;
using TopSpeed.Speech;

namespace TopSpeed.Game
{
    internal sealed partial class Game
    {
        private void SetUseUpdateProxy(bool enabled)
        {
            _settings.UseUpdateProxy = enabled;
            ApplyUpdateProxySettings();
            SaveSettings();
            _menuRegistry.RefreshGeneralSettingsMenu();
        }

        private void EditUpdateProxyUrl()
        {
            if (!_settings.UseUpdateProxy)
            {
                _speech.Speak(LocalizationService.Mark("Enable update download proxy first."));
                return;
            }

            BeginPromptTextInput(
                LocalizationService.Mark("Enter proxy URL"),
                _settings.UpdateProxyUrlPrefix ?? string.Empty,
                SpeechService.SpeakFlag.None,
                true,
                HandleUpdateProxyUrlInput);
        }

        private void HandleUpdateProxyUrlInput(TextInputResult result)
        {
            if (result.Cancelled)
                return;

            var normalized = NormalizeProxyUrl((result.Text ?? string.Empty).Trim());
            if (string.IsNullOrWhiteSpace(normalized))
            {
                _speech.Speak(LocalizationService.Mark("Proxy URL cannot be empty while proxy is enabled."));
                return;
            }

            if (!Uri.TryCreate(normalized, UriKind.Absolute, out var uri)
                || (!string.Equals(uri.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase)
                    && !string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase)))
            {
                _speech.Speak(LocalizationService.Mark("Proxy URL must be a valid HTTP or HTTPS URL."));
                return;
            }

            _settings.UpdateProxyUrlPrefix = normalized;
            ApplyUpdateProxySettings();
            SaveSettings();
            _menuRegistry.RefreshGeneralSettingsMenu();
            _speech.Speak(LocalizationService.Mark("Proxy URL saved."));
        }

        private static string NormalizeProxyUrl(string value)
        {
            var trimmed = value.Trim();
            if (trimmed.Length == 0)
                return string.Empty;
            if (!trimmed.EndsWith("/", StringComparison.Ordinal))
                trimmed += "/";
            return trimmed;
        }
    }
}
