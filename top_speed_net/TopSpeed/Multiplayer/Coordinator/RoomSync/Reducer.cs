using TopSpeed.Protocol;

namespace TopSpeed.Core.Multiplayer
{
    internal sealed class RoomPacketReducer
    {
        private readonly CoordinatorState _state;

        public RoomPacketReducer(CoordinatorState state)
        {
            _state = state;
        }

        public void ApplyRoomList(PacketRoomList roomList)
        {
            _state.Rooms.ApplyRoomList(roomList);
        }

        public RoomStateChange ApplyRoomState(PacketRoomState roomState)
        {
            return _state.Rooms.ApplyRoomState(roomState);
        }

        public RoomRaceChange ApplyRaceState(PacketRoomRaceStateChanged roomRaceStateChanged)
        {
            return _state.Rooms.ApplyRaceState(roomRaceStateChanged);
        }

        public RoomEventReduceResult ApplyRoomEvent(RoomEventInfo eventInfo, uint localPlayerId)
        {
            var updatedCurrentRoom = _state.Rooms.TryApplyCurrentRoomEvent(
                eventInfo,
                localPlayerId,
                out var localHostChanged);
            return new RoomEventReduceResult(updatedCurrentRoom, localHostChanged);
        }
    }

    internal readonly struct RoomEventReduceResult
    {
        public RoomEventReduceResult(bool updatedCurrentRoom, bool localHostChanged)
        {
            UpdatedCurrentRoom = updatedCurrentRoom;
            LocalHostChanged = localHostChanged;
        }

        public bool UpdatedCurrentRoom { get; }
        public bool LocalHostChanged { get; }
    }
}
