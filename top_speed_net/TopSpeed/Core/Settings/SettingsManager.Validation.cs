using System;
using System.Collections.Generic;
using System.Globalization;
using SharpDX.DirectInput;
using TopSpeed.Input;

namespace TopSpeed.Core.Settings
{
    internal sealed partial class SettingsManager
    {
        private static int ClampPercent(int value, string field, List<SettingsIssue> issues)
        {
            var clamped = AudioVolumeSettings.ClampPercent(value);
            if (clamped != value)
                issues.Add(new SettingsIssue(SettingsIssueSeverity.Warning, field, $"The key {field} has invalid value {value}, which was replaced with {clamped}."));
            return clamped;
        }

        private static int ReadDefaultServerPort(int? value, int fallback, string field, List<SettingsIssue> issues)
        {
            if (!value.HasValue)
                return fallback;

            var current = value.Value;
            if (current >= 1 && current <= 65535)
                return current;

            issues.Add(new SettingsIssue(
                SettingsIssueSeverity.Warning,
                field,
                $"The key {field} has invalid value {current}, which was replaced with {fallback}."));
            return fallback;
        }

        private static int ClampInt(int? value, int fallback, int min, int max, string field, List<SettingsIssue> issues)
        {
            if (!value.HasValue)
                return fallback;

            var current = value.Value;
            if (current < min)
            {
                issues.Add(new SettingsIssue(SettingsIssueSeverity.Warning, field, $"The key {field} has invalid value {current}, which was replaced with {min}."));
                return min;
            }

            if (current > max)
            {
                issues.Add(new SettingsIssue(SettingsIssueSeverity.Warning, field, $"The key {field} has invalid value {current}, which was replaced with {max}."));
                return max;
            }

            return current;
        }

        private static float ClampFloat(float value, float min, float max, string field, List<SettingsIssue> issues)
        {
            if (float.IsNaN(value) || float.IsInfinity(value))
            {
                issues.Add(new SettingsIssue(SettingsIssueSeverity.Warning, field, $"The key {field} has invalid value {value.ToString(CultureInfo.InvariantCulture)}, which was replaced with {min.ToString(CultureInfo.InvariantCulture)}."));
                return min;
            }

            if (value < min)
            {
                issues.Add(new SettingsIssue(SettingsIssueSeverity.Warning, field, $"The key {field} has invalid value {value.ToString(CultureInfo.InvariantCulture)}, which was replaced with {min.ToString(CultureInfo.InvariantCulture)}."));
                return min;
            }

            if (value > max)
            {
                issues.Add(new SettingsIssue(SettingsIssueSeverity.Warning, field, $"The key {field} has invalid value {value.ToString(CultureInfo.InvariantCulture)}, which was replaced with {max.ToString(CultureInfo.InvariantCulture)}."));
                return max;
            }

            return value;
        }

        private static Key ReadKey(int? value, Key fallback, string field, List<SettingsIssue> issues)
        {
            if (!value.HasValue)
                return fallback;

            if (Enum.IsDefined(typeof(Key), value.Value) && value.Value >= 0)
                return (Key)value.Value;

            issues.Add(new SettingsIssue(SettingsIssueSeverity.Warning, field, $"The key {field} has invalid value {value.Value}, which was replaced with {(int)fallback}."));
            return fallback;
        }

        private static JoystickAxisOrButton ReadJoystick(int? value, JoystickAxisOrButton fallback, string field, List<SettingsIssue> issues)
        {
            if (!value.HasValue)
                return fallback;

            if (Enum.IsDefined(typeof(JoystickAxisOrButton), value.Value) && value.Value >= 0)
                return (JoystickAxisOrButton)value.Value;

            issues.Add(new SettingsIssue(SettingsIssueSeverity.Warning, field, $"The key {field} has invalid value {value.Value}, which was replaced with {(int)fallback}."));
            return fallback;
        }

        private static TEnum ReadEnum<TEnum>(object? value, TEnum fallback, string field, List<SettingsIssue> issues)
            where TEnum : struct, Enum
        {
            if (value == null)
                return fallback;

            try
            {
                var underlyingType = Enum.GetUnderlyingType(typeof(TEnum));
                var converted = Convert.ChangeType(value, underlyingType);
                if (Enum.IsDefined(typeof(TEnum), converted))
                    return (TEnum)Enum.ToObject(typeof(TEnum), converted);
            }
            catch
            {
                // Fall through to error
            }

            issues.Add(new SettingsIssue(SettingsIssueSeverity.Warning, field, $"The key {field} has invalid value {value}, which was replaced with {fallback}."));
            return fallback;
        }
    }
}
