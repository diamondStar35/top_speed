using System;
using System.Collections.Generic;
using System.Net;
using TopSpeed.Bots;
using TopSpeed.Data;
using TopSpeed.Protocol;
using TopSpeed.Server.Bots;

namespace TopSpeed.Server.Network
{
    internal sealed class RoomRaceParticipantResult
    {
        public uint PlayerId { get; set; }
        public byte PlayerNumber { get; set; }
        public RoomRaceResultStatus Status { get; set; } = RoomRaceResultStatus.Pending;
        public RaceParticipantLifecycleState Lifecycle { get; set; } = RaceParticipantLifecycleState.Joined;
        public int TimeMs { get; set; }
        public byte FinishOrder { get; set; }
    }

    internal sealed class GameRoom
    {
        public GameRoom(uint id, string name, GameRoomType roomType, byte playersToStart)
        {
            Id = id;
            Name = name;
            RoomType = roomType;
            PlayersToStart = playersToStart;
            TrackName = "america";
            TrackSelection = TrackPackageRef.BuiltIn("america");
            Laps = 3;
        }

        public uint Id { get; }
        public uint Version { get; set; }
        public uint EventSequence { get; set; }
        public uint RaceInstanceId { get; set; }
        public string Name { get; set; }
        public GameRoomType RoomType { get; set; }
        public byte PlayersToStart { get; set; }
        public uint HostId { get; set; }
        public HashSet<uint> PlayerIds { get; } = new HashSet<uint>();
        public Dictionary<uint, RoomMemberPresenceState> MemberPresence { get; } = new Dictionary<uint, RoomMemberPresenceState>();
        public List<RoomBot> Bots { get; } = new List<RoomBot>();
        public Dictionary<uint, PlayerLoadout> PendingLoadouts { get; } = new Dictionary<uint, PlayerLoadout>();
        public HashSet<uint> PrepareSkips { get; } = new HashSet<uint>();
        public RoomRaceState RaceState { get; set; } = RoomRaceState.Lobby;
        public bool RacePaused { get; set; }
        public bool PreparingRace => RaceState == RoomRaceState.Preparing;
        public bool RaceStarted => RaceState == RoomRaceState.Racing;
        public bool TrackSelected { get; set; }
        public TrackData? TrackData { get; set; }
        public string TrackName { get; set; }
        public TrackPackageRef TrackSelection { get; set; }
        public byte Laps { get; set; }
        public uint GameRulesFlags { get; set; }
        // Effective rules for the current race instance = GameRulesFlags minus the host's per-race
        // disable mask (set when the race starts). Equals GameRulesFlags when there is no override.
        public uint RaceEffectiveGameRulesFlags { get; set; }
        public HashSet<uint> TrackReadyPlayers { get; } = new HashSet<uint>();
        public HashSet<uint> ActiveRaceParticipantIds { get; } = new HashSet<uint>();
        public List<RoomEventJournalEntry> EventJournal { get; } = new List<RoomEventJournalEntry>();
        public List<byte> RaceResults { get; } = new List<byte>();
        public Dictionary<byte, int> RaceFinishTimesMs { get; } = new Dictionary<byte, int>();
        public Dictionary<uint, RoomRaceParticipantResult> RaceParticipantResults { get; } = new Dictionary<uint, RoomRaceParticipantResult>();
        public DateTime RaceStartedUtc { get; set; }
        public bool RaceStopPending { get; set; }
        public float RaceStopDelaySeconds { get; set; }
        public HashSet<ulong> ActiveBumpPairs { get; } = new HashSet<ulong>();
        public Dictionary<uint, MediaBlob> MediaMap { get; } = new Dictionary<uint, MediaBlob>();
        public uint RaceSnapshotSequence { get; set; }
        public uint RaceSnapshotTick { get; set; }
    }

}

