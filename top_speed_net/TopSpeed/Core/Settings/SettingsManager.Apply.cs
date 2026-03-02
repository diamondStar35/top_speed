using System.Collections.Generic;
using TopSpeed.Input;

namespace TopSpeed.Core.Settings
{
    internal sealed partial class SettingsManager
    {
        private static void ApplyDocument(RaceSettings settings, SettingsFileDocument document, List<SettingsIssue> issues)
        {
            settings.Language = string.IsNullOrWhiteSpace(document.Language)
                ? settings.Language
                : document.Language!;

            if (document.Audio == null)
                issues.Add(new SettingsIssue(SettingsIssueSeverity.Warning, "audio", "The audio section is missing. Defaults were used for audio settings."));
            else
                ApplyAudio(settings, document.Audio, issues);

            if (document.Input == null)
                issues.Add(new SettingsIssue(SettingsIssueSeverity.Warning, "input", "The input section is missing. Defaults were used for input settings."));
            else
                ApplyInput(settings, document.Input, issues);

            if (document.Race == null)
                issues.Add(new SettingsIssue(SettingsIssueSeverity.Warning, "race", "The race section is missing. Defaults were used for race settings."));
            else
                ApplyRace(settings, document.Race, issues);

            if (document.Ui == null)
                issues.Add(new SettingsIssue(SettingsIssueSeverity.Warning, "ui", "The ui section is missing. Defaults were used for menu settings."));
            else
                ApplyUi(settings, document.Ui, issues);

            if (document.Network == null)
                issues.Add(new SettingsIssue(SettingsIssueSeverity.Warning, "network", "The network section is missing. Defaults were used for network settings."));
            else
                ApplyNetwork(settings, document.Network, issues);

            if (document.Accessibility == null)
                issues.Add(new SettingsIssue(SettingsIssueSeverity.Warning, "accessibility", "The accessibility section is missing. Defaults were used for accessibility settings."));
            else
                ApplyAccessibility(settings, document.Accessibility, issues);
        }

        private static void ApplyAudio(RaceSettings settings, SettingsAudioDocument audio, List<SettingsIssue> issues)
        {
            settings.AudioVolumes ??= new AudioVolumeSettings();
            var hasCategoryVolumes = false;

            if (audio.MasterVolumePercent.HasValue)
            {
                settings.AudioVolumes.MasterPercent = ClampPercent(audio.MasterVolumePercent.Value, "audio.masterVolumePercent", issues);
                hasCategoryVolumes = true;
            }

            if (audio.PlayerVehicleEnginePercent.HasValue)
            {
                settings.AudioVolumes.PlayerVehicleEnginePercent = ClampPercent(audio.PlayerVehicleEnginePercent.Value, "audio.playerVehicleEnginePercent", issues);
                hasCategoryVolumes = true;
            }

            if (audio.PlayerVehicleEventsPercent.HasValue)
            {
                settings.AudioVolumes.PlayerVehicleEventsPercent = ClampPercent(audio.PlayerVehicleEventsPercent.Value, "audio.playerVehicleEventsPercent", issues);
                hasCategoryVolumes = true;
            }

            if (audio.OtherVehicleEnginePercent.HasValue)
            {
                settings.AudioVolumes.OtherVehicleEnginePercent = ClampPercent(audio.OtherVehicleEnginePercent.Value, "audio.otherVehicleEnginePercent", issues);
                hasCategoryVolumes = true;
            }

            if (audio.OtherVehicleEventsPercent.HasValue)
            {
                settings.AudioVolumes.OtherVehicleEventsPercent = ClampPercent(audio.OtherVehicleEventsPercent.Value, "audio.otherVehicleEventsPercent", issues);
                hasCategoryVolumes = true;
            }

            if (audio.SurfaceLoopsPercent.HasValue)
            {
                settings.AudioVolumes.SurfaceLoopsPercent = ClampPercent(audio.SurfaceLoopsPercent.Value, "audio.surfaceLoopsPercent", issues);
                hasCategoryVolumes = true;
            }

            if (audio.MusicPercent.HasValue)
            {
                settings.AudioVolumes.MusicPercent = ClampPercent(audio.MusicPercent.Value, "audio.musicPercent", issues);
                hasCategoryVolumes = true;
            }

            if (audio.OnlineServerEventsPercent.HasValue)
            {
                settings.AudioVolumes.OnlineServerEventsPercent = ClampPercent(audio.OnlineServerEventsPercent.Value, "audio.onlineServerEventsPercent", issues);
                hasCategoryVolumes = true;
            }

            if (audio.HrtfAudio.HasValue)
                settings.HrtfAudio = audio.HrtfAudio.Value;

            if (audio.StereoWidening.HasValue)
                settings.StereoWidening = audio.StereoWidening.Value;

            if (audio.AutoDetectAudioDeviceFormat.HasValue)
                settings.AutoDetectAudioDeviceFormat = audio.AutoDetectAudioDeviceFormat.Value;

            if (hasCategoryVolumes)
            {
                settings.AudioVolumes.ClampAll();
                settings.SyncMusicVolumeFromAudioCategories();
            }
            else if (audio.MusicVolume.HasValue)
            {
                var value = (float)audio.MusicVolume.Value;
                if (!float.IsNaN(value) && !float.IsInfinity(value))
                {
                    settings.MusicVolume = ClampFloat(value, 0f, 1f, "audio.musicVolume", issues);
                    settings.SyncAudioCategoriesFromMusicVolume();
                }
                else
                {
                    issues.Add(new SettingsIssue(SettingsIssueSeverity.Warning, "audio.musicVolume", "Music volume is not a valid number and was reset to default."));
                }
            }
        }

