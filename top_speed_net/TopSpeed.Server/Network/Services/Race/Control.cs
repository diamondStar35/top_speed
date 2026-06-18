using TopSpeed.Bots;
using TopSpeed.Localization;
using TopSpeed.Protocol;
using TopSpeed.Server.Bots;

namespace TopSpeed.Server.Network
{
    internal sealed partial class RaceServer
    {
        private sealed partial class Race
        {
            public void CancelPrepare(GameRoom room, PlayerConnection host)
            {
                if (room == null || !room.PreparingRace)
                    return;

                room.RacePaused = false;
                room.PendingLoadouts.Clear();
                room.PrepareSkips.Clear();
                room.ActiveRaceParticipantIds.Clear();
                room.RaceStopPending = false;
                room.RaceStopDelaySeconds = 0f;
                room.RaceStartedUtc = default;

                foreach (var id in room.PlayerIds)
                {
                    if (_owner._players.TryGetValue(id, out var player))
                        player.State = PlayerState.NotReady;
                }

                foreach (var bot in room.Bots)
                    bot.State = PlayerState.NotReady;

                TransitionRaceState(room, RoomRaceState.Lobby);
                _owner._notify.ProtocolToRoomExcept(
                    room,
                    host.Id,
                    LocalizationService.Format(
                        LocalizationService.Mark("{0} canceled the current game."),
                        RaceServer.DescribePlayer(host)));
                _owner._notify.RoomLifecycle(room, RoomEventKind.PrepareCancelled);
                _owner._notify.RoomLifecycle(room, RoomEventKind.RoomSummaryUpdated);
                _owner._notify.BroadcastRoomState(room);
            }

            public void SetPaused(GameRoom room, PlayerConnection host, bool paused)
            {
                if (room == null || !room.RaceStarted)
                    return;

                room.RacePaused = paused;
                room.RaceStopPending = false;
                room.RaceStopDelaySeconds = 0f;

                _owner._room.TouchVersion(room);
                _owner._notify.ProtocolToRoom(
                    room,
                    LocalizationService.Format(
                        paused
                            ? LocalizationService.Mark("{0} paused the current game.")
                            : LocalizationService.Mark("{0} resumed the current game."),
                        RaceServer.DescribePlayer(host)));
                _owner._notify.RoomLifecycle(room, paused ? RoomEventKind.RacePaused : RoomEventKind.RaceResumed);
                _owner._notify.BroadcastRoomState(room);
            }

            public void StopWithoutResults(GameRoom room, PlayerConnection host)
            {
                if (room == null || !room.RaceStarted)
                    return;

                room.RacePaused = false;
                room.PendingLoadouts.Clear();
                room.PrepareSkips.Clear();
                room.ActiveRaceParticipantIds.Clear();
                room.RaceResults.Clear();
                room.RaceFinishTimesMs.Clear();
                room.RaceParticipantResults.Clear();
                room.ActiveBumpPairs.Clear();
                room.RaceStartedUtc = default;
                room.RaceStopPending = false;
                room.RaceStopDelaySeconds = 0f;
                room.RaceSnapshotSequence = 0;
                room.RaceSnapshotTick = 0;

                foreach (var id in room.PlayerIds)
                {
                    if (_owner._players.TryGetValue(id, out var player))
                    {
                        player.State = PlayerState.NotReady;
                        player.Speed = 0;
                        player.EngineRunning = false;
                        player.Braking = false;
                        player.Horning = false;
                        player.Backfiring = false;
                    }
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
                        TireSmoothedInputs = default,
                        SurfaceTemperatureC = float.NaN
                    };
                }

                TransitionRaceState(room, RoomRaceState.Lobby);
                _owner._notify.ProtocolToRoomExcept(
                    room,
                    host.Id,
                    LocalizationService.Format(
                        LocalizationService.Mark("{0} stopped the current game."),
                        RaceServer.DescribePlayer(host)));
                _owner._notify.RoomLifecycle(room, RoomEventKind.RaceStopped);
                _owner._notify.RoomLifecycle(room, RoomEventKind.RoomSummaryUpdated);
                _owner._notify.BroadcastRoomState(room);
            }
        }
    }
}

