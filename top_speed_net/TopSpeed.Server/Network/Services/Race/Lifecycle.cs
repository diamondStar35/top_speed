using System;
using System.Linq;
using LiteNetLib;
using TopSpeed.Bots;
using TopSpeed.Localization;
using TopSpeed.Protocol;
using TopSpeed.Server.Bots;
using TopSpeed.Server.Protocol;

namespace TopSpeed.Server.Network
{
    internal sealed partial class RaceServer
    {
        private sealed partial class Race
        {
            public void AssignRandomBotLoadouts(GameRoom room)
            {
                foreach (var bot in room.Bots)
                {
                    bot.Car = (CarType)_owner._random.Next((int)CarType.Vehicle1, (int)CarType.CustomVehicle);
                    bot.AutomaticTransmission = _owner._random.Next(0, 2) == 0;
                    RaceServer.ApplyVehicleDimensions(bot, bot.Car);
                }
            }

            public void AnnounceBotsReady(GameRoom room)
            {
                foreach (var bot in room.Bots.OrderBy(candidate => candidate.PlayerNumber))
                {
                    _owner._notify.ProtocolToRoom(
                        room,
                        LocalizationService.Format(
                            LocalizationService.Mark("{0} is ready."),
                            FormatBotJoinName(bot)));
                }
            }

            public int GetMinimumParticipantsToStart(GameRoom room)
            {
                if (room == null)
                    return 1;

                return 2;
            }

            public void TryStartAfterLoadout(GameRoom room)
            {
                if (!room.PreparingRace)
                    return;

                var minimumParticipants = GetMinimumParticipantsToStart(room);
                var activeHumanIds = _owner.EnumerateActiveHumanPlayerIds(room).ToArray();
                var activeParticipantsForBarrier = activeHumanIds.Length + room.Bots.Count;
                if (activeParticipantsForBarrier < minimumParticipants)
                {
                    _owner._startBarrierBlockedInsufficientActive++;
                    TransitionRaceState(room, RoomRaceState.Lobby);
                    room.RacePaused = false;
                    room.PendingLoadouts.Clear();
                    room.PrepareSkips.Clear();
                    _owner._notify.ProtocolToRoom(room, LocalizationService.Mark("Race start cancelled because there are not enough players."));
                    _owner._logger.Info(LocalizationService.Format(
                        LocalizationService.Mark("Race prepare cancelled: room={0} \"{1}\", participants={2}, minStart={3}, capacity={4}."),
                        room.Id,
                        room.Name,
                        activeParticipantsForBarrier,
                        minimumParticipants,
                        room.PlayersToStart));
                    return;
                }

                var readyHumans = CountReadyHumans(room);
                var skippedHumans = CountSkippedHumans(room);
                var unresolvedHumans = Math.Max(0, activeHumanIds.Length - (readyHumans + skippedHumans));
                if (unresolvedHumans > 0)
                {
                    _owner._startBarrierBlockedMissingReady++;
                    _owner._logger.Debug(LocalizationService.Format(
                        LocalizationService.Mark("Waiting for loadouts: room={0}, ready={1}, skipped={2}, totalHumans={3}."),
                        room.Id,
                        readyHumans,
                        skippedHumans,
                        activeHumanIds.Length));
                    return;
                }

                var activeHumanParticipantIds = activeHumanIds
                    .Where(id => room.PendingLoadouts.ContainsKey(id))
                    .ToArray();
                var activeParticipants = activeHumanParticipantIds.Length + room.Bots.Count;
                if (activeParticipants < minimumParticipants)
                {
                    _owner._startBarrierBlockedInsufficientActive++;
                    TransitionRaceState(room, RoomRaceState.Lobby);
                    room.RacePaused = false;
                    room.PendingLoadouts.Clear();
                    room.PrepareSkips.Clear();
                    _owner._notify.ProtocolToRoom(room, LocalizationService.Mark("Race start cancelled because there are not enough ready players."));
                    _owner._logger.Info(LocalizationService.Format(
                        LocalizationService.Mark("Race prepare cancelled after loadout: room={0} \"{1}\", active={2}, minStart={3}."),
                        room.Id,
                        room.Name,
                        activeParticipants,
                        minimumParticipants));
                    return;
                }

                if (!_owner.EnsureRoomPackageReady(room, activeHumanParticipantIds))
                {
                    _owner._startBarrierBlockedTrackNotReady++;
                    _owner._logger.Debug(LocalizationService.Format(
                        LocalizationService.Mark("Waiting for track package readiness: room={0}, trackHash={1}, ready={2}/{3}."),
                        room.Id,
                        room.TrackSelection?.Hash ?? string.Empty,
                        room.TrackReadyPlayers.Count(id => activeHumanParticipantIds.Contains(id)),
                        activeHumanParticipantIds.Length));
                    return;
                }

                _owner._notify.ProtocolToRoom(room, RoomTexts.AllPlayersReadyStartingGame);
                _owner._logger.Info(LocalizationService.Format(
                    LocalizationService.Mark("All loadouts ready: room={0} \"{1}\", starting race."),
                    room.Id,
                    room.Name));
                Start(room);
            }

