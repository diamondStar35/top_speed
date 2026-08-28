using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using TopSpeed.Bots;
using TopSpeed.Data;
using TopSpeed.Protocol;
using Xunit;

namespace TopSpeed.Tests;

/// <summary>
/// Closed-loop behaviour: planner -> physics -> road -> planner, over real tracks.
/// These are the tests that can tell whether the driver actually drives. A single-step assertion
/// cannot — an inverted steering loop still produces a plausible-looking first command.
/// </summary>
[Trait("Category", "Behavior")]
public sealed class BotDrivingLoopBehaviorTests
{
    [Theory]
    [InlineData(BotDrivingDifficulty.Easy)]
    [InlineData(BotDrivingDifficulty.Normal)]
    [InlineData(BotDrivingDifficulty.Hard)]
    public void OffCentreOnAStraight_ShouldSteerBackTowardTheLine(BotDrivingDifficulty difficulty)
    {
        var harness = BotRaceHarness.ForUniformTrack(TrackType.Straight);
        var bot = harness.AddBot(difficulty, positionX: 3.2f, speedKph: 120f);

        var startOffset = Math.Abs(bot.PositionX);
        harness.Run(5f);
        var endOffset = Math.Abs(bot.PositionX);

        endOffset.Should().BeLessThan(startOffset * 0.5f,
            "the driver must steer toward its target line, not away from it");
        bot.OffRoadEvents.Should().Be(0);
    }

    [Theory]
    [InlineData(TrackType.Straight)]
    [InlineData(TrackType.EasyLeft)]
    [InlineData(TrackType.EasyRight)]
    [InlineData(TrackType.Left)]
    [InlineData(TrackType.Right)]
    [InlineData(TrackType.HardLeft)]
    [InlineData(TrackType.HardRight)]
    [InlineData(TrackType.HairpinLeft)]
    [InlineData(TrackType.HairpinRight)]
    public void EveryCornerType_ShouldBeHeldWithoutLeavingTheRoad(TrackType type)
    {
        foreach (var difficulty in AllDifficulties)
        {
            var harness = BotRaceHarness.ForUniformTrack(type);
            var bot = harness.AddBot(difficulty);

            harness.Run(45f);

            bot.OffRoadEvents.Should().Be(0, $"{difficulty} bots must hold a {type}");
            bot.DistanceTravelled.Should().BeGreaterThan(500f, $"{difficulty} bots must keep making progress on a {type}");
        }
    }

    [Fact]
    public void EveryOfficialCar_ShouldCompleteALapOfARealTrackCleanly()
    {
        foreach (var car in OfficialCars)
        {
            var harness = BotRaceHarness.ForBuiltInTrack("america");
            var bot = harness.AddBot(BotDrivingDifficulty.Hard, car);

            harness.Run(60f);

            bot.OffRoadEvents.Should().Be(0, $"{car} must stay on the road");
            bot.DistanceTravelled.Should().BeGreaterThan(800f, $"{car} must make progress");
        }
    }

    [Fact]
    public void ClearRoad_ShouldNotProduceUnnecessaryBraking()
    {
        var harness = BotRaceHarness.ForUniformTrack(TrackType.Straight);
        harness.AddBot(BotDrivingDifficulty.Normal, speedKph: 60f);

        var report = harness.Run(30f);

        report.BrakeDutyCycle.Should().BeLessThan(0.02f,
            "there is nothing on a straight to brake for; brake noise here is the traffic-noise complaint");
    }

    [Fact]
    public void FollowingASteadyLeader_ShouldSettleInsteadOfSurging()
    {
        var harness = BotRaceHarness.ForSingleFileStraight();
        harness.AddPaceCar(positionY: 120f, positionX: 0f, speedKph: 130f, id: 100u);
        var follower = harness.AddBot(BotDrivingDifficulty.Normal, positionY: 40f, speedKph: 130f, id: 1u);

        harness.Run(12f);

        var speeds = new List<float>();
        for (var i = 0; i < 1200; i++)
        {
            harness.Step();
            speeds.Add(follower.SpeedKph);
        }

        follower.Collisions.Should().Be(0);
        Spread(speeds).Should().BeLessThan(12f, "a settled follower should hold station, not surge and lift");
    }

    [Fact]
    public void ChainOfFollowers_ShouldNotAmplifyALeadersLiftUpstream()
    {
        // The failure this guards against: a small lift at the front growing into a hard stop at
        // the back, which is what turned a queue of bots into a pile-up. The queue is settled
        // first, then the leader lifts, and each follower's undershoot is compared with the one
        // ahead of it - a stable chain damps the disturbance as it travels backwards.
        var harness = BotRaceHarness.ForSingleFileStraight();
        var pace = harness.AddPaceCar(positionY: 300f, positionX: 0f, speedKph: 140f, id: 100u);

        var chain = new List<BotRaceHarness.Runner>();
        for (var i = 0; i < 4; i++)
            chain.Add(harness.AddBot(BotDrivingDifficulty.Normal, positionY: 240f - (i * 40f), speedKph: 140f, id: (uint)(i + 1)));

        harness.Run(40f);
        chain.Select(c => c.SpeedKph).Should().OnlyContain(v => v > 138f, "the queue should be settled before the disturbance");

        pace.SpeedKph = 110f;

        var lows = chain.Select(c => float.MaxValue).ToArray();
        for (var step = 0; step < 2500; step++)
        {
            harness.Step();
            for (var i = 0; i < chain.Count; i++)
                lows[i] = Math.Min(lows[i], chain[i].SpeedKph);
        }

        for (var i = 1; i < chain.Count; i++)
        {
            var aheadDrop = 140f - lows[i - 1];
            var ownDrop = 140f - lows[i];
            ownDrop.Should().BeLessThan(Math.Max(2f, aheadDrop * 1.15f),
                $"position {i} must not brake harder than the car it is following");
        }

        chain.Sum(c => c.Collisions).Should().Be(0);
    }

