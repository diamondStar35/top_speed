using System;
using System.Collections.Generic;
using System.IO;

namespace TopSpeed.Data
{
    public static partial class TrackTsmParser
    {
        private static void ValidateSegmentField(
            string key,
            string value,
            int lineNumber,
            float minPart,
            string sectionId,
            Dictionary<string, string> segmentRooms,
            Dictionary<string, IReadOnlyList<string>> segmentSounds,
            Dictionary<string, string> segmentWeatherRefs,
            List<TrackTsmIssue> issues)
        {
            if (key == "type")
            {
                if (!TryParseTrackType(value, out _))
                    issues.Add(new TrackTsmIssue(TrackTsmIssueSeverity.Error, lineNumber, Localized("Invalid segment type '{0}'.", value)));
                return;
            }

            if (key == "surface")
            {
                if (!TryParseTrackSurface(value, out _))
                    issues.Add(new TrackTsmIssue(TrackTsmIssueSeverity.Error, lineNumber, Localized("Invalid segment surface '{0}'.", value)));
                return;
            }

            if (key == "noise")
            {
                if (!TryParseTrackNoise(value, out _))
                    issues.Add(new TrackTsmIssue(TrackTsmIssueSeverity.Error, lineNumber, Localized("Invalid segment noise '{0}'.", value)));
                return;
            }

            if (key == "length")
            {
                if (!TryParseFloat(value, out var length))
                    issues.Add(new TrackTsmIssue(TrackTsmIssueSeverity.Error, lineNumber, Localized("Invalid segment length '{0}'.", value)));
                else if (length < minPart)
                    issues.Add(new TrackTsmIssue(TrackTsmIssueSeverity.Warning, lineNumber, Localized("Segment length '{0}' is below minimum {1} and will be clamped.", length, minPart)));
                return;
            }

            if (key == "width")
            {
                if (!TryParseFloat(value, out _))
                    issues.Add(new TrackTsmIssue(TrackTsmIssueSeverity.Error, lineNumber, Localized("Invalid segment width '{0}'.", value)));
                return;
            }

            if (key == "height")
            {
                if (!TryParseFloat(value, out _))
                    issues.Add(new TrackTsmIssue(TrackTsmIssueSeverity.Error, lineNumber, Localized("Invalid segment height '{0}'.", value)));
                return;
            }

            if (key == "weather")
            {
                var weatherProfileId = NormalizeNullable(value);
                if (weatherProfileId == null)
                    issues.Add(new TrackTsmIssue(TrackTsmIssueSeverity.Error, lineNumber, Localized("Segment weather profile id cannot be empty.")));
                else
                    segmentWeatherRefs[sectionId] = weatherProfileId;
                return;
            }

            if (key == "weather_transition_seconds")
            {
                if (!TryParseFloat(value, out var transitionSeconds))
                    issues.Add(new TrackTsmIssue(TrackTsmIssueSeverity.Error, lineNumber, Localized("Invalid weather transition seconds '{0}'.", value)));
                else if (float.IsNaN(transitionSeconds) || float.IsInfinity(transitionSeconds) || transitionSeconds < 0f)
                    issues.Add(new TrackTsmIssue(TrackTsmIssueSeverity.Error, lineNumber, Localized("Weather transition seconds must be finite and non-negative.")));
                return;
            }

            if (key == "room" || key == "room_profile" || key == "room_preset")
            {
                var roomId = NormalizeNullable(value);
                if (roomId == null)
                    issues.Add(new TrackTsmIssue(TrackTsmIssueSeverity.Error, lineNumber, Localized("Segment room id cannot be empty.")));
                else
                    segmentRooms[sectionId] = roomId;
                return;
            }

            if (TryValidateRoomOverride(key, value, lineNumber, issues))
                return;

            if (key == "sound_sources" || key == "sound_source_ids")
            {
                var list = ParseCsvList(value);
                if (list.Count == 0)
                    issues.Add(new TrackTsmIssue(TrackTsmIssueSeverity.Error, lineNumber, Localized("Segment sound source list cannot be empty.")));
                else
                    segmentSounds[sectionId] = list;
                return;
            }

            if (key == "pit")
            {
                if (!TryParseSegmentPitPoint(value, out _))
                    issues.Add(new TrackTsmIssue(TrackTsmIssueSeverity.Error, lineNumber, Localized("Invalid pit value '{0}'. Use pit_entry or pit_exit.", value)));
                return;
            }

            if (key == "name")
                return;

            if (!IsMetadataKey(key))
                issues.Add(new TrackTsmIssue(TrackTsmIssueSeverity.Error, lineNumber, Localized("Unknown segment key '{0}'.", key)));
        }

