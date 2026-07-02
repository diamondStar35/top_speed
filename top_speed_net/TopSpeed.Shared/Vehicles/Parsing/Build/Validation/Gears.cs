using System.Collections.Generic;

namespace TopSpeed.Vehicles.Parsing
{
    public static partial class VehicleTsvParser
    {
        private static void ValidateGearRatios(
            Section gears,
            int gearCount,
            List<float>? gearRatios,
            List<VehicleTsvIssue> issues)
        {
            if (gearRatios == null)
                return;

            if (gearRatios.Count != gearCount)
            {
                issues.Add(new VehicleTsvIssue(
                    VehicleTsvIssueSeverity.Error,
                    gears.Entries["gear_ratios"].Line,
                    Localized("gear_ratios count ({0}) must match number_of_gears ({1}).", gearRatios.Count, gearCount)));
                return;
            }

            for (var i = 0; i < gearRatios.Count; i++)
            {
                var value = gearRatios[i];
                if (value < 0.20f || value > 8.00f)
                {
                    issues.Add(new VehicleTsvIssue(
                        VehicleTsvIssueSeverity.Error,
                        gears.Entries["gear_ratios"].Line,
                        Localized("gear_ratios[{0}] is outside allowed range 0.20 to 8.00.", i + 1)));
                }
            }

            for (var i = 1; i < gearRatios.Count; i++)
            {
                if (gearRatios[i] > gearRatios[i - 1] + 0.0001f)
                {
                    issues.Add(new VehicleTsvIssue(
                        VehicleTsvIssueSeverity.Error,
                        gears.Entries["gear_ratios"].Line,
                        Localized("gear_ratios must be non-increasing from gear 1 to last gear.")));
                    break;
                }
            }
        }
    }
}

