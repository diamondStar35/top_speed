using System;
using TopSpeed.Drive.Session;
using TopSpeed.Protocol;

namespace TopSpeed.Drive.Multiplayer
{
    internal sealed partial class MultiplayerSession
    {
        public void ApplyRaceSnapshot(PacketRaceSnapshot snapshot)
        {
            _session.ApplyExternalEvent(new ExternalEvent(Incoming.RaceSnapshot, snapshot));
        }

        public void ApplyBump(PacketPlayerBumped bump)
        {
            _session.ApplyExternalEvent(new ExternalEvent(Incoming.PlayerBumped, bump));
        }

        public void ApplyRemoteCrash(PacketPlayer crashed)
        {
            _session.ApplyExternalEvent(new ExternalEvent(Incoming.PlayerCrashed, crashed));
        }

        public void RemoveRemotePlayer(byte playerNumber, bool markDisconnected = true)
        {
            _session.ApplyExternalEvent(new ExternalEvent(Incoming.PlayerDisconnected, (playerNumber, markDisconnected)));
        }

        public void ApplyRemoteFinish(byte playerNumber, byte finishOrder)
        {
            _session.ApplyExternalEvent(new ExternalEvent(Incoming.RoomRacePlayerFinished, (playerNumber, finishOrder)));
        }

        public void HandleServerRaceCompleted(PacketRoomRaceCompleted packet)
        {
            _session.ApplyExternalEvent(new ExternalEvent(Incoming.RoomRaceCompleted, packet));
        }

        public void HandleServerRaceAborted()
        {
            _session.ApplyExternalEvent(new ExternalEvent(Incoming.RoomRaceAborted));
        }

        public void SyncParticipants(PacketRoomState roomState)
        {
            _session.ApplyExternalEvent(new ExternalEvent(Incoming.RoomParticipantSync, roomState));
        }

        public void ApplyRemoteLiveStart(PacketPlayerLiveStart start, long receivedUtcTicks)
        {
            _session.ApplyExternalEvent(new ExternalEvent(Incoming.LiveStart, (start, receivedUtcTicks)));
        }

        public void ApplyRemoteLiveFrame(PacketPlayerLiveFrame frame, long receivedUtcTicks)
        {
            _session.ApplyExternalEvent(new ExternalEvent(Incoming.LiveFrame, (frame, receivedUtcTicks)));
        }

        public void ApplyRemoteLiveStop(PacketPlayerLiveStop stop)
        {
            _session.ApplyExternalEvent(new ExternalEvent(Incoming.LiveStop, stop));
        }

        public void ApplyRemoteMediaBegin(PacketPlayerMediaBegin media)
        {
            _session.ApplyExternalEvent(new ExternalEvent(Incoming.MediaBegin, media));
        }

        public void ApplyRemoteMediaChunk(PacketPlayerMediaChunk media)
        {
            _session.ApplyExternalEvent(new ExternalEvent(Incoming.MediaChunk, media));
        }

        public void ApplyRemoteMediaEnd(PacketPlayerMediaEnd media)
        {
            _session.ApplyExternalEvent(new ExternalEvent(Incoming.MediaEnd, media));
        }

        private void HandleExternalEvent(SessionContext context, ExternalEvent externalEvent)
        {
            if (externalEvent.Id == Incoming.RaceSnapshot && externalEvent.Data is PacketRaceSnapshot snapshot)
                ApplyRaceSnapshotCore(snapshot);
            else if (externalEvent.Id == Incoming.PlayerBumped && externalEvent.Data is PacketPlayerBumped bump)
                ApplyBumpCore(bump);
            else if (externalEvent.Id == Incoming.PlayerCrashed && externalEvent.Data is PacketPlayer crashed)
                ApplyRemoteCrashCore(crashed);
            else if (externalEvent.Id == Incoming.PlayerDisconnected && externalEvent.Data is ValueTuple<byte, bool> removed)
                RemoveRemotePlayerCore(removed.Item1, removed.Item2);
            else if (externalEvent.Id == Incoming.RoomRacePlayerFinished && externalEvent.Data is ValueTuple<byte, byte> finished)
                ApplyRemoteFinishCore(finished.Item1, finished.Item2);
            else if (externalEvent.Id == Incoming.RoomRaceCompleted && externalEvent.Data is PacketRoomRaceCompleted completed)
            {
                ApplyCompletedRemoteFinishes(completed);
                FinalizeServerRace(BuildResultSummary(completed), completed: true);
            }
            else if (externalEvent.Id == Incoming.RoomRaceAborted)
                FinalizeServerRace(null, completed: false);
            else if (externalEvent.Id == Incoming.RoomParticipantSync && externalEvent.Data is PacketRoomState roomState)
                SyncParticipantsCore(roomState);
            else if (externalEvent.Id == Incoming.LiveStart && externalEvent.Data is ValueTuple<PacketPlayerLiveStart, long> liveStart)
                ApplyRemoteLiveStartCore(liveStart.Item1, liveStart.Item2);
            else if (externalEvent.Id == Incoming.LiveFrame && externalEvent.Data is ValueTuple<PacketPlayerLiveFrame, long> liveFrame)
                ApplyRemoteLiveFrameCore(liveFrame.Item1, liveFrame.Item2);
            else if (externalEvent.Id == Incoming.LiveStop && externalEvent.Data is PacketPlayerLiveStop liveStop)
                ApplyRemoteLiveStopCore(liveStop);
            else if (externalEvent.Id == Incoming.MediaBegin && externalEvent.Data is PacketPlayerMediaBegin mediaBegin)
                ApplyRemoteMediaBeginCore(mediaBegin);
            else if (externalEvent.Id == Incoming.MediaChunk && externalEvent.Data is PacketPlayerMediaChunk mediaChunk)
                ApplyRemoteMediaChunkCore(mediaChunk);
            else if (externalEvent.Id == Incoming.MediaEnd && externalEvent.Data is PacketPlayerMediaEnd mediaEnd)
                ApplyRemoteMediaEndCore(mediaEnd);
        }
    }
}
