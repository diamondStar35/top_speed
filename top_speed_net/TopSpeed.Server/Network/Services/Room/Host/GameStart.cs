using TopSpeed.Localization;
using TopSpeed.Protocol;
using TopSpeed.Server.Protocol;

namespace TopSpeed.Server.Network
{
    internal sealed partial class RaceServer
    {
        private sealed partial class Room
        {
            public void StartRoomGame(PlayerConnection player, uint disableGameRulesMask)
            {
                if (!TryGetHosted(player, out var room))
                    return;

                var minimumParticipants = _owner._race.GetMinimumParticipantsToStart(room);
                var activeParticipants = _owner.GetActiveParticipantCountForStartBarrier(room);
                if (activeParticipants < minimumParticipants)
                {
                    _owner._startBarrierBlockedInsufficientActive++;
                    _owner.SendProtocolMessage(
                        player,
                        ProtocolMessageCode.Failed,
                        LocalizationService.Format(
                            LocalizationService.Mark("Not enough players. {0} required to start."),
                            minimumParticipants));
                    return;
                }

                if (room.RaceStarted)
                {
                    _owner.SendProtocolMessage(player, ProtocolMessageCode.Failed, LocalizationService.Mark("A race is already in progress."));
                    return;
                }

                if (room.PreparingRace)
                {
                    _owner.SendProtocolMessage(player, ProtocolMessageCode.Failed, LocalizationService.Mark("Race setup is already in progress."));
                    return;
                }

                // The host can disable fuel consumption and/or tire wear for this race only (e.g.
                // when the chosen track has no pit area). That override applies to the race instance
                // without changing the room's persistent rules.
                var raceDisableMask = disableGameRulesMask
                    & ((uint)RoomGameRules.FuelConsumption | (uint)RoomGameRules.TireWear);
                room.RaceEffectiveGameRulesFlags = room.GameRulesFlags & ~raceDisableMask;

                _owner._race.TransitionRaceState(room, RoomRaceState.Preparing);
                room.PendingLoadouts.Clear();
                room.PrepareSkips.Clear();
                _owner.ResetRoomPackageReadiness(room);
                _owner._notify.RoomLifecycle(room, RoomEventKind.PrepareStarted);
                _owner._race.AssignRandomBotLoadouts(room);
                _owner._race.AnnounceBotsReady(room);
                _owner._logger.Info(LocalizationService.Format(
                    LocalizationService.Mark("Race prepare started: room={0} \"{1}\", requestedBy={2}, humans={3}, bots={4}, capacity={5}, minStart={6}."),
                    room.Id,
                    room.Name,
                    player.Id,
                    _owner.GetActiveHumanParticipantCount(room),
                    room.Bots.Count,
                    room.PlayersToStart,
                    minimumParticipants));

                _owner._notify.ProtocolToRoom(
                    room,
                    LocalizationService.Format(
                        LocalizationService.Mark("{0} is about to start the game. Choose your vehicle and transmission mode."),
                        RaceServer.DescribePlayer(player)));
                _owner._race.TryStartAfterLoadout(room);
            }
        }
    }
}
