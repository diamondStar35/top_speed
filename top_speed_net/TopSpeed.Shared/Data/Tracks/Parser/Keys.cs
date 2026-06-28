using System;

namespace TopSpeed.Data
{
    public static partial class TrackTsmParser
    {
        private static void ParseSegmentKey(ref SegmentBuilder builder, string key, string value, float minPart)
        {
            if (key == "type" && TryParseTrackType(value, out var type))
            {
                builder.Type = type;
                return;
            }

            if (key == "surface" && TryParseTrackSurface(value, out var surface))
            {
                builder.Surface = surface;
                return;
            }

            if (key == "noise" && TryParseTrackNoise(value, out var noise))
            {
                builder.Noise = noise;
                return;
            }

            if (key == "length" && TryParseFloat(value, out var length))
            {
                builder.Length = Math.Max(minPart, length);
                return;
            }

            if (key == "width" && TryParseFloat(value, out var width))
            {
                builder.Width = Math.Max(0f, width);
                return;
            }

            if (key == "height" && TryParseFloat(value, out var height))
            {
                builder.Height = height;
                return;
            }

            if (key == "weather")
            {
                builder.WeatherProfileId = NormalizeNullable(value);
                return;
            }

            if (key == "weather_transition_seconds" && TryParseFloat(value, out var transitionSeconds))
            {
                builder.WeatherTransitionSeconds = transitionSeconds < 0f ? 0f : transitionSeconds;
                return;
            }

            if (key == "room" || key == "room_profile" || key == "room_preset")
            {
                builder.RoomId = NormalizeNullable(value);
                return;
            }

            if (TryApplyRoomOverride(ref builder.RoomOverrides, key, value))
                return;

            if (key == "sound_sources" || key == "sound_source_ids")
            {
                builder.SoundSourceIds = ParseCsvList(value);
                return;
            }

            if (key == "pit" && TryParseSegmentPitPoint(value, out var pitPoint))
            {
                // Accumulate rather than overwrite so a segment can carry both markers
                // (e.g. a "pit = pit_entry" and "pit = pit_exit" line in any order).
                if (pitPoint == SegmentPitPoint.PitEntry)
                    builder.IsPitEntry = true;
                else
                    builder.IsPitExit = true;
                return;
            }

            builder.Metadata[key] = value;
        }

        private static void ParseWeatherKey(ref WeatherBuilder builder, string key, string value)
        {
            if (key == "kind" && TryParseWeatherKind(value, out var kind))
            {
                var profile = TrackWeatherProfile.CreatePreset(builder.Id, kind);
                builder.Kind = profile.Kind;
                builder.LongitudinalWindMps = profile.LongitudinalWindMps;
                builder.LateralWindMps = profile.LateralWindMps;
                builder.AirDensityKgPerM3 = profile.AirDensityKgPerM3;
                builder.DraftingFactor = profile.DraftingFactor;
                builder.TemperatureC = profile.TemperatureC;
                builder.Humidity = profile.Humidity;
                builder.PressureKpa = profile.PressureKpa;
                builder.VisibilityM = profile.VisibilityM;
                builder.RainGain = profile.RainGain;
                builder.WindGain = profile.WindGain;
                builder.StormGain = profile.StormGain;
                return;
            }

            if (key == "longitudinal_wind_mps" && TryParseFloat(value, out var longitudinalWind))
            {
                builder.LongitudinalWindMps = longitudinalWind;
                return;
            }

            if (key == "lateral_wind_mps" && TryParseFloat(value, out var lateralWind))
            {
                builder.LateralWindMps = lateralWind;
                return;
            }

            if (key == "air_density" && TryParseFloat(value, out var airDensity))
            {
                builder.AirDensityKgPerM3 = airDensity;
                return;
            }

            if (key == "drafting_factor" && TryParseFloat(value, out var draftingFactor))
            {
                builder.DraftingFactor = draftingFactor;
                return;
            }

            if (key == "temperature_c" && TryParseFloat(value, out var temperature))
            {
                builder.TemperatureC = temperature;
                return;
            }

            if (key == "humidity" && TryParseFloat(value, out var humidity))
            {
                builder.Humidity = humidity;
                return;
            }

            if (key == "pressure_kpa" && TryParseFloat(value, out var pressure))
            {
                builder.PressureKpa = pressure;
                return;
            }

            if (key == "visibility_m" && TryParseFloat(value, out var visibility))
            {
                builder.VisibilityM = visibility;
                return;
            }

            if (key == "rain_gain" && TryParseFloat(value, out var rainGain))
            {
                builder.RainGain = rainGain;
                return;
            }

            if (key == "wind_gain" && TryParseFloat(value, out var windGain))
            {
                builder.WindGain = windGain;
                return;
            }

            if (key == "storm_gain" && TryParseFloat(value, out var stormGain))
            {
                builder.StormGain = stormGain;
            }
        }

