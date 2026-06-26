using System;
using System.Collections.Generic;
using System.IO;
using TopSpeed.Audio;
using TopSpeed.Core;
using TopSpeed.Data;

namespace TopSpeed.Tracks
{
    internal sealed partial class Track
    {
        public static Track Load(string nameOrPath, AudioManager audio)
        {
            if (TrackCatalog.BuiltIn.TryGetValue(nameOrPath, out var builtIn))
                return new Track(nameOrPath, builtIn, audio, userDefined: false);

            var data = ReadCustomTrackData(nameOrPath);
            var displayName = ResolveCustomTrackName(nameOrPath, data.Name);
            return new Track(displayName, data, audio, userDefined: true);
        }

        public static Track LoadFromData(string trackName, TrackData data, AudioManager audio, bool userDefined)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));
            return new Track(trackName, data, audio, userDefined);
        }

        /// <summary>
        /// Whether a track has a usable pit area. Today every track does — when a track file defines
        /// no explicit pit entry/exit, the pit stop system falls back to the start/finish line (see
        /// <c>PitStop</c>), so this returns <c>true</c> for every track. This is the single plug-in
        /// point for the eventual "pit-less track" feature: once we decide how such tracks are
        /// expressed (e.g. a <c>disablePitArea</c> entry in <see cref="TrackData.Metadata"/>, or the
        /// absence of pit-point segments in <see cref="TrackData.Definitions"/>), implement the real
        /// check here and every no-pit warning call site picks it up automatically.
        /// </summary>
        public static bool HasPitArea(TrackData data)
        {
            _ = data;
            return true;
        }

        /// <summary>
        /// Resolves a track's data from a built-in key or custom file path without constructing a
        /// <see cref="Track"/> (so no audio is loaded). Used for cheap pre-race checks such as the
        /// no-pit-area warning. Returns false if the data cannot be read.
        /// </summary>
        public static bool TryResolveData(string nameOrPath, out TrackData data)
        {
            if (!string.IsNullOrWhiteSpace(nameOrPath) && TrackCatalog.BuiltIn.TryGetValue(nameOrPath, out var builtIn))
            {
                data = builtIn;
                return true;
            }

            if (!string.IsNullOrWhiteSpace(nameOrPath)
                && TrackTsmParser.TryLoad(nameOrPath, out var parsed, out _, MinPartLengthMeters))
            {
                data = parsed;
                return true;
            }

            data = null!;
            return false;
        }

        private static Dictionary<string, int> BuildSegmentIndex(IReadOnlyList<TrackDefinition> definitions)
        {
            var map = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            for (var i = 0; i < definitions.Count; i++)
            {
                var id = definitions[i].SegmentId;
                if (string.IsNullOrWhiteSpace(id))
                    continue;
                var normalizedId = id!;
                if (!map.ContainsKey(normalizedId))
                    map[normalizedId] = i;
            }

            return map;
        }

        private static string ResolveSourceDirectory(string? sourcePath)
        {
            if (!string.IsNullOrWhiteSpace(sourcePath))
            {
                var path = Path.GetFullPath(sourcePath);
                var directory = Path.GetDirectoryName(path);
                if (!string.IsNullOrWhiteSpace(directory))
                    return directory!;
            }

            return Path.Combine(AssetPaths.Root, "Tracks");
        }

        private static string ResolveCustomTrackName(string path, string? name)
        {
            var trimmedName = name?.Trim();
            if (!string.IsNullOrWhiteSpace(trimmedName))
                return trimmedName!;

            var directory = Path.GetDirectoryName(path);
            var folderName = string.IsNullOrWhiteSpace(directory) ? null : Path.GetFileName(directory);
            if (!string.IsNullOrWhiteSpace(folderName))
                return folderName!;

            var fileName = Path.GetFileNameWithoutExtension(path);
            return string.IsNullOrWhiteSpace(fileName) ? path : fileName;
        }

        private static TrackData ReadCustomTrackData(string filename)
        {
            if (TrackTsmParser.TryLoad(filename, out var parsed, out var issues, MinPartLengthMeters))
                return parsed;

            throw TrackLoadException.FromIssues(filename, issues);
        }
    }
}

