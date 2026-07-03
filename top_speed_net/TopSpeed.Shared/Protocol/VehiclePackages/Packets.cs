using System;

namespace TopSpeed.Protocol
{
    // NOTE: There are deliberately no upload packets. Unlike custom tracks, players
    // cannot upload vehicles; the server owner installs them into a Vehicles/ folder.

    public sealed class PacketVehiclePackageTransferBegin
    {
        public string VehicleId = string.Empty;
        public string Version = string.Empty;
        public string Hash = string.Empty;
        public uint TotalBytes;
    }

    public sealed class PacketVehiclePackageTransferChunk
    {
        public string Hash = string.Empty;
        public ushort ChunkIndex;
        public byte[] Data = Array.Empty<byte>();
    }

    public sealed class PacketVehiclePackageTransferEnd
    {
        public string Hash = string.Empty;
    }

    public sealed class PacketVehiclePackageReady
    {
        public string Hash = string.Empty;
    }

    public sealed class PacketVehiclePackageCatalogRequest
    {
    }

    public sealed class PacketVehiclePackageCatalogEntry
    {
        public VehiclePackageRef Vehicle = new VehiclePackageRef();
        public string DisplayName = string.Empty;
        // Which transmission modes the vehicle supports, so the client can skip the
        // automatic/manual prompt for single-mode vehicles before the package is downloaded.
        public bool SupportsAutomatic = true;
        public bool SupportsManual = true;
    }

    public sealed class PacketVehiclePackageCatalog
    {
        public PacketVehiclePackageCatalogEntry[] Vehicles = Array.Empty<PacketVehiclePackageCatalogEntry>();
    }

    // Broadcast once per selection so peers learn which custom vehicle a player drives
    // (kept out of the per-frame race snapshot). Empty hash = the player uses a built-in vehicle.
    public sealed class PacketRoomPlayerVehicle
    {
        public byte PlayerNumber;
        public string Hash = string.Empty;
    }
}
