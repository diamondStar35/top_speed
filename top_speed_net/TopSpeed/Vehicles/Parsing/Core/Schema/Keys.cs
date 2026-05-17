using System;
using System.Collections.Generic;

namespace TopSpeed.Vehicles.Parsing
{
    internal static partial class VehicleTsvParser
    {
        private static readonly Dictionary<string, HashSet<string>> s_allowedKeys = BuildAllowedKeys();

        private static Dictionary<string, HashSet<string>> BuildAllowedKeys()
        {
            return new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase)
            {
                ["meta"] = Set("name", "version", "description"),
                ["sounds"] = Set("engine", "start", "stop", "horn", "throttle", "crash", "brake", "backfire", "idle_freq", "top_freq", "shift_freq", "pitch_curve_exponent"),
                ["general"] = Set("surface_traction_factor", "max_speed", "has_wipers"),
                ["engine"] = Set(
                    "idle_rpm", "max_rpm", "rev_limiter", "auto_shift_rpm", "engine_braking", "mass_kg", "drivetrain_efficiency",
                    "launch_rpm", "fuel", "engine_displacement_l"),
                ["torque"] = Set(
                    "engine_braking_torque", "peak_torque", "peak_torque_rpm", "idle_torque", "redline_torque",
                    "power_factor"),
                ["engine_rot"] = Set(
                    "inertia_kgm2",
                    "coupling_rate",
                    "friction_base_nm",
                    "friction_linear_nm_per_krpm",
                    "friction_quadratic_nm_per_krpm2",
                    "idle_control_window_rpm",
                    "idle_control_gain_nm_per_rpm",
                    "min_coupled_rise_idle_rpm_per_s",
                    "min_coupled_rise_full_rpm_per_s",
                    "overrun_idle_fraction",
                    "overrun_curve_exponent",
                    "brake_transfer_efficiency"),
                ["resistance"] = Set(
                    "drag_coefficient",
                    "frontal_area",
                    "side_area",
                    "rolling_resistance",
                    "wheel_side_drag_n",
                    "wheel_side_drag_linear_n_per_mps",
                    "rolling_speed_factor",
                    "driveline_drag_nm",
                    "driveline_viscous_drag_nm_per_krpm"),
                ["torque_curve"] = Set("preset"),
                ["transmission"] = Set(
                    "primary_type",
                    "supported_types",
                    "shift_on_demand"),
                ["transmission_atc"] = Set(
                    "creep_accel_kphps",
                    "launch_coupling_min",
                    "launch_coupling_max",
                    "lock_speed_kph",
                    "lock_throttle_min",
                    "shift_release_coupling",
                    "engage_rate",
                    "disengage_rate"),
                ["transmission_dct"] = Set(
                    "launch_coupling_min",
                    "launch_coupling_max",
                    "lock_speed_kph",
                    "lock_throttle_min",
                    "shift_overlap_coupling",
                    "engage_rate",
                    "disengage_rate"),
                ["transmission_cvt"] = Set(
                    "ratio_min",
                    "ratio_max",
                    "target_rpm_low",
                    "target_rpm_high",
                    "ratio_change_rate",
                    "launch_coupling_min",
                    "launch_coupling_max",
                    "lock_speed_kph",
                    "lock_throttle_min",
                    "creep_accel_kphps",
                    "shift_hold_coupling",
                    "engage_rate",
                    "disengage_rate"),
                ["drivetrain"] = Set("final_drive", "reverse_max_speed", "reverse_power_factor", "reverse_gear_ratio", "brake_strength"),
                ["gears"] = Set("number_of_gears", "gear_ratios"),
                ["steering"] = Set("steering_response", "wheelbase", "max_steer_deg", "high_speed_stability", "high_speed_steer_gain", "high_speed_steer_start_kph", "high_speed_steer_full_kph"),
                ["tire_model"] = Set("tire_grip", "lateral_grip", "combined_grip_penalty", "slip_angle_peak_deg", "slip_angle_falloff", "turn_response", "mass_sensitivity", "downforce_grip_gain"),
                ["dynamics"] = Set("corner_stiffness_front", "corner_stiffness_rear", "yaw_inertia_scale", "steering_curve", "transient_damping"),
                ["dimensions"] = Set("vehicle_width", "vehicle_length"),
                ["tires"] = Set("tire_circumference", "tire_width", "tire_aspect", "tire_rim"),
                ["policy"] = Set(
                    "top_speed_gear",
                    "allow_overdrive_above_game_top_speed",
                    "base_auto_shift_cooldown",
                    "upshift_delay_default",
                    "auto_upshift_rpm_fraction",
                    "auto_upshift_rpm",
                    "auto_downshift_rpm_fraction",
                    "auto_downshift_rpm",
                    "top_speed_pursuit_speed_fraction",
                    "upshift_hysteresis",
                    "min_upshift_net_accel_mps2",
                    "prefer_intended_top_speed_gear_near_limit")
            };
        }

        private static HashSet<string> Set(params string[] values) => new HashSet<string>(values, StringComparer.OrdinalIgnoreCase);

        private static bool IsAllowedKey(string section, string key)
        {
            if (s_allowedKeys.TryGetValue(section, out var keys) && keys.Contains(key))
                return true;

            if (!string.Equals(section, "policy", StringComparison.OrdinalIgnoreCase))
            {
                if (!string.Equals(section, "torque_curve", StringComparison.OrdinalIgnoreCase))
                    return false;

                return key.EndsWith("rpm", StringComparison.OrdinalIgnoreCase);
            }

            if (key.StartsWith("upshift_delay_", StringComparison.OrdinalIgnoreCase))
                return true;

            return false;
        }
    }
}

