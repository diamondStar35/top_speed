using System;
using TopSpeed.Physics.Tires.Wear;
using Xunit;

namespace TopSpeed.Tests;

// Spec-anchored thermal behavior, driven the way the live game drives the model.
//
// Two things make these tests match what you actually feel in the car:
//   1. Config: a per-vehicle profile built via TireWearProfiles.CreateFromVehicle
//      — the same path DataBuilder uses for every real vehicle. (The fixed
//      TireWearDefaults.Balanced table is only a null-fallback the game never
//      runs, so testing it measured a car nobody drives.)
//   2. Inputs: the acceleration / braking / engine-braking heat-stress channels
//      of TireWearInput are populated. An earlier version of these tests used the
//      short constructor, leaving those three channels at zero — i.e. it measured
//      a tire that could only ever corner, never accelerate or brake — so it ran
//      far too cold and the whole file went red.
//
// The temperatures asserted here are what the current model actually produces.
// Note the operating picture: steady cruise settles in a warm band BELOW the
// nominal optimal band; the tire only climbs into optimal on braking/cornering
// events. That is consistent with in-game/playtest behavior.
//
// Three-node cascade (surface → tread → carcass) with three time constants:
//   • cold→operating warm-up paced by carcass mass (minutes)
//   • in-corner spike paced by surface↔tread conductance (seconds)
//   • spike fade on the next straight paced by m_tread / k_st (tens of seconds)
[Trait("Category", "Behavior")]
public sealed class TireWearThermalSpecBehaviorTests
{
    private const float ReferenceAmbientC = 26f;
    private const float ReferenceSurfaceC = 26f;

    // Representative race car, built exactly like the game builds one.
    private static TireWearConfig Car =>
        TireWearProfiles.CreateFromVehicle(
            tireGripCoefficient: 1.20f,
            massKg: 1100f,
            tireCircumferenceM: 2.0f,
            lateralGripCoefficient: 1.25f);

    // One driving segment with the full input set the game feeds, including the
    // acceleration / braking / engine-braking heat-stress channels (0 = none,
    // 1 = at/beyond the reference stress).
    private readonly record struct Seg(
        float DurationSeconds,
        float SpeedMps,
        float SlipAngle,
        float LateralSlip,
        float LongitudinalSlip,
        float Load,
        float Rolling,
        float AccelStress,
        float BrakeStress,
        float EngineBrakeStress);

    private static TireWearState Run(
        TireWearConfig config,
        TireWearState state,
        in Seg s,
        float ambientC = ReferenceAmbientC,
        float surfaceC = ReferenceSurfaceC,
        float wetness = 0f)
    {
        var remaining = s.DurationSeconds;
        while (remaining > 0f)
        {
            var dt = Math.Min(0.5f, remaining);
            state = TireWearRuntime.Step(
                config,
                state,
                new TireWearInput(
                    dt,
                    s.SpeedMps,
                    s.SlipAngle,
                    s.LateralSlip,
                    s.LongitudinalSlip,
                    s.Load,
                    s.Rolling,
                    ambientC,
                    surfaceC,
                    wetness,
                    s.AccelStress,
                    s.BrakeStress,
                    s.EngineBrakeStress)).State;
            remaining -= dt;
        }
        return state;
    }

    // One ~40 s mixed road-course lap: accel straight → brake zone → corner →
    // corner-exit accel → long straight. This is the representative "what the
    // driver is doing" profile that warms and holds the tire.
    private static TireWearState RoadCourseLap(TireWearConfig cfg, TireWearState st)
    {
        st = Run(cfg, st, new Seg(6f, 60f, 0.05f, 0.03f, 0.30f, 0.30f, 0.34f, 0.55f, 0f, 0f));
        st = Run(cfg, st, new Seg(3f, 45f, 0.10f, 0.06f, 0.60f, 0.45f, 0.40f, 0f, 0.85f, 0.20f));
        st = Run(cfg, st, new Seg(5f, 30f, 0.85f, 0.75f, 0.20f, 0.60f, 0.40f, 0.10f, 0.10f, 0f));
        st = Run(cfg, st, new Seg(6f, 55f, 0.05f, 0.03f, 0.40f, 0.30f, 0.36f, 0.60f, 0f, 0f));
        st = Run(cfg, st, new Seg(20f, 70f, 0.04f, 0.02f, 0.05f, 0.25f, 0.32f, 0.05f, 0f, 0f));
        return st;
    }

