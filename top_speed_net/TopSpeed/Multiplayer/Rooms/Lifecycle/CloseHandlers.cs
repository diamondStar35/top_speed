using TopSpeed.Menu;

namespace TopSpeed.Core.Multiplayer
{
    internal sealed partial class MultiplayerCoordinator
    {
        private bool HandleLobbyClose(CloseEvent _)
        {
            OpenDisconnectConfirmation();
            return true;
        }

        private bool HandleRoomControlsClose(CloseEvent _)
        {
            if (!_state.Rooms.CurrentRoom.InRoom)
            {
                _menu.ShowRoot(MultiplayerMenuKeys.Lobby);
                return true;
            }

            OpenLeaveRoomConfirmation();
            return true;
        }

        private bool HandleSavedServerFormClose(CloseEvent _)
        {
            CloseSavedServerForm();
            return true;
        }

        private bool HandleRoomOptionsClose(CloseEvent _)
        {
            CancelRoomOptionsChanges();
            return false;
        }

        private bool HandleRoomGameRulesClose(CloseEvent _)
        {
            if (!TryApplyRoomGameRulesDraft(announceNotConnected: true, out var appliedAny))
                return true;
            return false;
        }

        private bool HandleLoadoutVehicleClose(CloseEvent _)
        {
            OpenLoadoutExitConfirmation();
            return true;
        }
    }
}
