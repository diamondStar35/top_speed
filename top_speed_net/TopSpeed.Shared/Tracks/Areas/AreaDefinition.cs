using System;
using System.Collections.Generic;
using TopSpeed.Data;

namespace TopSpeed.Tracks.Areas
{
    public sealed class TrackAreaDefinition
    {
        private static readonly IReadOnlyDictionary<string, string> EmptyMetadata =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        public TrackAreaDefinition(
            string id,
            TrackAreaType type,
            string shapeId,
            string? name = null,
            TrackSurface? surface = null,
            TrackNoise? noise = null,
            float? widthMeters = null,
            TrackAreaFlags flags = TrackAreaFlags.None,
            IReadOnlyDictionary<string, string>? metadata = null)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("Area id is required.", nameof(id));
            if (string.IsNullOrWhiteSpace(shapeId))
                throw new ArgumentException("Shape id is required.", nameof(shapeId));

            Id = id.Trim();
            Type = type;
            ShapeId = shapeId.Trim();
            var trimmedName = name?.Trim();
            Name = string.IsNullOrWhiteSpace(trimmedName) ? null : trimmedName;
            Surface = surface;
            Noise = noise;
            WidthMeters = widthMeters;
            Flags = flags;
            Metadata = NormalizeMetadata(metadata);
        }

        public string Id { get; }
        public TrackAreaType Type { get; }
        public string ShapeId { get; }
        public string? Name { get; }
        public TrackSurface? Surface { get; }
        public TrackNoise? Noise { get; }
        public float? WidthMeters { get; }
        public TrackAreaFlags Flags { get; }
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
