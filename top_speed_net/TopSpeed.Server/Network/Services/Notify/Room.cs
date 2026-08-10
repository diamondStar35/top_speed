using System;
using System.Linq;
using TopSpeed.Localization;
using TopSpeed.Protocol;
using TopSpeed.Server.Protocol;

namespace TopSpeed.Server.Network
{
    internal sealed partial class RaceServer
    {
        private sealed partial class Notify
        {
            public void SendRoomList(PlayerConnection player)
            {
                var list = new PacketRoomList
                {
                    Rooms = _owner._rooms.Values
                        .OrderBy(room => room.Id)
                        .Take(ProtocolConstants.MaxRoomListEntries)
                        .Select(BuildRoomSummary)
                        .ToArray()
                };

                _owner.SendStream(player, PacketSerializer.WriteRoomList(list), PacketStream.Query);
            }

            public void SendRoomState(PlayerConnection player, GameRoom? room)
            {
                if (room == null)
                {
                    _owner.SendStream(player, PacketSerializer.WriteRoomState(new PacketRoomState
                    {
                        RoomVersion = 0,
                        EventSequence = 0,
                        RaceInstanceId = 0,
                        InRoom = false,
                        HostPlayerId = 0,
                        RoomType = GameRoomType.BotsRace,
                        PlayersToStart = 0,
                        RaceState = RoomRaceState.Lobby,
                        RacePaused = false,
                        GameRulesFlags = 0,
                        Players = Array.Empty<PacketRoomPlayer>()
                    }), PacketStream.Query);
                    return;
                }

                _owner.SendStream(player, PacketSerializer.WriteRoomState(new PacketRoomState
                {
                    RoomVersion = room.Version,
                    EventSequence = CurrentEventSequence(room),
                    RoomId = room.Id,
                    RaceInstanceId = room.RaceInstanceId,
                    HostPlayerId = room.HostId,
                    RoomName = room.Name,
                    RoomType = room.RoomType,
                    PlayersToStart = room.PlayersToStart,
                    RaceState = room.RaceState,
                    InRoom = true,
                    IsHost = room.HostId == player.Id,
                    RacePaused = room.RacePaused,
                    Track = CloneRoomDisplayTrackRef(room),
                    TrackName = GetRoomDisplayTrackName(room),
                    Laps = room.Laps,
                    GameRulesFlags = room.GameRulesFlags,
                    Players = BuildRoomPlayers(room)
                }), PacketStream.Query);
            }

            public void SendRoomGet(PlayerConnection player, GameRoom? room)
            {
                if (room == null)
                {
                    _owner.SendStream(player, PacketSerializer.WriteRoomGet(new PacketRoomGet
                    {
                        Found = false,
                        EventSequence = 0,
                        RaceInstanceId = 0,
                        RaceState = RoomRaceState.Lobby,
                        Players = Array.Empty<PacketRoomPlayer>()
                    }), PacketStream.Query);
                    return;
                }

                _owner.SendStream(player, PacketSerializer.WriteRoomGet(new PacketRoomGet
                {
                    Found = true,
                    RoomVersion = room.Version,
                    EventSequence = CurrentEventSequence(room),
                    RoomId = room.Id,
                    RaceInstanceId = room.RaceInstanceId,
                    HostPlayerId = room.HostId,
                    RoomName = room.Name,
                    RoomType = room.RoomType,
                    PlayersToStart = room.PlayersToStart,
                    RaceState = room.RaceState,
                    RacePaused = room.RacePaused,
                    Track = CloneRoomDisplayTrackRef(room),
                    TrackName = GetRoomDisplayTrackName(room),
                    Laps = room.Laps,
                    GameRulesFlags = room.GameRulesFlags,
                    Players = BuildRoomPlayers(room)
                }), PacketStream.Query);
            }

            public void BroadcastRoomState(GameRoom room)
            {
                if (room == null)
                    return;

                foreach (var id in room.PlayerIds)
                    if (_owner._players.TryGetValue(id, out var player))
                        SendRoomState(player, room);
            }

            public void RoomLifecycle(GameRoom room, RoomEventKind kind)
            {
                var evt = CreateRoomEvent(room, kind);
                var payload = PacketSerializer.WriteRoomEvent(evt);
                RoomEventJournal.Record(room, Command.RoomEvent, evt.EventSequence, payload, PacketStream.Room);
                if (kind == RoomEventKind.RoomCreated)
                {
                    ToLobby(payload, PacketStream.Room);
                    return;
                }

                ToRoom(room, payload, PacketStream.Room);
            }

