using System;
using System.Collections.Generic;
using TopSpeed.Localization;
using TopSpeed.Protocol;

namespace TopSpeed.Core.Multiplayer
{
    internal sealed partial class RoomStore
    {
        public void ApplyRoomListEvent(RoomEventInfo roomEvent)
        {
            if (roomEvent.Kind == RoomEventKind.None)
                return;

            var rooms = new List<RoomSummaryInfo>(RoomList.Rooms ?? Array.Empty<RoomSummaryInfo>());
            var index = rooms.FindIndex(r => r.RoomId == roomEvent.RoomId);

            switch (roomEvent.Kind)
            {
                case RoomEventKind.RoomRemoved:
                    if (index >= 0)
                        rooms.RemoveAt(index);
                    break;

                case RoomEventKind.RoomCreated:
                case RoomEventKind.RoomSummaryUpdated:
                case RoomEventKind.ParticipantJoined:
                case RoomEventKind.ParticipantLeft:
                case RoomEventKind.BotAdded:
                case RoomEventKind.BotRemoved:
                case RoomEventKind.PlayersToStartChanged:
                case RoomEventKind.PrepareStarted:
                case RoomEventKind.PrepareCancelled:
                case RoomEventKind.RaceStarted:
                case RoomEventKind.RaceStopped:
                case RoomEventKind.RacePaused:
                case RoomEventKind.RaceResumed:
                    var summary = new RoomSummaryInfo
                    {
                        RoomId = roomEvent.RoomId,
                        RoomVersion = roomEvent.RoomVersion,
                        RoomName = roomEvent.RoomName ?? string.Empty,
                        RoomType = RoomRules.NormalizeType(roomEvent.RoomType),
                        PlayerCount = roomEvent.PlayerCount,
                        PlayersToStart = RoomRules.NormalizePlayersToStart(roomEvent.RoomType, roomEvent.PlayersToStart),
                        RaceState = RoomRules.NormalizeRaceState(roomEvent.RaceState),
                        TrackName = roomEvent.TrackName ?? string.Empty
                    };
                    if (index >= 0)
                    {
                        if (roomEvent.RoomVersion != 0 && rooms[index].RoomVersion > roomEvent.RoomVersion)
                            break;
                        rooms[index] = summary;
                    }
                    else if (roomEvent.Kind != RoomEventKind.RoomSummaryUpdated || roomEvent.RoomId != 0)
                        rooms.Add(summary);
                    break;
            }

            rooms.Sort((a, b) => a.RoomId.CompareTo(b.RoomId));
            RoomList = new RoomListInfo { Rooms = rooms.ToArray() };
        }

        public bool TryApplyCurrentRoomEvent(
            RoomEventInfo roomEvent,
            uint localPlayerId,
            out bool localHostChanged)
        {
            localHostChanged = false;

            if (!CurrentRoom.InRoom || CurrentRoom.RoomId != roomEvent.RoomId)
                return false;
            if (IsStaleEvent(roomEvent.EventSequence, roomEvent.RoomId))
                return false;
            if (roomEvent.RoomVersion != 0 && CurrentRoom.RoomVersion > roomEvent.RoomVersion)
                return false;

            var previousIsHost = CurrentRoom.IsHost;

            CurrentRoom.RoomVersion = roomEvent.RoomVersion;
            if (roomEvent.EventSequence != 0)
                AdvanceEventSequence(roomEvent.RoomId, roomEvent.EventSequence);
            if (!string.IsNullOrWhiteSpace(roomEvent.RoomName))
                CurrentRoom.RoomName = roomEvent.RoomName;
            if (roomEvent.HostPlayerId != 0)
                CurrentRoom.HostPlayerId = roomEvent.HostPlayerId;
            CurrentRoom.RoomType = RoomRules.NormalizeType(roomEvent.RoomType);
            if (roomEvent.PlayersToStart > 0)
                CurrentRoom.PlayersToStart = RoomRules.NormalizePlayersToStart(roomEvent.RoomType, roomEvent.PlayersToStart);
            var nextRaceState = RoomRules.NormalizeRaceState(roomEvent.RaceState);
            if (roomEvent.RaceInstanceId != 0 || nextRaceState == RoomRaceState.Lobby)
                CurrentRoom.RaceInstanceId = roomEvent.RaceInstanceId;
            CurrentRoom.RaceState = nextRaceState;
            CurrentRoom.RacePaused = roomEvent.RacePaused;
            if (!string.IsNullOrWhiteSpace(roomEvent.TrackName))
                CurrentRoom.TrackName = roomEvent.TrackName;
            if (roomEvent.Track != null && PacketValidation.IsValidTrackPackageRef(roomEvent.Track))
                CurrentRoom.Track = CloneTrack(roomEvent.Track);
            if (roomEvent.Laps > 0)
                CurrentRoom.Laps = roomEvent.Laps;
            CurrentRoom.GameRulesFlags = roomEvent.GameRulesFlags;
            // No transient per-race override is delivered over the wire yet, so effective == room.
            CurrentRoom.RaceEffectiveGameRulesFlags = roomEvent.GameRulesFlags;
            CurrentRoom.IsHost = localPlayerId != 0 && CurrentRoom.HostPlayerId == localPlayerId;

            switch (roomEvent.Kind)
            {
                case RoomEventKind.ParticipantJoined:
                case RoomEventKind.BotAdded:
                case RoomEventKind.ParticipantStateChanged:
                    UpsertCurrentRoomParticipant(roomEvent);
                    break;

                case RoomEventKind.ParticipantLeft:
                case RoomEventKind.BotRemoved:
                    RemoveCurrentRoomParticipant(roomEvent.SubjectPlayerId);
                    break;
            }

            localHostChanged = previousIsHost != CurrentRoom.IsHost;
            WasHost = CurrentRoom.IsHost;
            UpdateClientStateFromRoom();
            return true;
        }

