using TopSpeed.Localization;
using TopSpeed.Protocol;
using TopSpeed.Server.Protocol;

namespace TopSpeed.Server.Network
{
    internal sealed partial class RaceServer
    {
        private sealed partial class Room
        {
            public void PlayerReady(PlayerConnection player, PacketRoomPlayerReady ready)
            {
                if (!player.RoomId.HasValue || !_owner._rooms.TryGetValue(player.RoomId.Value, out var room))
                {
                    _owner.SendProtocolMessage(player, ProtocolMessageCode.NotInRoom, LocalizationService.Mark("You are not in a game room."));
                    return;
                }

                if (!room.PreparingRace)
                {
                    _owner.SendProtocolMessage(player, ProtocolMessageCode.Failed, LocalizationService.Mark("Race setup has not started yet."));
                    return;
                }

                if (!room.PlayerIds.Contains(player.Id))
                {
                    _owner.SendProtocolMessage(player, ProtocolMessageCode.NotInRoom, LocalizationService.Mark("You are not in this game room."));
                    return;
                }
                if (!_owner.IsRoomMemberActive(room, player.Id))
                {
                    _owner.SendProtocolMessage(player, ProtocolMessageCode.Failed, LocalizationService.Mark("You are temporarily disconnected from this game room."));
                    return;
                }

                CarType selectedCar;
                var selectedVehicleHash = string.Empty;
                // A custom vehicle is only honoured when the server feature and the room's game rule
                // both allow it; otherwise the selection falls through to the built-in path below.
                if (ready.Vehicle != null && ready.Vehicle.IsCustomPackage
                    && IsCustomVehicleSelectionEnabled(room)
                    && _owner.TryGetVehiclePackage(ready.Vehicle.Hash, out var vehicleRecord))
                {
                    selectedCar = CarType.CustomVehicle;
                    selectedVehicleHash = vehicleRecord.Ref.Hash;
                    player.Car = selectedCar;
                    player.SelectedVehicleHash = selectedVehicleHash;
                    RaceServer.ApplyCustomVehicleDimensions(player, vehicleRecord);
                    // The package is deliberately not sent here. Broadcasting the selection below
                    // tells every client which vehicle to expect, and only those that do not already
                    // hold it ask for it. Sending it to the whole room regardless meant a player who
                    // had raced with the vehicle moments earlier downloaded it all over again.
                }
                else
                {
                    selectedCar = RaceServer.NormalizeNetworkCar(ready.Car);
                    player.Car = selectedCar;
                    player.SelectedVehicleHash = string.Empty;
                    RaceServer.ApplyVehicleDimensions(player, selectedCar);
                }

                _owner.BroadcastPlayerVehicle(room, player.PlayerNumber, selectedVehicleHash);
                room.PrepareSkips.Remove(player.Id);
                room.PendingLoadouts[player.Id] = new PlayerLoadout(selectedCar, ready.AutomaticTransmission, selectedVehicleHash);
                _owner._logger.Debug(LocalizationService.Format(
                    LocalizationService.Mark("Player ready: room={0}, player={1}, car={2}, automatic={3}, ready={4}/{5}."),
                    room.Id,
                    player.Id,
                    selectedCar,
                    ready.AutomaticTransmission,
                    room.PendingLoadouts.Count,
                    room.PlayerIds.Count));
                _owner._notify.ProtocolToRoom(
                    room,
                    LocalizationService.Format(
                        LocalizationService.Mark("{0} is ready."),
                        RaceServer.DescribePlayer(player)));
                _owner._race.TryStartAfterLoadout(room);
            }

            public void PlayerWithdraw(PlayerConnection player)
            {
                if (!player.RoomId.HasValue || !_owner._rooms.TryGetValue(player.RoomId.Value, out var room))
                {
                    _owner.SendProtocolMessage(player, ProtocolMessageCode.NotInRoom, LocalizationService.Mark("You are not in a game room."));
                    return;
                }

                if (!room.PreparingRace)
                {
                    _owner.SendProtocolMessage(player, ProtocolMessageCode.Failed, LocalizationService.Mark("Race setup has not started yet."));
                    return;
                }

                if (!room.PlayerIds.Contains(player.Id))
                {
                    _owner.SendProtocolMessage(player, ProtocolMessageCode.NotInRoom, LocalizationService.Mark("You are not in this game room."));
                    return;
                }
                if (!_owner.IsRoomMemberActive(room, player.Id))
                {
                    _owner.SendProtocolMessage(player, ProtocolMessageCode.Failed, LocalizationService.Mark("You are temporarily disconnected from this game room."));
                    return;
                }

                room.PendingLoadouts.Remove(player.Id);
                room.PrepareSkips.Add(player.Id);
                player.State = PlayerState.NotReady;
                TouchVersion(room);
                _owner._notify.RoomParticipant(
                    room,
                    RoomEventKind.ParticipantStateChanged,
                    player.Id,
                    player.PlayerNumber,
                    player.State,
                    string.IsNullOrWhiteSpace(player.Name)
                        ? LocalizationService.Format(LocalizationService.Mark("Player {0}"), player.PlayerNumber + 1)
                        : player.Name);
                _owner._notify.ProtocolToRoom(
                    room,
                    LocalizationService.Format(
                        LocalizationService.Mark("{0} left race preparation."),
                        RaceServer.DescribePlayer(player)));
                _owner._race.TryStartAfterLoadout(room);
            }
        }
    }
}
