using System;
using System.Collections.Generic;
using System.Globalization;
using System.Numerics;
using TopSpeed.Tracks.Areas;
using TopSpeed.Tracks.Topology;

namespace TopSpeed.Tracks.Markers
{
    public sealed class TrackMarkerManager
    {
        private readonly Dictionary<string, TrackMarkerDefinition> _markersById;
        private readonly TrackAreaManager _areaManager;
        private readonly float _defaultRangeMeters;
        private readonly float _defaultPolylineWidthMeters;

        public TrackMarkerManager(
            IEnumerable<TrackMarkerDefinition> markers,
            TrackAreaManager areaManager,
            float defaultRangeMeters = 5f,
            float defaultPolylineWidthMeters = 8f)
        {
            _areaManager = areaManager ?? throw new ArgumentNullException(nameof(areaManager));
            _defaultRangeMeters = Math.Max(0.1f, defaultRangeMeters);
            _defaultPolylineWidthMeters = Math.Max(0.1f, defaultPolylineWidthMeters);

            _markersById = new Dictionary<string, TrackMarkerDefinition>(StringComparer.OrdinalIgnoreCase);
            if (markers == null)
                return;

            foreach (var marker in markers)
            {
                if (marker == null)
                    continue;
                _markersById[marker.Id] = marker;
            }
        }

        public IReadOnlyCollection<TrackMarkerDefinition> Markers => _markersById.Values;

        public bool TryGetMarker(string id, out TrackMarkerDefinition marker)
        {
            marker = null!;
            if (string.IsNullOrWhiteSpace(id))
                return false;
            return _markersById.TryGetValue(id.Trim(), out marker!);
        }

        public IReadOnlyList<TrackMarkerDefinition> FindMarkersInRange(
            Vector2 position,
            float? rangeMeters = null,
            TrackMarkerType? type = null)
        {
            if (_markersById.Count == 0)
                return Array.Empty<TrackMarkerDefinition>();

            var hits = new List<(TrackMarkerDefinition Marker, float Distance)>();
            var maxRange = rangeMeters ?? _defaultRangeMeters;

            foreach (var marker in _markersById.Values)
            {
                if (type.HasValue && marker.Type != type.Value)
                    continue;
                if (!IsActive(marker, position, maxRange, out var distance))
                    continue;
                hits.Add((marker, distance));
            }

            if (hits.Count == 0)
                return Array.Empty<TrackMarkerDefinition>();

            hits.Sort((left, right) => left.Distance.CompareTo(right.Distance));
            var results = new List<TrackMarkerDefinition>(hits.Count);
            foreach (var entry in hits)
                results.Add(entry.Marker);
            return results;
        }

        public bool TryGetNearestMarker(
            Vector2 position,
            float? headingDegrees,
            out TrackMarkerDefinition marker,
            out float distanceMeters,
            out float? headingDeltaDegrees,
            float? rangeMeters = null,
            TrackMarkerType? type = null)
        {
            marker = null!;
            distanceMeters = 0f;
            headingDeltaDegrees = null;
            if (_markersById.Count == 0)
                return false;

            TrackMarkerDefinition? best = null;
            var bestDistance = float.MaxValue;
            var maxRange = rangeMeters ?? _defaultRangeMeters;

            foreach (var entry in _markersById.Values)
            {
                if (type.HasValue && entry.Type != type.Value)
                    continue;
                if (!IsActive(entry, position, maxRange, out var distance))
                    continue;

                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    best = entry;
                }
            }

            if (best == null)
                return false;

            marker = best;
            distanceMeters = bestDistance;
            headingDeltaDegrees = GetHeadingDelta(best.HeadingDegrees, headingDegrees);
            return true;
        }

        private bool IsActive(TrackMarkerDefinition marker, Vector2 position, float rangeMeters, out float distanceMeters)
        {
            var markerPos = new Vector2(marker.X, marker.Z);
            distanceMeters = Vector2.Distance(position, markerPos);

            if (!string.IsNullOrWhiteSpace(marker.ShapeId) &&
                _areaManager.TryGetShape(marker.ShapeId!, out var shape))
            {
                if (shape.Type == ShapeType.Polyline)
                {
                    var width = ResolvePolylineWidth(marker, shape);
                    return _areaManager.ContainsShape(shape.Id, position, width);
                }
                return _areaManager.ContainsShape(shape.Id, position);
            }

            return distanceMeters <= rangeMeters;
        }

        private float ResolvePolylineWidth(TrackMarkerDefinition marker, ShapeDefinition shape)
        {
            if (TryGetMetadataFloat(marker.Metadata, out var width, "width", "activation_width", "lane_width"))
                return Math.Max(0.1f, width);

            return _defaultPolylineWidthMeters;
        }

        private static float? GetHeadingDelta(float? targetHeadingDegrees, float? headingDegrees)
        {
            if (!targetHeadingDegrees.HasValue || !headingDegrees.HasValue)
                return null;
            var diff = Math.Abs(NormalizeDegrees(headingDegrees.Value - targetHeadingDegrees.Value));
            return diff > 180f ? 360f - diff : diff;
        }

        private static float NormalizeDegrees(float degrees)
        {
            var result = degrees % 360f;
            if (result < 0f)
                result += 360f;
            return result;
        }

        private static bool TryGetMetadataFloat(
            IReadOnlyDictionary<string, string> metadata,
            out float value,
            params string[] keys)
        {
            value = 0f;
            if (metadata == null || metadata.Count == 0)
                return false;

            foreach (var key in keys)
            {
                if (!metadata.TryGetValue(key, out var raw))
                    continue;
                if (float.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out value))
                    return true;
            }
            return false;
        }
    }
}
