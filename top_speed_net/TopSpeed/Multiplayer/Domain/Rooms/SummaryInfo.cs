using TopSpeed.Protocol;

namespace TopSpeed.Core.Multiplayer
{
    internal sealed class RoomSummaryInfo
    {
        public uint RoomId;
        public uint RoomVersion;
        public string RoomName = string.Empty;
        public GameRoomType RoomType;
        public byte PlayerCount;
        public byte PlayersToStart;
        public RoomRaceState RaceState;
        public string TrackName = string.Empty;
    }
}

