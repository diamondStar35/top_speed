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
            public void Leave(PlayerConnection player, bool notify)
            {
                if (!player.RoomId.HasValue)
                {
                    _owner.SendProtocolMessage(player, ProtocolMessageCode.NotInRoom, LocalizationService.Mark("You are not in a game room."));
                    return;
                }

                var roomId = player.RoomId.Value;
                _owner._trackPackageUploads.Remove(player.Id);
                if (!_owner._rooms.TryGetValue(roomId, out var room))
                {
                    player.RoomId = null;
                    player.Live = null;
                    player.Voice = null;
                    if (player.LifecycleState != ConnectionLifecycleState.Closed
                        && player.LifecycleState != ConnectionLifecycleState.Expired)
                    {
                        player.MarkSessionReady();
                    }
                    _owner._notify.SendRoomState(player, null);
                    return;
                }

                var oldNumber = player.PlayerNumber;
                var leftName = RaceServer.DescribePlayer(player);
                room.PlayerIds.Remove(player.Id);
                _owner.RemoveRoomMemberPresence(room, player.Id);
                if (room.RaceStarted)
                {
                    _owner._race.MarkParticipantDnf(room, player.Id, oldNumber);
                }

                var previousHostId = room.HostId;
                player.RoomId = null;
                player.PlayerNumber = 0;
                player.State = PlayerState.NotReady;
                if (player.LifecycleState != ConnectionLifecycleState.Closed
                    && player.LifecycleState != ConnectionLifecycleState.Expired)
                {
                    player.MarkSessionReady();
                }
                room.PendingLoadouts.Remove(player.Id);
                room.PrepareSkips.Remove(player.Id);
                room.TrackReadyPlayers.Remove(player.Id);
                room.VehiclePackageReadyByPlayer.Remove(player.Id);
                room.MediaMap.Remove(player.Id);
                _owner.StopLive(player, room, notifyRoom: notify);
                player.IncomingMedia = null;
                player.MediaLoaded = false;
                player.MediaPlaying = false;
                player.MediaId = 0;
                player.RadioVolumePercent = 100;

                if (notify)
                {
                    var disconnectPayload = PacketSerializer.WritePlayer(Command.PlayerDisconnected, player.Id, oldNumber);
                    _owner._notify.ToRoom(room, disconnectPayload, PacketStream.RaceEvent);
                    _owner._notify.ToRoom(room, disconnectPayload, PacketStream.Room);
                    _owner._notify.ProtocolToRoom(
                        room,
                        LocalizationService.Format(
                            LocalizationService.Mark("{0} has left the game."),
                            leftName));
                }

                _owner._notify.SendRoomState(player, null);

                var roomClosed = room.PlayerIds.Count == 0;
                if (roomClosed)
                {
                    _owner._rooms.Remove(room.Id);
                }
                else
                {
                    if (room.HostId == player.Id)
                        room.HostId = room.PlayerIds.OrderBy(id => id).First();
                    if (room.RaceStarted)
                        _owner._race.UpdateStopState(room);
                    if (room.PreparingRace)
                        _owner._race.TryStartAfterLoadout(room);
                    CompactNumbers(room);
                    TouchVersion(room);
                    _owner._notify.RoomParticipant(room, RoomEventKind.ParticipantLeft, player.Id, oldNumber, PlayerState.NotReady, leftName);
                    if (previousHostId != room.HostId)
                    {
                        if (_owner._players.TryGetValue(room.HostId, out var newHost))
                            _owner.SendProtocolMessage(newHost, ProtocolMessageCode.Ok, LocalizationService.Mark("You are now host of this room."));
                        _owner._notify.RoomLifecycle(room, RoomEventKind.HostChanged);
                    }
                    _owner._notify.RoomLifecycle(room, RoomEventKind.RoomSummaryUpdated);
                    _owner._notify.BroadcastRoomState(room);
                }

                _owner._logger.Info(LocalizationService.Format(
                    LocalizationService.Mark("Player left room: room={0} \"{1}\", player={2}, notify={3}, roomClosed={4}."),
                    room.Id,
                    room.Name,
                    player.Id,
                    notify,
                    roomClosed));
                if (roomClosed)
                {
                    _owner._logger.Info(LocalizationService.Format(
                        LocalizationService.Mark("Room closed: room={0} \"{1}\", reason=last_participant_left."),
                        room.Id,
                        room.Name));
                }
            }
        }
    }
}
