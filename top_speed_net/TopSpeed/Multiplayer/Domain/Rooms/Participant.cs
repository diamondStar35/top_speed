using TopSpeed.Protocol;

namespace TopSpeed.Core.Multiplayer
{
    internal sealed class RoomParticipant
    {
        public uint PlayerId;
        public byte PlayerNumber;
        public PlayerState State;
        public string Name = string.Empty;
    }
}

