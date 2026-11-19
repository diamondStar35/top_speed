using System.Collections.Generic;
using TopSpeed.Input;
using TopSpeed.Menu;

using TopSpeed.Localization;
namespace TopSpeed.Core.Multiplayer
{
    internal sealed partial class MultiplayerCoordinator
    {
        private void OpenDeleteSavedServerConfirm(int index)
        {
            if (index < 0 || index >= SavedServers.Count)
                return;

            _state.SavedServers.PendingDeleteIndex = index;
            _questions.Show(new Question(LocalizationService.Mark("Delete this server?"),
                LocalizationService.Mark("This will remove the saved server entry from the list. Are you sure you would like to continue?"),
                HandleDeleteSavedServerQuestionResult,
                new QuestionButton(QuestionId.Yes, LocalizationService.Mark("Yes, delete this server")),
                new QuestionButton(QuestionId.No, LocalizationService.Mark("No, keep this server"), flags: QuestionButtonFlags.Default)));
        }

        private void HandleSavedServerDiscardQuestionResult(int resultId)
        {
            if (resultId == QuestionId.Confirm)
                SaveSavedServerDraft();
            else if (resultId == QuestionId.Close || resultId == QuestionId.Cancel || resultId == QuestionId.No)
                DiscardSavedServerDraftChanges();
        }

        private void HandleDeleteSavedServerQuestionResult(int resultId)
        {
            if (resultId == QuestionId.Yes)
                ConfirmDeleteSavedServer();
        }

        private void ConfirmDeleteSavedServer()
        {
            var servers = _settings.SavedServers ?? (_settings.SavedServers = new List<SavedServerEntry>());
            if (_state.SavedServers.PendingDeleteIndex < 0 || _state.SavedServers.PendingDeleteIndex >= servers.Count)
            {
                if (_questions.IsQuestionMenu(_menu.CurrentId))
                    _menu.PopToPrevious();
                return;
            }

            servers.RemoveAt(_state.SavedServers.PendingDeleteIndex);
            _state.SavedServers.PendingDeleteIndex = -1;
            _saveSettings();
            RebuildSavedServersMenu();
            if (_questions.IsQuestionMenu(_menu.CurrentId))
                _menu.PopToPrevious();
            _speech.Speak(LocalizationService.Mark("Server deleted."));
        }
    }
}






