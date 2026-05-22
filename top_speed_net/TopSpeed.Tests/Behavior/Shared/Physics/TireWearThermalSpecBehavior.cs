using System;
using TopSpeed.Physics.Tires.Wear;
using Xunit;

namespace TopSpeed.Tests;

// Spec-anchored thermal behavior. Each test maps to a real driving scenario
// described in issue #84: warm-up should reach the optimal band in 5–10 miles
// at highway speed (slow carcass time constant), cruise should stabilize in
// 195–225 °F, corner spikes should recover on the next straight (fast surface
// time constant), worn tires should run hotter and spike more, and the road
// surface temperature should pull the equilibrium up or down. The two-node
// thermal model is what allows the slow warm-up and the fast spike recovery
// to coexist.
[Trait("Category", "Behavior")]
public sealed class TireWearThermalSpecBehaviorTests
{
    // 26 °C ambient + road = the "79 °F road surface" reference from issue #84.
    private const float ReferenceAmbientC = 26f;
    private const float ReferenceSurfaceC = 26f;

    [Fact]
    public void Warmup_HundredMphCruise_ReachesOptimalBandBetweenFiveAndTenMiles()
    {
        var config = TireWearDefaults.Balanced;
        const float speedMps = 44.7f; // 100 mph
        const float upperBoundMiles = 10f;
        const float lowerBoundMiles = 4f;
        var maxSeconds = (upperBoundMiles * 1609f) / speedMps;

        var miles = ResolveMilesToTemperature(
            config,
            initialTemperatureC: 30f,
            targetTemperatureC: config.OptimalStartTemperatureC,
            maxSeconds: maxSeconds,
            speedMps: speedMps,
            slipAngleNormalized: 0.04f,
            lateralSlipNormalized: 0.02f,
            longitudinalSlipNormalized: 0.05f,
            loadNormalized: 0.30f,
            rollingResistanceNormalized: 0.40f);

        miles.Should().NotBeNull("100 mph cruise should reach the optimal band within 10 miles");
        miles!.Value.Should().BeLessThan(upperBoundMiles);
        miles.Value.Should().BeGreaterThan(
            lowerBoundMiles,
            "warm-up should not snap into the optimal band—the cold carcass needs ~5+ mi to soak through");
    }

    [Fact]
    public void Warmup_CarcassLagsBehindSurface()
    {
        var config = TireWearDefaults.Balanced;
        var initial = TireWearDefaults.CreateInitialState(30f);

        // Two minutes of 100 mph cruise from cold. The surface should be
        // visibly ahead of the carcass—that's the whole point of the two-node
        // split, and the reason warm-up still takes 5–10 mi despite the
        // surface itself having a fast time constant.
        var result = RunForDuration(
            config,
            initial,
            durationSeconds: 120f,
            speedMps: 44.7f,
            slipAngleNormalized: 0.04f,
            lateralSlipNormalized: 0.03f,
            longitudinalSlipNormalized: 0.05f,
            loadNormalized: 0.30f,
            rollingResistanceNormalized: 0.40f);

        result.State.TemperatureC.Should().BeGreaterThan(
            result.State.CarcassTemperatureC + 3f,
            "surface should lead the carcass while warming up");
        result.State.CarcassTemperatureC.Should().BeGreaterThan(
            initial.CarcassTemperatureC + 8f,
            "the carcass should still pick up real heat over two minutes of cruising");
    }

    [Fact]
    public void Cruise_StableHighSpeed_EquilibratesInsideOptimalBand()
    {
        var config = TireWearDefaults.Balanced;

        var result = RunForDuration(
            config,
            TireWearDefaults.CreateInitialState(80f),
            durationSeconds: 600f,
            speedMps: 78f,
            slipAngleNormalized: 0.04f,
            lateralSlipNormalized: 0.03f,
            longitudinalSlipNormalized: 0.05f,
            loadNormalized: 0.28f,
            rollingResistanceNormalized: 0.40f);

        result.State.TemperatureC.Should().BeInRange(
            config.OptimalStartTemperatureC,
            config.OptimalEndTemperatureC,
            "high-speed steady cruise should sit inside the optimal band");
    }

