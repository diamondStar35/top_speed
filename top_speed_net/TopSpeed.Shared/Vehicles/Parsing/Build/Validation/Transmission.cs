using System.Collections.Generic;

namespace TopSpeed.Vehicles.Parsing
{
    public static partial class VehicleTsvParser
    {
        private static void ValidateShiftOnDemandWarnings(
            Section transmission,
            IReadOnlyList<TransmissionType> supportedTransmissionTypes,
            bool shiftOnDemand,
            List<VehicleTsvIssue> issues)
        {
            if (!shiftOnDemand)
                return;

            var hasAutomaticFamily = false;
            for (var i = 0; i < supportedTransmissionTypes.Count; i++)
            {
                if (!TransmissionTypes.IsAutomaticFamily(supportedTransmissionTypes[i]))
                    continue;

                hasAutomaticFamily = true;
                break;
            }

            if (hasAutomaticFamily)
                return;

            var line = transmission.Line;
            if (transmission.Entries.TryGetValue("shift_on_demand", out var shiftOnDemandEntry))
                line = shiftOnDemandEntry.Line;
            issues.Add(new VehicleTsvIssue(
                VehicleTsvIssueSeverity.Warning,
                line,
                Localized("shift_on_demand is ignored because supported_types does not include an automatic transmission type.")));
        }
    }
}

