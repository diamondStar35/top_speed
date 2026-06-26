using System.Collections.Generic;
using TopSpeed.Input;
using TopSpeed.Localization;

namespace TopSpeed.Core.Settings
{
    internal sealed partial class SettingsManager
    {
        private static void ApplyRace(DriveSettings settings, SettingsRaceDocument race, List<SettingsIssue> issues)
        {
            settings.AutomaticInfo = ReadEnum(race.AutomaticInfo, settings.AutomaticInfo, "race.automaticInfo", issues);
            settings.Copilot = ReadEnum(race.Copilot, settings.Copilot, "race.copilot", issues);
            settings.CurveAnnouncement = ReadEnum(race.CurveAnnouncement, settings.CurveAnnouncement, "race.curveAnnouncement", issues);
            if (race.CurveAnnouncementLeadTimeSeconds.HasValue)
            {
                settings.CurveAnnouncementLeadTimeSeconds = ClampFloat(
                    (float)race.CurveAnnouncementLeadTimeSeconds.Value,
                    0.5f,
                    4.0f,
                    "race.curveAnnouncementLeadTimeSeconds",
                    issues);
            }
            settings.NrOfLaps = ClampInt(race.NumberOfLaps, settings.NrOfLaps, 1, 16, "race.numberOfLaps", issues);
            settings.NrOfComputers = ClampInt(race.NumberOfComputers, settings.NrOfComputers, 1, 7, "race.numberOfComputers", issues);
            settings.Difficulty = ReadEnum(race.Difficulty, settings.Difficulty, "race.difficulty", issues);
            settings.Units = ReadEnum(race.Units, settings.Units, "race.units", issues);

            if (race.FuelConsumptionEnabled.HasValue)
                settings.FuelConsumptionEnabled = race.FuelConsumptionEnabled.Value;
            if (race.TireWearEnabled.HasValue)
                settings.TireWearEnabled = race.TireWearEnabled.Value;
            if (race.RandomCustomTracks.HasValue)
                settings.RandomCustomTracks = race.RandomCustomTracks.Value;
            if (race.RandomCustomVehicles.HasValue)
                settings.RandomCustomVehicles = race.RandomCustomVehicles.Value;
            if (race.SingleRaceCustomVehicles.HasValue)
                settings.SingleRaceCustomVehicles = race.SingleRaceCustomVehicles.Value;
        }

        private static void ApplyUi(DriveSettings settings, SettingsUiDocument ui, List<SettingsIssue> issues)
        {
            if (ui.UsageHints.HasValue)
                settings.UsageHints = ui.UsageHints.Value;
            if (ui.MenuAutoFocus.HasValue)
                settings.MenuAutoFocus = ui.MenuAutoFocus.Value;
            if (ui.MenuWrapNavigation.HasValue)
                settings.MenuWrapNavigation = ui.MenuWrapNavigation.Value;
            if (ui.MenuNavigatePanning.HasValue)
                settings.MenuNavigatePanning = ui.MenuNavigatePanning.Value;
            if (ui.PlayLogoAtStartup.HasValue)
                settings.PlayLogoAtStartup = ui.PlayLogoAtStartup.Value;
            if (ui.AutoCheckUpdates.HasValue)
                settings.AutoCheckUpdates = ui.AutoCheckUpdates.Value;

            if (ui.MenuSoundPreset == null)
                return;

            var preset = ui.MenuSoundPreset.Trim();
            if (preset.Length == 0)
            {
                issues.Add(new SettingsIssue(
                    SettingsIssueSeverity.Warning,
                    "ui.menuSoundPreset",
                    LocalizationService.Mark("Menu sound preset was empty and was reset to default.")));
                return;
            }

            settings.MenuSoundPreset = preset;
        }
    }
}