        private static void ApplyInput(RaceSettings settings, SettingsInputDocument input, List<SettingsIssue> issues)
        {
            if (input.ForceFeedback.HasValue)
                settings.ForceFeedback = input.ForceFeedback.Value;

            settings.DeviceMode = ReadEnum(input.DeviceMode, settings.DeviceMode, "input.deviceMode", issues);

            if (input.Keyboard == null)
                issues.Add(new SettingsIssue(SettingsIssueSeverity.Warning, "input.keyboard", "Keyboard bindings section is missing. Defaults were used for keyboard bindings."));
            else
                ApplyKeyboard(settings, input.Keyboard, issues);

            if (input.Joystick == null)
                issues.Add(new SettingsIssue(SettingsIssueSeverity.Warning, "input.joystick", "Joystick bindings section is missing. Defaults were used for joystick bindings."));
            else
                ApplyJoystick(settings, input.Joystick, issues);
        }

        private static void ApplyKeyboard(RaceSettings settings, SettingsKeyboardDocument keyboard, List<SettingsIssue> issues)
        {
            settings.KeyLeft = ReadKey(keyboard.Left, settings.KeyLeft, "input.keyboard.left", issues);
            settings.KeyRight = ReadKey(keyboard.Right, settings.KeyRight, "input.keyboard.right", issues);
            settings.KeyThrottle = ReadKey(keyboard.Throttle, settings.KeyThrottle, "input.keyboard.throttle", issues);
            settings.KeyBrake = ReadKey(keyboard.Brake, settings.KeyBrake, "input.keyboard.brake", issues);
            settings.KeyGearUp = ReadKey(keyboard.GearUp, settings.KeyGearUp, "input.keyboard.gearUp", issues);
            settings.KeyGearDown = ReadKey(keyboard.GearDown, settings.KeyGearDown, "input.keyboard.gearDown", issues);
            settings.KeyHorn = ReadKey(keyboard.Horn, settings.KeyHorn, "input.keyboard.horn", issues);
            settings.KeyRequestInfo = ReadKey(keyboard.RequestInfo, settings.KeyRequestInfo, "input.keyboard.requestInfo", issues);
            settings.KeyCurrentGear = ReadKey(keyboard.CurrentGear, settings.KeyCurrentGear, "input.keyboard.currentGear", issues);
            settings.KeyCurrentLapNr = ReadKey(keyboard.CurrentLapNr, settings.KeyCurrentLapNr, "input.keyboard.currentLapNr", issues);
            settings.KeyCurrentRacePerc = ReadKey(keyboard.CurrentRacePerc, settings.KeyCurrentRacePerc, "input.keyboard.currentRacePerc", issues);
            settings.KeyCurrentLapPerc = ReadKey(keyboard.CurrentLapPerc, settings.KeyCurrentLapPerc, "input.keyboard.currentLapPerc", issues);
            settings.KeyCurrentRaceTime = ReadKey(keyboard.CurrentRaceTime, settings.KeyCurrentRaceTime, "input.keyboard.currentRaceTime", issues);
            settings.KeyStartEngine = ReadKey(keyboard.StartEngine, settings.KeyStartEngine, "input.keyboard.startEngine", issues);
            settings.KeyReportDistance = ReadKey(keyboard.ReportDistance, settings.KeyReportDistance, "input.keyboard.reportDistance", issues);
            settings.KeyReportSpeed = ReadKey(keyboard.ReportSpeed, settings.KeyReportSpeed, "input.keyboard.reportSpeed", issues);
            settings.KeyTrackName = ReadKey(keyboard.TrackName, settings.KeyTrackName, "input.keyboard.trackName", issues);
            settings.KeyPause = ReadKey(keyboard.Pause, settings.KeyPause, "input.keyboard.pause", issues);
        }

