using System.Collections.Generic;

namespace TopSpeed.Vehicles.Parsing
{
    public static partial class VehicleTsvParser
    {
        private static bool TryBuild(string fullPath, Dictionary<string, Section> sections, List<VehicleTsvIssue> issues, out CustomVehicleTsvData data)
        {
            data = null!;

            var parsedSections = ParseSections(sections);
            var parsedValues = ParseValues(parsedSections, issues);

            ValidateResolvedValues(parsedSections, parsedValues, issues);

            if (!TryBuildTorqueCurve(
                    parsedSections.TorqueCurve,
                    parsedValues.IdleRpm,
                    parsedValues.RevLimiter,
                    parsedValues.PeakTorqueRpm,
                    parsedValues.IdleTorque,
                    parsedValues.PeakTorque,
                    parsedValues.RedlineTorque,
                    issues,
                    out var torqueCurvePreset,
                    out var torqueCurveRpm,
                    out var torqueCurveTorqueNm))
            {
                return false;
            }

            TryResolveTireCircumference(
                parsedSections.Tires,
                parsedValues.TireCircumference,
                parsedValues.TireWidth,
                parsedValues.TireAspect,
                parsedValues.TireRim,
                issues,
                out var tireCircumferenceResolved);

            if (!ValidatePolicy(parsedSections.Policy, parsedValues.GearCount, parsedValues.IdleRpm, parsedValues.RevLimiter, issues))
                return false;

            if (HasErrors(issues))
                return false;

            var transmissionPolicy = BuildTransmissionPolicy(
                parsedSections.Policy,
                parsedValues.GearCount,
                parsedValues.IdleRpm,
                parsedValues.RevLimiter,
                parsedValues.AutoShiftRpm);

            data = BuildParsedData(
                fullPath,
                parsedValues,
                torqueCurvePreset,
                torqueCurveRpm,
                torqueCurveTorqueNm,
                tireCircumferenceResolved,
                transmissionPolicy);

            return true;
        }
    }
}

