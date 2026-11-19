using System;
using TopSpeed.Localization;
using TopSpeed.Speech;

namespace TopSpeed.Core.Multiplayer
{
    internal sealed partial class MultiplayerCoordinator
    {
        private void UpdateSavedServerDraftName()
        {
            _promptTextInput(
                LocalizationService.Mark("Enter the server name."),
                _state.SavedServers.Draft.Name,
                SpeechService.SpeakFlag.None,
                true,
                result =>
                {
                    if (result.Cancelled)
                        return;

                    _state.SavedServers.Draft.Name = (result.Text ?? string.Empty).Trim();
                    RebuildSavedServerFormMenu();
                });
        }

        private void UpdateSavedServerDraftHost()
        {
            _promptTextInput(
                LocalizationService.Mark("Enter the server IP address or host name."),
                _state.SavedServers.Draft.Host,
                SpeechService.SpeakFlag.None,
                true,
                result =>
                {
                    if (result.Cancelled)
                        return;

                    _state.SavedServers.Draft.Host = (result.Text ?? string.Empty).Trim();
                    RebuildSavedServerFormMenu();
                });
        }

        private void UpdateSavedServerDraftPort()
        {
            var current = _state.SavedServers.Draft.Port > 0 ? _state.SavedServers.Draft.Port.ToString() : string.Empty;
            _promptTextInput(
                LocalizationService.Mark("Enter the server port, or leave empty for default."),
                current,
                SpeechService.SpeakFlag.None,
                true,
                result =>
                {
                    if (result.Cancelled)
                        return;

                    var trimmed = (result.Text ?? string.Empty).Trim();
                    if (string.IsNullOrWhiteSpace(trimmed))
                    {
                        _state.SavedServers.Draft.Port = 0;
                        RebuildSavedServerFormMenu();
                        return;
                    }

                    if (!int.TryParse(trimmed, out var port) || port < 1 || port > 65535)
                    {
                        _speech.Speak(LocalizationService.Mark("Invalid port. Enter a number between 1 and 65535."));
                        return;
                    }

                    _state.SavedServers.Draft.Port = port;
                    RebuildSavedServerFormMenu();
                });
        }

        private void UpdateSavedServerDraftDefaultCallSign()
        {
            _promptTextInput(
                LocalizationService.Mark("Enter the default call sign for this server. Leave empty to use the global default call sign."),
                _state.SavedServers.Draft.DefaultCallSign,
                SpeechService.SpeakFlag.None,
                true,
                result =>
                {
                    if (result.Cancelled)
                        return;

                    _state.SavedServers.Draft.DefaultCallSign = (result.Text ?? string.Empty).Trim();
                    RebuildSavedServerFormMenu();
                });
        }
    }
}