        private static void ValidateWeatherField(string key, string value, int lineNumber, List<TrackTsmIssue> issues)
        {
            if (key == "kind")
            {
                if (!TryParseWeatherKind(value, out _))
                    issues.Add(new TrackTsmIssue(TrackTsmIssueSeverity.Error, lineNumber, Localized("Invalid weather kind '{0}'.", value)));
                return;
            }

            if (key == "longitudinal_wind_mps" ||
                key == "lateral_wind_mps" ||
                key == "temperature_c")
            {
                if (!TryParseFloat(value, out _))
                    issues.Add(new TrackTsmIssue(TrackTsmIssueSeverity.Error, lineNumber, Localized("Invalid weather value '{0}' for key '{1}'.", value, key)));
                return;
            }

            if (!TryParseFloat(value, out var numericValue) || float.IsNaN(numericValue) || float.IsInfinity(numericValue))
            {
                issues.Add(new TrackTsmIssue(TrackTsmIssueSeverity.Error, lineNumber, Localized("Invalid weather value '{0}' for key '{1}'.", value, key)));
                return;
            }

            switch (key)
            {
                case "air_density":
                    if (numericValue <= 0f)
                        issues.Add(new TrackTsmIssue(TrackTsmIssueSeverity.Error, lineNumber, Localized("Weather key '{0}' must be greater than zero.", key)));
                    return;
                case "drafting_factor":
                    if (numericValue <= 0f)
                        issues.Add(new TrackTsmIssue(TrackTsmIssueSeverity.Error, lineNumber, Localized("Weather key '{0}' must be greater than zero.", key)));
                    return;
                case "humidity":
                    if (numericValue < 0f || numericValue > 1f)
                        issues.Add(new TrackTsmIssue(TrackTsmIssueSeverity.Error, lineNumber, Localized("Humidity must be between 0 and 1.")));
                    return;
                case "pressure_kpa":
                    if (numericValue <= 0f)
                        issues.Add(new TrackTsmIssue(TrackTsmIssueSeverity.Error, lineNumber, Localized("Weather key '{0}' must be greater than zero.", key)));
                    return;
                case "visibility_m":
                    if (numericValue <= 0f)
                        issues.Add(new TrackTsmIssue(TrackTsmIssueSeverity.Error, lineNumber, Localized("Weather key '{0}' must be greater than zero.", key)));
                    return;
                case "rain_gain":
                case "wind_gain":
                case "storm_gain":
                    if (numericValue < 0f)
                        issues.Add(new TrackTsmIssue(TrackTsmIssueSeverity.Error, lineNumber, Localized("Weather key '{0}' must be non-negative.", key)));
                    return;
            }

            if (!IsMetadataKey(key))
                issues.Add(new TrackTsmIssue(TrackTsmIssueSeverity.Error, lineNumber, Localized("Unknown weather key '{0}'.", key)));
        }

