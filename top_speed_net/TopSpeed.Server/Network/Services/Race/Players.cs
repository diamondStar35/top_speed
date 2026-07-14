using TopSpeed.Localization;
using TopSpeed.Protocol;
using TopSpeed.Server.Protocol;

namespace TopSpeed.Server.Network
{
    internal sealed partial class RaceServer
    {
        private sealed partial class Race
        {
            public void HandlePlayerState(PlayerConnection player, PacketRacePlayerState state)
            {
                if (!TryGetPlayerRoom(player, out var room))
                {
                    _owner._authorityDropsPlayerState++;
                    return;
                }
                if (!ValidatePacket(room, player, state.RaceInstanceId, state.PlayerId, state.PlayerNumber, ref _owner._authorityDropsPlayerState, nameof(Command.PlayerState)))
                    return;

                var previousState = player.State;
                var raceDistance = room.RaceState == RoomRaceState.Racing ? RaceServer.GetRaceDistance(room) : 0f;

                if (state.State == PlayerState.AwaitingStart || state.State == PlayerState.Racing || state.State == PlayerState.Finished)
                    player.State = state.State;
                else
                    _owner._authorityDropsPlayerState++;

                if (room.RaceState == RoomRaceState.Preparing && player.State != PlayerState.AwaitingStart)
                {
                    _owner._authorityDropsPlayerState++;
                    player.State = PlayerState.AwaitingStart;
                }
                if (previousState == PlayerState.Finished && player.State != PlayerState.Finished)
                {
                    _owner._authorityDropsPlayerState++;
                    player.State = PlayerState.Finished;
                }

                if (previousState != player.State)
                    _owner._logger.Debug(LocalizationService.Format(
                        LocalizationService.Mark("Player state transition: room={0}, player={1}, {2} -> {3} (packet={4})."),
                        room.Id,
                        player.Id,
                        previousState,
                        player.State,
                        state.State));
                if (previousState != player.State)
                {
                    _owner._room.TouchVersion(room);
                    _owner._notify.RoomParticipant(
                        room,
                        RoomEventKind.ParticipantStateChanged,
                        player.Id,
                        player.PlayerNumber,
                        player.State,
                        string.IsNullOrWhiteSpace(player.Name)
                            ? LocalizationService.Format(LocalizationService.Mark("Player {0}"), player.PlayerNumber + 1)
                            : player.Name);
                }

                if (room.RaceState == RoomRaceState.Racing && previousState != PlayerState.Finished && player.State == PlayerState.Finished)
                    ResolveHumanFinish(room, player, out _);
            }

            public void HandlePlayerData(PlayerConnection player, PacketRacePlayerData data)
            {
                if (!TryGetPlayerRoom(player, out var room))
                {
                    _owner._authorityDropsPlayerData++;
                    return;
                }
                if (!ValidatePacket(room, player, data.RaceInstanceId, data.PlayerId, data.PlayerNumber, ref _owner._authorityDropsPlayerData, nameof(Command.PlayerDataToServer)))
                    return;

                var previousState = player.State;
                var raceDistance = room.RaceState == RoomRaceState.Racing ? RaceServer.GetRaceDistance(room) : 0f;
                var incomingPositionX = data.RaceData.PositionX;
                var incomingPositionY = data.RaceData.PositionY;
                if (!PacketValidation.IsFinitePosition(incomingPositionX, incomingPositionY))
                {
                    _owner._authorityDropsPlayerData++;
                    return;
                }

                // A custom-vehicle player's car and dimensions are established authoritatively at
                // loadout (PlayerReady) and must NOT be taken from the per-frame data stream: the wire
                // CarType for a custom car is CustomVehicle, and NormalizeNetworkCar collapses that to
                // Vehicle1, corrupting the server's record so every subsequent snapshot advertises the
                // default car to peers. Only built-in players derive their car from incoming data.
                if (string.IsNullOrEmpty(player.SelectedVehicleHash))
                {
                    player.Car = RaceServer.NormalizeNetworkCar(data.Car);
                    RaceServer.ApplyVehicleDimensions(player, player.Car);
                }
                player.PositionX = incomingPositionX;
                player.PositionY = incomingPositionY;
                player.Speed = data.RaceData.Speed;
                player.Frequency = data.RaceData.Frequency;
                player.EngineRunning = data.EngineRunning;
                player.Braking = data.Braking;
                player.Horning = data.Horning;
                player.Backfiring = data.Backfiring;
                _owner.UpdateMediaState(player, room, data);
                player.RadioVolumePercent = (byte)Math.Clamp((int)data.RadioVolumePercent, 0, 100);
                var nextState = data.State;

                if (nextState == PlayerState.Undefined || nextState == PlayerState.NotReady)
                {
                    _owner._authorityDropsPlayerData++;
                    nextState = player.State;
                }

                if (room.RaceState == RoomRaceState.Preparing)
                {
                    if (nextState != PlayerState.AwaitingStart)
                    {
                        _owner._authorityDropsPlayerData++;
                        nextState = PlayerState.AwaitingStart;
                    }
                }
                else if (nextState != PlayerState.AwaitingStart && nextState != PlayerState.Racing && nextState != PlayerState.Finished)
                {
                    _owner._authorityDropsPlayerData++;
                    nextState = player.State;
                }
                if (previousState == PlayerState.Finished && nextState != PlayerState.Finished)
                {
                    _owner._authorityDropsPlayerData++;
                    nextState = PlayerState.Finished;
                }
                if (room.RaceState == RoomRaceState.Racing
                    && previousState != PlayerState.Finished
                    && RaceDistanceRules.HasCrossedFinish(player.PositionY, raceDistance))
                {
                    nextState = PlayerState.Finished;
                }

                player.State = nextState;
                if (room.RaceState == RoomRaceState.Racing && previousState != PlayerState.Finished && nextState == PlayerState.Finished)
                    ResolveHumanFinish(room, player, out _);
                if (previousState != nextState)
                    _owner._logger.Debug(LocalizationService.Format(
                        LocalizationService.Mark("Player state transition from data: room={0}, player={1}, {2} -> {3}."),
                        room.Id,
                        player.Id,
                        previousState,
                        nextState));
            }

