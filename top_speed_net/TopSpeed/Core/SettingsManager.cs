using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using TopSpeed.Input;

namespace TopSpeed.Core
{
    internal sealed class SettingsManager
    {
        private const string SettingsFileName = "TopSpeed.bin";
        private readonly string _settingsPath = string.Empty;

        public SettingsManager(string? settingsPath = null)
        {
            _settingsPath = string.IsNullOrWhiteSpace(settingsPath)
                ? Path.Combine(Directory.GetCurrentDirectory(), SettingsFileName)
                : settingsPath!;
        }

        public RaceSettings Load()
        {
            var settings = new RaceSettings();
            if (!File.Exists(_settingsPath))
            {
                Save(settings);
                return settings;
            }

            string[] lines;
            try
            {
                lines = File.ReadAllLines(_settingsPath);
            }
            catch
            {
                return settings;
            }

            var language = settings.Language;
            var serverAddress = settings.LastServerAddress;
            var screenReaderRate = settings.ScreenReaderRateMs;
            var menuSoundPreset = settings.MenuSoundPreset;
            var values = new List<int>();
            foreach (var rawLine in lines)
            {
                if (string.IsNullOrWhiteSpace(rawLine))
                    continue;

                var line = rawLine.Trim();
                var equals = line.IndexOf('=');
                if (equals > 0)
                {
                    var key = line.Substring(0, equals).Trim();
                    var val = line.Substring(equals + 1).Trim();
                    if (key.Equals("lang", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(val))
                    {
                        language = val;
                    }
                    else if ((key.Equals("server_addr", StringComparison.OrdinalIgnoreCase) ||
                              key.Equals("server_address", StringComparison.OrdinalIgnoreCase)) &&
                             !string.IsNullOrWhiteSpace(val))
                    {
                        serverAddress = val;
                    }
                    if (key.Equals("sr_rate", StringComparison.OrdinalIgnoreCase) ||
                        key.Equals("screen_reader_rate", StringComparison.OrdinalIgnoreCase))
                    {
                        if (float.TryParse(val, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsedRate))
                            screenReaderRate = parsedRate;
                    }
                    else if (key.Equals("menu_sound", StringComparison.OrdinalIgnoreCase) ||
                             key.Equals("menu_sound_preset", StringComparison.OrdinalIgnoreCase))
                    {
                        if (!string.IsNullOrWhiteSpace(val))
                            menuSoundPreset = val.Trim();
                    }
                    continue;
                }

                if (int.TryParse(line, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed))
                    values.Add(parsed);
            }

            settings.Language = language;
            settings.LastServerAddress = serverAddress ?? string.Empty;
            settings.ScreenReaderRateMs = screenReaderRate;
            settings.MenuSoundPreset = menuSoundPreset ?? settings.MenuSoundPreset;
            ApplyValues(settings, values);
            return settings;
        }

        public void Save(RaceSettings settings)
        {
            var language = string.IsNullOrWhiteSpace(settings.Language) ? "en" : settings.Language;
            var lines = new List<string>
            {
                $"lang={language}"
            };
            if (!string.IsNullOrWhiteSpace(settings.LastServerAddress))
                lines.Add($"server_addr={settings.LastServerAddress}");
            if (settings.ScreenReaderRateMs > 0f)
                lines.Add($"sr_rate={settings.ScreenReaderRateMs.ToString(CultureInfo.InvariantCulture)}");
            if (!string.IsNullOrWhiteSpace(settings.MenuSoundPreset))
                lines.Add($"menu_sound={settings.MenuSoundPreset}");

            AppendValue(lines, (int)settings.JoystickLeft);
            AppendValue(lines, (int)settings.JoystickRight);
            AppendValue(lines, (int)settings.JoystickThrottle);
            AppendValue(lines, (int)settings.JoystickBrake);
            AppendValue(lines, (int)settings.JoystickGearUp);
            AppendValue(lines, (int)settings.JoystickGearDown);
            AppendValue(lines, (int)settings.JoystickHorn);
            AppendValue(lines, (int)settings.JoystickRequestInfo);
            AppendValue(lines, (int)settings.JoystickCurrentGear);
            AppendValue(lines, (int)settings.JoystickCurrentLapNr);
            AppendValue(lines, (int)settings.JoystickCurrentRacePerc);
            AppendValue(lines, (int)settings.JoystickCurrentLapPerc);
            AppendValue(lines, (int)settings.JoystickCurrentRaceTime);
            AppendValue(lines, settings.JoystickCenter.X);
            AppendValue(lines, settings.JoystickCenter.Y);
            AppendValue(lines, settings.JoystickCenter.Z);
            AppendValue(lines, settings.JoystickCenter.Rx);
            AppendValue(lines, settings.JoystickCenter.Ry);
            AppendValue(lines, settings.JoystickCenter.Rz);
            AppendValue(lines, settings.JoystickCenter.Slider1);
            AppendValue(lines, settings.JoystickCenter.Slider2);
            AppendValue(lines, settings.ForceFeedback ? 1 : 0);
            AppendValue(lines, (int)settings.DeviceMode);
            AppendValue(lines, (int)settings.AutomaticInfo);
            AppendValue(lines, (int)settings.KeyLeft);
            AppendValue(lines, (int)settings.KeyRight);
            AppendValue(lines, (int)settings.KeyThrottle);
            AppendValue(lines, (int)settings.KeyBrake);
            AppendValue(lines, (int)settings.KeyGearUp);
            AppendValue(lines, (int)settings.KeyGearDown);
            AppendValue(lines, (int)settings.KeyHorn);
            AppendValue(lines, (int)settings.KeyRequestInfo);
            AppendValue(lines, (int)settings.KeyCurrentGear);
            AppendValue(lines, (int)settings.KeyCurrentLapNr);
            AppendValue(lines, (int)settings.KeyCurrentRacePerc);
            AppendValue(lines, (int)settings.KeyCurrentLapPerc);
            AppendValue(lines, (int)settings.KeyCurrentRaceTime);
            AppendValue(lines, (int)settings.Copilot);
            AppendValue(lines, (int)settings.CurveAnnouncement);
            AppendValue(lines, settings.NrOfLaps);
            AppendValue(lines, settings.ServerNumber);
            AppendValue(lines, settings.NrOfComputers);
            AppendValue(lines, (int)settings.Difficulty);
            AppendValue(lines, settings.ThreeDSound ? 1 : 0);
            AppendValue(lines, settings.ReverseStereo ? 1 : 0);
            AppendValue(lines, settings.RandomCustomTracks ? 1 : 0);
            AppendValue(lines, settings.RandomCustomVehicles ? 1 : 0);
            AppendValue(lines, settings.SingleRaceCustomVehicles ? 1 : 0);
            AppendValue(lines, (int)Math.Round(settings.MusicVolume * 100f));
            AppendValue(lines, settings.ServerPort);
            AppendValue(lines, (int)settings.JoystickStartEngine);
            AppendValue(lines, (int)settings.JoystickReportDistance);
            AppendValue(lines, (int)settings.JoystickReportSpeed);
            AppendValue(lines, (int)settings.KeyStartEngine);
            AppendValue(lines, (int)settings.KeyReportDistance);
            AppendValue(lines, (int)settings.KeyReportSpeed);
            AppendValue(lines, (int)settings.JoystickTrackName);
            AppendValue(lines, (int)settings.JoystickPause);
            AppendValue(lines, (int)settings.KeyTrackName);
            AppendValue(lines, (int)settings.KeyPause);
            AppendValue(lines, (int)settings.Units);
            AppendValue(lines, settings.UsageHints ? 1 : 0);
            AppendValue(lines, settings.MenuWrapNavigation ? 1 : 0);
            AppendValue(lines, settings.MenuNavigatePanning ? 1 : 0);
            AppendValue(lines, settings.AutoDetectAudioDeviceFormat ? 1 : 0);
            AppendValue(lines, (int)settings.JoystickReportWheelAngle);
            AppendValue(lines, (int)settings.JoystickReportHeading);
            AppendValue(lines, (int)settings.KeyReportWheelAngle);
            AppendValue(lines, (int)settings.KeyReportHeading);

            try
            {
                File.WriteAllLines(_settingsPath, lines);
            }
            catch
            {
                // Ignore settings write failures.
            }
        }

        private static void AppendValue(List<string> lines, int value)
        {
            lines.Add(value.ToString(CultureInfo.InvariantCulture));
        }

        private static void ApplyValues(RaceSettings settings, List<int> values)
        {
            var index = 0;
            var hasHardwareAcceleration = values.Count >= 50;
            if (TryNext(values, ref index, out var value)) settings.JoystickLeft = AsJoystick(value, settings.JoystickLeft);
            if (TryNext(values, ref index, out value)) settings.JoystickRight = AsJoystick(value, settings.JoystickRight);
            if (TryNext(values, ref index, out value)) settings.JoystickThrottle = AsJoystick(value, settings.JoystickThrottle);
            if (TryNext(values, ref index, out value)) settings.JoystickBrake = AsJoystick(value, settings.JoystickBrake);
            if (TryNext(values, ref index, out value)) settings.JoystickGearUp = AsJoystick(value, settings.JoystickGearUp);
            if (TryNext(values, ref index, out value)) settings.JoystickGearDown = AsJoystick(value, settings.JoystickGearDown);
            if (TryNext(values, ref index, out value)) settings.JoystickHorn = AsJoystick(value, settings.JoystickHorn);
            if (TryNext(values, ref index, out value)) settings.JoystickRequestInfo = AsJoystick(value, settings.JoystickRequestInfo);
            if (TryNext(values, ref index, out value)) settings.JoystickCurrentGear = AsJoystick(value, settings.JoystickCurrentGear);
            if (TryNext(values, ref index, out value)) settings.JoystickCurrentLapNr = AsJoystick(value, settings.JoystickCurrentLapNr);
            if (TryNext(values, ref index, out value)) settings.JoystickCurrentRacePerc = AsJoystick(value, settings.JoystickCurrentRacePerc);
            if (TryNext(values, ref index, out value)) settings.JoystickCurrentLapPerc = AsJoystick(value, settings.JoystickCurrentLapPerc);
            if (TryNext(values, ref index, out value)) settings.JoystickCurrentRaceTime = AsJoystick(value, settings.JoystickCurrentRaceTime);
            if (TryNext(values, ref index, out value)) settings.JoystickCenter = SetAxis(settings.JoystickCenter, value, AxisField.X);
            if (TryNext(values, ref index, out value)) settings.JoystickCenter = SetAxis(settings.JoystickCenter, value, AxisField.Y);
            if (TryNext(values, ref index, out value)) settings.JoystickCenter = SetAxis(settings.JoystickCenter, value, AxisField.Z);
            if (TryNext(values, ref index, out value)) settings.JoystickCenter = SetAxis(settings.JoystickCenter, value, AxisField.Rx);
            if (TryNext(values, ref index, out value)) settings.JoystickCenter = SetAxis(settings.JoystickCenter, value, AxisField.Ry);
            if (TryNext(values, ref index, out value)) settings.JoystickCenter = SetAxis(settings.JoystickCenter, value, AxisField.Rz);
            if (TryNext(values, ref index, out value)) settings.JoystickCenter = SetAxis(settings.JoystickCenter, value, AxisField.Slider1);
            if (TryNext(values, ref index, out value)) settings.JoystickCenter = SetAxis(settings.JoystickCenter, value, AxisField.Slider2);
            if (TryNext(values, ref index, out value)) settings.ForceFeedback = value != 0;
            if (TryNext(values, ref index, out value)) settings.DeviceMode = AsDeviceMode(value);
            if (TryNext(values, ref index, out value)) settings.AutomaticInfo = AsAutomaticInfo(value, settings.AutomaticInfo);
            if (TryNext(values, ref index, out value)) settings.KeyLeft = AsKey(value, settings.KeyLeft);
            if (TryNext(values, ref index, out value)) settings.KeyRight = AsKey(value, settings.KeyRight);
            if (TryNext(values, ref index, out value)) settings.KeyThrottle = AsKey(value, settings.KeyThrottle);
            if (TryNext(values, ref index, out value)) settings.KeyBrake = AsKey(value, settings.KeyBrake);
            if (TryNext(values, ref index, out value)) settings.KeyGearUp = AsKey(value, settings.KeyGearUp);
            if (TryNext(values, ref index, out value)) settings.KeyGearDown = AsKey(value, settings.KeyGearDown);
            if (TryNext(values, ref index, out value)) settings.KeyHorn = AsKey(value, settings.KeyHorn);
            if (TryNext(values, ref index, out value)) settings.KeyRequestInfo = AsKey(value, settings.KeyRequestInfo);
            if (TryNext(values, ref index, out value)) settings.KeyCurrentGear = AsKey(value, settings.KeyCurrentGear);
            if (TryNext(values, ref index, out value)) settings.KeyCurrentLapNr = AsKey(value, settings.KeyCurrentLapNr);
            if (TryNext(values, ref index, out value)) settings.KeyCurrentRacePerc = AsKey(value, settings.KeyCurrentRacePerc);
            if (TryNext(values, ref index, out value)) settings.KeyCurrentLapPerc = AsKey(value, settings.KeyCurrentLapPerc);
            if (TryNext(values, ref index, out value)) settings.KeyCurrentRaceTime = AsKey(value, settings.KeyCurrentRaceTime);
            if (TryNext(values, ref index, out value)) settings.Copilot = AsCopilot(value, settings.Copilot);
            if (TryNext(values, ref index, out value)) settings.CurveAnnouncement = AsCurveAnnouncement(value, settings.CurveAnnouncement);
            if (TryNext(values, ref index, out value)) settings.NrOfLaps = Math.Max(1, Math.Min(16, value));
            if (TryNext(values, ref index, out value)) settings.ServerNumber = value;
            if (TryNext(values, ref index, out value)) settings.NrOfComputers = Math.Max(1, Math.Min(7, value));
            if (TryNext(values, ref index, out value)) settings.Difficulty = AsDifficulty(value, settings.Difficulty);
            if (TryNext(values, ref index, out value)) settings.ThreeDSound = value != 0;
            if (TryNext(values, ref index, out value)) settings.ReverseStereo = value != 0;
            if (TryNext(values, ref index, out value)) settings.RandomCustomTracks = value != 0;
            if (TryNext(values, ref index, out value)) settings.RandomCustomVehicles = value != 0;
            if (TryNext(values, ref index, out value)) settings.SingleRaceCustomVehicles = value != 0;
            if (TryNext(values, ref index, out value)) settings.MusicVolume = Math.Max(0f, Math.Min(1f, value / 100f));
            if (TryNext(values, ref index, out value)) settings.ServerPort = ClampPort(value, settings.ServerPort);
            if (TryNext(values, ref index, out value)) settings.JoystickStartEngine = AsJoystick(value, settings.JoystickStartEngine);
            if (TryNext(values, ref index, out value)) settings.JoystickReportDistance = AsJoystick(value, settings.JoystickReportDistance);
            if (TryNext(values, ref index, out value)) settings.JoystickReportSpeed = AsJoystick(value, settings.JoystickReportSpeed);
            if (TryNext(values, ref index, out value)) settings.KeyStartEngine = AsKey(value, settings.KeyStartEngine);
            if (TryNext(values, ref index, out value)) settings.KeyReportDistance = AsKey(value, settings.KeyReportDistance);
            if (TryNext(values, ref index, out value)) settings.KeyReportSpeed = AsKey(value, settings.KeyReportSpeed);
            if (TryNext(values, ref index, out value)) settings.JoystickTrackName = AsJoystick(value, settings.JoystickTrackName);
            if (TryNext(values, ref index, out value)) settings.JoystickPause = AsJoystick(value, settings.JoystickPause);
            if (TryNext(values, ref index, out value)) settings.KeyTrackName = AsKey(value, settings.KeyTrackName);
            if (TryNext(values, ref index, out value)) settings.KeyPause = AsKey(value, settings.KeyPause);
            if (TryNext(values, ref index, out value)) settings.Units = AsUnitSystem(value, settings.Units);
            if (TryNext(values, ref index, out value)) settings.UsageHints = value != 0;
            if (TryNext(values, ref index, out value)) settings.MenuWrapNavigation = value != 0;
            if (TryNext(values, ref index, out value)) settings.MenuNavigatePanning = value != 0;
            if (TryNext(values, ref index, out value)) settings.AutoDetectAudioDeviceFormat = value != 0;
            if (TryNext(values, ref index, out value)) { }
            if (TryNext(values, ref index, out value)) settings.JoystickReportWheelAngle = AsJoystick(value, settings.JoystickReportWheelAngle);
            if (TryNext(values, ref index, out value)) settings.JoystickReportHeading = AsJoystick(value, settings.JoystickReportHeading);
            if (TryNext(values, ref index, out value)) settings.KeyReportWheelAngle = AsKey(value, settings.KeyReportWheelAngle);
            if (TryNext(values, ref index, out value)) settings.KeyReportHeading = AsKey(value, settings.KeyReportHeading);
        }

        private static int ClampPort(int value, int fallback)
        {
            if (value <= 0)
                return 0;
            return value >= 1 && value <= 65535 ? value : fallback;
        }

        private static bool TryNext(List<int> values, ref int index, out int value)
        {
            if (index >= values.Count)
            {
                value = 0;
                return false;
            }
            value = values[index++];
            return true;
        }

        private static JoystickAxisOrButton AsJoystick(int value, JoystickAxisOrButton fallback)
        {
            return value >= 0 ? (JoystickAxisOrButton)value : fallback;
        }

        private static SharpDX.DirectInput.Key AsKey(int value, SharpDX.DirectInput.Key fallback)
        {
            return value >= 0 ? (SharpDX.DirectInput.Key)value : fallback;
        }

        private static InputDeviceMode AsDeviceMode(int value)
        {
            if (value <= 0)
                return InputDeviceMode.Keyboard;
            if (value == 1)
                return InputDeviceMode.Joystick;
            return InputDeviceMode.Both;
        }

        private static AutomaticInfoMode AsAutomaticInfo(int value, AutomaticInfoMode fallback)
        {
            return value switch
            {
                0 => AutomaticInfoMode.Off,
                1 => AutomaticInfoMode.LapsOnly,
                2 => AutomaticInfoMode.On,
                _ => fallback
            };
        }

        private static CopilotMode AsCopilot(int value, CopilotMode fallback)
        {
            return value switch
            {
                0 => CopilotMode.Off,
                1 => CopilotMode.CurvesOnly,
                2 => CopilotMode.All,
                _ => fallback
            };
        }

        private static CurveAnnouncementMode AsCurveAnnouncement(int value, CurveAnnouncementMode fallback)
        {
            return value switch
            {
                0 => CurveAnnouncementMode.FixedDistance,
                1 => CurveAnnouncementMode.SpeedDependent,
                _ => fallback
            };
        }

        private static RaceDifficulty AsDifficulty(int value, RaceDifficulty fallback)
        {
            return value switch
            {
                0 => RaceDifficulty.Easy,
                1 => RaceDifficulty.Normal,
                2 => RaceDifficulty.Hard,
                _ => fallback
            };
        }

        private static UnitSystem AsUnitSystem(int value, UnitSystem fallback)
        {
            return value switch
            {
                0 => UnitSystem.Metric,
                1 => UnitSystem.Imperial,
                _ => fallback
            };
        }

        private enum AxisField
        {
            X,
            Y,
            Z,
            Rx,
            Ry,
            Rz,
            Slider1,
            Slider2
        }

        private static JoystickStateSnapshot SetAxis(JoystickStateSnapshot snapshot, int value, AxisField field)
        {
            switch (field)
            {
                case AxisField.X:
                    snapshot.X = value;
                    break;
                case AxisField.Y:
                    snapshot.Y = value;
                    break;
                case AxisField.Z:
                    snapshot.Z = value;
                    break;
                case AxisField.Rx:
                    snapshot.Rx = value;
                    break;
                case AxisField.Ry:
                    snapshot.Ry = value;
                    break;
                case AxisField.Rz:
                    snapshot.Rz = value;
                    break;
                case AxisField.Slider1:
                    snapshot.Slider1 = value;
                    break;
                case AxisField.Slider2:
                    snapshot.Slider2 = value;
                    break;
            }
            return snapshot;
        }
    }
}
