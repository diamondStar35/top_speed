using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using LiteNetLib;
using TopSpeed.Protocol;

namespace TopSpeed.Server.Network
{
    internal sealed class ServerPlayerTestSnapshot
    {
        public uint PlayerId { get; set; }
        public byte PlayerNumber { get; set; }
        public ulong ResumeToken { get; set; }
        public uint? RoomId { get; set; }
        public HandshakeState Handshake { get; set; }
        public ConnectionLifecycleState LifecycleState { get; set; }
        public PlayerState State { get; set; }
        public bool HasVoice { get; set; }
        public uint VoiceStreamId { get; set; }
        public ushort VoiceFrequencyTenths { get; set; }
        public bool VoicePushToTalk { get; set; }
    }

    internal sealed class ServerStressSnapshot
    {
        public int PlayerCount { get; set; }
        public int RoomCount { get; set; }
        public int RacingRoomCount { get; set; }
        public int PreparingRoomCount { get; set; }
        public int CompletedRoomCount { get; set; }
        public int AbortedRoomCount { get; set; }
        public int ActiveRaceParticipantCount { get; set; }
        public int FinishedResultCount { get; set; }
        public int DnfResultCount { get; set; }
        public int UnresolvedResultCount { get; set; }
        public int CompletionInvariantFailureCount { get; set; }
        public int TotalJournalEventCount { get; set; }
        public int MaxJournalEventCount { get; set; }
        public int AuthorityDropCount { get; set; }
        public int RaceSnapshotSends { get; set; }
        public int StateSyncFramesSent { get; set; }
        public int StaleEpochDrops { get; set; }
        public int ReplayGapCount { get; set; }
        public int EpochRejectCount { get; set; }
        public int HeartbeatSuspicionCount { get; set; }
        public int StartBarrierBlockedInsufficientActive { get; set; }
        public int StartBarrierBlockedMissingReady { get; set; }
        public int StartBarrierBlockedTrackNotReady { get; set; }
    }

    internal sealed partial class RaceServer
    {
        internal void AddRoomForTest(GameRoom room, params PlayerConnection[] players)
        {
            lock (_lock)
            {
                _rooms.Add(room.Id, room);
                foreach (var player in players)
                {
                    _players.Add(player.Id, player);
                    room.PlayerIds.Add(player.Id);
                    player.RoomId = room.Id;
                }
            }
        }

        internal void InjectPacketForTest(IPEndPoint endPoint, byte[] payload)
        {
            OnPacket(endPoint, payload);
            lock (_lock)
            {
                EnsureServerLoopThreadUnsafe();
                DrainCommandQueueUnsafe();
            }
        }

        internal void DisconnectPeerForTest(IPEndPoint endPoint)
        {
            OnPeerDisconnected(
                endPoint,
                DisconnectMapping.FromTransportReason(DisconnectReason.Timeout),
                DisconnectReason.Timeout,
                SocketError.TimedOut);
            lock (_lock)
            {
                EnsureServerLoopThreadUnsafe();
                DrainCommandQueueUnsafe();
            }
        }

        internal ServerPlayerTestSnapshot GetPlayerSnapshotForTest(uint playerId)
        {
            lock (_lock)
            {
                if (!_players.TryGetValue(playerId, out var player))
                    throw new InvalidOperationException("Player was not found.");

                return new ServerPlayerTestSnapshot
                {
                    PlayerId = player.Id,
                    PlayerNumber = player.PlayerNumber,
                    ResumeToken = player.ResumeToken,
                    RoomId = player.RoomId,
                    Handshake = player.Handshake,
                    LifecycleState = player.LifecycleState,
                    State = player.State,
                    HasVoice = player.Voice != null,
                    VoiceStreamId = player.Voice?.StreamId ?? 0,
                    VoiceFrequencyTenths = player.Voice?.FrequencyTenths ?? 0,
                    VoicePushToTalk = player.Voice?.PushToTalk ?? false
                };
            }
        }

        internal uint GetRoomRaceInstanceForTest(uint roomId)
        {
            lock (_lock)
            {
                return _rooms.TryGetValue(roomId, out var room) ? room.RaceInstanceId : 0;
            }
        }

        internal ServerStressSnapshot GetStressSnapshotForTest()
        {
            lock (_lock)
            {
                var snapshot = new ServerStressSnapshot
                {
                    PlayerCount = _players.Count,
                    RoomCount = _rooms.Count,
                    PreparingRoomCount = _rooms.Values.Count(room => room.RaceState == RoomRaceState.Preparing),
                    RacingRoomCount = _rooms.Values.Count(room => room.RaceState == RoomRaceState.Racing),
                    CompletedRoomCount = _rooms.Values.Count(room => room.RaceState == RoomRaceState.Completed),
                    AbortedRoomCount = _rooms.Values.Count(room => room.RaceState == RoomRaceState.Aborted),
                    ActiveRaceParticipantCount = _rooms.Values.Sum(room => room.ActiveRaceParticipantIds.Count),
                    TotalJournalEventCount = _rooms.Values.Sum(room => room.EventJournal.Count),
                    MaxJournalEventCount = _rooms.Values.Select(room => room.EventJournal.Count).DefaultIfEmpty(0).Max(),
                    AuthorityDropCount =
                        _authorityDropsPlayerState +
                        _authorityDropsPlayerData +
                        _authorityDropsPlayerStarted +
                        _authorityDropsPlayerCrashed,
                    RaceSnapshotSends = _raceSnapshotSends,
                    StateSyncFramesSent = _stateSyncFramesSent,
                    StaleEpochDrops = _droppedPacketsStaleEpoch,
                    ReplayGapCount = _replayGapCount,
                    EpochRejectCount = _epochRejectCount,
                    HeartbeatSuspicionCount = _heartbeatSuspicionCount,
                    StartBarrierBlockedInsufficientActive = _startBarrierBlockedInsufficientActive,
                    StartBarrierBlockedMissingReady = _startBarrierBlockedMissingReady,
                    StartBarrierBlockedTrackNotReady = _startBarrierBlockedTrackNotReady
                };

                foreach (var room in _rooms.Values)
                {
                    if (room.RaceState == RoomRaceState.Completed
                        && !RaceCompletionInvariants.TryValidateTerminalResults(room, out _))
                    {
                        snapshot.CompletionInvariantFailureCount++;
                    }

                    foreach (var result in room.RaceParticipantResults.Values)
                    {
                        if (result.Status == RoomRaceResultStatus.Finished)
                            snapshot.FinishedResultCount++;
                        else if (result.Status == RoomRaceResultStatus.Dnf)
                            snapshot.DnfResultCount++;
                        else
                            snapshot.UnresolvedResultCount++;
                    }
                }

                return snapshot;
            }
        }
    }
}
