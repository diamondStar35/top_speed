using TopSpeed.Menu;

using TopSpeed.Localization;
namespace TopSpeed.Core.Multiplayer
{
    internal sealed partial class MultiplayerCoordinator
    {
        private void Disconnect()
        {
            _state.Connection.IsPingPending = false;
            _clearSession();
            _speech.Speak(LocalizationService.Mark("Disconnected from server."));
            _menu.ShowRoot("main");
            _menu.FadeInMenuMusic();
            _enterMenuState();
        }

        private void OpenDisconnectConfirmation()
        {
            if (_questions.IsQuestionMenu(_menu.CurrentId))
                return;

            _questions.Show(new Question(LocalizationService.Mark("Leave server?"),
                LocalizationService.Mark("Are you sure you want to disconnect?"),
                QuestionId.No,
                HandleDisconnectQuestionResult,
                new QuestionButton(QuestionId.Yes, LocalizationService.Mark("Yes")),
                new QuestionButton(QuestionId.No, LocalizationService.Mark("No"), flags: QuestionButtonFlags.Default)));
        }

        private void HandleDisconnectQuestionResult(int resultId)
        {
            if (resultId == QuestionId.Yes)
                Disconnect();
        }
    }
}






