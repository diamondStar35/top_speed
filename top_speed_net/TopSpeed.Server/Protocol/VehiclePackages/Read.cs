using TopSpeed.Protocol;

namespace TopSpeed.Server.Protocol
{
    internal static partial class PacketSerializer
    {
        public static bool TryReadVehiclePackageReady(byte[] data, out PacketVehiclePackageReady packet)
        {
            packet = new PacketVehiclePackageReady();
            if (data.Length < 2 + 2)
                return false;
            if (data[0] != ProtocolConstants.Version || data[1] != (byte)Command.VehiclePackageReady)
                return false;
            try
            {
                var reader = new PacketReader(data);
                reader.ReadByte();
                reader.ReadByte();
                packet.Hash = VehiclePackageRef.NormalizeHash(reader.ReadString16());
                return PacketValidation.IsValidVehiclePackageReady(packet);
            }
            catch
            {
                packet = new PacketVehiclePackageReady();
                return false;
            }
        }

        public static bool TryReadVehiclePackageCatalogRequest(byte[] data, out PacketVehiclePackageCatalogRequest packet)
        {
            packet = new PacketVehiclePackageCatalogRequest();
            if (data.Length != 2)
                return false;
            if (data[0] != ProtocolConstants.Version || data[1] != (byte)Command.VehiclePackageCatalogRequest)
                return false;
            return PacketValidation.IsValidVehiclePackageCatalogRequest(packet);
        }
    }
}
