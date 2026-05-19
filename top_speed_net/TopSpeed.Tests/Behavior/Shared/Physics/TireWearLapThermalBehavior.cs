using System;
using TopSpeed.Physics.Tires;
using TopSpeed.Physics.Tires.Wear;
using TopSpeed.Vehicles;
using Xunit;

namespace TopSpeed.Tests;

[Trait("Category", "Behavior")]
public sealed class TireWearLapThermalBehaviorTests
{
    private static readonly LapSegment[] AmericaLikeLap =
    {
        new LapSegment(20, 0.92f, 0, 0.10f),
        new LapSegment(9, 0.82f, 22, 0.16f),
        new LapSegment(17, 0.90f, 0, 0.10f),
        new LapSegment(11, 0.76f, 30, 0.18f),
        new LapSegment(16, 0.88f, 0, 0.10f),
        new LapSegment(10, 0.74f, 36, 0.20f),
        new LapSegment(15, 0.90f, 0, 0.10f),
    };

    private static readonly LapSegment[] ThermalStressLap =
    {
        new LapSegment(18, 0.88f, 0, 0.12f),
        new LapSegment(12, 0.78f, 30, 0.22f),
        new LapSegment(14, 0.86f, 0, 0.12f),
        new LapSegment(12, 0.72f, 40, 0.26f),
        new LapSegment(14, 0.84f, 0, 0.12f),
        new LapSegment(11, 0.70f, 46, 0.30f),
        new LapSegment(12, 0.86f, 0, 0.12f),
    };

    [Fact]
    public void OfficialProfiles_LapLikeRun_FreshTires_ShouldAvoidEarlyOverheat()
    {
        foreach (var spec in OfficialVehicleCatalog.Vehicles)
        {
            var summary = SimulateLapProfile(
                spec,
                new TireWearState(wearFraction: 0.06f, temperatureC: 30f, smoothedSlipNormalized: 0.08f),
                lapCount: 2,
                AmericaLikeLap);

            summary.PeakTemperatureC.Should().BeLessThan(
                spec.TireWearConfig.OverheatEndTemperatureC + 10f,
                $"{spec.Name} should not overheat quickly when tires are still fresh");
            summary.FinalWearFraction.Should().BeLessThan(
                0.35f,
                $"{spec.Name} should not consume excessive tire life in only two America-like laps");
            summary.FinalTemperatureC.Should().BeGreaterThan(
                spec.TireWearConfig.ColdEndTemperatureC + 5f,
                $"{spec.Name} should still warm beyond the cold band during lap-like driving");
        }
    }

    [Fact]
    public void OfficialProfiles_LapLikeRun_WornTires_ShouldLoseThermalControl()
    {
        foreach (var spec in OfficialVehicleCatalog.Vehicles)
        {
            var fresh = SimulateLapProfile(
                spec,
                new TireWearState(wearFraction: 0.10f, temperatureC: 78f, smoothedSlipNormalized: 0.20f),
                lapCount: 4,
                ThermalStressLap);
            var worn = SimulateLapProfile(
                spec,
                new TireWearState(wearFraction: 0.82f, temperatureC: 78f, smoothedSlipNormalized: 0.20f),
                lapCount: 4,
                ThermalStressLap);

            if (fresh.PeakTemperatureC > spec.TireWearConfig.OptimalEndTemperatureC + 2f)
            {
                worn.PeakTemperatureC.Should().BeGreaterThan(
                    fresh.PeakTemperatureC + 1f,
                    $"{spec.Name} worn tires should run hotter than fresh tires once laps are thermally demanding");
            }

            var freshWearDelta = fresh.FinalWearFraction - 0.10f;
            var wornWearDelta = worn.FinalWearFraction - 0.82f;
            wornWearDelta.Should().BeGreaterThan(
                freshWearDelta * 1.20f,
                $"{spec.Name} worn tires should wear faster once thermal control is degraded");
        }
    }

    [Fact]
    public void OfficialProfiles_LapLikeRun_HighSpeedStraights_ShouldProvideThermalRecovery()
    {
        foreach (var spec in OfficialVehicleCatalog.Vehicles)
        {
            var summary = SimulateLapProfile(
                spec,
                new TireWearState(wearFraction: 0.22f, temperatureC: 90f, smoothedSlipNormalized: 0.22f),
                lapCount: 3,
                AmericaLikeLap);

            if (summary.StraightRecoveryChecks == 0)
            {
                summary.PeakTemperatureC.Should().BeLessThan(
                    spec.TireWearConfig.OverheatEndTemperatureC + 4f,
                    $"{spec.Name} should not overheat when no straight recovery window is triggered");
                continue;
            }

            summary.StraightRecoverySuccesses.Should().BeGreaterThanOrEqualTo(
                (int)Math.Ceiling(summary.StraightRecoveryChecks * 0.70f),
                $"{spec.Name} should usually cool or stabilize on high-speed straights when slip is low");
        }
    }

