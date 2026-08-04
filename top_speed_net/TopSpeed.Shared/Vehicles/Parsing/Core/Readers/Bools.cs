using System.Collections.Generic;
using System.Globalization;

namespace TopSpeed.Vehicles.Parsing
{
    public static partial class VehicleTsvParser
    {
        private static bool RequireBoolInt(Section section, string key, List<VehicleTsvIssue> issues)
        {
            if (!section.Entries.TryGetValue(key, out var entry))
            {
                issues.Add(new VehicleTsvIssue(VehicleTsvIssueSeverity.Error, section.Line, Localized("Missing required key '{0}' in section [{1}].", key, section.Name)));
                return false;
            }

            if (TryParseBool(entry.Value, out var boolValue))
                return boolValue;
            if (int.TryParse(entry.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var intValue))
                return intValue != 0;

            issues.Add(new VehicleTsvIssue(VehicleTsvIssueSeverity.Error, entry.Line, Localized("Key '{0}' must be a boolean or 0/1 integer.", key)));
            return false;
        }

        private static bool? OptionalBoolInt(Section section, string key, List<VehicleTsvIssue> issues)
        {
            if (!section.Entries.TryGetValue(key, out var entry))
                return null;

            if (TryParseBool(entry.Value, out var boolValue))
                return boolValue;
            if (int.TryParse(entry.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var intValue))
                return intValue != 0;

            issues.Add(new VehicleTsvIssue(VehicleTsvIssueSeverity.Error, entry.Line, Localized("Key '{0}' must be a boolean or 0/1 integer.", key)));
            return null;
        }
    }
}

