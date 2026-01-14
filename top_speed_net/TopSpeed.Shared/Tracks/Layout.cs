using System;
using System.Collections.Generic;
using TopSpeed.Data;

namespace TopSpeed.Tracks.Geometry
{
    public sealed class TrackLayoutMetadata
    {
        public string? Name { get; }
        public string? ShortName { get; }
        public string? Description { get; }
        public string? Author { get; }
        public string? Version { get; }
        public string? Source { get; }
        public IReadOnlyList<string> Tags { get; }

        public TrackLayoutMetadata(
            string? name = null,
            string? shortName = null,
            string? description = null,
            string? author = null,
            string? version = null,
            string? source = null,
            IReadOnlyList<string>? tags = null)
        {
            Name = Normalize(name);
            ShortName = Normalize(shortName);
            Description = Normalize(description);
            Author = Normalize(author);
            Version = Normalize(version);
            Source = Normalize(source);
            Tags = tags ?? Array.Empty<string>();
        }

        private static string? Normalize(string? value)
        {
            var trimmed = value?.Trim();
            return string.IsNullOrWhiteSpace(trimmed) ? null : trimmed;
        }
    }

    public readonly struct TrackWidthZone
    {
        public float StartMeters { get; }
        public float EndMeters { get; }
        public float WidthMeters { get; }
        public float ShoulderLeftMeters { get; }
        public float ShoulderRightMeters { get; }

        public TrackWidthZone(float startMeters, float endMeters, float widthMeters, float shoulderLeftMeters = 0f, float shoulderRightMeters = 0f)
        {
            if (!IsFinite(startMeters))
                throw new ArgumentOutOfRangeException(nameof(startMeters));
            if (!IsFinite(endMeters))
                throw new ArgumentOutOfRangeException(nameof(endMeters));
            if (endMeters < startMeters)
                throw new ArgumentException("Width zone end must be >= start.", nameof(endMeters));
            if (!IsFinite(widthMeters) || widthMeters <= 0f)
                throw new ArgumentOutOfRangeException(nameof(widthMeters));
            if (!IsFinite(shoulderLeftMeters) || shoulderLeftMeters < 0f)
                throw new ArgumentOutOfRangeException(nameof(shoulderLeftMeters));
            if (!IsFinite(shoulderRightMeters) || shoulderRightMeters < 0f)
                throw new ArgumentOutOfRangeException(nameof(shoulderRightMeters));

            StartMeters = startMeters;
            EndMeters = endMeters;
            WidthMeters = widthMeters;
            ShoulderLeftMeters = shoulderLeftMeters;
            ShoulderRightMeters = shoulderRightMeters;
        }

        public bool Contains(float s)
        {
            return s >= StartMeters && s < EndMeters;
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }
    }

    public readonly struct TrackSpeedLimitZone
    {
        public float StartMeters { get; }
        public float EndMeters { get; }
        public float MaxSpeedKph { get; }

        public TrackSpeedLimitZone(float startMeters, float endMeters, float maxSpeedKph)
        {
            if (!IsFinite(startMeters))
                throw new ArgumentOutOfRangeException(nameof(startMeters));
            if (!IsFinite(endMeters))
                throw new ArgumentOutOfRangeException(nameof(endMeters));
            if (endMeters < startMeters)
                throw new ArgumentException("Speed limit end must be >= start.", nameof(endMeters));
            if (!IsFinite(maxSpeedKph) || maxSpeedKph <= 0f)
                throw new ArgumentOutOfRangeException(nameof(maxSpeedKph));

            StartMeters = startMeters;
            EndMeters = endMeters;
            MaxSpeedKph = maxSpeedKph;
        }

        public bool Contains(float s)
        {
            return s >= StartMeters && s < EndMeters;
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }
    }

