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
                        language = val;
                    continue;
                }

                if (int.TryParse(line, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed))
                    values.Add(parsed);
            }

            settings.Language = language;
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
            AppendValue(lines, settings.HardwareAcceleration ? 1 : 0);
            AppendValue(lines, settings.ReverseStereo ? 1 : 0);
            AppendValue(lines, settings.RandomCustomTracks ? 1 : 0);
            AppendValue(lines, settings.RandomCustomVehicles ? 1 : 0);
            AppendValue(lines, settings.SingleRaceCustomVehicles ? 1 : 0);
            AppendValue(lines, (int)Math.Round(settings.MusicVolume * 100f));

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
            if (TryNext(values, ref index, out value)) settings.NrOfLaps = Math.Max(2, Math.Min(20, value));
            if (TryNext(values, ref index, out value)) settings.ServerNumber = value;
            if (TryNext(values, ref index, out value)) settings.NrOfComputers = Math.Max(1, Math.Min(7, value));
            if (TryNext(values, ref index, out value)) settings.Difficulty = AsDifficulty(value, settings.Difficulty);
            if (TryNext(values, ref index, out value)) settings.ThreeDSound = value != 0;
            if (TryNext(values, ref index, out value)) settings.HardwareAcceleration = value != 0;
            if (TryNext(values, ref index, out value)) settings.ReverseStereo = value != 0;
            if (TryNext(values, ref index, out value)) settings.RandomCustomTracks = value != 0;
            if (TryNext(values, ref index, out value)) settings.RandomCustomVehicles = value != 0;
            if (TryNext(values, ref index, out value)) settings.SingleRaceCustomVehicles = value != 0;
            if (TryNext(values, ref index, out value)) settings.MusicVolume = Math.Max(0f, Math.Min(1f, value / 100f));
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
