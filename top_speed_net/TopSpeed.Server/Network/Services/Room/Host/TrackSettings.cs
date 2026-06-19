using TopSpeed.Localization;
using TopSpeed.Protocol;
using TopSpeed.Server.Protocol;

namespace TopSpeed.Server.Network
{
    internal sealed partial class RaceServer
    {
        private sealed partial class Room
        {
            public void SetTrack(PlayerConnection player, PacketRoomSetTrack packet)
            {
                if (!TryGetHosted(player, out var room))
                    return;
                if (room.RaceStarted || room.PreparingRace)
                {
                    _owner._roomMutationDenied++;
                    _owner._logger.Debug(LocalizationService.Format(
                        LocalizationService.Mark("Room track change denied: room={0}, player={1}, raceStarted={2}, preparing={3}."),
                        room.Id,
                        player.Id,
                        room.RaceStarted,
                        room.PreparingRace));
                    _owner.SendProtocolMessage(player, ProtocolMessageCode.Failed, LocalizationService.Mark("Cannot change track while race setup or race is active."));
                    return;
                }

                if (packet.Track == null || !PacketValidation.IsValidTrackPackageRef(packet.Track))
                {
                    _owner.SendProtocolMessage(player, ProtocolMessageCode.InvalidTrack, LocalizationService.Mark("Invalid track selection."));
                    return;
                }

                if (packet.Track.IsBuiltIn)
                {
                    var trackName = (packet.Track.BuiltInTrackKey ?? string.Empty).Trim();
                    if (string.IsNullOrWhiteSpace(trackName))
                    {
                        _owner.SendProtocolMessage(player, ProtocolMessageCode.InvalidTrack, LocalizationService.Mark("Track cannot be empty."));
                        return;
                    }

                    SetTrackData(room, trackName);
                }
                else
                {
                    if (!IsCustomSelectionEnabled(room))
                    {
                        _owner.SendProtocolMessage(player, ProtocolMessageCode.Failed, LocalizationService.Mark("Custom tracks are not enabled for this room."));
                        return;
                    }

                    if (!SetTrackData(room, packet.Track))
                    {
                        _owner.SendProtocolMessage(player, ProtocolMessageCode.InvalidTrack, LocalizationService.Mark("Custom track package is not available on server."));
                        return;
                    }
                }

                _owner.BroadcastSelectedTrackToRoom(room);
                TouchVersion(room);
                _owner._notify.RoomLifecycle(room, RoomEventKind.TrackChanged);
            }

            public void SetLaps(PlayerConnection player, PacketRoomSetLaps packet)
            {
                if (!TryGetHosted(player, out var room))
                    return;
                if (room.RaceStarted || room.PreparingRace)
                {
                    _owner._roomMutationDenied++;
                    _owner._logger.Debug(LocalizationService.Format(
                        LocalizationService.Mark("Room laps change denied: room={0}, player={1}, raceStarted={2}, preparing={3}."),
                        room.Id,
                        player.Id,
                        room.RaceStarted,
                        room.PreparingRace));
                    _owner.SendProtocolMessage(player, ProtocolMessageCode.Failed, LocalizationService.Mark("Cannot change laps while race setup or race is active."));
                    return;
                }

                if (packet.Laps < 1 || packet.Laps > 500)
                {
                    _owner.SendProtocolMessage(player, ProtocolMessageCode.InvalidLaps, LocalizationService.Mark("Laps must be between 1 and 500."));
                    return;
                }

                room.Laps = packet.Laps;
                if (room.TrackSelected)
                {
                    if (room.TrackSelection != null && room.TrackSelection.IsCustomPackage)
                    {
                        if (!SetTrackData(room, room.TrackSelection))
                            SetTrackData(room, "america");
                    }
                    else
                    {
                        SetTrackData(room, room.TrackName);
                    }
                }

                _owner.BroadcastSelectedTrackToRoom(room);
                TouchVersion(room);
                _owner._notify.RoomLifecycle(room, RoomEventKind.LapsChanged);
            }
        }
    }
}
