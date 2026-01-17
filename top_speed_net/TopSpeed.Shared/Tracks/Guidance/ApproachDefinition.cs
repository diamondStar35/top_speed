using System;
using System.Collections.Generic;

namespace TopSpeed.Tracks.Guidance
{
    public sealed class TrackApproachDefinition
    {
        private static readonly IReadOnlyDictionary<string, string> EmptyMetadata =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        public TrackApproachDefinition(
            string sectorId,
            string? name = null,
            string? entryPortalId = null,
            string? exitPortalId = null,
            float? entryHeadingDegrees = null,
            float? exitHeadingDegrees = null,
            float? widthMeters = null,
            float? lengthMeters = null,
            float? alignmentToleranceDegrees = null,
            IReadOnlyDictionary<string, string>? metadata = null)
        {
            if (string.IsNullOrWhiteSpace(sectorId))
                throw new ArgumentException("Sector id is required.", nameof(sectorId));

            SectorId = sectorId.Trim();
            var trimmedName = name?.Trim();
            Name = string.IsNullOrWhiteSpace(trimmedName) ? null : trimmedName;
            EntryPortalId = string.IsNullOrWhiteSpace(entryPortalId) ? null : entryPortalId!.Trim();
            ExitPortalId = string.IsNullOrWhiteSpace(exitPortalId) ? null : exitPortalId!.Trim();
            EntryHeadingDegrees = entryHeadingDegrees;
            ExitHeadingDegrees = exitHeadingDegrees;
            WidthMeters = widthMeters;
            LengthMeters = lengthMeters;
            AlignmentToleranceDegrees = alignmentToleranceDegrees;
            Metadata = NormalizeMetadata(metadata);
        }

        public string SectorId { get; }
        public string? Name { get; }
        public string? EntryPortalId { get; }
        public string? ExitPortalId { get; }
        public float? EntryHeadingDegrees { get; }
        public float? ExitHeadingDegrees { get; }
        public float? WidthMeters { get; }
        public float? LengthMeters { get; }
        public float? AlignmentToleranceDegrees { get; }
        public IReadOnlyDictionary<string, string> Metadata { get; }

        private static IReadOnlyDictionary<string, string> NormalizeMetadata(IReadOnlyDictionary<string, string>? metadata)
        {
            if (metadata == null || metadata.Count == 0)
                return EmptyMetadata;
            var copy = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var pair in metadata)
                copy[pair.Key] = pair.Value;
            return copy;
        }
    }
}
