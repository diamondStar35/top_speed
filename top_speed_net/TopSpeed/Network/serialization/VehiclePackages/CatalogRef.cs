using TopSpeed.Protocol;

namespace TopSpeed.Network
{
    internal static partial class ClientPacketSerializer
    {
        private static VehiclePackageRef ReadCatalogVehicleRef(ref PacketReader reader)
        {
            var kind = (RoomVehicleSelectionKind)reader.ReadByte();
            var builtInCar = reader.ReadByte();
            var vehicleId = reader.ReadString16();
            var version = reader.ReadString16();
            var hash = reader.ReadString16();

            return new VehiclePackageRef
            {
                Kind = kind,
                BuiltInCar = builtInCar,
                VehicleId = vehicleId ?? string.Empty,
                Version = version ?? string.Empty,
                Hash = VehiclePackageRef.NormalizeHash(hash)
            };
        }

        private static int MeasureCatalogVehicleRef(VehiclePackageRef vehicle)
        {
            var normalized = VehiclePackageRef.Clone(vehicle);
            return 1
                + 1
                + 2 + PacketWriter.MeasureString16(normalized.VehicleId)
                + 2 + PacketWriter.MeasureString16(normalized.Version)
                + 2 + PacketWriter.MeasureString16(normalized.Hash);
        }

        private static void WriteCatalogVehicleRef(ref PacketWriter writer, VehiclePackageRef vehicle)
        {
            var normalized = VehiclePackageRef.Clone(vehicle);
            writer.WriteByte((byte)normalized.Kind);
            writer.WriteByte(normalized.BuiltInCar);
            writer.WriteString16(normalized.VehicleId ?? string.Empty);
            writer.WriteString16(normalized.Version ?? string.Empty);
            writer.WriteString16(normalized.Hash ?? string.Empty);
        }
    }
}
