using TopSpeed.Protocol;

namespace TopSpeed.Core.Multiplayer
{
    internal sealed class RoomEventInfo
    {
        public uint RoomId;
        public uint RoomVersion;
        public uint EventSequence;
        public uint RaceInstanceId;
        public RoomEventKind Kind;
        public uint HostPlayerId;
        public GameRoomType RoomType;
        public byte PlayerCount;
        public byte PlayersToStart;
        public RoomRaceState RaceState;
        public bool RacePaused;
        public string TrackName = string.Empty;
        public TrackPackageRef Track = TrackPackageRef.BuiltIn(string.Empty);
        public int Laps;
        public uint GameRulesFlags;
        public string RoomName = string.Empty;
        public uint SubjectPlayerId;
        public byte SubjectPlayerNumber;
        public PlayerState SubjectPlayerState;
        public string SubjectPlayerName = string.Empty;
    }
}

