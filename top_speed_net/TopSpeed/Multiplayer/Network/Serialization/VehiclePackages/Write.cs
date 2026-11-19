using TopSpeed.Protocol;

namespace TopSpeed.Network
{
    internal static partial class ClientPacketSerializer
    {
        public static byte[] WriteVehiclePackageCatalogRequest()
        {
            return WritePacketHeader(Command.VehiclePackageCatalogRequest, 0);
        }

        public static byte[] WriteVehiclePackageRequest(PacketVehiclePackageRequest packet)
        {
            var payload = 2 + PacketWriter.MeasureString16(packet.Hash);
            var buffer = WritePacketHeader(Command.VehiclePackageRequest, payload);
            var writer = new PacketWriter(buffer);
            writer.WriteByte(ProtocolConstants.Version);
            writer.WriteByte((byte)Command.VehiclePackageRequest);
            writer.WriteString16(packet.Hash ?? string.Empty);
            return buffer;
        }

        public static byte[] WriteVehiclePackageReady(PacketVehiclePackageReady packet)
        {
            var payload = 2 + PacketWriter.MeasureString16(packet.Hash);
            var buffer = WritePacketHeader(Command.VehiclePackageReady, payload);
            var writer = new PacketWriter(buffer);
            writer.WriteByte(ProtocolConstants.Version);
            writer.WriteByte((byte)Command.VehiclePackageReady);
            writer.WriteString16(packet.Hash ?? string.Empty);
            return buffer;
        }
    }
}
