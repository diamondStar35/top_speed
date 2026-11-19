using System;
using TopSpeed.Localization;
using TopSpeed.Network;
using TopSpeed.Speech;

namespace TopSpeed.Core.Multiplayer
{
    internal sealed partial class MultiplayerCoordinator
    {
        private void HandleServerPortInput(string text)
        {
            var trimmed = (text ?? string.Empty).Trim();
            if (string.IsNullOrEmpty(trimmed))
            {
                _settings.DefaultServerPort = ClientProtocol.DefaultServerPort;
                _saveSettings();
                _speech.Speak(LocalizationService.Format(
                    LocalizationService.Mark("Default server port reset to {0}."),
                    ClientProtocol.DefaultServerPort));
                return;
            }

            if (!int.TryParse(trimmed, out var port) || port < 1 || port > 65535)
            {
                _speech.Speak(LocalizationService.Mark("Invalid port. Enter a number between 1 and 65535."));
                BeginServerPortEntry();
                return;
            }

            _settings.DefaultServerPort = port;
            _saveSettings();
            _speech.Speak(LocalizationService.Format(LocalizationService.Mark("Default server port set to {0}."), port));
        }

        private int ResolveServerPort()
        {
            return _settings.DefaultServerPort >= 1 && _settings.DefaultServerPort <= 65535
                ? _settings.DefaultServerPort
                : ClientProtocol.DefaultServerPort;
        }

        private void PromptServerAddressInput(string? initialValue)
        {
            _promptTextInput(
                LocalizationService.Mark("Enter the server IP address or domain."),
                initialValue,
                SpeechService.SpeakFlag.None,
                true,
                result =>
                {
                    if (result.Cancelled)
                        return;

                    if (!HandleServerAddressInput(result.Text))
                    {
                        var retry = string.IsNullOrWhiteSpace(result.Text) ? initialValue : result.Text;
                        if (OperatingSystem.IsAndroid())
                            retry = null;
                        PromptServerAddressInput(retry);
                    }
                });
        }

        private void PromptCallSignInput(string? initialValue)
        {
            _promptTextInput(
                LocalizationService.Mark("Enter your call sign."),
                initialValue,
                SpeechService.SpeakFlag.None,
                true,
                result =>
                {
                    if (result.Cancelled)
                        return;

                    if (!HandleCallSignInput(result.Text))
                        PromptCallSignInput(result.Text);
                });
        }

        private void HandleDefaultCallSignInput(string text)
        {
            var trimmed = (text ?? string.Empty).Trim();
            _settings.DefaultCallSign = trimmed;
            _saveSettings();

            if (trimmed.Length == 0)
            {
                _speech.Speak(LocalizationService.Mark("Default call sign cleared."));
                return;
            }

            _speech.Speak(LocalizationService.Format(
                LocalizationService.Mark("Default call sign set to {0}."),
                trimmed));
        }
    }
}

