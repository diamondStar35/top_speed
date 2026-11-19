using System;
using TopSpeed.Protocol;

namespace TopSpeed.Network
{
    internal static partial class ClientPacketSerializer
    {
        public static bool TryReadPlayerVoiceStart(byte[] data, out PacketPlayerVoiceStart packet)
        {
            packet = new PacketPlayerVoiceStart();
            if (data.Length < 2 + 4 + 1 + 4 + 1 + 2 + 1 + 1 + 2 + 1)
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
            packet.FrequencyTenths = reader.ReadUInt16();
            packet.PushToTalk = reader.ReadBool();
            return true;
        }

        public static bool TryReadPlayerVoiceFrame(byte[] data, out PacketPlayerVoiceFrame packet)
        {
            packet = new PacketPlayerVoiceFrame();
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
            if (length == 0 || length > ProtocolConstants.MaxVoiceFrameBytes)
                return false;
            if (data.Length != 2 + 4 + 1 + 4 + 2 + 4 + 2 + length)
                return false;

            var bytes = new byte[length];
            for (var i = 0; i < length; i++)
                bytes[i] = reader.ReadByte();
            packet.Data = bytes;
            return true;
        }

        public static bool TryReadPlayerVoiceStop(byte[] data, out PacketPlayerVoiceStop packet)
        {
            packet = new PacketPlayerVoiceStop();
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

        public static byte[] WritePlayerVoiceStart(
            uint playerId,
            byte playerNumber,
            uint streamId,
            LiveCodec codec,
            ushort sampleRate,
            byte channels,
            byte frameMs,
            ushort frequencyTenths,
            bool pushToTalk)
        {
            var buffer = WritePacketHeader(Command.PlayerVoiceStart, 4 + 1 + 4 + 1 + 2 + 1 + 1 + 2 + 1);
            var writer = new PacketWriter(buffer);
            writer.WriteByte(ProtocolConstants.Version);
            writer.WriteByte((byte)Command.PlayerVoiceStart);
            writer.WriteUInt32(playerId);
            writer.WriteByte(playerNumber);
            writer.WriteUInt32(streamId);
            writer.WriteByte((byte)codec);
            writer.WriteUInt16(sampleRate);
            writer.WriteByte(channels);
            writer.WriteByte(frameMs);
            writer.WriteUInt16(frequencyTenths);
            writer.WriteBool(pushToTalk);
            return buffer;
        }

        public static byte[] WritePlayerVoiceFrame(
            uint playerId,
            byte playerNumber,
            uint streamId,
            ushort sequence,
            uint timestamp,
            byte[] data)
        {
            var bytes = data ?? Array.Empty<byte>();
            if (bytes.Length == 0 || bytes.Length > ProtocolConstants.MaxVoiceFrameBytes)
                throw new ArgumentOutOfRangeException(nameof(data), $"Voice frame cannot exceed {ProtocolConstants.MaxVoiceFrameBytes} bytes.");

            var buffer = WritePacketHeader(Command.PlayerVoiceFrame, 4 + 1 + 4 + 2 + 4 + 2 + bytes.Length);
            var writer = new PacketWriter(buffer);
            writer.WriteByte(ProtocolConstants.Version);
            writer.WriteByte((byte)Command.PlayerVoiceFrame);
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

        public static byte[] WritePlayerVoiceStop(uint playerId, byte playerNumber, uint streamId)
        {
            var buffer = WritePacketHeader(Command.PlayerVoiceStop, 4 + 1 + 4);
            var writer = new PacketWriter(buffer);
            writer.WriteByte(ProtocolConstants.Version);
            writer.WriteByte((byte)Command.PlayerVoiceStop);
            writer.WriteUInt32(playerId);
            writer.WriteByte(playerNumber);
            writer.WriteUInt32(streamId);
            return buffer;
        }
    }
}
