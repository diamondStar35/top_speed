using System;

namespace TopSpeed.Protocol
{
    public sealed class PacketTrackPackageUploadBegin
    {
        public uint UploadId;
        public string TrackId = string.Empty;
        public string Version = string.Empty;
        public string Hash = string.Empty;
        public uint TotalBytes;
    }

    public sealed class PacketTrackPackageUploadChunk
    {
        public uint UploadId;
        public ushort ChunkIndex;
        public byte[] Data = Array.Empty<byte>();
    }

    public sealed class PacketTrackPackageUploadEnd
    {
        public uint UploadId;
    }

    public sealed class PacketTrackPackageUploadResult
    {
        public uint UploadId;
        public TrackPackageUploadStatus Status;
        public string Hash = string.Empty;
        public string Message = string.Empty;
    }

    public sealed class PacketTrackPackageTransferBegin
    {
        public string TrackId = string.Empty;
        public string Version = string.Empty;
        public string Hash = string.Empty;
        public uint TotalBytes;
    }

    public sealed class PacketTrackPackageTransferChunk
    {
        public string Hash = string.Empty;
        public ushort ChunkIndex;
        public byte[] Data = Array.Empty<byte>();
    }

    public sealed class PacketTrackPackageTransferEnd
    {
        public string Hash = string.Empty;
    }

    public sealed class PacketTrackPackageReady
    {
        public string Hash = string.Empty;
    }

    public sealed class PacketTrackPackageCatalogRequest
    {
    }

    public sealed class PacketTrackPackageCatalogEntry
    {
        public TrackPackageRef Track = new TrackPackageRef();
        public string DisplayName = string.Empty;
        // Whether this track has a pit area (false when its [meta] sets pit_area = false). Carried so
        // a host can warn before starting a race on a no-pit track while fuel/tire models are enabled,
        // without first downloading the package. Defaults true (unknown/legacy => assume pit area).
        public bool HasPitArea = true;
    }

    public sealed class PacketTrackPackageCatalog
    {
        public PacketTrackPackageCatalogEntry[] Tracks = Array.Empty<PacketTrackPackageCatalogEntry>();
    }
}
