using System.Collections.Generic;

namespace TopSpeed.Vehicles.Parsing
{
    public static partial class VehicleTsvParser
    {
        private static List<float>? RequireFloatCsv(Section section, string key, List<VehicleTsvIssue> issues)
        {
            if (!section.Entries.TryGetValue(key, out var entry))
            {
                issues.Add(new VehicleTsvIssue(VehicleTsvIssueSeverity.Error, section.Line, Localized("Missing required key '{0}' in section [{1}].", key, section.Name)));
                return null;
            }

            var values = new List<float>();
            var tokens = entry.Value.Split(',');
            for (var i = 0; i < tokens.Length; i++)
            {
                var token = tokens[i].Trim();
                if (token.Length == 0)
                    continue;
                if (!TryParseFloat(token, out var parsed))
                {
                    issues.Add(new VehicleTsvIssue(VehicleTsvIssueSeverity.Error, entry.Line, Localized("Key '{0}' contains a non-float value '{1}'.", key, token)));
                    return null;
                }

                values.Add(parsed);
            }

            if (values.Count == 0)
            {
                issues.Add(new VehicleTsvIssue(VehicleTsvIssueSeverity.Error, entry.Line, Localized("Key '{0}' must contain at least one float value.", key)));
                return null;
            }

            return values;
        }
    }
}

