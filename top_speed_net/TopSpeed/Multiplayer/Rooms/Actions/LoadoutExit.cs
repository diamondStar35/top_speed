using TopSpeed.Localization;
using TopSpeed.Menu;
using TopSpeed.Protocol;

namespace TopSpeed.Core.Multiplayer
{
    internal sealed partial class MultiplayerCoordinator
    {
        private const int QuitLoadoutQuestionYesId = 2001;
        private const int QuitLoadoutQuestionNoId = 2002;
        private const int CancelGameQuestionYesId = 2003;
        private const int CancelGameQuestionNoId = 2004;

        private void OpenLoadoutExitConfirmation()
        {
            if (_questions.IsQuestionMenu(_menu.CurrentId))
                return;

            if (_state.Rooms.CurrentRoom.InRoom
                && _state.Rooms.CurrentRoom.IsHost
                && _state.Rooms.CurrentRoom.RaceState == RoomRaceState.Preparing)
            {
                _questions.Show(new Question(LocalizationService.Mark("Cancel current game?"),
                    LocalizationService.Mark("If you leave race preparation now, the current game will be canceled for everyone in this room."),
                    CancelGameQuestionNoId,
                    HandleCancelGameQuestionResult,
                    new QuestionButton(CancelGameQuestionYesId, LocalizationService.Mark("Yes, cancel the current game")),
                    new QuestionButton(CancelGameQuestionNoId, LocalizationService.Mark("No, continue preparing"), flags: QuestionButtonFlags.Default)));
                return;
            }

            _questions.Show(new Question(LocalizationService.Mark("Quit race preparation?"),
                LocalizationService.Mark("Do you want to quit race preparation and stay in this game room?"),
                QuitLoadoutQuestionNoId,
                HandleQuitLoadoutQuestionResult,
                new QuestionButton(QuitLoadoutQuestionYesId, LocalizationService.Mark("Yes, quit race preparation")),
                new QuestionButton(QuitLoadoutQuestionNoId, LocalizationService.Mark("No, continue preparing"), flags: QuestionButtonFlags.Default)));
        }

        private void HandleCancelGameQuestionResult(int resultId)
        {
            if (resultId == CancelGameQuestionYesId)
                ConfirmCancelCurrentGame();
            else
                _menu.ShowRoot(MultiplayerMenuKeys.LoadoutVehicle);
        }

        private void ConfirmCancelCurrentGame()
        {
            var session = SessionOrNull();
            if (session == null)
            {
                _speech.Speak(LocalizationService.Mark("Not connected to a server."));
                return;
            }

            if (!_state.Rooms.CurrentRoom.InRoom || !_state.Rooms.CurrentRoom.IsHost)
            {
                _speech.Speak(LocalizationService.Mark("Only the host can cancel the current game."));
                return;
            }

            TrySend(session.SendRoomRaceControl(RoomRaceControlAction.CancelPrepare), LocalizationService.Mark("game cancel request"));
        }

        private void HandleQuitLoadoutQuestionResult(int resultId)
        {
            if (resultId == QuitLoadoutQuestionYesId)
                ConfirmQuitLoadout();
            else
                _menu.ShowRoot(MultiplayerMenuKeys.LoadoutVehicle);
        }

        private void ConfirmQuitLoadout()
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

            if (_state.Rooms.CurrentRoom.RaceState == RoomRaceState.Preparing)
            {
                if (!TrySend(session.SendRoomPlayerWithdraw(), LocalizationService.Mark("race preparation withdrawal")))
                    return;
                _speech.Speak(LocalizationService.Mark("You left race preparation and returned to room controls."));
            }
            else
            {
                _speech.Speak(LocalizationService.Mark("Returned to room controls."));
            }

            _menu.ShowRoot(MultiplayerMenuKeys.RoomControls);
        }
    }
}
