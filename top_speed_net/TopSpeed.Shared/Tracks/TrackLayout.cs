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
        public TrackGeometrySpec Geometry { get; }
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
            IReadOnlyList<TrackMarker>? markers = null)
        {
            if (defaultWidthMeters <= 0f)
                throw new ArgumentOutOfRangeException(nameof(defaultWidthMeters));

            Geometry = geometry ?? throw new ArgumentNullException(nameof(geometry));
            Weather = weather;
            Ambience = ambience;
            DefaultSurface = defaultSurface;
            DefaultNoise = defaultNoise;
            DefaultWidthMeters = defaultWidthMeters;
            Metadata = metadata ?? new TrackLayoutMetadata();
            SurfaceZones = surfaceZones ?? Array.Empty<TrackZone<TrackSurface>>();
            NoiseZones = noiseZones ?? Array.Empty<TrackZone<TrackNoise>>();
            WidthZones = widthZones ?? Array.Empty<TrackWidthZone>();
            SpeedLimitZones = speedLimitZones ?? Array.Empty<TrackSpeedLimitZone>();
            Markers = markers ?? Array.Empty<TrackMarker>();
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
}
