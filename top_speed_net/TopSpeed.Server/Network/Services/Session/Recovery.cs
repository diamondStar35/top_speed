using System;
using System.Linq;
using TopSpeed.Localization;
using TopSpeed.Protocol;
using TopSpeed.Server.Protocol;

namespace TopSpeed.Server.Network
{
    internal sealed partial class RaceServer
    {
        private sealed partial class Session
        {
            public PlayerConnection? ResolveResume(PlayerConnection pending, uint resumePlayerId, ulong resumeToken)
            {
                if (resumePlayerId == 0)
                {
                    return AcceptAsFreshSession(pending, fallbackReason: "new_session", preferredEvictionPlayerId: 0);
                }

                if (!_owner._players.TryGetValue(resumePlayerId, out var existing))
                {
                    // Treat invalid resume requests as a fresh connect instead of rejecting.
                    return AcceptAsFreshSession(pending, fallbackReason: "reconnect_token_invalid", preferredEvictionPlayerId: 0);
                }

                var now = DateTime.UtcNow;
                var resumeIdentityMatches = existing.Id == resumePlayerId
                                            && existing.ResumeToken != 0
                                            && existing.ResumeToken == resumeToken;
                var remoteIpMatches = Equals(existing.RemoteAddress, pending.EndPoint.Address);
                if (!remoteIpMatches || !resumeIdentityMatches)
                {
                    // Exact-IP resume is denied, but player is allowed to enter as a new session.
                    if (!remoteIpMatches
                        && resumeIdentityMatches
                        && ConnectionRecoveryRules.IsRecoverableState(existing.LifecycleState))
                    {
                        existing.MarkExpired();
                        RemoveConnection(
                            existing,
                            notifyRoom: true,
                            sendDisconnectPacket: false,
                            reason: "reconnect_ip_mismatch");
                    }

                    return AcceptAsFreshSession(
                        pending,
                        fallbackReason: !remoteIpMatches ? "reconnect_ip_mismatch" : "reconnect_token_invalid",
                        preferredEvictionPlayerId: existing.Id);
                }

                if (ConnectionRecoveryRules.IsGraceExpired(existing.LifecycleState, existing.SuspendedUtc, now, ReconnectGrace))
                {
                    return AcceptAsFreshSession(pending, fallbackReason: "reconnect_grace_expired", preferredEvictionPlayerId: existing.Id);
                }

                if (!ConnectionRecoveryRules.CanResume(
                        existing.LifecycleState,
                        existing.SuspendedUtc,
                        now,
                        ReconnectGrace,
                        existing.Id,
                        existing.ResumeToken,
                        resumePlayerId,
                        resumeToken,
                        remoteIpMatches: remoteIpMatches))
                {
                    return AcceptAsFreshSession(pending, fallbackReason: "reconnect_token_invalid", preferredEvictionPlayerId: existing.Id);
                }

                _owner._endpointIndex.Remove(pending.EndPoint.ToString());
                _owner._endpointEpochIndex.TryRemove(pending.EndPoint.ToString(), out _);
                _owner._players.Remove(pending.Id);
                _owner._endpointIndex.Remove(existing.EndPoint.ToString());
                _owner._endpointEpochIndex.TryRemove(existing.EndPoint.ToString(), out _);
                existing.MarkReconnecting();
                existing.Rebind(pending.EndPoint);
                existing.Handshake = HandshakeState.Pending;
                existing.ClientVersion = pending.ClientVersion;
                existing.ClientSupportedRange = pending.ClientSupportedRange;
                existing.NegotiatedProtocol = pending.NegotiatedProtocol;
                _owner._endpointIndex[existing.EndPoint.ToString()] = existing.Id;
                _owner._endpointEpochIndex[existing.EndPoint.ToString()] = existing.ConnectionEpoch;

                _owner._logger.Info(LocalizationService.Format(
                    LocalizationService.Mark("Connection resumed: playerId={0}, endpoint={1}, epoch={2}."),
                    existing.Id,
                    existing.EndPoint,
                    existing.ConnectionEpoch));
                return existing;
            }