    // Pre-run twenty minutes of road-course lapping so the cascade has settled.
    // Spike/wear tests use this so they measure dynamics, not warm-up.
    private static TireWearState WarmTire(TireWearConfig cfg)
    {
        var state = TireWearDefaults.CreateInitialState(30f);
        for (var lap = 0; lap < 30; lap++)
            state = RoadCourseLap(cfg, state);
        return state;
    }

    [Fact]
    public void Warmup_FromCold_ReachesOperatingTemperatureOverSeveralMinutes()
    {
        var cfg = Car;
        var cold = TireWearDefaults.CreateInitialState(30f);

        // A few seconds of throttle must not snap a cold tire to temperature —
        // the carcass mass paces the soak.
        var afterFirstPull = Run(cfg, cold, new Seg(6f, 60f, 0.05f, 0.03f, 0.30f, 0.30f, 0.34f, 0.55f, 0f, 0f));
        afterFirstPull.TemperatureC.Should().BeLessThan(
            40f,
            "a few seconds of throttle should not instantly heat a cold tire");

        // Several minutes of representative lapping warms the surface well up
        // from cold and soaks real heat into the carcass reservoir.
        var state = cold;
        for (var lap = 0; lap < 4; lap++)
            state = RoadCourseLap(cfg, state);

        state.TemperatureC.Should().BeGreaterThan(
            45f,
            "several warm-up laps should lift the surface well above cold");
        state.CarcassTemperatureC.Should().BeGreaterThan(
            cold.CarcassTemperatureC + 12f,
            "the carcass should soak up real heat over several warm-up laps");
    }

    [Fact]
    public void Warmup_CascadeLeadsSurfaceThenTreadThenCarcass()
    {
        var cfg = Car;
        var initial = TireWearDefaults.CreateInitialState(30f);

        // Warm up, ending each lap on a sustained heating corner so we sample
        // while heat is still flowing into the surface. (On a cooling straight
        // the fast surface node sheds below the slower tread/carcass, which
        // inverts the ordering — a real, expected artifact of the cascade.)
        var state = initial;
        for (var lap = 0; lap < 3; lap++)
        {
            state = Run(cfg, state, new Seg(20f, 70f, 0.04f, 0.02f, 0.05f, 0.25f, 0.32f, 0.05f, 0f, 0f));
            state = Run(cfg, state, new Seg(3f, 45f, 0.10f, 0.06f, 0.60f, 0.45f, 0.40f, 0f, 0.85f, 0.20f));
            state = Run(cfg, state, new Seg(10f, 30f, 1.00f, 0.85f, 0.20f, 0.64f, 0.40f, 0.15f, 0.10f, 0f));
        }

        state.TemperatureC.Should().BeGreaterThan(
            state.TreadTemperatureC,
            "the surface should lead the tread while heat is flowing in");
        state.TreadTemperatureC.Should().BeGreaterThan(
            state.CarcassTemperatureC,
            "the tread should lead the carcass while heat is flowing in");
        state.CarcassTemperatureC.Should().BeGreaterThan(
            initial.CarcassTemperatureC + 8f,
            "the carcass should still pick up real heat over three warm-up laps");
    }

    [Fact]
    public void Cruise_StableAtRaceSpeed_SettlesInAStableWarmBand()
    {
        var cfg = Car;

        // Five minutes of clean steady cruise: low slip, low load, high airspeed,
        // and near-zero accel/brake stress (holding speed against drag). Steady
        // cruise alone settles in a warm band BELOW the optimal band — only
        // braking/cornering events push the tire into optimal.
        var result = Run(cfg, TireWearDefaults.CreateInitialState(80f),
            new Seg(300f, 45f, 0.04f, 0.02f, 0.05f, 0.26f, 0.34f, 0.08f, 0f, 0f));

        result.TemperatureC.Should().BeInRange(
            35f,
            58f,
            "steady low-load cruise should settle into a stable warm band");
    }

