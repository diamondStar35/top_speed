using System;
using TopSpeed.Protocol;

namespace TopSpeed.Server.Network
{
    internal sealed class VehiclePackageRecord
    {
        public VehiclePackageRef Ref { get; set; } = new VehiclePackageRef();
        public VehiclePackagePayload Payload { get; set; } = new VehiclePackagePayload();
        public byte[] Bytes { get; set; } = Array.Empty<byte>();
        public string DisplayName { get; set; } = string.Empty;
        public float WidthM { get; set; }
        public float LengthM { get; set; }
        public float MassKg { get; set; }
        public DateTime LastAccessUtc { get; set; } = DateTime.UtcNow;
        public string SourcePath { get; set; } = string.Empty;
        public DateTime SourceLastWriteUtc { get; set; } = DateTime.MinValue;
    }
}