        private static void ApplyJoystick(RaceSettings settings, SettingsJoystickDocument joystick, List<SettingsIssue> issues)
        {
            settings.JoystickLeft = ReadJoystick(joystick.Left, settings.JoystickLeft, "input.joystick.left", issues);
            settings.JoystickRight = ReadJoystick(joystick.Right, settings.JoystickRight, "input.joystick.right", issues);
            settings.JoystickThrottle = ReadJoystick(joystick.Throttle, settings.JoystickThrottle, "input.joystick.throttle", issues);
            settings.JoystickBrake = ReadJoystick(joystick.Brake, settings.JoystickBrake, "input.joystick.brake", issues);
            settings.JoystickGearUp = ReadJoystick(joystick.GearUp, settings.JoystickGearUp, "input.joystick.gearUp", issues);
            settings.JoystickGearDown = ReadJoystick(joystick.GearDown, settings.JoystickGearDown, "input.joystick.gearDown", issues);
            settings.JoystickHorn = ReadJoystick(joystick.Horn, settings.JoystickHorn, "input.joystick.horn", issues);
            settings.JoystickRequestInfo = ReadJoystick(joystick.RequestInfo, settings.JoystickRequestInfo, "input.joystick.requestInfo", issues);
            settings.JoystickCurrentGear = ReadJoystick(joystick.CurrentGear, settings.JoystickCurrentGear, "input.joystick.currentGear", issues);
            settings.JoystickCurrentLapNr = ReadJoystick(joystick.CurrentLapNr, settings.JoystickCurrentLapNr, "input.joystick.currentLapNr", issues);
            settings.JoystickCurrentRacePerc = ReadJoystick(joystick.CurrentRacePerc, settings.JoystickCurrentRacePerc, "input.joystick.currentRacePerc", issues);
            settings.JoystickCurrentLapPerc = ReadJoystick(joystick.CurrentLapPerc, settings.JoystickCurrentLapPerc, "input.joystick.currentLapPerc", issues);
            settings.JoystickCurrentRaceTime = ReadJoystick(joystick.CurrentRaceTime, settings.JoystickCurrentRaceTime, "input.joystick.currentRaceTime", issues);
            settings.JoystickStartEngine = ReadJoystick(joystick.StartEngine, settings.JoystickStartEngine, "input.joystick.startEngine", issues);
            settings.JoystickReportDistance = ReadJoystick(joystick.ReportDistance, settings.JoystickReportDistance, "input.joystick.reportDistance", issues);
            settings.JoystickReportSpeed = ReadJoystick(joystick.ReportSpeed, settings.JoystickReportSpeed, "input.joystick.reportSpeed", issues);
            settings.JoystickTrackName = ReadJoystick(joystick.TrackName, settings.JoystickTrackName, "input.joystick.trackName", issues);
            settings.JoystickPause = ReadJoystick(joystick.Pause, settings.JoystickPause, "input.joystick.pause", issues);

            if (joystick.Center == null)
                return;

            var center = settings.JoystickCenter;
            if (joystick.Center.X.HasValue) center.X = joystick.Center.X.Value;
            if (joystick.Center.Y.HasValue) center.Y = joystick.Center.Y.Value;
            if (joystick.Center.Z.HasValue) center.Z = joystick.Center.Z.Value;
            if (joystick.Center.Rx.HasValue) center.Rx = joystick.Center.Rx.Value;
            if (joystick.Center.Ry.HasValue) center.Ry = joystick.Center.Ry.Value;
            if (joystick.Center.Rz.HasValue) center.Rz = joystick.Center.Rz.Value;
            if (joystick.Center.Slider1.HasValue) center.Slider1 = joystick.Center.Slider1.Value;
            if (joystick.Center.Slider2.HasValue) center.Slider2 = joystick.Center.Slider2.Value;
            settings.JoystickCenter = center;
        }

