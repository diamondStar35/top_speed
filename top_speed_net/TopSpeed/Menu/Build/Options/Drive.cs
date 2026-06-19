using System;
using System.Globalization;
using System.Collections.Generic;
using TopSpeed.Data;
using TopSpeed.Input;

using TopSpeed.Localization;
namespace TopSpeed.Menu
{
    internal sealed partial class MenuRegistry
    {
        private MenuScreen BuildOptionsDriveSettingsMenu()
        {
            var items = new List<MenuItem>
            {
                new RadioButton(LocalizationService.Mark("Copilot"),
                    new[]
                    {
                        LocalizationService.Mark("off"),
                        LocalizationService.Mark("curves only"),
                        LocalizationService.Mark("all")
                    },
                    () => (int)_settings.Copilot,
                    value => _settingsActions.UpdateSetting(() => _settings.Copilot = (CopilotMode)value),
                    hintProvider: HintAdjustProvider(LocalizationService.Mark("Choose what information the copilot reports during a race."))),
                new Switch(LocalizationService.Mark("Curve announcements"),
                    LocalizationService.Mark("speed dependent"),
                    LocalizationService.Mark("fixed distance"),
                    () => _settings.CurveAnnouncement == CurveAnnouncementMode.SpeedDependent,
                    value => _settingsActions.UpdateSetting(() => _settings.CurveAnnouncement = value ? CurveAnnouncementMode.SpeedDependent : CurveAnnouncementMode.FixedDistance),
                    hintProvider: HintChangeProvider(LocalizationService.Mark("Switch between fixed distance and speed dependent curve announcements."))),
                new Slider(
                    LocalizationService.Mark("Speed dependent curve announcement lead time"),
                    "5-40",
                    () => (int)Math.Round(Math.Max(0.5f, Math.Min(4.0f, _settings.CurveAnnouncementLeadTimeSeconds)) * 10.0f),
                    value => _settingsActions.UpdateSetting(() => _settings.CurveAnnouncementLeadTimeSeconds = value / 10.0f),
                    hintProvider: HintForPlatformProvider(
                        LocalizationService.Mark("Sets how early speed dependent curve announcements are spoken."),
                        LocalizationService.Mark("Use LEFT or RIGHT to change by 0.1 seconds, PAGE UP or PAGE DOWN to change by 1.0 second, HOME for maximum, END for minimum."),
                        LocalizationService.Mark("Swipe up or down with two fingers to change by 1.0 second, swipe left or right with two fingers to change by 0.1 seconds, and swipe up or down with three fingers for maximum or minimum.")),
                    formatValue: FormatCurveLeadTimeSeconds),
                new RadioButton(LocalizationService.Mark("Automatic race information"),
                    new[]
                    {
                        LocalizationService.Mark("off"),
                        LocalizationService.Mark("laps only"),
                        LocalizationService.Mark("on")
                    },
                    () => (int)_settings.AutomaticInfo,
                    value => _settingsActions.UpdateSetting(() => _settings.AutomaticInfo = (AutomaticInfoMode)value),
                    hintProvider: HintAdjustProvider(LocalizationService.Mark("Choose how much automatic race information is spoken, such as lap numbers and player positions."))),
                new Slider(LocalizationService.Mark("Number of laps"),
                    "1-500",
                    () => _settings.NrOfLaps,
                    value => _settingsActions.UpdateSetting(() => _settings.NrOfLaps = value),
                    hintProvider: HintSliderProvider(LocalizationService.Mark("Sets how many laps the race will be for single race, time trial, and multiplayer."))),
                new Slider(LocalizationService.Mark("Number of computer players"),
                    "1-7",
                    () => _settings.NrOfComputers,
                    value => _settingsActions.UpdateSetting(() => _settings.NrOfComputers = value),
                    hintProvider: HintSliderProvider(LocalizationService.Mark("Sets how many computer-controlled cars will race against you."))),
                new RadioButton(LocalizationService.Mark("Single race difficulty"),
                    new[]
                    {
                        LocalizationService.Mark("easy"),
                        LocalizationService.Mark("normal"),
                        LocalizationService.Mark("hard")
                    },
                    () => (int)_settings.Difficulty,
                    value => _settingsActions.UpdateSetting(() => _settings.Difficulty = (RaceDifficulty)value),
                    hintProvider: HintAdjustProvider(LocalizationService.Mark("Choose the difficulty level for single races.")))
            };
            return BackMenu("options_drive", items);
        }

        private MenuScreen BuildOptionsLapsMenu()
        {
            var items = new List<MenuItem>();
            for (var laps = 1; laps <= 500; laps++)
            {
                var value = laps;
                items.Add(new MenuItem(laps.ToString(), MenuAction.Back, onActivate: () => _settingsActions.UpdateSetting(() => _settings.NrOfLaps = value)));
            }

            return BackMenu("options_drive_laps", items, LocalizationService.Mark("How many laps should the race be? This applies to single race, time trial, and multiplayer modes."));
        }

        private MenuScreen BuildOptionsComputersMenu()
        {
            var items = new List<MenuItem>();
            for (var count = 1; count <= 7; count++)
            {
                var value = count;
                items.Add(new MenuItem(count.ToString(), MenuAction.Back, onActivate: () => _settingsActions.UpdateSetting(() => _settings.NrOfComputers = value)));
            }

            return BackMenu("options_drive_computers", items, LocalizationService.Mark("Number of computer players"));
        }

        private static string FormatCurveLeadTimeSeconds(int tenths)
        {
            var clampedTenths = Math.Max(5, Math.Min(40, tenths));
            var seconds = (clampedTenths / 10.0f).ToString("0.0", CultureInfo.InvariantCulture);
            return LocalizationService.Format(LocalizationService.Mark("{0} seconds"), seconds);
        }
    }
}


