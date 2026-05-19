using System;
using TopSpeed.Physics.Tires;
using TopSpeed.Physics.Tires.Wear;
using TopSpeed.Vehicles;
using Xunit;

namespace TopSpeed.Tests;

[Trait("Category", "Behavior")]
public sealed class TireWearCornerExitCoolingBehaviorTests
{
    [Fact]
    public void OfficialProfiles_CornerExitStraightAtSpeed_ShouldShowMeasurableCoolingTrend()
    {
        foreach (var spec in OfficialVehicleCatalog.Vehicles)
        {
            var result = RunCornerExitScenario(
                spec,
                straightSteeringInputResolver: _ => 0,
                straightLongitudinalSlip: 0.10f);
            var requiredDropC = ResolveRequiredDropC(result.EntryTemperatureC, spec.TireWearConfig.OptimalEndTemperatureC);

            result.EntryTemperatureC.Should().BeGreaterThan(
                spec.TireWearConfig.OptimalStartTemperatureC + 1f,
                $"{spec.Name} should be heated before straight-exit cooling is evaluated");
            result.TemperatureAfter45SecondsC.Should().BeLessThan(
                result.EntryTemperatureC - requiredDropC,
                $"{spec.Name} should cool on straight at speed after cornering");
            result.DecreasingSeconds.Should().BeGreaterThanOrEqualTo(
                24,
                $"{spec.Name} should show consistent downward temperature trend during straight exit");
        }
    }

    [Fact]
    public void OfficialProfiles_CornerExitWithSteeringCorrections_ShouldStillCoolAtSpeed()
    {
        foreach (var spec in OfficialVehicleCatalog.Vehicles)
        {
            var result = RunCornerExitScenario(
                spec,
                straightSteeringInputResolver: second => second % 2 == 0 ? 6 : -6,
                straightLongitudinalSlip: 0.12f);
            var requiredDropC = ResolveRequiredDropC(result.EntryTemperatureC, spec.TireWearConfig.OptimalEndTemperatureC) * 0.65f;

            result.EntryTemperatureC.Should().BeGreaterThan(
                spec.TireWearConfig.OptimalStartTemperatureC + 1f,
                $"{spec.Name} should be heated before corrected-straight cooling is evaluated");
            result.TemperatureAfter45SecondsC.Should().BeLessThan(
                result.EntryTemperatureC - requiredDropC,
                $"{spec.Name} should still cool with light steering corrections at speed");
            result.DecreasingSeconds.Should().BeGreaterThanOrEqualTo(
                18,
                $"{spec.Name} should still show a mostly downward trend with light steering corrections");
        }
    }

    [Fact]
    public void OfficialProfiles_CornerExitWithModerateSteeringCorrections_ShouldStillCoolAtSpeed()
    {
        foreach (var spec in OfficialVehicleCatalog.Vehicles)
        {
            var result = RunCornerExitScenario(
                spec,
                straightSteeringInputResolver: second =>
                {
                    if (second % 6 == 0)
                        return 14;
                    if (second % 6 == 3)
                        return -14;
                    return 0;
                },
                straightLongitudinalSlip: 0.14f);
            var requiredDropC = ResolveRequiredDropC(result.EntryTemperatureC, spec.TireWearConfig.OptimalEndTemperatureC) * 0.45f;

            result.EntryTemperatureC.Should().BeGreaterThan(
                spec.TireWearConfig.OptimalStartTemperatureC + 1f,
                $"{spec.Name} should be heated before moderate-correction cooling is evaluated");
            result.TemperatureAfter45SecondsC.Should().BeLessThan(
                result.EntryTemperatureC - requiredDropC,
                $"{spec.Name} should cool even with moderate steering corrections on a straight " +
                $"(avgHeat={result.AverageHeatingRateCPerSecond:F2}, avgCool={result.AverageCoolingRateCPerSecond:F2}, " +
                $"avgSlipAngle={result.AverageSlipAngleNormalized:F2}, avgLateralSlip={result.AverageLateralSlipNormalized:F2})");
            result.DecreasingSeconds.Should().BeGreaterThanOrEqualTo(
                14,
                $"{spec.Name} should maintain a net cooling trend with moderate steering corrections");
        }
    }

