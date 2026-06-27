using System;
using System.Collections.Generic;
using TopSpeed.Data;

namespace TopSpeed.Protocol
{
    public struct PlayerRaceData
    {
        public float PositionX;
        public float PositionY;
        public ushort Speed;
        public int Frequency;
    }

    public sealed class PacketHeader
    {
        public byte Version;
        public Command Command;
    }

    public sealed class PacketPlayer
    {
        public uint PlayerId;
        public byte PlayerNumber;
    }

    public sealed class PacketPlayerHello
    {
        public string Name = string.Empty;
    }

    public sealed class PacketPlayerState
    {
        public uint PlayerId;
        public byte PlayerNumber;
        public PlayerState State;
    }

    public sealed class PacketRacePlayer
    {
        public uint RaceInstanceId;
        public uint PlayerId;
        public byte PlayerNumber;
    }

    public sealed class PacketRacePlayerState
    {
        public uint RaceInstanceId;
        public uint PlayerId;
        public byte PlayerNumber;
        public PlayerState State;
    }

    public sealed class PacketRacePlayerData
    {
        public uint RaceInstanceId;
        public uint PlayerId;
        public byte PlayerNumber;
        public CarType Car;
        public PlayerRaceData RaceData;
        public PlayerState State;
        public bool EngineRunning;
        public bool Braking;
        public bool Horning;
        public bool Backfiring;
        public bool MediaLoaded;
        public bool MediaPlaying;
        public uint MediaId;
        public byte RadioVolumePercent;
    }

    public sealed class PacketPlayerData
    {
        public uint PlayerId;
        public byte PlayerNumber;
        public CarType Car;
        public PlayerRaceData RaceData;
        public PlayerState State;
        public bool EngineRunning;
        public bool Braking;
        public bool Horning;
        public bool Backfiring;
        public bool MediaLoaded;
        public bool MediaPlaying;
        public uint MediaId;
        public byte RadioVolumePercent;
    }

    public sealed class PacketRaceSnapshot
    {
        public uint Sequence;
        public uint Tick;
        public PacketPlayerData[] Players = Array.Empty<PacketPlayerData>();
    }

    public sealed class PacketPlayerMediaBegin
    {
        public uint PlayerId;
        public byte PlayerNumber;
        public uint MediaId;
        public uint TransferId;
        public uint TotalBytes;
        public string FileExtension = string.Empty;
        public ushort FrequencyTenths;
    }

    public sealed class PacketPlayerMediaChunk
    {
        public uint PlayerId;
        public byte PlayerNumber;
        public uint MediaId;
        public uint TransferId;
        public ushort ChunkIndex;
        public byte[] Data = Array.Empty<byte>();
    }

    public sealed class PacketPlayerMediaEnd
    {
        public uint PlayerId;
        public byte PlayerNumber;
        public uint MediaId;
        public uint TransferId;
    }

    public sealed class PacketPlayerCommunicatorMediaState
    {
        public uint PlayerId;
        public byte PlayerNumber;
        public uint MediaId;
        public ushort FrequencyTenths;
        public bool MediaLoaded;
        public bool MediaPlaying;
        public byte VolumePercent;
    }

    public sealed class PacketPlayerBumped
    {
        public uint PlayerId;
        public byte PlayerNumber;
        public float BumpX;
        public float BumpY;
        public float SpeedDeltaKph;
    }

    public sealed class PacketLoadCustomTrack
    {
        public byte NrOfLaps;
        public string TrackName = string.Empty;
        public TrackAmbience TrackAmbience;
        public string DefaultWeatherProfileId = TrackWeatherProfile.DefaultProfileId;
        public IReadOnlyDictionary<string, TrackWeatherProfile> WeatherProfiles = new Dictionary<string, TrackWeatherProfile>(StringComparer.OrdinalIgnoreCase);
        public ushort TrackLength;
        public TrackDefinition[] Definitions = Array.Empty<TrackDefinition>();
    }

    public sealed class PacketRaceResults
    {
        public byte NPlayers;
        public PacketRaceResultEntry[] Results = Array.Empty<PacketRaceResultEntry>();
    }

    public sealed class PacketRaceResultEntry
    {
        public byte PlayerNumber;
        public int TimeMs;
    }

    public sealed class PacketRoomRaceResultEntry
    {
        public uint PlayerId;
        public byte PlayerNumber;
        public byte FinishOrder;
        public int TimeMs;
        public RoomRaceResultStatus Status;
    }

    public sealed class PacketServerInfo
    {
        public string Motd = string.Empty;
    }

    public sealed class PacketPlayerJoined
    {
        public uint PlayerId;
        public byte PlayerNumber;
        public string Name = string.Empty;
    }

    public sealed class PacketRoomSummary
    {
        public uint RoomId;
        public string RoomName = string.Empty;
        public GameRoomType RoomType;
        public byte PlayerCount;
        public byte PlayersToStart;
        public RoomRaceState RaceState;
        public string TrackName = string.Empty;
        public TrackPackageRef Track = new TrackPackageRef();
    }