    private static LapRunSummary SimulateLapProfile(
        OfficialVehicleSpec spec,
        TireWearState initialWearState,
        int lapCount,
        LapSegment[] lapProfile)
    {
        const float ambientTemperatureC = 26f;
        const float surfaceTemperatureC = 33f;
        const float wetnessNormalized = 0f;
        const float dt = 1f;

        var tireParameters = CreateTireParameters(spec);
        var tireState = new TireModelState(0f, 0f);
        var wearState = initialWearState;
        var runtime = TireWearRuntime.Resolve(spec.TireWearConfig, wearState);
        var peakTemperatureC = wearState.TemperatureC;
        var straightRecoveryChecks = 0;
        var straightRecoverySuccesses = 0;

        for (var lap = 0; lap < lapCount; lap++)
        {
            foreach (var segment in lapProfile)
            {
                var speedMps = Clamp((spec.TopSpeed / 3.6f) * segment.SpeedFactor, 18f, 96f);
                var segmentStartTemperatureC = wearState.TemperatureC;
                var segmentSeconds = segment.DurationSeconds;

                for (var second = 0; second < segmentSeconds; second++)
                {
                    var tireOutput = TireModelSolver.Solve(
                        tireParameters,
                        new TireModelInput(
                            dt,
                            speedMps,
                            segment.SteeringInput,
                            surfaceTractionMod: 1f,
                            surfaceLateralMultiplier: 1f),
                        tireState);
                    tireState = tireOutput.State;

                    var loadNormalized = TireWearInputSignals.ResolveLoadNormalized(
                        spec.MassKg,
                        tireOutput.LateralLoadRatio,
                        segment.LongitudinalSlipNormalized);
                    var rollingResistanceNormalized = TireWearInputSignals.ResolveRollingResistanceNormalized(
                        spec.RollingResistanceCoefficient,
                        surfaceRollingResistanceFactor: 1f,
                        speedMps);

                    runtime = TireWearRuntime.Step(
                        spec.TireWearConfig,
                        wearState,
                        new TireWearInput(
                            dt,
                            speedMps,
                            tireOutput.SlipAngleNormalized,
                            tireOutput.LateralSlipNormalized,
                            segment.LongitudinalSlipNormalized,
                            loadNormalized,
                            rollingResistanceNormalized,
                            ambientTemperatureC,
                            surfaceTemperatureC,
                            wetnessNormalized));
                    wearState = runtime.State;
                }

                peakTemperatureC = Math.Max(peakTemperatureC, wearState.TemperatureC);
                if (segment.SteeringInput == 0 && speedMps >= 36f && segmentStartTemperatureC > spec.TireWearConfig.OptimalStartTemperatureC + 2f)
                {
                    straightRecoveryChecks++;
                    if (wearState.TemperatureC <= segmentStartTemperatureC + 1f)
                        straightRecoverySuccesses++;
                }
            }
        }

        return new LapRunSummary(
            PeakTemperatureC: peakTemperatureC,
            FinalTemperatureC: wearState.TemperatureC,
            FinalWearFraction: wearState.WearFraction,
            StraightRecoveryChecks: straightRecoveryChecks,
            StraightRecoverySuccesses: straightRecoverySuccesses);
    }

    private static TireModelParameters CreateTireParameters(OfficialVehicleSpec spec)
    {
        return new TireModelParameters(
            spec.Steering,
            spec.MaxSteerDeg,
            spec.WheelbaseM,
            spec.WidthM,
            spec.LengthM,
            spec.TireGripCoefficient,
            spec.LateralGripCoefficient,
            spec.HighSpeedStability,
            spec.MassKg,
            spec.HighSpeedSteerGain,
            spec.HighSpeedSteerStartKph,
            spec.HighSpeedSteerFullKph,
            spec.CombinedGripPenalty,
            spec.SlipAnglePeakDeg,
            spec.SlipAngleFalloff,
            spec.TurnResponse,
            spec.MassSensitivity,
            spec.DownforceGripGain,
            spec.CornerStiffnessFront,
            spec.CornerStiffnessRear,
            spec.YawInertiaScale,
            spec.SteeringCurve,
            spec.TransientDamping);
    }

    private static float Clamp(float value, float min, float max)
    {
        if (value < min)
            return min;
        if (value > max)
            return max;
        return value;
    }

    private readonly record struct LapSegment(
        int DurationSeconds,
        float SpeedFactor,
        int SteeringInput,
        float LongitudinalSlipNormalized);

    private readonly record struct LapRunSummary(
        float PeakTemperatureC,
        float FinalTemperatureC,
        float FinalWearFraction,
        int StraightRecoveryChecks,
        int StraightRecoverySuccesses);
}
