using System;
using System.Collections.Generic;
using System.Numerics;

namespace TopSpeed.Data
{
    public static partial class TrackTsmParser
    {
        private struct SegmentBuilder
        {
            public string Id;
            public TrackType Type;
            public TrackSurface Surface;
            public TrackNoise Noise;
            public float Length;
            public float Width;
            public float Height;
            public string? WeatherProfileId;
            public float WeatherTransitionSeconds;
            public string? RoomId;
            public TrackRoomOverrides RoomOverrides;
            public IReadOnlyList<string> SoundSourceIds;
            public Dictionary<string, string> Metadata;
            public bool IsPitEntry;
            public bool IsPitExit;

            public static SegmentBuilder Create(string id)
            {
                return new SegmentBuilder
                {
                    Id = string.IsNullOrWhiteSpace(id) ? Guid.NewGuid().ToString("N") : id.Trim(),
                    Type = TrackType.Straight,
                    Surface = TrackSurface.Asphalt,
                    Noise = TrackNoise.NoNoise,
                    Length = 50.0f,
                    Width = 10.0f,
                    Height = 0f,
                    WeatherProfileId = null,
                    WeatherTransitionSeconds = 0f,
                    RoomId = null,
                    RoomOverrides = new TrackRoomOverrides(),
                    SoundSourceIds = Array.Empty<string>(),
                    Metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase),
                    IsPitEntry = false,
                    IsPitExit = false
                };
            }
        }

        private struct WeatherBuilder
        {
            public string Id;
            public TrackWeather Kind;
            public float LongitudinalWindMps;
            public float LateralWindMps;
            public float AirDensityKgPerM3;
            public float DraftingFactor;
            public float TemperatureC;
            public float Humidity;
            public float PressureKpa;
            public float VisibilityM;
            public float RainGain;
            public float WindGain;
            public float StormGain;

            public static WeatherBuilder Create(string id)
            {
                var profile = TrackWeatherProfile.CreatePreset(id, TrackWeather.Sunny);
                return new WeatherBuilder
                {
                    Id = profile.Id,
                    Kind = profile.Kind,
                    LongitudinalWindMps = profile.LongitudinalWindMps,
                    LateralWindMps = profile.LateralWindMps,
                    AirDensityKgPerM3 = profile.AirDensityKgPerM3,
                    DraftingFactor = profile.DraftingFactor,
                    TemperatureC = profile.TemperatureC,
                    Humidity = profile.Humidity,
                    PressureKpa = profile.PressureKpa,
                    VisibilityM = profile.VisibilityM,
                    RainGain = profile.RainGain,
                    WindGain = profile.WindGain,
                    StormGain = profile.StormGain
                };
            }

            public TrackWeatherProfile Build()
            {
                return new TrackWeatherProfile(
                    Id,
                    Kind,
                    LongitudinalWindMps,
                    LateralWindMps,
                    AirDensityKgPerM3,
                    DraftingFactor,
                    TemperatureC,
                    Humidity,
                    PressureKpa,
                    VisibilityM,
                    RainGain,
                    WindGain,
                    StormGain);
            }
        }

        private struct RoomBuilder
        {
            public string Id;
            public string? Name;
            public string PresetId;
            public float? ReverbTimeSeconds;
            public float? ReverbGain;
            public float? HfDecayRatio;
            public float? LateReverbGain;
            public float? Diffusion;
            public float? AirAbsorption;
            public float? OcclusionScale;
            public float? TransmissionScale;
            public float? OcclusionOverride;
            public float? TransmissionOverrideLow;
            public float? TransmissionOverrideMid;
            public float? TransmissionOverrideHigh;
            public float? AirAbsorptionOverrideLow;
            public float? AirAbsorptionOverrideMid;
            public float? AirAbsorptionOverrideHigh;

            public static RoomBuilder Create(string id)
            {
                var builder = new RoomBuilder
                {
                    Id = string.IsNullOrWhiteSpace(id) ? Guid.NewGuid().ToString("N") : id.Trim(),
                    Name = null,
                    PresetId = "outdoor_open"
                };
                return builder;
            }

            public void ApplyPreset(string presetId)
            {
                var normalized = presetId?.Trim();
                if (string.IsNullOrWhiteSpace(normalized))
                    return;
                if (!TrackRoomLibrary.IsPreset(normalized!))
                    return;
                PresetId = normalized!;
            }

