using System.Collections.Generic;
using System.Globalization;

namespace TopSpeed.Vehicles.Parsing
{
    public static partial class VehicleTsvParser
    {
        private static int RequireIntRange(Section section, string key, int min, int max, List<VehicleTsvIssue> issues)
        {
            if (!section.Entries.TryGetValue(key, out var entry))
            {
                issues.Add(new VehicleTsvIssue(VehicleTsvIssueSeverity.Error, section.Line, Localized("Missing required key '{0}' in section [{1}].", key, section.Name)));
                return min;
            }

            if (!int.TryParse(entry.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value))
            {
                issues.Add(new VehicleTsvIssue(VehicleTsvIssueSeverity.Error, entry.Line, Localized("Key '{0}' must be an integer.", key)));
                return min;
            }

            if (value < min || value > max)
                issues.Add(new VehicleTsvIssue(VehicleTsvIssueSeverity.Error, entry.Line, Localized("Key '{0}' value {1} is outside allowed range {2} to {3}.", key, value, min, max)));
            return value;
        }

        private static int? OptionalInt(Section section, string key, List<VehicleTsvIssue> issues)
        {
            if (!section.Entries.TryGetValue(key, out var entry))
                return null;
            if (!int.TryParse(entry.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value))
            {
                issues.Add(new VehicleTsvIssue(VehicleTsvIssueSeverity.Error, entry.Line, Localized("Key '{0}' must be an integer.", key)));
                return null;
            }

            return value;
        }

        private static float RequireFloatRange(Section section, string key, float min, float max, List<VehicleTsvIssue> issues)
        {
            if (!section.Entries.TryGetValue(key, out var entry))
            {
                issues.Add(new VehicleTsvIssue(VehicleTsvIssueSeverity.Error, section.Line, Localized("Missing required key '{0}' in section [{1}].", key, section.Name)));
                return min;
            }

            if (!TryParseFloat(entry.Value, out var value))
            {
                issues.Add(new VehicleTsvIssue(VehicleTsvIssueSeverity.Error, entry.Line, Localized("Key '{0}' must be a float.", key)));
                return min;
            }

            if (value < min || value > max)
            {
                issues.Add(new VehicleTsvIssue(
                    VehicleTsvIssueSeverity.Error,
                    entry.Line,
                    Localized(
                        "Key '{0}' value {1} is outside allowed range {2} to {3}.",
                        key,
                        value.ToString(CultureInfo.InvariantCulture),
                        min.ToString(CultureInfo.InvariantCulture),
                        max.ToString(CultureInfo.InvariantCulture))));
            }

            return value;
        }

        private static float? OptionalFloat(Section section, string key, List<VehicleTsvIssue> issues)
        {
            if (!section.Entries.TryGetValue(key, out var entry))
                return null;
            if (!TryParseFloat(entry.Value, out var value))
            {
                issues.Add(new VehicleTsvIssue(VehicleTsvIssueSeverity.Error, entry.Line, Localized("Key '{0}' must be a float.", key)));
                return null;
            }

            return value;
        }

        private static float? OptionalFloatRange(Section section, string key, float min, float max, List<VehicleTsvIssue> issues)
        {
            var value = OptionalFloat(section, key, issues);
            if (!value.HasValue)
                return null;

            if (value.Value < min || value.Value > max)
            {
                issues.Add(new VehicleTsvIssue(
                    VehicleTsvIssueSeverity.Error,
                    section.Entries[key].Line,
                    Localized(
                        "Key '{0}' value {1} is outside allowed range {2} to {3}.",
                        key,
                        value.Value.ToString(CultureInfo.InvariantCulture),
                        min.ToString(CultureInfo.InvariantCulture),
                        max.ToString(CultureInfo.InvariantCulture))));
            }

            return value;
        }
    }
}

