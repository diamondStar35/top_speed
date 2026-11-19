using System;
using TopSpeed.Localization;
using TopSpeed.Runtime;
using TopSpeed.Speech;

namespace TopSpeed.Core.Multiplayer
{
    internal sealed partial class MultiplayerCoordinator
    {
        private void OpenGlobalChatInput()
        {
            var session = SessionOrNull();
            if (session == null)
            {
                _speech.Speak(LocalizationService.Mark("Not connected to a server."));
                return;
            }

            _promptTextInput(
                LocalizationService.Mark("Enter your global chat message."),
                null,
                SpeechService.SpeakFlag.None,
                true,
                HandleGlobalChatInput);
        }

        private void OpenRoomChatInput()
        {
            var session = SessionOrNull();
            if (session == null)
            {
                _speech.Speak(LocalizationService.Mark("Not connected to a server."));
                return;
            }

            if (!_state.Rooms.CurrentRoom.InRoom)
            {
                _speech.Speak(LocalizationService.Mark("You are not in a game room."));
                return;
            }

            _promptTextInput(
                LocalizationService.Mark("Enter your room chat message."),
                null,
                SpeechService.SpeakFlag.None,
                true,
                HandleRoomChatInput);
        }

        private void HandleGlobalChatInput(TextInputResult result)
        {
            if (result.Cancelled)
                return;

            var session = SessionOrNull();
            if (session == null)
            {
                _speech.Speak(LocalizationService.Mark("Not connected to a server."));
                return;
            }

            var text = (result.Text ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(text))
            {
                _speech.Speak(LocalizationService.Mark("Chat message cannot be empty."));
                return;
            }

            if (!session.SendChatMessage(text))
                _speech.Speak(LocalizationService.Mark("Failed to send chat message."));
        }

        private void HandleRoomChatInput(TextInputResult result)
        {
            if (result.Cancelled)
                return;

            var session = SessionOrNull();
            if (session == null)
            {
                _speech.Speak(LocalizationService.Mark("Not connected to a server."));
                return;
            }

            if (!_state.Rooms.CurrentRoom.InRoom)
            {
                _speech.Speak(LocalizationService.Mark("You are not in a game room."));
                return;
            }

            var text = (result.Text ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(text))
            {
                _speech.Speak(LocalizationService.Mark("Chat message cannot be empty."));
                return;
            }

            if (!session.SendRoomChatMessage(text))
                _speech.Speak(LocalizationService.Mark("Failed to send room chat message."));
        }

        internal void OpenGlobalChatHotkey()
        {
            _chatFlow.OpenGlobalChatHotkey();
        }

        internal void OpenGlobalChatHotkeyCore()
        {
            OpenGlobalChatInput();
        }

        internal void OpenRoomChatHotkey()
        {
            _chatFlow.OpenRoomChatHotkey();
        }

        internal void OpenRoomChatHotkeyCore()
        {
            OpenRoomChatInput();
        }
    }
}



