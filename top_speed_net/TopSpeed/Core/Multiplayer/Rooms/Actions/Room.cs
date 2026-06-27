using TopSpeed.Drive;
using TopSpeed.Localization;
using TopSpeed.Menu;
using TopSpeed.Network;
using TopSpeed.Protocol;
using TopSpeed.Tracks;

namespace TopSpeed.Core.Multiplayer
{
    internal sealed partial class MultiplayerCoordinator
    {
        private const int NoPitAreaDisableId = 4001;
        private const int NoPitAreaRaceAnywayId = 4002;
        private const int NoPitAreaCancelId = 4003;

        private void OpenLeaveRoomConfirmation()
        {
            if (!_state.Rooms.CurrentRoom.InRoom)
            {
                _speech.Speak(LocalizationService.Mark("You are not currently inside a game room."));
                return;
            }

            if (_questions.IsQuestionMenu(_menu.CurrentId))
                return;

            _questions.Show(new Question(LocalizationService.Mark("Leave this game room?"),
                LocalizationService.Mark("Are you sure you want to leave the current room?"),
                QuestionId.No,
                HandleLeaveRoomQuestionResult,
                new QuestionButton(QuestionId.Yes, LocalizationService.Mark("Yes, leave this game room")),
                new QuestionButton(QuestionId.No, LocalizationService.Mark("No, stay in this game room"), flags: QuestionButtonFlags.Default)));
        }

        private void HandleLeaveRoomQuestionResult(int resultId)
        {
            if (resultId == QuestionId.Yes)
                ConfirmLeaveRoom();
        }

        private void ConfirmLeaveRoom()
        {
            var session = SessionOrNull();
            if (session == null)
            {
                if (_state.Connection.ClientState == MultiplayerClientState.Reconnecting)
                {
                    _speech.Speak(LocalizationService.Mark("Reconnection is in progress. Disconnecting now."));
                    Disconnect();
                    return;
                }

                _speech.Speak(LocalizationService.Mark("Not connected to a server. Returning to main menu."));
                Disconnect();
                return;
            }

            if (!TrySend(session.SendRoomLeave(), LocalizationService.Mark("room leave request")))
                return;
            _speech.Speak(LocalizationService.Mark("Leaving game room."));
            _menu.ShowRoot(MultiplayerMenuKeys.Lobby);
        }

        private void StartGame()
        {
            var session = SessionOrNull();
            if (session == null)
            {
                _speech.Speak(LocalizationService.Mark("Not connected to a server."));
                return;
            }

            if (!_state.Rooms.CurrentRoom.InRoom || !_state.Rooms.CurrentRoom.IsHost)
            {
                _speech.Speak(LocalizationService.Mark("Only the host can start the game."));
                return;
            }

            // No-pit-area warning: if the chosen track has no pit area while fuel consumption and/or
            // tire wear are enabled, let the host disable those models for this race only (without
            // changing the persistent room rules). Otherwise start immediately.
            var rules = _state.Rooms.CurrentRoom.GameRulesFlags;
            var fuelEnabled = (rules & (uint)RoomGameRules.FuelConsumption) != 0u;
            var tireEnabled = (rules & (uint)RoomGameRules.TireWear) != 0u;
            var hasPitArea = !Track.TryResolveData(_state.Rooms.CurrentRoom.TrackName, out var trackData)
                || trackData.HasPitArea;

            if (PitAreaWarning.IsRequired(hasPitArea, fuelEnabled, tireEnabled))
            {
                ShowHostNoPitAreaWarning(fuelEnabled, tireEnabled);
                return;
            }

            TrySend(session.SendRoomStartRace(0u), LocalizationService.Mark("race start request"));
        }

        private void ShowHostNoPitAreaWarning(bool fuelEnabled, bool tireEnabled)
        {
            var dialog = new Dialog(
                PitAreaWarning.BuildTitle(),
                PitAreaWarning.BuildCaption(fuelEnabled, tireEnabled),
                NoPitAreaCancelId,
                new[] { new DialogItem(LocalizationService.Mark("Choose how to start this race.")) },
                resultId => HandleHostNoPitAreaResult(resultId, fuelEnabled, tireEnabled),
                new DialogButton(NoPitAreaDisableId, LocalizationService.Mark("Disable for this race"), flags: DialogButtonFlags.Default),
                new DialogButton(NoPitAreaRaceAnywayId, LocalizationService.Mark("Race anyway")),
                new DialogButton(NoPitAreaCancelId, LocalizationService.Mark("Cancel")));
            _dialogs.Show(dialog);
        }

        private void HandleHostNoPitAreaResult(int resultId, bool fuelEnabled, bool tireEnabled)
        {
            if (resultId == NoPitAreaCancelId)
                return;

            var session = SessionOrNull();
            if (session == null)
            {
                _speech.Speak(LocalizationService.Mark("Not connected to a server."));
                return;
            }

            var disableMask = 0u;
            if (resultId == NoPitAreaDisableId)
            {
                if (fuelEnabled)
                    disableMask |= (uint)RoomGameRules.FuelConsumption;
                if (tireEnabled)
                    disableMask |= (uint)RoomGameRules.TireWear;
            }

            TrySend(session.SendRoomStartRace(disableMask), LocalizationService.Mark("race start request"));
        }

        private void AddBotToRoom()
        {
            var session = SessionOrNull();
            if (session == null)
            {
                _speech.Speak(LocalizationService.Mark("Not connected to a server."));
                return;
            }

            if (!_state.Rooms.CurrentRoom.InRoom || !_state.Rooms.CurrentRoom.IsHost || _state.Rooms.CurrentRoom.RoomType != GameRoomType.BotsRace)
            {
                _speech.Speak(LocalizationService.Mark("Bots can only be managed by the host in race-with-bots rooms."));
                return;
            }

            TrySend(session.SendRoomAddBot(), LocalizationService.Mark("add bot request"));
        }

        private void RemoveLastBotFromRoom()
        {
            var session = SessionOrNull();
            if (session == null)
            {
                _speech.Speak(LocalizationService.Mark("Not connected to a server."));
                return;
            }

            if (!_state.Rooms.CurrentRoom.InRoom || !_state.Rooms.CurrentRoom.IsHost || _state.Rooms.CurrentRoom.RoomType != GameRoomType.BotsRace)
            {
                _speech.Speak(LocalizationService.Mark("Bots can only be managed by the host in race-with-bots rooms."));
                return;
            }

            TrySend(session.SendRoomRemoveBot(), LocalizationService.Mark("remove bot request"));
        }
    }
}
