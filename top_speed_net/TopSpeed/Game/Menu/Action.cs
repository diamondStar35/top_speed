using TopSpeed.Menu;
using TopSpeed.Core;
using TopSpeed.Localization;

namespace TopSpeed.Game
{
    internal sealed partial class Game
    {
        private void HandleMenuAction(MenuAction action)
        {
            switch (action)
            {
                case MenuAction.Exit:
                    RequestExit();
                    break;
                case MenuAction.QuickStart:
                    PrepareQuickStart();
                    QueueDriveStart(DriveMode.QuickStart);
                    break;
                default:
                    break;
            }
        }

        private void RequestExit()
        {
            if (!_settings.ConfirmExit)
            {
                ExitRequested?.Invoke();
                return;
            }

            if (_multiplayerCoordinator.Questions.IsQuestionMenu(_menu.CurrentId))
                return;

            _multiplayerCoordinator.Questions.Show(new Question(
                LocalizationService.Mark("Exit game?"),
                LocalizationService.Mark("Are you sure you want to exit the game?"),
                QuestionId.No,
                resultId =>
                {
                    if (resultId == QuestionId.Yes)
                        ExitRequested?.Invoke();
                },
                new QuestionButton(QuestionId.Yes, LocalizationService.Mark("Yes")),
                new QuestionButton(QuestionId.No, LocalizationService.Mark("No"), flags: QuestionButtonFlags.Default)));
        }

        private bool UpdateModalOperations()
        {
            return _multiplayerCoordinator.UpdatePendingOperations();
        }

        private void EnterMenuState()
        {
            _state = AppState.Menu;
        }
    }
}



