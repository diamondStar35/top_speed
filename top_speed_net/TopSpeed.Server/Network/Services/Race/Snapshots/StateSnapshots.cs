using System;
using System.Linq;
using LiteNetLib;
using TopSpeed.Bots;
using TopSpeed.Data;
using TopSpeed.Localization;
using TopSpeed.Protocol;
using TopSpeed.Server.Protocol;
using TopSpeed.Server.Tracks;

namespace TopSpeed.Server.Network
{
    internal sealed partial class RaceServer
    {
        private int CountActiveRaceParticipants(GameRoom room)
        {
            var humanRacers = room.PlayerIds.Count(id => _players.TryGetValue(id, out var player) && IsActiveRaceState(player.State));
            var botRacers = room.Bots.Count(bot => IsActiveRaceState(bot.State));
            return humanRacers + botRacers;
        }

        private static bool IsActiveRaceState(PlayerState state)
        {
            return state == PlayerState.AwaitingStart || state == PlayerState.Racing || state == PlayerState.Pitting;
        }

        private void SendRaceSnapshot(GameRoom room, DeliveryMethod deliveryMethod)
        {
            _raceSnapshotSends++;
            _logger.Debug(LocalizationService.Format(
                LocalizationService.Mark("Race snapshot send: room={0}, delivery={1}."),
                room.Id,
                deliveryMethod));
            var payload = BuildRaceSnapshotPayload(room);
            if (payload == null)
                return;

            var delivery = deliveryMethod == DeliveryMethod.ReliableOrdered
                ? PacketDeliveryKind.ReliableOrdered
                : deliveryMethod == DeliveryMethod.Sequenced
                    ? PacketDeliveryKind.Sequenced
                    : PacketDeliveryKind.Unreliable;
            _notify.ToRoom(room, payload, PacketStream.RaceState, delivery);
        }

        private void BroadcastPlayerData()
        {
            foreach (var room in _rooms.Values)
            {
                if (!room.RaceStarted)
                    continue;
                var payload = BuildRaceSnapshotPayload(room);
                if (payload == null)
                    continue;
                _notify.ToRoom(room, payload, PacketStream.RaceState, PacketDeliveryKind.Unreliable);
            }
        }

        private byte[]? BuildRaceSnapshotPayload(GameRoom room)
        {
            var max = ProtocolConstants.MaxPlayers;
            var items = new PacketPlayerData[max];
            var count = 0;

            foreach (var id in room.PlayerIds)
            {
                if (!_players.TryGetValue(id, out var player))
                    continue;
                if (player.State == PlayerState.NotReady || player.State == PlayerState.Undefined || player.State == PlayerState.Pitting)
                    continue;
                if (count >= max)
                    break;
                items[count++] = player.ToPacket();
            }

            if (count < max)
            {
                foreach (var bot in room.Bots)
                {
                    if (bot.State == PlayerState.NotReady || bot.State == PlayerState.Undefined)
                        continue;
                    if (count >= max)
                        break;
                    items[count++] = ToBotPacket(bot);
                }
            }

            if (count == 0)
                return null;

            var players = new PacketPlayerData[count];
            Array.Copy(items, players, count);
            var snapshot = new PacketRaceSnapshot
            {
                Sequence = ++room.RaceSnapshotSequence,
                Tick = room.RaceSnapshotTick = _simulationTick,
                Players = players
            };
            _stateSyncFramesSent += count;
            return PacketSerializer.WriteRaceSnapshot(snapshot);
        }

    }
}

