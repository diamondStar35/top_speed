using System;
using System.Collections.Generic;

namespace TopSpeed.Data
{
    public enum TrackType
    {
        Straight = 0,
        EasyLeft = 1,
        Left = 2,
        HardLeft = 3,
        HairpinLeft = 4,
        EasyRight = 5,
        Right = 6,
        HardRight = 7,
        HairpinRight = 8
    }

    public enum TrackSurface
    {
        Asphalt = 0,
        Gravel = 1,
        Water = 2,
        Sand = 3,
        Snow = 4
    }

    public enum TrackNoise
    {
        NoNoise = 0,
        Crowd = 1,
        Ocean = 2,
        Runway = 3,
        Clock = 4,
        Jet = 5,
        Thunder = 6,
        Pile = 7,
        Construction = 8,
        River = 9,
        Helicopter = 10,
        Owl = 11
    }

    public enum TrackWeather
    {
        Sunny = 0,
        Rain = 1,
        Wind = 2,
        Storm = 3
    }

    public enum TrackAmbience
    {
        NoAmbience = 0,
        Desert = 1,
        Airport = 2
    }

    public enum SegmentPitPoint
    {
        PitEntry = 0,
        PitExit = 1
    }

    public readonly struct TrackDefinition
    {
        private static readonly IReadOnlyList<string> EmptySoundSources = Array.Empty<string>();
        private static readonly IReadOnlyDictionary<string, string> EmptyMetadata = new Dictionary<string, string>();

        public TrackType Type { get; }
        public TrackSurface Surface { get; }
        public TrackNoise Noise { get; }
        public float Length { get; }
        public string? SegmentId { get; }
        public float Width { get; }
        public float Height { get; }
        public string? WeatherProfileId { get; }
        public float WeatherTransitionSeconds { get; }
        public string? RoomId { get; }
        public TrackRoomOverrides? RoomOverrides { get; }
        public IReadOnlyList<string> SoundSourceIds { get; }
        public IReadOnlyDictionary<string, string> Metadata { get; }
        public SegmentPitPoint? PitPoint { get; }

        public TrackDefinition(TrackType type, TrackSurface surface, TrackNoise noise, float length)
            : this(type, surface, noise, length, null, 0f, 0f, null, 0f, null, null, null, null)
        {
        }

        public TrackDefinition(
            TrackType type,
            TrackSurface surface,
            TrackNoise noise,
            float length,
            string? segmentId,
            float width,
            float height,
            string? roomId,
            TrackRoomOverrides? roomOverrides,
            IReadOnlyList<string>? soundSourceIds,
            IReadOnlyDictionary<string, string>? metadata)
            : this(type, surface, noise, length, segmentId, width, height, null, 0f, roomId, roomOverrides, soundSourceIds, metadata)
        {
        }

        public TrackDefinition(
            TrackType type,
            TrackSurface surface,
            TrackNoise noise,
            float length,
            string? segmentId,
            float width,
            float height,
            string? weatherProfileId,
            float weatherTransitionSeconds,
            string? roomId,
            TrackRoomOverrides? roomOverrides,
            IReadOnlyList<string>? soundSourceIds,
            IReadOnlyDictionary<string, string>? metadata,
            SegmentPitPoint? pitPoint = null)
        {
            Type = type;
            Surface = surface;
            Noise = noise;
            Length = length;
            SegmentId = segmentId;
            Width = width;
            Height = height;
            WeatherProfileId = string.IsNullOrWhiteSpace(weatherProfileId) ? null : weatherProfileId?.Trim();
            WeatherTransitionSeconds = weatherTransitionSeconds < 0f ? 0f : weatherTransitionSeconds;
            RoomId = roomId;
            RoomOverrides = roomOverrides;
            SoundSourceIds = soundSourceIds ?? EmptySoundSources;
            Metadata = metadata ?? EmptyMetadata;
            PitPoint = pitPoint;
        }
    }

    public sealed class TrackData
    {
        private static readonly IReadOnlyDictionary<string, string> EmptyMetadata = new Dictionary<string, string>();
        private static readonly IReadOnlyDictionary<string, TrackRoomDefinition> EmptyRooms = new Dictionary<string, TrackRoomDefinition>();
        private static readonly IReadOnlyDictionary<string, TrackSoundSourceDefinition> EmptySounds = new Dictionary<string, TrackSoundSourceDefinition>();
        private static readonly IReadOnlyDictionary<string, TrackWeatherProfile> EmptyWeatherProfiles = new Dictionary<string, TrackWeatherProfile>(StringComparer.OrdinalIgnoreCase);

        public bool UserDefined { get; }
        public string? Name { get; }
        public string? Version { get; }
        public string DefaultWeatherProfileId { get; }
        public IReadOnlyDictionary<string, TrackWeatherProfile> WeatherProfiles { get; }
        public TrackWeather Weather => DefaultWeatherProfile.Kind;
        public TrackWeatherProfile DefaultWeatherProfile => ResolveWeatherProfile(DefaultWeatherProfileId);
        public TrackAmbience Ambience { get; }
        public TrackDefinition[] Definitions { get; }
        public IReadOnlyDictionary<string, string> Metadata { get; }
        public IReadOnlyDictionary<string, TrackRoomDefinition> RoomProfiles { get; }
        public IReadOnlyDictionary<string, TrackSoundSourceDefinition> SoundSources { get; }
        public string? SourcePath { get; }
        public int Length => Definitions.Length;
        public byte Laps { get; set; }

