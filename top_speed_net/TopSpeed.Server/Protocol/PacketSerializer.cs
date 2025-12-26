using System;
using TopSpeed.Data;
using TopSpeed.Protocol;

namespace TopSpeed.Server.Protocol
{
    internal static class PacketSerializer
    {
        public static bool TryReadHeader(byte[] data, out PacketHeader header)
        {
            header = new PacketHeader();
            if (data.Length < 2)
                return false;
            header.Version = data[0];
            header.Command = (Command)data[1];
            return true;
        }

        public static bool TryReadPlayerState(byte[] data, out PacketPlayerState packet)
        {
            packet = new PacketPlayerState();
            if (data.Length < 2 + 4 + 1 + 1)
                return false;
            var reader = new PacketReader(data);
            reader.ReadByte();
            reader.ReadByte();
            packet.PlayerId = reader.ReadUInt32();
            packet.PlayerNumber = reader.ReadByte();
            packet.State = (PlayerState)reader.ReadByte();
            return true;
        }

        public static bool TryReadPlayer(byte[] data, out PacketPlayer packet)
        {
            packet = new PacketPlayer();
            if (data.Length < 2 + 4 + 1)
                return false;
            var reader = new PacketReader(data);
            reader.ReadByte();
            reader.ReadByte();
            packet.PlayerId = reader.ReadUInt32();
            packet.PlayerNumber = reader.ReadByte();
            return true;
        }

        public static bool TryReadPlayerHello(byte[] data, out PacketPlayerHello packet)
        {
            packet = new PacketPlayerHello();
            if (data.Length < 2 + ProtocolConstants.MaxPlayerNameLength)
                return false;
            var reader = new PacketReader(data);
            reader.ReadByte();
            reader.ReadByte();
            packet.Name = reader.ReadFixedString(ProtocolConstants.MaxPlayerNameLength);
            return true;
        }

        public static bool TryReadPlayerData(byte[] data, out PacketPlayerData packet)
        {
            packet = new PacketPlayerData();
            if (data.Length < 2 + 4 + 1 + 1 + 4 + 4 + 2 + 4 + 1 + 1 + 1 + 1 + 1)
                return false;
            var reader = new PacketReader(data);
            reader.ReadByte();
            reader.ReadByte();
            packet.PlayerId = reader.ReadUInt32();
            packet.PlayerNumber = reader.ReadByte();
            packet.Car = (CarType)reader.ReadByte();
            packet.RaceData.PositionX = reader.ReadInt32();
            packet.RaceData.PositionY = reader.ReadInt32();
            packet.RaceData.Speed = reader.ReadUInt16();
            packet.RaceData.Frequency = reader.ReadInt32();
            packet.State = (PlayerState)reader.ReadByte();
            packet.EngineRunning = reader.ReadBool();
            packet.Braking = reader.ReadBool();
            packet.Horning = reader.ReadBool();
            packet.Backfiring = reader.ReadBool();
            return true;
        }

        public static byte[] WritePacketHeader(Command command, int payloadSize)
        {
            var buffer = new byte[2 + payloadSize];
            buffer[0] = ProtocolConstants.Version;
            buffer[1] = (byte)command;
            return buffer;
        }

        public static byte[] WritePlayerNumber(uint id, byte playerNumber)
        {
            return WritePlayer(Command.PlayerNumber, id, playerNumber);
        }

        public static byte[] WritePlayer(Command command, uint id, byte playerNumber)
        {
            var buffer = WritePacketHeader(command, 4 + 1);
            var writer = new PacketWriter(buffer);
            writer.WriteByte(ProtocolConstants.Version);
            writer.WriteByte((byte)command);
            writer.WriteUInt32(id);
            writer.WriteByte(playerNumber);
            return buffer;
        }

        public static byte[] WritePlayerState(Command command, uint id, byte playerNumber, PlayerState state)
        {
            var buffer = WritePacketHeader(command, 4 + 1 + 1);
            var writer = new PacketWriter(buffer);
            writer.WriteByte(ProtocolConstants.Version);
            writer.WriteByte((byte)command);
            writer.WriteUInt32(id);
            writer.WriteByte(playerNumber);
            writer.WriteByte((byte)state);
            return buffer;
        }

