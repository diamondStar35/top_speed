using System;
using System.Collections.Generic;

namespace TopSpeed.Vehicles.Parsing
{
    public static partial class VehicleTsvParser
    {
        private static string RequireString(Section section, string key, List<VehicleTsvIssue> issues)
        {
            if (!section.Entries.TryGetValue(key, out var entry))
            {
                issues.Add(new VehicleTsvIssue(VehicleTsvIssueSeverity.Error, section.Line, Localized("Missing required key '{0}' in section [{1}].", key, section.Name)));
                return string.Empty;
            }

            var value = entry.Value.Trim();
            if (value.Length == 0)
                issues.Add(new VehicleTsvIssue(VehicleTsvIssueSeverity.Error, entry.Line, Localized("Key '{0}' in section [{1}] must not be empty.", key, section.Name)));
            return value;
        }

        private static string? OptionalString(Section section, string key)
        {
            if (!section.Entries.TryGetValue(key, out var entry))
                return null;
            var value = entry.Value.Trim();
            return value.Length == 0 ? null : value;
        }

        private static IReadOnlyList<string> RequireCsvStrings(Section section, string key, List<VehicleTsvIssue> issues)
        {
            if (!section.Entries.TryGetValue(key, out var entry))
            {
                issues.Add(new VehicleTsvIssue(VehicleTsvIssueSeverity.Error, section.Line, Localized("Missing required key '{0}' in section [{1}].", key, section.Name)));
                return Array.Empty<string>();
            }

            var values = ParseCsvStrings(entry.Value);
            if (values.Count == 0)
                issues.Add(new VehicleTsvIssue(VehicleTsvIssueSeverity.Error, entry.Line, Localized("Key '{0}' must contain at least one path.", key)));
            return values;
        }

        private static IReadOnlyList<string> OptionalCsvStrings(Section section, string key)
        {
            if (!section.Entries.TryGetValue(key, out var entry))
                return Array.Empty<string>();
            return ParseCsvStrings(entry.Value);
        }
    }
}

