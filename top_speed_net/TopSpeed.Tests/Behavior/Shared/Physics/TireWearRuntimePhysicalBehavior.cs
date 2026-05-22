using System;
using TopSpeed.Physics.Tires.Wear;
using TopSpeed.Vehicles;
using Xunit;

namespace TopSpeed.Tests;

[Trait("Category", "Behavior")]
public sealed class TireWearRuntimePhysicalBehaviorTests
{
    [Fact]
    public void Vehicle1_AggressiveStraight_ShouldWarmBeyondColdBandWithoutRunaway()
    {
        var config = OfficialVehicleCatalog.Get(0).TireWearConfig;
        var result = RunForDuration(
            config,
            TireWearDefaults.CreateInitialState(26f),
            durationSeconds: 900f,
            speedMps: 44f,
            slipAngleNormalized: 0.03f,
            lateralSlipNormalized: 0.02f,
            longitudinalSlipNormalized: 0.30f,
            loadNormalized: 0.34f,
            rollingResistanceNormalized: 0.40f,
            ambientTemperatureC: 26f,
            surfaceTemperatureC: 33f,
            wetnessNormalized: 0f);

        result.State.TemperatureC.Should().BeGreaterThan(config.ColdEndTemperatureC + 8f);
        result.State.TemperatureC.Should().BeLessThan(config.OverheatEndTemperatureC + 8f);
    }

    [Fact]
    public void OfficialProfiles_AggressiveStraight_ShouldWarmBeyondColdBand()
    {
        foreach (var spec in OfficialVehicleCatalog.Vehicles)
        {
            var config = spec.TireWearConfig;
            var result = RunForDuration(
                config,
                TireWearDefaults.CreateInitialState(26f),
                durationSeconds: 900f,
                speedMps: 40f,
                slipAngleNormalized: 0.03f,
                lateralSlipNormalized: 0.02f,
                longitudinalSlipNormalized: 0.28f,
                loadNormalized: 0.32f,
                rollingResistanceNormalized: 0.38f,
                ambientTemperatureC: 26f,
                surfaceTemperatureC: 33f,
                wetnessNormalized: 0f);

            var minimumWorkingTemperatureC = config.ColdEndTemperatureC + 8f;
            result.State.TemperatureC.Should().BeGreaterThanOrEqualTo(
                minimumWorkingTemperatureC,
                $"{spec.Name} should warm beyond the cold band under sustained aggressive straight driving");
        }
    }

    [Fact]
    public void OfficialProfiles_GentleStraight_ShouldRemainBelowOverheatRange()
    {
        foreach (var spec in OfficialVehicleCatalog.Vehicles)
        {
            var config = spec.TireWearConfig;
            var result = RunForDuration(
                config,
                TireWearDefaults.CreateInitialState(26f),
                durationSeconds: 1200f,
                speedMps: 32f,
                slipAngleNormalized: 0.01f,
                lateralSlipNormalized: 0.01f,
                longitudinalSlipNormalized: 0.08f,
                loadNormalized: 0.20f,
                rollingResistanceNormalized: 0.30f,
                ambientTemperatureC: 26f,
                surfaceTemperatureC: 33f,
                wetnessNormalized: 0f);

            result.State.TemperatureC.Should().BeLessThan(
                config.OverheatEndTemperatureC,
                $"{spec.Name} should not overheat during long gentle straight cruising");
        }
    }

    [Fact]
    public void Vehicle1_PostCornerStraight_ShouldCoolGraduallyNotCollapse()
    {
        var config = OfficialVehicleCatalog.Get(0).TireWearConfig;
        var heated = RunForDuration(
            config,
            TireWearDefaults.CreateInitialState(28f),
            durationSeconds: 120f,
            speedMps: 44f,
            slipAngleNormalized: 0.70f,
            lateralSlipNormalized: 0.55f,
            longitudinalSlipNormalized: 0.45f,
            loadNormalized: 0.62f,
            rollingResistanceNormalized: 0.36f,
            ambientTemperatureC: 28f,
            surfaceTemperatureC: 36f,
            wetnessNormalized: 0f);

        var shortStraight = RunForDuration(
            config,
            heated.State,
            durationSeconds: 15f,
            speedMps: 50f,
            slipAngleNormalized: 0.02f,
            lateralSlipNormalized: 0.01f,
            longitudinalSlipNormalized: 0.05f,
            loadNormalized: 0.22f,
            rollingResistanceNormalized: 0.32f,
            ambientTemperatureC: 28f,
            surfaceTemperatureC: 36f,
            wetnessNormalized: 0f);

        shortStraight.State.TemperatureC.Should().BeGreaterThan(heated.State.TemperatureC - 30f);
        shortStraight.State.TemperatureC.Should().BeGreaterThan(config.OptimalStartTemperatureC - 12f);
    }

