using System;
using System.Linq;
using FluentAssertions;
using TopSpeed.Bots;
using TopSpeed.Data;
using TopSpeed.Protocol;
using Xunit;

namespace TopSpeed.Tests;

/// <summary>
/// Single-step decisions of the bot driver. Anything about how the car actually behaves over time
/// lives in <see cref="BotDrivingLoopBehaviorTests"/> — a one-shot assertion cannot tell a working
/// control loop from an inverted one.
/// </summary>
[Trait("Category", "Behavior")]
public sealed class BotDrivingPlannerBehaviorTests
{
    [Fact]
    public void UpcomingHairpin_ShouldReduceTargetSpeedAndRequestBraking()
    {
        var state = default(BotDriverState);
        var input = Input(
            BotDrivingDifficulty.Normal,
            speedKph: 260f,
            road: Ladder(TrackType.Straight, TrackType.HairpinLeft, cornerAt: 30f));

        var output = BotDrivingPlanner.Step(ref state, in input);

        output.TargetSpeedKph.Should().BeLessThan(200f);
        output.Braking.Should().BeTrue();
    }

    [Fact]
    public void ClearStraight_ShouldNotBrake()
    {
        var state = default(BotDriverState);
        var input = Input(BotDrivingDifficulty.Normal, speedKph: 120f);

        var output = BotDrivingPlanner.Step(ref state, in input);

        output.Braking.Should().BeFalse();
        output.Throttle.Should().BeGreaterThan(0f);
    }

    [Fact]
    public void GentleCurveWellWithinGrip_ShouldNotBrake()
    {
        var state = default(BotDriverState);
        var input = Input(
            BotDrivingDifficulty.Normal,
            speedKph: 120f,
            road: Ladder(TrackType.EasyLeft, TrackType.EasyLeft, cornerAt: 0f));

        var output = BotDrivingPlanner.Step(ref state, in input);

        output.Braking.Should().BeFalse();
    }

    [Fact]
    public void HarderCorners_ShouldImposeLowerSpeedLimits()
    {
        static float Limit(TrackType type)
        {
            var state = default(BotDriverState);
            var input = Input(BotDrivingDifficulty.Hard, 60f, Ladder(type, type, cornerAt: 0f));
            return BotDrivingPlanner.Step(ref state, in input).TargetSpeedKph;
        }

        var straight = Limit(TrackType.Straight);
        var easy = Limit(TrackType.EasyRight);
        var hard = Limit(TrackType.HardRight);
        var hairpin = Limit(TrackType.HairpinRight);

        straight.Should().BeGreaterThan(hard);
        easy.Should().BeGreaterThan(hard);
        hard.Should().BeGreaterThan(hairpin);
    }

    [Fact]
    public void HigherDifficulty_ShouldCarryMoreSpeedThroughTheSameCorner()
    {
        static float Limit(BotDrivingDifficulty difficulty)
        {
            var state = default(BotDriverState);
            var input = Input(difficulty, 60f, Ladder(TrackType.HardLeft, TrackType.HardLeft, cornerAt: 0f));
            return BotDrivingPlanner.Step(ref state, in input).TargetSpeedKph;
        }

        Limit(BotDrivingDifficulty.Hard).Should().BeGreaterThan(Limit(BotDrivingDifficulty.Normal));
        Limit(BotDrivingDifficulty.Normal).Should().BeGreaterThan(Limit(BotDrivingDifficulty.Easy));
    }

    [Fact]
    public void SlowerVehicleAhead_ShouldMoveOffItsLine()
    {
        var state = default(BotDriverState);
        var traffic = new[] { Vehicle(2, isHuman: false, x: 0f, y: 30f, speed: 80f) };
        var input = Input(BotDrivingDifficulty.Hard, 160f, traffic: traffic);

        BotDrivingPlanner.Step(ref state, in input);

        state.Maneuver.Should().NotBe(BotManeuver.Follow);
        Math.Abs(state.TargetOffsetM).Should().BeGreaterThan(0.5f);
    }