        private static void ParseRoomKey(ref RoomBuilder builder, string key, string value)
        {
            if (key == "name")
            {
                builder.Name = NormalizeNullable(value);
                return;
            }

            if (key == "room_preset")
            {
                builder.ApplyPreset(value);
                return;
            }

            if (key == "reverb_time" && TryParseFloat(value, out var reverbTime))
            {
                builder.ReverbTimeSeconds = Math.Max(0f, reverbTime);
                return;
            }

            if (key == "reverb_gain" && TryParseFloat(value, out var reverbGain))
            {
                builder.ReverbGain = reverbGain;
                return;
            }

            if (key == "hf_decay_ratio" && TryParseFloat(value, out var hfDecay))
            {
                builder.HfDecayRatio = hfDecay;
                return;
            }

            if (key == "late_reverb_gain" && TryParseFloat(value, out var lateGain))
            {
                builder.LateReverbGain = lateGain;
                return;
            }

            if (key == "diffusion" && TryParseFloat(value, out var diffusion))
            {
                builder.Diffusion = diffusion;
                return;
            }

            if (key == "air_absorption" && TryParseFloat(value, out var airAbsorption))
            {
                builder.AirAbsorption = airAbsorption;
                return;
            }

            if (key == "occlusion_scale" && TryParseFloat(value, out var occlusionScale))
            {
                builder.OcclusionScale = occlusionScale;
                return;
            }

            if (key == "transmission_scale" && TryParseFloat(value, out var transmissionScale))
            {
                builder.TransmissionScale = transmissionScale;
                return;
            }

            if (key == "occlusion_override" && TryParseFloat(value, out var occlusionOverride))
            {
                builder.OcclusionOverride = occlusionOverride;
                return;
            }

            if (key == "transmission_override" && TryParseFloat(value, out var transmissionOverride))
            {
                builder.TransmissionOverrideLow = transmissionOverride;
                builder.TransmissionOverrideMid = transmissionOverride;
                builder.TransmissionOverrideHigh = transmissionOverride;
                return;
            }

            if (key == "transmission_override_low" && TryParseFloat(value, out var transmissionLow))
            {
                builder.TransmissionOverrideLow = transmissionLow;
                return;
            }

            if (key == "transmission_override_mid" && TryParseFloat(value, out var transmissionMid))
            {
                builder.TransmissionOverrideMid = transmissionMid;
                return;
            }

            if (key == "transmission_override_high" && TryParseFloat(value, out var transmissionHigh))
            {
                builder.TransmissionOverrideHigh = transmissionHigh;
                return;
            }

            if (key == "air_absorption_override" && TryParseFloat(value, out var airOverride))
            {
                builder.AirAbsorptionOverrideLow = airOverride;
                builder.AirAbsorptionOverrideMid = airOverride;
                builder.AirAbsorptionOverrideHigh = airOverride;
                return;
            }

            if (key == "air_absorption_override_low" && TryParseFloat(value, out var airLow))
            {
                builder.AirAbsorptionOverrideLow = airLow;
                return;
            }

            if (key == "air_absorption_override_mid" && TryParseFloat(value, out var airMid))
            {
                builder.AirAbsorptionOverrideMid = airMid;
                return;
            }

            if (key == "air_absorption_override_high" && TryParseFloat(value, out var airHigh))
            {
                builder.AirAbsorptionOverrideHigh = airHigh;
            }
        }

