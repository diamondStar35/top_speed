using System;
using System.Collections.Generic;
using TopSpeed.Data;
using TopSpeed.Protocol;

namespace TopSpeed.Network
{
    internal static partial class ClientPacketSerializer
    {
        public static bool TryReadRaceSnapshot(byte[] data, out PacketRaceSnapshot packet)
        {
            packet = new PacketRaceSnapshot();
            if (data.Length < 2 + 4 + 4 + 1)
                return false;
            if (data[0] != ProtocolConstants.Version || data[1] != (byte)Command.RaceSnapshot)
                return false;

            var reader = new PacketReader(data);
            reader.ReadByte();
            reader.ReadByte();
            packet.Sequence = reader.ReadUInt32();
            packet.Tick = reader.ReadUInt32();
            var count = reader.ReadByte();
            var available = Math.Max(0, (data.Length - (2 + 4 + 4 + 1)) / PlayerDataFieldSize);
            var actualCount = Math.Min(count, available);
            var players = new PacketPlayerData[actualCount];
            for (var i = 0; i < actualCount; i++)
            {
                var item = new PacketPlayerData();
                ReadPlayerDataFields(ref reader, item);
                players[i] = item;
            }
            packet.Players = players;
            return true;
        }

        public static bool TryReadPlayerBumped(byte[] data, out PacketPlayerBumped packet)
        {
            packet = new PacketPlayerBumped();
            if (data.Length < 2 + 4 + 1 + 4 + 4 + 4)
                return false;
            var reader = new PacketReader(data);
            reader.ReadByte();
            reader.ReadByte();
            packet.PlayerId = reader.ReadUInt32();
            packet.PlayerNumber = reader.ReadByte();
            packet.BumpX = reader.ReadSingle();
            packet.BumpY = reader.ReadSingle();
            packet.SpeedDeltaKph = reader.ReadSingle();
            return true;
        }

        public static bool TryReadLoadCustomTrack(byte[] data, out PacketLoadCustomTrack packet)
        {
            packet = new PacketLoadCustomTrack();
            const int headerSize = 2;
            const int baseSize = 4 + 12 + 1 + 2;
            if (data.Length < headerSize + baseSize)
                return false;
            if (data[0] != ProtocolConstants.Version || data[1] != (byte)Command.LoadCustomTrack)
                return false;
            try
            {
                var reader = new PacketReader(data);
                reader.ReadByte();
                reader.ReadByte();
                packet.NrOfLaps = reader.ReadInt32();
                packet.TrackName = reader.ReadFixedString(12);
                packet.TrackAmbience = (TrackAmbience)reader.ReadByte();
                packet.TrackLength = reader.ReadUInt16();
                packet.DefaultWeatherProfileId = reader.ReadString16();
                var profileCount = reader.ReadByte();
                var weatherProfiles = new Dictionary<string, TrackWeatherProfile>(StringComparer.OrdinalIgnoreCase);
                for (var i = 0; i < profileCount; i++)
                {
                    var profile = ReadWeatherProfile(ref reader);
                    weatherProfiles[profile.Id] = profile;
                }

                var definitionCount = packet.TrackLength;
                var definitions = new TrackDefinition[definitionCount];
                for (var i = 0; i < definitionCount; i++)
                {
                    var type = (TrackType)reader.ReadByte();
                    var surface = (TrackSurface)reader.ReadByte();
                    var noise = (TrackNoise)reader.ReadByte();
                    var segmentLength = reader.ReadSingle();
                    var weatherProfileId = reader.ReadString16();
                    var transitionSeconds = reader.ReadSingle();
                    definitions[i] = new TrackDefinition(
                        type,
                        surface,
                        noise,
                        segmentLength,
                        segmentId: null,
                        width: 0f,
                        height: 0f,
                        weatherProfileId: string.IsNullOrWhiteSpace(weatherProfileId) ? null : weatherProfileId,
                        weatherTransitionSeconds: transitionSeconds,
                        roomId: null,
                        roomOverrides: null,
                        soundSourceIds: null,
                        metadata: null);
                }

                packet.WeatherProfiles = weatherProfiles;
                packet.Definitions = definitions;
                return true;
            }
            catch (ArgumentOutOfRangeException)
            {
                packet = new PacketLoadCustomTrack();
                return false;
            }
            catch (ArgumentException)
            {
                packet = new PacketLoadCustomTrack();
                return false;
            }
            catch (IndexOutOfRangeException)
            {
                packet = new PacketLoadCustomTrack();
                return false;
            }
        }

        private static TrackWeatherProfile ReadWeatherProfile(ref PacketReader reader)
        {
            var id = reader.ReadString16();
            var kind = (TrackWeather)reader.ReadByte();
            return new TrackWeatherProfile(
                id,
                kind,
                reader.ReadSingle(),
                reader.ReadSingle(),
                reader.ReadSingle(),
                reader.ReadSingle(),
                reader.ReadSingle(),
                reader.ReadSingle(),
                reader.ReadSingle(),
                reader.ReadSingle(),
                reader.ReadSingle(),
                reader.ReadSingle(),
                reader.ReadSingle());
        }

        public static bool TryReadRaceResults(byte[] data, out PacketRaceResults packet)
        {
            packet = new PacketRaceResults();
            if (data.Length < 2 + 1)
                return false;
            if (data[0] != ProtocolConstants.Version || data[1] != (byte)Command.StopRace)
                return false;
            var reader = new PacketReader(data);
            reader.ReadByte();
            reader.ReadByte();
            var count = reader.ReadByte();
            var availableEntries = Math.Max(0, (data.Length - 3) / 5);
            var max = Math.Min(count, (byte)availableEntries);
            var results = new PacketRaceResultEntry[max];
            for (var i = 0; i < max; i++)
            {
                results[i] = new PacketRaceResultEntry
                {
                    PlayerNumber = reader.ReadByte(),
                    TimeMs = reader.ReadInt32()
                };
            }
            packet.Results = results;
            packet.NPlayers = max;
            return true;
        }

        public static byte[] WriteRacePlayerDataToServer(
            uint raceInstanceId,
            uint playerId,
            byte playerNumber,
            CarType car,
            PlayerRaceData raceData,
            PlayerState state,
            bool engineRunning,
            bool braking,
            bool horning,
            bool backfiring,
            bool mediaLoaded,
            bool mediaPlaying,
            uint mediaId,
            byte radioVolumePercent)
        {
            var buffer = WritePacketHeader(Command.PlayerDataToServer, 4 + 4 + 1 + 1 + 4 + 4 + 2 + 4 + 1 + 1 + 1 + 1 + 1 + 1 + 1 + 4 + 1);
            var writer = new PacketWriter(buffer);
            writer.WriteByte(ProtocolConstants.Version);
            writer.WriteByte((byte)Command.PlayerDataToServer);
            writer.WriteUInt32(raceInstanceId);
            writer.WriteUInt32(playerId);
            writer.WriteByte(playerNumber);
            writer.WriteByte((byte)car);
            writer.WriteSingle(raceData.PositionX);
            writer.WriteSingle(raceData.PositionY);
            writer.WriteUInt16(raceData.Speed);
            writer.WriteInt32(raceData.Frequency);
            writer.WriteByte((byte)state);
            writer.WriteBool(engineRunning);
            writer.WriteBool(braking);
            writer.WriteBool(horning);
            writer.WriteBool(backfiring);
            writer.WriteBool(mediaLoaded);
            writer.WriteBool(mediaPlaying);
            writer.WriteUInt32(mediaId);
            writer.WriteByte(radioVolumePercent);
            return buffer;
        }
    }
}

