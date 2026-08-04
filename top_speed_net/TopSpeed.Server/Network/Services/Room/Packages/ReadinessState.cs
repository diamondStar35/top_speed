using System.Collections.Generic;
using TopSpeed.Protocol;

namespace TopSpeed.Server.Network
{
    internal sealed partial class RaceServer
    {
        private bool EnsureRoomPackageReady(GameRoom room, IEnumerable<uint> participantIds)
        {
            if (room == null || room.TrackSelection == null || !room.TrackSelection.IsCustomPackage)
                return true;

            if (!TryGetPackage(room.TrackSelection.Hash, out var package))
                return false;

            var allReady = true;
            foreach (var id in participantIds)
            {
                if (room.TrackReadyPlayers.Contains(id))
                    continue;

                allReady = false;
                if (_players.TryGetValue(id, out var player))
                    SendPackageToPlayer(player, package);
            }

            return allReady;
        }

        private void ResetRoomPackageReadiness(GameRoom room)
        {
            room.TrackReadyPlayers.Clear();
            ResetRoomVehiclePackageReadiness(room);
        }

        private void MarkPlayerPackageReady(GameRoom room, uint playerId)
        {
            room.TrackReadyPlayers.Add(playerId);
        }
    }
}

