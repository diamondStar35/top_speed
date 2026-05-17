using System.Collections.Generic;

namespace TopSpeed.Vehicles.Parsing
{
    internal static partial class VehicleTsvParser
    {
        private static void ParseEngineValues(Section section, ParsedValues values, List<VehicleTsvIssue> issues)
        {
            values.IdleRpm = RequireFloatRange(section, "idle_rpm", 300f, 3000f, issues);
            values.MaxRpm = RequireFloatRange(section, "max_rpm", 1000f, 20000f, issues);
            values.RevLimiter = RequireFloatRange(section, "rev_limiter", 800f, 18000f, issues);
            values.AutoShiftRpm = RequireFloatRange(section, "auto_shift_rpm", 0f, 18000f, issues);
            values.EngineBraking = RequireFloatRange(section, "engine_braking", 0f, 1.5f, issues);
            values.FuelTankCapacityLiters = OptionalFloat(section, "fuel", issues)
                ?? VehicleDefinition.FuelTankCapacityDefaultLiters;
            values.EngineDisplacementLiters = OptionalFloat(section, "engine_displacement_l", issues)
                ?? VehicleDefinition.EngineDisplacementDefaultLiters;
            if (values.FuelTankCapacityLiters < TopSpeed.Physics.Fuel.FuelDefaults.MinTankCapacityLiters
                || values.FuelTankCapacityLiters > TopSpeed.Physics.Fuel.FuelDefaults.MaxTankCapacityLiters)
            {
                var entry = section.Entries.TryGetValue("fuel", out var fuelEntry)
                    ? fuelEntry
                    : section.Entries["engine_braking"];
                issues.Add(new VehicleTsvIssue(
                    VehicleTsvIssueSeverity.Error,
                    entry.Line,
                    Localized(
                        "fuel must be between {0} and {1} liters.",
                        TopSpeed.Physics.Fuel.FuelDefaults.MinTankCapacityLiters,
                        TopSpeed.Physics.Fuel.FuelDefaults.MaxTankCapacityLiters)));
            }

            if (values.EngineDisplacementLiters < TopSpeed.Physics.Fuel.FuelDefaults.MinEngineDisplacementLiters
                || values.EngineDisplacementLiters > TopSpeed.Physics.Fuel.FuelDefaults.MaxEngineDisplacementLiters)
            {
                var entry = section.Entries.TryGetValue("engine_displacement_l", out var displacementEntry)
                    ? displacementEntry
                    : section.Entries["engine_braking"];
                issues.Add(new VehicleTsvIssue(
                    VehicleTsvIssueSeverity.Error,
                    entry.Line,
                    Localized(
                        "engine_displacement_l must be between {0} and {1} liters.",
                        TopSpeed.Physics.Fuel.FuelDefaults.MinEngineDisplacementLiters,
                        TopSpeed.Physics.Fuel.FuelDefaults.MaxEngineDisplacementLiters)));
            }

            values.MassKg = RequireFloatRange(section, "mass_kg", 20f, 10000f, issues);
            values.DrivetrainEfficiency = RequireFloatRange(section, "drivetrain_efficiency", 0.1f, 1.0f, issues);
            values.LaunchRpm = RequireFloatRange(section, "launch_rpm", 0f, 18000f, issues);
        }

        private static void ParseTorqueValues(Section section, ParsedValues values, List<VehicleTsvIssue> issues)
        {
            values.EngineBrakingTorque = RequireFloatRange(section, "engine_braking_torque", 0f, 3000f, issues);
            values.PeakTorque = RequireFloatRange(section, "peak_torque", 10f, 3000f, issues);
            values.PeakTorqueRpm = RequireFloatRange(section, "peak_torque_rpm", 500f, 18000f, issues);
            values.IdleTorque = RequireFloatRange(section, "idle_torque", 0f, 3000f, issues);
            values.RedlineTorque = RequireFloatRange(section, "redline_torque", 0f, 3000f, issues);
            values.PowerFactor = RequireFloatRange(section, "power_factor", 0.05f, 2f, issues);
        }

