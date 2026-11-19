using System;
using TopSpeed.Protocol;

namespace TopSpeed.Network
{
    internal static partial class ClientPacketSerializer
    {
        public static bool TryReadPlayerMediaBegin(byte[] data, out PacketPlayerMediaBegin packet)
        {
            return TryReadPlayerMediaBeginCore(data, out packet, expectsFrequency: false);
        }

        public static bool TryReadPlayerCommunicatorMediaBegin(byte[] data, out PacketPlayerMediaBegin packet)
        {
            return TryReadPlayerMediaBeginCore(data, out packet, expectsFrequency: true);
        }

        public static bool TryReadPlayerMediaChunk(byte[] data, out PacketPlayerMediaChunk packet)
        {
            packet = new PacketPlayerMediaChunk();
            if (data.Length < 2 + 4 + 1 + 4 + 4 + 2 + 2)
                return false;
            var reader = new PacketReader(data);
            reader.ReadByte();
            reader.ReadByte();
            packet.PlayerId = reader.ReadUInt32();
            packet.PlayerNumber = reader.ReadByte();
            packet.MediaId = reader.ReadUInt32();
            packet.TransferId = reader.ReadUInt32();
            packet.ChunkIndex = reader.ReadUInt16();
            var length = reader.ReadUInt16();
            if (length > ProtocolConstants.MaxMediaChunkBytes)
                return false;
            if (data.Length != 2 + 4 + 1 + 4 + 4 + 2 + 2 + length)
                return false;
            var bytes = new byte[length];
            for (var i = 0; i < length; i++)
                bytes[i] = reader.ReadByte();
            packet.Data = bytes;
            return true;
        }

        public static bool TryReadPlayerCommunicatorMediaChunk(byte[] data, out PacketPlayerMediaChunk packet)
        {
            return TryReadPlayerMediaChunk(data, out packet);
        }

        public static bool TryReadPlayerMediaEnd(byte[] data, out PacketPlayerMediaEnd packet)
        {
            packet = new PacketPlayerMediaEnd();
            if (data.Length < 2 + 4 + 1 + 4 + 4)
                return false;
            var reader = new PacketReader(data);
            reader.ReadByte();
            reader.ReadByte();
            packet.PlayerId = reader.ReadUInt32();
            packet.PlayerNumber = reader.ReadByte();
            packet.MediaId = reader.ReadUInt32();
            packet.TransferId = reader.ReadUInt32();
            return true;
        }

        public static bool TryReadPlayerCommunicatorMediaEnd(byte[] data, out PacketPlayerMediaEnd packet)
        {
            return TryReadPlayerMediaEnd(data, out packet);
        }

        public static bool TryReadPlayerCommunicatorMediaState(byte[] data, out PacketPlayerCommunicatorMediaState packet)
        {
            packet = new PacketPlayerCommunicatorMediaState();
            if (data.Length < 2 + 4 + 1 + 4 + 2 + 1 + 1 + 1)
                return false;
            var reader = new PacketReader(data);
            reader.ReadByte();
            reader.ReadByte();
            packet.PlayerId = reader.ReadUInt32();
            packet.PlayerNumber = reader.ReadByte();
            packet.MediaId = reader.ReadUInt32();
            packet.FrequencyTenths = reader.ReadUInt16();
            packet.MediaLoaded = reader.ReadBool();
            packet.MediaPlaying = reader.ReadBool();
            packet.VolumePercent = reader.ReadByte();
            return true;
        }

        public static byte[] WritePlayerMediaBegin(uint playerId, byte playerNumber, uint mediaId, uint totalBytes, string fileExtension)
        {
            return WritePlayerMediaBegin(playerId, playerNumber, mediaId, mediaId, totalBytes, fileExtension);
        }

        public static byte[] WritePlayerMediaBegin(uint playerId, byte playerNumber, uint mediaId, uint transferId, uint totalBytes, string fileExtension)
        {
            return WritePlayerMediaBeginCore(
                Command.PlayerMediaBegin,
                playerId,
                playerNumber,
                mediaId,
                transferId,
                totalBytes,
                fileExtension,
                includeFrequency: false,
                frequencyTenths: 0);
        }

        public static byte[] WritePlayerCommunicatorMediaBegin(
            uint playerId,
            byte playerNumber,
            uint mediaId,
            uint transferId,
            uint totalBytes,
            string fileExtension,
            ushort frequencyTenths)
        {
            return WritePlayerMediaBeginCore(
                Command.PlayerCommunicatorMediaBegin,
                playerId,
                playerNumber,
                mediaId,
                transferId,
                totalBytes,
                fileExtension,
                includeFrequency: true,
                frequencyTenths: frequencyTenths);
        }

        public static byte[] WritePlayerMediaChunk(uint playerId, byte playerNumber, uint mediaId, ushort chunkIndex, byte[] data)
        {
            return WritePlayerMediaChunk(playerId, playerNumber, mediaId, mediaId, chunkIndex, data);
        }

        public static byte[] WritePlayerMediaChunk(uint playerId, byte playerNumber, uint mediaId, uint transferId, ushort chunkIndex, byte[] data)
        {
            return WritePlayerMediaChunkCore(
                Command.PlayerMediaChunk,
                playerId,
                playerNumber,
                mediaId,
                transferId,
                chunkIndex,
                data);
        }

        public static byte[] WritePlayerCommunicatorMediaChunk(
            uint playerId,
            byte playerNumber,
            uint mediaId,
            uint transferId,
            ushort chunkIndex,
            byte[] data)
        {
            return WritePlayerMediaChunkCore(
                Command.PlayerCommunicatorMediaChunk,
                playerId,
                playerNumber,
                mediaId,
                transferId,
                chunkIndex,
                data);
        }

