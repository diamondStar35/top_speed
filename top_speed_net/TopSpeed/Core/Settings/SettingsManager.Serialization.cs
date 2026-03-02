using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Json;
using System.Text;
using TopSpeed.Input;

namespace TopSpeed.Core.Settings
{
    internal sealed partial class SettingsManager
    {
        private static SettingsFileDocument? ReadDocument(string path)
        {
            var serializer = new DataContractJsonSerializer(typeof(SettingsFileDocument));
            using (var stream = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                return serializer.ReadObject(stream) as SettingsFileDocument;
            }
        }

        private static void WriteDocument(string path, SettingsFileDocument document)
        {
            var serializer = new DataContractJsonSerializer(typeof(SettingsFileDocument));
            using (var stream = new MemoryStream())
            {
                serializer.WriteObject(stream, document);
                stream.Position = 0;
                using (var reader = new StreamReader(stream, Encoding.UTF8, true))
                {
                    var compactJson = reader.ReadToEnd();
                    var prettyJson = PrettyPrintJson(compactJson);
                    File.WriteAllText(path, prettyJson + Environment.NewLine, new UTF8Encoding(false));
                }
            }
        }

        private static string PrettyPrintJson(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
                return "{}";

            var sb = new StringBuilder(json.Length + 256);
            var indentLevel = 0;
            var inString = false;
            var escaping = false;

            for (var i = 0; i < json.Length; i++)
            {
                var c = json[i];
                if (inString)
                {
                    sb.Append(c);
                    if (escaping)
                    {
                        escaping = false;
                    }
                    else if (c == '\\')
                    {
                        escaping = true;
                    }
                    else if (c == '"')
                    {
                        inString = false;
                    }

                    continue;
                }

                if (char.IsWhiteSpace(c))
                    continue;

                switch (c)
                {
                    case '"':
                        inString = true;
                        sb.Append(c);
                        break;
                    case '{':
                    case '[':
                    {
                        var closing = c == '{' ? '}' : ']';
                        var nextIndex = NextNonWhitespaceIndex(json, i + 1);
                        if (nextIndex >= 0 && json[nextIndex] == closing)
                        {
                            sb.Append(c);
                            sb.Append(closing);
                            i = nextIndex;
                            break;
                        }

                        sb.Append(c);
                        sb.AppendLine();
                        indentLevel++;
                        AppendIndent(sb, indentLevel);
                        break;
                    }
                    case '}':
                    case ']':
                        sb.AppendLine();
                        indentLevel = Math.Max(0, indentLevel - 1);
                        AppendIndent(sb, indentLevel);
                        sb.Append(c);
                        break;
                    case ',':
                        sb.Append(c);
                        sb.AppendLine();
                        AppendIndent(sb, indentLevel);
                        break;
                    case ':':
                        sb.Append(": ");
                        break;
                    default:
                        sb.Append(c);
                        break;
                }
            }

            return sb.ToString();
        }

        private static int NextNonWhitespaceIndex(string text, int start)
        {
            for (var i = start; i < text.Length; i++)
            {
                if (!char.IsWhiteSpace(text[i]))
                    return i;
            }

            return -1;
        }

        private static void AppendIndent(StringBuilder sb, int indentLevel)
        {
            for (var i = 0; i < indentLevel; i++)
            {
                sb.Append("  ");
            }
        }

