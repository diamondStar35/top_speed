using System;
using System.IO;
using System.Linq;
using TopSpeed.Vehicles.Parsing;
using Xunit;

namespace TopSpeed.Tests
{
    [Trait("Category", "Behavior")]
    public sealed class VehicleParserBehaviorTests
    {
        [Fact]
        public void ShiftOnDemandWithoutAutomatic_ShouldAddWarning()
        {
            using var tempFile = TempVehicleFile.Create(BuildVehicleTsv(
                primaryType: "manual",
                supportedTypes: "manual",
                shiftOnDemand: true,
                includeAtcSection: false));

            var ok = VehicleTsvParser.TryLoadFromFile(tempFile.Path, out var _, out var issues);

            ok.Should().BeTrue(DescribeIssues(issues));
            issues.Where(x => x.Severity == VehicleTsvIssueSeverity.Warning)
                .Select(x => x.Message)
                .Should()
                .ContainSingle(x => x.Contains("shift_on_demand is ignored", StringComparison.OrdinalIgnoreCase));
            issues.Select(x => x.Severity).Should().NotContain(VehicleTsvIssueSeverity.Error);
        }

        [Fact]
        public void AdvancedResistanceKeys_ShouldBeParsed()
        {
            using var tempFile = TempVehicleFile.Create(BuildVehicleTsv(
                primaryType: "manual",
                supportedTypes: "manual",
                shiftOnDemand: false,
                includeAtcSection: false));

            var ok = VehicleTsvParser.TryLoadFromFile(tempFile.Path, out var data, out var issues);

            ok.Should().BeTrue(DescribeIssues(issues));
            issues.Select(x => x.Severity).Should().NotContain(VehicleTsvIssueSeverity.Error);
            data.SideAreaM2.Should().BeApproximately(3.9f, 0.001f);
            data.RollingResistanceSpeedFactor.Should().BeApproximately(0.014f, 0.001f);
            data.WheelSideDragBaseN.Should().BeApproximately(94f, 0.001f);
            data.WheelSideDragLinearNPerMps.Should().BeApproximately(3.6f, 0.001f);
            data.CoupledDrivelineDragNm.Should().BeApproximately(22f, 0.001f);
            data.CoupledDrivelineViscousDragNmPerKrpm.Should().BeApproximately(7.5f, 0.001f);
            data.EngineOverrunIdleLossFraction.Should().BeApproximately(0.25f, 0.001f);
            data.OverrunCurveExponent.Should().BeApproximately(1.35f, 0.001f);
            data.EngineBrakeTransferEfficiency.Should().BeApproximately(0.64f, 0.001f);
        }

        [Fact]
        public void StopSound_ShouldStayOptional()
        {
            using var withStop = TempVehicleFile.Create(BuildVehicleTsv(
                primaryType: "manual",
                supportedTypes: "manual",
                shiftOnDemand: false,
                includeAtcSection: false,
                stopSound: "stop.wav"));
            using var withoutStop = TempVehicleFile.Create(BuildVehicleTsv(
                primaryType: "manual",
                supportedTypes: "manual",
                shiftOnDemand: false,
                includeAtcSection: false));

            VehicleTsvParser.TryLoadFromFile(withStop.Path, out var withStopData, out var withStopIssues).Should().BeTrue(DescribeIssues(withStopIssues));
            VehicleTsvParser.TryLoadFromFile(withoutStop.Path, out var withoutStopData, out var withoutStopIssues).Should().BeTrue(DescribeIssues(withoutStopIssues));

            withStopData.Sounds.Stop.Should().Be("stop.wav");
            withoutStopData.Sounds.Stop.Should().BeNull();
        }

