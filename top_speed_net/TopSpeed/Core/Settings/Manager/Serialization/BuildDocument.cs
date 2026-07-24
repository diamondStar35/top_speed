using System;
using System.Collections.Generic;
using TopSpeed.Input;

namespace TopSpeed.Core.Settings
{
    internal sealed partial class SettingsManager
    {
        private static SettingsFileDocument BuildDocument(DriveSettings settings)
        {
            var audio = settings.AudioVolumes ?? new AudioVolumeSettings();
            audio.ClampAll();

            return new SettingsFileDocument
            {
                SchemaVersion = CurrentSchemaVersion,
                Language = settings.Language,
                Audio = new SettingsAudioDocument
                {
                    MusicVolume = Round3Decimal(settings.MusicVolume),
                    MasterVolumePercent = audio.MasterPercent,
                    PlayerVehicleEnginePercent = audio.PlayerVehicleEnginePercent,
                    PlayerVehicleEventsPercent = audio.PlayerVehicleEventsPercent,
                    OtherVehicleEnginePercent = audio.OtherVehicleEnginePercent,
                    OtherVehicleEventsPercent = audio.OtherVehicleEventsPercent,
                    SurfaceLoopsPercent = audio.SurfaceLoopsPercent,
                    RadioPercent = audio.RadioPercent,
                    AmbientsAndSourcesPercent = audio.AmbientsAndSourcesPercent,
                    MusicPercent = audio.MusicPercent,
                    OnlineServerEventsPercent = audio.OnlineServerEventsPercent,
                    CommunicatorPercent = audio.CommunicatorPercent,
                    HrtfAudio = settings.HrtfAudio,
                    StereoWidening = settings.StereoWidening,
                    AutoDetectAudioDeviceFormat = settings.AutoDetectAudioDeviceFormat,
                    VoiceInputDevice = settings.VoiceInputDeviceName,
                    VoiceInputGainPercent = settings.VoiceInputGainPercent
                },
                Input = new SettingsInputDocument
                {
                    ForceFeedback = settings.ForceFeedback,
                    KeyboardProgressiveRate = (int)settings.KeyboardProgressiveRate,
                    DeviceMode = (int)settings.DeviceMode,
                    AndroidUseMotionSteering = settings.AndroidUseMotionSteering,
                    Keyboard = new SettingsKeyboardDocument
                    {
                        Bindings = BuildKeyboardBindings(settings.KeyboardBindings)
                    },
                    MenuShortcuts = BuildMenuShortcuts(settings.ShortcutKeyBindings, settings.ShortcutModifierBindings),
                    Controller = new SettingsControllerDocument
                    {
                        Bindings = BuildControllerBindings(settings.ControllerBindings),
                        ThrottleInvertMode = (int)settings.ControllerThrottleInvertMode,
                        BrakeInvertMode = (int)settings.ControllerBrakeInvertMode,
                        ClutchInvertMode = (int)settings.ControllerClutchInvertMode,
                        SteeringDeadZone = settings.ControllerSteeringDeadZone,
                        Center = new SettingsControllerCenterDocument
                        {
                            X = settings.ControllerCenter.X,
                            Y = settings.ControllerCenter.Y,
                            Z = settings.ControllerCenter.Z,
                            Rx = settings.ControllerCenter.Rx,
                            Ry = settings.ControllerCenter.Ry,
                            Rz = settings.ControllerCenter.Rz,
                            Slider1 = settings.ControllerCenter.Slider1,
                            Slider2 = settings.ControllerCenter.Slider2
                        }
                    }
                },
                Race = new SettingsRaceDocument
                {
                    AutomaticInfo = (int)settings.AutomaticInfo,
                    Copilot = (int)settings.Copilot,
                    CurveAnnouncement = (int)settings.CurveAnnouncement,
                    CurveAnnouncementLeadTimeSeconds = Round3Decimal(settings.CurveAnnouncementLeadTimeSeconds),
                    NumberOfLaps = settings.NrOfLaps,
                    NumberOfComputers = settings.NrOfComputers,
                    Difficulty = (int)settings.Difficulty,
                    Units = (int)settings.Units,
                    FuelConsumptionEnabled = settings.FuelConsumptionEnabled,
                    TireWearEnabled = settings.TireWearEnabled,
                    BriefStatusReports = settings.BriefStatusReports,
                    RandomCustomTracks = settings.RandomCustomTracks,
                    RandomCustomVehicles = settings.RandomCustomVehicles,
                    SingleRaceCustomVehicles = settings.SingleRaceCustomVehicles
                },
                Ui = new SettingsUiDocument
                {
                    UsageHints = settings.UsageHints,
                    MenuAutoFocus = settings.MenuAutoFocus,
                    MenuWrapNavigation = settings.MenuWrapNavigation,
                    MenuSoundPreset = settings.MenuSoundPreset,
                    MenuNavigatePanning = settings.MenuNavigatePanning,
                    PlayLogoAtStartup = settings.PlayLogoAtStartup,
                    AutoCheckUpdates = settings.AutoCheckUpdates
                },
                Speech = new SettingsSpeechDocument
                {
                    Mode = (int)settings.SpeechMode,
                    ScreenReaderRateMs = Round3Decimal(settings.ScreenReaderRateMs),
                    Backend = settings.SpeechBackendId,
                    Voice = settings.SpeechVoiceIndex,
                    Rate = Round3Decimal(settings.SpeechRate),
                    Interrupt = settings.ScreenReaderInterrupt
                },
                Network = new SettingsNetworkDocument
                {
                    LastServerAddress = settings.LastServerAddress,
                    DefaultServerPort = settings.DefaultServerPort,
                    DefaultCallSign = settings.DefaultCallSign,
                    UseUpdateProxy = settings.UseUpdateProxy,
                    UpdateProxyUrlPrefix = settings.UpdateProxyUrlPrefix,
                    SavedServers = new SettingsSavedServersDocument
                    {
                        Servers = BuildSavedServers(settings.SavedServers)
                    }
                },
                Radio = new SettingsRadioDocument
                {
                    LastFolder = settings.RadioLastFolder,
                    ShuffleEnabled = settings.RadioShuffle
                }
            };
        }

