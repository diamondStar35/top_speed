using System.Linq;
using TopSpeed.Localization;
using TopSpeed.Protocol;
using TopSpeed.Server.Protocol;

namespace TopSpeed.Server.Network
{
    internal sealed partial class RaceServer
    {
        private sealed partial class Room
        {
            public void Create(PlayerConnection player, PacketRoomCreate packet)
            {
                var roomId = _owner.AllocateRoomId();
                var roomName = (packet.RoomName ?? string.Empty).Trim();
                if (string.IsNullOrWhiteSpace(roomName))
                    roomName = LocalizationService.Format(LocalizationService.Mark("Game {0}"), roomId);
                if (roomName.Length > ProtocolConstants.MaxRoomNameLength)
                    roomName = roomName.Substring(0, ProtocolConstants.MaxRoomNameLength);

                var roomType = RoomRules.NormalizeType(packet.RoomType);
                var playersToStart = RoomRules.NormalizePlayersToStart(roomType, packet.PlayersToStart);

                var room = new GameRoom(roomId, roomName, roomType, playersToStart);
                _owner._rooms[room.Id] = room;
                SetTrackData(room, room.TrackName);
                JoinPlayer(player, room);
                _owner._notify.RoomLifecycle(room, RoomEventKind.RoomCreated);
                _owner._notify.ProtocolToLobby(LocalizationService.Format(
                    LocalizationService.Mark("{0} created game room {1}."),
                    RaceServer.DescribePlayer(player),
                    room.Name));
                _owner._logger.Info(LocalizationService.Format(
                    LocalizationService.Mark("Room created: room={0} \"{1}\", host={2}, type={3}, playersToStart={4}."),
                    room.Id,
                    room.Name,
                    player.Id,
                    room.RoomType,
                    room.PlayersToStart));
            }

            public void Join(PlayerConnection player, PacketRoomJoin packet)
            {
                if (!_owner._rooms.TryGetValue(packet.RoomId, out var room))
                {
                    _owner.SendProtocolMessage(player, ProtocolMessageCode.RoomNotFound, LocalizationService.Mark("Game room not found."));
                    return;
                }

                if (player.RoomId == room.Id && room.PlayerIds.Contains(player.Id))
                {
                    _owner._notify.SendRoomState(player, room);
                    return;
                }

                if (room.RaceStarted || room.PreparingRace)
                {
                    _owner._joinDeniedGameInProgress++;
                    _owner._logger.Debug(LocalizationService.Format(
                        LocalizationService.Mark("Join denied: player={0}, room={1}, raceStarted={2}, preparing={3}."),
                        player.Id,
                        room.Id,
                        room.RaceStarted,
                        room.PreparingRace));
                    _owner.SendProtocolMessage(player, ProtocolMessageCode.Failed, LocalizationService.Mark("This game room is currently in progress."));
                    return;
                }

                if (GetRoomParticipantCount(room) >= room.PlayersToStart)
                {
                    _owner.SendProtocolMessage(player, ProtocolMessageCode.RoomFull, RoomTexts.RoomUnavailableFull);
                    return;
                }

                JoinPlayer(player, room);
                _owner._notify.RoomLifecycle(room, RoomEventKind.RoomSummaryUpdated);
                _owner._logger.Info(LocalizationService.Format(
                    LocalizationService.Mark("Player joined room: room={0} \"{1}\", player={2}, participants={3}/{4}."),
                    room.Id,
                    room.Name,
                    player.Id,
                    GetRoomParticipantCount(room),
                    room.PlayersToStart));
            }

            public void JoinPlayer(PlayerConnection player, GameRoom room)
            {
                if (player.RoomId == room.Id && room.PlayerIds.Contains(player.Id))
                {
                    _owner.SetRoomMemberPresence(room, player.Id, RoomMemberPresenceState.Active);
                    player.MarkInRoom();
                    _owner.SendStream(player, PacketSerializer.WritePlayerNumber(player.Id, player.PlayerNumber), PacketStream.Control);
                    _owner.SendSelectedTrackToPlayer(room, player);
                    _owner.SyncMediaTo(room, player);
                    _owner.SyncLiveTo(room, player);
                    // Voice is relayed server-wide and was already synced at
                    // SendInitialConnectionState, so we do not re-sync on room join.
                    _owner._notify.SendRoomState(player, room);
                    return;
                }

                if (player.RoomId.HasValue)
                    Leave(player, true);

                room.PlayerIds.Add(player.Id);
                _owner.SetRoomMemberPresence(room, player.Id, RoomMemberPresenceState.Active);
                if (room.HostId == 0 || !room.PlayerIds.Contains(room.HostId))
                    room.HostId = player.Id;

                player.RoomId = room.Id;
                player.MarkInRoom();
                player.PlayerNumber = byte.MaxValue;
                player.State = PlayerState.NotReady;
                room.PrepareSkips.Remove(player.Id);
                room.TrackReadyPlayers.Remove(player.Id);
                room.VehiclePackageReadyByPlayer.Remove(player.Id);
                CompactNumbers(room);

                _owner.SendStream(player, PacketSerializer.WritePlayerNumber(player.Id, player.PlayerNumber), PacketStream.Control);
                _owner.SendSelectedTrackToPlayer(room, player);
                _owner.SyncMediaTo(room, player);
                _owner.SyncLiveTo(room, player);
                // Voice is relayed server-wide and was already synced at
                // SendInitialConnectionState, so we do not re-sync on room join.
                TouchVersion(room);
                _owner._notify.SendRoomState(player, room);

                var joinedName = BuildRoomParticipantName(player);
                _owner._notify.RoomParticipant(
                    room,
                    RoomEventKind.ParticipantJoined,
                    player.Id,
                    player.PlayerNumber,
                    player.State,
                    joinedName);

                var joined = new PacketPlayerJoined { PlayerId = player.Id, PlayerNumber = player.PlayerNumber, Name = joinedName };
                _owner._notify.ToRoomExcept(room, player.Id, PacketSerializer.WritePlayerJoined(joined), PacketStream.Room);
                _owner._notify.BroadcastRoomState(room);
            }

            private static string BuildRoomParticipantName(PlayerConnection player)
            {
                return string.IsNullOrWhiteSpace(player.Name)
                    ? LocalizationService.Format(LocalizationService.Mark("Player {0}"), player.PlayerNumber + 1)
                    : player.Name;
            }
        }
    }
}