    [Fact]
    public void Vehicle1_LowSlipStraight_ShouldWarmOutOfColdBandAndStabilize()
    {
        var config = OfficialVehicleCatalog.Get(0).TireWearConfig;
        var result = RunForDuration(
            config,
            TireWearDefaults.CreateInitialState(26f),
            durationSeconds: 900f,
            speedMps: 38f,
            slipAngleNormalized: 0.01f,
            lateralSlipNormalized: 0.01f,
            longitudinalSlipNormalized: 0.03f,
            loadNormalized: 0.16f,
            rollingResistanceNormalized: 0.28f,
            ambientTemperatureC: 26f,
            surfaceTemperatureC: 33f,
            wetnessNormalized: 0f);

        result.State.TemperatureC.Should().BeGreaterThan(config.ColdEndTemperatureC + 7f);
        result.State.TemperatureC.Should().BeLessThan(config.OverheatEndTemperatureC);
    }

    [Fact]
    public void Vehicle1_LowSlipStraight_ShouldReach40CAndEnterWorkingRangeWithinReasonableDistance()
    {
        var config = OfficialVehicleCatalog.Get(0).TireWearConfig;
        var secondsToForty = ResolveSecondsToTemperature(
            config,
            TireWearDefaults.CreateInitialState(26f),
            targetTemperatureC: 40f,
            maxSeconds: 900f,
            speedMps: 38f,
            slipAngleNormalized: 0.01f,
            lateralSlipNormalized: 0.01f,
            longitudinalSlipNormalized: 0.01f,
            loadNormalized: 0.16f,
            rollingResistanceNormalized: 0.28f,
            ambientTemperatureC: 26f,
            surfaceTemperatureC: 33f,
            wetnessNormalized: 0f);
        var secondsToNinety = ResolveSecondsToTemperature(
            config,
            TireWearDefaults.CreateInitialState(26f),
            targetTemperatureC: 90f,
            maxSeconds: 1200f,
            speedMps: 38f,
            slipAngleNormalized: 0.01f,
            lateralSlipNormalized: 0.01f,
            longitudinalSlipNormalized: 0.01f,
            loadNormalized: 0.16f,
            rollingResistanceNormalized: 0.28f,
            ambientTemperatureC: 26f,
            surfaceTemperatureC: 33f,
            wetnessNormalized: 0f);

        secondsToForty.Should().NotBeNull();
        secondsToForty!.Value.Should().BeLessThan(700f);
        secondsToNinety.Should().NotBeNull();
        secondsToNinety!.Value.Should().BeGreaterThan(200f);
        secondsToNinety.Value.Should().BeLessThan(650f);
    }

    [Fact]
    public void Vehicle1_AggressiveStraight_ShouldCreateMoreWearStressThanLowSlipStraight()
    {
        var config = OfficialVehicleCatalog.Get(0).TireWearConfig;
        var aggressive = RunForDuration(
            config,
            TireWearDefaults.CreateInitialState(26f),
            durationSeconds: 900f,
            speedMps: 44f,
            slipAngleNormalized: 0.03f,
            lateralSlipNormalized: 0.02f,
            longitudinalSlipNormalized: 0.30f,
            loadNormalized: 0.34f,
            rollingResistanceNormalized: 0.40f,
            ambientTemperatureC: 26f,
            surfaceTemperatureC: 33f,
            wetnessNormalized: 0f);
        var lowSlip = RunForDuration(
            config,
            TireWearDefaults.CreateInitialState(26f),
            durationSeconds: 900f,
            speedMps: 38f,
            slipAngleNormalized: 0.01f,
            lateralSlipNormalized: 0.01f,
            longitudinalSlipNormalized: 0.03f,
            loadNormalized: 0.16f,
            rollingResistanceNormalized: 0.28f,
            ambientTemperatureC: 26f,
            surfaceTemperatureC: 33f,
            wetnessNormalized: 0f);

        aggressive.State.TemperatureC.Should().BeGreaterThan(lowSlip.State.TemperatureC - 6f);
        aggressive.State.WearFraction.Should().BeGreaterThan(lowSlip.State.WearFraction);
    }