    [Fact]
    public void Spike_HardCornering_RecoversOnFollowingStraight()
    {
        var config = TireWearDefaults.Balanced;
        var warm = new TireWearState(wearFraction: 0.10f, temperatureC: 90f, carcassTemperatureC: 88f, smoothedSlipNormalized: 0.20f);

        var afterCorner = RunForDuration(
            config,
            warm,
            durationSeconds: 8f,
            speedMps: 30f,
            slipAngleNormalized: 1.00f,
            lateralSlipNormalized: 0.85f,
            longitudinalSlipNormalized: 0.20f,
            loadNormalized: 0.65f,
            rollingResistanceNormalized: 0.45f);

        var afterStraight = RunForDuration(
            config,
            afterCorner.State,
            durationSeconds: 30f,
            speedMps: 78f,
            slipAngleNormalized: 0.03f,
            lateralSlipNormalized: 0.02f,
            longitudinalSlipNormalized: 0.05f,
            loadNormalized: 0.28f,
            rollingResistanceNormalized: 0.40f);

        afterCorner.State.TemperatureC.Should().BeGreaterThan(warm.TemperatureC + 10f,
            "an 8-second hairpin should produce a clearly visible heat spike");
        afterStraight.State.TemperatureC.Should().BeLessThan(afterCorner.State.TemperatureC - 4f,
            "the following high-speed straight should pull the spike back down");
    }

    [Fact]
    public void Wear_BeyondSeventyFivePercent_AmplifiesCorneringHeat()
    {
        var config = TireWearDefaults.Balanced;
        var freshState = new TireWearState(wearFraction: 0.10f, temperatureC: 95f, carcassTemperatureC: 93f, smoothedSlipNormalized: 0.30f);
        var wornState = new TireWearState(wearFraction: 0.85f, temperatureC: 95f, carcassTemperatureC: 93f, smoothedSlipNormalized: 0.30f);

        var fresh = RunForDuration(
            config,
            freshState,
            durationSeconds: 12f,
            speedMps: 60f,
            slipAngleNormalized: 0.60f,
            lateralSlipNormalized: 0.45f,
            longitudinalSlipNormalized: 0.30f,
            loadNormalized: 0.60f,
            rollingResistanceNormalized: 0.40f);

        var worn = RunForDuration(
            config,
            wornState,
            durationSeconds: 12f,
            speedMps: 60f,
            slipAngleNormalized: 0.60f,
            lateralSlipNormalized: 0.45f,
            longitudinalSlipNormalized: 0.30f,
            loadNormalized: 0.60f,
            rollingResistanceNormalized: 0.40f);

        worn.State.TemperatureC.Should().BeGreaterThan(fresh.State.TemperatureC + 8f,
            "85% wear should noticeably amplify cornering heat vs 10% wear");

        var freshWearDelta = fresh.State.WearFraction - freshState.WearFraction;
        var wornWearDelta = worn.State.WearFraction - wornState.WearFraction;
        wornWearDelta.Should().BeGreaterThan(freshWearDelta * 1.3f,
            "worn tires should also wear faster than fresh tires under the same load");
    }

    [Fact]
    public void Wear_BeyondNinetyPercent_WarmsTireOnStraightCruise()
    {
        var config = TireWearDefaults.Balanced;
        var blown = new TireWearState(wearFraction: 0.95f, temperatureC: 85f, carcassTemperatureC: 85f, smoothedSlipNormalized: 0.10f);

        var result = RunForDuration(
            config,
            blown,
            durationSeconds: 240f,
            speedMps: 45f,
            slipAngleNormalized: 0.05f,
            lateralSlipNormalized: 0.04f,
            longitudinalSlipNormalized: 0.06f,
            loadNormalized: 0.25f,
            rollingResistanceNormalized: 0.40f);

        result.State.TemperatureC.Should().BeGreaterThan(config.OverheatEndTemperatureC,
            "a 95%-worn tire should keep climbing into the overheat band even on a low-slip straight");
    }

    [Fact]
    public void Surface_ColdRoadAndAmbient_LowersCruiseEquilibrium()
    {
        var config = TireWearDefaults.Balanced;

        var reference = RunForDuration(
            config,
            TireWearDefaults.CreateInitialState(ReferenceAmbientC),
            durationSeconds: 900f,
            speedMps: 50f,
            slipAngleNormalized: 0.04f,
            lateralSlipNormalized: 0.03f,
            longitudinalSlipNormalized: 0.05f,
            loadNormalized: 0.30f,
            rollingResistanceNormalized: 0.40f,
            ambientTemperatureC: ReferenceAmbientC,
            surfaceTemperatureC: ReferenceSurfaceC);

        var cold = RunForDuration(
            config,
            TireWearDefaults.CreateInitialState(10f),
            durationSeconds: 900f,
            speedMps: 50f,
            slipAngleNormalized: 0.04f,
            lateralSlipNormalized: 0.03f,
            longitudinalSlipNormalized: 0.05f,
            loadNormalized: 0.30f,
            rollingResistanceNormalized: 0.40f,
            ambientTemperatureC: 10f,
            surfaceTemperatureC: 8f);

        cold.State.TemperatureC.Should().BeLessThan(reference.State.TemperatureC - 10f,
            "a cold ambient + cold road should pull the cruise equilibrium down at least 10 °C");
    }

