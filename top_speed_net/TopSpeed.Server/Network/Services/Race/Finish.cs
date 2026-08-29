using TopSpeed.Localization;
using TopSpeed.Protocol;
using TopSpeed.Server.Bots;

namespace TopSpeed.Server.Network
{
    internal sealed partial class RaceServer
    {
        private sealed partial class Race
        {
            public bool ResolveHumanFinish(GameRoom room, PlayerConnection player, out byte finishOrder)
            {
                player.State = PlayerState.Finished;
                var raceDistance = RaceServer.GetRaceDistance(room);
                if (raceDistance > 0f && player.PositionY < raceDistance)
                    player.PositionY = raceDistance;

                return ResolveParticipantFinish(room, player.Id, player.PlayerNumber, CaptureFinishTimeMs(room), out finishOrder);
            }

            public bool ResolveBotFinish(GameRoom room, RoomBot bot, float finishY, out byte finishOrder)
            {
                ServerBotFinish.BeginStop(bot, finishY);
                return ResolveParticipantFinish(room, bot.Id, bot.PlayerNumber, CaptureFinishTimeMs(room), out finishOrder);
            }

            private bool ResolveParticipantFinish(GameRoom room, uint playerId, byte playerNumber, int finishTimeMs, out byte finishOrder)
            {
                finishOrder = 0;
                if (!TryMarkParticipantFinished(room, playerId, playerNumber, finishTimeMs, out finishOrder))
                {
                    _owner._logger.Debug(LocalizationService.Format(
                        LocalizationService.Mark("Finish ignored: room={0}, player={1}, number={2}."),
                        room.Id,
                        playerId,
                        playerNumber));
                    UpdateStopState(room);
                    return false;
                }

                var finishedCount = 0;
                foreach (var result in room.RaceParticipantResults.Values)
                {
                    if (result.Status == RoomRaceResultStatus.Finished)
                        finishedCount++;
                }

                _owner._logger.Debug(LocalizationService.Format(
                    LocalizationService.Mark("Finish recorded: room={0}, player={1}, number={2}, place={3}, timeMs={4}."),
                    room.Id,
                    playerId,
                    playerNumber,
                    finishOrder,
                    finishTimeMs));
                _owner._notify.RacePlayerFinished(room, playerId, playerNumber, finishOrder, finishTimeMs);
                if (finishedCount < room.ActiveRaceParticipantIds.Count)
                {
                    _owner._logger.Debug(LocalizationService.Format(
                        LocalizationService.Mark("Race completion pending: room={0}, finished={1}/{2}."),
                        room.Id,
                        finishedCount,
                        room.ActiveRaceParticipantIds.Count));
                }

                UpdateStopState(room);
                return true;
            }
        }
    }
}