    [Fact]
    public void OfficialProfiles_LowSlipStraight_ShouldLeaveColdBandWithinReasonableTime()
    {
        foreach (var spec in OfficialVehicleCatalog.Vehicles)
        {
            var config = spec.TireWearConfig;
            var massNormalized = Clamp01((spec.MassKg - 300f) / 1600f);
            var target = config.ColdEndTemperatureC + (4f + (5f * massNormalized));
            var seconds = ResolveSecondsToTemperature(
                config,
                TireWearDefaults.CreateInitialState(26f),
                targetTemperatureC: target,
                maxSeconds: 1200f,
                speedMps: 36f,
                slipAngleNormalized: 0.01f,
                lateralSlipNormalized: 0.01f,
                longitudinalSlipNormalized: 0.03f,
                loadNormalized: 0.15f,
                rollingResistanceNormalized: 0.28f,
                ambientTemperatureC: 26f,
                surfaceTemperatureC: 33f,
                wetnessNormalized: 0f);

            seconds.Should().NotBeNull($"{spec.Name} should leave cold band during sustained straight-line throttle");
            seconds!.Value.Should().BeLessThan(1100f, $"{spec.Name} warm-up should not take excessively long in dry warm weather");
        }
    }

    [Fact]
    public void Vehicle1_HighSpeedFreshStraight_ShouldStabilizeWithoutEarlyThermalRunaway()
    {
        var config = OfficialVehicleCatalog.Get(0).TireWearConfig;
        var result = RunForDuration(
            config,
            new TireWearState(wearFraction: 0.08f, temperatureC: 38f, smoothedSlipNormalized: 0.08f),
            durationSeconds: 900f,
            speedMps: 82f,
            slipAngleNormalized: 0.02f,
            lateralSlipNormalized: 0.02f,
            longitudinalSlipNormalized: 0.10f,
            loadNormalized: 0.24f,
            rollingResistanceNormalized: 0.31f,
            ambientTemperatureC: 27f,
            surfaceTemperatureC: 34f,
            wetnessNormalized: 0f);

        result.State.TemperatureC.Should().BeGreaterThan(config.ColdEndTemperatureC + 3f);
        result.State.TemperatureC.Should().BeLessThan(config.OverheatEndTemperatureC + 10f);
    }

    [Fact]
    public void CupStyleProfile_HighSpeedModerateSlip_ShouldAvoidEarlyOverheatOnFreshTires()
    {
        var config = TireWearProfiles.CreateFromVehicle(
            tireGripCoefficient: 1.16f,
            massKg: 1560f,
            tireCircumferenceM: 2.28f,
            lateralGripCoefficient: 1.12f);
        var result = RunForDuration(
            config,
            new TireWearState(wearFraction: 0.06f, temperatureC: 30f, smoothedSlipNormalized: 0.10f),
            durationSeconds: 240f,
            speedMps: 94f,
            slipAngleNormalized: 0.24f,
            lateralSlipNormalized: 0.18f,
            longitudinalSlipNormalized: 0.14f,
            loadNormalized: 0.55f,
            rollingResistanceNormalized: 0.34f,
            ambientTemperatureC: 26f,
            surfaceTemperatureC: 33f,
            wetnessNormalized: 0f);

        result.State.TemperatureC.Should().BeLessThan(config.OverheatEndTemperatureC + 6f);
        result.State.WearFraction.Should().BeLessThan(0.36f);
    }

    [Fact]
    public void Vehicle1_HighUtilizationWithoutSlide_ShouldRunCoolerThanActiveSlide()
    {
        var config = OfficialVehicleCatalog.Get(0).TireWearConfig;
        var initialState = TireWearDefaults.CreateInitialState(30f);
        var highUtilization = RunForDuration(
            config,
            initialState,
            durationSeconds: 180f,
            speedMps: 82f,
            slipAngleNormalized: 1.0f,
            lateralSlipNormalized: 1.0f,
            longitudinalSlipNormalized: 0.10f,
            loadNormalized: 0.62f,
            rollingResistanceNormalized: 0.33f,
            ambientTemperatureC: 26f,
            surfaceTemperatureC: 33f,
            wetnessNormalized: 0f);
        var activeSlide = RunForDuration(
            config,
            initialState,
            durationSeconds: 180f,
            speedMps: 82f,
            slipAngleNormalized: 2.1f,
            lateralSlipNormalized: 2.0f,
            longitudinalSlipNormalized: 0.10f,
            loadNormalized: 0.62f,
            rollingResistanceNormalized: 0.33f,
            ambientTemperatureC: 26f,
            surfaceTemperatureC: 33f,
            wetnessNormalized: 0f);

        activeSlide.State.TemperatureC.Should().BeGreaterThan(highUtilization.State.TemperatureC + 1f);
        activeSlide.State.WearFraction.Should().BeGreaterThan(highUtilization.State.WearFraction);
    }