    [Fact]
    public void BoxedIn_ShouldFollowRatherThanForceAGap()
    {
        var state = default(BotDriverState);
        var traffic = new[]
        {
            Vehicle(2, isHuman: false, x: -2.2f, y: 26f, speed: 80f),
            Vehicle(3, isHuman: false, x: 0f, y: 30f, speed: 80f),
            Vehicle(4, isHuman: false, x: 2.2f, y: 26f, speed: 80f)
        };
        var input = Input(BotDrivingDifficulty.Hard, 160f, traffic: traffic);

        var output = BotDrivingPlanner.Step(ref state, in input);

        state.Maneuver.Should().Be(BotManeuver.Follow);
        output.Braking.Should().BeTrue();
    }

    [Fact]
    public void HumanAhead_ShouldNotBeTargeted()
    {
        // The driver has no aggression behaviour at all any more: a human is traffic to avoid,
        // never something to line up on.
        var state = default(BotDriverState);
        var human = Vehicle(9, isHuman: true, x: 0f, y: 12f, speed: 150f);
        var input = Input(BotDrivingDifficulty.Hard, 170f, traffic: new[] { human });

        BotDrivingPlanner.Step(ref state, in input);

        var targetX = state.TargetOffsetM;
        Math.Abs(targetX - human.PositionX).Should().BeGreaterThan(1.5f);
    }

    [Fact]
    public void ClosingFastOnAStoppedCar_ShouldBrakeHard()
    {
        var state = default(BotDriverState);
        var traffic = new[] { Vehicle(2, isHuman: false, x: 0f, y: 22f, speed: 0f) };
        var input = Input(BotDrivingDifficulty.Normal, 200f, traffic: traffic);

        var output = BotDrivingPlanner.Step(ref state, in input);

        output.Brake.Should().BeLessThan(-50f);
        output.Throttle.Should().Be(0f);
    }

    [Fact]
    public void SameSeedAndInput_ShouldProduceIdenticalDecisions()
    {
        var input = Input(BotDrivingDifficulty.Hard, 150f, traffic: new[]
        {
            Vehicle(2, isHuman: false, x: 1.2f, y: 30f, speed: 110f)
        });

        var firstState = default(BotDriverState);
        var secondState = default(BotDriverState);
        var first = BotDrivingPlanner.Step(ref firstState, in input);
        var second = BotDrivingPlanner.Step(ref secondState, in input);

        first.Throttle.Should().Be(second.Throttle);
        first.Brake.Should().Be(second.Brake);
        first.Steering.Should().Be(second.Steering);
        first.TargetSpeedKph.Should().Be(second.TargetSpeedKph);
        firstState.TargetOffsetM.Should().Be(secondState.TargetOffsetM);
    }

    [Fact]
    public void TrafficOrder_ShouldNotChangeTheDecision()
    {
        var traffic = new[]
        {
            Vehicle(2, isHuman: false, x: -2f, y: 30f, speed: 90f),
            Vehicle(3, isHuman: false, x: 2f, y: 45f, speed: 120f),
            Vehicle(4, isHuman: true, x: 0.5f, y: 60f, speed: 130f)
        };
        var reversed = traffic.Reverse().ToArray();

        var firstState = default(BotDriverState);
        var secondState = default(BotDriverState);
        var forward = Input(BotDrivingDifficulty.Hard, 170f, traffic: traffic);
        var backward = Input(BotDrivingDifficulty.Hard, 170f, traffic: reversed);

        var first = BotDrivingPlanner.Step(ref firstState, in forward);
        var second = BotDrivingPlanner.Step(ref secondState, in backward);

        first.Steering.Should().Be(second.Steering);
        first.Throttle.Should().Be(second.Throttle);
        first.Brake.Should().Be(second.Brake);
        firstState.TargetOffsetM.Should().Be(secondState.TargetOffsetM);
    }

