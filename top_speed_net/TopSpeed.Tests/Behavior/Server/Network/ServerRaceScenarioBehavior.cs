using System.Linq;
using TopSpeed.Protocol;
using TopSpeed.Server.Network;
using TopSpeed.Server.Bots;
using Xunit;

namespace TopSpeed.Tests;

[Trait("Category", "Behavior")]
public sealed class ServerRaceScenarioBehaviorTests
{
    [Fact]
    public void ServerRoomScenario_ShouldCompleteWhenAllActiveParticipantsResolve()
    {
        var room = new GameRoom(1, "integration", GameRoomType.PlayersRace, 2);
        room.ActiveRaceParticipantIds.Add(10);
        room.ActiveRaceParticipantIds.Add(11);
        room.RaceParticipantResults[10] = new RoomRaceParticipantResult
        {
            PlayerId = 10,
            PlayerNumber = 0,
            Status = RoomRaceResultStatus.Finished,
            FinishOrder = 1,
            TimeMs = 1000
        };
        room.RaceParticipantResults[11] = new RoomRaceParticipantResult
        {
            PlayerId = 11,
            PlayerNumber = 1,
            Status = RoomRaceResultStatus.Pending
        };

        SnapshotStatuses(room).Should().Contain(RoomRaceResultStatus.Pending);
        RaceResultRules.ShouldComplete(SnapshotStatuses(room)).Should().BeFalse();

        room.RaceParticipantResults[11].Status = RoomRaceResultStatus.Finished;
        room.RaceParticipantResults[11].FinishOrder = 2;

        RaceResultRules.ShouldComplete(SnapshotStatuses(room)).Should().BeTrue();
    }

    [Fact]
    public void ServerRoomScenario_ShouldCompleteWhenDisconnectedParticipantExpiresAsDnf()
    {
        var room = new GameRoom(1, "integration", GameRoomType.PlayersRace, 2);
        room.ActiveRaceParticipantIds.Add(10);
        room.ActiveRaceParticipantIds.Add(11);
        room.RaceParticipantResults[10] = new RoomRaceParticipantResult
        {
            PlayerId = 10,
            PlayerNumber = 0,
            Status = RoomRaceResultStatus.Finished,
            FinishOrder = 1,
            TimeMs = 1000
        };
        room.RaceParticipantResults[11] = new RoomRaceParticipantResult
        {
            PlayerId = 11,
            PlayerNumber = 1,
            Status = RaceResultRules.ResolveParticipantStatus(RoomRaceResultStatus.Pending, RoomRaceResultStatus.Dnf)
        };

        RaceResultRules.ShouldComplete(SnapshotStatuses(room)).Should().BeTrue();
    }

    [Fact]
    public void ServerRoomScenario_ShouldKeepReconnectedParticipantPendingBeforeGraceExpiry()
    {
        var room = new GameRoom(1, "integration", GameRoomType.PlayersRace, 2);
        room.ActiveRaceParticipantIds.Add(10);
        room.ActiveRaceParticipantIds.Add(11);
        room.RaceParticipantResults[10] = new RoomRaceParticipantResult
        {
            PlayerId = 10,
            PlayerNumber = 0,
            Status = RoomRaceResultStatus.Finished,
            FinishOrder = 1,
            TimeMs = 1000
        };
        room.RaceParticipantResults[11] = new RoomRaceParticipantResult
        {
            PlayerId = 11,
            PlayerNumber = 1,
            Status = RaceResultRules.ResolveParticipantStatus(RoomRaceResultStatus.Pending, RoomRaceResultStatus.Pending)
        };

        RaceResultRules.ShouldComplete(SnapshotStatuses(room)).Should().BeFalse();
    }

    [Fact]
    public void ServerRoomScenario_ShouldRecordFinishOnce()
    {
        var room = new GameRoom(1, "integration", GameRoomType.PlayersRace, 2)
        {
            RaceState = RoomRaceState.Racing
        };
        room.ActiveRaceParticipantIds.Add(10);

        RaceParticipantFinisher.TryMarkFinished(room, 10, 0, 1000, out var firstOrder).Should().BeTrue();
        RaceParticipantFinisher.TryMarkFinished(room, 10, 0, 2000, out var duplicateOrder).Should().BeFalse();

        firstOrder.Should().Be(1);
        duplicateOrder.Should().Be(0);
        room.RaceParticipantResults[10].Lifecycle.Should().Be(RaceParticipantLifecycleState.Finished);
        room.RaceParticipantResults[10].FinishOrder.Should().Be(1);
        room.RaceParticipantResults[10].TimeMs.Should().Be(1000);
        room.RaceResults.Should().Equal((byte)0);
        room.RaceFinishTimesMs[0].Should().Be(1000);
    }

