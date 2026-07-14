using System;
using TopSpeed.Protocol;

namespace TopSpeed.Core.Multiplayer
{
    internal sealed class RoomSnapshot
    {
        public uint RoomVersion;
        public uint EventSequence;
        public uint RoomId;
        public uint RaceInstanceId;
        public uint HostPlayerId;
        public string RoomName = string.Empty;
        public GameRoomType RoomType;
        public byte PlayersToStart;
        public RoomRaceState RaceState;
        public bool RacePaused;
        public bool InRoom;
        public bool IsHost;
        public string TrackName = string.Empty;
        public TrackPackageRef Track = TrackPackageRef.BuiltIn(string.Empty);
        public int Laps;
        public uint GameRulesFlags;
        // Effective rules for the in-progress race instance: the persistent GameRulesFlags minus
        // any per-race transient disable mask. Equals GameRulesFlags when there is no override.
        public uint RaceEffectiveGameRulesFlags;
        public RoomParticipant[] Players = Array.Empty<RoomParticipant>();
    }
}

