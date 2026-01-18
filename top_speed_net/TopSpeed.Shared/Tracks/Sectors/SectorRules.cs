using System;
using System.Collections.Generic;
using TopSpeed.Tracks.Map;

namespace TopSpeed.Tracks.Sectors
{
    public sealed class TrackSectorRules
    {
        private static readonly IReadOnlyCollection<string> EmptyStrings = Array.Empty<string>();
        private static readonly IReadOnlyCollection<MapDirection> EmptyDirections = Array.Empty<MapDirection>();

        public TrackSectorRules(
            string sectorId,
            bool isClosed,
            bool isRestricted,
            bool requiresStop,
            bool requiresYield,
            float? minSpeedKph,
            float? maxSpeedKph,
            IReadOnlyCollection<string>? allowedEntryPortals,
            IReadOnlyCollection<string>? deniedEntryPortals,
            IReadOnlyCollection<string>? allowedExitPortals,
            IReadOnlyCollection<string>? deniedExitPortals,
            IReadOnlyCollection<MapDirection>? allowedEntryDirections,
            IReadOnlyCollection<MapDirection>? deniedEntryDirections,
            IReadOnlyCollection<MapDirection>? allowedExitDirections,
            IReadOnlyCollection<MapDirection>? deniedExitDirections,
            IReadOnlyDictionary<string, string>? metadata)
        {
            if (string.IsNullOrWhiteSpace(sectorId))
                throw new ArgumentException("Sector id is required.", nameof(sectorId));

            SectorId = sectorId.Trim();
            IsClosed = isClosed;
            IsRestricted = isRestricted;
            RequiresStop = requiresStop;
            RequiresYield = requiresYield;
            MinSpeedKph = minSpeedKph;
            MaxSpeedKph = maxSpeedKph;
            AllowedEntryPortals = allowedEntryPortals ?? EmptyStrings;
            DeniedEntryPortals = deniedEntryPortals ?? EmptyStrings;
            AllowedExitPortals = allowedExitPortals ?? EmptyStrings;
            DeniedExitPortals = deniedExitPortals ?? EmptyStrings;
            AllowedEntryDirections = allowedEntryDirections ?? EmptyDirections;
            DeniedEntryDirections = deniedEntryDirections ?? EmptyDirections;
            AllowedExitDirections = allowedExitDirections ?? EmptyDirections;
            DeniedExitDirections = deniedExitDirections ?? EmptyDirections;
            Metadata = metadata ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }

        public string SectorId { get; }
        public bool IsClosed { get; }
        public bool IsRestricted { get; }
        public bool RequiresStop { get; }
        public bool RequiresYield { get; }
        public float? MinSpeedKph { get; }
        public float? MaxSpeedKph { get; }
        public IReadOnlyCollection<string> AllowedEntryPortals { get; }
        public IReadOnlyCollection<string> DeniedEntryPortals { get; }
        public IReadOnlyCollection<string> AllowedExitPortals { get; }
        public IReadOnlyCollection<string> DeniedExitPortals { get; }
        public IReadOnlyCollection<MapDirection> AllowedEntryDirections { get; }
        public IReadOnlyCollection<MapDirection> DeniedEntryDirections { get; }
        public IReadOnlyCollection<MapDirection> AllowedExitDirections { get; }
        public IReadOnlyCollection<MapDirection> DeniedExitDirections { get; }
        public IReadOnlyDictionary<string, string> Metadata { get; }
    }
}