    public readonly struct TrackMarker
    {
        public string Name { get; }
        public float PositionMeters { get; }

        public TrackMarker(string name, float positionMeters)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Marker name is required.", nameof(name));
            if (!IsFinite(positionMeters))
                throw new ArgumentOutOfRangeException(nameof(positionMeters));

            Name = name.Trim();
            PositionMeters = positionMeters;
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }
    }

    public sealed class TrackLayout
    {
        public TrackLayoutMetadata Metadata { get; }
        public TrackGraph Graph { get; }
        public TrackGraphRoute PrimaryRoute { get; }
        public TrackGeometrySpec Geometry => _primaryRouteGeometry;
        public TrackWeather Weather { get; }
        public TrackAmbience Ambience { get; }
        public TrackSurface DefaultSurface { get; }
        public TrackNoise DefaultNoise { get; }
        public float DefaultWidthMeters { get; }
        public IReadOnlyList<TrackZone<TrackSurface>> SurfaceZones { get; }
        public IReadOnlyList<TrackZone<TrackNoise>> NoiseZones { get; }
        public IReadOnlyList<TrackWidthZone> WidthZones { get; }
        public IReadOnlyList<TrackSpeedLimitZone> SpeedLimitZones { get; }      
        public IReadOnlyList<TrackMarker> Markers { get; }
        public IReadOnlyList<TrackStartFinishSubgraph> StartFinishSubgraphs { get; }
        public float PrimaryRouteLengthMeters => _primaryRouteLength;

        private readonly TrackGeometrySpec _primaryRouteGeometry;
        private readonly TrackGraphEdge[] _primaryRouteEdges;
        private readonly float[] _primaryRouteEdgeStart;
        private readonly float _primaryRouteLength;

        public TrackLayout(
            TrackGeometrySpec geometry,
            TrackWeather weather,
            TrackAmbience ambience,
            TrackSurface defaultSurface,
            TrackNoise defaultNoise,
            float defaultWidthMeters,
            TrackLayoutMetadata? metadata = null,
            IReadOnlyList<TrackZone<TrackSurface>>? surfaceZones = null,        
            IReadOnlyList<TrackZone<TrackNoise>>? noiseZones = null,
            IReadOnlyList<TrackWidthZone>? widthZones = null,
            IReadOnlyList<TrackSpeedLimitZone>? speedLimitZones = null,
            IReadOnlyList<TrackMarker>? markers = null,
            IReadOnlyList<TrackStartFinishSubgraph>? startFinishSubgraphs = null)
            : this(
                CreateSingleLoopGraph(
                    geometry,
                    defaultSurface,
                    defaultNoise,
                    defaultWidthMeters,
                    weather,
                    ambience,
                    surfaceZones,
                    noiseZones,
                    widthZones,
                    speedLimitZones,
                    markers),
                weather,
                ambience,
                defaultSurface,
                defaultNoise,
                defaultWidthMeters,
                metadata,
                startFinishSubgraphs)
        {
        }

        public TrackLayout(
            TrackGraph graph,
            TrackWeather weather,
            TrackAmbience ambience,
            TrackSurface defaultSurface,
            TrackNoise defaultNoise,
            float defaultWidthMeters,
            TrackLayoutMetadata? metadata = null,
            IReadOnlyList<TrackStartFinishSubgraph>? startFinishSubgraphs = null)
        {
            if (defaultWidthMeters <= 0f)
                throw new ArgumentOutOfRangeException(nameof(defaultWidthMeters));

            Graph = graph ?? throw new ArgumentNullException(nameof(graph));
            Weather = weather;
            Ambience = ambience;
            DefaultSurface = defaultSurface;
            DefaultNoise = defaultNoise;
            DefaultWidthMeters = defaultWidthMeters;
            Metadata = metadata ?? new TrackLayoutMetadata();
            StartFinishSubgraphs = startFinishSubgraphs ?? Array.Empty<TrackStartFinishSubgraph>();

            PrimaryRoute = graph.PrimaryRoute;
            _primaryRouteEdges = ResolveRouteEdges(graph, PrimaryRoute);
            _primaryRouteEdgeStart = BuildEdgeStart(_primaryRouteEdges, out _primaryRouteLength);
            _primaryRouteGeometry = BuildRouteGeometry(_primaryRouteEdges, PrimaryRoute.IsLoop);

            SurfaceZones = BuildRouteZones(_primaryRouteEdges, _primaryRouteEdgeStart, profile => profile.SurfaceZones);
            NoiseZones = BuildRouteZones(_primaryRouteEdges, _primaryRouteEdgeStart, profile => profile.NoiseZones);
            WidthZones = BuildRouteWidthZones(_primaryRouteEdges, _primaryRouteEdgeStart);
            SpeedLimitZones = BuildRouteSpeedZones(_primaryRouteEdges, _primaryRouteEdgeStart);
            Markers = BuildRouteMarkers(_primaryRouteEdges, _primaryRouteEdgeStart);
        }

        public TrackSurface SurfaceAt(float sMeters)
        {
            var edge = ResolvePrimaryEdge(sMeters, out var localS);
            return edge.Profile.SurfaceAt(localS);
        }

        public TrackNoise NoiseAt(float sMeters)
        {
            var edge = ResolvePrimaryEdge(sMeters, out var localS);
            return edge.Profile.NoiseAt(localS);
        }

        public float WidthAt(float sMeters)
        {
            var edge = ResolvePrimaryEdge(sMeters, out var localS);
            return edge.Profile.WidthAt(localS);
        }

        public bool TryGetSpeedLimit(float sMeters, out float maxSpeedKph)
        {
            var edge = ResolvePrimaryEdge(sMeters, out var localS);
            return edge.Profile.TryGetSpeedLimit(localS, out maxSpeedKph);
        }

        public TrackGraphEdge ResolvePrimaryEdge(float sMeters, out float localS)
        {
            if (_primaryRouteEdges.Length == 0)
            {
                if (Graph.Edges.Count == 0)
                    throw new InvalidOperationException("Cannot resolve edge: layout has no edges.");
                localS = 0f;
                return Graph.Edges[0];
            }

            var s = ResolveRouteDistance(sMeters);
            var index = FindEdgeIndex(s);
            localS = s - _primaryRouteEdgeStart[index];
            return _primaryRouteEdges[index];
        }

        public void ResolvePrimaryEdgeBounds(
            float sMeters,
            out TrackGraphEdge edge,
            out float localS,
            out float edgeStart,
            out float edgeEnd)
        {
            if (_primaryRouteEdges.Length == 0)
            {
                if (Graph.Edges.Count == 0)
                    throw new InvalidOperationException("Cannot resolve edge: layout has no edges.");
                edge = Graph.Edges[0];
                localS = 0f;
                edgeStart = 0f;
                edgeEnd = edge.LengthMeters;
                return;
            }

            var s = ResolveRouteDistance(sMeters);
            var index = FindEdgeIndex(s);
            edge = _primaryRouteEdges[index];
            edgeStart = _primaryRouteEdgeStart[index];
            edgeEnd = edgeStart + edge.LengthMeters;
            localS = s - edgeStart;
        }

        private float ResolveRouteDistance(float sMeters)
        {
            if (_primaryRouteLength <= 0f)
                return 0f;

            if (!PrimaryRoute.IsLoop)
                return Math.Max(0f, Math.Min(_primaryRouteLength, sMeters));

            var wrapped = sMeters % _primaryRouteLength;
            if (wrapped < 0f)
                wrapped += _primaryRouteLength;
            if (wrapped == _primaryRouteLength)
                return 0f;
            return wrapped;
        }

        private int FindEdgeIndex(float sMeters)
        {
            var index = Array.BinarySearch(_primaryRouteEdgeStart, sMeters);
            if (index >= 0)
                return index >= _primaryRouteEdges.Length ? _primaryRouteEdges.Length - 1 : index;

            index = ~index - 1;
            if (index < 0)
                index = 0;
            if (index >= _primaryRouteEdges.Length)
                index = _primaryRouteEdges.Length - 1;
            return index;
        }

        private static TrackGraphEdge[] ResolveRouteEdges(TrackGraph graph, TrackGraphRoute route)
        {
            var edges = new TrackGraphEdge[route.EdgeIds.Count];
            for (var i = 0; i < route.EdgeIds.Count; i++)
                edges[i] = graph.GetEdge(route.EdgeIds[i]);
            return edges;
        }

        private static float[] BuildEdgeStart(TrackGraphEdge[] edges, out float lengthMeters)
        {
            var start = new float[edges.Length];
            var total = 0f;
            for (var i = 0; i < edges.Length; i++)
            {
                start[i] = total;
                total += edges[i].LengthMeters;
            }
            lengthMeters = total;
            return start;
        }

        private static TrackGeometrySpec BuildRouteGeometry(TrackGraphEdge[] edges, bool enforceClosure)
        {
            var spans = new List<TrackGeometrySpan>();
            var spacing = float.MaxValue;
            for (var i = 0; i < edges.Length; i++)
            {
                var geometry = edges[i].Geometry;
                if (geometry == null || geometry.Spans.Count == 0)
                    continue;
                spacing = Math.Min(spacing, geometry.SampleSpacingMeters);
                // Note: Route closure is determined by route.IsLoop (enforceClosure parameter),
                // not by individual edge geometry flags
                for (var s = 0; s < geometry.Spans.Count; s++)
                    spans.Add(geometry.Spans[s]);
            }

            if (spans.Count == 0)
                throw new InvalidOperationException("Primary route contains no geometry spans.");
            if (spacing <= 0f || float.IsInfinity(spacing))
                spacing = 1f;

            return new TrackGeometrySpec(spans, spacing, enforceClosure);
        }

        private static IReadOnlyList<TrackZone<T>> BuildRouteZones<T>(
            TrackGraphEdge[] edges,
            float[] edgeStart,
            Func<TrackEdgeProfile, IReadOnlyList<TrackZone<T>>> selector)
        {
            var zones = new List<TrackZone<T>>();
            for (var i = 0; i < edges.Length; i++)
            {
                var offset = edgeStart[i];
                var list = selector(edges[i].Profile);
                for (var j = 0; j < list.Count; j++)
                {
                    var zone = list[j];
                    zones.Add(new TrackZone<T>(
                        zone.StartMeters + offset,
                        zone.EndMeters + offset,
                        zone.Value));
                }
            }
            return zones.Count == 0 ? Array.Empty<TrackZone<T>>() : zones;
        }

        private static IReadOnlyList<TrackWidthZone> BuildRouteWidthZones(
            TrackGraphEdge[] edges,
            float[] edgeStart)
        {
            var zones = new List<TrackWidthZone>();
            for (var i = 0; i < edges.Length; i++)
            {
                var offset = edgeStart[i];
                var list = edges[i].Profile.WidthZones;
                for (var j = 0; j < list.Count; j++)
                {
                    var zone = list[j];
                    zones.Add(new TrackWidthZone(
                        zone.StartMeters + offset,
                        zone.EndMeters + offset,
                        zone.WidthMeters,
                        zone.ShoulderLeftMeters,
                        zone.ShoulderRightMeters));
                }
            }
            return zones.Count == 0 ? Array.Empty<TrackWidthZone>() : zones;
        }

        private static IReadOnlyList<TrackSpeedLimitZone> BuildRouteSpeedZones(
            TrackGraphEdge[] edges,
            float[] edgeStart)
        {
            var zones = new List<TrackSpeedLimitZone>();
            for (var i = 0; i < edges.Length; i++)
            {
                var offset = edgeStart[i];
                var list = edges[i].Profile.SpeedLimitZones;
                for (var j = 0; j < list.Count; j++)
                {
                    var zone = list[j];
                    zones.Add(new TrackSpeedLimitZone(
                        zone.StartMeters + offset,
                        zone.EndMeters + offset,
                        zone.MaxSpeedKph));
                }
            }
            return zones.Count == 0 ? Array.Empty<TrackSpeedLimitZone>() : zones;
        }

        private static IReadOnlyList<TrackMarker> BuildRouteMarkers(
            TrackGraphEdge[] edges,
            float[] edgeStart)
        {
            var markers = new List<TrackMarker>();
            for (var i = 0; i < edges.Length; i++)
            {
                var offset = edgeStart[i];
                var list = edges[i].Profile.Markers;
                for (var j = 0; j < list.Count; j++)
                {
                    var marker = list[j];
                    markers.Add(new TrackMarker(marker.Name, marker.PositionMeters + offset));
                }
            }
            return markers.Count == 0 ? Array.Empty<TrackMarker>() : markers;
        }

        private static TrackGraph CreateSingleLoopGraph(
            TrackGeometrySpec geometry,
            TrackSurface defaultSurface,
            TrackNoise defaultNoise,
            float defaultWidthMeters,
            TrackWeather defaultWeather,
            TrackAmbience defaultAmbience,
            IReadOnlyList<TrackZone<TrackSurface>>? surfaceZones,
            IReadOnlyList<TrackZone<TrackNoise>>? noiseZones,
            IReadOnlyList<TrackWidthZone>? widthZones,
            IReadOnlyList<TrackSpeedLimitZone>? speedLimitZones,
            IReadOnlyList<TrackMarker>? markers)
        {
            if (geometry == null)
                throw new ArgumentNullException(nameof(geometry));

            var profile = new TrackEdgeProfile(
                defaultSurface,
                defaultNoise,
                defaultWidthMeters,
                defaultWeather,
                defaultAmbience,
                surfaceZones,
                noiseZones,
                widthZones,
                speedLimitZones,
                markers);

            return TrackGraph.CreateSingleLoop(geometry, profile);
        }
    }
}