        public static byte[] WritePlayerMediaEnd(uint playerId, byte playerNumber, uint mediaId)
        {
            return WritePlayerMediaEnd(playerId, playerNumber, mediaId, mediaId);
        }

        public static byte[] WritePlayerMediaEnd(uint playerId, byte playerNumber, uint mediaId, uint transferId)
        {
            return WritePlayerMediaEndCore(
                Command.PlayerMediaEnd,
                playerId,
                playerNumber,
                mediaId,
                transferId);
        }

        public static byte[] WritePlayerCommunicatorMediaEnd(
            uint playerId,
            byte playerNumber,
            uint mediaId,
            uint transferId)
        {
            return WritePlayerMediaEndCore(
                Command.PlayerCommunicatorMediaEnd,
                playerId,
                playerNumber,
                mediaId,
                transferId);
        }

        public static byte[] WritePlayerCommunicatorMediaState(PacketPlayerCommunicatorMediaState state)
        {
            var packet = state ?? new PacketPlayerCommunicatorMediaState();
            var buffer = WritePacketHeader(Command.PlayerCommunicatorMediaState, 4 + 1 + 4 + 2 + 1 + 1 + 1);
            var writer = new PacketWriter(buffer);
            writer.WriteByte(ProtocolConstants.Version);
            writer.WriteByte((byte)Command.PlayerCommunicatorMediaState);
            writer.WriteUInt32(packet.PlayerId);
            writer.WriteByte(packet.PlayerNumber);
            writer.WriteUInt32(packet.MediaId);
            writer.WriteUInt16(packet.FrequencyTenths);
            writer.WriteBool(packet.MediaLoaded);
            writer.WriteBool(packet.MediaPlaying);
            writer.WriteByte(packet.VolumePercent);
            return buffer;
        }

        private static bool TryReadPlayerMediaBeginCore(byte[] data, out PacketPlayerMediaBegin packet, bool expectsFrequency)
        {
            packet = new PacketPlayerMediaBegin();
            var minLength = 2 + 4 + 1 + 4 + 4 + 4 + ProtocolConstants.MaxMediaFileExtensionLength + (expectsFrequency ? 2 : 0);
            if (data.Length < minLength)
                return false;

            var reader = new PacketReader(data);
            reader.ReadByte();
            reader.ReadByte();
            packet.PlayerId = reader.ReadUInt32();
            packet.PlayerNumber = reader.ReadByte();
            packet.MediaId = reader.ReadUInt32();
            packet.TransferId = reader.ReadUInt32();
            packet.TotalBytes = reader.ReadUInt32();
            packet.FileExtension = reader.ReadFixedString(ProtocolConstants.MaxMediaFileExtensionLength);
            if (expectsFrequency)
                packet.FrequencyTenths = reader.ReadUInt16();
            return true;
        }

        private static byte[] WritePlayerMediaBeginCore(
            Command command,
            uint playerId,
            byte playerNumber,
            uint mediaId,
            uint transferId,
            uint totalBytes,
            string fileExtension,
            bool includeFrequency,
            ushort frequencyTenths)
        {
            var payloadSize = 4 + 1 + 4 + 4 + 4 + ProtocolConstants.MaxMediaFileExtensionLength + (includeFrequency ? 2 : 0);
            var buffer = WritePacketHeader(command, payloadSize);
            var writer = new PacketWriter(buffer);
            writer.WriteByte(ProtocolConstants.Version);
            writer.WriteByte((byte)command);
            writer.WriteUInt32(playerId);
            writer.WriteByte(playerNumber);
            writer.WriteUInt32(mediaId);
            writer.WriteUInt32(transferId);
            writer.WriteUInt32(totalBytes);
            writer.WriteFixedString(fileExtension ?? string.Empty, ProtocolConstants.MaxMediaFileExtensionLength);
            if (includeFrequency)
                writer.WriteUInt16(frequencyTenths);
            return buffer;
        }

        private static byte[] WritePlayerMediaChunkCore(
            Command command,
            uint playerId,
            byte playerNumber,
            uint mediaId,
            uint transferId,
            ushort chunkIndex,
            byte[] data)
        {
            var bytes = data ?? Array.Empty<byte>();
            if (bytes.Length > ProtocolConstants.MaxMediaChunkBytes)
                throw new ArgumentOutOfRangeException(nameof(data), $"Media chunk cannot exceed {ProtocolConstants.MaxMediaChunkBytes} bytes.");

            var buffer = WritePacketHeader(command, 4 + 1 + 4 + 4 + 2 + 2 + bytes.Length);
            var writer = new PacketWriter(buffer);
            writer.WriteByte(ProtocolConstants.Version);
            writer.WriteByte((byte)command);
            writer.WriteUInt32(playerId);
            writer.WriteByte(playerNumber);
            writer.WriteUInt32(mediaId);
            writer.WriteUInt32(transferId);
            writer.WriteUInt16(chunkIndex);
            writer.WriteUInt16((ushort)bytes.Length);
            for (var i = 0; i < bytes.Length; i++)
                writer.WriteByte(bytes[i]);
            return buffer;
        }

        private static byte[] WritePlayerMediaEndCore(
            Command command,
            uint playerId,
            byte playerNumber,
            uint mediaId,
            uint transferId)
        {
            var buffer = WritePacketHeader(command, 4 + 1 + 4 + 4);
            var writer = new PacketWriter(buffer);
            writer.WriteByte(ProtocolConstants.Version);
            writer.WriteByte((byte)command);
            writer.WriteUInt32(playerId);
            writer.WriteByte(playerNumber);
            writer.WriteUInt32(mediaId);
            writer.WriteUInt32(transferId);
            return buffer;
        }
    }
}
