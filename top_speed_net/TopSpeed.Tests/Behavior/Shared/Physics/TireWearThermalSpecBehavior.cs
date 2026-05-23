using System;
using TopSpeed.Physics.Tires.Wear;
using Xunit;

namespace TopSpeed.Tests;

// Spec-anchored thermal behavior. Each test maps to a real driving scenario
// described in issue #84. The three-node cascade (surface → tread → carcass)
// lets each scenario set an independent time constant:
//   • cold-to-optimal warm-up is paced by the carcass mass (~200 s)
//   • in-corner spike rise is paced by the surface↔tread conductance (~4 s)
//   • spike fade on the next straight is paced by m_tread / k_st (~15 s)
[Trait("Category", "Behavior")]
public sealed class TireWearThermalSpecBehaviorTests
{
    // 26 °C ambient + road = the "79 °F road surface" reference from issue #84.
    private const float ReferenceAmbientC = 26f;
    private const float ReferenceSurfaceC = 26f;

    [Fact]
    public void Warmup_SuperspeedwayLap_ReachesOptimalBandInFiveToTenMiles()
    {
        var config = TireWearDefaults.Balanced;
        var state = TireWearDefaults.CreateInitialState(30f);
        var miles = RunSuperspeedwayLapsUntil(
            config,
            ref state,
            targetTemperatureC: config.OptimalStartTemperatureC,
            maxMiles: 12f);

        miles.Should().NotBeNull(
            "a banked-oval lap profile should reach the optimal band within 12 miles");
        miles!.Value.Should().BeLessThan(
            10f,
            "tires should reach optimal in less than 10 miles on a superspeedway");
        miles.Value.Should().BeGreaterThanOrEqualTo(
            5f,
            "warm-up should not snap into optimal—the carcass needs minutes to soak");
    }

    [Fact]
    public void Warmup_CascadeLagsCarcassBehindTread_AndTreadBehindSurface()
    {
        var config = TireWearDefaults.Balanced;
        var initial = TireWearDefaults.CreateInitialState(30f);

        // 90 s of representative superspeedway driving from cold.
        var state = initial;
        for (var lap = 0; lap < 3; lap++)
        {
            state = RunSuperspeedwayLap(config, state);
        }

        state.TemperatureC.Should().BeGreaterThan(
            state.TreadTemperatureC,
            "the surface should lead the tread while warming up");
        state.TreadTemperatureC.Should().BeGreaterThan(
            state.CarcassTemperatureC,
            "the tread should lead the carcass while warming up");
        state.CarcassTemperatureC.Should().BeGreaterThan(
            initial.CarcassTemperatureC + 6f,
            "the carcass should still pick up real heat over three warm-up laps");
    }

    [Fact]
    public void Cruise_StableAtRaceSpeed_DoesNotClimbOutOfOptimalBand()
    {
        var config = TireWearDefaults.Balanced;

        // Five minutes of clean steady cruise on a superspeedway: low slip,
        // low load, high airspeed. We expect the equilibrium to settle below
        // the middle of the optimal band so race-cruise inputs do not creep
        // toward the overheat threshold.
        var result = RunForDuration(
            config,
            TireWearDefaults.CreateInitialState(80f),
            durationSeconds: 300f,
            speedMps: 45f,
            slipAngleNormalized: 0.04f,
            lateralSlipNormalized: 0.02f,
            longitudinalSlipNormalized: 0.05f,
            loadNormalized: 0.26f,
            rollingResistanceNormalized: 0.34f);

        result.State.TemperatureC.Should().BeInRange(
            config.OptimalStartTemperatureC,
            config.OptimalEndTemperatureC,
            "steady race-cruise should stabilize inside the optimal band");
    }

    [Fact]
    public void Cruise_FiveMinutesVsFifteenMinutes_DoesNotDriftMoreThanThreeDegrees()
    {
        var config = TireWearDefaults.Balanced;

        var five = RunForDuration(
            config,
            TireWearDefaults.CreateInitialState(80f),
            durationSeconds: 5f * 60f,
            speedMps: 45f,
            slipAngleNormalized: 0.04f,
            lateralSlipNormalized: 0.02f,
            longitudinalSlipNormalized: 0.05f,
            loadNormalized: 0.26f,
            rollingResistanceNormalized: 0.34f);

        var fifteen = RunForDuration(
            config,
            five.State,
            durationSeconds: 10f * 60f,
            speedMps: 45f,
            slipAngleNormalized: 0.04f,
            lateralSlipNormalized: 0.02f,
            longitudinalSlipNormalized: 0.05f,
            loadNormalized: 0.26f,
            rollingResistanceNormalized: 0.34f);

        Math.Abs(fifteen.State.TemperatureC - five.State.TemperatureC).Should().BeLessThan(
            3f,
            "an extra ten minutes of cruise should not drift more than 3 °C—race cruise must be stable");
    }