        private static void ApplyRace(RaceSettings settings, SettingsRaceDocument race, List<SettingsIssue> issues)
        {
            settings.AutomaticInfo = ReadEnum(race.AutomaticInfo, settings.AutomaticInfo, "race.automaticInfo", issues);
            settings.Copilot = ReadEnum(race.Copilot, settings.Copilot, "race.copilot", issues);
            settings.CurveAnnouncement = ReadEnum(race.CurveAnnouncement, settings.CurveAnnouncement, "race.curveAnnouncement", issues);
            settings.NrOfLaps = ClampInt(race.NumberOfLaps, settings.NrOfLaps, 1, 16, "race.numberOfLaps", issues);
            settings.NrOfComputers = ClampInt(race.NumberOfComputers, settings.NrOfComputers, 1, 7, "race.numberOfComputers", issues);
            settings.Difficulty = ReadEnum(race.Difficulty, settings.Difficulty, "race.difficulty", issues);
            settings.Units = ReadEnum(race.Units, settings.Units, "race.units", issues);

            if (race.RandomCustomTracks.HasValue)
                settings.RandomCustomTracks = race.RandomCustomTracks.Value;
            if (race.RandomCustomVehicles.HasValue)
                settings.RandomCustomVehicles = race.RandomCustomVehicles.Value;
            if (race.SingleRaceCustomVehicles.HasValue)
                settings.SingleRaceCustomVehicles = race.SingleRaceCustomVehicles.Value;
        }

        private static void ApplyUi(RaceSettings settings, SettingsUiDocument ui, List<SettingsIssue> issues)
        {
            if (ui.UsageHints.HasValue)
                settings.UsageHints = ui.UsageHints.Value;
            if (ui.MenuWrapNavigation.HasValue)
                settings.MenuWrapNavigation = ui.MenuWrapNavigation.Value;
            if (ui.MenuNavigatePanning.HasValue)
                settings.MenuNavigatePanning = ui.MenuNavigatePanning.Value;

            if (ui.MenuSoundPreset == null)
                return;

            var preset = ui.MenuSoundPreset.Trim();
            if (preset.Length == 0)
            {
                issues.Add(new SettingsIssue(SettingsIssueSeverity.Warning, "ui.menuSoundPreset", "Menu sound preset was empty and was reset to default."));
                return;
            }

            settings.MenuSoundPreset = preset;
        }

        private static void ApplyNetwork(RaceSettings settings, SettingsNetworkDocument network, List<SettingsIssue> issues)
        {
            if (network.LastServerAddress != null)
                settings.LastServerAddress = network.LastServerAddress;

            settings.DefaultServerPort = ReadDefaultServerPort(network.DefaultServerPort, settings.DefaultServerPort, "network.defaultServerPort", issues);
            settings.SavedServers = ParseSavedServers(network.SavedServers?.Servers, issues);
        }

        private static List<SavedServerEntry> ParseSavedServers(List<SettingsSavedServerDocument>? savedServers, List<SettingsIssue> issues)
        {
            var result = new List<SavedServerEntry>();
            if (savedServers == null)
                return result;

            for (var i = 0; i < savedServers.Count; i++)
            {
                var entry = savedServers[i];
                if (entry == null)
                    continue;

                var host = (entry.Host ?? string.Empty).Trim();
                if (host.Length == 0)
                {
                    issues.Add(new SettingsIssue(SettingsIssueSeverity.Warning, $"network.savedServers.servers[{i}]", "A saved server entry was ignored because the host is empty."));
                    continue;
                }

                result.Add(new SavedServerEntry
                {
                    Name = (entry.Name ?? string.Empty).Trim(),
                    Host = host,
                    Port = ClampInt(entry.Port, 0, 0, 65535, $"network.savedServers.servers[{i}].port", issues)
                });
            }

            return result;
        }

        private static void ApplyAccessibility(RaceSettings settings, SettingsAccessibilityDocument accessibility, List<SettingsIssue> issues)
        {
            if (!accessibility.ScreenReaderRateMs.HasValue)
                return;

            var value = (float)accessibility.ScreenReaderRateMs.Value;
            if (!float.IsNaN(value) && !float.IsInfinity(value))
            {
                settings.ScreenReaderRateMs = ClampFloat(value, 0f, float.MaxValue, "accessibility.screenReaderRateMs", issues);
            }
            else
            {
                issues.Add(new SettingsIssue(SettingsIssueSeverity.Warning, "accessibility.screenReaderRateMs", "Screen reader rate is not a valid number and was reset to default."));
            }
        }
    }
}