            public void HandlePlayerStarted(PlayerConnection player, PacketRacePlayer started)
            {
                if (!TryGetPlayerRoom(player, out var room))
                {
                    _owner._authorityDropsPlayerStarted++;
                    return;
                }
                if (!ValidatePacket(room, player, started.RaceInstanceId, started.PlayerId, started.PlayerNumber, ref _owner._authorityDropsPlayerStarted, nameof(Command.PlayerStarted)))
                    return;
                if (!room.RaceStarted)
                {
                    _owner._authorityDropsPlayerStarted++;
                    return;
                }
                if (room.RacePaused)
                {
                    _owner._authorityDropsPlayerStarted++;
                    return;
                }

                if (player.State == PlayerState.AwaitingStart || player.State == PlayerState.Racing)
                    player.State = PlayerState.Racing;
                else
                    _owner._authorityDropsPlayerStarted++;
            }

            public void HandlePlayerCrashed(PlayerConnection player, PacketRacePlayer crashed)
            {
                if (!TryGetPlayerRoom(player, out var room))
                {
                    _owner._authorityDropsPlayerCrashed++;
                    return;
                }
                if (!ValidatePacket(room, player, crashed.RaceInstanceId, crashed.PlayerId, crashed.PlayerNumber, ref _owner._authorityDropsPlayerCrashed, nameof(Command.PlayerCrashed)))
                    return;
                if (!room.RaceStarted)
                {
                    _owner._authorityDropsPlayerCrashed++;
                    return;
                }

                if (player.State != PlayerState.Racing && player.State != PlayerState.Finished)
                {
                    _owner._authorityDropsPlayerCrashed++;
                    return;
                }

                _owner._notify.ToRoomExcept(room, player.Id, PacketSerializer.WritePlayer(Command.PlayerCrashed, player.Id, player.PlayerNumber), PacketStream.RaceEvent);
            }

            public void MarkFinished(GameRoom room, PlayerConnection player)
            {
                ResolveHumanFinish(room, player, out _);
            }

            private bool TryGetPlayerRoom(PlayerConnection player, out GameRoom room)
            {
                if (player.RoomId.HasValue && _owner._rooms.TryGetValue(player.RoomId.Value, out var resolvedRoom) && resolvedRoom != null)
                {
                    room = resolvedRoom;
                    return true;
                }

                room = null!;
                return false;
            }

            private bool ValidatePacket(GameRoom room, PlayerConnection player, uint raceInstanceId, uint payloadPlayerId, byte payloadPlayerNumber, ref int dropCounter, string commandName)
            {
                if (!PacketValidation.IsValidPlayerId(payloadPlayerId)
                    || !PacketValidation.IsValidPlayerNumber(payloadPlayerNumber)
                    || !PacketValidation.IsValidRaceInstance(raceInstanceId))
                {
                    dropCounter++;
                    return false;
                }

                if (room.RaceState != RoomRaceState.Preparing && room.RaceState != RoomRaceState.Racing)
                {
                    dropCounter++;
                    return false;
                }

                if (payloadPlayerId != player.Id || payloadPlayerNumber != player.PlayerNumber)
                {
                    dropCounter++;
                    _owner._logger.Debug(LocalizationService.Format(
                        LocalizationService.Mark("{0} payload mismatch: room={1}, connectionPlayer={2}/{3}, payload={4}/{5}."),
                        commandName,
                        room.Id,
                        player.Id,
                        player.PlayerNumber,
                        payloadPlayerId,
                        payloadPlayerNumber));
                    return false;
                }

                if (raceInstanceId == 0 || raceInstanceId != room.RaceInstanceId)
                {
                    dropCounter++;
                    _owner._logger.Debug(LocalizationService.Format(
                        LocalizationService.Mark("{0} race instance mismatch: room={1}, connectionPlayer={2}/{3}, payloadRaceInstance={4}, roomRaceInstance={5}."),
                        commandName,
                        room.Id,
                        player.Id,
                        player.PlayerNumber,
                        raceInstanceId,
                        room.RaceInstanceId));
                    return false;
                }

                return true;
            }
        }
    }
}