        private void UpdateRoomListRaceState(uint roomId, uint roomVersion, RoomRaceState state)
        {
            var rooms = RoomList.Rooms ?? Array.Empty<RoomSummaryInfo>();
            if (rooms.Length == 0)
                return;

            var updated = false;
            var copy = new RoomSummaryInfo[rooms.Length];
            for (var i = 0; i < rooms.Length; i++)
            {
                var source = rooms[i];
                if (source.RoomId == roomId)
                {
                    if (roomVersion != 0 && source.RoomVersion > roomVersion)
                    {
                        copy[i] = source;
                        continue;
                    }

                    if (roomVersion != 0)
                        source.RoomVersion = roomVersion;
                    source.RaceState = state;
                    updated = true;
                }
                copy[i] = source;
            }

            if (updated)
                RoomList = new RoomListInfo { Rooms = copy };
        }

        private void UpsertCurrentRoomParticipant(RoomEventInfo roomEvent)
        {
            if (roomEvent.SubjectPlayerId == 0)
                return;

            var players = new List<RoomParticipant>(CurrentRoom.Players ?? Array.Empty<RoomParticipant>());
            var index = players.FindIndex(p => p.PlayerId == roomEvent.SubjectPlayerId);
            var name = string.IsNullOrWhiteSpace(roomEvent.SubjectPlayerName)
                ? LocalizationService.Translate(LocalizationService.Mark("Player ")) + (roomEvent.SubjectPlayerNumber + 1)
                : roomEvent.SubjectPlayerName;
            var item = new RoomParticipant
            {
                PlayerId = roomEvent.SubjectPlayerId,
                PlayerNumber = roomEvent.SubjectPlayerNumber,
                State = RoomRules.NormalizeParticipantState(roomEvent.SubjectPlayerState),
                Name = name
            };

            if (index >= 0)
                players[index] = item;
            else
                players.Add(item);

            players.Sort((a, b) => a.PlayerNumber.CompareTo(b.PlayerNumber));
            CurrentRoom.Players = players.ToArray();
        }

        private void RemoveCurrentRoomParticipant(uint playerId)
        {
            if (playerId == 0)
                return;

            var players = new List<RoomParticipant>(CurrentRoom.Players ?? Array.Empty<RoomParticipant>());
            var removed = players.RemoveAll(p => p.PlayerId == playerId);
            if (removed == 0)
                return;

            players.Sort((a, b) => a.PlayerNumber.CompareTo(b.PlayerNumber));
            CurrentRoom.Players = players.ToArray();
        }

        private static TrackPackageRef CloneTrack(TrackPackageRef track)
        {
            return TrackPackageRef.Clone(track);
        }
    }
}
