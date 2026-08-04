using System.Collections.Generic;

namespace TopSpeed.Vehicles.Parsing
{
    public static partial class VehicleTsvParser
    {
        private static void TryResolveTireCircumference(
            Section tires,
            float? tireCircumference,
            int? tireWidth,
            int? tireAspect,
            int? tireRim,
            List<VehicleTsvIssue> issues,
            out float tireCircumferenceResolved)
        {
            tireCircumferenceResolved = 0f;
            if (tireCircumference.HasValue && tireCircumference.Value > 0f)
            {
                if (tireCircumference.Value < 0.2f || tireCircumference.Value > 5f)
                    issues.Add(new VehicleTsvIssue(VehicleTsvIssueSeverity.Error, tires.Entries["tire_circumference"].Line, Localized("tire_circumference must be between 0.2 and 5.0 meters.")));
                else
                    tireCircumferenceResolved = tireCircumference.Value;
                return;
            }

            if (!tireWidth.HasValue || !tireAspect.HasValue || !tireRim.HasValue)
            {
                issues.Add(new VehicleTsvIssue(VehicleTsvIssueSeverity.Error, tires.Line, Localized("Provide tire_circumference or all of tire_width, tire_aspect, and tire_rim.")));
                return;
            }

            if (tireWidth.Value < 20 || tireWidth.Value > 450)
                issues.Add(new VehicleTsvIssue(VehicleTsvIssueSeverity.Error, tires.Entries["tire_width"].Line, Localized("tire_width must be between 20 and 450 mm.")));
            if (tireAspect.Value < 5 || tireAspect.Value > 150)
                issues.Add(new VehicleTsvIssue(VehicleTsvIssueSeverity.Error, tires.Entries["tire_aspect"].Line, Localized("tire_aspect must be between 5 and 150.")));
            if (tireRim.Value < 4 || tireRim.Value > 30)
                issues.Add(new VehicleTsvIssue(VehicleTsvIssueSeverity.Error, tires.Entries["tire_rim"].Line, Localized("tire_rim must be between 4 and 30 inches.")));
            if (!HasErrors(issues))
                tireCircumferenceResolved = CalculateTireCircumferenceM(tireWidth.Value, tireAspect.Value, tireRim.Value);
        }
    }
}

