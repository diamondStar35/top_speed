using System;
using TopSpeed.Data;
using TopSpeed.Network;
using TopSpeed.Protocol;

namespace TopSpeed.Game
{
    internal sealed partial class Game
    {
        private sealed partial class MultiplayerDispatch
        {
            private void RegisterRoom()
            {
                _reg.Add("room", Command.PlayerJoined, HandlePlayerJoined);
                _reg.Add("room", Command.LoadCustomTrack, HandleLoadCustomTrack);
                _reg.Add("room", Command.TrackPackageUploadResult, HandleTrackPackageUploadResult);
                _reg.Add("room", Command.TrackPackageCatalog, HandleTrackPackageCatalog);
                _reg.Add("room", Command.VehiclePackageCatalog, HandleVehiclePackageCatalog);
                _reg.Add("room", Command.VehiclePackageTransferBegin, HandleVehiclePackageTransferBegin);
                _reg.Add("room", Command.VehiclePackageTransferChunk, HandleVehiclePackageTransferChunk);
                _reg.Add("room", Command.VehiclePackageTransferEnd, HandleVehiclePackageTransferEnd);
                _reg.Add("room", Command.RoomPlayerVehicle, HandleRoomPlayerVehicle);
                _reg.Add("room", Command.TrackPackageTransferBegin, HandleTrackPackageTransferBegin);
                _reg.Add("room", Command.TrackPackageTransferChunk, HandleTrackPackageTransferChunk);
                _reg.Add("room", Command.TrackPackageTransferEnd, HandleTrackPackageTransferEnd);
                _reg.Add("room", Command.RoomList, HandleRoomList);
                _reg.Add("room", Command.RoomState, HandleRoomState);
                _reg.Add("room", Command.RoomEvent, HandleRoomEvent);
                _reg.Add("room", Command.RoomRaceStateChanged, HandleRoomRaceStateChanged);
                _reg.Add("room", Command.RoomRacePlayerFinished, HandleRoomRacePlayerFinished);
                _reg.Add("room", Command.RoomRaceCompleted, HandleRoomRaceCompleted);
                _reg.Add("room", Command.RoomRaceAborted, HandleRoomRaceAborted);
                _reg.Add("room", Command.OnlinePlayers, HandleOnlinePlayers);
            }

            private bool HandlePlayerJoined(IncomingPacket packet)
            {
                ClientPacketSerializer.TryReadPlayerJoined(packet.Payload, out _);
                return true;
            }

            private bool HandleLoadCustomTrack(IncomingPacket packet)
            {
                if (ClientPacketSerializer.TryReadLoadCustomTrack(packet.Payload, out var track))
                {
                    var name = string.IsNullOrWhiteSpace(track.TrackName) ? "custom" : track.TrackName;
                    var userDefined = string.Equals(name, "custom", StringComparison.OrdinalIgnoreCase);
                    _owner._multiplayerRaceRuntime.SetTrack(
                        new TrackData(
                            userDefined,
                            track.DefaultWeatherProfileId,
                            track.WeatherProfiles,
                            track.TrackAmbience,
                            track.Definitions),
                        name,
                        track.NrOfLaps);
                    if (_owner._multiplayerRaceRuntime.PendingStart)
                        _owner.StartMultiplayerRace();
                }

                return true;
            }

            private bool HandleTrackPackageUploadResult(IncomingPacket packet)
            {
                if (ClientPacketSerializer.TryReadTrackPackageUploadResult(packet.Payload, out var result))
                    _owner._multiplayerCoordinator.HandleTrackPackageUploadResult(result);
                return true;
            }

            private bool HandleTrackPackageCatalog(IncomingPacket packet)
            {
                if (ClientPacketSerializer.TryReadTrackPackageCatalog(packet.Payload, out var catalog))
                    _owner._multiplayerCoordinator.HandleTrackPackageCatalog(catalog);
                return true;
            }

            private bool HandleVehiclePackageCatalog(IncomingPacket packet)
            {
                if (ClientPacketSerializer.TryReadVehiclePackageCatalog(packet.Payload, out var catalog))
                    _owner._multiplayerCoordinator.HandleVehiclePackageCatalog(catalog);
                return true;
            }

            private bool HandleVehiclePackageTransferBegin(IncomingPacket packet)
            {
                if (ClientPacketSerializer.TryReadVehiclePackageTransferBegin(packet.Payload, out var begin))
                    _owner.HandleVehiclePackageTransferBegin(begin);
                return true;
            }

