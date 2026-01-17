using System;
using System.Collections.Generic;
using System.Numerics;
using TopSpeed.Tracks.Topology;

namespace TopSpeed.Tracks.Areas
{
    public sealed class TrackAreaManager
    {
        private readonly Dictionary<string, ShapeDefinition> _shapes;
        private readonly Dictionary<string, TrackAreaDefinition> _areasById;
        private readonly List<TrackAreaDefinition> _areas;

        public TrackAreaManager(IEnumerable<ShapeDefinition> shapes, IEnumerable<TrackAreaDefinition> areas)
        {
            _shapes = new Dictionary<string, ShapeDefinition>(StringComparer.OrdinalIgnoreCase);
            _areasById = new Dictionary<string, TrackAreaDefinition>(StringComparer.OrdinalIgnoreCase);
            _areas = new List<TrackAreaDefinition>();

            if (shapes != null)
            {
                foreach (var shape in shapes)
                    AddShape(shape);
            }

            if (areas != null)
            {
                foreach (var area in areas)
                    AddArea(area);
            }
        }

        public IReadOnlyList<TrackAreaDefinition> Areas => _areas;

        public void AddShape(ShapeDefinition shape)
        {
            if (shape == null)
                throw new ArgumentNullException(nameof(shape));
            _shapes[shape.Id] = shape;
        }

        public void AddArea(TrackAreaDefinition area)
        {
            if (area == null)
                throw new ArgumentNullException(nameof(area));
            _areasById[area.Id] = area;
            _areas.Add(area);
        }

        public bool TryGetShape(string id, out ShapeDefinition shape)
        {
            shape = null!;
            if (string.IsNullOrWhiteSpace(id))
                return false;
            return _shapes.TryGetValue(id.Trim(), out shape!);
        }

        public bool TryGetArea(string id, out TrackAreaDefinition area)
        {
            area = null!;
            if (string.IsNullOrWhiteSpace(id))
                return false;
            return _areasById.TryGetValue(id.Trim(), out area!);
        }

        public IReadOnlyList<TrackAreaDefinition> FindAreasContaining(Vector2 position)
        {
            if (_areas.Count == 0)
                return Array.Empty<TrackAreaDefinition>();

            var hits = new List<TrackAreaDefinition>();
            foreach (var area in _areas)
            {
                if (Contains(area, position))
                    hits.Add(area);
            }
            return hits;
        }

        public bool Contains(TrackAreaDefinition area, Vector2 position)
        {
            if (area == null)
                return false;
            if (!TryGetShape(area.ShapeId, out var shape))
                return false;
            return Contains(shape, position, area.WidthMeters);
        }

        public bool ContainsShape(string shapeId, Vector2 position, float? widthMeters = null)
        {
            if (!TryGetShape(shapeId, out var shape))
                return false;
            return Contains(shape, position, widthMeters);
        }

        private static bool Contains(ShapeDefinition shape, Vector2 position, float? widthMeters)
        {
            switch (shape.Type)
            {
                case ShapeType.Rectangle:
                    return ContainsRectangle(shape, position);
                case ShapeType.Circle:
                    return ContainsCircle(shape, position);
                case ShapeType.Polygon:
                    return ContainsPolygon(shape.Points, position);
                case ShapeType.Polyline:
                    return ContainsPolyline(shape.Points, position, widthMeters);
                default:
                    return false;
            }
        }

        private static bool ContainsRectangle(ShapeDefinition shape, Vector2 position)
        {
            var minX = shape.X;
            var minZ = shape.Z;
            var maxX = shape.X + shape.Width;
            var maxZ = shape.Z + shape.Height;
            return position.X >= minX && position.X <= maxX &&
                   position.Y >= minZ && position.Y <= maxZ;
        }

        private static bool ContainsCircle(ShapeDefinition shape, Vector2 position)
        {
            var dx = position.X - shape.X;
            var dz = position.Y - shape.Z;
            return (dx * dx + dz * dz) <= (shape.Radius * shape.Radius);
        }

        private static bool ContainsPolygon(IReadOnlyList<Vector2> points, Vector2 position)
        {
            if (points == null || points.Count < 3)
                return false;

            var inside = false;
            var j = points.Count - 1;
            for (var i = 0; i < points.Count; i++)
            {
                var xi = points[i].X;
                var zi = points[i].Y;
                var xj = points[j].X;
                var zj = points[j].Y;

                var intersect = ((zi > position.Y) != (zj > position.Y)) &&
                                (position.X < (xj - xi) * (position.Y - zi) / (zj - zi + float.Epsilon) + xi);
                if (intersect)
                    inside = !inside;
                j = i;
            }

            return inside;
        }

        private static bool ContainsPolyline(IReadOnlyList<Vector2> points, Vector2 position, float? widthMeters)
        {
            if (points == null || points.Count < 2)
                return false;

            var width = widthMeters.GetValueOrDefault();
            if (width <= 0f)
                return false;

            var radius = width * 0.5f;
            var radiusSq = radius * radius;
            for (var i = 0; i < points.Count - 1; i++)
            {
                if (DistanceToSegmentSquared(points[i], points[i + 1], position) <= radiusSq)
                    return true;
            }
            return false;
        }

        private static float DistanceToSegmentSquared(Vector2 a, Vector2 b, Vector2 p)
        {
            var ab = b - a;
            var ap = p - a;
            var abLenSq = Vector2.Dot(ab, ab);
            if (abLenSq <= float.Epsilon)
                return Vector2.Dot(ap, ap);

            var t = Vector2.Dot(ap, ab) / abLenSq;
            if (t < 0f)
                t = 0f;
            else if (t > 1f)
                t = 1f;

            var closest = a + ab * t;
            var delta = p - closest;
            return Vector2.Dot(delta, delta);
        }
    }
}
