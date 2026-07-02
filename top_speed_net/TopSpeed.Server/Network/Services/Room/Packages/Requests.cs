using TopSpeed.Protocol;

namespace TopSpeed.Server.Network
{
    internal sealed partial class RaceServer
    {
        private sealed partial class Room
        {
            public void HandlePackageCatalogRequest(PlayerConnection player, PacketTrackPackageCatalogRequest packet)
            {
                if (!TryGetHosted(player, out var room))
                    return;

                if (!_owner._config.Features.CustomTracks || !IsCustomSelectionEnabled(room))
                {
                    _owner.SendTrackPackageCatalog(player, new PacketTrackPackageCatalog());
                    return;
                }

                _owner.SendTrackPackageCatalog(player, _owner.BuildTrackPackageCatalog());
            }

            public void HandlePackageReady(PlayerConnection player, PacketTrackPackageReady packet)
            {
                if (!player.RoomId.HasValue)
                    return;
                if (!_owner._rooms.TryGetValue(player.RoomId.Value, out var room))
                    return;

                var hash = TrackPackageRef.NormalizeHash(packet.Hash);
                if (!room.TrackSelection.IsCustomPackage)
                    return;
                if (!string.Equals(room.TrackSelection.Hash, hash, System.StringComparison.OrdinalIgnoreCase))
                    return;

                _owner.MarkPlayerPackageReady(room, player.Id);
                if (room.PreparingRace)
                    _owner._race.TryStartAfterLoadout(room);
            }

            public void HandleVehiclePackageReady(PlayerConnection player, PacketVehiclePackageReady packet)
            {
                if (!player.RoomId.HasValue)
                    return;
                if (!_owner._rooms.TryGetValue(player.RoomId.Value, out var room))
                    return;

                var hash = VehiclePackageRef.NormalizeHash(packet.Hash);
                if (string.IsNullOrWhiteSpace(hash))
                    return;

                _owner.MarkPlayerVehiclePackageReady(room, player.Id, hash);
                if (room.PreparingRace)
                    _owner._race.TryStartAfterLoadout(room);
            }

            private bool IsCustomSelectionEnabled(GameRoom room)
            {
                return room != null
                    && _owner._config.Features.CustomTracks
                    && (room.GameRulesFlags & (uint)RoomGameRules.CustomTracks) != 0u;
            }
        }
    }
}