            public void TransitionRaceState(GameRoom room, RoomRaceState nextState, RoomRaceAbortReason abortReason = RoomRaceAbortReason.None)
            {
                if (room == null || room.RaceState == nextState)
                    return;

                room.RaceState = nextState;
                _owner._room.TouchVersion(room);
                _owner._notify.RaceStateChanged(room);
                if (nextState == RoomRaceState.Aborted)
                    _owner._notify.RaceAborted(room, abortReason);
            }

            public void Abort(GameRoom room, RoomRaceAbortReason reason)
            {
                if (room == null)
                    return;
                if (room.RaceState != RoomRaceState.Preparing && room.RaceState != RoomRaceState.Racing)
                    return;

                FinalizeUnresolvedParticipantsAsDnf(room);
                room.ActiveRaceParticipantIds.Clear();
                room.PendingLoadouts.Clear();
                room.PrepareSkips.Clear();
                room.RacePaused = false;
                room.RaceStopPending = false;
                room.RaceStopDelaySeconds = 0f;
                room.RaceStartedUtc = default(DateTime);

                foreach (var id in room.PlayerIds)
                {
                    if (_owner._players.TryGetValue(id, out var player))
                        player.State = PlayerState.NotReady;
                }

                foreach (var bot in room.Bots)
                    bot.State = PlayerState.NotReady;

                TransitionRaceState(room, RoomRaceState.Aborted, reason);
                _owner._notify.RoomLifecycle(room, RoomEventKind.RoomSummaryUpdated);
                _owner._notify.BroadcastRoomState(room);
            }

            public void Start(GameRoom room)
            {
                if (room.RaceStarted)
                    return;

                _owner._room.ShuffleNumbersForGameStart(room);
                var activePlayerIds = _owner.EnumerateActiveHumanPlayerIds(room)
                    .Where(id => room.PendingLoadouts.ContainsKey(id))
                    .ToList();
                if (!room.TrackSelected || room.TrackData == null)
                    _owner._room.SetTrackData(room, room.TrackName);

                RoomEventJournal.ClearForRaceStart(room);
                room.RaceInstanceId++;
                TransitionRaceState(room, RoomRaceState.Racing);
                room.RacePaused = false;
                room.ActiveRaceParticipantIds.Clear();
                room.RaceResults.Clear();
                room.RaceFinishTimesMs.Clear();
                room.RaceParticipantResults.Clear();
                room.RaceStartedUtc = DateTime.UtcNow;
                room.RaceStopPending = false;
                room.RaceStopDelaySeconds = 0f;
                room.ActiveBumpPairs.Clear();
                room.RaceSnapshotSequence = 0;
                room.RaceSnapshotTick = 0;
                var laneHalfWidth = _owner.GetLaneHalfWidth(room);
                var rowSpacing = _owner.GetStartRowSpacing(room);

                foreach (var id in room.PlayerIds)
                {
                    if (!_owner._players.TryGetValue(id, out var player))
                        continue;

                    if (activePlayerIds.Contains(id))
                    {
                        room.ActiveRaceParticipantIds.Add(id);
                        player.State = PlayerState.AwaitingStart;
                        player.PositionX = RaceServer.CalculateStartX(player.PlayerNumber, player.WidthM, laneHalfWidth);
                        player.PositionY = RaceServer.CalculateStartY(player.PlayerNumber, rowSpacing);
                        player.Speed = 0;
                        player.Frequency = ProtocolConstants.DefaultFrequency;
                        player.EngineRunning = false;
                        player.Braking = false;
                        player.Horning = false;
                        player.Backfiring = false;
                    }
                    else
                    {
                        player.State = PlayerState.NotReady;
                    }
                }

                foreach (var bot in room.Bots)
                {
                    room.ActiveRaceParticipantIds.Add(bot.Id);
                    bot.State = PlayerState.AwaitingStart;
                    bot.RacePhase = BotRacePhase.Normal;
                    bot.CrashRecoverySeconds = 0f;
                    bot.SpeedKph = 0f;
                    bot.StartDelaySeconds = BotRaceStartDelaySeconds + _owner.GetBotReactionDelay(bot.Difficulty);
                    bot.EngineStartSecondsRemaining = 0f;
                    bot.EngineFrequency = bot.AudioProfile.IdleFrequency;
                    bot.Horning = false;
                    bot.HornSecondsRemaining = 0f;
                    bot.BackfireArmed = true;
                    bot.BackfirePulseSeconds = 0f;
                    bot.PositionX = RaceServer.CalculateStartX(bot.PlayerNumber, bot.WidthM, laneHalfWidth);
                    bot.PositionY = RaceServer.CalculateStartY(bot.PlayerNumber, rowSpacing);
                    bot.PhysicsState = new BotPhysicsState
                    {
                        PositionX = bot.PositionX,
                        PositionY = bot.PositionY,
                        SpeedKph = 0f,
                        Gear = 1,
                        AutoShiftCooldownSeconds = 0f,
                        TireWearFraction = 0f,
                        TireTemperatureC = float.NaN,
                        TireTreadTemperatureC = float.NaN,
                        TireCarcassTemperatureC = float.NaN,
                        TireSmoothedSlipNormalized = 0f,
                        SurfaceTemperatureC = float.NaN
                    };
                }

                InitializeParticipants(room);
                _owner.BroadcastSelectedTrackToRoom(room);
                var startPayload = PacketSerializer.WriteGeneral(Command.StartRace);
                foreach (var id in activePlayerIds)
                {
                    if (_owner._players.TryGetValue(id, out var player))
                        _owner.SendStream(player, startPayload, PacketStream.RaceEvent);
                }
                _owner.SendRaceSnapshot(room, DeliveryMethod.ReliableOrdered);
                _owner._room.TouchVersion(room);
                _owner._notify.RoomLifecycle(room, RoomEventKind.RaceStarted);
                _owner._notify.RoomLifecycle(room, RoomEventKind.RoomSummaryUpdated);
                _owner._notify.BroadcastRoomState(room);
                _owner._logger.Info(LocalizationService.Format(
                    LocalizationService.Mark("Race started: room={0} \"{1}\", track={2}, laps={3}, humans={4}, bots={5}."),
                    room.Id,
                    room.Name,
                    room.TrackName,
                    room.Laps,
                    activePlayerIds.Count,
                    room.Bots.Count));
                room.PendingLoadouts.Clear();
                room.PrepareSkips.Clear();
            }