    [Fact]
    public void TargetLine_ShouldStayInsideTheCorridor()
    {
        var state = default(BotDriverState);
        var traffic = new[] { Vehicle(2, isHuman: false, x: 0f, y: 25f, speed: 60f) };
        var input = Input(BotDrivingDifficulty.Hard, 150f, traffic: traffic);

        BotDrivingPlanner.Step(ref state, in input);

        // 5 m half width, 1.8 m car, 0.35 m margin.
        Math.Abs(state.TargetOffsetM).Should().BeLessThanOrEqualTo(3.75f);
    }

    [Fact]
    public void AfterContact_ShouldRecenterAndStopOvertaking()
    {
        var state = default(BotDriverState);
        var traffic = new[] { Vehicle(2, isHuman: false, x: 0f, y: 30f, speed: 80f) };
        var input = Input(BotDrivingDifficulty.Hard, 160f, traffic: traffic);

        BotDrivingPlanner.Step(ref state, in input);
        var overtakingOffset = Math.Abs(state.TargetOffsetM);

        BotDrivingPlanner.NotifyContact(ref state);
        BotDrivingPlanner.Step(ref state, in input);

        state.RecoverySecondsRemaining.Should().BeGreaterThan(0f);
        Math.Abs(state.TargetOffsetM).Should().BeLessThan(overtakingOffset);
    }

    [Fact]
    public void NoRoadData_ShouldStopSafely()
    {
        var state = default(BotDriverState);
        var ego = new BotEgoState(0f, 0f, 100f, 0f, 0f, 3);
        var capabilities = Capabilities();
        var input = new BotDrivingInput(
            BotDrivingDifficulty.Normal,
            seed: 1u,
            vehicleId: 1u,
            elapsedSeconds: 0.016f,
            in ego,
            in capabilities,
            Array.Empty<BotRoadPreview>(),
            Array.Empty<BotVehicleObservation>());

        var output = BotDrivingPlanner.Step(ref state, in input);

        output.Throttle.Should().Be(0f);
        output.Brake.Should().Be(-100f);
    }

    internal static BotCapabilities Capabilities(CarType car = CarType.Vehicle1)
        => BotCapabilities.From(BotPhysicsCatalog.Get(car));

    private static BotDrivingInput Input(
        BotDrivingDifficulty difficulty,
        float speedKph,
        BotRoadPreview[]? road = null,
        BotVehicleObservation[]? traffic = null,
        uint seed = 77u)
    {
        var ego = new BotEgoState(0f, 0f, speedKph, 0f, 0f, 4);
        var capabilities = Capabilities();
        return new BotDrivingInput(
            difficulty,
            seed,
            vehicleId: 1u,
            elapsedSeconds: 0.1f,
            in ego,
            in capabilities,
            road ?? Ladder(TrackType.Straight, TrackType.Straight, cornerAt: 0f),
            traffic ?? Array.Empty<BotVehicleObservation>());
    }

    /// <summary>
    /// A full-length preview ladder: <paramref name="near"/> up to <paramref name="cornerAt"/>,
    /// then <paramref name="far"/>. The planner's backward pass needs the whole ladder, not two
    /// samples, to work out where the braking point is.
    /// </summary>
    private static BotRoadPreview[] Ladder(TrackType near, TrackType far, float cornerAt)
    {
        var distances = BotRoadSampling.CreateDistances();
        BotRoadSampling.FillDistances(200f, distances);
        var preview = new BotRoadPreview[distances.Length];
        for (var i = 0; i < distances.Length; i++)
        {
            var type = distances[i] >= cornerAt ? far : near;
            preview[i] = Road(distances[i], type);
        }
        return preview;
    }

    private static BotRoadPreview Road(float distance, TrackType type, float left = -5f, float right = 5f)
    {
        return new BotRoadPreview(distance, left, right, TrackSurface.Asphalt, type);
    }

    private static BotVehicleObservation Vehicle(uint id, bool isHuman, float x, float y, float speed)
    {
        return new BotVehicleObservation(id, isHuman, x, y, speed, widthM: 1.8f, lengthM: 4.5f);
    }
}