        /// <summary>
        /// Whether this track has a pit area. True unless the <c>[meta]</c> section explicitly sets
        /// <c>pit_area = false</c>. A track with a pit area uses its defined pit entry/exit segments,
        /// or falls back to the start/finish line when none are defined; a track without one cannot
        /// pit at all (fuel/tire models that need pitting should be warned about / disabled).
        /// </summary>
        public bool HasPitArea => !MetadataDisablesPitArea(Metadata);

        /// <summary>Whether <c>pit_area</c> in the metadata is an explicit false token.</summary>
        internal static bool MetadataDisablesPitArea(IReadOnlyDictionary<string, string>? metadata)
        {
            if (metadata == null || !metadata.TryGetValue("pit_area", out var raw) || raw == null)
                return false;
            switch (raw.Trim().ToLowerInvariant())
            {
                case "false":
                case "0":
                case "no":
                case "off":
                    return true;
                default:
                    return false;
            }
        }

        public TrackData(
            bool userDefined,
            TrackWeather weather,
            TrackAmbience ambience,
            TrackDefinition[] definitions,
            byte laps = 0,
            string? name = null,
            string? version = null,
            IReadOnlyDictionary<string, string>? metadata = null,
            IReadOnlyDictionary<string, TrackRoomDefinition>? roomProfiles = null,
            IReadOnlyDictionary<string, TrackSoundSourceDefinition>? soundSources = null,
            string? sourcePath = null)
            : this(
                userDefined,
                TrackWeatherProfile.DefaultProfileId,
                new Dictionary<string, TrackWeatherProfile>(StringComparer.OrdinalIgnoreCase)
                {
                    [TrackWeatherProfile.DefaultProfileId] = TrackWeatherProfile.CreatePreset(TrackWeatherProfile.DefaultProfileId, weather)
                },
                ambience,
                definitions,
                laps,
                name,
                version,
                metadata,
                roomProfiles,
                soundSources,
                sourcePath)
        {
        }

        public TrackData(
            bool userDefined,
            string defaultWeatherProfileId,
            IReadOnlyDictionary<string, TrackWeatherProfile>? weatherProfiles,
            TrackAmbience ambience,
            TrackDefinition[] definitions,
            byte laps = 0,
            string? name = null,
            string? version = null,
            IReadOnlyDictionary<string, string>? metadata = null,
            IReadOnlyDictionary<string, TrackRoomDefinition>? roomProfiles = null,
            IReadOnlyDictionary<string, TrackSoundSourceDefinition>? soundSources = null,
            string? sourcePath = null)
        {
            UserDefined = userDefined;
            WeatherProfiles = weatherProfiles ?? EmptyWeatherProfiles;
            var trimmedWeatherId = defaultWeatherProfileId?.Trim();
            DefaultWeatherProfileId = string.IsNullOrWhiteSpace(trimmedWeatherId)
                ? TrackWeatherProfile.DefaultProfileId
                : trimmedWeatherId!;
            Ambience = ambience;
            Definitions = definitions;
            Laps = laps;
            var trimmedName = name?.Trim();
            Name = string.IsNullOrWhiteSpace(trimmedName) ? null : trimmedName;
            var trimmedVersion = version?.Trim();
            Version = string.IsNullOrWhiteSpace(trimmedVersion) ? null : trimmedVersion;
            Metadata = metadata ?? EmptyMetadata;
            RoomProfiles = roomProfiles ?? EmptyRooms;
            SoundSources = soundSources ?? EmptySounds;
            var trimmedSourcePath = sourcePath?.Trim();
            SourcePath = string.IsNullOrWhiteSpace(trimmedSourcePath) ? null : trimmedSourcePath;
        }

        public bool TryGetWeatherProfile(string? profileId, out TrackWeatherProfile profile)
        {
            var id = string.IsNullOrWhiteSpace(profileId) ? DefaultWeatherProfileId : profileId!.Trim();
            if (WeatherProfiles.TryGetValue(id, out profile))
                return true;

            if (WeatherProfiles.TryGetValue(DefaultWeatherProfileId, out profile))
                return true;

            profile = TrackWeatherProfile.CreatePreset(TrackWeatherProfile.DefaultProfileId, TrackWeather.Sunny);
            return false;
        }

        public TrackWeatherProfile ResolveWeatherProfile(string? profileId)
        {
            return TryGetWeatherProfile(profileId, out var profile)
                ? profile
                : TrackWeatherProfile.CreatePreset(TrackWeatherProfile.DefaultProfileId, TrackWeather.Sunny);
        }

        public TrackData WithLaps(byte laps)
        {
            return new TrackData(
                UserDefined,
                DefaultWeatherProfileId,
                WeatherProfiles,
                Ambience,
                Definitions,
                laps,
                Name,
                Version,
                Metadata,
                RoomProfiles,
                SoundSources,
                SourcePath);
        }
    }
}
