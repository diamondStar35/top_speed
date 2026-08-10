using System;
using TopSpeed.Localization;
using TopSpeed.Protocol;

namespace TopSpeed.Core.Multiplayer
{
    internal sealed partial class MultiplayerCoordinator
    {
        internal static string FormatTrackDisplayName(TrackPackageRef track, string trackName)
        {
            var display = FormatTrackRefDisplay(track);
            if (!string.IsNullOrWhiteSpace(display))
                return display;

            if (TryGetTrackDisplay(trackName, out var builtInDisplay))
                return LocalizationService.Translate(builtInDisplay);

            var fallback = (trackName ?? string.Empty).Trim();
            return string.IsNullOrWhiteSpace(fallback)
                ? LocalizationService.Mark("Unknown")
                : fallback;
        }

        private static string ResolveTrackAnnouncement(TrackPackageRef track, string trackName)
        {
            return FormatTrackDisplayName(track, trackName);
        }

        private static bool TryGetTrackDisplay(string trackKey, out string display)
        {
            display = string.Empty;
            if (string.IsNullOrWhiteSpace(trackKey))
                return false;

            for (var i = 0; i < RoomTrackOptions.Length; i++)
            {
                if (!string.Equals(RoomTrackOptions[i].Key, trackKey, StringComparison.OrdinalIgnoreCase))
                    continue;

                display = RoomTrackOptions[i].Display;
                return true;
            }

            return false;
        }

        private static TrackPackageRef CloneTrackRef(TrackPackageRef track)
        {
            return TrackPackageRef.Clone(track);
        }

        private static bool TrackRefsEqual(TrackPackageRef left, TrackPackageRef right)
        {
            return TrackPackageRef.AreEqual(left, right);
        }

        private static string FormatTrackRefDisplay(TrackPackageRef track)
        {
            if (track != null && track.IsRandomBuiltIn)
                return LocalizationService.Translate(LocalizationService.Mark("Random"));

            if (track != null && track.IsCustomPackage)
            {
                var id = string.IsNullOrWhiteSpace(track.TrackId) ? LocalizationService.Mark("Custom track") : track.TrackId;
                if (string.IsNullOrWhiteSpace(track.Version))
                    return id;
                return id + " (" + track.Version + ")";
            }

            var builtIn = track?.BuiltInTrackKey ?? string.Empty;
            if (TryGetTrackDisplay(builtIn, out var display))
                return LocalizationService.Translate(display);
            return builtIn;
        }
    }
}
