using System;
using System.Collections.Generic;
using System.IO;
using TopSpeed.Data;
using TopSpeed.Tracks.Geometry;

namespace TopSpeed.Server.Tracks
{
    internal static class TrackLoader
    {
        private const int Types = 9;
        private const int Surfaces = 5;
        private const int Noises = 12;
        private const float MinPartLength = 50.0f;

        public static TrackData LoadTrack(string nameOrPath, byte defaultLaps)
        {
            if (TryLoadLayout(nameOrPath, out var layout))
            {
                var laps = ResolveLaps(nameOrPath, defaultLaps);
                var userDefined = LooksLikePath(nameOrPath);
                return new TrackData(userDefined, layout.Weather, layout.Ambience, Array.Empty<TrackDefinition>(), laps, layout.Metadata?.Name);
            }

            if (!LooksLikePath(nameOrPath))
                throw new FileNotFoundException("Track layout not found.", nameOrPath);

            var data = ReadCustomTrackData(nameOrPath);
            data.Laps = ResolveLaps(nameOrPath, defaultLaps);
            return data;
        }

        private static bool TryLoadLayout(string nameOrPath, out TrackLayout layout)
        {
            layout = null!;
            if (string.IsNullOrWhiteSpace(nameOrPath))
                return false;

            var root = Path.Combine(AppContext.BaseDirectory, "Tracks");
            var sources = new ITrackLayoutSource[]
            {
                new FileTrackLayoutSource(new[] { root })
            };
            var loader = new TrackLayoutLoader(sources);
            var request = new TrackLayoutLoadRequest(nameOrPath, validate: true, buildGeometry: false, allowWarnings: true);
            var result = loader.Load(request);
            if (!result.IsSuccess || result.Layout == null)
                return false;

            layout = result.Layout;
            return true;
        }

        private static bool LooksLikePath(string identifier)
        {
            if (string.IsNullOrWhiteSpace(identifier))
                return false;
            if (identifier.IndexOfAny(new[] { '\\', '/' }) >= 0)
                return true;
            return Path.HasExtension(identifier);
        }

        private static byte ResolveLaps(string trackName, byte defaultLaps)
        {
            return trackName.IndexOf("adv", StringComparison.OrdinalIgnoreCase) < 0
                ? defaultLaps
                : (byte)1;
        }

        private static TrackData ReadCustomTrackData(string filename)
        {
            var resolvedPath = ResolveTrackPath(filename);
            if (resolvedPath == null)
            {
                return CreateFallbackTrack();
            }

            var values = new List<int>();
            string? name = null;
            foreach (var line in File.ReadLines(resolvedPath))
            {
                var trimmed = line.Trim();
                if (TryParseCustomTrackName(trimmed, out var parsedName))
                {
                    if (string.IsNullOrWhiteSpace(name))
                        name = parsedName;
                    continue;
                }

                AppendIntsFromLine(trimmed, values);
            }

            var length = 0;
            var index = 0;
            var minPartLengthLegacy = 5000;

            while (index < values.Count)
            {
                var first = values[index++];
                if (first < 0)
                    break;
                if (index < values.Count) index++;
                if (index >= values.Count) break;
                var third = values[index++];
                if (third < minPartLengthLegacy && index < values.Count)
                    index++;
                length++;
            }

            if (length == 0)
            {
                return CreateFallbackTrack();
            }

            var definitions = new TrackDefinition[length];
            index = 0;
            for (var i = 0; i < length; i++)
            {
                var typeValue = index < values.Count ? values[index++] : 0;
                var surfaceValue = index < values.Count ? values[index++] : 0;
                var temp = index < values.Count ? values[index++] : 0;

                var noiseValue = 0;
                var lengthValueLegacy = 0;
                if (temp < Noises)
                {
                    noiseValue = temp;
                    lengthValueLegacy = index < values.Count ? values[index++] : minPartLengthLegacy;
                }
                else
                {
                    if (typeValue >= Types)
                    {
                        noiseValue = (typeValue - Types) + 1;
                        typeValue = 0;
                    }
                    else
                    {
                        noiseValue = 0;
                    }
                    lengthValueLegacy = temp;
                }

                if (typeValue >= Types)
                    typeValue = 0;
                if (surfaceValue >= Surfaces)
                    surfaceValue = 0;
                if (noiseValue >= Noises)
                    noiseValue = 0;
                if (lengthValueLegacy < minPartLengthLegacy)
                    lengthValueLegacy = minPartLengthLegacy;

                definitions[i] = new TrackDefinition((TrackType)typeValue, (TrackSurface)surfaceValue, (TrackNoise)noiseValue, lengthValueLegacy / 100.0f);
            }

            if (index < values.Count)
                index++; // skip -1

            var weatherValue = index < values.Count ? values[index++] : 0;
            if (weatherValue < 0)
                weatherValue = 0;
            var ambienceValue = index < values.Count ? values[index++] : 0;
            if (ambienceValue < 0)
                ambienceValue = 0;

            return new TrackData(true, (TrackWeather)weatherValue, (TrackAmbience)ambienceValue, definitions, name: name);
        }

        private static int AppendIntsFromLine(string line, List<int> values)
        {
            if (string.IsNullOrWhiteSpace(line))
                return 0;

            var trimmed = line.Trim();
            if (trimmed.StartsWith("#", StringComparison.Ordinal) ||
                trimmed.StartsWith(";", StringComparison.Ordinal))
            {
                return 0;
            }

            var added = 0;
            var parts = trimmed.Split((char[])null!, StringSplitOptions.RemoveEmptyEntries);
            foreach (var part in parts)
            {
                if (int.TryParse(part, out var value))
                {
                    values.Add(value);
                    added++;
                }
            }

            return added;
        }

        private static string? ResolveTrackPath(string nameOrPath)
        {
            if (File.Exists(nameOrPath))
                return nameOrPath;

            var baseDir = AppContext.BaseDirectory;
            var candidate = Path.Combine(baseDir, "Tracks", nameOrPath);
            if (File.Exists(candidate))
                return candidate;

            return null;
        }

        private static TrackData CreateFallbackTrack()
        {
            var definitions = new[]
            {
                new TrackDefinition(TrackType.Straight, TrackSurface.Asphalt, TrackNoise.NoNoise, MinPartLength)
            };

            return new TrackData(true, TrackWeather.Sunny, TrackAmbience.NoAmbience, definitions);
        }

        private static bool TryParseCustomTrackName(string line, out string name)
        {
            name = string.Empty;
            if (string.IsNullOrWhiteSpace(line))
                return false;

            var trimmed = line.Trim();
            if (trimmed.StartsWith("#", StringComparison.Ordinal) ||
                trimmed.StartsWith(";", StringComparison.Ordinal))
            {
                trimmed = trimmed.Substring(1).TrimStart();
            }

            var separatorIndex = trimmed.IndexOf('=');
            if (separatorIndex < 0)
                separatorIndex = trimmed.IndexOf(':');
            if (separatorIndex <= 0)
                return false;

            var key = trimmed.Substring(0, separatorIndex).Trim();
            if (!key.Equals("name", StringComparison.OrdinalIgnoreCase) &&
                !key.Equals("trackname", StringComparison.OrdinalIgnoreCase) &&
                !key.Equals("title", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            var value = trimmed.Substring(separatorIndex + 1).Trim().Trim('"');
            if (string.IsNullOrWhiteSpace(value))
                return false;

            name = value;
            return true;
        }
    }
}