        private static void ValidateRoomField(string key, string value, int lineNumber, List<TrackTsmIssue> issues)
        {
            if (key == "name")
                return;

            if (key == "room_preset")
            {
                if (!TrackRoomLibrary.IsPreset(value.Trim()))
                    issues.Add(new TrackTsmIssue(TrackTsmIssueSeverity.Error, lineNumber, Localized("Unknown room preset '{0}'.", value)));
                return;
            }

            if (IsRoomNumericKey(key))
            {
                if (!TryParseFloat(value, out _))
                    issues.Add(new TrackTsmIssue(TrackTsmIssueSeverity.Error, lineNumber, Localized("Invalid room value '{0}' for key '{1}'.", value, key)));
                return;
            }

            if (!IsMetadataKey(key))
                issues.Add(new TrackTsmIssue(TrackTsmIssueSeverity.Error, lineNumber, Localized("Unknown room key '{0}'.", key)));
        }

        private static void ValidateSoundField(
            string key,
            string value,
            int lineNumber,
            string sectionId,
            Dictionary<string, string> soundStartAreas,
            Dictionary<string, string> soundEndAreas,
            List<TrackTsmIssue> issues)
        {
            if (key == "type")
            {
                if (!TryParseSoundType(value, out _))
                    issues.Add(new TrackTsmIssue(TrackTsmIssueSeverity.Error, lineNumber, Localized("Invalid sound type '{0}'.", value)));
                return;
            }

            if (key == "path" || key == "file")
            {
                if (!IsValidTrackRelativeSoundPath(value))
                    issues.Add(new TrackTsmIssue(TrackTsmIssueSeverity.Error, lineNumber, Localized("Sound key '{0}' must be a track-relative path and cannot escape the track folder.", key)));
                return;
            }

            if (key == "variant_paths")
            {
                var list = ParseCsvList(value);
                if (list.Count == 0)
                    issues.Add(new TrackTsmIssue(TrackTsmIssueSeverity.Error, lineNumber, Localized("Sound list for '{0}' cannot be empty.", key)));
                else
                {
                    for (var i = 0; i < list.Count; i++)
                    {
                        if (!IsValidTrackRelativeSoundPath(list[i]))
                            issues.Add(new TrackTsmIssue(TrackTsmIssueSeverity.Error, lineNumber, Localized("Sound key '{0}' contains invalid path '{1}'. Paths must be track-relative.", key, list[i])));
                    }
                }
                return;
            }

            if (key == "variant_source_ids")
            {
                var list = ParseCsvList(value);
                if (list.Count == 0)
                    issues.Add(new TrackTsmIssue(TrackTsmIssueSeverity.Error, lineNumber, Localized("Sound list for '{0}' cannot be empty.", key)));
                return;
            }

            if (key == "random_mode")
            {
                if (!TryParseSoundRandomMode(value, out _))
                    issues.Add(new TrackTsmIssue(TrackTsmIssueSeverity.Error, lineNumber, Localized("Invalid sound random mode '{0}'.", value)));
                return;
            }

            if (IsSoundBooleanKey(key))
            {
                if (!TryParseBool(value, out _))
                    issues.Add(new TrackTsmIssue(TrackTsmIssueSeverity.Error, lineNumber, Localized("Invalid boolean '{0}' for key '{1}'.", value, key)));
                return;
            }

            if (key == "start_area")
            {
                var area = NormalizeNullable(value);
                if (area == null)
                    issues.Add(new TrackTsmIssue(TrackTsmIssueSeverity.Error, lineNumber, Localized("start_area cannot be empty.")));
                else
                    soundStartAreas[sectionId] = area;
                return;
            }

            if (key == "end_area")
            {
                var area = NormalizeNullable(value);
                if (area == null)
                    issues.Add(new TrackTsmIssue(TrackTsmIssueSeverity.Error, lineNumber, Localized("end_area cannot be empty.")));
                else
                    soundEndAreas[sectionId] = area;
                return;
            }

            if (IsSoundVectorKey(key))
            {
                if (!TryParseVector(value, out _))
                    issues.Add(new TrackTsmIssue(TrackTsmIssueSeverity.Error, lineNumber, Localized("Invalid vector '{0}' for key '{1}'.", value, key)));
                return;
            }

            if (IsSoundNumericKey(key))
            {
                if (!TryParseFloat(value, out _))
                    issues.Add(new TrackTsmIssue(TrackTsmIssueSeverity.Error, lineNumber, Localized("Invalid numeric value '{0}' for key '{1}'.", value, key)));
                return;
            }

            if (!IsMetadataKey(key))
                issues.Add(new TrackTsmIssue(TrackTsmIssueSeverity.Error, lineNumber, Localized("Unknown sound key '{0}'.", key)));
        }

