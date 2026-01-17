using System;
using System.Collections.Generic;

namespace TopSpeed.Tracks.Beacons
{
    public sealed class TrackBeaconDefinition
    {
        private static readonly IReadOnlyDictionary<string, string> EmptyMetadata =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        public TrackBeaconDefinition(
            string id,
            TrackBeaconType type,
            float x,
            float z,
            string? name = null,
            string? nameSecondary = null,
            string? sectorId = null,
            string? shapeId = null,
            float? orientationDegrees = null,
            float? activationRadiusMeters = null,
            TrackBeaconRole role = TrackBeaconRole.Undefined,
            IReadOnlyDictionary<string, string>? metadata = null)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("Beacon id is required.", nameof(id));

            Id = id.Trim();
            Type = type;
            Role = role;
            X = x;
            Z = z;

            var trimmedName = name?.Trim();
            Name = string.IsNullOrWhiteSpace(trimmedName) ? null : trimmedName;
            var trimmedSecondary = nameSecondary?.Trim();
            NameSecondary = string.IsNullOrWhiteSpace(trimmedSecondary) ? null : trimmedSecondary;

            var trimmedSector = sectorId?.Trim();
            SectorId = string.IsNullOrWhiteSpace(trimmedSector) ? null : trimmedSector;
            var trimmedShape = shapeId?.Trim();
            ShapeId = string.IsNullOrWhiteSpace(trimmedShape) ? null : trimmedShape;

            OrientationDegrees = orientationDegrees;
            ActivationRadiusMeters = activationRadiusMeters.HasValue
                ? Math.Max(0.1f, activationRadiusMeters.Value)
                : null;
            Metadata = NormalizeMetadata(metadata);
        }

        public string Id { get; }
        public TrackBeaconType Type { get; }
        public TrackBeaconRole Role { get; }
        public float X { get; }
        public float Z { get; }
        public string? Name { get; }
        public string? NameSecondary { get; }
        public string? SectorId { get; }
        public string? ShapeId { get; }
        public float? OrientationDegrees { get; }
        public float? ActivationRadiusMeters { get; }
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
