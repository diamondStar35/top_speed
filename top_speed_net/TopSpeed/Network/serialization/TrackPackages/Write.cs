using System;
using TopSpeed.Protocol;

namespace TopSpeed.Network
{
    internal static partial class ClientPacketSerializer
    {
        public static byte[] WriteTrackPackageUploadBegin(PacketTrackPackageUploadBegin packet)
        {
            var payload = 4
                + 2 + PacketWriter.MeasureString16(packet.TrackId)
                + 2 + PacketWriter.MeasureString16(packet.Version)
                + 2 + PacketWriter.MeasureString16(packet.Hash)
                + 4;
            var buffer = WritePacketHeader(Command.TrackPackageUploadBegin, payload);
            var writer = new PacketWriter(buffer);
            writer.WriteByte(ProtocolConstants.Version);
            writer.WriteByte((byte)Command.TrackPackageUploadBegin);
            writer.WriteUInt32(packet.UploadId);
            writer.WriteString16(packet.TrackId ?? string.Empty);
            writer.WriteString16(packet.Version ?? string.Empty);
            writer.WriteString16(packet.Hash ?? string.Empty);
            writer.WriteUInt32(packet.TotalBytes);
            return buffer;
        }

        public static byte[] WriteTrackPackageUploadChunk(PacketTrackPackageUploadChunk packet)
        {
            var bytes = packet.Data ?? Array.Empty<byte>();
            if (bytes.Length == 0 || bytes.Length > ProtocolConstants.MaxTrackPackageChunkBytes)
                throw new ArgumentOutOfRangeException(nameof(packet), "Invalid track package chunk size.");

            var buffer = WritePacketHeader(Command.TrackPackageUploadChunk, 4 + 2 + 2 + bytes.Length);
            var writer = new PacketWriter(buffer);
            writer.WriteByte(ProtocolConstants.Version);
            writer.WriteByte((byte)Command.TrackPackageUploadChunk);
            writer.WriteUInt32(packet.UploadId);
            writer.WriteUInt16(packet.ChunkIndex);
            writer.WriteUInt16((ushort)bytes.Length);
            for (var i = 0; i < bytes.Length; i++)
                writer.WriteByte(bytes[i]);
            return buffer;
        }

        public static byte[] WriteTrackPackageUploadEnd(PacketTrackPackageUploadEnd packet)
        {
            var buffer = WritePacketHeader(Command.TrackPackageUploadEnd, 4);
            var writer = new PacketWriter(buffer);
            writer.WriteByte(ProtocolConstants.Version);
            writer.WriteByte((byte)Command.TrackPackageUploadEnd);
            writer.WriteUInt32(packet.UploadId);
            return buffer;
        }

        public static byte[] WriteTrackPackageUploadResult(PacketTrackPackageUploadResult packet)
        {
            var payload = 4 + 1
                + 2 + PacketWriter.MeasureString16(packet.Hash)
                + 2 + PacketWriter.MeasureString16(packet.Message);
            var buffer = WritePacketHeader(Command.TrackPackageUploadResult, payload);
            var writer = new PacketWriter(buffer);
            writer.WriteByte(ProtocolConstants.Version);
            writer.WriteByte((byte)Command.TrackPackageUploadResult);
            writer.WriteUInt32(packet.UploadId);
            writer.WriteByte((byte)packet.Status);
            writer.WriteString16(packet.Hash ?? string.Empty);
            writer.WriteString16(packet.Message ?? string.Empty);
            return buffer;
        }

        public static byte[] WriteTrackPackageTransferBegin(PacketTrackPackageTransferBegin packet)
        {
            var payload = 2 + PacketWriter.MeasureString16(packet.TrackId)
                + 2 + PacketWriter.MeasureString16(packet.Version)
                + 2 + PacketWriter.MeasureString16(packet.Hash)
                + 4;
            var buffer = WritePacketHeader(Command.TrackPackageTransferBegin, payload);
            var writer = new PacketWriter(buffer);
            writer.WriteByte(ProtocolConstants.Version);
            writer.WriteByte((byte)Command.TrackPackageTransferBegin);
            writer.WriteString16(packet.TrackId ?? string.Empty);
            writer.WriteString16(packet.Version ?? string.Empty);
            writer.WriteString16(packet.Hash ?? string.Empty);
            writer.WriteUInt32(packet.TotalBytes);
            return buffer;
        }

