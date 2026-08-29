using System;
using System.Collections.Generic;
using TopSpeed.Vehicles;
using Xunit;

namespace TopSpeed.Tests;

[Trait("Category", "Behavior")]
public sealed class EngineShutdownBehaviorTests
{
    [Fact]
    public void CombustionOff_DisengagedFromDriveline_ShouldNotBeReseededFromWheelSpeed()
    {
        var engine = EngineHarness.BuildEngine();
        engine.StopEngine();

        engine.SyncFromSpeed(
            speedGameUnits: 90f,
            gear: 3,
            elapsed: 0.05f,
            throttleInput: 0,
            inReverse: false,
            couplingMode: EngineCouplingMode.Disengaged,
            couplingFactor: 0f,
            minimumCoupledRpm: 0f,
            combustionEnabled: false);

        engine.Rpm.Should().BeApproximately(0f, 0.0001f);
    }

    [Fact]
    public void CombustionOff_LockedToDriveline_ShouldStillBeBackDrivenFromWheelSpeed()
    {
        var engine = EngineHarness.BuildEngine();
        engine.StopEngine();

        engine.SyncFromSpeed(
            speedGameUnits: 90f,
            gear: 3,
            elapsed: 0.05f,
            throttleInput: 0,
            inReverse: false,
            couplingMode: EngineCouplingMode.Locked,
            couplingFactor: 1f,
            minimumCoupledRpm: 0f,
            combustionEnabled: false);

        engine.Rpm.Should().BeGreaterThan(engine.StallRpm);
    }

    [Fact]
    public void CombustionOff_ShouldWindTheEngineDownToAStop()
    {
        // The sequence every finishing car runs. Cutting the engine loop outright is what made a
        // finishing rival drop from a full racing note to silence in one frame; keeping the engine
        // synced with combustion off lets it die on its own friction and inertia instead.
        var engine = EngineHarness.BuildEngine();
        engine.SyncFromSpeed(200f, 4, 0.1f, throttleInput: 100);
        engine.Rpm.Should().BeGreaterThan(1000f);

        var seconds = WindDown(engine, rollingCarFrom: 200f, out var pitches);

        engine.Rpm.Should().Be(0f, "the engine must actually die rather than idle forever");
        seconds.Should().BeGreaterThan(1f, "an instant cut is the bug this replaces - it has to be audible");
        pitches[pitches.Count - 1].Should().BeLessThan(pitches[0], "the note has to fall as it dies");
    }

    [Fact]
    public void CombustionOn_ShouldHoldTheEngineAtIdle()
    {
        var engine = EngineHarness.BuildEngine();
        engine.SyncFromSpeed(200f, 4, 0.1f, throttleInput: 100);

        for (var i = 0; i < 2000; i++)
            engine.SyncFromSpeed(0f, 4, 0.016f, throttleInput: 0, combustionEnabled: true);

        engine.Rpm.Should().BeGreaterThan(1f, "a running engine idles; only a shutdown takes it to zero");
    }

    [Fact]
    public void WindDown_ShouldFallSmoothlyRatherThanJump()
    {
        var engine = EngineHarness.BuildEngine();
        engine.SyncFromSpeed(200f, 4, 0.1f, throttleInput: 100);

        WindDown(engine, rollingCarFrom: 200f, out var pitches);

        for (var i = 1; i < pitches.Count; i++)
            pitches[i].Should().BeLessThanOrEqualTo(pitches[i - 1], "the pitch must never rise while the engine is dying");
    }

    [Fact]
    public void StationaryCar_ShouldStillWindDownRatherThanCutOut()
    {
        // A networked finisher is snapped to a standstill by the server, so its engine has no
        // driveline holding it up and dies quickly - but it must still fall, not vanish.
        var engine = EngineHarness.BuildEngine();
        engine.SyncFromSpeed(200f, 4, 0.1f, throttleInput: 100);
        var startPitch = Pitch(engine);

        WindDown(engine, rollingCarFrom: 0f, out var pitches);

        engine.Rpm.Should().Be(0f);
        pitches[pitches.Count - 1].Should().BeLessThan(startPitch);
    }

    private static float WindDown(EngineModel engine, float rollingCarFrom, out List<int> pitches)
    {
        const float dt = 0.016f;
        const float brakingKphPerSecond = 9f * 3.6f;
        var speed = rollingCarFrom;
        var seconds = 0f;
        pitches = new List<int> { Pitch(engine) };

        for (var i = 0; i < 4000 && engine.Rpm > 1f; i++)
        {
            speed = rollingCarFrom <= 0f ? 0f : Math.Max(0f, speed - (brakingKphPerSecond * dt));
            engine.SyncFromSpeed(speed, 4, dt, throttleInput: 0, combustionEnabled: false);
            pitches.Add(Pitch(engine));
            seconds += dt;
        }

        return seconds;
    }

    private static int Pitch(EngineModel engine)
    {
        return EnginePitch.FromRpm(
            engine.Rpm,
            engine.StallRpm,
            engine.IdleRpm,
            engine.RevLimiter,
            idleFreq: 22050,
            topFreq: 55000,
            pitchCurveExponent: 1f);
    }
}