            public TrackRoomDefinition Build()
            {
                if (!TrackRoomLibrary.TryGetPreset(PresetId, out var preset))
                    TrackRoomLibrary.TryGetPreset("outdoor_open", out preset);

                return new TrackRoomDefinition(
                    Id,
                    Name,
                    ReverbTimeSeconds ?? preset.ReverbTimeSeconds,
                    ReverbGain ?? preset.ReverbGain,
                    HfDecayRatio ?? preset.HfDecayRatio,
                    LateReverbGain ?? preset.LateReverbGain,
                    Diffusion ?? preset.Diffusion,
                    AirAbsorption ?? preset.AirAbsorption,
                    OcclusionScale ?? preset.OcclusionScale,
                    TransmissionScale ?? preset.TransmissionScale,
                    OcclusionOverride ?? preset.OcclusionOverride,
                    TransmissionOverrideLow ?? preset.TransmissionOverrideLow,
                    TransmissionOverrideMid ?? preset.TransmissionOverrideMid,
                    TransmissionOverrideHigh ?? preset.TransmissionOverrideHigh,
                    AirAbsorptionOverrideLow ?? preset.AirAbsorptionOverrideLow,
                    AirAbsorptionOverrideMid ?? preset.AirAbsorptionOverrideMid,
                    AirAbsorptionOverrideHigh ?? preset.AirAbsorptionOverrideHigh);
            }
        }

        private struct SoundBuilder
        {
            public string Id;
            public TrackSoundSourceType Type;
            public string? Path;
            public IReadOnlyList<string> VariantPaths;
            public IReadOnlyList<string> VariantSourceIds;
            public TrackSoundRandomMode RandomMode;
            public bool Loop;
            public float Volume;
            public bool Spatial;
            public bool AllowHrtf;
            public float FadeInSeconds;
            public float FadeOutSeconds;
            public float? CrossfadeSeconds;
            public float Pitch;
            public float Pan;
            public float? MinDistance;
            public float? MaxDistance;
            public float? Rolloff;
            public bool Global;
            public string? StartAreaId;
            public string? EndAreaId;
            public Vector3? StartPosition;
            public float? StartRadiusMeters;
            public Vector3? EndPosition;
            public float? EndRadiusMeters;
            public Vector3? Position;
            public float? SpeedMetersPerSecond;

            public static SoundBuilder Create(string id)
            {
                return new SoundBuilder
                {
                    Id = string.IsNullOrWhiteSpace(id) ? Guid.NewGuid().ToString("N") : id.Trim(),
                    Type = TrackSoundSourceType.Ambient,
                    Path = null,
                    VariantPaths = Array.Empty<string>(),
                    VariantSourceIds = Array.Empty<string>(),
                    RandomMode = TrackSoundRandomMode.OnStart,
                    Loop = true,
                    Volume = 1.0f,
                    Spatial = true,
                    AllowHrtf = true,
                    FadeInSeconds = 0f,
                    FadeOutSeconds = 0f,
                    CrossfadeSeconds = null,
                    Pitch = 1.0f,
                    Pan = 0f,
                    MinDistance = null,
                    MaxDistance = null,
                    Rolloff = null,
                    Global = false,
                    StartAreaId = null,
                    EndAreaId = null,
                    StartPosition = null,
                    StartRadiusMeters = null,
                    EndPosition = null,
                    EndRadiusMeters = null,
                    Position = null,
                    SpeedMetersPerSecond = null
                };
            }

            public TrackSoundSourceDefinition Build()
            {
                return new TrackSoundSourceDefinition(
                    Id,
                    Type,
                    Path,
                    VariantPaths,
                    VariantSourceIds,
                    RandomMode,
                    Loop,
                    Volume,
                    Spatial,
                    AllowHrtf,
                    FadeInSeconds,
                    FadeOutSeconds,
                    CrossfadeSeconds,
                    Pitch,
                    Pan,
                    MinDistance,
                    MaxDistance,
                    Rolloff,
                    Global,
                    StartAreaId,
                    EndAreaId,
                    StartPosition,
                    StartRadiusMeters,
                    EndPosition,
                    EndRadiusMeters,
                    Position,
                    SpeedMetersPerSecond);
            }
        }
    }
}