        public static byte[] WritePlayerData(PacketPlayerData data)
        {
            var buffer = WritePacketHeader(Command.PlayerData, 4 + 1 + 1 + 4 + 4 + 2 + 4 + 1 + 1 + 1 + 1 + 1);
            var writer = new PacketWriter(buffer);
            writer.WriteByte(ProtocolConstants.Version);
            writer.WriteByte((byte)Command.PlayerData);
            writer.WriteUInt32(data.PlayerId);
            writer.WriteByte(data.PlayerNumber);
            writer.WriteByte((byte)data.Car);
            writer.WriteInt32(data.RaceData.PositionX);
            writer.WriteInt32(data.RaceData.PositionY);
            writer.WriteUInt16(data.RaceData.Speed);
            writer.WriteInt32(data.RaceData.Frequency);
            writer.WriteByte((byte)data.State);
            writer.WriteBool(data.EngineRunning);
            writer.WriteBool(data.Braking);
            writer.WriteBool(data.Horning);
            writer.WriteBool(data.Backfiring);
            return buffer;
        }

        public static byte[] WritePlayerBumped(PacketPlayerBumped bump)
        {
            var buffer = WritePacketHeader(Command.PlayerBumped, 4 + 1 + 4 + 4 + 2);
            var writer = new PacketWriter(buffer);
            writer.WriteByte(ProtocolConstants.Version);
            writer.WriteByte((byte)Command.PlayerBumped);
            writer.WriteUInt32(bump.PlayerId);
            writer.WriteByte(bump.PlayerNumber);
            writer.WriteInt32(bump.BumpX);
            writer.WriteInt32(bump.BumpY);
            writer.WriteUInt16(bump.BumpSpeed);
            return buffer;
        }

        public static byte[] WriteLoadCustomTrack(PacketLoadCustomTrack track)
        {
            var maxLength = Math.Min(track.TrackLength, (ushort)ProtocolConstants.MaxMultiTrackLength);
            var definitionCount = Math.Min(track.Definitions.Length, maxLength);
            var payload = 1 + 12 + 1 + 1 + 2 + (definitionCount * (1 + 1 + 1 + 4));
            var buffer = WritePacketHeader(Command.LoadCustomTrack, payload);
            var writer = new PacketWriter(buffer);
            writer.WriteByte(ProtocolConstants.Version);
            writer.WriteByte((byte)Command.LoadCustomTrack);
            writer.WriteByte(track.NrOfLaps);
            writer.WriteFixedString(track.TrackName, 12);
            writer.WriteByte((byte)track.TrackWeather);
            writer.WriteByte((byte)track.TrackAmbience);
            writer.WriteUInt16(maxLength);
            for (var i = 0; i < definitionCount; i++)
            {
                var def = track.Definitions[i];
                writer.WriteByte((byte)def.Type);
                writer.WriteByte((byte)def.Surface);
                writer.WriteByte((byte)def.Noise);
                writer.WriteUInt32((uint)def.Length);
            }
            return buffer;
        }

        public static byte[] WriteRaceResults(PacketRaceResults results)
        {
            var count = Math.Min(results.Results.Length, ProtocolConstants.MaxPlayers);
            var payload = 1 + count;
            var buffer = WritePacketHeader(Command.StopRace, payload);
            var writer = new PacketWriter(buffer);
            writer.WriteByte(ProtocolConstants.Version);
            writer.WriteByte((byte)Command.StopRace);
            writer.WriteByte((byte)count);
            for (var i = 0; i < count; i++)
                writer.WriteByte(results.Results[i]);
            return buffer;
        }

        public static byte[] WriteServerInfo(PacketServerInfo info)
        {
            var buffer = WritePacketHeader(Command.ServerInfo, ProtocolConstants.MaxMotdLength);
            var writer = new PacketWriter(buffer);
            writer.WriteByte(ProtocolConstants.Version);
            writer.WriteByte((byte)Command.ServerInfo);
            writer.WriteFixedString(info.Motd ?? string.Empty, ProtocolConstants.MaxMotdLength);
            return buffer;
        }

        public static byte[] WritePlayerJoined(PacketPlayerJoined joined)
        {
            var buffer = WritePacketHeader(Command.PlayerJoined, 4 + 1 + ProtocolConstants.MaxPlayerNameLength);
            var writer = new PacketWriter(buffer);
            writer.WriteByte(ProtocolConstants.Version);
            writer.WriteByte((byte)Command.PlayerJoined);
            writer.WriteUInt32(joined.PlayerId);
            writer.WriteByte(joined.PlayerNumber);
            writer.WriteFixedString(joined.Name ?? string.Empty, ProtocolConstants.MaxPlayerNameLength);
            return buffer;
        }

        public static byte[] WriteGeneral(Command command)
        {
            var buffer = WritePacketHeader(command, 0);
            return buffer;
        }
    }
}