        private static void ParseEngineRotValues(Section section, ParsedValues values, List<VehicleTsvIssue> issues)
        {
            values.EngineInertiaKgm2 = RequireFloatRange(section, "inertia_kgm2", 0.01f, 5f, issues);
            values.DrivelineCouplingRate = RequireFloatRange(section, "coupling_rate", 0.1f, 80f, issues);
            values.EngineFrictionBaseNm = RequireFloatRange(section, "friction_base_nm", 0f, 1000f, issues);
            values.EngineFrictionLinearNmPerKrpm = RequireFloatRange(section, "friction_linear_nm_per_krpm", 0f, 1000f, issues);
            values.EngineFrictionQuadraticNmPerKrpm2 = RequireFloatRange(section, "friction_quadratic_nm_per_krpm2", 0f, 1000f, issues);
            values.IdleControlWindowRpm = RequireFloatRange(section, "idle_control_window_rpm", 0f, 1000f, issues);
            values.IdleControlGainNmPerRpm = RequireFloatRange(section, "idle_control_gain_nm_per_rpm", 0f, 2f, issues);
            values.MinCoupledRiseIdleRpmPerSecond = RequireFloatRange(section, "min_coupled_rise_idle_rpm_per_s", 0f, 20000f, issues);
            values.MinCoupledRiseFullRpmPerSecond = RequireFloatRange(section, "min_coupled_rise_full_rpm_per_s", 0f, 20000f, issues);
            values.EngineOverrunIdleLossFraction = RequireFloatRange(section, "overrun_idle_fraction", 0f, 1f, issues);
            values.OverrunCurveExponent = RequireFloatRange(section, "overrun_curve_exponent", 0.2f, 5f, issues);
            values.EngineBrakeTransferEfficiency = RequireFloatRange(section, "brake_transfer_efficiency", 0.1f, 1f, issues);

            if (values.MinCoupledRiseFullRpmPerSecond < values.MinCoupledRiseIdleRpmPerSecond)
            {
                issues.Add(new VehicleTsvIssue(
                    VehicleTsvIssueSeverity.Error,
                    section.Line,
                    Localized("min_coupled_rise_full_rpm_per_s must be greater than or equal to min_coupled_rise_idle_rpm_per_s.")));
            }
        }

        private static void ParseResistanceValues(Section section, ParsedValues values, List<VehicleTsvIssue> issues)
        {
            values.DragCoefficient = RequireFloatRange(section, "drag_coefficient", 0.01f, 1.5f, issues);
            values.FrontalArea = RequireFloatRange(section, "frontal_area", 0.05f, 10f, issues);
            values.SideArea = RequireFloatRange(section, "side_area", 0.05f, 20f, issues);
            values.RollingResistance = RequireFloatRange(section, "rolling_resistance", 0.001f, 0.1f, issues);
            values.WheelSideDragBaseN = RequireFloatRange(section, "wheel_side_drag_n", 0f, 5000f, issues);
            values.WheelSideDragLinearNPerMps = RequireFloatRange(section, "wheel_side_drag_linear_n_per_mps", 0f, 200f, issues);
            values.RollingResistanceSpeedFactor = RequireFloatRange(section, "rolling_speed_factor", 0f, 1f, issues);
            values.CoupledDrivelineDragNm = RequireFloatRange(section, "driveline_drag_nm", 0f, 2000f, issues);
            values.CoupledDrivelineViscousDragNmPerKrpm = RequireFloatRange(section, "driveline_viscous_drag_nm_per_krpm", 0f, 500f, issues);
        }

        private static void ParseDrivetrainValues(Section section, ParsedValues values, List<VehicleTsvIssue> issues)
        {
            values.FinalDrive = RequireFloatRange(section, "final_drive", 0.5f, 8f, issues);
            values.ReverseMaxSpeed = RequireFloatRange(section, "reverse_max_speed", 1f, 100f, issues);
            values.ReversePowerFactor = RequireFloatRange(section, "reverse_power_factor", 0.05f, 2f, issues);
            values.ReverseGearRatio = RequireFloatRange(section, "reverse_gear_ratio", 0.5f, 8f, issues);
            values.BrakeStrength = RequireFloatRange(section, "brake_strength", 0.1f, 5f, issues);
        }
    }
}

