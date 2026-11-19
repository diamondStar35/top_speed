using TopSpeed.Protocol;

namespace TopSpeed.Core.Multiplayer
{
    internal sealed partial class MultiplayerCoordinator
    {
        internal void HandleRoomRaceStateChangedCore(PacketRoomRaceStateChanged roomRaceStateChanged)
        {
            var change = _roomReducer.ApplyRaceState(roomRaceStateChanged);
            if (change.Applied)
                SyncClientStateFromRoomStore();
            _roomUi.HandleRoomRaceStateChanged(change);
        }
    }
}
