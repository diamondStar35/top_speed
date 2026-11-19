using TopSpeed.Menu;

using TopSpeed.Localization;
namespace TopSpeed.Game
{
    internal sealed partial class Game
    {
        private void OpenMultiplayerRaceQuitConfirmation()
        {
            _multiplayerRaceRuntime.OpenQuitConfirmation();
        }

        private void HandleMultiplayerRaceQuitQuestionResult(int resultId)
        {
            _multiplayerRaceRuntime.HandleQuitQuestionResult(resultId);
        }

        private void CancelMultiplayerRaceQuitConfirmation()
        {
            _multiplayerRaceRuntime.CancelQuitConfirmation();
        }

        private void ConfirmQuitMultiplayerRace()
        {
            _multiplayerRaceRuntime.ConfirmQuit();
        }

        private bool TrySendSession(bool sent, string action)
        {
            if (sent)
                return true;

            _speech.Speak(
                LocalizationService.Format(
                    LocalizationService.Mark("Failed to send {0}. Please check your connection."),
                    action));
            return false;
        }
    }
}