            private bool HandleVehiclePackageTransferChunk(IncomingPacket packet)
            {
                if (ClientPacketSerializer.TryReadVehiclePackageTransferChunk(packet.Payload, out var chunk))
                    _owner.HandleVehiclePackageTransferChunk(chunk);
                return true;
            }

            private bool HandleVehiclePackageTransferEnd(IncomingPacket packet)
            {
                if (ClientPacketSerializer.TryReadVehiclePackageTransferEnd(packet.Payload, out var end))
                    _owner.HandleVehiclePackageTransferEnd(end);
                return true;
            }

            private bool HandleRoomPlayerVehicle(IncomingPacket packet)
            {
                if (ClientPacketSerializer.TryReadRoomPlayerVehicle(packet.Payload, out var vehicle))
                    _owner.HandleRoomPlayerVehicle(vehicle);
                return true;
            }

            private bool HandleTrackPackageTransferBegin(IncomingPacket packet)
            {
                if (ClientPacketSerializer.TryReadTrackPackageTransferBegin(packet.Payload, out var begin))
                    _owner.HandleTrackPackageTransferBegin(begin);
                return true;
            }

            private bool HandleTrackPackageTransferChunk(IncomingPacket packet)
            {
                if (ClientPacketSerializer.TryReadTrackPackageTransferChunk(packet.Payload, out var chunk))
                    _owner.HandleTrackPackageTransferChunk(chunk);
                return true;
            }

            private bool HandleTrackPackageTransferEnd(IncomingPacket packet)
            {
                if (ClientPacketSerializer.TryReadTrackPackageTransferEnd(packet.Payload, out var end))
                    _owner.HandleTrackPackageTransferEnd(end);
                return true;
            }

            private bool HandleRoomList(IncomingPacket packet)
            {
                if (ClientPacketSerializer.TryReadRoomList(packet.Payload, out var roomList))
                    _owner._multiplayerCoordinator.HandleRoomList(roomList);
                return true;
            }

            private bool HandleRoomState(IncomingPacket packet)
            {
                if (ClientPacketSerializer.TryReadRoomState(packet.Payload, out var roomState))
                {
                    _owner.SetMultiplayerRoomLaps(roomState.Laps);
                    _owner.TryApplyCachedTrackPackage(roomState.Track);
                    _owner._multiplayerRaceRuntime.ApplyRoomState(roomState);

                    if (_owner._multiplayerRaceRuntime.Mode != null && _owner._multiplayerRaceRuntime.MatchesRoom(roomState.RoomId))
                    {
                        if (!roomState.InRoom)
                        {
                            _owner._multiplayerRaceRuntime.Mode.HandleServerRaceAborted();
                        }
                        else if (_owner._multiplayerRaceRuntime.MatchesContext(
                            roomState.RoomId,
                            roomState.RaceInstanceId,
                            roomState.RaceState == RoomRaceState.Preparing || roomState.RaceState == RoomRaceState.Racing))
                        {
                            if (roomState.RaceState == RoomRaceState.Racing)
                            {
                                _owner._multiplayerRaceRuntime.Mode.SetHostPaused(roomState.RacePaused);
                                _owner._multiplayerRaceRuntime.Mode.SyncParticipants(roomState);
                            }
                            else if (roomState.RaceState == RoomRaceState.Completed
                                     && !_owner._multiplayerRaceRuntime.Mode.ServerStopReceived)
                            {
                                _owner.RequestMultiplayerRoomResync();
                            }
                            else if (roomState.RaceState == RoomRaceState.Aborted || roomState.RaceState == RoomRaceState.Lobby)
                                _owner._multiplayerRaceRuntime.Mode.HandleServerRaceAborted();
                        }
                        else if (roomState.RaceState == RoomRaceState.Racing && roomState.RaceInstanceId == 0)
                        {
                            _owner._multiplayerRaceRuntime.Mode.SetHostPaused(roomState.RacePaused);
                            _owner._multiplayerRaceRuntime.Mode.SyncParticipants(roomState);
                        }
                    }

                    _owner._multiplayerCoordinator.HandleRoomState(roomState);
                }
                return true;
            }

            private bool HandleRoomRaceStateChanged(IncomingPacket packet)
            {
                if (ClientPacketSerializer.TryReadRoomRaceStateChanged(packet.Payload, out var changed))
                {
                    _owner._multiplayerCoordinator.HandleRoomRaceStateChanged(changed);
                    if (!_owner._multiplayerRaceRuntime.ApplyRaceState(changed))
                    {
                        if (_owner._multiplayerRaceRuntime.ShouldRequestResync(changed.RoomId, changed.RaceInstanceId, changed.EventSequence))
                            _owner.RequestMultiplayerRoomResync();
                        return true;
                    }

                    if (_owner._multiplayerRaceRuntime.Mode != null
                        && _owner._multiplayerRaceRuntime.MatchesContext(changed.RoomId, changed.RaceInstanceId, allowBindRaceInstance: true)
                        && (changed.State == RoomRaceState.Aborted || changed.State == RoomRaceState.Lobby))
                    {
                        _owner._multiplayerRaceRuntime.Mode.HandleServerRaceAborted();
                    }
                }

                return true;
            }