    [Fact]
    public void FasterBotBehindASlowOne_ShouldGetPast()
    {
        var harness = BotRaceHarness.ForUniformTrack(TrackType.Straight);
        var pace = harness.AddPaceCar(positionY: 90f, positionX: 0f, speedKph: 90f, id: 100u);
        var chaser = harness.AddBot(BotDrivingDifficulty.Hard, positionY: 20f, speedKph: 150f, id: 1u);

        harness.Run(25f);

        chaser.PositionY.Should().BeGreaterThan(pace.PositionY, "a much faster bot must find a way past, not queue behind");
        chaser.Collisions.Should().Be(0);
    }

    [Fact]
    public void FullGridOnARealTrack_ShouldRaceWithoutCarnage()
    {
        var harness = BotRaceHarness.ForBuiltInTrack("america");
        var laneHalfWidth = harness.Road.LaneHalfWidth;
        var rowSpacing = BotRaceRules.CalculateStartRowSpacing(4.5f);

        for (var i = 0; i < 8; i++)
        {
            harness.AddBot(
                (BotDrivingDifficulty)(i % 3),
                OfficialCars[i % OfficialCars.Length],
                positionY: Math.Max(0f, BotRaceRules.CalculateStartY(i, rowSpacing)),
                positionX: BotRaceRules.CalculateStartX(i, 1.8f, laneHalfWidth),
                id: (uint)(i + 1));
        }

        var report = harness.Run(90f);

        report.FullCrashes.Should().Be(0, "a bot leaving the road at speed is a driving failure");
        report.OffRoadEvents.Should().Be(0);
        report.Collisions.Should().BeLessThanOrEqualTo(2, "contact should be rare and incidental, not a chain reaction");
        report.MinDistanceTravelledM.Should().BeGreaterThan(1400f, "no bot may be left stranded or deadlocked");
        report.BrakeDutyCycle.Should().BeLessThan(0.25f);
    }

    [Fact]
    public void FullGrid_ShouldSpreadOutRatherThanStackIntoOneGroove()
    {
        var harness = BotRaceHarness.ForBuiltInTrack("germany");
        for (var i = 0; i < 6; i++)
            harness.AddBot(BotDrivingDifficulty.Normal, CarType.Vehicle1, positionY: i * -14f, id: (uint)(i + 1));

        harness.Run(60f);

        var gaps = harness.Runners
            .OrderBy(r => r.PositionY)
            .Select(r => r.PositionY)
            .Zip(harness.Runners.OrderBy(r => r.PositionY).Skip(1).Select(r => r.PositionY), (a, b) => b - a)
            .ToArray();

        gaps.Should().OnlyContain(gap => gap > 5f, "bots must not end up nose to tail on top of each other");
    }

    [Fact]
    public void SameSeededField_ShouldSimulateIdentically()
    {
        static float[] RunOnce()
        {
            var harness = BotRaceHarness.ForBuiltInTrack("austria");
            for (var i = 0; i < 4; i++)
                harness.AddBot(BotDrivingDifficulty.Hard, CarType.Vehicle7, positionY: i * -12f, id: (uint)(i + 1));
            harness.Run(20f);
            return harness.Runners.Select(r => r.PositionY).ToArray();
        }

        RunOnce().Should().Equal(RunOnce());
    }

    private static readonly BotDrivingDifficulty[] AllDifficulties =
    {
        BotDrivingDifficulty.Easy,
        BotDrivingDifficulty.Normal,
        BotDrivingDifficulty.Hard
    };

    private static readonly CarType[] OfficialCars =
    {
        CarType.Vehicle1, CarType.Vehicle2, CarType.Vehicle3, CarType.Vehicle4,
        CarType.Vehicle5, CarType.Vehicle6, CarType.Vehicle7, CarType.Vehicle8,
        CarType.Vehicle9, CarType.Vehicle10, CarType.Vehicle11, CarType.Vehicle12
    };

    private static float Spread(IReadOnlyList<float> values)
    {
        if (values.Count == 0)
            return 0f;
        var min = values[0];
        var max = values[0];
        for (var i = 1; i < values.Count; i++)
        {
            if (values[i] < min) min = values[i];
            if (values[i] > max) max = values[i];
        }
        return max - min;
    }
}
