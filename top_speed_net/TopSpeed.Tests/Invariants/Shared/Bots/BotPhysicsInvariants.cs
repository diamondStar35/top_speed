using System;
using FsCheck.Fluent;
using FsCheck.Xunit;
using TopSpeed.Bots;
using TopSpeed.Data;
using TopSpeed.Protocol;
using Xunit;

namespace TopSpeed.Tests;

[Trait("Category", "Invariant")]
public sealed class BotPhysicsInvariantTests
{
    [Property(MaxTest = 100, Arbitrary = new[] { typeof(BotPhysicsArbitraries) })]
    public void ScenarioSteps_ShouldKeepFiniteState_AndValidGear(BotScenario scenario)
    {
        var config = BotPhysicsCatalog.Get(scenario.CarType);
        var state = BotPhysicsHarness.CreateState(config, scenario.InitialSpeedKph);

        for (var i = 0; i < scenario.Steps; i++)
        {
            var input = new BotPhysicsInput(
                scenario.ElapsedSeconds,
                scenario.Surface,
                scenario.Throttle,
                scenario.Brake,
                scenario.Steering,
                ambientTemperatureC: float.NaN,
                rainGain: 0f,
                stormGain: 0f,
                windGain: 0f);

            BotPhysics.Step(config, ref state, input);

            IsFinite(state.PositionX).Should().BeTrue();
            IsFinite(state.PositionY).Should().BeTrue();
            IsFinite(state.SpeedKph).Should().BeTrue();
            IsFinite(state.LateralVelocityMps).Should().BeTrue();
            IsFinite(state.YawRateRad).Should().BeTrue();
            IsFinite(state.AutomaticCouplingFactor).Should().BeTrue();
            IsFinite(state.CvtRatio).Should().BeTrue();
            IsFinite(state.EffectiveDriveRatio).Should().BeTrue();
            state.SpeedKph.Should().BeGreaterThanOrEqualTo(0f);
            state.Gear.Should().BeInRange(1, config.Gears);
        }
    }

    [Property(MaxTest = 100, Arbitrary = new[] { typeof(BotPhysicsArbitraries) })]
    public void FullBrakeScenario_ShouldNotIncreaseSpeed(BotBrakingScenario scenario)
    {
        var config = BotPhysicsCatalog.Get(scenario.CarType);
        var state = BotPhysicsHarness.CreateState(config, scenario.InitialSpeedKph);
        var initialSpeed = state.SpeedKph;

        for (var i = 0; i < scenario.Steps; i++)
        {
            BotPhysics.Step(
                config,
                ref state,
                new BotPhysicsInput(
                    scenario.ElapsedSeconds,
                    scenario.Surface,
                    throttle: 0,
                    brake: -100,
                    steering: scenario.Steering,
                    ambientTemperatureC: float.NaN,
                    rainGain: 0f,
                    stormGain: 0f,
                    windGain: 0f));
        }

        state.SpeedKph.Should().BeLessThanOrEqualTo(initialSpeed + 0.001f);
        state.SpeedKph.Should().BeGreaterThanOrEqualTo(0f);
    }

    private static bool IsFinite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
}

public sealed record BotScenario(
    CarType CarType,
    TrackSurface Surface,
    int Throttle,
    int Brake,
    int Steering,
    int Steps,
    float ElapsedSeconds,
    float InitialSpeedKph);

public sealed record BotBrakingScenario(
    CarType CarType,
    TrackSurface Surface,
    int Steering,
    int Steps,
    float ElapsedSeconds,
    float InitialSpeedKph);

public static class BotPhysicsArbitraries
{
    public static FsCheck.Arbitrary<BotScenario> BotScenario()
    {
        var generator =
            from car in Gen.Choose(0, 11)
            from surface in Gen.Elements(TrackSurface.Asphalt, TrackSurface.Gravel, TrackSurface.Water, TrackSurface.Sand, TrackSurface.Snow)
            from throttle in Gen.Choose(0, 100)
            from brake in Gen.Choose(-100, 0)
            from steering in Gen.Choose(-100, 100)
            from steps in Gen.Choose(1, 120)
            from elapsed in Gen.Choose(2, 20)
            from initialSpeed in Gen.Choose(0, 240)
            select new BotScenario(
                CarType: (CarType)car,
                Surface: surface,
                Throttle: throttle,
                Brake: brake,
                Steering: steering,
                Steps: steps,
                ElapsedSeconds: elapsed / 100f,
                InitialSpeedKph: initialSpeed);

        return Arb.From(generator);
    }

    public static FsCheck.Arbitrary<BotBrakingScenario> BotBrakingScenario()
    {
        var generator =
            from car in Gen.Choose(0, 11)
            from surface in Gen.Elements(TrackSurface.Asphalt, TrackSurface.Gravel, TrackSurface.Water, TrackSurface.Sand, TrackSurface.Snow)
            from steering in Gen.Choose(-60, 60)
            from steps in Gen.Choose(1, 120)
            from elapsed in Gen.Choose(2, 20)
            from initialSpeed in Gen.Choose(20, 240)
            select new BotBrakingScenario(
                CarType: (CarType)car,
                Surface: surface,
                Steering: steering,
                Steps: steps,
                ElapsedSeconds: elapsed / 100f,
                InitialSpeedKph: initialSpeed);

        return Arb.From(generator);
    }
}
