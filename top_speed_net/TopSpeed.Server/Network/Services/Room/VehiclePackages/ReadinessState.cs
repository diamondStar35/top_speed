using System;
using System.Collections.Generic;
using TopSpeed.Protocol;
using TopSpeed.Server.Protocol;

namespace TopSpeed.Server.Network
{
    internal sealed partial class RaceServer
    {
        // The set of custom vehicle package hashes that must be downloaded by everyone this race
        // = the distinct custom vehicle each active participant selected.
        private HashSet<string> RequiredRoomVehicleHashes(IEnumerable<uint> participantIds)
        {
            var required = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var id in participantIds)
            {
                if (!_players.TryGetValue(id, out var player) || player == null)
                    continue;
                var hash = VehiclePackageRef.NormalizeHash(player.SelectedVehicleHash);
                if (!string.IsNullOrWhiteSpace(hash))
                    required.Add(hash);
            }

            return required;
        }

        // True only when every participant has confirmed every required custom vehicle package.
        // Re-sends any package a straggler has not yet acknowledged.
        private bool EnsureRoomVehiclePackagesReady(GameRoom room, IReadOnlyList<uint> participantIds)
        {
            if (room == null)
                return true;

            var required = RequiredRoomVehicleHashes(participantIds);
            if (required.Count == 0)
                return true;

            var allReady = true;
            for (var i = 0; i < participantIds.Count; i++)
            {
                var id = participantIds[i];
                room.VehiclePackageReadyByPlayer.TryGetValue(id, out var confirmed);

                foreach (var hash in required)
                {
                    if (confirmed != null && confirmed.Contains(hash))
                        continue;

                    allReady = false;

                    // Do not send the package here. Clients ask for what they lack, so pushing it
                    // would hand it to someone who may already have it. Re-announcing which vehicle
                    // is expected is enough: a client still missing it asks again, and one that has
                    // it stays quiet. This costs a hash rather than megabytes.
                    NudgeVehicleSelection(room, participantIds, id, hash);
                }
            }

            return allReady;
        }

        // Re-tells one player which participant is using the vehicle they are still missing, so they
        // ask for it again. Covers the case where the original announcement was acted on before the
        // client could hold on to it, without the server guessing that a package needs sending.
        private void NudgeVehicleSelection(GameRoom room, IReadOnlyList<uint> participantIds, uint missingPlayerId, string hash)
        {
            if (!_players.TryGetValue(missingPlayerId, out var missingPlayer) || missingPlayer == null)
                return;

            for (var i = 0; i < participantIds.Count; i++)
            {
                if (!_players.TryGetValue(participantIds[i], out var owner) || owner == null)
                    continue;
                if (!string.Equals(VehiclePackageRef.NormalizeHash(owner.SelectedVehicleHash), hash, StringComparison.OrdinalIgnoreCase))
                    continue;

                SendStream(missingPlayer, PacketSerializer.WriteRoomPlayerVehicle(new PacketRoomPlayerVehicle
                {
                    PlayerNumber = owner.PlayerNumber,
                    Hash = hash
                }), PacketStream.Room);
                return;
            }
        }

        private void MarkPlayerVehiclePackageReady(GameRoom room, uint playerId, string hash)
        {
            if (room == null)
                return;
            var normalizedHash = VehiclePackageRef.NormalizeHash(hash);
            if (string.IsNullOrWhiteSpace(normalizedHash))
                return;

            if (!room.VehiclePackageReadyByPlayer.TryGetValue(playerId, out var confirmed) || confirmed == null)
            {
                confirmed = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                room.VehiclePackageReadyByPlayer[playerId] = confirmed;
            }

            confirmed.Add(normalizedHash);
        }

        private void ResetRoomVehiclePackageReadiness(GameRoom room)
        {
            room?.VehiclePackageReadyByPlayer.Clear();
        }
    }
}
