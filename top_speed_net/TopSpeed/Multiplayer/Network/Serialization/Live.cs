using System;
using TopSpeed.Protocol;

namespace TopSpeed.Network
{
    internal static partial class ClientPacketSerializer
    {
        public static bool TryReadPlayerLiveStart(byte[] data, out PacketPlayerLiveStart packet)
        {
            packet = new PacketPlayerLiveStart();
            if (data.Length < 2 + 4 + 1 + 4 + 1 + 2 + 1 + 1)
                return false;

            var reader = new PacketReader(data);
            reader.ReadByte();
            reader.ReadByte();
            packet.PlayerId = reader.ReadUInt32();
            packet.PlayerNumber = reader.ReadByte();
            packet.StreamId = reader.ReadUInt32();
            packet.Codec = (LiveCodec)reader.ReadByte();
            packet.SampleRate = reader.ReadUInt16();
            packet.Channels = reader.ReadByte();
            packet.FrameMs = reader.ReadByte();
            return true;
        }

        public static bool TryReadPlayerLiveFrame(byte[] data, out PacketPlayerLiveFrame packet)
        {
            packet = new PacketPlayerLiveFrame();
            if (data.Length < 2 + 4 + 1 + 4 + 2 + 4 + 2)
                return false;

            var reader = new PacketReader(data);
            reader.ReadByte();
            reader.ReadByte();
            packet.PlayerId = reader.ReadUInt32();
            packet.PlayerNumber = reader.ReadByte();
            packet.StreamId = reader.ReadUInt32();
            packet.Sequence = reader.ReadUInt16();
            packet.Timestamp = reader.ReadUInt32();
            var length = reader.ReadUInt16();
            if (length == 0 || length > ProtocolConstants.MaxLiveFrameBytes)
                return false;
            if (data.Length != 2 + 4 + 1 + 4 + 2 + 4 + 2 + length)
                return false;

            var bytes = new byte[length];
            for (var i = 0; i < length; i++)
                bytes[i] = reader.ReadByte();
            packet.Data = bytes;
            return true;
        }

        public static bool TryReadPlayerLiveStop(byte[] data, out PacketPlayerLiveStop packet)
        {
            packet = new PacketPlayerLiveStop();
            if (data.Length < 2 + 4 + 1 + 4)
                return false;

            var reader = new PacketReader(data);
            reader.ReadByte();
            reader.ReadByte();
            packet.PlayerId = reader.ReadUInt32();
            packet.PlayerNumber = reader.ReadByte();
            packet.StreamId = reader.ReadUInt32();
            return true;
        }

        public static byte[] WritePlayerLiveStart(
            uint playerId,
            byte playerNumber,
            uint streamId,
            LiveCodec codec,
            ushort sampleRate,
            byte channels,
            byte frameMs)
        {
            var buffer = WritePacketHeader(Command.PlayerLiveStart, 4 + 1 + 4 + 1 + 2 + 1 + 1);
            var writer = new PacketWriter(buffer);
            writer.WriteByte(ProtocolConstants.Version);
            writer.WriteByte((byte)Command.PlayerLiveStart);
            writer.WriteUInt32(playerId);
            writer.WriteByte(playerNumber);
            writer.WriteUInt32(streamId);
            writer.WriteByte((byte)codec);
            writer.WriteUInt16(sampleRate);
            writer.WriteByte(channels);
            writer.WriteByte(frameMs);
            return buffer;
        }

        public static byte[] WritePlayerLiveFrame(
            uint playerId,
            byte playerNumber,
            uint streamId,
            ushort sequence,
            uint timestamp,
            byte[] data)
        {
            var bytes = data ?? Array.Empty<byte>();
            if (bytes.Length == 0 || bytes.Length > ProtocolConstants.MaxLiveFrameBytes)
                throw new ArgumentOutOfRangeException(nameof(data), $"Live frame cannot exceed {ProtocolConstants.MaxLiveFrameBytes} bytes.");

            var buffer = WritePacketHeader(Command.PlayerLiveFrame, 4 + 1 + 4 + 2 + 4 + 2 + bytes.Length);
            var writer = new PacketWriter(buffer);
            writer.WriteByte(ProtocolConstants.Version);
            writer.WriteByte((byte)Command.PlayerLiveFrame);
            writer.WriteUInt32(playerId);
            writer.WriteByte(playerNumber);
            writer.WriteUInt32(streamId);
            writer.WriteUInt16(sequence);
            writer.WriteUInt32(timestamp);
            writer.WriteUInt16((ushort)bytes.Length);
            for (var i = 0; i < bytes.Length; i++)
                writer.WriteByte(bytes[i]);
            return buffer;
        }

        public static byte[] WritePlayerLiveStop(uint playerId, byte playerNumber, uint streamId)
        {
            var buffer = WritePacketHeader(Command.PlayerLiveStop, 4 + 1 + 4);
            var writer = new PacketWriter(buffer);
            writer.WriteByte(ProtocolConstants.Version);
            writer.WriteByte((byte)Command.PlayerLiveStop);
            writer.WriteUInt32(playerId);
            writer.WriteByte(playerNumber);
            writer.WriteUInt32(streamId);
            return buffer;
        }
    }
}

