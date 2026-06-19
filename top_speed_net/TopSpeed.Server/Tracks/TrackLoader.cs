using System;
using System.Collections.Generic;
using TopSpeed.Data;
using TopSpeed.Localization;
using TopSpeed.Server.Logging;

namespace TopSpeed.Server.Tracks
{
    internal static class TrackLoader
    {
        private const float MinPartLength = 50.0f;

        public static TrackData LoadTrack(string nameOrPath, int defaultLaps, Logger? logger = null)
        {
            if (TrackCatalog.BuiltIn.TryGetValue(nameOrPath, out var builtIn))
            {
                var laps = ResolveLaps(nameOrPath, defaultLaps);
                return builtIn.WithLaps(laps);
            }

            var data = ReadCustomTrackData(nameOrPath, logger);
            data.Laps = ResolveLaps(nameOrPath, defaultLaps);
            return data;
        }

        private static int ResolveLaps(string trackName, int defaultLaps)
        {
            return trackName.IndexOf("adv", StringComparison.OrdinalIgnoreCase) < 0
                ? defaultLaps
                : 1;
        }

        private static TrackData ReadCustomTrackData(string filename, Logger? logger)
        {
            if (!TrackTsmParser.TryLoad(filename, out var parsed, out var issues, MinPartLength))
            {
                LogTrackIssues(filename, issues, logger);
                return CreateFallbackTrack();
            }
            return parsed;
        }

        private static TrackData CreateFallbackTrack()
        {
            var definitions = new[]
            {
                new TrackDefinition(TrackType.Straight, TrackSurface.Asphalt, TrackNoise.NoNoise, MinPartLength)
            };

            return new TrackData(true, TrackWeather.Sunny, TrackAmbience.NoAmbience, definitions);
        }

        private static void LogTrackIssues(string filename, IReadOnlyList<TrackTsmIssue> issues, Logger? logger)
        {
            if (issues == null || issues.Count == 0)
            {
                logger?.Warning(LocalizationService.Format(
                    LocalizationService.Mark("[TrackLoader] Failed to load '{0}'."),
                    filename));
                return;
            }

            logger?.Warning(LocalizationService.Format(
                LocalizationService.Mark("[TrackLoader] Failed to load '{0}':"),
                filename));
            for (var i = 0; i < issues.Count; i++)
                logger?.Warning("  - " + issues[i]);
        }
    }
}
