using System;
using System.Collections.Generic;
using TopSpeed.Input;
using TopSpeed.Menu;

using TopSpeed.Localization;
namespace TopSpeed.Core.Multiplayer
{
    internal sealed partial class MultiplayerCoordinator
    {
        private void RebuildSavedServerFormMenu()
        {
            var controls = new[]
            {
                new MenuFormControl(
                    () => string.IsNullOrWhiteSpace(_state.SavedServers.Draft.Name)
                        ? LocalizationService.Mark("Server name, currently empty.")
                        : LocalizationService.Format(
                            LocalizationService.Mark("Server name, currently set to {0}"),
                            _state.SavedServers.Draft.Name),
                    UpdateSavedServerDraftName),
                new MenuFormControl(
                    () => string.IsNullOrWhiteSpace(_state.SavedServers.Draft.Host)
                        ? LocalizationService.Mark("Server IP or host, currently empty.")
                        : LocalizationService.Format(
                            LocalizationService.Mark("Server IP or host, currently set to {0}"),
                            _state.SavedServers.Draft.Host),
                    UpdateSavedServerDraftHost),
                new MenuFormControl(
                    () => _state.SavedServers.Draft.Port > 0
                        ? LocalizationService.Format(
                            LocalizationService.Mark("Server port, currently set to {0}"),
                            _state.SavedServers.Draft.Port)
                        : LocalizationService.Mark("Server port, currently empty."),
                    UpdateSavedServerDraftPort),
                new MenuFormControl(
                    () => string.IsNullOrWhiteSpace(_state.SavedServers.Draft.DefaultCallSign)
                        ? LocalizationService.Mark("Default call sign, currently empty.")
                        : LocalizationService.Format(
                            LocalizationService.Mark("Default call sign, currently set to {0}"),
                            _state.SavedServers.Draft.DefaultCallSign),
                    UpdateSavedServerDraftDefaultCallSign)
            };

            var saveLabel = _state.SavedServers.EditIndex >= 0
                ? LocalizationService.Mark("Save server changes")
                : LocalizationService.Mark("Save server");
            var items = MenuFormBuilder.BuildItems(
                controls,
                saveLabel,
                SaveSavedServerDraft);
            _menu.UpdateItems(MultiplayerMenuKeys.SavedServerForm, items, preserveSelection: true);
        }

        private void CloseSavedServerForm()
        {
            if (!IsSavedServerDraftDirty())
            {
                _menu.PopToPrevious();
                return;
            }

            _questions.Show(new Question(LocalizationService.Mark("Save changes before closing?"),
                LocalizationService.Mark("Are you sure you would like to discard all changes?"),
                HandleSavedServerDiscardQuestionResult,
                new QuestionButton(QuestionId.Confirm, LocalizationService.Mark("Save changes"), flags: QuestionButtonFlags.Default),
                new QuestionButton(QuestionId.Close, LocalizationService.Mark("Discard changes"))));
        }

        private bool IsSavedServerDraftDirty()
        {
            var current = NormalizeSavedServerDraft(_state.SavedServers.Draft);
            var original = NormalizeSavedServerDraft(_state.SavedServers.Original ?? new SavedServerEntry());

            if (_state.SavedServers.EditIndex < 0)
                return !string.IsNullOrWhiteSpace(current.Host)
                    || !string.IsNullOrWhiteSpace(current.Name)
                    || current.Port != 0
                    || !string.IsNullOrWhiteSpace(current.DefaultCallSign);

            return !string.Equals(current.Name, original.Name, StringComparison.Ordinal)
                || !string.Equals(current.Host, original.Host, StringComparison.OrdinalIgnoreCase)
                || current.Port != original.Port
                || !string.Equals(current.DefaultCallSign, original.DefaultCallSign, StringComparison.Ordinal);
        }

        private void DiscardSavedServerDraftChanges()
        {
            if (_questions.IsQuestionMenu(_menu.CurrentId))
                _menu.PopToPrevious();
            if (string.Equals(_menu.CurrentId, MultiplayerMenuKeys.SavedServerForm, StringComparison.Ordinal))
                _menu.PopToPrevious();
        }

        private void SaveSavedServerDraft()
        {
            var normalized = NormalizeSavedServerDraft(_state.SavedServers.Draft);
            if (string.IsNullOrWhiteSpace(normalized.Host))
            {
                _speech.Speak(LocalizationService.Mark("Server IP or host cannot be empty."));
                return;
            }

            var servers = _settings.SavedServers ?? (_settings.SavedServers = new List<SavedServerEntry>());
            if (_state.SavedServers.EditIndex >= 0 && _state.SavedServers.EditIndex < servers.Count)
                servers[_state.SavedServers.EditIndex] = normalized;
            else
                servers.Add(normalized);

            _saveSettings();
            RebuildSavedServersMenu();

            if (_questions.IsQuestionMenu(_menu.CurrentId))
                _menu.PopToPrevious();
            if (string.Equals(_menu.CurrentId, MultiplayerMenuKeys.SavedServerForm, StringComparison.Ordinal))
                _menu.PopToPrevious();

            _speech.Speak(LocalizationService.Mark("Server saved."));
        }
    }
}







