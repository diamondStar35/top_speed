using System;
using TopSpeed.Data;
using TopSpeed.Protocol;

namespace TopSpeed.Network
{
    internal static partial class ClientPacketSerializer
    {
        private const int PlayerDataFieldSize = 32;

        public static bool TryReadHeader(byte[] data, out Command command)
        {
            command = Command.Disconnect;
            if (data.Length < 2)
                return false;
            if (data[0] != ProtocolConstants.Version)
                return false;
            command = (Command)data[1];
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

        public static bool TryReadServerInfo(byte[] data, out PacketServerInfo packet)
        {
            packet = new PacketServerInfo();
            if (data.Length < 2 + ProtocolConstants.MaxMotdLength)
                return false;
            if (data[0] != ProtocolConstants.Version || data[1] != (byte)Command.ServerInfo)
                return false;
            var reader = new PacketReader(data);
            reader.ReadByte();
            reader.ReadByte();
            packet.Motd = reader.ReadFixedString(ProtocolConstants.MaxMotdLength);
            return true;
        }

        public static bool TryReadProtocolMessage(byte[] data, out PacketProtocolMessage packet)
        {
            packet = new PacketProtocolMessage();
            if (data.Length < 2 + 1 + ProtocolConstants.MaxProtocolMessageLength)
                return false;
            if (data[0] != ProtocolConstants.Version || data[1] != (byte)Command.ProtocolMessage)
                return false;
            var reader = new PacketReader(data);
            reader.ReadByte();
            reader.ReadByte();
            packet.Code = (ProtocolMessageCode)reader.ReadByte();
            packet.Message = reader.ReadFixedString(ProtocolConstants.MaxProtocolMessageLength);
            return true;
        }

        public static bool TryReadDisconnect(byte[] data, out string message)
        {
            message = string.Empty;
            if (data == null || data.Length < 2)
                return false;
            if (data[0] != ProtocolConstants.Version || data[1] != (byte)Command.Disconnect)
                return false;
            if (data.Length < 2 + ProtocolConstants.MaxProtocolDetailsLength)
                return false;

            var reader = new PacketReader(data);
            reader.ReadByte();
            reader.ReadByte();
            message = reader.ReadFixedString(ProtocolConstants.MaxProtocolDetailsLength);
            return true;
        }

        public static byte[] WritePlayerState(Command command, uint playerId, byte playerNumber, PlayerState state)
        {
            var buffer = WritePacketHeader(command, 4 + 1 + 1);
            var writer = new PacketWriter(buffer);
            writer.WriteByte(ProtocolConstants.Version);
            writer.WriteByte((byte)command);
            writer.WriteUInt32(playerId);
            writer.WriteByte(playerNumber);
            writer.WriteByte((byte)state);
            return buffer;
        }

        public static byte[] WriteRacePlayerState(Command command, uint raceInstanceId, uint playerId, byte playerNumber, PlayerState state)
        {
            var buffer = WritePacketHeader(command, 4 + 4 + 1 + 1);
            var writer = new PacketWriter(buffer);
            writer.WriteByte(ProtocolConstants.Version);
            writer.WriteByte((byte)command);
            writer.WriteUInt32(raceInstanceId);
            writer.WriteUInt32(playerId);
            writer.WriteByte(playerNumber);
            writer.WriteByte((byte)state);
            return buffer;
        }

        public static byte[] WritePlayer(Command command, uint playerId, byte playerNumber)
        {
            var buffer = WritePacketHeader(command, 4 + 1);
            var writer = new PacketWriter(buffer);
            writer.WriteByte(ProtocolConstants.Version);
            writer.WriteByte((byte)command);
            writer.WriteUInt32(playerId);
            writer.WriteByte(playerNumber);
            return buffer;
        }

        public static byte[] WriteRacePlayer(Command command, uint raceInstanceId, uint playerId, byte playerNumber)
        {
            var buffer = WritePacketHeader(command, 4 + 4 + 1);
            var writer = new PacketWriter(buffer);
            writer.WriteByte(ProtocolConstants.Version);
            writer.WriteByte((byte)command);
            writer.WriteUInt32(raceInstanceId);
            writer.WriteUInt32(playerId);
            writer.WriteByte(playerNumber);
            return buffer;
        }

        public static byte[] WriteGeneral(Command command)
        {
            return WritePacketHeader(command, 0);
        }

        private static byte[] WritePacketHeader(Command command, int payloadSize)
        {
            var buffer = new byte[2 + payloadSize];
            buffer[0] = ProtocolConstants.Version;
            buffer[1] = (byte)command;
            return buffer;
        }

        private static void ReadPlayerDataFields(ref PacketReader reader, PacketPlayerData packet)
        {
            packet.PlayerId = reader.ReadUInt32();
            packet.PlayerNumber = reader.ReadByte();
            packet.Car = (CarType)reader.ReadByte();
            packet.RaceData.PositionX = reader.ReadSingle();
            packet.RaceData.PositionY = reader.ReadSingle();
            packet.RaceData.Speed = reader.ReadUInt16();
            packet.RaceData.Frequency = reader.ReadInt32();
            packet.State = (PlayerState)reader.ReadByte();
            packet.EngineRunning = reader.ReadBool();
            packet.Braking = reader.ReadBool();
            packet.Horning = reader.ReadBool();
            packet.Backfiring = reader.ReadBool();
            packet.MediaLoaded = reader.ReadBool();
            packet.MediaPlaying = reader.ReadBool();
            packet.MediaId = reader.ReadUInt32();
            packet.RadioVolumePercent = reader.ReadByte();
        }
    }
}