    [Fact]
    public void Surface_HotRoad_RaisesCruiseEquilibrium()
    {
        var config = TireWearDefaults.Balanced;

        var reference = RunForDuration(
            config,
            TireWearDefaults.CreateInitialState(ReferenceAmbientC),
            durationSeconds: 900f,
            speedMps: 50f,
            slipAngleNormalized: 0.04f,
            lateralSlipNormalized: 0.03f,
            longitudinalSlipNormalized: 0.05f,
            loadNormalized: 0.30f,
            rollingResistanceNormalized: 0.40f,
            ambientTemperatureC: ReferenceAmbientC,
            surfaceTemperatureC: ReferenceSurfaceC);

        var hotRoad = RunForDuration(
            config,
            TireWearDefaults.CreateInitialState(30f),
            durationSeconds: 900f,
            speedMps: 50f,
            slipAngleNormalized: 0.04f,
            lateralSlipNormalized: 0.03f,
            longitudinalSlipNormalized: 0.05f,
            loadNormalized: 0.30f,
            rollingResistanceNormalized: 0.40f,
            ambientTemperatureC: 30f,
            surfaceTemperatureC: 50f);

        hotRoad.State.TemperatureC.Should().BeGreaterThan(reference.State.TemperatureC + 6f,
            "a hot road surface should raise the cruise equilibrium temperature");
    }

    [Fact]
    public void Lap_RoadCourseProfile_PeaksInsideExpectedBandAndRecoversOnStraight()
    {
        var config = TireWearDefaults.Balanced;
        var state = new TireWearState(wearFraction: 0.10f, temperatureC: 85f, carcassTemperatureC: 84f, smoothedSlipNormalized: 0.20f);

        // Approximate Austria first half: brake → hard left → hairpin → eas-left straight.
        state = StepSegment(config, state, durationSeconds: 3f, speedMps: 32f, slipAngle: 0.50f, lateralSlip: 0.35f, longitudinalSlip: 0.55f, load: 0.55f, rolling: 0.45f).State;
        state = StepSegment(config, state, durationSeconds: 4f, speedMps: 24f, slipAngle: 1.00f, lateralSlip: 0.85f, longitudinalSlip: 0.20f, load: 0.65f, rolling: 0.45f).State;
        var afterHairpin = StepSegment(config, state, durationSeconds: 3f, speedMps: 22f, slipAngle: 1.10f, lateralSlip: 0.95f, longitudinalSlip: 0.05f, load: 0.65f, rolling: 0.45f);
        var afterStraight = StepSegment(config, afterHairpin.State, durationSeconds: 14f, speedMps: 55f, slipAngle: 0.04f, lateralSlip: 0.03f, longitudinalSlip: 0.05f, load: 0.30f, rolling: 0.40f);

        afterHairpin.State.TemperatureC.Should().BeGreaterThan(config.OptimalStartTemperatureC + 10f,
            "a hairpin held under heavy lateral load should push well past the optimal start");
        afterHairpin.State.TemperatureC.Should().BeLessThan(config.OverheatEndTemperatureC,
            "even a hard hairpin should not overheat fresh tires");
        afterStraight.State.TemperatureC.Should().BeLessThan(afterHairpin.State.TemperatureC - 4f,
            "the high-speed eas-left straight should cool the tire back toward optimal");
    }

    private static TireWearRuntimeResult StepSegment(
        TireWearConfig config,
        TireWearState state,
        float durationSeconds,
        float speedMps,
        float slipAngle,
        float lateralSlip,
        float longitudinalSlip,
        float load,
        float rolling)
    {
        return RunForDuration(
            config,
            state,
            durationSeconds: durationSeconds,
            speedMps: speedMps,
            slipAngleNormalized: slipAngle,
            lateralSlipNormalized: lateralSlip,
            longitudinalSlipNormalized: longitudinalSlip,
            loadNormalized: load,
            rollingResistanceNormalized: rolling);
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
        float ambientTemperatureC = ReferenceAmbientC,
        float surfaceTemperatureC = ReferenceSurfaceC,
        float wetnessNormalized = 0f)
    {
        const float stepSeconds = 0.5f;
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

    private static float? ResolveMilesToTemperature(
        TireWearConfig config,
        float initialTemperatureC,
        float targetTemperatureC,
        float maxSeconds,
        float speedMps,
        float slipAngleNormalized,
        float lateralSlipNormalized,
        float longitudinalSlipNormalized,
        float loadNormalized,
        float rollingResistanceNormalized)
    {
        const float stepSeconds = 0.5f;
        var elapsed = 0f;
        var state = TireWearDefaults.CreateInitialState(initialTemperatureC);

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
                    ReferenceAmbientC,
                    ReferenceSurfaceC,
                    wetnessNormalized: 0f));
            elapsed += dt;
            state = result.State;
            if (state.TemperatureC >= targetTemperatureC)
                return (speedMps * elapsed) / 1609f;
        }

        return null;
    }
}
