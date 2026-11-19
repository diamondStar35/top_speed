using TopSpeed.Protocol;

namespace TopSpeed.Core.Multiplayer
{
    internal sealed partial class MultiplayerCoordinator
    {
        public void HandleRoomEvent(PacketRoomEvent roomEvent)
        {
            _roomsFlow.HandleRoomEvent(roomEvent);
        }

        public void HandleRoomRaceStateChanged(PacketRoomRaceStateChanged roomRaceStateChanged)
        {
            _roomsFlow.HandleRoomRaceStateChanged(roomRaceStateChanged);
        }

        internal void HandleRoomEventCore(PacketRoomEvent roomEvent)
        {
            var eventInfo = RoomMap.ToEvent(roomEvent);
            if (eventInfo == null)
                return;

            var session = SessionOrNull();
            var localPlayerId = session?.PlayerId ?? 0u;

            var result = _roomReducer.ApplyRoomEvent(eventInfo, localPlayerId);
            if (result.UpdatedCurrentRoom)
                SyncClientStateFromRoomStore();
            _roomUi.HandleRoomEvent(eventInfo, localPlayerId, result.UpdatedCurrentRoom, result.LocalHostChanged);
        }
    }
}