            public void Stop(GameRoom room)
            {
                _owner._logger.Debug(LocalizationService.Format(
                    LocalizationService.Mark("Stopping race: room={0}, active={1}, trackedResults={2}."),
                    room.Id,
                    room.ActiveRaceParticipantIds.Count,
                    room.RaceParticipantResults.Count));
                FinalizeUnresolvedParticipantsAsDnf(room);
                if (!RaceCompletionInvariants.TryValidateTerminalResults(room, out var invariantReason))
                {
                    _owner._logger.Debug(LocalizationService.Format(
                        LocalizationService.Mark("Race completion invariant failed before emit: room={0}, reason={1}."),
                        room.Id,
                        invariantReason));
                }
                TransitionRaceState(room, RoomRaceState.Completed);
                room.RacePaused = false;
                room.PendingLoadouts.Clear();
                room.PrepareSkips.Clear();
                room.ActiveBumpPairs.Clear();
                room.ActiveRaceParticipantIds.Clear();
                _owner._notify.RaceCompleted(room);

                room.RaceStartedUtc = default(DateTime);
                room.RaceStopPending = false;
                room.RaceStopDelaySeconds = 0f;
                room.RaceSnapshotSequence = 0;
                room.RaceSnapshotTick = 0;
                foreach (var id in room.PlayerIds)
                {
                    if (_owner._players.TryGetValue(id, out var player))
                        player.State = PlayerState.NotReady;
                }
                foreach (var bot in room.Bots)
                {
                    bot.State = PlayerState.NotReady;
                    bot.RacePhase = BotRacePhase.Normal;
                    bot.CrashRecoverySeconds = 0f;
                    bot.SpeedKph = 0f;
                    bot.StartDelaySeconds = 0f;
                    bot.EngineStartSecondsRemaining = 0f;
                    bot.EngineFrequency = bot.AudioProfile.IdleFrequency;
                    bot.Horning = false;
                    bot.HornSecondsRemaining = 0f;
                    bot.BackfireArmed = true;
                    bot.BackfirePulseSeconds = 0f;
                    bot.PhysicsState = new BotPhysicsState
                    {
                        PositionX = bot.PositionX,
                        PositionY = bot.PositionY,
                        SpeedKph = 0f,
                        Gear = 1,
                        AutoShiftCooldownSeconds = 0f,
                        TireWearFraction = 0f,
                        TireTemperatureC = float.NaN,
                        TireTreadTemperatureC = float.NaN,
                        TireCarcassTemperatureC = float.NaN,
                        TireSmoothedSlipNormalized = 0f,
                        SurfaceTemperatureC = float.NaN
                    };
                }

                _owner._room.TouchVersion(room);
                _owner._notify.RoomLifecycle(room, RoomEventKind.RoomSummaryUpdated);
                _owner._notify.BroadcastRoomState(room);
                _owner._logger.Info(LocalizationService.Format(
                    LocalizationService.Mark("Race stopped: room={0} \"{1}\", results={2}."),
                    room.Id,
                    room.Name,
                    string.Join(",", room.RaceResults)));
            }

            private int CountReadyHumans(GameRoom room)
            {
                return room.PendingLoadouts.Keys.Count(id => _owner.IsRoomMemberActive(room, id));
            }

            private int CountSkippedHumans(GameRoom room)
            {
                return room.PrepareSkips.Count(id => _owner.IsRoomMemberActive(room, id));
            }
        }
    }
}