        private static bool TryValidateRoomOverride(string key, string value, int lineNumber, List<TrackTsmIssue> issues)
        {
            if (!IsRoomOverrideKey(key))
                return false;

            if (!TryParseFloat(value, out _))
            {
                issues.Add(new TrackTsmIssue(TrackTsmIssueSeverity.Error, lineNumber, Localized("Invalid room override value '{0}' for key '{1}'.", value, key)));
            }

            return true;
        }

        private static bool IsRoomNumericKey(string key)
        {
            return key == "reverb_time" ||
                   key == "reverb_gain" ||
                   key == "hf_decay_ratio" ||
                   key == "late_reverb_gain" ||
                   key == "diffusion" ||
                   key == "air_absorption" ||
                   key == "occlusion_scale" ||
                   key == "transmission_scale" ||
                   IsRoomOverrideKey(key);
        }

        private static bool IsRoomOverrideKey(string key)
        {
            return key == "occlusion_override" ||
                   key == "transmission_override" ||
                   key == "transmission_override_low" ||
                   key == "transmission_override_mid" ||
                   key == "transmission_override_high" ||
                   key == "air_absorption_override" ||
                   key == "air_absorption_override_low" ||
                   key == "air_absorption_override_mid" ||
                   key == "air_absorption_override_high";
        }

        private static bool IsSoundNumericKey(string key)
        {
            return key == "volume" ||
                   key == "fade_in" ||
                   key == "fade_out" ||
                   key == "crossfade_seconds" ||
                   key == "pitch" ||
                   key == "pan" ||
                   key == "min_distance" ||
                   key == "max_distance" ||
                   key == "rolloff" ||
                   key == "start_radius" ||
                   key == "end_radius" ||
                   key == "speed" ||
                   key == "speed_meters_per_second";
        }

        private static bool IsSoundBooleanKey(string key)
        {
            return key == "loop" ||
                   key == "spatial" ||
                   key == "allow_hrtf" ||
                   key == "global";
        }

        private static bool IsSoundVectorKey(string key)
        {
            return key == "start_position" ||
                   key == "end_position" ||
                   key == "position";
        }

        private static bool IsValidTrackRelativeSoundPath(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return false;

            var trimmed = value.Trim();
            if (Path.IsPathRooted(trimmed) || trimmed.IndexOf(':') >= 0)
                return false;

            var normalized = trimmed.Replace('\\', '/');
            var parts = normalized.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0)
                return false;

            for (var i = 0; i < parts.Length; i++)
            {
                var part = parts[i].Trim();
                if (part.Length == 0 || part == "." || part == "..")
                    return false;
            }

            return true;
        }

        private static bool IsMetadataKey(string key)
        {
            return key.StartsWith("meta", StringComparison.OrdinalIgnoreCase) ||
                   key.StartsWith("metadata", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsValidAmbience(string raw)
        {
            if (TryParseInt(raw, out var ambienceInt))
                return ambienceInt >= 0 && ambienceInt <= 2;
            var normalized = NormalizeLookupToken(raw);
            return normalized == "noambience" ||
                   normalized == "none" ||
                   normalized == "desert" ||
                   normalized == "airport";
        }
    }
}