    [Fact]
    public void ServerRoomScenario_ShouldAssignStableFinishOrder()
    {
        var room = new GameRoom(1, "integration", GameRoomType.PlayersRace, 2)
        {
            RaceState = RoomRaceState.Racing
        };
        room.ActiveRaceParticipantIds.Add(10);
        room.ActiveRaceParticipantIds.Add(11);

        RaceParticipantFinisher.TryMarkFinished(room, 11, 1, 900, out var firstOrder).Should().BeTrue();
        RaceParticipantFinisher.TryMarkFinished(room, 10, 0, 1000, out var secondOrder).Should().BeTrue();

        firstOrder.Should().Be(1);
        secondOrder.Should().Be(2);
        room.RaceParticipantResults[11].FinishOrder.Should().Be(1);
        room.RaceParticipantResults[10].FinishOrder.Should().Be(2);
        RaceResultRules.ShouldComplete(SnapshotStatuses(room)).Should().BeTrue();
    }

    [Fact]
    public void ServerRoomScenario_ShouldResolveDisconnectAfterGraceAsDnf()
    {
        var room = new GameRoom(1, "integration", GameRoomType.PlayersRace, 2)
        {
            RaceState = RoomRaceState.Racing
        };
        room.ActiveRaceParticipantIds.Add(10);
        room.ActiveRaceParticipantIds.Add(11);
        room.RaceParticipantResults[10] = new RoomRaceParticipantResult
        {
            PlayerId = 10,
            PlayerNumber = 0,
            Status = RoomRaceResultStatus.Finished,
            Lifecycle = RaceParticipantLifecycleState.Finished,
            FinishOrder = 1,
            TimeMs = 1000
        };

        RaceParticipantFinisher.TryMarkDnf(room, 11, 1, RaceParticipantLifecycleState.Expired).Should().BeTrue();

        room.RaceParticipantResults[11].Status.Should().Be(RoomRaceResultStatus.Dnf);
        room.RaceParticipantResults[11].Lifecycle.Should().Be(RaceParticipantLifecycleState.Expired);
        RaceResultRules.ShouldComplete(SnapshotStatuses(room)).Should().BeTrue();
    }

    [Fact]
    public void RoomEventJournal_ShouldReplayOnlyMissingOrderedEvents()
    {
        var room = new GameRoom(1, "integration", GameRoomType.PlayersRace, 2)
        {
            RaceInstanceId = 5
        };

        RoomEventJournal.Record(room, Command.RoomRaceStateChanged, 1, new byte[] { 1 }, PacketStream.Room).Should().BeTrue();
        RoomEventJournal.Record(room, Command.RoomRacePlayerFinished, 2, new byte[] { 2 }, PacketStream.Room).Should().BeTrue();
        RoomEventJournal.Record(room, Command.RoomRaceCompleted, 3, new byte[] { 3 }, PacketStream.Room).Should().BeTrue();

        RoomEventJournal.ReplayAfter(room, 1)
            .Select(entry => entry.Sequence)
            .Should()
            .Equal(2u, 3u);
    }

    [Fact]
    public void RoomEventJournal_ShouldRejectNonIncreasingSequences()
    {
        var room = new GameRoom(1, "integration", GameRoomType.PlayersRace, 2);

        RoomEventJournal.Record(room, Command.RoomRaceStateChanged, 2, new byte[] { 2 }, PacketStream.Room).Should().BeTrue();
        RoomEventJournal.Record(room, Command.RoomRacePlayerFinished, 2, new byte[] { 3 }, PacketStream.Room).Should().BeFalse();
        RoomEventJournal.Record(room, Command.RoomRaceCompleted, 1, new byte[] { 1 }, PacketStream.Room).Should().BeFalse();

        room.EventJournal.Should().ContainSingle();
    }

