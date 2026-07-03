using System;
using TopSpeed.Protocol;

namespace TopSpeed.Server.Protocol
{
    internal static partial class PacketSerializer
    {
        public static byte[] WriteVehiclePackageTransferBegin(PacketVehiclePackageTransferBegin packet)
        {
            var payload = 2 + PacketWriter.MeasureString16(packet.VehicleId)
                + 2 + PacketWriter.MeasureString16(packet.Version)
                + 2 + PacketWriter.MeasureString16(packet.Hash)
                + 4;
            var buffer = WritePacketHeader(Command.VehiclePackageTransferBegin, payload);
            var writer = new PacketWriter(buffer);
            writer.WriteByte(ProtocolConstants.Version);
            writer.WriteByte((byte)Command.VehiclePackageTransferBegin);
            writer.WriteString16(packet.VehicleId ?? string.Empty);
            writer.WriteString16(packet.Version ?? string.Empty);
            writer.WriteString16(packet.Hash ?? string.Empty);
            writer.WriteUInt32(packet.TotalBytes);
            return buffer;
        }

        public static byte[] WriteVehiclePackageTransferChunk(PacketVehiclePackageTransferChunk packet)
        {
            var bytes = packet.Data ?? Array.Empty<byte>();
            if (bytes.Length == 0 || bytes.Length > ProtocolConstants.MaxVehiclePackageChunkBytes)
                throw new ArgumentOutOfRangeException(nameof(packet), "Invalid vehicle package chunk size.");

            var payload = 2 + PacketWriter.MeasureString16(packet.Hash) + 2 + 2 + bytes.Length;
            var buffer = WritePacketHeader(Command.VehiclePackageTransferChunk, payload);
            var writer = new PacketWriter(buffer);
            writer.WriteByte(ProtocolConstants.Version);
            writer.WriteByte((byte)Command.VehiclePackageTransferChunk);
            writer.WriteString16(packet.Hash ?? string.Empty);
            writer.WriteUInt16(packet.ChunkIndex);
            writer.WriteUInt16((ushort)bytes.Length);
            for (var i = 0; i < bytes.Length; i++)
                writer.WriteByte(bytes[i]);
            return buffer;
        }

        public static byte[] WriteVehiclePackageTransferEnd(PacketVehiclePackageTransferEnd packet)
        {
            var payload = 2 + PacketWriter.MeasureString16(packet.Hash);
            var buffer = WritePacketHeader(Command.VehiclePackageTransferEnd, payload);
            var writer = new PacketWriter(buffer);
            writer.WriteByte(ProtocolConstants.Version);
            writer.WriteByte((byte)Command.VehiclePackageTransferEnd);
            writer.WriteString16(packet.Hash ?? string.Empty);
            return buffer;
        }

        public static byte[] WriteRoomPlayerVehicle(PacketRoomPlayerVehicle packet)
        {
            var payload = 1 + 2 + PacketWriter.MeasureString16(packet.Hash);
            var buffer = WritePacketHeader(Command.RoomPlayerVehicle, payload);
            var writer = new PacketWriter(buffer);
            writer.WriteByte(ProtocolConstants.Version);
            writer.WriteByte((byte)Command.RoomPlayerVehicle);
            writer.WriteByte(packet.PlayerNumber);
            writer.WriteString16(packet.Hash ?? string.Empty);
            return buffer;
        }

        public static byte[] WriteVehiclePackageCatalog(PacketVehiclePackageCatalog packet)
        {
            packet ??= new PacketVehiclePackageCatalog();
            var vehicles = packet.Vehicles ?? Array.Empty<PacketVehiclePackageCatalogEntry>();
            var count = Math.Min(vehicles.Length, ProtocolConstants.MaxVehiclePackageCatalogEntries);

            var payload = 2;
            for (var i = 0; i < count; i++)
            {
                var entry = vehicles[i] ?? new PacketVehiclePackageCatalogEntry();
                payload += MeasureCatalogVehicleRef(entry.Vehicle);
                payload += 2 + PacketWriter.MeasureString16(entry.DisplayName ?? string.Empty);
                payload += 2; // SupportsAutomatic + SupportsManual
            }

            var buffer = WritePacketHeader(Command.VehiclePackageCatalog, payload);
            var writer = new PacketWriter(buffer);
            writer.WriteByte(ProtocolConstants.Version);
            writer.WriteByte((byte)Command.VehiclePackageCatalog);
            writer.WriteUInt16((ushort)count);
            for (var i = 0; i < count; i++)
            {
                var entry = vehicles[i] ?? new PacketVehiclePackageCatalogEntry();
                WriteCatalogVehicleRef(ref writer, entry.Vehicle);
                writer.WriteString16(entry.DisplayName ?? string.Empty);
                writer.WriteBool(entry.SupportsAutomatic);
                writer.WriteBool(entry.SupportsManual);
            }

            return buffer;
        }
    }
}