            public void SendInitialConnectionState(PlayerConnection player)
            {
                _owner.SendStream(player, PacketSerializer.WritePlayerNumber(player.Id, player.PlayerNumber), PacketStream.Control);
                if (!string.IsNullOrWhiteSpace(_owner._config.Motd))
                    _owner.SendStream(player, PacketSerializer.WriteServerInfo(new PacketServerInfo { Motd = _owner._config.Motd }), PacketStream.Control);

                if (player.RoomId.HasValue && _owner._rooms.TryGetValue(player.RoomId.Value, out var room))
                {
                    _owner.SetRoomMemberPresence(room, player.Id, RoomMemberPresenceState.Active);
                    player.MarkInRoom();
                    if (room.PlayerIds.Contains(player.Id))
                    {
                        _owner.SendSelectedTrackToPlayer(room, player);
                        _owner.SyncMediaTo(room, player);
                        _owner.SyncLiveTo(room, player);
                    }
                    else
                    {
                        _owner._room.JoinPlayer(player, room);
                    }

                    SendResumeRaceState(player, room);
                    _owner._notify.SendRoomState(player, room);
                }
                else
                {
                    player.MarkSessionReady();
                    player.MarkActive();
                    _owner._room.HandleStateRequest(player);
                }

                // Voice chat is relayed server-wide (lobby + every room), so a
                // freshly-connected or resuming player needs to be told about
                // every currently-active voice stream regardless of room state.
                _owner.SyncVoiceTo(player);
                _owner.SyncCommunicatorMediaTo(player);

                if (player.RoomId.HasValue && _owner._rooms.ContainsKey(player.RoomId.Value))
                    player.MarkActive();

                _owner._logger.Info(LocalizationService.Format(
                    LocalizationService.Mark("Connection established: playerId={0}, endpoint={1}, protocol={2}."),
                    player.Id,
                    player.EndPoint,
                    player.NegotiatedProtocol));
            }

            private PlayerConnection? RejectResume(PlayerConnection pending, string reason)
            {
                RemoveConnection(
                    pending,
                    notifyRoom: false,
                    sendDisconnectPacket: true,
                    reason: reason,
                    disconnectMessage: LocalizationService.Mark("Unable to resume the previous multiplayer session."));
                return null;
            }

            private PlayerConnection? AcceptAsFreshSession(PlayerConnection pending, string fallbackReason, uint preferredEvictionPlayerId)
            {
                if (_owner._players.Count > _owner._config.MaxPlayers)
                {
                    if (!TryEvictRecoverableConnectionForCapacity(pending.Id, preferredEvictionPlayerId, fallbackReason))
                    {
                        RemoveConnection(
                            pending,
                            notifyRoom: false,
                            sendDisconnectPacket: true,
                            reason: "server_full",
                            disconnectMessage: LocalizationService.Mark("This server is full."));
                        return null;
                    }
                }

                _owner._logger.Info(LocalizationService.Format(
                    LocalizationService.Mark("Resume fallback accepted as new session: pendingPlayerId={0}, endpoint={1}, reason={2}."),
                    pending.Id,
                    pending.EndPoint,
                    fallbackReason));
                return pending;
            }

            private bool TryEvictRecoverableConnectionForCapacity(uint pendingPlayerId, uint preferredEvictionPlayerId, string reason)
            {
                PlayerConnection? candidate = null;
                if (preferredEvictionPlayerId != 0
                    && _owner._players.TryGetValue(preferredEvictionPlayerId, out var preferred)
                    && preferred.Id != pendingPlayerId
                    && ConnectionRecoveryRules.IsRecoverableState(preferred.LifecycleState))
                {
                    candidate = preferred;
                }

                if (candidate == null)
                {
                    candidate = _owner._players.Values
                        .Where(player => player.Id != pendingPlayerId && ConnectionRecoveryRules.IsRecoverableState(player.LifecycleState))
                        .OrderBy(player => player.SuspendedUtc ?? DateTime.MaxValue)
                        .FirstOrDefault();
                }

                if (candidate == null)
                    return false;

                candidate.MarkExpired();
                RemoveConnection(
                    candidate,
                    notifyRoom: true,
                    sendDisconnectPacket: false,
                    reason: reason);
                return _owner._players.Count <= _owner._config.MaxPlayers;
            }

            private void SendResumeRaceState(PlayerConnection player, GameRoom room)
            {
                if (room == null)
                    return;

                if (room.RaceState == RoomRaceState.Racing && room.ActiveRaceParticipantIds.Contains(player.Id))
                {
                    _owner.SendStream(player, PacketSerializer.WriteGeneral(Command.StartRace), PacketStream.RaceEvent);
                    var payload = _owner.BuildRaceSnapshotPayload(room);
                    if (payload != null)
                        _owner.SendStream(player, payload, PacketStream.RaceState, PacketDeliveryKind.ReliableOrdered);
                    _owner._notify.ReplayRoomEventsTo(player, room, afterSequence: 0);
                }
                else if (room.RaceState == RoomRaceState.Completed)
                {
                    _owner._notify.ReplayRoomEventsTo(player, room, afterSequence: 0);
                    _owner._notify.SendRaceCompletionTo(player, room);
                }
                else if (room.RaceState == RoomRaceState.Aborted)
                {
                    _owner._notify.ReplayRoomEventsTo(player, room, afterSequence: 0);
                }
                else
                {
                    _owner.SendStream(player, PacketSerializer.WriteRoomRaceStateChanged(new PacketRoomRaceStateChanged
                    {
                        RoomId = room.Id,
                        RoomVersion = room.Version,
                        EventSequence = room.EventSequence,
                        RaceInstanceId = room.RaceInstanceId,
                        State = room.RaceState,
                        EffectiveGameRulesFlags = room.RaceEffectiveGameRulesFlags
                    }), PacketStream.Room);
                }
            }
        }
    }
}


