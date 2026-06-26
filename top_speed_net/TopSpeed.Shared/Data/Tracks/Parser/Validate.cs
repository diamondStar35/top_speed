using System;
using System.Collections.Generic;
using System.IO;

namespace TopSpeed.Data
{
    public static partial class TrackTsmParser
    {
        private static bool ValidateFile(string filename, float minPart, List<TrackTsmIssue> issues)
        {
            var sectionKind = string.Empty;
            var segmentIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var weatherIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var roomIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var soundIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var segmentRooms = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var segmentSounds = new Dictionary<string, IReadOnlyList<string>>(StringComparer.OrdinalIgnoreCase);
            var segmentWeatherRefs = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var soundStartAreas = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var soundEndAreas = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            string? defaultWeatherProfileId = null;
            var currentSectionId = string.Empty;
            var pitAreaDisabled = false;
            var pitAreaLine = 0;
            var hasPitPointSegment = false;

            int lineNumber = 0;
            foreach (var raw in File.ReadLines(filename))
            {
                lineNumber++;
                var line = StripInlineComment(raw).Trim();
                if (line.Length == 0)
                    continue;

                if (TryParseSectionHeader(line, out var nextKind, out var nextId, out _))
                {
                    sectionKind = nextKind;
                    currentSectionId = nextId.Trim();

                    if (sectionKind != "meta" &&
                        sectionKind != "segment" &&
                        sectionKind != "weather" &&
                        sectionKind != "room" &&
                        sectionKind != "sound")
                    {
                        issues.Add(new TrackTsmIssue(TrackTsmIssueSeverity.Error, lineNumber, Localized("Unknown section '{0}'.", nextKind)));
                        sectionKind = string.Empty;
                        continue;
                    }

                    if ((sectionKind == "segment" || sectionKind == "weather" || sectionKind == "room" || sectionKind == "sound") &&
                        currentSectionId.Length == 0)
                    {
                        issues.Add(new TrackTsmIssue(TrackTsmIssueSeverity.Error, lineNumber, Localized("Section '{0}' requires an id.", sectionKind)));
                        sectionKind = string.Empty;
                        continue;
                    }

                    if (sectionKind == "segment")
                    {
                        if (!segmentIds.Add(currentSectionId))
                            issues.Add(new TrackTsmIssue(TrackTsmIssueSeverity.Error, lineNumber, Localized("Duplicate segment id '{0}'.", currentSectionId)));
                        if (!segmentSounds.ContainsKey(currentSectionId))
                            segmentSounds[currentSectionId] = Array.Empty<string>();
                    }
                    else if (sectionKind == "weather")
                    {
                        if (!weatherIds.Add(currentSectionId))
                            issues.Add(new TrackTsmIssue(TrackTsmIssueSeverity.Error, lineNumber, Localized("Duplicate weather id '{0}'.", currentSectionId)));
                    }
                    else if (sectionKind == "room")
                    {
                        if (!roomIds.Add(currentSectionId))
                            issues.Add(new TrackTsmIssue(TrackTsmIssueSeverity.Error, lineNumber, Localized("Duplicate room id '{0}'.", currentSectionId)));
                    }
                    else if (sectionKind == "sound")
                    {
                        if (!soundIds.Add(currentSectionId))
                            issues.Add(new TrackTsmIssue(TrackTsmIssueSeverity.Error, lineNumber, Localized("Duplicate sound id '{0}'.", currentSectionId)));
                    }

                    continue;
                }

                if (!TryParseKeyValue(line, out var rawKey, out var rawValue))
                {
                    issues.Add(new TrackTsmIssue(TrackTsmIssueSeverity.Error, lineNumber, Localized("Malformed line. Expected '[section]' or 'key = value'.")));
                    continue;
                }

                if (sectionKind.Length == 0)
                {
                    issues.Add(new TrackTsmIssue(TrackTsmIssueSeverity.Error, lineNumber, Localized("Property '{0}' is outside any section.", rawKey.Trim())));
                    continue;
                }

                var key = NormalizeIdentifier(rawKey);
                var value = rawValue.Trim();
                if (value.Length == 0)
                {
                    issues.Add(new TrackTsmIssue(TrackTsmIssueSeverity.Error, lineNumber, Localized("Key '{0}' is missing a value.", rawKey.Trim())));
                    continue;
                }

                switch (sectionKind)
                {
                    case "meta":
                        if (key == "weather")
                            defaultWeatherProfileId = NormalizeNullable(value);
                        else if (key == "ambience" && !IsValidAmbience(value))
                            issues.Add(new TrackTsmIssue(TrackTsmIssueSeverity.Error, lineNumber, Localized("Invalid ambience value '{0}'.", value)));
                        else if (key == "pit_area")
                        {
                            if (!TryParseBool(value, out var pitAreaValue))
                                issues.Add(new TrackTsmIssue(TrackTsmIssueSeverity.Error, lineNumber, Localized("Invalid boolean '{0}' for key '{1}'.", value, key)));
                            else if (!pitAreaValue)
                            {
                                pitAreaDisabled = true;
                                pitAreaLine = lineNumber;
                            }
                        }
                        break;
                    case "segment":
                        if (key == "pit" && TryParseSegmentPitPoint(value, out _))
                            hasPitPointSegment = true;
                        ValidateSegmentField(
                            key,
                            value,
                            lineNumber,
                            minPart,
                            currentSectionId,
                            segmentRooms,
                            segmentSounds,
                            segmentWeatherRefs,
                            issues);
                        break;
                    case "weather":
                        ValidateWeatherField(key, value, lineNumber, issues);
                        break;
                    case "room":
                        ValidateRoomField(key, value, lineNumber, issues);
                        break;
                    case "sound":
                        ValidateSoundField(
                            key,
                            value,
                            lineNumber,
                            currentSectionId,
                            soundStartAreas,
                            soundEndAreas,
                            issues);
                        break;
                }
            }

            if (segmentIds.Count == 0)
                issues.Add(new TrackTsmIssue(TrackTsmIssueSeverity.Error, 0, Localized("Track must include at least one [segment:<id>] section.")));

            if (pitAreaDisabled && hasPitPointSegment)
                issues.Add(new TrackTsmIssue(
                    TrackTsmIssueSeverity.Warning,
                    pitAreaLine,
                    Localized("Track sets 'pit_area = false' but also defines pit entry/exit segments; pitting is disabled and the pit segments are ignored.")));

            if (string.IsNullOrWhiteSpace(defaultWeatherProfileId))
            {
                issues.Add(new TrackTsmIssue(
                    TrackTsmIssueSeverity.Error,
                    0,
                    Localized("Track [meta] must define a weather profile reference, for example 'weather = default'.")));
            }
            else if (!weatherIds.Contains(defaultWeatherProfileId!))
            {
                issues.Add(new TrackTsmIssue(
                    TrackTsmIssueSeverity.Error,
                    0,
                    Localized("Track default weather profile '{0}' does not match any [weather:{0}] section.", (object)defaultWeatherProfileId!)));
            }

            foreach (var pair in segmentRooms)
            {
                var roomId = pair.Value;
                if (string.IsNullOrWhiteSpace(roomId))
                    continue;
                if (!roomIds.Contains(roomId) && !TrackRoomLibrary.IsPreset(roomId))
                {
                    issues.Add(new TrackTsmIssue(
                        TrackTsmIssueSeverity.Error,
                        0,
                        Localized("Segment '{0}' references room '{1}', but no matching [room:{1}] section or preset exists.", pair.Key, roomId)));
                }
            }

            foreach (var pair in segmentSounds)
            {
                foreach (var soundId in pair.Value)
                {
                    if (!soundIds.Contains(soundId))
                    {
                        issues.Add(new TrackTsmIssue(
                            TrackTsmIssueSeverity.Error,
                            0,
                            Localized("Segment '{0}' references sound source '{1}', but no matching [sound:{1}] section exists.", pair.Key, soundId)));
                    }
                }
            }

            foreach (var pair in segmentWeatherRefs)
            {
                if (!weatherIds.Contains(pair.Value))
                {
                    issues.Add(new TrackTsmIssue(
                        TrackTsmIssueSeverity.Error,
                        0,
                        Localized("Segment '{0}' references weather profile '{1}', but no matching [weather:{1}] section exists.", pair.Key, pair.Value)));
                }
            }

            foreach (var pair in soundStartAreas)
            {
                if (!segmentIds.Contains(pair.Value))
                {
                    issues.Add(new TrackTsmIssue(
                        TrackTsmIssueSeverity.Error,
                        0,
                        Localized("Sound '{0}' references start_area '{1}', but no matching segment id exists.", pair.Key, pair.Value)));
                }
            }

            foreach (var pair in soundEndAreas)
            {
                if (!segmentIds.Contains(pair.Value))
                {
                    issues.Add(new TrackTsmIssue(
                        TrackTsmIssueSeverity.Error,
                        0,
                        Localized("Sound '{0}' references end_area '{1}', but no matching segment id exists.", pair.Key, pair.Value)));
                }
            }

            for (var i = 0; i < issues.Count; i++)
            {
                if (issues[i].Severity == TrackTsmIssueSeverity.Error)
                    return false;
            }

            return true;
        }
    }
}
