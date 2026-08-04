using System;

namespace TopSpeed.Protocol
{
    public static partial class PacketValidation
    {
        public static bool IsValidVehiclePackageRef(VehiclePackageRef vehicle)
        {
            if (vehicle == null)
                return false;

            if (vehicle.Kind == RoomVehicleSelectionKind.None)
                return true;

            if (vehicle.Kind == RoomVehicleSelectionKind.BuiltIn)
                return true;

            if (vehicle.Kind != RoomVehicleSelectionKind.CustomPackage)
                return false;

            if (string.IsNullOrWhiteSpace(vehicle.VehicleId) || vehicle.VehicleId.Length > ProtocolConstants.MaxVehicleIdLength)
                return false;
            if (string.IsNullOrWhiteSpace(vehicle.Version) || vehicle.Version.Length > ProtocolConstants.MaxVehicleVersionLength)
                return false;
            if (string.IsNullOrWhiteSpace(vehicle.Hash) || vehicle.Hash.Length > ProtocolConstants.MaxVehicleHashLength)
                return false;

            return true;
        }

        public static bool IsValidVehiclePackageTransferBegin(PacketVehiclePackageTransferBegin packet)
        {
            return packet != null
                && !string.IsNullOrWhiteSpace(packet.VehicleId)
                && packet.VehicleId.Length <= ProtocolConstants.MaxVehicleIdLength
                && !string.IsNullOrWhiteSpace(packet.Version)
                && packet.Version.Length <= ProtocolConstants.MaxVehicleVersionLength
                && !string.IsNullOrWhiteSpace(packet.Hash)
                && packet.Hash.Length <= ProtocolConstants.MaxVehicleHashLength
                && packet.TotalBytes > 0
                && packet.TotalBytes <= ProtocolConstants.MaxVehiclePackageBytes;
        }

        public static bool IsValidVehiclePackageTransferChunk(PacketVehiclePackageTransferChunk packet)
        {
            return packet != null
                && !string.IsNullOrWhiteSpace(packet.Hash)
                && packet.Hash.Length <= ProtocolConstants.MaxVehicleHashLength
                && packet.Data != null
                && packet.Data.Length > 0
                && packet.Data.Length <= ProtocolConstants.MaxVehiclePackageChunkBytes;
        }

        public static bool IsValidVehiclePackageTransferEnd(PacketVehiclePackageTransferEnd packet)
        {
            return packet != null
                && !string.IsNullOrWhiteSpace(packet.Hash)
                && packet.Hash.Length <= ProtocolConstants.MaxVehicleHashLength;
        }

        public static bool IsValidVehiclePackageReady(PacketVehiclePackageReady packet)
        {
            return packet != null
                && !string.IsNullOrWhiteSpace(packet.Hash)
                && packet.Hash.Length <= ProtocolConstants.MaxVehicleHashLength;
        }

        public static bool IsValidVehiclePackageRequest(PacketVehiclePackageRequest packet)
        {
            return packet != null
                && !string.IsNullOrWhiteSpace(packet.Hash)
                && packet.Hash.Length <= ProtocolConstants.MaxVehicleHashLength;
        }

        public static bool IsValidVehiclePackageCatalogRequest(PacketVehiclePackageCatalogRequest packet)
        {
            return packet != null;
        }

        public static bool IsValidVehiclePackageCatalogEntry(PacketVehiclePackageCatalogEntry entry)
        {
            return entry != null
                && entry.Vehicle != null
                && entry.Vehicle.IsCustomPackage
                && IsValidVehiclePackageRef(entry.Vehicle)
                && !string.IsNullOrWhiteSpace(entry.DisplayName)
                && entry.DisplayName.Length <= ProtocolConstants.MaxVehiclePackageDisplayNameLength;
        }

        public static bool IsValidVehiclePackageCatalog(PacketVehiclePackageCatalog packet)
        {
            if (packet == null || packet.Vehicles == null)
                return false;

            if (packet.Vehicles.Length > ProtocolConstants.MaxVehiclePackageCatalogEntries)
                return false;

            for (var i = 0; i < packet.Vehicles.Length; i++)
            {
                if (!IsValidVehiclePackageCatalogEntry(packet.Vehicles[i]))
                    return false;
            }

            return true;
        }
    }
}
