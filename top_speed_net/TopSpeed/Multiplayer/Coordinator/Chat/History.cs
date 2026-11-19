using TopSpeed.Speech;
using TopSpeed.Menu;
using TopSpeed.Input;
using TopSpeed.Localization;

namespace TopSpeed.Core.Multiplayer
{
    internal sealed partial class MultiplayerCoordinator
    {
        private void AddGlobalChatMessage(string message)
        {
            var text = NormalizeChatMessage(message);
            if (text == null)
                return;

            _state.Chat.History.AddGlobalChat(text);
            UpdateHistoryScreens();
        }

        private void AddRoomChatMessage(string message)
        {
            var text = NormalizeChatMessage(message);
            if (text == null)
                return;

            _state.Chat.History.AddRoomChat(text);
            UpdateHistoryScreens();
        }

        private void AddConnectionMessage(string message)
        {
            var text = NormalizeChatMessage(message);
            if (text == null)
                return;

            _state.Chat.History.AddConnection(text);
            UpdateHistoryScreens();
        }

        private void AddRoomEventMessage(string message)
        {
            var text = NormalizeChatMessage(message);
            if (text == null)
                return;

            _state.Chat.History.AddRoomEvent(text);
            UpdateHistoryScreens();
        }

        internal void NextChatCategory()
        {
            _chatFlow.NextCategory();
        }

        internal void NextChatCategoryCore()
        {
            var result = _state.Chat.History.MoveToNext(wrapNavigation: false);
            if (result.Moved)
                PlayNetworkSound("buffer_switch.ogg");
            UpdateHistoryScreens();
            SpeakHistoryNavigationResult(result);
        }

        internal void PreviousChatCategory()
        {
            _chatFlow.PreviousCategory();
        }

        internal void PreviousChatCategoryCore()
        {
            var result = _state.Chat.History.MoveToPrevious(wrapNavigation: false);
            if (result.Moved)
                PlayNetworkSound("buffer_switch.ogg");
            UpdateHistoryScreens();
            SpeakHistoryNavigationResult(result);
        }

        internal void NextChatItem()
        {
            _chatFlow.NextItem();
        }

        internal void NextChatItemCore()
        {
            var result = _state.Chat.History.MoveCurrentItem(1, wrapNavigation: false);
            SpeakHistoryNavigationResult(result);
        }

        internal void PreviousChatItem()
        {
            _chatFlow.PreviousItem();
        }

        internal void PreviousChatItemCore()
        {
            var result = _state.Chat.History.MoveCurrentItem(-1, wrapNavigation: false);
            SpeakHistoryNavigationResult(result);
        }

        internal void FirstChatItem()
        {
            _chatFlow.FirstItem();
        }

        internal void FirstChatItemCore()
        {
            var result = _state.Chat.History.MoveCurrentItemToFirst();
            SpeakHistoryNavigationResult(result);
        }

        internal void LastChatItem()
        {
            _chatFlow.LastItem();
        }

        internal void LastChatItemCore()
        {
            var result = _state.Chat.History.MoveCurrentItemToLast();
            SpeakHistoryNavigationResult(result);
        }

        internal void CopyFocusedChatItem()
        {
            _chatFlow.CopyFocusedItem();
        }

        internal void CopyFocusedChatItemCore()
        {
            CopyHistoryTextToClipboard(_state.Chat.History.GetCurrentFocusedItemText());
        }

        internal bool TryHandleRaceLoopHistoryShortcuts(IInputService input)
        {
            if (input == null)
                return false;

            return _menu.TryTriggerShortcutAction(MultiplayerBufferFirstItemShortcutActionId, input)
                || _menu.TryTriggerShortcutAction(MultiplayerBufferLastItemShortcutActionId, input)
                || _menu.TryTriggerShortcutAction(MultiplayerBufferPreviousItemShortcutActionId, input)
                || _menu.TryTriggerShortcutAction(MultiplayerBufferNextItemShortcutActionId, input)
                || _menu.TryTriggerShortcutAction(MultiplayerBufferPreviousCategoryShortcutActionId, input)
                || _menu.TryTriggerShortcutAction(MultiplayerBufferNextCategoryShortcutActionId, input)
                || _menu.TryTriggerShortcutAction(MultiplayerBufferCopyFocusedItemShortcutActionId, input)
                || _menu.TryTriggerShortcutAction(MultiplayerCommunicatorToggleShortcutActionId, input)
                || _menu.TryTriggerShortcutAction(MultiplayerCommunicatorSetFrequencyShortcutActionId, input)
                || _menu.TryTriggerShortcutAction(MultiplayerCommunicatorAnnounceFrequencyShortcutActionId, input)
                || _menu.TryTriggerShortcutAction(MultiplayerCommunicatorToggleVoiceActivationShortcutActionId, input);
        }

        private void PlayChatItemNavigationFeedback(Chat.HistoryMoveResult result)
        {
            var menuId = _menu.CurrentId ?? string.Empty;
            if (result.Moved)
                _menu.TryPlayMenuCue(menuId, MenuFeedbackCue.Navigate);

            if (result.Wrapped)
                _menu.TryPlayMenuCue(menuId, MenuFeedbackCue.Wrap);
            else if (result.EdgeReached)
                _menu.TryPlayMenuCue(menuId, MenuFeedbackCue.Edge);
        }

        private void SpeakHistoryNavigationResult(Chat.HistoryMoveResult result)
        {
            if (InteractionHints.IsTouchPlatform())
                PlayChatItemNavigationFeedback(result);

            if (!result.Moved && result.EdgeReached)
                return;

            _speech.Speak(result.Text, SpeechService.SpeakFlag.None);
        }

        private void CopyHistoryTextToClipboard(string text)
        {
            _trySetClipboardText((text ?? string.Empty).Trim());
            _speech.Speak(LocalizationService.Mark("Copied."));
        }

        private void CopyHistoryItemFromScreen(string text)
        {
            CopyHistoryTextToClipboard(text);
        }
    }
}