    [Fact]
    public void RoomEventJournal_ShouldClearForNextRaceWithoutResettingRoomSequence()
    {
        var room = new GameRoom(1, "integration", GameRoomType.PlayersRace, 2)
        {
            EventSequence = 7
        };
        RoomEventJournal.Record(room, Command.RoomRaceStateChanged, 7, new byte[] { 7 }, PacketStream.Room);

        RoomEventJournal.ClearForRaceStart(room);

        room.EventJournal.Should().BeEmpty();
        room.EventSequence.Should().Be(7);
    }

    [Fact]
    public void RaceCompletionInvariants_ShouldRejectDuplicateFinishOrders()
    {
        var room = new GameRoom(1, "integration", GameRoomType.PlayersRace, 2);
        room.RaceParticipantResults[10] = new RoomRaceParticipantResult
        {
            PlayerId = 10,
            PlayerNumber = 0,
            Status = RoomRaceResultStatus.Finished,
            Lifecycle = RaceParticipantLifecycleState.Finished,
            FinishOrder = 1
        };
        room.RaceParticipantResults[11] = new RoomRaceParticipantResult
        {
            PlayerId = 11,
            PlayerNumber = 1,
            Status = RoomRaceResultStatus.Finished,
            Lifecycle = RaceParticipantLifecycleState.Finished,
            FinishOrder = 1
        };

        RaceCompletionInvariants.TryValidateTerminalResults(room, out var reason).Should().BeFalse();
        reason.Should().Be("duplicate_finish_order");
    }

    [Fact]
    public void ServerBotFinish_ShouldRollToAHaltAndClearSignals()
    {
        var bot = new RoomBot
        {
            Id = 20,
            PlayerNumber = 2,
            State = PlayerState.Racing,
            PositionX = 3f,
            PositionY = 990f,
            SpeedKph = 150f,
            Horning = true,
            HornSecondsRemaining = 1f,
            BackfirePulseSeconds = 1f,
            BackfireArmed = false,
            RacePhase = BotRacePhase.Crashing,
            CrashRecoverySeconds = 1f,
            StartDelaySeconds = 1f,
            EngineStartSecondsRemaining = 1f,
            PhysicsState = new TopSpeed.Bots.BotPhysicsState
            {
                PositionX = 3f,
                PositionY = 990f,
                SpeedKph = 150f,
                Gear = 4,
                AutoShiftCooldownSeconds = 1f
            }
        };

        ServerBotFinish.BeginStop(bot, 1000f);

        bot.State.Should().Be(PlayerState.Finished);
        bot.PositionY.Should().Be(1000f);
        bot.Horning.Should().BeFalse();
        bot.HornSecondsRemaining.Should().Be(0f);
        bot.BackfirePulseSeconds.Should().Be(0f);
        bot.BackfireArmed.Should().BeTrue();

        // The car keeps its speed and rolls to a halt; freezing it here is what made a finishing
        // rival drop from a racing note to silence in one snapshot for every listener.
        bot.RacePhase.Should().Be(BotRacePhase.Stopping);
        bot.SpeedKph.Should().Be(150f);
        bot.PhysicsState.SpeedKph.Should().Be(150f);
        bot.PhysicsState.PositionY.Should().Be(1000f);

        ServerBotFinish.CompleteStop(bot);

        bot.RacePhase.Should().Be(BotRacePhase.Normal);
        bot.SpeedKph.Should().Be(0f);
        bot.PhysicsState.SpeedKph.Should().Be(0f);
        bot.PhysicsState.Gear.Should().Be(1);
        bot.EngineFrequency.Should().Be(bot.AudioProfile.IdleFrequency);
    }

    private static RoomRaceResultStatus[] SnapshotStatuses(GameRoom room)
    {
        var statuses = new RoomRaceResultStatus[room.ActiveRaceParticipantIds.Count];
        var index = 0;
        foreach (var id in room.ActiveRaceParticipantIds)
        {
            statuses[index++] = room.RaceParticipantResults.TryGetValue(id, out var result)
                ? result.Status
                : RoomRaceResultStatus.Pending;
        }

        return statuses;
    }
}