    public sealed class PacketRoomList
    {
        public PacketRoomSummary[] Rooms = Array.Empty<PacketRoomSummary>();
    }

    public sealed class PacketRoomCreate
    {
        public string RoomName = string.Empty;
        public GameRoomType RoomType;
        public byte PlayersToStart;
    }

    public sealed class PacketRoomJoin
    {
        public uint RoomId;
    }

    public sealed class PacketRoomGetRequest
    {
        public uint RoomId;
    }

    public sealed class PacketRoomSetTrack
    {
        public TrackPackageRef Track = new TrackPackageRef();
    }

    public sealed class PacketRoomSetLaps
    {
        public byte Laps;
    }

    public sealed class PacketRoomSetPlayersToStart
    {
        public byte PlayersToStart;
    }

    public sealed class PacketRoomSetGameRules
    {
        public uint GameRulesFlags;
    }

    public sealed class PacketRoomPlayerReady
    {
        public CarType Car;
        public bool AutomaticTransmission;
    }

    public sealed class PacketRoomRaceControl
    {
        public RoomRaceControlAction Action;
    }

    public sealed class PacketRoomPlayer
    {
        public uint PlayerId;
        public byte PlayerNumber;
        public PlayerState State;
        public string Name = string.Empty;
    }

    public sealed class PacketRoomState
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
        public bool InRoom;
        public bool IsHost;
        public bool RacePaused;
        public string TrackName = string.Empty;
        public TrackPackageRef Track = new TrackPackageRef();
        public byte Laps;
        public uint GameRulesFlags;
        public PacketRoomPlayer[] Players = Array.Empty<PacketRoomPlayer>();
    }

    public sealed class PacketRoomGet
    {
        public bool Found;
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
        public string TrackName = string.Empty;
        public TrackPackageRef Track = new TrackPackageRef();
        public byte Laps;
        public uint GameRulesFlags;
        public PacketRoomPlayer[] Players = Array.Empty<PacketRoomPlayer>();
    }

    public sealed class PacketRoomEvent
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
        public TrackPackageRef Track = new TrackPackageRef();
        public byte Laps;
        public uint GameRulesFlags;
        public string RoomName = string.Empty;
        public uint SubjectPlayerId;
        public byte SubjectPlayerNumber;
        public PlayerState SubjectPlayerState;
        public string SubjectPlayerName = string.Empty;
    }

    public sealed class PacketRoomRaceStateChanged
    {
        public uint RoomId;
        public uint RoomVersion;
        public uint EventSequence;
        public uint RaceInstanceId;
        public RoomRaceState State;
        // Effective RoomGameRules for this race instance: the room's persistent rules minus any
        // per-race disable the host chose (e.g. turning fuel/tire off for a pit-less track). Clients
        // build their car from this so the override never changes the persistent room rules.
        public uint EffectiveGameRulesFlags;
    }

    public sealed class PacketRoomRacePlayerFinished
    {
        public uint RoomId;
        public uint RoomVersion;
        public uint EventSequence;
        public uint RaceInstanceId;
        public uint PlayerId;
        public byte PlayerNumber;
        public byte FinishOrder;
        public int TimeMs;
    }

    public sealed class PacketRoomRaceCompleted
    {
        public uint RoomId;
        public uint RoomVersion;
        public uint EventSequence;
        public uint RaceInstanceId;
        public PacketRoomRaceResultEntry[] Results = Array.Empty<PacketRoomRaceResultEntry>();
    }

    public sealed class PacketRoomRaceAborted
    {
        public uint RoomId;
        public uint RoomVersion;
        public uint EventSequence;
        public uint RaceInstanceId;
        public RoomRaceAbortReason Reason;
    }

    public sealed class PacketOnlinePlayer
    {
        public uint PlayerId;
        public byte PlayerNumber;
        public OnlinePresenceState PresenceState;
        public string Name = string.Empty;
        public string RoomName = string.Empty;
    }

    public sealed class PacketOnlinePlayers
    {
        public PacketOnlinePlayer[] Players = Array.Empty<PacketOnlinePlayer>();
    }

    public sealed class PacketProtocolMessage
    {
        public ProtocolMessageCode Code;
        public string Message = string.Empty;
    }

    public sealed class PacketProtocolHello
    {
        public ProtocolVer ClientVersion;
        public ProtocolVer MinSupported;
        public ProtocolVer MaxSupported;
        public uint ResumePlayerId;
        public ulong ResumeToken;
    }

    public sealed class PacketProtocolWelcome
    {
        public ProtocolCompatStatus Status;
        public ProtocolVer NegotiatedVersion;
        public ProtocolVer ServerMinSupported;
        public ProtocolVer ServerMaxSupported;
        public ulong ResumeToken;
        public string Message = string.Empty;
    }

    public sealed class PacketClientHeartbeat
    {
        public uint PlayerId;
        public ulong SessionId;
        public uint ClientTick;
        public uint LastReceivedServerTick;
    }

    public sealed class PacketServerHeartbeat
    {
        public uint ServerTick;
        public uint LastReceivedClientTick;
    }
}
