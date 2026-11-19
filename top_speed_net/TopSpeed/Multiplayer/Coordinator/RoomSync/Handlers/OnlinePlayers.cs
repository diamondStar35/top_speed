using System;
using TopSpeed.Localization;
using TopSpeed.Protocol;

namespace TopSpeed.Core.Multiplayer
{
    internal sealed partial class MultiplayerCoordinator
    {
        public void HandleOnlinePlayers(PacketOnlinePlayers onlinePlayers)
        {
            _roomsFlow.HandleOnlinePlayers(onlinePlayers);
        }

        internal void HandleOnlinePlayersCore(PacketOnlinePlayers onlinePlayers)
        {
            _state.Rooms.ApplyOnlinePlayers(onlinePlayers);
            _roomUi.HandleOnlinePlayers();
        }
    }
}