    [Fact]
    public void Spike_HardCornering_ProducesVisibleSurfaceSpikeAndFastRecovery()
    {
        var config = TireWearDefaults.Balanced;
        var warm = WarmTire(config);
        var preSpikeSurfaceC = warm.TemperatureC;

        var afterCorner = RunForDuration(
            config,
            warm,
            durationSeconds: 10f,
            speedMps: 22f,
            slipAngleNormalized: 1.05f,
            lateralSlipNormalized: 0.95f,
            longitudinalSlipNormalized: 0.25f,
            loadNormalized: 0.72f,
            rollingResistanceNormalized: 0.40f);

        var afterStraight = RunForDuration(
            config,
            afterCorner.State,
            durationSeconds: 14f,
            speedMps: 67f,
            slipAngleNormalized: 0.04f,
            lateralSlipNormalized: 0.02f,
            longitudinalSlipNormalized: 0.05f,
            loadNormalized: 0.22f,
            rollingResistanceNormalized: 0.32f);

        var spike = afterCorner.State.TemperatureC - preSpikeSurfaceC;
        var recovery = afterCorner.State.TemperatureC - afterStraight.State.TemperatureC;

        spike.Should().BeGreaterThan(
            15f,
            "ten seconds of hard cornering should push the surface clearly above cruise");
        recovery.Should().BeGreaterThan(
            8f,
            "a 14 s high-speed straight should bleed the spike by at least 8 °C");
        recovery.Should().BeLessThan(
            spike + 2f,
            "the straight should not fully erase the spike—the tread reservoir keeps a floor");
    }

    [Fact]
    public void Spike_CarcassBarelyMovesDuringCornerSpike()
    {
        var config = TireWearDefaults.Balanced;
        var warm = WarmTire(config);

        var afterCorner = RunForDuration(
            config,
            warm,
            durationSeconds: 10f,
            speedMps: 22f,
            slipAngleNormalized: 1.05f,
            lateralSlipNormalized: 0.95f,
            longitudinalSlipNormalized: 0.25f,
            loadNormalized: 0.72f,
            rollingResistanceNormalized: 0.40f);

        var surfaceJump = afterCorner.State.TemperatureC - warm.TemperatureC;
        var carcassJump = afterCorner.State.CarcassTemperatureC - warm.CarcassTemperatureC;

        // The cascade only works if the slow carcass node really is slow.
        // A 10 s corner should move the surface several times more than the
        // carcass — otherwise spike recovery would be impossible (carcass
        // would hold the surface elevated).
        surfaceJump.Should().BeGreaterThan(
            carcassJump * 4f,
            "the surface must respond much faster than the carcass during an in-corner spike");
    }

    [Fact]
    public void Wear_BeyondSeventyFivePercent_AmplifiesCorneringHeatByAtLeastTenDegrees()
    {
        var config = TireWearDefaults.Balanced;
        var fresh = WarmTire(config);
        var freshState = new TireWearState(
            wearFraction: 0.10f,
            temperatureC: fresh.TemperatureC,
            treadTemperatureC: fresh.TreadTemperatureC,
            carcassTemperatureC: fresh.CarcassTemperatureC,
            smoothedSlipNormalized: 0.30f);
        var wornState = new TireWearState(
            wearFraction: 0.85f,
            temperatureC: fresh.TemperatureC,
            treadTemperatureC: fresh.TreadTemperatureC,
            carcassTemperatureC: fresh.CarcassTemperatureC,
            smoothedSlipNormalized: 0.30f);

        var freshAfter = RunForDuration(
            config,
            freshState,
            durationSeconds: 12f,
            speedMps: 26f,
            slipAngleNormalized: 1.00f,
            lateralSlipNormalized: 0.90f,
            longitudinalSlipNormalized: 0.25f,
            loadNormalized: 0.72f,
            rollingResistanceNormalized: 0.40f);

        var wornAfter = RunForDuration(
            config,
            wornState,
            durationSeconds: 12f,
            speedMps: 26f,
            slipAngleNormalized: 1.00f,
            lateralSlipNormalized: 0.90f,
            longitudinalSlipNormalized: 0.25f,
            loadNormalized: 0.72f,
            rollingResistanceNormalized: 0.40f);

        (wornAfter.State.TemperatureC - freshAfter.State.TemperatureC).Should().BeGreaterThan(
            10f,
            "85 % wear should amplify the corner spike by at least 10 °C vs 10 % wear");
        (wornAfter.State.WearFraction - wornState.WearFraction).Should().BeGreaterThan(
            (freshAfter.State.WearFraction - freshState.WearFraction) * 1.3f,
            "worn tires should also wear faster than fresh tires under identical loading");
    }