        private static void ParseSoundKey(ref SoundBuilder builder, string key, string value)
        {
            if (key == "type" && TryParseSoundType(value, out var type))
            {
                builder.Type = type;
                return;
            }

            if (key == "path" || key == "file")
            {
                builder.Path = NormalizeNullable(value);
                return;
            }

            if (key == "variant_paths")
            {
                builder.VariantPaths = ParseCsvList(value);
                return;
            }

            if (key == "variant_source_ids")
            {
                builder.VariantSourceIds = ParseCsvList(value);
                return;
            }

            if (key == "random_mode" && TryParseSoundRandomMode(value, out var randomMode))
            {
                builder.RandomMode = randomMode;
                return;
            }

            if (key == "loop" && TryParseBool(value, out var loop))
            {
                builder.Loop = loop;
                return;
            }

            if (key == "volume" && TryParseFloat(value, out var volume))
            {
                builder.Volume = volume;
                return;
            }

            if (key == "spatial" && TryParseBool(value, out var spatial))
            {
                builder.Spatial = spatial;
                return;
            }

            if (key == "allow_hrtf" && TryParseBool(value, out var allowHrtf))
            {
                builder.AllowHrtf = allowHrtf;
                return;
            }

            if (key == "fade_in" && TryParseFloat(value, out var fadeIn))
            {
                builder.FadeInSeconds = Math.Max(0f, fadeIn);
                return;
            }

            if (key == "fade_out" && TryParseFloat(value, out var fadeOut))
            {
                builder.FadeOutSeconds = Math.Max(0f, fadeOut);
                return;
            }

            if (key == "crossfade_seconds" && TryParseFloat(value, out var crossfade))
            {
                builder.CrossfadeSeconds = Math.Max(0f, crossfade);
                return;
            }

            if (key == "pitch" && TryParseFloat(value, out var pitch))
            {
                builder.Pitch = pitch;
                return;
            }

            if (key == "pan" && TryParseFloat(value, out var pan))
            {
                builder.Pan = pan;
                return;
            }

            if (key == "min_distance" && TryParseFloat(value, out var minDistance))
            {
                builder.MinDistance = minDistance;
                return;
            }

            if (key == "max_distance" && TryParseFloat(value, out var maxDistance))
            {
                builder.MaxDistance = maxDistance;
                return;
            }

            if (key == "rolloff" && TryParseFloat(value, out var rolloff))
            {
                builder.Rolloff = rolloff;
                return;
            }

            if (key == "global" && TryParseBool(value, out var global))
            {
                builder.Global = global;
                return;
            }

            if (key == "start_area")
            {
                builder.StartAreaId = NormalizeNullable(value);
                return;
            }

            if (key == "end_area")
            {
                builder.EndAreaId = NormalizeNullable(value);
                return;
            }

            if (key == "start_position" && TryParseVector(value, out var startPos))
            {
                builder.StartPosition = startPos;
                return;
            }

            if (key == "end_position" && TryParseVector(value, out var endPos))
            {
                builder.EndPosition = endPos;
                return;
            }

            if (key == "position" && TryParseVector(value, out var pos))
            {
                builder.Position = pos;
                return;
            }

            if (key == "start_radius" && TryParseFloat(value, out var startRadius))
            {
                builder.StartRadiusMeters = startRadius;
                return;
            }

            if (key == "end_radius" && TryParseFloat(value, out var endRadius))
            {
                builder.EndRadiusMeters = endRadius;
                return;
            }

            if ((key == "speed" || key == "speed_meters_per_second") && TryParseFloat(value, out var speed))
            {
                builder.SpeedMetersPerSecond = speed;
            }
        }

        private static bool TryApplyRoomOverride(ref TrackRoomOverrides overrides, string key, string value)
        {
            if (key == "reverb_time" && TryParseFloat(value, out var reverbTime)) { overrides.ReverbTimeSeconds = reverbTime; return true; }
            if (key == "reverb_gain" && TryParseFloat(value, out var reverbGain)) { overrides.ReverbGain = reverbGain; return true; }
            if (key == "hf_decay_ratio" && TryParseFloat(value, out var hfDecay)) { overrides.HfDecayRatio = hfDecay; return true; }
            if (key == "late_reverb_gain" && TryParseFloat(value, out var lateGain)) { overrides.LateReverbGain = lateGain; return true; }
            if (key == "diffusion" && TryParseFloat(value, out var diffusion)) { overrides.Diffusion = diffusion; return true; }
            if (key == "air_absorption" && TryParseFloat(value, out var airAbs)) { overrides.AirAbsorption = airAbs; return true; }
            if (key == "occlusion_scale" && TryParseFloat(value, out var occScale)) { overrides.OcclusionScale = occScale; return true; }
            if (key == "transmission_scale" && TryParseFloat(value, out var txScale)) { overrides.TransmissionScale = txScale; return true; }
            if (key == "occlusion_override" && TryParseFloat(value, out var occOverride)) { overrides.OcclusionOverride = occOverride; return true; }
            if (key == "transmission_override" && TryParseFloat(value, out var txOverride)) { overrides.TransmissionOverrideLow = txOverride; overrides.TransmissionOverrideMid = txOverride; overrides.TransmissionOverrideHigh = txOverride; return true; }
            if (key == "transmission_override_low" && TryParseFloat(value, out var txLow)) { overrides.TransmissionOverrideLow = txLow; return true; }
            if (key == "transmission_override_mid" && TryParseFloat(value, out var txMid)) { overrides.TransmissionOverrideMid = txMid; return true; }
            if (key == "transmission_override_high" && TryParseFloat(value, out var txHigh)) { overrides.TransmissionOverrideHigh = txHigh; return true; }
            if (key == "air_absorption_override" && TryParseFloat(value, out var airOverride)) { overrides.AirAbsorptionOverrideLow = airOverride; overrides.AirAbsorptionOverrideMid = airOverride; overrides.AirAbsorptionOverrideHigh = airOverride; return true; }
            if (key == "air_absorption_override_low" && TryParseFloat(value, out var airLow)) { overrides.AirAbsorptionOverrideLow = airLow; return true; }
            if (key == "air_absorption_override_mid" && TryParseFloat(value, out var airMid)) { overrides.AirAbsorptionOverrideMid = airMid; return true; }
            if (key == "air_absorption_override_high" && TryParseFloat(value, out var airHigh)) { overrides.AirAbsorptionOverrideHigh = airHigh; return true; }
            return false;
        }
    }
}
