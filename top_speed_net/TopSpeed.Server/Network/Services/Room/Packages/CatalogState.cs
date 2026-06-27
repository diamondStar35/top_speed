using System;
using System.Linq;
using TopSpeed.Localization;
using TopSpeed.Protocol;
using TopSpeed.Server.Protocol;

namespace TopSpeed.Server.Network
{
    internal sealed partial class RaceServer
    {
        private void SendTrackPackageUploadResult(PlayerConnection player, uint uploadId, TrackPackageUploadStatus status, string hash, string message)
        {
            SendStream(player, PacketSerializer.WriteTrackPackageUploadResult(new PacketTrackPackageUploadResult
            {
                UploadId = uploadId,
                Status = status,
                Hash = TrackPackageRef.NormalizeHash(hash),
                Message = message ?? string.Empty
            }), PacketStream.Room);
        }

        private PacketTrackPackageCatalog BuildTrackPackageCatalog()
        {
            RefreshServerPackages();
            var entries = _trackPackageCache.Values
                .Where(record => record != null && record.Ref != null && record.Ref.IsCustomPackage)
                .OrderBy(record => record.Ref.TrackId, StringComparer.OrdinalIgnoreCase)
                .ThenBy(record => record.Ref.Version, StringComparer.OrdinalIgnoreCase)
                .Select(record => new PacketTrackPackageCatalogEntry
                {
                    Track = TrackPackageRef.Custom(record.Ref.TrackId, record.Ref.Version, record.Ref.Hash),
                    DisplayName = ResolveTrackPackageDisplayName(record),
                    HasPitArea = record.TrackData?.HasPitArea ?? true
                })
                .Take(ProtocolConstants.MaxTrackPackageCatalogEntries)
                .ToArray();

            return new PacketTrackPackageCatalog
            {
                Tracks = entries
            };
        }

        private void SendTrackPackageCatalog(PlayerConnection player, PacketTrackPackageCatalog packet)
        {
            if (player == null)
                return;

            SendStream(player, PacketSerializer.WriteTrackPackageCatalog(packet ?? new PacketTrackPackageCatalog()), PacketStream.Room);
        }

        private void SendPackageCatalogToRoom(GameRoom room, PacketTrackPackageCatalog packet)
        {
            if (room == null)
                return;

            var payload = PacketSerializer.WriteTrackPackageCatalog(packet ?? new PacketTrackPackageCatalog());
            _notify.ToRoom(room, payload, PacketStream.Room);
        }

        private static string ResolveTrackPackageDisplayName(PackageRecord record)
        {
            var metadata = record.Payload?.Metadata;
            if (metadata != null && metadata.TryGetValue("name", out var name))
            {
                var trimmed = (name ?? string.Empty).Trim();
                if (!string.IsNullOrWhiteSpace(trimmed))
                    return ClampTrackPackageDisplayName(trimmed);
            }

            var trackId = (record.Ref?.TrackId ?? string.Empty).Trim();
            var version = (record.Ref?.Version ?? string.Empty).Trim();
            if (!string.IsNullOrWhiteSpace(trackId) && !string.IsNullOrWhiteSpace(version))
                return ClampTrackPackageDisplayName(trackId + " (" + version + ")");
            if (!string.IsNullOrWhiteSpace(trackId))
                return ClampTrackPackageDisplayName(trackId);

            return ClampTrackPackageDisplayName(LocalizationService.Mark("Custom track"));
        }

        private static string ClampTrackPackageDisplayName(string value)
        {
            var trimmed = (value ?? string.Empty).Trim();
            if (trimmed.Length <= ProtocolConstants.MaxTrackPackageDisplayNameLength)
                return trimmed;
            return trimmed.Substring(0, ProtocolConstants.MaxTrackPackageDisplayNameLength);
        }
    }
}


