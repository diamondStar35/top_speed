using System;
using System.Collections.Generic;
using System.Globalization;
using System.Numerics;
using TopSpeed.Tracks.Areas;
using TopSpeed.Tracks.Topology;

namespace TopSpeed.Tracks.Beacons
{
    public readonly struct TrackBeaconCue
    {
        public TrackBeaconCue(TrackBeaconDefinition beacon, float distanceMeters, float? headingDeltaDegrees)
        {
            Beacon = beacon ?? throw new ArgumentNullException(nameof(beacon));
            DistanceMeters = distanceMeters;
            HeadingDeltaDegrees = headingDeltaDegrees;
        }

        public TrackBeaconDefinition Beacon { get; }
        public float DistanceMeters { get; }
        public float? HeadingDeltaDegrees { get; }
    }

    public sealed class TrackBeaconManager
    {
        private readonly Dictionary<string, TrackBeaconDefinition> _beaconsById;
        private readonly TrackAreaManager _areaManager;
        private readonly float _defaultActivationRadiusMeters;
        private readonly float _defaultPolylineWidthMeters;

        public TrackBeaconManager(
            IEnumerable<TrackBeaconDefinition> beacons,
            TrackAreaManager areaManager,
            float defaultActivationRadiusMeters = 12f,
            float defaultPolylineWidthMeters = 8f)
        {
            _areaManager = areaManager ?? throw new ArgumentNullException(nameof(areaManager));
            _defaultActivationRadiusMeters = Math.Max(0.1f, defaultActivationRadiusMeters);
            _defaultPolylineWidthMeters = Math.Max(0.1f, defaultPolylineWidthMeters);

            _beaconsById = new Dictionary<string, TrackBeaconDefinition>(StringComparer.OrdinalIgnoreCase);
            if (beacons == null)
                return;

            foreach (var beacon in beacons)
            {
                if (beacon == null)
                    continue;
                _beaconsById[beacon.Id] = beacon;
            }
        }

        public IReadOnlyCollection<TrackBeaconDefinition> Beacons => _beaconsById.Values;

        public bool TryGetBeacon(string id, out TrackBeaconDefinition beacon)
        {
            beacon = null!;
            if (string.IsNullOrWhiteSpace(id))
                return false;
            return _beaconsById.TryGetValue(id.Trim(), out beacon!);
        }

        public IReadOnlyList<TrackBeaconDefinition> FindActiveBeacons(
            Vector2 position,
            float? rangeMeters = null,
            TrackBeaconRole? role = null,
            TrackBeaconType? type = null)
        {
            if (_beaconsById.Count == 0)
                return Array.Empty<TrackBeaconDefinition>();

            var hits = new List<(TrackBeaconDefinition Beacon, float Distance)>();
            foreach (var beacon in _beaconsById.Values)
            {
                if (role.HasValue && beacon.Role != role.Value)
                    continue;
                if (type.HasValue && beacon.Type != type.Value)
                    continue;

                if (!IsActive(beacon, position, out var distance))
                    continue;
                if (rangeMeters.HasValue && distance > rangeMeters.Value)
                    continue;
                hits.Add((beacon, distance));
            }

            if (hits.Count == 0)
                return Array.Empty<TrackBeaconDefinition>();

            hits.Sort((left, right) => left.Distance.CompareTo(right.Distance));
            var results = new List<TrackBeaconDefinition>(hits.Count);
            foreach (var entry in hits)
                results.Add(entry.Beacon);
            return results;
        }

        public bool TryGetNearestCue(
            Vector2 position,
            float? headingDegrees,
            out TrackBeaconCue cue,
            float? rangeMeters = null,
            TrackBeaconRole? role = null,
            TrackBeaconType? type = null)
        {
            cue = default;
            if (_beaconsById.Count == 0)
                return false;

            TrackBeaconDefinition? best = null;
            var bestDistance = float.MaxValue;
            foreach (var beacon in _beaconsById.Values)
            {
                if (role.HasValue && beacon.Role != role.Value)
                    continue;
                if (type.HasValue && beacon.Type != type.Value)
                    continue;
                if (!IsActive(beacon, position, out var distance))
                    continue;
                if (rangeMeters.HasValue && distance > rangeMeters.Value)
                    continue;

                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    best = beacon;
                }
            }

            if (best == null)
                return false;

            var delta = GetHeadingDelta(best.OrientationDegrees, headingDegrees);
            cue = new TrackBeaconCue(best, bestDistance, delta);
            return true;
        }

        private bool IsActive(TrackBeaconDefinition beacon, Vector2 position, out float distanceMeters)
        {
            var beaconPos = new Vector2(beacon.X, beacon.Z);
            distanceMeters = Vector2.Distance(position, beaconPos);

            if (!string.IsNullOrWhiteSpace(beacon.ShapeId) &&
                _areaManager.TryGetShape(beacon.ShapeId!, out var shape))
            {
                if (shape.Type == ShapeType.Polyline)
                {
                    var width = ResolvePolylineWidth(beacon, shape);
                    return _areaManager.ContainsShape(shape.Id, position, width);
                }
                return _areaManager.ContainsShape(shape.Id, position);
            }

            var radius = beacon.ActivationRadiusMeters ?? _defaultActivationRadiusMeters;
            return distanceMeters <= radius;
        }

        private float ResolvePolylineWidth(TrackBeaconDefinition beacon, ShapeDefinition shape)
        {
            if (TryGetMetadataFloat(beacon.Metadata, out var width, "width", "activation_width", "lane_width"))
                return Math.Max(0.1f, width);

            if (beacon.ActivationRadiusMeters.HasValue)
                return Math.Max(0.1f, beacon.ActivationRadiusMeters.Value * 2f);

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
