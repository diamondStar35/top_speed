using System;
using System.Collections.Generic;
using System.Linq;
using TopSpeed.Protocol;
using TopSpeed.Vehicles.Parsing;

namespace TopSpeed.Server.Network
{
    internal sealed partial class RaceServer
    {
        private readonly Dictionary<string, VehiclePackageRecord> _vehiclePackageCache =
            new Dictionary<string, VehiclePackageRecord>(StringComparer.OrdinalIgnoreCase);

        private bool TryGetVehiclePackage(string hash, out VehiclePackageRecord record)
        {
            record = null!;
            var key = VehiclePackageRef.NormalizeHash(hash);
            if (string.IsNullOrWhiteSpace(key))
                return false;
            if (!_vehiclePackageCache.TryGetValue(key, out var found) || found == null)
                return false;

            record = found;
            record.LastAccessUtc = DateTime.UtcNow;
            return true;
        }

        private bool StoreVehiclePackage(
            VehiclePackagePayload payload,
            byte[] bytes,
            CustomVehicleTsvData parsed,
            string sourcePath,
            DateTime sourceLastWriteUtc)
        {
            if (payload == null || bytes == null)
                return false;

            var hash = VehiclePackageRef.NormalizeHash(payload.Manifest.Hash);
            if (string.IsNullOrWhiteSpace(hash))
                return false;

            _vehiclePackageCache[hash] = new VehiclePackageRecord
            {
                Ref = VehiclePackageRef.Custom(payload.Manifest.VehicleId, payload.Manifest.Version, hash),
                Payload = payload,
                Bytes = bytes,
                DisplayName = payload.Manifest.DisplayName,
                WidthM = parsed?.WidthM ?? 0f,
                LengthM = parsed?.LengthM ?? 0f,
                MassKg = parsed?.MassKg ?? 0f,
                SupportsAutomatic = VehicleTransmissionSupport.SupportsAutomatic(parsed),
                SupportsManual = VehicleTransmissionSupport.SupportsManual(parsed),
                LastAccessUtc = DateTime.UtcNow,
                SourcePath = sourcePath ?? string.Empty,
                SourceLastWriteUtc = sourceLastWriteUtc
            };

            EvictVehiclePackages();
            return true;
        }

        private void EvictVehiclePackages()
        {
            if (_vehiclePackageCache.Count <= ProtocolConstants.MaxVehiclePackageCacheEntries)
                return;

            var candidates = _vehiclePackageCache
                .OrderBy(pair => pair.Value.LastAccessUtc)
                .Select(pair => pair.Key)
                .ToList();

            for (var i = 0; i < candidates.Count && _vehiclePackageCache.Count > ProtocolConstants.MaxVehiclePackageCacheEntries; i++)
                _vehiclePackageCache.Remove(candidates[i]);
        }
    }
}
