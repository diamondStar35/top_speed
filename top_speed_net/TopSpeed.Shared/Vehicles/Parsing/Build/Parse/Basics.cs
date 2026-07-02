using System.Collections.Generic;

namespace TopSpeed.Vehicles.Parsing
{
    public static partial class VehicleTsvParser
    {
        private static void ParseMetaValues(Section section, ParsedValues values, List<VehicleTsvIssue> issues)
        {
            values.MetaName = RequireString(section, "name", issues);
            values.MetaVersion = RequireString(section, "version", issues);
            values.MetaDescription = RequireString(section, "description", issues);
        }

        private static void ParseSoundValues(Section section, ParsedValues values, List<VehicleTsvIssue> issues)
        {
            values.EngineSound = RequireString(section, "engine", issues);
            values.StartSound = RequireString(section, "start", issues);
            values.StopSound = OptionalString(section, "stop");
            values.HornSound = RequireString(section, "horn", issues);
            values.ThrottleSound = OptionalString(section, "throttle");
            values.CrashVariants = RequireCsvStrings(section, "crash", issues);
            values.BrakeSound = RequireString(section, "brake", issues);
            values.BackfireVariants = OptionalCsvStrings(section, "backfire");

            values.IdleFreq = RequireIntRange(section, "idle_freq", 100, 200000, issues);
            values.TopFreq = RequireIntRange(section, "top_freq", 100, 200000, issues);
            values.ShiftFreq = RequireIntRange(section, "shift_freq", 100, 200000, issues);
            values.PitchCurveExponent = OptionalFloat(section, "pitch_curve_exponent", issues)
                ?? TopSpeed.Vehicles.VehicleDefinition.PitchCurveExponentDefault;
        }

        private static void ParseGeneralValues(Section section, ParsedValues values, List<VehicleTsvIssue> issues)
        {
            values.SurfaceTractionFactor = RequireFloatRange(section, "surface_traction_factor", 0f, 5f, issues);
            values.TopSpeed = RequireFloatRange(section, "max_speed", 10f, 500f, issues);
            values.HasWipers = RequireBoolInt(section, "has_wipers", issues);
        }

        private static void ParseGearValues(Section section, ParsedValues values, List<VehicleTsvIssue> issues)
        {
            values.GearCount = RequireIntRange(section, "number_of_gears", 1, 10, issues);
            values.GearRatios = RequireFloatCsv(section, "gear_ratios", issues);
        }
    }
}

