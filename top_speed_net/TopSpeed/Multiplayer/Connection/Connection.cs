using System;
using TopSpeed.Network;
using TopSpeed.Localization;
using TopSpeed.Speech;

namespace TopSpeed.Core.Multiplayer
{
    internal sealed partial class MultiplayerCoordinator
    {
        public void BeginManualServerEntry()
        {
            _connectionFlow.BeginManualServerEntry();
        }

        internal void BeginManualServerEntryCore()
        {
            string? initialAddress = _settings.LastServerAddress;
            if (OperatingSystem.IsAndroid())
                initialAddress = null;
            PromptServerAddressInput(initialAddress);
        }

        public void BeginServerPortEntry()
        {
            _connectionFlow.BeginServerPortEntry();
        }

        public void BeginDefaultCallSignEntry()
        {
            _connectionFlow.BeginDefaultCallSignEntry();
        }

        internal void BeginServerPortEntryCore()
        {
            var current = _settings.DefaultServerPort.ToString();
            _promptTextInput(
                LocalizationService.Mark("Enter the default server port used for manual connections."),
                current,
                SpeechService.SpeakFlag.None,
                true,
                result =>
                {
                    if (result.Cancelled)
                        return;

                    HandleServerPortInput(result.Text);
                });
        }

        internal void BeginDefaultCallSignEntryCore()
        {
            _promptTextInput(
                LocalizationService.Mark("Enter the default call sign used when connecting to servers. Leave empty to clear it."),
                _settings.DefaultCallSign,
                SpeechService.SpeakFlag.None,
                true,
                result =>
                {
                    if (result.Cancelled)
                        return;

                    HandleDefaultCallSignInput(result.Text);
                });
        }

        private MultiplayerSession? SessionOrNull()
        {
            return _getSession();
        }
    }
}


