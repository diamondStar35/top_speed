using System;
using System.Collections.Generic;
using System.Linq;
using TopSpeed.Data;

namespace TopSpeed.Tracks.Geometry
{
    public sealed class TrackGraphNode
    {
        public string Id { get; }
        public string? Name { get; }
        public string? ShortName { get; }
        public TrackIntersectionProfile? Intersection { get; }
        public IReadOnlyDictionary<string, string> Metadata { get; }

        public TrackGraphNode(
            string id,
            string? name = null,
            string? shortName = null,
            IReadOnlyDictionary<string, string>? metadata = null,
            TrackIntersectionProfile? intersection = null)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("Node id is required.", nameof(id));

            Id = id.Trim();
            var trimmedName = name?.Trim();
            Name = string.IsNullOrWhiteSpace(trimmedName) ? null : trimmedName;
            var trimmedShort = shortName?.Trim();
            ShortName = string.IsNullOrWhiteSpace(trimmedShort) ? null : trimmedShort;
            Intersection = intersection;
            Metadata = metadata ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }
    }

    public enum TrackTurnDirection
    {
        Unknown = 0,
        Left = 1,
        Right = 2,
        Straight = 3,
        UTurn = 4
    }

    public sealed class TrackEdgeProfile
    {
        public TrackSurface DefaultSurface { get; }
        public TrackNoise DefaultNoise { get; }
        public float DefaultWidthMeters { get; }
        public TrackWeather DefaultWeather { get; }
        public TrackAmbience DefaultAmbience { get; }
        public IReadOnlyList<TrackZone<TrackSurface>> SurfaceZones { get; }
        public IReadOnlyList<TrackZone<TrackNoise>> NoiseZones { get; }
        public IReadOnlyList<TrackWidthZone> WidthZones { get; }
        public IReadOnlyList<TrackSpeedLimitZone> SpeedLimitZones { get; }
        public IReadOnlyList<TrackMarker> Markers { get; }
        public IReadOnlyList<TrackWeatherZone> WeatherZones { get; }
        public IReadOnlyList<TrackAmbienceZone> AmbienceZones { get; }
        public IReadOnlyList<TrackHazardZone> Hazards { get; }
        public IReadOnlyList<TrackCheckpoint> Checkpoints { get; }
        public IReadOnlyList<TrackHitLaneZone> HitLanes { get; }
        public IReadOnlyList<string> AllowedVehicles { get; }
        public IReadOnlyList<TrackAudioEmitter> Emitters { get; }
        public IReadOnlyList<TrackTriggerZone> Triggers { get; }
        public IReadOnlyList<TrackBoundaryZone> BoundaryZones { get; }

        public TrackEdgeProfile(
            TrackSurface defaultSurface,
            TrackNoise defaultNoise,
            float defaultWidthMeters,
            TrackWeather defaultWeather,
            TrackAmbience defaultAmbience,
            IReadOnlyList<TrackZone<TrackSurface>>? surfaceZones = null,
            IReadOnlyList<TrackZone<TrackNoise>>? noiseZones = null,
            IReadOnlyList<TrackWidthZone>? widthZones = null,
            IReadOnlyList<TrackSpeedLimitZone>? speedLimitZones = null,
            IReadOnlyList<TrackMarker>? markers = null,
            IReadOnlyList<TrackWeatherZone>? weatherZones = null,
            IReadOnlyList<TrackAmbienceZone>? ambienceZones = null,
            IReadOnlyList<TrackHazardZone>? hazards = null,
            IReadOnlyList<TrackCheckpoint>? checkpoints = null,
            IReadOnlyList<TrackHitLaneZone>? hitLanes = null,
            IReadOnlyList<string>? allowedVehicles = null,
            IReadOnlyList<TrackAudioEmitter>? emitters = null,
            IReadOnlyList<TrackTriggerZone>? triggers = null,
            IReadOnlyList<TrackBoundaryZone>? boundaryZones = null)
        {
            if (defaultWidthMeters <= 0f)
                throw new ArgumentOutOfRangeException(nameof(defaultWidthMeters));

            DefaultSurface = defaultSurface;
            DefaultNoise = defaultNoise;
            DefaultWidthMeters = defaultWidthMeters;
            DefaultWeather = defaultWeather;
            DefaultAmbience = defaultAmbience;
            SurfaceZones = surfaceZones ?? Array.Empty<TrackZone<TrackSurface>>();
            NoiseZones = noiseZones ?? Array.Empty<TrackZone<TrackNoise>>();
            WidthZones = widthZones ?? Array.Empty<TrackWidthZone>();
            SpeedLimitZones = speedLimitZones ?? Array.Empty<TrackSpeedLimitZone>();
            Markers = markers ?? Array.Empty<TrackMarker>();
            WeatherZones = weatherZones ?? Array.Empty<TrackWeatherZone>();
            AmbienceZones = ambienceZones ?? Array.Empty<TrackAmbienceZone>();
            Hazards = hazards ?? Array.Empty<TrackHazardZone>();
            Checkpoints = checkpoints ?? Array.Empty<TrackCheckpoint>();
            HitLanes = hitLanes ?? Array.Empty<TrackHitLaneZone>();
            AllowedVehicles = allowedVehicles ?? Array.Empty<string>();
            Emitters = emitters ?? Array.Empty<TrackAudioEmitter>();
            Triggers = triggers ?? Array.Empty<TrackTriggerZone>();
            BoundaryZones = boundaryZones ?? Array.Empty<TrackBoundaryZone>();
        }

        public TrackSurface SurfaceAt(float sMeters)
        {
            for (var i = 0; i < SurfaceZones.Count; i++)
            {
                if (SurfaceZones[i].Contains(sMeters))
                    return SurfaceZones[i].Value;
            }
            return DefaultSurface;
        }

        public TrackNoise NoiseAt(float sMeters)
        {
            for (var i = 0; i < NoiseZones.Count; i++)
            {
                if (NoiseZones[i].Contains(sMeters))
                    return NoiseZones[i].Value;
            }
            return DefaultNoise;
        }

        public float WidthAt(float sMeters)
        {
            for (var i = 0; i < WidthZones.Count; i++)
            {
                if (WidthZones[i].Contains(sMeters))
                    return WidthZones[i].WidthMeters;
            }
            return DefaultWidthMeters;
        }

        public bool TryGetSpeedLimit(float sMeters, out float maxSpeedKph)
        {
            for (var i = 0; i < SpeedLimitZones.Count; i++)
            {
                if (SpeedLimitZones[i].Contains(sMeters))
                {
                    maxSpeedKph = SpeedLimitZones[i].MaxSpeedKph;
                    return true;
                }
            }
            maxSpeedKph = 0f;
            return false;
        }
    }

    public sealed class TrackGraphEdge
    {
        public string Id { get; }
        public string FromNodeId { get; }
        public string ToNodeId { get; }
        public string? Name { get; }
        public string? ShortName { get; }
        public TrackGeometrySpec Geometry { get; }
        public TrackEdgeProfile Profile { get; }
        public IReadOnlyDictionary<string, string> Metadata { get; }
        public IReadOnlyList<string> ConnectorFromEdgeIds { get; }
        public TrackTurnDirection TurnDirection { get; }
        public float LengthMeters { get; }
        public bool IsConnector => ConnectorFromEdgeIds.Count > 0 || TurnDirection != TrackTurnDirection.Unknown;

        public TrackGraphEdge(
            string id,
            string fromNodeId,
            string toNodeId,
            string? name,
            string? shortName,
            TrackGeometrySpec geometry,
            TrackEdgeProfile profile,
            IReadOnlyList<string>? connectorFromEdgeIds = null,
            TrackTurnDirection turnDirection = TrackTurnDirection.Unknown,
            IReadOnlyDictionary<string, string>? metadata = null)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("Edge id is required.", nameof(id));
            if (string.IsNullOrWhiteSpace(fromNodeId))
                throw new ArgumentException("From node id is required.", nameof(fromNodeId));
            if (string.IsNullOrWhiteSpace(toNodeId))
                throw new ArgumentException("To node id is required.", nameof(toNodeId));

            Id = id.Trim();
            FromNodeId = fromNodeId.Trim();
            ToNodeId = toNodeId.Trim();
            var trimmedName = name?.Trim();
            Name = string.IsNullOrWhiteSpace(trimmedName) ? null : trimmedName;
            var trimmedShort = shortName?.Trim();
            ShortName = string.IsNullOrWhiteSpace(trimmedShort) ? null : trimmedShort;
            Geometry = geometry ?? throw new ArgumentNullException(nameof(geometry));
            Profile = profile ?? throw new ArgumentNullException(nameof(profile));
            Metadata = metadata ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            ConnectorFromEdgeIds = connectorFromEdgeIds ?? Array.Empty<string>();
            TurnDirection = turnDirection;
            LengthMeters = geometry.TotalLengthMeters;
        }
    }

    public sealed class TrackGraphRoute
    {
        public string Id { get; }
        public IReadOnlyList<string> EdgeIds { get; }
        public bool IsLoop { get; }

        public TrackGraphRoute(string id, IReadOnlyList<string> edgeIds, bool isLoop)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("Route id is required.", nameof(id));
            if (edgeIds == null || edgeIds.Count == 0)
                throw new ArgumentException("Route must include at least one edge.", nameof(edgeIds));

            Id = id.Trim();
            EdgeIds = edgeIds;
            IsLoop = isLoop;
        }
    }

    public sealed class TrackGraph
    {
        private readonly Dictionary<string, TrackGraphNode> _nodeLookup;
        private readonly Dictionary<string, TrackGraphEdge> _edgeLookup;
        private readonly Dictionary<string, TrackGraphRoute> _routeLookup;

        public IReadOnlyList<TrackGraphNode> Nodes { get; }
        public IReadOnlyList<TrackGraphEdge> Edges { get; }
        public IReadOnlyList<TrackGraphRoute> Routes { get; }
        public string PrimaryRouteId { get; }
        public TrackGraphRoute PrimaryRoute { get; }

        public TrackGraph(
            IReadOnlyList<TrackGraphNode> nodes,
            IReadOnlyList<TrackGraphEdge> edges,
            IReadOnlyList<TrackGraphRoute> routes,
            string? primaryRouteId = null)
        {
            if (nodes == null)
                throw new ArgumentNullException(nameof(nodes));
            if (edges == null)
                throw new ArgumentNullException(nameof(edges));
            if (routes == null)
                throw new ArgumentNullException(nameof(routes));

            Nodes = nodes;
            Edges = edges;
            Routes = routes;

            _nodeLookup = new Dictionary<string, TrackGraphNode>(StringComparer.OrdinalIgnoreCase);
            foreach (var node in nodes)
            {
                if (_nodeLookup.ContainsKey(node.Id))
                    throw new ArgumentException($"Duplicate node id '{node.Id}'.", nameof(nodes));
                _nodeLookup[node.Id] = node;
            }

            _edgeLookup = new Dictionary<string, TrackGraphEdge>(StringComparer.OrdinalIgnoreCase);
            foreach (var edge in edges)
            {
                if (_edgeLookup.ContainsKey(edge.Id))
                    throw new ArgumentException($"Duplicate edge id '{edge.Id}'.", nameof(edges));
                if (!_nodeLookup.ContainsKey(edge.FromNodeId))
                    throw new ArgumentException($"Edge '{edge.Id}' references missing from-node '{edge.FromNodeId}'.", nameof(edges));
                if (!_nodeLookup.ContainsKey(edge.ToNodeId))
                    throw new ArgumentException($"Edge '{edge.Id}' references missing to-node '{edge.ToNodeId}'.", nameof(edges));
                _edgeLookup[edge.Id] = edge;
            }

            _routeLookup = new Dictionary<string, TrackGraphRoute>(StringComparer.OrdinalIgnoreCase);
            foreach (var route in routes)
            {
                if (_routeLookup.ContainsKey(route.Id))
                    throw new ArgumentException($"Duplicate route id '{route.Id}'.", nameof(routes));
                foreach (var edgeId in route.EdgeIds)
                {
                    if (!_edgeLookup.ContainsKey(edgeId))
                        throw new ArgumentException($"Route '{route.Id}' references missing edge '{edgeId}'.", nameof(routes));
                }

                // Validate edge continuity: each edge's ToNode must match the next edge's FromNode
                for (var i = 1; i < route.EdgeIds.Count; i++)
                {
                    var prevEdge = _edgeLookup[route.EdgeIds[i - 1]];
                    var currEdge = _edgeLookup[route.EdgeIds[i]];
                    if (!prevEdge.ToNodeId.Equals(currEdge.FromNodeId, StringComparison.OrdinalIgnoreCase))
                        throw new ArgumentException(
                            $"Route '{route.Id}' has discontinuous edges: edge '{prevEdge.Id}' ends at node '{prevEdge.ToNodeId}' " +
                            $"but edge '{currEdge.Id}' starts at node '{currEdge.FromNodeId}'.", nameof(routes));
                }

                _routeLookup[route.Id] = route;
            }

            if (string.IsNullOrWhiteSpace(primaryRouteId))
            {
                if (routes.Count == 0)
                    throw new ArgumentException("Graph must define at least one route.", nameof(routes));
                PrimaryRouteId = routes[0].Id;
            }
            else
            {
                PrimaryRouteId = primaryRouteId!.Trim();
            }

            if (!_routeLookup.TryGetValue(PrimaryRouteId, out var primaryRoute))
                throw new ArgumentException($"Primary route '{PrimaryRouteId}' not found.", nameof(primaryRouteId));

            PrimaryRoute = primaryRoute;
        }

        public TrackGraphEdge GetEdge(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("Edge id is required.", nameof(id));
            if (_edgeLookup.TryGetValue(id.Trim(), out var edge))
                return edge;
            throw new KeyNotFoundException($"Edge '{id}' not found.");
        }

        public bool TryGetEdge(string id, out TrackGraphEdge edge)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                edge = null!;
                return false;
            }
            return _edgeLookup.TryGetValue(id.Trim(), out edge!);
        }

        public static TrackGraph CreateSingleLoop(
            TrackGeometrySpec geometry,
            TrackEdgeProfile profile,
            string edgeId = "main",
            string nodeId = "loop",
            string routeId = "primary")
        {
            if (geometry == null)
                throw new ArgumentNullException(nameof(geometry));
            if (profile == null)
                throw new ArgumentNullException(nameof(profile));

            var node = new TrackGraphNode(nodeId);
            var edge = new TrackGraphEdge(edgeId, node.Id, node.Id, null, null, geometry, profile);
            var route = new TrackGraphRoute(routeId, new[] { edgeId }, isLoop: true);
            return new TrackGraph(
                new[] { node },
                new[] { edge },
                new[] { route },
                primaryRouteId: route.Id);
        }
    }
}
