using System.Collections.Generic;
using System.Linq;
using System.Net;
using TopSpeed.Bots;
using TopSpeed.Data;
using TopSpeed.Protocol;
using TopSpeed.Server.Bots;
using TopSpeed.Server.Config;
using TopSpeed.Server.Logging;
using TopSpeed.Server.Network;
using Xunit;
using Xunit.Abstractions;

namespace TopSpeed.Tests;

[Trait("Category", "Behavior")]
public sealed class ServerBotDrivingBehaviorTests(ITestOutputHelper output)
{
    private const int RaceTicks = 11250; // 90 seconds at the server's 8 ms step.

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void WideCustomTrack_ShouldKeepBotsOnTheSameRoadAsClients(int difficulty)
    {
        var definitions = IowaDefinitions();
        var room = CreateRoom(definitions);
        var bot = CreateBot(0, (BotDifficulty)difficulty);
        room.Bots.Add(bot);
        room.ActiveRaceParticipantIds.Add(bot.Id);
        using var logger = new Logger(LogLevel.None, logFilePath: null, writeToConsole: false);
        using var server = new RaceServer(new RaceServerConfig(), logger);
        server.AddRoomForTest(room);
        var clientRoad = new RoadModel(definitions);
        var offline = new BotRaceHarness(definitions);
        offline.AddBot((BotDrivingDifficulty)difficulty, CarType.Vehicle7);
        var outsideTicks = 0;
        var crashTicks = 0;
        for (var tick = 0; tick < RaceTicks; tick++)
        {
            server.Update(BotRaceHarness.StepSeconds);
            offline.Step();
            var road = clientRoad.At(bot.PositionY);
            if (bot.PositionX < road.Left || bot.PositionX > road.Right)
                outsideTicks++;
            if (bot.RacePhase != BotRacePhase.Normal)
                crashTicks++;
        }
        output.WriteLine($"Server: distance={bot.PositionY}, outside ticks={outsideTicks}, crash ticks={crashTicks}; offline: distance={offline.Runners[0].PositionY}, crashes={offline.Runners[0].FullCrashes}");
        outsideTicks.Should().Be(0, "server bots and clients must agree on the road corridor");
        crashTicks.Should().Be(0);
        bot.PositionY.Should().BeGreaterThan(offline.Runners[0].PositionY * 0.9f,
            "server integration must preserve the shared driver's progress");
        offline.Runners[0].OffRoadEvents.Should().Be(0);
        (offline.Runners[0].BrakeTicks / (float)offline.Runners[0].Ticks).Should().BeLessThan(0.25f);
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(0, 90)]
    [InlineData(1, 0)]
    [InlineData(1, 90)]
    [InlineData(2, 0)]
    [InlineData(2, 90)]
    public void FasterBot_ShouldPassHumanTrafficOnCustomTrack(int difficulty, ushort humanSpeed)
    {
        var room = CreateRoom(IowaDefinitions());
        var road = new RoadModel(room.TrackData!.Definitions);
        var bot = CreateBot(0, (BotDifficulty)difficulty);
        bot.PositionY = 40f;
        var human = new PlayerConnection(new IPEndPoint(IPAddress.Loopback, 39000), 1, 1)
        {
            State = PlayerState.Racing, PlayerNumber = 1, PositionY = 120f, Speed = humanSpeed
        };
        room.Bots.Add(bot);
        room.ActiveRaceParticipantIds.Add(bot.Id);
        room.ActiveRaceParticipantIds.Add(human.Id);
        using var logger = new Logger(LogLevel.None, logFilePath: null, writeToConsole: false);
        using var server = new RaceServer(new RaceServerConfig(), logger);
        server.AddRoomForTest(room, human);
        var offRoadTicks = 0;
        var collisions = 0;
        for (var tick = 0; tick < RaceTicks; tick++)
        {
            human.PositionY += humanSpeed / 3.6f * BotRaceHarness.StepSeconds;
            var humanRoad = road.At(human.PositionY);
            human.PositionX = (humanRoad.Left + humanRoad.Right) * 0.5f;
            server.Update(BotRaceHarness.StepSeconds);
            var botRoad = road.At(bot.PositionY);
            if (bot.PositionX < botRoad.Left || bot.PositionX > botRoad.Right)
                offRoadTicks++;
            if (room.ActiveBumpPairs.Count > 0)
                collisions++;
        }
        output.WriteLine($"Bot={bot.PositionY}, human={human.PositionY}, collision ticks={collisions}, off-road ticks={offRoadTicks}");
        offRoadTicks.Should().Be(0);
        collisions.Should().Be(0);
        bot.PositionY.Should().BeGreaterThan(human.PositionY + 100f,
            "a faster bot must be able to pass instead of remaining stuck near a player");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void FullGrid_ShouldCompleteCornersWithoutStrandingBots(int difficulty)
    {
        var room = CreateRoom(IowaDefinitions());
        var road = new RoadModel(room.TrackData!.Definitions);
        for (var i = 0; i < 8; i++)
        {
            var bot = CreateBot(i, (BotDifficulty)difficulty);
            bot.PositionY = BotRaceRules.CalculateStartY(i, BotRaceRules.MinStartRowSpacing);
            var segment = road.At(bot.PositionY);
            bot.PositionX = (segment.Left + segment.Right) * 0.5f
                + BotRaceRules.CalculateStartX(i, bot.WidthM, (segment.Right - segment.Left) * 0.5f);
            room.Bots.Add(bot);
            room.ActiveRaceParticipantIds.Add(bot.Id);
        }
        using var logger = new Logger(LogLevel.None, logFilePath: null, writeToConsole: false);
        using var server = new RaceServer(new RaceServerConfig(), logger);
        server.AddRoomForTest(room);
        var previousPairs = new HashSet<ulong>();
        var collisions = 0;
        var offRoadTicks = 0;
        var crashTicks = 0;
        for (var tick = 0; tick < RaceTicks; tick++)
        {
            server.Update(BotRaceHarness.StepSeconds);
            collisions += room.ActiveBumpPairs.Count(pair => !previousPairs.Contains(pair));
            previousPairs.Clear();
            previousPairs.UnionWith(room.ActiveBumpPairs);
            foreach (var bot in room.Bots)
            {
                var segment = road.At(bot.PositionY);
                if (bot.PositionX < segment.Left || bot.PositionX > segment.Right)
                    offRoadTicks++;
                if (bot.RacePhase != BotRacePhase.Normal)
                    crashTicks++;
            }
        }
        output.WriteLine($"Distances: {string.Join(", ", room.Bots.Select(bot => bot.PositionY))}; collisions={collisions}, off-road ticks={offRoadTicks}, crash ticks={crashTicks}");
        offRoadTicks.Should().Be(0);
        crashTicks.Should().Be(0);
        collisions.Should().BeLessThanOrEqualTo(2);
        room.Bots.Should().OnlyContain(bot => bot.PositionY > 2000f,
            "every bot must continue making progress through successive laps");
    }

    private static GameRoom CreateRoom(TrackDefinition[] definitions) =>
        new(1, "Iowa", GameRoomType.PlayersRace, 2)
        {
            TrackData = new TrackData(true, TrackWeather.Sunny, TrackAmbience.NoAmbience, definitions),
            RaceState = RoomRaceState.Racing,
            Laps = 20
        };

    private static RoomBot CreateBot(int index, BotDifficulty difficulty) => new()
    {
        Id = 1_000_000u + (uint)index, PlayerNumber = (byte)index,
        State = PlayerState.Racing, Difficulty = difficulty,
        AddedOrder = index + 1, Car = CarType.Vehicle7,
        PhysicsConfig = BotPhysicsCatalog.Get(CarType.Vehicle7),
        PhysicsState = new BotPhysicsState { Gear = 1 }
    };

    private static TrackDefinition[] IowaDefinitions()
    {
        // Iowa Speedway geometry from the reported server track, independent of build output.
        var types = new[]
        {
            TrackType.Straight, TrackType.HairpinLeft, TrackType.EasyLeft,
            TrackType.Straight, TrackType.HairpinLeft, TrackType.EasyLeft
        };
        var lengths = new[] { 122f, 200f, 230f, 239f, 100f, 316f };
        var widths = new[] { 17f, 18f, 18f, 18f, 18f, 17f };
        var definitions = new TrackDefinition[types.Length];
        for (var i = 0; i < definitions.Length; i++)
            definitions[i] = new TrackDefinition(types[i], TrackSurface.Asphalt,
                TrackNoise.NoNoise, lengths[i], null, widths[i], 0f, null, null, null, null);
        return definitions;
    }
}