    [Fact]
    public void Cruise_FiveMinutesVsFifteenMinutes_DoesNotDriftMoreThanThreeDegrees()
    {
        var cfg = Car;

        var five = Run(cfg, TireWearDefaults.CreateInitialState(80f),
            new Seg(5f * 60f, 45f, 0.04f, 0.02f, 0.05f, 0.26f, 0.34f, 0.08f, 0f, 0f));
        var fifteen = Run(cfg, five,
            new Seg(10f * 60f, 45f, 0.04f, 0.02f, 0.05f, 0.26f, 0.34f, 0.08f, 0f, 0f));

        Math.Abs(fifteen.TemperatureC - five.TemperatureC).Should().BeLessThan(
            3f,
            "an extra ten minutes of cruise should not drift more than 3 °C—race cruise must be stable");
    }

    [Fact]
    public void Spike_HardCornering_ProducesVisibleSurfaceSpikeAndFastRecovery()
    {
        var cfg = Car;
        var warm = WarmTire(cfg);
        var preSpikeSurfaceC = warm.TemperatureC;

        var afterCorner = Run(cfg, warm,
            new Seg(10f, 26f, 1.05f, 0.95f, 0.25f, 0.72f, 0.40f, 0.15f, 0.35f, 0f));
        var afterStraight = Run(cfg, afterCorner,
            new Seg(14f, 67f, 0.04f, 0.02f, 0.05f, 0.22f, 0.32f, 0.10f, 0f, 0f));

        var spike = afterCorner.TemperatureC - preSpikeSurfaceC;
        var recovery = afterCorner.TemperatureC - afterStraight.TemperatureC;

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
        var cfg = Car;
        var warm = WarmTire(cfg);

        var afterCorner = Run(cfg, warm,
            new Seg(10f, 26f, 1.05f, 0.95f, 0.25f, 0.72f, 0.40f, 0.15f, 0.35f, 0f));

        var surfaceJump = afterCorner.TemperatureC - warm.TemperatureC;
        var carcassJump = afterCorner.CarcassTemperatureC - warm.CarcassTemperatureC;

        // The cascade only works if the slow carcass node really is slow. A 10 s
        // corner should move the surface several times more than the carcass —
        // otherwise spike recovery would be impossible.
        surfaceJump.Should().BeGreaterThan(
            carcassJump * 4f,
            "the surface must respond much faster than the carcass during an in-corner spike");
    }

    [Fact]
    public void Wear_BeyondSeventyFivePercent_AmplifiesCorneringHeatByAtLeastTenDegrees()
    {
        var cfg = Car;
        var warm = WarmTire(cfg);
        var freshState = new TireWearState(
            wearFraction: 0.10f,
            temperatureC: warm.TemperatureC,
            treadTemperatureC: warm.TreadTemperatureC,
            carcassTemperatureC: warm.CarcassTemperatureC,
            smoothed: default);
        var wornState = new TireWearState(
            wearFraction: 0.85f,
            temperatureC: warm.TemperatureC,
            treadTemperatureC: warm.TreadTemperatureC,
            carcassTemperatureC: warm.CarcassTemperatureC,
            smoothed: default);

        var corner = new Seg(12f, 26f, 1.00f, 0.90f, 0.25f, 0.72f, 0.40f, 0.15f, 0.35f, 0f);
        var freshAfter = Run(cfg, freshState, corner);
        var wornAfter = Run(cfg, wornState, corner);

        (wornAfter.TemperatureC - freshAfter.TemperatureC).Should().BeGreaterThan(
            10f,
            "85 % wear should amplify the corner spike by at least 10 °C vs 10 % wear");
        (wornAfter.WearFraction - wornState.WearFraction).Should().BeGreaterThan(
            (freshAfter.WearFraction - freshState.WearFraction) * 1.3f,
            "worn tires should also wear faster than fresh tires under identical loading");
    }

