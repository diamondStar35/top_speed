using System;
using System.Collections.Generic;
using System.Numerics;

namespace TopSpeed.Tracks.Topology
{
    public sealed class ShapeDefinition
    {
        public ShapeDefinition(
            string id,
            ShapeType type,
            float x = 0f,
            float z = 0f,
            float width = 0f,
            float height = 0f,
            float radius = 0f,
            IReadOnlyList<Vector2>? points = null)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("Shape id is required.", nameof(id));

            Id = id.Trim();
            Type = type;
            X = x;
            Z = z;
            Width = width;
            Height = height;
            Radius = radius;
            Points = points ?? Array.Empty<Vector2>();
        }

        public string Id { get; }
        public ShapeType Type { get; }
        public float X { get; }
        public float Z { get; }
        public float Width { get; }
        public float Height { get; }
        public float Radius { get; }
        public IReadOnlyList<Vector2> Points { get; }
    }
}
