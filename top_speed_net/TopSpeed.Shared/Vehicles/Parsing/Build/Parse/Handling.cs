using System.Collections.Generic;

namespace TopSpeed.Vehicles.Parsing
{
    public static partial class VehicleTsvParser
    {
        private static void ParseSteeringValues(Section section, ParsedValues values, List<VehicleTsvIssue> issues)
        {
            values.SteeringResponse = RequireFloatRange(section, "steering_response", 0.1f, 5f, issues);
            values.Wheelbase = RequireFloatRange(section, "wheelbase", 0.3f, 8f, issues);
            values.MaxSteerDeg = RequireFloatRange(section, "max_steer_deg", 5f, 60f, issues);
            values.HighSpeedStability = RequireFloatRange(section, "high_speed_stability", 0f, 1f, issues);
            values.HighSpeedSteerGain = RequireFloatRange(section, "high_speed_steer_gain", 0.7f, 1.6f, issues);
            values.HighSpeedSteerStartKph = RequireFloatRange(section, "high_speed_steer_start_kph", 60f, 260f, issues);
            values.HighSpeedSteerFullKph = RequireFloatRange(section, "high_speed_steer_full_kph", 100f, 350f, issues);
        }

        private static void ParseTireModelValues(Section section, ParsedValues values, List<VehicleTsvIssue> issues)
        {
            values.TireGrip = RequireFloatRange(section, "tire_grip", 0.1f, 3f, issues);
            values.LateralGrip = RequireFloatRange(section, "lateral_grip", 0.1f, 3f, issues);
            values.CombinedGripPenalty = RequireFloatRange(section, "combined_grip_penalty", 0f, 1f, issues);
            values.SlipAnglePeakDeg = RequireFloatRange(section, "slip_angle_peak_deg", 0.5f, 20f, issues);
            values.SlipAngleFalloff = RequireFloatRange(section, "slip_angle_falloff", 0.01f, 5f, issues);
            values.TurnResponse = RequireFloatRange(section, "turn_response", 0.2f, 2.5f, issues);
            values.MassSensitivity = RequireFloatRange(section, "mass_sensitivity", 0f, 1f, issues);
            values.DownforceGripGain = RequireFloatRange(section, "downforce_grip_gain", 0f, 1f, issues);
        }

        private static void ParseDynamicsValues(Section section, ParsedValues values, List<VehicleTsvIssue> issues)
        {
            values.CornerStiffnessFront = RequireFloatRange(section, "corner_stiffness_front", 0.2f, 3f, issues);
            values.CornerStiffnessRear = RequireFloatRange(section, "corner_stiffness_rear", 0.2f, 3f, issues);
            values.YawInertiaScale = RequireFloatRange(section, "yaw_inertia_scale", 0.5f, 2f, issues);
            values.SteeringCurve = RequireFloatRange(section, "steering_curve", 0.5f, 2f, issues);
            values.TransientDamping = RequireFloatRange(section, "transient_damping", 0f, 6f, issues);
        }

        private static void ParseDimensionValues(Section section, ParsedValues values, List<VehicleTsvIssue> issues)
        {
            values.WidthM = RequireFloatRange(section, "vehicle_width", 0.2f, 5f, issues);
            values.LengthM = RequireFloatRange(section, "vehicle_length", 0.3f, 20f, issues);
        }

        private static void ParseTireInputValues(Section section, ParsedValues values, List<VehicleTsvIssue> issues)
        {
            values.TireCircumference = OptionalFloat(section, "tire_circumference", issues);
            values.TireWidth = OptionalInt(section, "tire_width", issues);
            values.TireAspect = OptionalInt(section, "tire_aspect", issues);
            values.TireRim = OptionalInt(section, "tire_rim", issues);
        }
    }
}