        public static byte[] WriteTrackPackageTransferChunk(PacketTrackPackageTransferChunk packet)
        {
            var bytes = packet.Data ?? Array.Empty<byte>();
            if (bytes.Length == 0 || bytes.Length > ProtocolConstants.MaxTrackPackageChunkBytes)
                throw new ArgumentOutOfRangeException(nameof(packet), "Invalid track package chunk size.");

            var payload = 2 + PacketWriter.MeasureString16(packet.Hash) + 2 + 2 + bytes.Length;
            var buffer = WritePacketHeader(Command.TrackPackageTransferChunk, payload);
            var writer = new PacketWriter(buffer);
            writer.WriteByte(ProtocolConstants.Version);
            writer.WriteByte((byte)Command.TrackPackageTransferChunk);
            writer.WriteString16(packet.Hash ?? string.Empty);
            writer.WriteUInt16(packet.ChunkIndex);
            writer.WriteUInt16((ushort)bytes.Length);
            for (var i = 0; i < bytes.Length; i++)
                writer.WriteByte(bytes[i]);
            return buffer;
        }

        public static byte[] WriteTrackPackageTransferEnd(PacketTrackPackageTransferEnd packet)
        {
            var payload = 2 + PacketWriter.MeasureString16(packet.Hash);
            var buffer = WritePacketHeader(Command.TrackPackageTransferEnd, payload);
            var writer = new PacketWriter(buffer);
            writer.WriteByte(ProtocolConstants.Version);
            writer.WriteByte((byte)Command.TrackPackageTransferEnd);
            writer.WriteString16(packet.Hash ?? string.Empty);
            return buffer;
        }

        public static byte[] WriteTrackPackageReady(PacketTrackPackageReady packet)
        {
            var payload = 2 + PacketWriter.MeasureString16(packet.Hash);
            var buffer = WritePacketHeader(Command.TrackPackageReady, payload);
            var writer = new PacketWriter(buffer);
            writer.WriteByte(ProtocolConstants.Version);
            writer.WriteByte((byte)Command.TrackPackageReady);
            writer.WriteString16(packet.Hash ?? string.Empty);
            return buffer;
        }

        public static byte[] WriteTrackPackageCatalogRequest()
        {
            return WritePacketHeader(Command.TrackPackageCatalogRequest, 0);
        }

        public static byte[] WriteTrackPackageCatalog(PacketTrackPackageCatalog packet)
        {
            packet ??= new PacketTrackPackageCatalog();
            var tracks = packet.Tracks ?? Array.Empty<PacketTrackPackageCatalogEntry>();
            var count = Math.Min(tracks.Length, ProtocolConstants.MaxTrackPackageCatalogEntries);

            var payload = 2;
            for (var i = 0; i < count; i++)
            {
                var entry = tracks[i] ?? new PacketTrackPackageCatalogEntry();
                payload += MeasureCatalogTrackRef(entry.Track);
                payload += 2 + PacketWriter.MeasureString16(entry.DisplayName ?? string.Empty);
                payload += 1;
            }

            var buffer = WritePacketHeader(Command.TrackPackageCatalog, payload);
            var writer = new PacketWriter(buffer);
            writer.WriteByte(ProtocolConstants.Version);
            writer.WriteByte((byte)Command.TrackPackageCatalog);
            writer.WriteUInt16((ushort)count);
            for (var i = 0; i < count; i++)
            {
                var entry = tracks[i] ?? new PacketTrackPackageCatalogEntry();
                WriteCatalogTrackRef(ref writer, entry.Track);
                writer.WriteString16(entry.DisplayName ?? string.Empty);
                writer.WriteBool(entry.HasPitArea);
            }

            return buffer;
        }
    }
}
