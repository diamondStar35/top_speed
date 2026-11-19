using System;
using TopSpeed.Protocol;

namespace TopSpeed.Core.Multiplayer
{
    internal static class RoomMap
    {
        public static RoomListInfo ToList(PacketRoomList? packet)
        {
            if (packet == null || packet.Rooms == null || packet.Rooms.Length == 0)
                return new RoomListInfo();

            var valid = 0;
            for (var i = 0; i < packet.Rooms.Length; i++)
            {
                var src = packet.Rooms[i];
                if (src != null && src.RoomId != 0)
                    valid++;
            }

            if (valid == 0)
                return new RoomListInfo();

            var rooms = new RoomSummaryInfo[valid];
            var index = 0;
            for (var i = 0; i < packet.Rooms.Length; i++)
            {
                var src = packet.Rooms[i];
                if (src == null || src.RoomId == 0)
                    continue;

                rooms[index++] = new RoomSummaryInfo
                {
                    RoomId = src.RoomId,
                    RoomName = src.RoomName ?? string.Empty,
                    RoomType = RoomRules.NormalizeType(src.RoomType),
                    PlayerCount = src.PlayerCount,
                    PlayersToStart = RoomRules.NormalizePlayersToStart(src.RoomType, src.PlayersToStart),
                    RaceState = RoomRules.NormalizeRaceState(src.RaceState),
                    TrackName = src.TrackName ?? string.Empty
                };
            }

            return new RoomListInfo { Rooms = rooms };
        }

        public static RoomSnapshot ToSnapshot(PacketRoomState? packet)
        {
            if (packet == null)
                return new RoomSnapshot();

            return new RoomSnapshot
            {
                RoomVersion = packet.RoomVersion,
                EventSequence = packet.EventSequence,
                RoomId = packet.RoomId,
                RaceInstanceId = packet.RaceInstanceId,
                HostPlayerId = packet.HostPlayerId,
                RoomName = packet.RoomName ?? string.Empty,
                RoomType = RoomRules.NormalizeType(packet.RoomType),
                PlayersToStart = RoomRules.NormalizePlayersToStart(packet.RoomType, packet.PlayersToStart),
                RaceState = RoomRules.NormalizeRaceState(packet.RaceState),
                RacePaused = packet.RacePaused,
                InRoom = packet.InRoom,
                IsHost = packet.IsHost,
                TrackName = packet.TrackName ?? string.Empty,
                Track = CloneTrack(packet.Track),
                Laps = packet.Laps,
                GameRulesFlags = packet.GameRulesFlags,
                // No transient per-race override is delivered over the wire yet, so the effective
                // rules equal the persistent room rules. When pit-less tracks introduce the host's
                // "disable for this race" override, the server will compute effective rules
                // (room minus the disable mask) and deliver them here. See PitAreaWarning / the MP
                // host-side seam in MultiplayerCoordinator.
                RaceEffectiveGameRulesFlags = packet.GameRulesFlags,
                Players = ToParticipants(packet.Players)
            };
        }

        public static RoomEventInfo? ToEvent(PacketRoomEvent? packet)
        {
            if (packet == null)
                return null;
            if (packet.RoomId == 0 || packet.Kind == RoomEventKind.None)
                return null;

            return new RoomEventInfo
            {
                RoomId = packet.RoomId,
                RoomVersion = packet.RoomVersion,
                EventSequence = packet.EventSequence,
                RaceInstanceId = packet.RaceInstanceId,
                Kind = packet.Kind,
                HostPlayerId = packet.HostPlayerId,
                RoomType = RoomRules.NormalizeType(packet.RoomType),
                PlayerCount = packet.PlayerCount,
                PlayersToStart = RoomRules.NormalizePlayersToStart(packet.RoomType, packet.PlayersToStart),
                RaceState = RoomRules.NormalizeRaceState(packet.RaceState),
                RacePaused = packet.RacePaused,
                TrackName = packet.TrackName ?? string.Empty,
                Track = CloneTrack(packet.Track),
                Laps = packet.Laps,
                GameRulesFlags = packet.GameRulesFlags,
                RoomName = packet.RoomName ?? string.Empty,
                SubjectPlayerId = packet.SubjectPlayerId,
                SubjectPlayerNumber = packet.SubjectPlayerNumber,
                SubjectPlayerState = packet.SubjectPlayerState,
                SubjectPlayerName = packet.SubjectPlayerName ?? string.Empty
            };
        }

        private static RoomParticipant[] ToParticipants(PacketRoomPlayer[]? packetPlayers)
        {
            if (packetPlayers == null || packetPlayers.Length == 0)
                return Array.Empty<RoomParticipant>();

            var valid = 0;
            for (var i = 0; i < packetPlayers.Length; i++)
            {
                var src = packetPlayers[i];
                if (src != null && src.PlayerId != 0)
                    valid++;
            }

            if (valid == 0)
                return Array.Empty<RoomParticipant>();

            var players = new RoomParticipant[valid];
            var index = 0;
            for (var i = 0; i < packetPlayers.Length; i++)
            {
                var src = packetPlayers[i];
                if (src == null || src.PlayerId == 0)
                    continue;

                players[index++] = new RoomParticipant
                {
                    PlayerId = src.PlayerId,
                    PlayerNumber = src.PlayerNumber,
                    State = RoomRules.NormalizeParticipantState(src.State),
                    Name = src.Name ?? string.Empty
                };
            }

            return players;
        }

        private static TrackPackageRef CloneTrack(TrackPackageRef track)
        {
            return TrackPackageRef.Clone(track);
        }
    }
}

