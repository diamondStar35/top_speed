using System;
using TopSpeed.Protocol;

namespace TopSpeed.Network
{
    internal static partial class ClientPacketSerializer
    {
        public static bool TryReadVehiclePackageTransferBegin(byte[] data, out PacketVehiclePackageTransferBegin packet)
        {
            packet = new PacketVehiclePackageTransferBegin();
            if (data.Length < 2 + 2 + 2 + 2 + 4)
                return false;
            if (data[0] != ProtocolConstants.Version || data[1] != (byte)Command.VehiclePackageTransferBegin)
                return false;

            try
            {
                var reader = new PacketReader(data);
                reader.ReadByte();
                reader.ReadByte();
                packet.VehicleId = reader.ReadString16();
                packet.Version = reader.ReadString16();
                packet.Hash = VehiclePackageRef.NormalizeHash(reader.ReadString16());
                packet.TotalBytes = reader.ReadUInt32();
                return PacketValidation.IsValidVehiclePackageTransferBegin(packet);
            }
            catch
            {
                packet = new PacketVehiclePackageTransferBegin();
                return false;
            }
        }

        public static bool TryReadVehiclePackageTransferChunk(byte[] data, out PacketVehiclePackageTransferChunk packet)
        {
            packet = new PacketVehiclePackageTransferChunk();
            if (data.Length < 2 + 2 + 2 + 2)
                return false;
            if (data[0] != ProtocolConstants.Version || data[1] != (byte)Command.VehiclePackageTransferChunk)
                return false;

            try
            {
                var reader = new PacketReader(data);
                reader.ReadByte();
                reader.ReadByte();
                var rawHash = reader.ReadString16();
                packet.Hash = VehiclePackageRef.NormalizeHash(rawHash);
                packet.ChunkIndex = reader.ReadUInt16();
                var length = reader.ReadUInt16();
                if (length == 0 || length > ProtocolConstants.MaxVehiclePackageChunkBytes)
                    return false;
                if (data.Length != 2 + 2 + PacketWriter.MeasureString16(rawHash) + 2 + 2 + length)
                    return false;

                var bytes = new byte[length];
                for (var i = 0; i < length; i++)
                    bytes[i] = reader.ReadByte();
                packet.Data = bytes;
                return PacketValidation.IsValidVehiclePackageTransferChunk(packet);
            }
            catch
            {
                packet = new PacketVehiclePackageTransferChunk();
                return false;
            }
        }

        public static bool TryReadVehiclePackageTransferEnd(byte[] data, out PacketVehiclePackageTransferEnd packet)
        {
            packet = new PacketVehiclePackageTransferEnd();
            if (data.Length < 2 + 2)
                return false;
            if (data[0] != ProtocolConstants.Version || data[1] != (byte)Command.VehiclePackageTransferEnd)
                return false;

            try
            {
                var reader = new PacketReader(data);
                reader.ReadByte();
                reader.ReadByte();
                packet.Hash = VehiclePackageRef.NormalizeHash(reader.ReadString16());
                return PacketValidation.IsValidVehiclePackageTransferEnd(packet);
            }
            catch
            {
                packet = new PacketVehiclePackageTransferEnd();
                return false;
            }
        }

        public static bool TryReadRoomPlayerVehicle(byte[] data, out PacketRoomPlayerVehicle packet)
        {
            packet = new PacketRoomPlayerVehicle();
            if (data.Length < 2 + 1 + 2)
                return false;
            if (data[0] != ProtocolConstants.Version || data[1] != (byte)Command.RoomPlayerVehicle)
                return false;

            try
            {
                var reader = new PacketReader(data);
                reader.ReadByte();
                reader.ReadByte();
                packet.PlayerNumber = reader.ReadByte();
                packet.Hash = VehiclePackageRef.NormalizeHash(reader.ReadString16());
                return true;
            }
            catch
            {
                packet = new PacketRoomPlayerVehicle();
                return false;
            }
        }

        public static bool TryReadVehiclePackageCatalog(byte[] data, out PacketVehiclePackageCatalog packet)
        {
            packet = new PacketVehiclePackageCatalog();
            if (data.Length < 2 + 2)
                return false;
            if (data[0] != ProtocolConstants.Version || data[1] != (byte)Command.VehiclePackageCatalog)
                return false;

            try
            {
                var reader = new PacketReader(data);
                reader.ReadByte();
                reader.ReadByte();
                var count = reader.ReadUInt16();
                if (count > ProtocolConstants.MaxVehiclePackageCatalogEntries)
                    return false;

                var vehicles = new PacketVehiclePackageCatalogEntry[count];
                for (var i = 0; i < count; i++)
                {
                    var vehicle = ReadCatalogVehicleRef(ref reader);
                    var displayName = reader.ReadString16();
                    vehicles[i] = new PacketVehiclePackageCatalogEntry
                    {
                        Vehicle = vehicle,
                        DisplayName = displayName
                    };
                }

                packet.Vehicles = vehicles;
                return PacketValidation.IsValidVehiclePackageCatalog(packet);
            }
            catch
            {
                packet = new PacketVehiclePackageCatalog();
                return false;
            }
        }
    }
}
