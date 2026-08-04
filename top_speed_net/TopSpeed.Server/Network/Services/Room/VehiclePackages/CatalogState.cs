using System.Linq;
using TopSpeed.Localization;
using TopSpeed.Protocol;
using TopSpeed.Server.Protocol;

namespace TopSpeed.Server.Network
{
    internal sealed partial class RaceServer
    {
        private PacketVehiclePackageCatalog BuildVehiclePackageCatalog()
        {
            RefreshServerVehiclePackages();
            var entries = _vehiclePackageCache.Values
                .Where(record => record != null && record.Ref != null && record.Ref.IsCustomPackage)
                .OrderBy(record => record.Ref.VehicleId, System.StringComparer.OrdinalIgnoreCase)
                .ThenBy(record => record.Ref.Version, System.StringComparer.OrdinalIgnoreCase)
                .Select(record => new PacketVehiclePackageCatalogEntry
                {
                    Vehicle = VehiclePackageRef.Custom(record.Ref.VehicleId, record.Ref.Version, record.Ref.Hash),
                    DisplayName = ResolveVehiclePackageDisplayName(record),
                    SupportsAutomatic = record.SupportsAutomatic,
                    SupportsManual = record.SupportsManual
                })
                .Take(ProtocolConstants.MaxVehiclePackageCatalogEntries)
                .ToArray();

            return new PacketVehiclePackageCatalog
            {
                Vehicles = entries
            };
        }

        private void SendVehiclePackageCatalog(PlayerConnection player, PacketVehiclePackageCatalog packet)
        {
            if (player == null)
                return;

            SendStream(player, PacketSerializer.WriteVehiclePackageCatalog(packet ?? new PacketVehiclePackageCatalog()), PacketStream.Room);
        }

        private static string ResolveVehiclePackageDisplayName(VehiclePackageRecord record)
        {
            var name = (record?.DisplayName ?? string.Empty).Trim();
            if (!string.IsNullOrWhiteSpace(name))
                return ClampVehiclePackageDisplayName(name);

            var vehicleId = (record?.Ref?.VehicleId ?? string.Empty).Trim();
            var version = (record?.Ref?.Version ?? string.Empty).Trim();
            if (!string.IsNullOrWhiteSpace(vehicleId) && !string.IsNullOrWhiteSpace(version))
                return ClampVehiclePackageDisplayName(vehicleId + " (" + version + ")");
            if (!string.IsNullOrWhiteSpace(vehicleId))
                return ClampVehiclePackageDisplayName(vehicleId);

            return ClampVehiclePackageDisplayName(LocalizationService.Mark("Custom vehicle"));
        }

        private static string ClampVehiclePackageDisplayName(string value)
        {
            var trimmed = (value ?? string.Empty).Trim();
            if (trimmed.Length <= ProtocolConstants.MaxVehiclePackageDisplayNameLength)
                return trimmed;
            return trimmed.Substring(0, ProtocolConstants.MaxVehiclePackageDisplayNameLength);
        }

        // Whether any connected player currently has this custom vehicle package selected.
        // Fully wired once per-player vehicle selection is stored (see room ready handling).
        private bool IsVehiclePackageInUse(string hash)
        {
            var normalizedHash = VehiclePackageRef.NormalizeHash(hash);
            if (string.IsNullOrWhiteSpace(normalizedHash))
                return false;

            foreach (var player in _players.Values)
            {
                if (player == null)
                    continue;
                if (string.Equals(VehiclePackageRef.NormalizeHash(player.SelectedVehicleHash), normalizedHash, System.StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }
    }
}
