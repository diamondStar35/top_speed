using System.Collections.Generic;

namespace TopSpeed.Vehicles.Parsing
{
    public static partial class VehicleTsvParser
    {
        private static void AddTransmissionIssue(
            List<VehicleTsvIssue> issues,
            Section transmission,
            string? key,
            string message)
        {
            var line = transmission.Line;
            if (!string.IsNullOrWhiteSpace(key))
            {
                var lookupKey = key!;
                if (transmission.Entries.TryGetValue(lookupKey, out var entry))
                    line = entry.Line;
            }

            issues.Add(new VehicleTsvIssue(VehicleTsvIssueSeverity.Error, line, message));
        }

        private static void AddTransmissionWarning(
            List<VehicleTsvIssue> issues,
            Section transmission,
            string message)
        {
            issues.Add(new VehicleTsvIssue(VehicleTsvIssueSeverity.Warning, transmission.Line, message));
        }

        private static bool SupportsTransmissionType(IReadOnlyList<TransmissionType> values, TransmissionType expected)
        {
            for (var i = 0; i < values.Count; i++)
            {
                if (values[i] == expected)
                    return true;
            }

            return false;
        }
    }
}

