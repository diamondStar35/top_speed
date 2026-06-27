using System;
using System.Linq;
using TopSpeed.Protocol;
using TopSpeed.Server.Protocol;

namespace TopSpeed.Server.Network
{
    internal sealed partial class RaceServer
    {
        private sealed partial class Notify
        {
            public void RaceStateChanged(GameRoom room)
            {
                var sequence = NextEventSequence(room);
                var payload = PacketSerializer.WriteRoomRaceStateChanged(new PacketRoomRaceStateChanged
                {
                    RoomId = room.Id,
                    RoomVersion = room.Version,
                    EventSequence = sequence,
                    RaceInstanceId = room.RaceInstanceId,
                    State = room.RaceState,
                    EffectiveGameRulesFlags = room.RaceEffectiveGameRulesFlags
                });
                RoomEventJournal.Record(room, Command.RoomRaceStateChanged, sequence, payload, PacketStream.Room);
                ToRoom(room, payload, PacketStream.Room);
            }

            public void RacePlayerFinished(GameRoom room, uint playerId, byte playerNumber, byte finishOrder, int timeMs)
            {
                var sequence = NextEventSequence(room);
                var payload = PacketSerializer.WriteRoomRacePlayerFinished(new PacketRoomRacePlayerFinished
                {
                    RoomId = room.Id,
                    RoomVersion = room.Version,
                    EventSequence = sequence,
                    RaceInstanceId = room.RaceInstanceId,
                    PlayerId = playerId,
                    PlayerNumber = playerNumber,
                    FinishOrder = finishOrder,
                    TimeMs = Math.Max(0, timeMs)
                });
                RoomEventJournal.Record(room, Command.RoomRacePlayerFinished, sequence, payload, PacketStream.Room);
                ToRoom(room, payload, PacketStream.Room);
            }

            public void RaceCompleted(GameRoom room)
            {
                var sequence = NextEventSequence(room);
                var packet = BuildRoomRaceCompleted(room);
                _owner._logger.Debug(string.Format(
                    System.Globalization.CultureInfo.InvariantCulture,
                    "Room race completed emit: room={0}, raceInstance={1}, results={2}.",
                    room.Id,
                    room.RaceInstanceId,
                    packet.Results.Length));
                var payload = PacketSerializer.WriteRoomRaceCompleted(packet);
                RoomEventJournal.Record(room, Command.RoomRaceCompleted, sequence, payload, PacketStream.Room);
                ToRoom(room, payload, PacketStream.Room);
            }

            public void SendRaceCompletionTo(PlayerConnection player, GameRoom room)
            {
                if (player == null || room == null)
                    return;

                _owner.SendStream(player, PacketSerializer.WriteRoomRaceCompleted(BuildRoomRaceCompleted(room)), PacketStream.Room);
            }

            public void RaceAborted(GameRoom room, RoomRaceAbortReason reason)
            {
                var sequence = NextEventSequence(room);
                var payload = PacketSerializer.WriteRoomRaceAborted(new PacketRoomRaceAborted
                {
                    RoomId = room.Id,
                    RoomVersion = room.Version,
                    EventSequence = sequence,
                    RaceInstanceId = room.RaceInstanceId,
                    Reason = reason
                });
                RoomEventJournal.Record(room, Command.RoomRaceAborted, sequence, payload, PacketStream.Room);
                ToRoom(room, payload, PacketStream.Room);
            }

            private PacketRoomRaceCompleted BuildRoomRaceCompleted(GameRoom room)
            {
                var ordered = room.RaceParticipantResults.Values
                    .OrderBy(result => result.Status == RoomRaceResultStatus.Finished ? 0 : 1)
                    .ThenBy(result => result.Status == RoomRaceResultStatus.Finished ? result.FinishOrder : byte.MaxValue)
                    .ThenBy(result => result.PlayerNumber)
                    .Take(ProtocolConstants.MaxPlayers)
                    .ToArray();

                var results = new PacketRoomRaceResultEntry[ordered.Length];
                for (var i = 0; i < ordered.Length; i++)
                {
                    var item = ordered[i];
                    var status = RaceResultRules.NormalizeCompletionStatus(item.Status);

                    results[i] = new PacketRoomRaceResultEntry
                    {
                        PlayerId = item.PlayerId,
                        PlayerNumber = item.PlayerNumber,
                        FinishOrder = status == RoomRaceResultStatus.Finished ? item.FinishOrder : (byte)0,
                        TimeMs = status == RoomRaceResultStatus.Finished ? Math.Max(0, item.TimeMs) : 0,
                        Status = status
                    };
                }

                return new PacketRoomRaceCompleted
                {
                    RoomId = room.Id,
                    RoomVersion = room.Version,
                    EventSequence = CurrentEventSequence(room),
                    RaceInstanceId = room.RaceInstanceId,
                    Results = results
                };
            }
        }
    }
}

