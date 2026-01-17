using System;
using System.Collections.Generic;

namespace TopSpeed.Tracks.Markers
{
    public sealed class TrackMarkerDefinition
    {
        private static readonly IReadOnlyDictionary<string, string> EmptyMetadata =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        public TrackMarkerDefinition(
            string id,
            TrackMarkerType type,
            float x,
            float z,
            string? name = null,
            string? shapeId = null,
            float? headingDegrees = null,
            IReadOnlyDictionary<string, string>? metadata = null)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("Marker id is required.", nameof(id));

            Id = id.Trim();
            Type = type;
            X = x;
            Z = z;
            var trimmedName = name?.Trim();
            Name = string.IsNullOrWhiteSpace(trimmedName) ? null : trimmedName;
            var trimmedShape = shapeId?.Trim();
            ShapeId = string.IsNullOrWhiteSpace(trimmedShape) ? null : trimmedShape;
            HeadingDegrees = headingDegrees;
            Metadata = NormalizeMetadata(metadata);
        }

        public string Id { get; }
        public TrackMarkerType Type { get; }
        public float X { get; }
        public float Z { get; }
        public string? Name { get; }
        public string? ShapeId { get; }
        public float? HeadingDegrees { get; }
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