        private static SettingsFileDocument BuildDocument(RaceSettings settings)
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
                    MusicPercent = audio.MusicPercent,
                    OnlineServerEventsPercent = audio.OnlineServerEventsPercent,
                    HrtfAudio = settings.HrtfAudio,
                    StereoWidening = settings.StereoWidening,
                    AutoDetectAudioDeviceFormat = settings.AutoDetectAudioDeviceFormat
                },
                Input = new SettingsInputDocument
                {
                    ForceFeedback = settings.ForceFeedback,
                    DeviceMode = (int)settings.DeviceMode,
                    Keyboard = new SettingsKeyboardDocument
                    {
                        Left = (int)settings.KeyLeft,
                        Right = (int)settings.KeyRight,
                        Throttle = (int)settings.KeyThrottle,
                        Brake = (int)settings.KeyBrake,
                        GearUp = (int)settings.KeyGearUp,
                        GearDown = (int)settings.KeyGearDown,
                        Horn = (int)settings.KeyHorn,
                        RequestInfo = (int)settings.KeyRequestInfo,
                        CurrentGear = (int)settings.KeyCurrentGear,
                        CurrentLapNr = (int)settings.KeyCurrentLapNr,
                        CurrentRacePerc = (int)settings.KeyCurrentRacePerc,
                        CurrentLapPerc = (int)settings.KeyCurrentLapPerc,
                        CurrentRaceTime = (int)settings.KeyCurrentRaceTime,
                        StartEngine = (int)settings.KeyStartEngine,
                        ReportDistance = (int)settings.KeyReportDistance,
                        ReportSpeed = (int)settings.KeyReportSpeed,
                        TrackName = (int)settings.KeyTrackName,
                        Pause = (int)settings.KeyPause
                    },
                    Joystick = new SettingsJoystickDocument
                    {
                        Left = (int)settings.JoystickLeft,
                        Right = (int)settings.JoystickRight,
                        Throttle = (int)settings.JoystickThrottle,
                        Brake = (int)settings.JoystickBrake,
                        GearUp = (int)settings.JoystickGearUp,
                        GearDown = (int)settings.JoystickGearDown,
                        Horn = (int)settings.JoystickHorn,
                        RequestInfo = (int)settings.JoystickRequestInfo,
                        CurrentGear = (int)settings.JoystickCurrentGear,
                        CurrentLapNr = (int)settings.JoystickCurrentLapNr,
                        CurrentRacePerc = (int)settings.JoystickCurrentRacePerc,
                        CurrentLapPerc = (int)settings.JoystickCurrentLapPerc,
                        CurrentRaceTime = (int)settings.JoystickCurrentRaceTime,
                        StartEngine = (int)settings.JoystickStartEngine,
                        ReportDistance = (int)settings.JoystickReportDistance,
                        ReportSpeed = (int)settings.JoystickReportSpeed,
                        TrackName = (int)settings.JoystickTrackName,
                        Pause = (int)settings.JoystickPause,
                        Center = new SettingsJoystickCenterDocument
                        {
                            X = settings.JoystickCenter.X,
                            Y = settings.JoystickCenter.Y,
                            Z = settings.JoystickCenter.Z,
                            Rx = settings.JoystickCenter.Rx,
                            Ry = settings.JoystickCenter.Ry,
                            Rz = settings.JoystickCenter.Rz,
                            Slider1 = settings.JoystickCenter.Slider1,
                            Slider2 = settings.JoystickCenter.Slider2
                        }
                    }
                },
                Race = new SettingsRaceDocument
                {
                    AutomaticInfo = (int)settings.AutomaticInfo,
                    Copilot = (int)settings.Copilot,
                    CurveAnnouncement = (int)settings.CurveAnnouncement,
                    NumberOfLaps = settings.NrOfLaps,
                    NumberOfComputers = settings.NrOfComputers,
                    Difficulty = (int)settings.Difficulty,
                    Units = (int)settings.Units,
                    RandomCustomTracks = settings.RandomCustomTracks,
                    RandomCustomVehicles = settings.RandomCustomVehicles,
                    SingleRaceCustomVehicles = settings.SingleRaceCustomVehicles
                },
                Ui = new SettingsUiDocument
                {
                    UsageHints = settings.UsageHints,
                    MenuWrapNavigation = settings.MenuWrapNavigation,
                    MenuSoundPreset = settings.MenuSoundPreset,
                    MenuNavigatePanning = settings.MenuNavigatePanning
                },
                Network = new SettingsNetworkDocument
                {
                    LastServerAddress = settings.LastServerAddress,
                    DefaultServerPort = settings.DefaultServerPort,
                    SavedServers = new SettingsSavedServersDocument
                    {
                        Servers = BuildSavedServers(settings.SavedServers)
                    }
                },
                Accessibility = new SettingsAccessibilityDocument
                {
                    ScreenReaderRateMs = Round3Decimal(settings.ScreenReaderRateMs)
                }
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
                    Port = entry.Port
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