            public void RoomParticipant(GameRoom room, RoomEventKind kind, uint playerId, byte playerNumber, PlayerState state, string name)
            {
                var evt = CreateRoomEvent(room, kind);
                evt.SubjectPlayerId = playerId;
                evt.SubjectPlayerNumber = playerNumber;
                evt.SubjectPlayerState = state;
                evt.SubjectPlayerName = name ?? string.Empty;
                var payload = PacketSerializer.WriteRoomEvent(evt);
                RoomEventJournal.Record(room, Command.RoomEvent, evt.EventSequence, payload, PacketStream.Room);

                ToRoom(room, payload, PacketStream.Room);
            }

            private PacketRoomSummary BuildRoomSummary(GameRoom room)
            {
                return new PacketRoomSummary
                {
                    RoomId = room.Id,
                    RoomName = room.Name,
                    RoomType = room.RoomType,
                    PlayerCount = (byte)Math.Min(ProtocolConstants.MaxPlayers, RaceServer.GetRoomParticipantCount(room)),
                    PlayersToStart = room.PlayersToStart,
                    RaceState = room.RaceState,
                    Track = CloneRoomDisplayTrackRef(room),
                    TrackName = GetRoomDisplayTrackName(room)
                };
            }

            private PacketRoomPlayer[] BuildRoomPlayers(GameRoom room)
            {
                return room.PlayerIds
                    .Where(id => _owner._players.ContainsKey(id))
                    .Select(id => _owner._players[id])
                    .Select(player => new PacketRoomPlayer
                    {
                        PlayerId = player.Id,
                        PlayerNumber = player.PlayerNumber,
                        State = player.State,
                        Name = string.IsNullOrWhiteSpace(player.Name)
                            ? LocalizationService.Format(LocalizationService.Mark("Player {0}"), player.PlayerNumber + 1)
                            : player.Name
                    })
                    .Concat(room.Bots.Select(bot => new PacketRoomPlayer
                    {
                        PlayerId = bot.Id,
                        PlayerNumber = bot.PlayerNumber,
                        State = bot.State,
                        Name = RaceServer.FormatBotDisplayName(bot)
                    }))
                    .OrderBy(player => player.PlayerNumber)
                    .ToArray();
            }

            private PacketRoomEvent CreateRoomEvent(GameRoom room, RoomEventKind kind)
            {
                return new PacketRoomEvent
                {
                    RoomId = room.Id,
                    RoomVersion = room.Version,
                    EventSequence = NextEventSequence(room),
                    RaceInstanceId = room.RaceInstanceId,
                    Kind = kind,
                    HostPlayerId = room.HostId,
                    RoomType = room.RoomType,
                    PlayerCount = (byte)Math.Min(ProtocolConstants.MaxPlayers, RaceServer.GetRoomParticipantCount(room)),
                    PlayersToStart = room.PlayersToStart,
                    RaceState = room.RaceState,
                    RacePaused = room.RacePaused,
                    Track = CloneRoomDisplayTrackRef(room),
                    TrackName = GetRoomDisplayTrackName(room),
                    Laps = room.Laps,
                    GameRulesFlags = room.GameRulesFlags,
                    RoomName = room.Name
                };
            }

            private static TrackPackageRef CloneTrackRef(TrackPackageRef track)
            {
                return TrackPackageRef.Clone(track);
            }

            private static TrackPackageRef CloneRoomDisplayTrackRef(GameRoom room)
            {
                if (room?.RandomTrackSelection != null && room.RandomTrackSelection.IsRandomBuiltIn)
                    return TrackPackageRef.Clone(room.RandomTrackSelection);

                return TrackPackageRef.Clone(room?.TrackSelection);
            }

            private static string GetRoomDisplayTrackName(GameRoom room)
            {
                var track = CloneRoomDisplayTrackRef(room);
                if (track.IsBuiltIn)
                    return track.BuiltInTrackKey;
                if (track.IsCustomPackage)
                    return track.TrackId;

                return room?.TrackName ?? string.Empty;
            }
        }
    }
}