            private bool HandleRoomEvent(IncomingPacket packet)
            {
                if (ClientPacketSerializer.TryReadRoomEvent(packet.Payload, out var roomEvent))
                {
                    _owner.SetMultiplayerRoomLaps(roomEvent.Laps);
                    _owner.TryApplyCachedTrackPackage(roomEvent.Track);
                    if (_owner._multiplayerRaceRuntime.Mode != null
                        && _owner._multiplayerRaceRuntime.MatchesContext(
                            roomEvent.RoomId,
                            roomEvent.RaceInstanceId,
                            roomEvent.RaceState == RoomRaceState.Racing))
                    {
                        if (roomEvent.Kind == RoomEventKind.ParticipantLeft || roomEvent.Kind == RoomEventKind.BotRemoved)
                        {
                            var session = _owner._session;
                            if (session == null || roomEvent.SubjectPlayerNumber != session.PlayerNumber)
                                _owner._multiplayerRaceRuntime.Mode.RemoveRemotePlayer(roomEvent.SubjectPlayerNumber);
                        }
                    }

                    _owner._multiplayerCoordinator.HandleRoomEvent(roomEvent);
                }
                return true;
            }

            private bool HandleRoomRacePlayerFinished(IncomingPacket packet)
            {
                if (_owner._multiplayerRaceRuntime.Mode == null)
                    return true;

                if (ClientPacketSerializer.TryReadRoomRacePlayerFinished(packet.Payload, out var finished))
                {
                    if (_owner._multiplayerRaceRuntime.AcceptRaceEvent(finished.RoomId, finished.RaceInstanceId, finished.EventSequence, allowBindRaceInstance: true))
                        _owner._multiplayerRaceRuntime.Mode.ApplyRemoteFinish(finished.PlayerNumber, finished.FinishOrder);
                    else if (_owner._multiplayerRaceRuntime.ShouldRequestResync(finished.RoomId, finished.RaceInstanceId, finished.EventSequence))
                        _owner.RequestMultiplayerRoomResync();
                }

                return true;
            }

            private bool HandleRoomRaceCompleted(IncomingPacket packet)
            {
                if (_owner._multiplayerRaceRuntime.Mode == null)
                    return true;

                if (ClientPacketSerializer.TryReadRoomRaceCompleted(packet.Payload, out var completed))
                {
                    if (_owner._multiplayerRaceRuntime.AcceptRaceEvent(completed.RoomId, completed.RaceInstanceId, completed.EventSequence, allowBindRaceInstance: true))
                    {
                        _owner._multiplayerRaceRuntime.Mode.HandleServerRaceCompleted(completed);
                        // Defer the keep prompt until the race exits back to the menu (see field docs).
                        _owner._pendingVehicleKeepPromptAfterRace = true;
                    }
                    else if (_owner._multiplayerRaceRuntime.ShouldRequestResync(completed.RoomId, completed.RaceInstanceId, completed.EventSequence))
                        _owner.RequestMultiplayerRoomResync();
                }

                return true;
            }

            private bool HandleRoomRaceAborted(IncomingPacket packet)
            {
                if (_owner._multiplayerRaceRuntime.Mode == null)
                    return true;

                if (ClientPacketSerializer.TryReadRoomRaceAborted(packet.Payload, out var aborted))
                {
                    if (_owner._multiplayerRaceRuntime.AcceptRaceEvent(aborted.RoomId, aborted.RaceInstanceId, aborted.EventSequence, allowBindRaceInstance: true))
                        _owner._multiplayerRaceRuntime.Mode.HandleServerRaceAborted();
                    else if (_owner._multiplayerRaceRuntime.ShouldRequestResync(aborted.RoomId, aborted.RaceInstanceId, aborted.EventSequence))
                        _owner.RequestMultiplayerRoomResync();
                }

                return true;
            }

            private bool HandleOnlinePlayers(IncomingPacket packet)
            {
                if (ClientPacketSerializer.TryReadOnlinePlayers(packet.Payload, out var onlinePlayers))
                    _owner._multiplayerCoordinator.HandleOnlinePlayers(onlinePlayers);
                return true;
            }
        }
    }
}
