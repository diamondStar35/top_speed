using TopSpeed.Protocol;

namespace TopSpeed.Core.Multiplayer
{
    internal sealed partial class MultiplayerCoordinator
    {
        public void HandleRoomList(PacketRoomList roomList)
        {
            _roomsFlow.HandleRoomList(roomList);
        }

        internal void HandleRoomListCore(PacketRoomList roomList)
        {
            _roomReducer.ApplyRoomList(roomList);
            _roomUi.HandleRoomList();
        }
    }
}

