using System;
using System.IO;
using TopSpeed.Data;
using TopSpeed.Tracks.Map;

namespace TopSpeed.Server.Tracks
{
    internal static class TrackLoader
    {
        public static TrackData LoadTrack(string nameOrPath, byte defaultLaps)
        {
            if (!TrackMapFormat.TryParse(nameOrPath, out var map, out var issues) || map == null)
            {
                var message = issues.Count > 0 ? issues[0].Message : "Track map not found.";
                throw new FileNotFoundException(message, nameOrPath);
            }

            var validation = TrackMapValidator.Validate(map);
            if (!validation.IsValid)
            {
                var message = validation.Issues.Count > 0
                    ? validation.Issues[0].Message
                    : "Track map validation failed.";
                throw new InvalidDataException(message);
            }

            var laps = ResolveLaps(nameOrPath, defaultLaps);
            var userDefined = LooksLikePath(nameOrPath);
            return new TrackData(userDefined, map.Metadata.Weather, map.Metadata.Ambience, Array.Empty<TrackDefinition>(), laps, map.Metadata.Name);
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
    }
}
