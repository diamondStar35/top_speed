using TopSpeed.Localization;
using TopSpeed.Protocol;
using TopSpeed.Server.Protocol;

namespace TopSpeed.Server.Network
{
    internal sealed partial class RaceServer
    {
        private sealed partial class Room
        {
            public void SetPlayersToStart(PlayerConnection player, PacketRoomSetPlayersToStart packet)
            {
                if (!TryGetHosted(player, out var room))
                    return;
                if (room.RaceStarted || room.PreparingRace)
                {
                    _owner._roomMutationDenied++;
                    _owner._logger.Debug(LocalizationService.Format(
                        LocalizationService.Mark("Room player-limit change denied: room={0}, player={1}, raceStarted={2}, preparing={3}."),
                        room.Id,
                        player.Id,
                        room.RaceStarted,
                        room.PreparingRace));
                    _owner.SendProtocolMessage(player, ProtocolMessageCode.Failed, LocalizationService.Mark("Cannot change player limit while race setup or race is active."));
                    return;
                }

                var value = packet.PlayersToStart;
                if (value < 2 || value > ProtocolConstants.MaxRoomPlayersToStart)
                {
                    _owner.SendProtocolMessage(player, ProtocolMessageCode.InvalidPlayersToStart, LocalizationService.Mark("Player limit must be between 2 and 10."));
                    return;
                }

                if (room.RoomType == GameRoomType.OneOnOne && value != 2)
                {
                    _owner.SendProtocolMessage(player, ProtocolMessageCode.InvalidPlayersToStart, LocalizationService.Mark("One-on-one rooms always allow a maximum of 2 players."));
                    return;
                }

                value = RoomRules.NormalizePlayersToStart(room.RoomType, value);
                if (GetRoomParticipantCount(room) > value)
                {
                    _owner.SendProtocolMessage(player, ProtocolMessageCode.InvalidPlayersToStart, LocalizationService.Mark("Cannot set lower than current players in room."));
                    return;
                }

                room.PlayersToStart = value;
                TouchVersion(room);
                _owner._notify.RoomLifecycle(room, RoomEventKind.PlayersToStartChanged);
                _owner._notify.RoomLifecycle(room, RoomEventKind.RoomSummaryUpdated);
            }

            public void SetGameRules(PlayerConnection player, PacketRoomSetGameRules packet)
            {
                if (!TryGetHosted(player, out var room))
                    return;
                if (room.RaceStarted || room.PreparingRace)
                {
                    _owner._roomMutationDenied++;
                    _owner._logger.Debug(LocalizationService.Format(
                        LocalizationService.Mark("Room game-rules change denied: room={0}, player={1}, raceStarted={2}, preparing={3}."),
                        room.Id,
                        player.Id,
                        room.RaceStarted,
                        room.PreparingRace));
                    _owner.SendProtocolMessage(player, ProtocolMessageCode.Failed, LocalizationService.Mark("Cannot change game rules while race setup or race is active."));
                    return;
                }

                var requestedFlags = packet.GameRulesFlags;
                if (!_owner._config.Features.CustomTracks
                    && (requestedFlags & (uint)RoomGameRules.CustomTracks) != 0u)
                {
                    _owner.SendProtocolMessage(player, ProtocolMessageCode.Failed, LocalizationService.Mark("Custom tracks are disabled on this server."));
                    return;
                }

                if (!_owner._config.Features.CustomVehicles
                    && (requestedFlags & (uint)RoomGameRules.CustomVehicles) != 0u)
                {
                    _owner.SendProtocolMessage(player, ProtocolMessageCode.Failed, LocalizationService.Mark("Custom vehicles are disabled on this server."));
                    return;
                }

                var allowedFlags = GameRulesPolicy.ResolveAllowedGameRules(_owner._config.Features);
                var normalizedFlags = requestedFlags & allowedFlags;
                if (room.GameRulesFlags == normalizedFlags)
                    return;

                room.GameRulesFlags = normalizedFlags;
                TouchVersion(room);
                _owner._notify.RoomLifecycle(room, RoomEventKind.GameRulesChanged);

                if (!_owner._config.Features.CustomTracks || !IsCustomSelectionEnabled(room))
                    _owner.SendPackageCatalogToRoom(room, new PacketTrackPackageCatalog());
                else
                    _owner.SendPackageCatalogToRoom(room, _owner.BuildTrackPackageCatalog());
            }
        }
    }
}