    [Fact]
    public void Vehicle1_WornTires_ShouldHeatUpFasterAndWearFasterThanFreshTires()
    {
        var config = OfficialVehicleCatalog.Get(0).TireWearConfig;
        var freshInitial = new TireWearState(wearFraction: 0.15f, temperatureC: 92f, smoothedSlipNormalized: 0.35f);
        var wornInitial = new TireWearState(wearFraction: 0.70f, temperatureC: 92f, smoothedSlipNormalized: 0.35f);

        var fresh = RunForDuration(
            config,
            freshInitial,
            durationSeconds: 120f,
            speedMps: 58f,
            slipAngleNormalized: 0.62f,
            lateralSlipNormalized: 0.48f,
            longitudinalSlipNormalized: 0.44f,
            loadNormalized: 0.66f,
            rollingResistanceNormalized: 0.36f,
            ambientTemperatureC: 28f,
            surfaceTemperatureC: 35f,
            wetnessNormalized: 0f);
        var worn = RunForDuration(
            config,
            wornInitial,
            durationSeconds: 120f,
            speedMps: 58f,
            slipAngleNormalized: 0.62f,
            lateralSlipNormalized: 0.48f,
            longitudinalSlipNormalized: 0.44f,
            loadNormalized: 0.66f,
            rollingResistanceNormalized: 0.36f,
            ambientTemperatureC: 28f,
            surfaceTemperatureC: 35f,
            wetnessNormalized: 0f);

        worn.State.TemperatureC.Should().BeGreaterThan(fresh.State.TemperatureC + 1f);
        var freshWearDelta = fresh.State.WearFraction - freshInitial.WearFraction;
        var wornWearDelta = worn.State.WearFraction - wornInitial.WearFraction;
        wornWearDelta.Should().BeGreaterThan(freshWearDelta * 1.20f);
    }

    private static float? ResolveSecondsToTemperature(
        TireWearConfig config,
        TireWearState initialState,
        float targetTemperatureC,
        float maxSeconds,
        float speedMps,
        float slipAngleNormalized,
        float lateralSlipNormalized,
        float longitudinalSlipNormalized,
        float loadNormalized,
        float rollingResistanceNormalized,
        float ambientTemperatureC,
        float surfaceTemperatureC,
        float wetnessNormalized)
    {
        const float stepSeconds = 1f;
        var elapsed = 0f;
        var state = initialState;

        while (elapsed < maxSeconds)
        {
            var dt = Math.Min(stepSeconds, maxSeconds - elapsed);
            var result = TireWearRuntime.Step(
                config,
                state,
                new TireWearInput(
                    dt,
                    speedMps,
                    slipAngleNormalized,
                    lateralSlipNormalized,
                    longitudinalSlipNormalized,
                    loadNormalized,
                    rollingResistanceNormalized,
                    ambientTemperatureC,
                    surfaceTemperatureC,
                    wetnessNormalized));
            elapsed += dt;
            state = result.State;

            if (state.TemperatureC >= targetTemperatureC)
                return elapsed;
        }

        return null;
    }

    private static TireWearRuntimeResult RunForDuration(
        TireWearConfig config,
        TireWearState initialState,
        float durationSeconds,
        float speedMps,
        float slipAngleNormalized,
        float lateralSlipNormalized,
        float longitudinalSlipNormalized,
        float loadNormalized,
        float rollingResistanceNormalized,
        float ambientTemperatureC,
        float surfaceTemperatureC,
        float wetnessNormalized)
    {
        const float stepSeconds = 1f;
        var remaining = Math.Max(0f, durationSeconds);
        var state = initialState;
        var runtime = TireWearRuntime.Resolve(config, state);

        while (remaining > 0f)
        {
            var dt = Math.Min(stepSeconds, remaining);
            runtime = TireWearRuntime.Step(
                config,
                state,
                new TireWearInput(
                    dt,
                    speedMps,
                    slipAngleNormalized,
                    lateralSlipNormalized,
                    longitudinalSlipNormalized,
                    loadNormalized,
                    rollingResistanceNormalized,
                    ambientTemperatureC,
                    surfaceTemperatureC,
                    wetnessNormalized));
            state = runtime.State;
            remaining -= dt;
        }

        return runtime;
    }

    private static float Clamp01(float value)
    {
        if (value < 0f)
            return 0f;
        if (value > 1f)
            return 1f;
        return value;
    }
}
