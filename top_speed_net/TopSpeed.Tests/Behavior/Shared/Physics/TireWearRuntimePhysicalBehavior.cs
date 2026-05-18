using System;
using TopSpeed.Physics.Tires.Wear;
using TopSpeed.Vehicles;
using Xunit;

namespace TopSpeed.Tests;

[Trait("Category", "Behavior")]
public sealed class TireWearRuntimePhysicalBehaviorTests
{
    [Fact]
    public void Vehicle1_AggressiveStraight_ShouldReach90CWithinReasonableTime()
    {
        var config = OfficialVehicleCatalog.Get(0).TireWearConfig;
        var initialState = TireWearDefaults.CreateInitialState(26f);
        var secondsToNinety = ResolveSecondsToTemperature(
            config,
            initialState,
            targetTemperatureC: 90f,
            maxSeconds: 1800f,
            speedMps: 44f,
            slipAngleNormalized: 0.03f,
            lateralSlipNormalized: 0.02f,
            longitudinalSlipNormalized: 0.30f,
            loadNormalized: 0.34f,
            rollingResistanceNormalized: 0.40f,
            ambientTemperatureC: 26f,
            surfaceTemperatureC: 33f,
            wetnessNormalized: 0f);

        secondsToNinety.Should().NotBeNull();
        secondsToNinety!.Value.Should().BeInRange(80f, 420f);
    }

    [Fact]
    public void OfficialProfiles_AggressiveStraight_ShouldReachOwnOptimalStart()
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

            result.State.TemperatureC.Should().BeGreaterThanOrEqualTo(
                config.OptimalStartTemperatureC - 1f,
                $"{spec.Name} should reach its own optimal start under sustained aggressive straight driving");
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

        shortStraight.State.TemperatureC.Should().BeGreaterThan(heated.State.TemperatureC - 12f);
        shortStraight.State.TemperatureC.Should().BeGreaterThan(config.OptimalStartTemperatureC - 2f);
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

        result.State.TemperatureC.Should().BeGreaterThan(config.ColdEndTemperatureC + 10f);
        result.State.TemperatureC.Should().BeLessThan(config.OverheatEndTemperatureC);
    }

    [Fact]
    public void Vehicle1_LowSlipStraight_ShouldReach50CQuicklyButNotReach90C()
    {
        var config = OfficialVehicleCatalog.Get(0).TireWearConfig;
        var secondsToFifty = ResolveSecondsToTemperature(
            config,
            TireWearDefaults.CreateInitialState(26f),
            targetTemperatureC: 50f,
            maxSeconds: 300f,
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

        secondsToFifty.Should().NotBeNull();
        secondsToFifty!.Value.Should().BeLessThan(180f);
        secondsToNinety.Should().BeNull();
    }

    [Fact]
    public void Vehicle1_AggressiveStraight_ShouldReach90CFasterThanLowSlipStraight()
    {
        var config = OfficialVehicleCatalog.Get(0).TireWearConfig;
        var aggressiveSeconds = ResolveSecondsToTemperature(
            config,
            TireWearDefaults.CreateInitialState(26f),
            targetTemperatureC: 90f,
            maxSeconds: 1800f,
            speedMps: 44f,
            slipAngleNormalized: 0.03f,
            lateralSlipNormalized: 0.02f,
            longitudinalSlipNormalized: 0.30f,
            loadNormalized: 0.34f,
            rollingResistanceNormalized: 0.40f,
            ambientTemperatureC: 26f,
            surfaceTemperatureC: 33f,
            wetnessNormalized: 0f);
        var lowSlipSeconds = ResolveSecondsToTemperature(
            config,
            TireWearDefaults.CreateInitialState(26f),
            targetTemperatureC: 90f,
            maxSeconds: 1800f,
            speedMps: 38f,
            slipAngleNormalized: 0.01f,
            lateralSlipNormalized: 0.01f,
            longitudinalSlipNormalized: 0.03f,
            loadNormalized: 0.16f,
            rollingResistanceNormalized: 0.28f,
            ambientTemperatureC: 26f,
            surfaceTemperatureC: 33f,
            wetnessNormalized: 0f);

        aggressiveSeconds.Should().NotBeNull();
        aggressiveSeconds!.Value.Should().BeLessThan(1200f);
        if (lowSlipSeconds.HasValue)
            lowSlipSeconds.Value.Should().BeGreaterThan(aggressiveSeconds.Value * 1.35f);
    }

    [Fact]
    public void OfficialProfiles_LowSlipStraight_ShouldLeaveColdBandWithinReasonableTime()
    {
        foreach (var spec in OfficialVehicleCatalog.Vehicles)
        {
            var config = spec.TireWearConfig;
            var target = config.ColdEndTemperatureC + 8f;
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
            seconds!.Value.Should().BeLessThan(900f, $"{spec.Name} warm-up should not take excessively long in dry warm weather");
        }
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
}
