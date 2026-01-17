using System;

namespace TopSpeed.Tracks.Topology
{
    public sealed class PathDefinition
    {
        public PathDefinition(
            string id,
            PathType type,
            string? shapeId,
            string? fromPortalId,
            string? toPortalId,
            float widthMeters,
            string? name = null)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("Path id is required.", nameof(id));

            Id = id.Trim();
            Type = type;
            ShapeId = string.IsNullOrWhiteSpace(shapeId) ? null : shapeId!.Trim();
            FromPortalId = string.IsNullOrWhiteSpace(fromPortalId) ? null : fromPortalId!.Trim();
            ToPortalId = string.IsNullOrWhiteSpace(toPortalId) ? null : toPortalId!.Trim();
            WidthMeters = widthMeters;
            var trimmedName = name?.Trim();
            Name = string.IsNullOrWhiteSpace(trimmedName) ? null : trimmedName;
        }

        public string Id { get; }
        public PathType Type { get; }
        public string? ShapeId { get; }
        public string? FromPortalId { get; }
        public string? ToPortalId { get; }
        public float WidthMeters { get; }
        public string? Name { get; }
    }
}