    [Fact]
    public void Wear_BeyondNinetyPercent_WarmsTireOnStraightCruise()
    {
        var config = TireWearDefaults.Balanced;
        var warm = WarmTire(config);
        var blown = new TireWearState(
            wearFraction: 0.95f,
            temperatureC: warm.TemperatureC,
            treadTemperatureC: warm.TreadTemperatureC,
            carcassTemperatureC: warm.CarcassTemperatureC,
            smoothedSlipNormalized: 0.10f);

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

        result.State.TemperatureC.Should().BeGreaterThan(
            config.OverheatEndTemperatureC,
            "a 95 %-worn tire should keep climbing into the overheat band even on a low-slip straight");
    }

    [Fact]
    public void Surface_ColdRoadAndAmbient_LowersCruiseEquilibrium()
    {
        var config = TireWearDefaults.Balanced;

        var reference = RunForDuration(
            config,
            TireWearDefaults.CreateInitialState(ReferenceAmbientC),
            durationSeconds: 900f,
            speedMps: 45f,
            slipAngleNormalized: 0.04f,
            lateralSlipNormalized: 0.02f,
            longitudinalSlipNormalized: 0.05f,
            loadNormalized: 0.26f,
            rollingResistanceNormalized: 0.34f,
            ambientTemperatureC: ReferenceAmbientC,
            surfaceTemperatureC: ReferenceSurfaceC);

        var cold = RunForDuration(
            config,
            TireWearDefaults.CreateInitialState(10f),
            durationSeconds: 900f,
            speedMps: 45f,
            slipAngleNormalized: 0.04f,
            lateralSlipNormalized: 0.02f,
            longitudinalSlipNormalized: 0.05f,
            loadNormalized: 0.26f,
            rollingResistanceNormalized: 0.34f,
            ambientTemperatureC: 10f,
            surfaceTemperatureC: 8f);

        cold.State.TemperatureC.Should().BeLessThan(
            reference.State.TemperatureC - 10f,
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
            speedMps: 45f,
            slipAngleNormalized: 0.04f,
            lateralSlipNormalized: 0.02f,
            longitudinalSlipNormalized: 0.05f,
            loadNormalized: 0.26f,
            rollingResistanceNormalized: 0.34f,
            ambientTemperatureC: ReferenceAmbientC,
            surfaceTemperatureC: ReferenceSurfaceC);

        var hotRoad = RunForDuration(
            config,
            TireWearDefaults.CreateInitialState(30f),
            durationSeconds: 900f,
            speedMps: 45f,
            slipAngleNormalized: 0.04f,
            lateralSlipNormalized: 0.02f,
            longitudinalSlipNormalized: 0.05f,
            loadNormalized: 0.26f,
            rollingResistanceNormalized: 0.34f,
            ambientTemperatureC: 30f,
            surfaceTemperatureC: 50f);

        hotRoad.State.TemperatureC.Should().BeGreaterThan(
            reference.State.TemperatureC + 5f,
            "a hot road surface should raise the cruise equilibrium temperature");
    }

    [Fact]
    public void Lap_RoadCourseProfile_PeaksInsideExpectedBandAndRecoversOnStraight()
    {
        var config = TireWearDefaults.Balanced;
        var warm = WarmTire(config);
        var state = new TireWearState(
            wearFraction: 0.10f,
            temperatureC: warm.TemperatureC,
            treadTemperatureC: warm.TreadTemperatureC,
            carcassTemperatureC: warm.CarcassTemperatureC,
            smoothedSlipNormalized: 0.20f);

        // Approximate Austria first half: brake → hard left → hairpin → eas-left straight.
        state = StepSegment(config, state, durationSeconds: 3f, speedMps: 32f, slipAngle: 0.55f, lateralSlip: 0.40f, longitudinalSlip: 0.60f, load: 0.55f, rolling: 0.45f).State;
        state = StepSegment(config, state, durationSeconds: 4f, speedMps: 24f, slipAngle: 1.00f, lateralSlip: 0.88f, longitudinalSlip: 0.20f, load: 0.65f, rolling: 0.45f).State;
        var afterHairpin = StepSegment(config, state, durationSeconds: 4f, speedMps: 22f, slipAngle: 1.10f, lateralSlip: 0.95f, longitudinalSlip: 0.10f, load: 0.70f, rolling: 0.45f);
        var afterStraight = StepSegment(config, afterHairpin.State, durationSeconds: 14f, speedMps: 60f, slipAngle: 0.04f, lateralSlip: 0.03f, longitudinalSlip: 0.05f, load: 0.28f, rolling: 0.36f);

        afterHairpin.State.TemperatureC.Should().BeInRange(
            config.OptimalStartTemperatureC + 18f,
            config.OverheatEndTemperatureC,
            "a full Austria hairpin sequence should peak in the upper optimal band, not overheat");
        afterStraight.State.TemperatureC.Should().BeLessThan(
            afterHairpin.State.TemperatureC - 6f,
            "the high-speed eas-left straight should bleed the spike by at least 6 °C in 14 s");
    }

