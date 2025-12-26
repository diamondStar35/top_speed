using System;
using TopSpeed.Data;

namespace TopSpeed.Protocol
{
    public struct PlayerRaceData
    {
        public int PositionX;
        public int PositionY;
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
    }

    public sealed class PacketPlayerBumped
    {
        public uint PlayerId;
        public byte PlayerNumber;
        public int BumpX;
        public int BumpY;
        public ushort BumpSpeed;
    }

    public sealed class PacketLoadCustomTrack
    {
        public byte NrOfLaps;
        public string TrackName = string.Empty;
        public TrackWeather TrackWeather;
        public TrackAmbience TrackAmbience;
        public ushort TrackLength;
        public TrackDefinition[] Definitions = Array.Empty<TrackDefinition>();
    }

    public sealed class PacketRaceResults
    {
        public byte NPlayers;
        public byte[] Results = Array.Empty<byte>();
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
}