        [Fact]
        public void TireWearModelKeys_ShouldOverrideDerivedRuntimeConfig()
        {
            using var tempFile = TempVehicleFile.Create(BuildVehicleTsv(
                primaryType: "manual",
                supportedTypes: "manual",
                shiftOnDemand: false,
                includeAtcSection: false,
                tireWearOverrides: @"
wear_base_per_km=0.0085
wear_slip_rate_per_s=0.0009
wear_cornering_slip_weight=0.45
wear_longitudinal_slip_weight=0.8
wear_load_gain=1.3
wear_hot_start_c=96
wear_hot_gain_per_c=0.042
wear_cold_start_c=24
wear_cold_gain_per_c=0.011
temp_cold_end_c=38
temp_optimal_start_c=78
temp_optimal_end_c=104
temp_overheat_end_c=141
grip_very_cold=0.72
grip_cold_end=0.94
grip_optimal=1.03
grip_overheat_end=0.73
grip_cooked=0.62
wear_grip_at_full_wear=0.61
heat_cornering_c_per_s=14
heat_longitudinal_c_per_s=12
heat_load_c_per_s=7
heat_rolling_c_per_s=4
cool_airflow_per_mps_per_c_per_s=0.0031
exchange_ambient_per_c_per_s=0.031
exchange_road_per_c_per_s=0.051
exchange_wet_road_per_c_per_s=0.072
surface_to_tread_conductance_per_s=0.18
tread_to_carcass_conductance_per_s=0.045
tread_mass_ratio=1.1
carcass_mass_ratio=2.4
slip_smoothing_tau_s=1.2"));

            var ok = VehicleTsvParser.TryLoadFromFile(tempFile.Path, out var data, out var issues);

            ok.Should().BeTrue(DescribeIssues(issues));
            issues.Select(x => x.Severity).Should().NotContain(VehicleTsvIssueSeverity.Error);
            data.TireWearConfig.BaseWearPerKilometer.Should().BeApproximately(0.0085f, 0.00001f);
            data.TireWearConfig.SlipWearRatePerSecond.Should().BeApproximately(0.0009f, 0.0000001f);
            data.TireWearConfig.CorneringSlipWearWeight.Should().BeApproximately(0.45f, 0.0001f);
            data.TireWearConfig.LongitudinalSlipWearWeight.Should().BeApproximately(0.8f, 0.0001f);
            data.TireWearConfig.LoadWearGain.Should().BeApproximately(1.3f, 0.0001f);
            data.TireWearConfig.WearHotStartTemperatureC.Should().BeApproximately(96f, 0.0001f);
            data.TireWearConfig.WearHotGainPerC.Should().BeApproximately(0.042f, 0.0001f);
            data.TireWearConfig.ColdEndTemperatureC.Should().BeApproximately(38f, 0.0001f);
            data.TireWearConfig.OptimalStartTemperatureC.Should().BeApproximately(78f, 0.0001f);
            data.TireWearConfig.OptimalEndTemperatureC.Should().BeApproximately(104f, 0.0001f);
            data.TireWearConfig.OverheatEndTemperatureC.Should().BeApproximately(141f, 0.0001f);
            data.TireWearConfig.GripAtVeryCold.Should().BeApproximately(0.72f, 0.0001f);
            data.TireWearConfig.GripAtOverheatEnd.Should().BeApproximately(0.73f, 0.0001f);
            data.TireWearConfig.GripAtFullWear.Should().BeApproximately(0.61f, 0.0001f);
            data.TireWearConfig.CorneringHeatCPerSecond.Should().BeApproximately(14f, 0.0001f);
            data.TireWearConfig.LongitudinalHeatCPerSecond.Should().BeApproximately(12f, 0.0001f);
            data.TireWearConfig.LoadHeatCPerSecond.Should().BeApproximately(7f, 0.0001f);
            data.TireWearConfig.RollingHeatCPerSecond.Should().BeApproximately(4f, 0.0001f);
            data.TireWearConfig.AirflowCoolingPerMpsPerCPerSecond.Should().BeApproximately(0.0031f, 0.0000001f);
            data.TireWearConfig.AmbientExchangePerCPerSecond.Should().BeApproximately(0.031f, 0.0001f);
            data.TireWearConfig.RoadExchangePerCPerSecond.Should().BeApproximately(0.051f, 0.0001f);
            data.TireWearConfig.WetRoadExchangePerCPerSecond.Should().BeApproximately(0.072f, 0.0001f);
            data.TireWearConfig.SurfaceToTreadConductancePerSecond.Should().BeApproximately(0.18f, 0.0001f);
            data.TireWearConfig.TreadToCarcassConductancePerSecond.Should().BeApproximately(0.045f, 0.0001f);
            data.TireWearConfig.TreadMassRatio.Should().BeApproximately(1.1f, 0.0001f);
            data.TireWearConfig.CarcassMassRatio.Should().BeApproximately(2.4f, 0.0001f);
            data.TireWearConfig.SlipSmoothingTimeConstantSeconds.Should().BeApproximately(1.2f, 0.0001f);
        }

        private static string DescribeIssues(System.Collections.Generic.IReadOnlyList<VehicleTsvIssue> issues)
        {
            if (issues.Count == 0)
                return "the parser should accept the generated fixture";

            return string.Join("; ", issues.Select(x => $"{x.Severity}@{x.Line}:{x.Message}"));
        }

        [Fact]
        public void MissingTorqueCurveSection_ShouldFailWithHelpfulMessage()
        {
            using var tempFile = TempVehicleFile.Create(BuildVehicleTsv(
                primaryType: "manual",
                supportedTypes: "manual",
                shiftOnDemand: false,
                includeAtcSection: false,
                includeTorqueCurveSection: false));

            var ok = VehicleTsvParser.TryLoadFromFile(tempFile.Path, out var _, out var issues);

            ok.Should().BeFalse();
            issues.Select(x => x.Message)
                .Should()
                .Contain(x => x.Contains("Missing required section [torque_curve]", StringComparison.OrdinalIgnoreCase));
        }

        private static string BuildVehicleTsv(
            string primaryType,
            string supportedTypes,
            bool shiftOnDemand,
            bool includeAtcSection,
            bool includeTorqueCurveSection = true,
            string? stopSound = null,
            string? tireWearOverrides = null)
        {
            var atcSection = includeAtcSection
                ? @"
[transmission_atc]
creep_accel_kphps=0.7
launch_coupling_min=0.2
launch_coupling_max=0.9
lock_speed_kph=30
lock_throttle_min=0.2
shift_release_coupling=0.5
engage_rate=12
disengage_rate=18
"
                : string.Empty;
            var torqueCurveSection = includeTorqueCurveSection
                ? @"
[torque_curve]
700rpm=120
3000rpm=280
6500rpm=180
"
                : string.Empty;
            var stopLine = string.IsNullOrWhiteSpace(stopSound) ? string.Empty : $"stop={stopSound}\n";
            var tireWearLines = string.IsNullOrWhiteSpace(tireWearOverrides)
                ? string.Empty
                : $"{tireWearOverrides.Trim()}\n";

            return $@"
[meta]
name=Parser Test Vehicle
version=1
description=Parser validation test

[sounds]
engine=builtin6
start=builtin1
{stopLine}horn=builtin/horn.ogg
crash=builtin3
brake=builtin/brake.ogg
idle_freq=400
top_freq=2200
shift_freq=1200
pitch_curve_exponent=0.85

[general]
surface_traction_factor=1
max_speed=180
has_wipers=0

[engine]
idle_rpm=700
max_rpm=7000
rev_limiter=6500
auto_shift_rpm=0
engine_braking=0.3
mass_kg=1500
drivetrain_efficiency=0.85
launch_rpm=1800

[torque]
engine_braking_torque=150
peak_torque=280
peak_torque_rpm=3500
idle_torque=120
redline_torque=180
power_factor=0.5

[engine_rot]
inertia_kgm2=0.24
coupling_rate=12
friction_base_nm=20
friction_linear_nm_per_krpm=6
friction_quadratic_nm_per_krpm2=0.4
idle_control_window_rpm=150
idle_control_gain_nm_per_rpm=0.08
min_coupled_rise_idle_rpm_per_s=2200
min_coupled_rise_full_rpm_per_s=6200
overrun_idle_fraction=0.25
overrun_curve_exponent=1.35
brake_transfer_efficiency=0.64

[resistance]
drag_coefficient=0.3
frontal_area=2.2
side_area=3.9
rolling_resistance=0.015
wheel_side_drag_n=94
wheel_side_drag_linear_n_per_mps=3.6
rolling_speed_factor=0.014
driveline_drag_nm=22
driveline_viscous_drag_nm_per_krpm=7.5

{torqueCurveSection}

[transmission]
primary_type={primaryType}
supported_types={supportedTypes}
shift_on_demand={(shiftOnDemand ? 1 : 0)}
{atcSection}

[drivetrain]
final_drive=3.8
reverse_max_speed=35
reverse_power_factor=0.55
reverse_gear_ratio=3.2
brake_strength=1.0

[gears]
number_of_gears=5
gear_ratios=3.7,2.1,1.4,1.1,0.9

[steering]
steering_response=1.2
wheelbase=2.6
max_steer_deg=32
high_speed_stability=0.25
high_speed_steer_gain=0.92
high_speed_steer_start_kph=140
high_speed_steer_full_kph=220

[tire_model]
tire_grip=0.92
lateral_grip=1.00
combined_grip_penalty=0.72
slip_angle_peak_deg=8.0
slip_angle_falloff=1.25
turn_response=1.05
mass_sensitivity=0.75
downforce_grip_gain=0.10
{tireWearLines}

[dynamics]
corner_stiffness_front=1.05
corner_stiffness_rear=0.98
yaw_inertia_scale=1.05
steering_curve=1.00
transient_damping=1.10

[dimensions]
vehicle_width=1.84
vehicle_length=4.40

[tires]
tire_width=215
tire_aspect=55
tire_rim=17
";
        }

        private sealed class TempVehicleFile : IDisposable
        {
            public string Path { get; }

            private TempVehicleFile(string path)
            {
                Path = path;
            }

            public static TempVehicleFile Create(string content)
            {
                var path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"topspeed_vehicle_{Guid.NewGuid():N}.tsv");
                File.WriteAllText(path, content);
                return new TempVehicleFile(path);
            }

            public void Dispose()
            {
                if (File.Exists(Path))
                    File.Delete(Path);
            }
        }
    }
}
