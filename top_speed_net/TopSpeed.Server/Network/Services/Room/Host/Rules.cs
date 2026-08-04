using System.Collections.Generic;
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

                // Mask rules this server disallows instead of rejecting the whole request. Rejecting
                // discarded every other rule the host changed in the same visit, and reported only
                // the first offending rule. Masking keeps the rest and lets us name them all.
                var requestedFlags = packet.GameRulesFlags;
                var allowedFlags = GameRulesPolicy.ResolveAllowedGameRules(_owner._config.Features);
                var normalizedFlags = requestedFlags & allowedFlags;
                var disallowedFlags = requestedFlags & ~allowedFlags;
                if (disallowedFlags != 0u)
                    SendDisallowedGameRulesMessage(player, disallowedFlags);

                if (room.GameRulesFlags == normalizedFlags)
                {
                    // Nothing actually changed, but if the host asked for a disallowed rule, still
                    // re-announce the authoritative rules so their menu reverts to what is really set.
                    if (disallowedFlags != 0u)
                        _owner._notify.RoomLifecycle(room, RoomEventKind.GameRulesChanged);
                    return;
                }

                room.GameRulesFlags = normalizedFlags;
                TouchVersion(room);
                _owner._notify.RoomLifecycle(room, RoomEventKind.GameRulesChanged);

                if (!_owner._config.Features.CustomTracks || !IsCustomSelectionEnabled(room))
                    _owner.SendPackageCatalogToRoom(room, new PacketTrackPackageCatalog());
                else
                    _owner.SendPackageCatalogToRoom(room, _owner.BuildTrackPackageCatalog());
            }

            // Names every rule the host asked for that this server disallows, in one message, so a
            // host enabling several blocked rules at once hears about all of them rather than the
            // first alone.
            private void SendDisallowedGameRulesMessage(PlayerConnection player, uint disallowedFlags)
            {
                var names = new List<string>();
                if ((disallowedFlags & (uint)RoomGameRules.CustomTracks) != 0u)
                    names.Add(LocalizationService.Translate(LocalizationService.Mark("custom tracks")));
                if ((disallowedFlags & (uint)RoomGameRules.CustomVehicles) != 0u)
                    names.Add(LocalizationService.Translate(LocalizationService.Mark("custom vehicles")));
                if (names.Count == 0)
                    return;

                _owner.SendProtocolMessage(
                    player,
                    ProtocolMessageCode.Failed,
                    LocalizationService.Format(
                        LocalizationService.Mark("These features are disabled on this server: {0}."),
                        string.Join(", ", names)));
            }
        }
    }
}