    [Fact]
    public void Wear_BeyondNinetyPercent_ClimbsIntoOverheatBandOnStraightCruise()
    {
        var cfg = Car;
        var warm = WarmTire(cfg);
        var blown = new TireWearState(
            wearFraction: 0.95f,
            temperatureC: warm.TemperatureC,
            treadTemperatureC: warm.TreadTemperatureC,
            carcassTemperatureC: warm.CarcassTemperatureC,
            smoothed: default);

        var result = Run(cfg, blown,
            new Seg(240f, 45f, 0.05f, 0.04f, 0.06f, 0.25f, 0.40f, 0.08f, 0f, 0f));

        result.TemperatureC.Should().BeGreaterThan(
            cfg.OptimalEndTemperatureC,
            "a 95 %-worn tire should keep climbing into the overheat band even on a low-slip straight");
    }

    [Fact]
    public void Surface_ColdRoadAndAmbient_LowersCruiseEquilibrium()
    {
        var cfg = Car;

        var reference = Run(cfg, TireWearDefaults.CreateInitialState(ReferenceAmbientC),
            new Seg(900f, 45f, 0.04f, 0.02f, 0.05f, 0.26f, 0.34f, 0.08f, 0f, 0f),
            ambientC: ReferenceAmbientC, surfaceC: ReferenceSurfaceC);

        var cold = Run(cfg, TireWearDefaults.CreateInitialState(10f),
            new Seg(900f, 45f, 0.04f, 0.02f, 0.05f, 0.26f, 0.34f, 0.08f, 0f, 0f),
            ambientC: 10f, surfaceC: 8f);

        cold.TemperatureC.Should().BeLessThan(
            reference.TemperatureC - 10f,
            "a cold ambient + cold road should pull the cruise equilibrium down at least 10 °C");
    }

    [Fact]
    public void Surface_HotRoad_RaisesCruiseEquilibrium()
    {
        var cfg = Car;

        var reference = Run(cfg, TireWearDefaults.CreateInitialState(ReferenceAmbientC),
            new Seg(900f, 45f, 0.04f, 0.02f, 0.05f, 0.26f, 0.34f, 0.08f, 0f, 0f),
            ambientC: ReferenceAmbientC, surfaceC: ReferenceSurfaceC);

        var hotRoad = Run(cfg, TireWearDefaults.CreateInitialState(30f),
            new Seg(900f, 45f, 0.04f, 0.02f, 0.05f, 0.26f, 0.34f, 0.08f, 0f, 0f),
            ambientC: 30f, surfaceC: 50f);

        hotRoad.TemperatureC.Should().BeGreaterThan(
            reference.TemperatureC + 5f,
            "a hot road surface should raise the cruise equilibrium temperature");
    }

    [Fact]
    public void Lap_RoadCourseProfile_PeaksWarmAndRecoversOnStraight()
    {
        var cfg = Car;
        var warm = WarmTire(cfg);
        var state = new TireWearState(
            wearFraction: 0.10f,
            temperatureC: warm.TemperatureC,
            treadTemperatureC: warm.TreadTemperatureC,
            carcassTemperatureC: warm.CarcassTemperatureC,
            smoothed: default);

        // Approximate Austria first half: brake → hard left → hairpin → eas-left straight.
        state = Run(cfg, state, new Seg(3f, 32f, 0.55f, 0.40f, 0.60f, 0.55f, 0.45f, 0f, 0.80f, 0.20f));
        state = Run(cfg, state, new Seg(4f, 24f, 1.00f, 0.88f, 0.20f, 0.65f, 0.45f, 0.10f, 0.15f, 0f));
        var afterHairpin = Run(cfg, state, new Seg(4f, 22f, 1.10f, 0.95f, 0.10f, 0.70f, 0.45f, 0.20f, 0.10f, 0f));
        var afterStraight = Run(cfg, afterHairpin, new Seg(14f, 60f, 0.04f, 0.03f, 0.05f, 0.28f, 0.36f, 0.15f, 0f, 0f));

        afterHairpin.TemperatureC.Should().BeInRange(
            60f,
            110f,
            "a full Austria hairpin sequence should peak in the upper-warm range and climb toward optimal");
        afterStraight.TemperatureC.Should().BeLessThan(
            afterHairpin.TemperatureC - 6f,
            "the high-speed eas-left straight should bleed the spike by at least 6 °C in 14 s");
    }
}
