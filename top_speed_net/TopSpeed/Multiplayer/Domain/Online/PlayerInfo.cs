using TopSpeed.Protocol;

namespace TopSpeed.Core.Multiplayer
{
    internal sealed class OnlinePlayerInfo
    {
        public uint PlayerId;
        public byte PlayerNumber;
        public string Name = string.Empty;
        public OnlinePresenceState PresenceState;
        public string RoomName = string.Empty;
    }
}