        private static List<SettingsKeyboardBindingDocument> BuildKeyboardBindings(Dictionary<DriveIntent, InputKey>? bindings)
        {
            var result = new List<SettingsKeyboardBindingDocument>();
            if (bindings == null)
                return result;

            foreach (var pair in bindings)
            {
                if (pair.Key == DriveIntent.Steering)
                    continue;

                result.Add(new SettingsKeyboardBindingDocument
                {
                    Intent = pair.Key.ToString(),
                    Key = (int)pair.Value
                });
            }

            result.Sort((left, right) => string.Compare(left.Intent, right.Intent, StringComparison.Ordinal));
            return result;
        }

        private static List<SettingsControllerBindingDocument> BuildControllerBindings(Dictionary<DriveIntent, TopSpeed.Input.Devices.Controller.AxisOrButton>? bindings)
        {
            var result = new List<SettingsControllerBindingDocument>();
            if (bindings == null)
                return result;

            foreach (var pair in bindings)
            {
                if (pair.Key == DriveIntent.Steering)
                    continue;

                result.Add(new SettingsControllerBindingDocument
                {
                    Intent = pair.Key.ToString(),
                    Axis = (int)pair.Value
                });
            }

            result.Sort((left, right) => string.Compare(left.Intent, right.Intent, StringComparison.Ordinal));
            return result;
        }

        private static SettingsMenuShortcutsDocument BuildMenuShortcuts(
            Dictionary<string, InputKey>? shortcuts,
            Dictionary<string, TopSpeed.Shortcuts.ShortcutModifiers>? modifiers)
        {
            var bindings = new List<SettingsMenuShortcutBindingDocument>();
            var shortcutModifiers = modifiers ?? new Dictionary<string, TopSpeed.Shortcuts.ShortcutModifiers>(StringComparer.Ordinal);
            if (shortcuts != null)
            {
                foreach (var pair in shortcuts)
                {
                    if (string.IsNullOrWhiteSpace(pair.Key))
                        continue;

                    shortcutModifiers.TryGetValue(pair.Key, out var currentModifiers);
                    bindings.Add(new SettingsMenuShortcutBindingDocument
                    {
                        Id = pair.Key,
                        Key = (int)pair.Value,
                        Shift = currentModifiers.Shift,
                        Control = currentModifiers.Control,
                        Alt = currentModifiers.Alt
                    });
                }
            }

            bindings.Sort((left, right) => string.Compare(left.Id, right.Id, StringComparison.Ordinal));
            return new SettingsMenuShortcutsDocument
            {
                Bindings = bindings
            };
        }

        private static List<SettingsSavedServerDocument> BuildSavedServers(List<SavedServerEntry>? savedServers)
        {
            var result = new List<SettingsSavedServerDocument>();
            if (savedServers == null)
                return result;

            for (var i = 0; i < savedServers.Count; i++)
            {
                var entry = savedServers[i];
                if (entry == null || string.IsNullOrWhiteSpace(entry.Host))
                    continue;

                result.Add(new SettingsSavedServerDocument
                {
                    Name = entry.Name,
                    Host = entry.Host,
                    Port = entry.Port,
                    DefaultCallSign = entry.DefaultCallSign
                });
            }

            return result;
        }

        private static decimal Round3Decimal(float value)
        {
            if (float.IsNaN(value) || float.IsInfinity(value))
                return 0m;
            return Math.Round((decimal)value, 3, MidpointRounding.AwayFromZero);
        }
    }
}