    private static TireWearState WarmTire(TireWearConfig config)
    {
        // Pre-run twenty minutes of representative road-course cruise so the
        // cascade has settled. Spike/recovery tests use this so they measure
        // dynamics, not warm-up.
        var state = TireWearDefaults.CreateInitialState(ReferenceAmbientC + 4f);
        var result = RunForDuration(
            config,
            state,
            durationSeconds: 60f * 20f,
            speedMps: 40f,
            slipAngleNormalized: 0.05f,
            lateralSlipNormalized: 0.03f,
            longitudinalSlipNormalized: 0.05f,
            loadNormalized: 0.30f,
            rollingResistanceNormalized: 0.34f);
        return result.State;
    }

    // Banked-oval lap: alternating ~8 s turns + ~11 s straights at ~150 mph avg.
    // 1.5 mi per lap; matches the Python simulator in /home/ubuntu/tire_sim/sim.py.
    private static TireWearState RunSuperspeedwayLap(TireWearConfig config, TireWearState state)
    {
        state = StepSegment(config, state, 8f, 68f, 0.55f, 0.45f, 0.06f, 0.48f, 0.34f).State;
        state = StepSegment(config, state, 11f, 78f, 0.05f, 0.03f, 0.04f, 0.26f, 0.32f).State;
        state = StepSegment(config, state, 8f, 68f, 0.55f, 0.45f, 0.06f, 0.48f, 0.34f).State;
        state = StepSegment(config, state, 11f, 78f, 0.05f, 0.03f, 0.04f, 0.26f, 0.32f).State;
        return state;
    }

    private static float? RunSuperspeedwayLapsUntil(
        TireWearConfig config,
        ref TireWearState state,
        float targetTemperatureC,
        float maxMiles)
    {
        const float lapMiles = 1.5f;
        var miles = 0f;
        while (miles < maxMiles)
        {
            // Walk one lap as four segments. We need substep granularity to
            // detect the moment the surface crosses the threshold.
            foreach (var segment in SuperspeedwaySegments())
            {
                var (duration, speedMps, slipAngle, lateralSlip, longitudinalSlip, load, rolling) = segment;
                var elapsed = 0f;
                while (elapsed < duration)
                {
                    var dt = Math.Min(0.5f, duration - elapsed);
                    var result = TireWearRuntime.Step(
                        config,
                        state,
                        new TireWearInput(
                            dt,
                            speedMps,
                            slipAngle,
                            lateralSlip,
                            longitudinalSlip,
                            load,
                            rolling,
                            ReferenceAmbientC,
                            ReferenceSurfaceC,
                            wetnessNormalized: 0f));
                    state = result.State;
                    elapsed += dt;
                    miles += (speedMps * dt) / 1609.344f;
                    if (state.TemperatureC >= targetTemperatureC)
                        return miles;
                }
            }

            // Guard against an infinite loop if the lap definition stops
            // advancing distance (shouldn't happen but defends the test).
            if (miles < lapMiles * 0.5f)
                return null;
        }
        return null;
    }

    private static System.Collections.Generic.IEnumerable<(float duration, float speedMps, float slipAngle, float lateralSlip, float longitudinalSlip, float load, float rolling)> SuperspeedwaySegments()
    {
        yield return (8f, 68f, 0.55f, 0.45f, 0.06f, 0.48f, 0.34f);
        yield return (11f, 78f, 0.05f, 0.03f, 0.04f, 0.26f, 0.32f);
        yield return (8f, 68f, 0.55f, 0.45f, 0.06f, 0.48f, 0.34f);
        yield return (11f, 78f, 0.05f, 0.03f, 0.04f, 0.26f, 0.32f);
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
}
