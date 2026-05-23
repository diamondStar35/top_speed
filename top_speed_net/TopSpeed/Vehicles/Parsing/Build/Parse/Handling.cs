using System.Collections.Generic;

namespace TopSpeed.Vehicles.Parsing
{
    internal static partial class VehicleTsvParser
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
            values.TireWearBasePerKilometer = OptionalFloatRange(section, "wear_base_per_km", 0.0005f, 0.05f, issues);
            values.TireWearSlipRatePerSecond = OptionalFloatRange(section, "wear_slip_rate_per_s", 0f, 0.01f, issues);
            values.TireWearCorneringSlipWeight = OptionalFloatRange(section, "wear_cornering_slip_weight", 0f, 3f, issues);
            values.TireWearLongitudinalSlipWeight = OptionalFloatRange(section, "wear_longitudinal_slip_weight", 0f, 3f, issues);
            values.TireWearLoadGain = OptionalFloatRange(section, "wear_load_gain", 0f, 5f, issues);
            values.TireWearHotStartTemperatureC = OptionalFloatRange(section, "wear_hot_start_c", 50f, 200f, issues);
            values.TireWearHotGainPerC = OptionalFloatRange(section, "wear_hot_gain_per_c", 0f, 0.2f, issues);
            values.TireWearColdStartTemperatureC = OptionalFloatRange(section, "wear_cold_start_c", -40f, 80f, issues);
            values.TireWearColdGainPerC = OptionalFloatRange(section, "wear_cold_gain_per_c", 0f, 0.2f, issues);
            values.TireTemperatureColdEndC = OptionalFloatRange(section, "temp_cold_end_c", -20f, 100f, issues);
            values.TireTemperatureOptimalStartC = OptionalFloatRange(section, "temp_optimal_start_c", 20f, 180f, issues);
            values.TireTemperatureOptimalEndC = OptionalFloatRange(section, "temp_optimal_end_c", 30f, 220f, issues);
            values.TireTemperatureOverheatEndC = OptionalFloatRange(section, "temp_overheat_end_c", 40f, 260f, issues);
            values.TireGripVeryCold = OptionalFloatRange(section, "grip_very_cold", 0.35f, 1.2f, issues);
            values.TireGripColdEnd = OptionalFloatRange(section, "grip_cold_end", 0.35f, 1.2f, issues);
            values.TireGripOptimal = OptionalFloatRange(section, "grip_optimal", 0.35f, 1.2f, issues);
            values.TireGripOverheatEnd = OptionalFloatRange(section, "grip_overheat_end", 0.35f, 1.2f, issues);
            values.TireGripCooked = OptionalFloatRange(section, "grip_cooked", 0.35f, 1.2f, issues);
            values.TireWearGripAtFullWear = OptionalFloatRange(section, "wear_grip_at_full_wear", 0.35f, 1f, issues);
            values.TireHeatCorneringCPerSecond = OptionalFloatRange(section, "heat_cornering_c_per_s", 0f, 80f, issues);
            values.TireHeatLongitudinalCPerSecond = OptionalFloatRange(section, "heat_longitudinal_c_per_s", 0f, 80f, issues);
            values.TireHeatLoadCPerSecond = OptionalFloatRange(section, "heat_load_c_per_s", 0f, 80f, issues);
            values.TireHeatRollingCPerSecond = OptionalFloatRange(section, "heat_rolling_c_per_s", 0f, 80f, issues);
            values.TireCoolingAirflowPerMpsPerCPerSecond = OptionalFloatRange(section, "cool_airflow_per_mps_per_c_per_s", 0f, 0.1f, issues);
            values.TireExchangeAmbientPerCPerSecond = OptionalFloatRange(section, "exchange_ambient_per_c_per_s", 0f, 0.3f, issues);
            values.TireExchangeRoadPerCPerSecond = OptionalFloatRange(section, "exchange_road_per_c_per_s", 0f, 0.3f, issues);
            values.TireExchangeWetRoadPerCPerSecond = OptionalFloatRange(section, "exchange_wet_road_per_c_per_s", 0f, 0.3f, issues);
            values.TireSurfaceToTreadConductancePerSecond = OptionalFloatRange(section, "surface_to_tread_conductance_per_s", 0f, 1f, issues);
            values.TireTreadToCarcassConductancePerSecond = OptionalFloatRange(section, "tread_to_carcass_conductance_per_s", 0f, 1f, issues);
            values.TireTreadMassRatio = OptionalFloatRange(section, "tread_mass_ratio", 0.2f, 20f, issues);
            values.TireCarcassMassRatio = OptionalFloatRange(section, "carcass_mass_ratio", 0.2f, 20f, issues);
            values.TireSlipSmoothingTauSeconds = OptionalFloatRange(section, "slip_smoothing_tau_s", 0.05f, 20f, issues);
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

