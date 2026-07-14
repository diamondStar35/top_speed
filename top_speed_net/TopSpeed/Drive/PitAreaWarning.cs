using TopSpeed.Localization;

namespace TopSpeed.Drive
{
    /// <summary>
    /// Shared logic for the "selected track has no pit area" warning. Reused by single-player /
    /// time-trial (<c>StartDrive</c>) and by the multiplayer host at options-confirm. The warning
    /// only matters when the track lacks a pit area AND at least one model that needs pitting (fuel
    /// consumption or tire wear) is enabled. <see cref="Tracks.Track.HasPitArea"/> is the single
    /// predicate that currently reports every track as having a pit area, so this stays dormant
    /// until pit-less tracks exist.
    /// </summary>
    internal static class PitAreaWarning
    {
        public static bool IsRequired(bool hasPitArea, bool fuelEnabled, bool tireEnabled)
        {
            return !hasPitArea && (fuelEnabled || tireEnabled);
        }

        public static string BuildTitle()
        {
            return LocalizationService.Mark("No pit area");
        }

        /// <summary>
        /// Caption naming the currently-enabled model(s) so the player understands what disabling
        /// would change. Assumes <see cref="IsRequired"/> already returned true.
        /// </summary>
        public static string BuildCaption(bool fuelEnabled, bool tireEnabled)
        {
            if (fuelEnabled && tireEnabled)
            {
                return LocalizationService.Mark(
                    "The selected track has no pit area, but fuel consumption and tire wear are enabled. Without a pit area you cannot refuel or replace tires during the race.");
            }

            if (fuelEnabled)
            {
                return LocalizationService.Mark(
                    "The selected track has no pit area, but fuel consumption is enabled. Without a pit area you cannot refuel during the race.");
            }

            return LocalizationService.Mark(
                "The selected track has no pit area, but tire wear is enabled. Without a pit area you cannot replace tires during the race.");
        }
    }
}
