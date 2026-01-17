using System;

namespace TopSpeed.Tracks.Topology
{
    public sealed class PortalDefinition
    {
        public PortalDefinition(
            string id,
            string sectorId,
            float x,
            float z,
            float widthMeters,
            float? entryHeadingDegrees,
            float? exitHeadingDegrees,
            PortalRole role)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("Portal id is required.", nameof(id));
            if (string.IsNullOrWhiteSpace(sectorId))
                throw new ArgumentException("Sector id is required.", nameof(sectorId));

            Id = id.Trim();
            SectorId = sectorId.Trim();
            X = x;
            Z = z;
            WidthMeters = widthMeters;
            EntryHeadingDegrees = entryHeadingDegrees;
            ExitHeadingDegrees = exitHeadingDegrees;
            Role = role;
        }

        public string Id { get; }
        public string SectorId { get; }
        public float X { get; }
        public float Z { get; }
        public float WidthMeters { get; }
        public float? EntryHeadingDegrees { get; }
        public float? ExitHeadingDegrees { get; }
        public PortalRole Role { get; }
    }
}