    private static CornerExitCoolingResult RunCornerExitScenario(
        OfficialVehicleSpec spec,
        Func<int, int> straightSteeringInputResolver,
        float straightLongitudinalSlip)
    {
        const float dt = 1f;
        const float ambientTemperatureC = 26f;
        const float surfaceTemperatureC = 33f;
        const float wetnessNormalized = 0f;
        const int cornerSteeringInput = 48;
        const float cornerLongitudinalSlip = 0.24f;
        const int cornerSeconds = 100;
        const int straightSeconds = 45;

        var speedMps = Clamp((spec.TopSpeed / 3.6f) * 0.78f, 24f, 72f);
        var tireParameters = CreateTireParameters(spec);
        var tireState = new TireModelState(0f, 0f);
        var wearState = TireWearDefaults.CreateInitialState(ambientTemperatureC);
        var runtime = TireWearRuntime.Resolve(spec.TireWearConfig, wearState);

        for (var second = 0; second < cornerSeconds; second++)
        {
            var tireOutput = TireModelSolver.Solve(
                tireParameters,
                new TireModelInput(
                    dt,
                    speedMps,
                    cornerSteeringInput,
                    surfaceTractionMod: 1f,
                    surfaceLateralMultiplier: 1f),
                tireState);
            tireState = tireOutput.State;

            runtime = TireWearRuntime.Step(
                spec.TireWearConfig,
                wearState,
                new TireWearInput(
                    dt,
                    speedMps,
                    tireOutput.SlipAngleNormalized,
                    tireOutput.LateralSlipNormalized,
                    cornerLongitudinalSlip,
                    ResolveLoadNormalized(spec.MassKg, tireOutput.LateralLoadRatio, cornerLongitudinalSlip),
                    ResolveRollingResistanceNormalized(spec.RollingResistanceCoefficient, speedMps),
                    ambientTemperatureC,
                    surfaceTemperatureC,
                    wetnessNormalized));
            wearState = runtime.State;
        }

        var entryTemperatureC = wearState.TemperatureC;
        var decreasingSeconds = 0;
        var previousTemperatureC = entryTemperatureC;
        var temperatureAfter45SecondsC = entryTemperatureC;
        var heatingRateSum = 0f;
        var coolingRateSum = 0f;
        var slipAngleSum = 0f;
        var lateralSlipSum = 0f;

        for (var second = 1; second <= straightSeconds; second++)
        {
            var straightSteeringInput = straightSteeringInputResolver(second);
            var tireOutput = TireModelSolver.Solve(
                tireParameters,
                new TireModelInput(
                    dt,
                    speedMps,
                    straightSteeringInput,
                    surfaceTractionMod: 1f,
                    surfaceLateralMultiplier: 1f),
                tireState);
            tireState = tireOutput.State;

            runtime = TireWearRuntime.Step(
                spec.TireWearConfig,
                wearState,
                new TireWearInput(
                    dt,
                    speedMps,
                    tireOutput.SlipAngleNormalized,
                    tireOutput.LateralSlipNormalized,
                    straightLongitudinalSlip,
                    ResolveLoadNormalized(spec.MassKg, tireOutput.LateralLoadRatio, straightLongitudinalSlip),
                    ResolveRollingResistanceNormalized(spec.RollingResistanceCoefficient, speedMps),
                    ambientTemperatureC,
                    surfaceTemperatureC,
                    wetnessNormalized));
            wearState = runtime.State;
            heatingRateSum += runtime.HeatingRateCPerSecond;
            coolingRateSum += runtime.CoolingRateCPerSecond;
            slipAngleSum += tireOutput.SlipAngleNormalized;
            lateralSlipSum += tireOutput.LateralSlipNormalized;

            if (wearState.TemperatureC < previousTemperatureC)
                decreasingSeconds++;
            previousTemperatureC = wearState.TemperatureC;

            if (second == straightSeconds)
                temperatureAfter45SecondsC = wearState.TemperatureC;
        }

        return new CornerExitCoolingResult(
            entryTemperatureC,
            temperatureAfter45SecondsC,
            decreasingSeconds,
            heatingRateSum / straightSeconds,
            coolingRateSum / straightSeconds,
            slipAngleSum / straightSeconds,
            lateralSlipSum / straightSeconds);
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

    private static float ResolveRequiredDropC(float entryTemperatureC, float optimalEndTemperatureC)
    {
        var overOptimalC = entryTemperatureC - optimalEndTemperatureC;
        return Clamp((overOptimalC * 0.45f) + 2.5f, 3f, 14f);
    }

    private static float ResolveLoadNormalized(float massKg, float lateralLoadRatio, float longitudinalSlipNormalized)
    {
        var massNormalized = Clamp01((massKg - 700f) / 1800f);
        return Clamp01(
            (lateralLoadRatio * 0.56f)
            + (longitudinalSlipNormalized * 0.24f)
            + (massNormalized * 0.20f));
    }

    private static float ResolveRollingResistanceNormalized(float rollingResistanceCoefficient, float speedMps)
    {
        var speedFactor = Clamp01(speedMps / 45f);
        var normalizedRollingCoefficient = Clamp01(rollingResistanceCoefficient / 0.030f);
        return Clamp01(normalizedRollingCoefficient * speedFactor);
    }

    private static float Clamp01(float value)
    {
        return Clamp(value, 0f, 1f);
    }

    private static float Clamp(float value, float min, float max)
    {
        if (value < min)
            return min;
        if (value > max)
            return max;
        return value;
    }

    private readonly record struct CornerExitCoolingResult(
        float EntryTemperatureC,
        float TemperatureAfter45SecondsC,
        int DecreasingSeconds,
        float AverageHeatingRateCPerSecond,
        float AverageCoolingRateCPerSecond,
        float AverageSlipAngleNormalized,
        float AverageLateralSlipNormalized);
}
