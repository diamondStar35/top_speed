using System;
using System.Collections.Generic;
using TopSpeed.Data;

namespace TopSpeed.Tracks.Sectors
{
    public sealed class TrackSectorDefinition
    {
        private static readonly IReadOnlyDictionary<string, string> EmptyMetadata =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        public TrackSectorDefinition(
            string id,
            TrackSectorType type,
            string? name = null,
            string? areaId = null,
            string? code = null,
            TrackSurface? surface = null,
            TrackNoise? noise = null,
            TrackSectorFlags flags = TrackSectorFlags.None,
            IReadOnlyDictionary<string, string>? metadata = null)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("Sector id is required.", nameof(id));

            Id = id.Trim();
            Type = type;
            var trimmedName = name?.Trim();
            Name = string.IsNullOrWhiteSpace(trimmedName) ? null : trimmedName;
            var trimmedArea = areaId?.Trim();
            AreaId = string.IsNullOrWhiteSpace(trimmedArea) ? null : trimmedArea;
            var trimmedCode = code?.Trim();
            Code = string.IsNullOrWhiteSpace(trimmedCode) ? null : trimmedCode;
            Surface = surface;
            Noise = noise;
            Flags = flags;
            Metadata = NormalizeMetadata(metadata);
        }

        public string Id { get; }
        public TrackSectorType Type { get; }
        public string? Name { get; }
        public string? AreaId { get; }
        public string? Code { get; }
        public TrackSurface? Surface { get; }
        public TrackNoise? Noise { get; }
        public TrackSectorFlags Flags { get; }
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
