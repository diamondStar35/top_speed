using System;
using TopSpeed.Protocol;

namespace TopSpeed.Game
{
    internal sealed partial class Game
    {
        private void TryApplyCachedTrackPackage(TrackPackageRef track)
        {
            if (track == null || !track.IsCustomPackage)
                return;

            var hash = TrackPackageRef.NormalizeHash(track.Hash);
            if (string.IsNullOrWhiteSpace(hash))
                return;
            if (!TryGetCachedTrackPackage(hash, out var cached))
                return;

            ApplyTrackPackage(hash, cached);
            SendTrackPackageReady(hash);
            CloseTrackDownloadProgressDialog(hash);
        }

        private void HandleTrackPackageTransferBegin(PacketTrackPackageTransferBegin packet)
        {
            if (packet == null)
                return;

            var hash = TrackPackageRef.NormalizeHash(packet.Hash);
            if (string.IsNullOrWhiteSpace(hash))
                return;
            if (packet.TotalBytes == 0 || packet.TotalBytes > ProtocolConstants.MaxTrackPackageBytes)
                return;

            if (TryGetCachedTrackPackage(hash, out var cached))
            {
                ApplyTrackPackage(hash, cached);
                SendTrackPackageReady(hash);
                CloseTrackDownloadProgressDialog(hash);
                return;
            }

            var transfer = new IncomingTrackPackageTransfer
            {
                TrackId = packet.TrackId ?? string.Empty,
                Version = packet.Version ?? string.Empty,
                Hash = hash,
                DisplayName = ResolveTrackPackageDisplayName(packet.TrackId, packet.Version),
                Bytes = new byte[(int)packet.TotalBytes],
                Offset = 0,
                NextChunkIndex = 0
            };
            _multiplayerTrackPackageTransfers[hash] = transfer;
            ShowTrackDownloadProgressDialog(hash, transfer);
        }

        private void HandleTrackPackageTransferChunk(PacketTrackPackageTransferChunk packet)
        {
            if (packet == null)
                return;

            var hash = TrackPackageRef.NormalizeHash(packet.Hash);
            if (string.IsNullOrWhiteSpace(hash))
                return;
            if (!_multiplayerTrackPackageTransfers.TryGetValue(hash, out var transfer))
                return;
            if (packet.ChunkIndex != transfer.NextChunkIndex)
            {
                _multiplayerTrackPackageTransfers.Remove(hash);
                CloseTrackDownloadProgressDialog(hash);
                return;
            }

            var bytes = packet.Data ?? Array.Empty<byte>();
            if (bytes.Length == 0 || transfer.Offset + bytes.Length > transfer.Bytes.Length)
            {
                _multiplayerTrackPackageTransfers.Remove(hash);
                CloseTrackDownloadProgressDialog(hash);
                return;
            }

            Buffer.BlockCopy(bytes, 0, transfer.Bytes, transfer.Offset, bytes.Length);
            transfer.Offset += bytes.Length;
            transfer.NextChunkIndex++;
            UpdateTrackDownloadProgressDialog(hash, transfer);
        }

        private void HandleTrackPackageTransferEnd(PacketTrackPackageTransferEnd packet)
        {
            if (packet == null)
                return;

            var hash = TrackPackageRef.NormalizeHash(packet.Hash);
            if (string.IsNullOrWhiteSpace(hash))
                return;

            if (!_multiplayerTrackPackageTransfers.TryGetValue(hash, out var transfer))
            {
                if (TryGetCachedTrackPackage(hash, out var cached))
                {
                    ApplyTrackPackage(hash, cached);
                    SendTrackPackageReady(hash);
                }

                CloseTrackDownloadProgressDialog(hash);
                return;
            }

            _multiplayerTrackPackageTransfers.Remove(hash);
            if (transfer.Offset != transfer.Bytes.Length)
            {
                CloseTrackDownloadProgressDialog(hash);
                return;
            }

            if (!TrackPackageCodec.TryDeserialize(transfer.Bytes, out var payload, out _))
            {
                CloseTrackDownloadProgressDialog(hash);
                return;
            }

            var computedHash = TrackPackageCodec.ComputeHash(payload);
            if (!string.Equals(computedHash, hash, StringComparison.OrdinalIgnoreCase))
            {
                CloseTrackDownloadProgressDialog(hash);
                return;
            }

            payload.Manifest.Hash = computedHash;
            payload.Manifest.TrackId = string.IsNullOrWhiteSpace(payload.Manifest.TrackId) ? transfer.TrackId : payload.Manifest.TrackId;
            payload.Manifest.Version = string.IsNullOrWhiteSpace(payload.Manifest.Version) ? transfer.Version : payload.Manifest.Version;
            SaveTrackPackageToCache(computedHash, payload, transfer.Bytes);
            ApplyTrackPackage(computedHash, payload);
            SendTrackPackageReady(computedHash);
            CloseTrackDownloadProgressDialog(hash);
        }

        private void SendTrackPackageReady(string hash)
        {
            var session = _session;
            if (session == null)
                return;

            var normalizedHash = TrackPackageRef.NormalizeHash(hash);
            if (string.IsNullOrWhiteSpace(normalizedHash))
                return;
            session.SendTrackPackageReady(normalizedHash);
        }

        private void ApplyTrackPackage(string hash, TrackPackagePayload payload)
        {
            if (payload == null)
                return;

            var normalizedHash = TrackPackageRef.NormalizeHash(hash);
            if (string.IsNullOrWhiteSpace(normalizedHash))
                return;

            MaterializeTrackPackageAssets(normalizedHash, payload);
            payload.Manifest.Hash = normalizedHash;
            var trackName = string.IsNullOrWhiteSpace(payload.Manifest.TrackId) ? "custom" : payload.Manifest.TrackId;
            var sourcePath = GetTrackPackageAssetRootMarkerPath(normalizedHash);
            var track = TrackPackageCodec.ToTrackData(payload, userDefined: true, sourcePath: sourcePath);
            var laps = _multiplayerCurrentRoomLaps > 0 ? _multiplayerCurrentRoomLaps : payload.Manifest.Laps;
            if (laps == 0)
                laps = 3;
            _multiplayerRaceRuntime.SetTrack(track.WithLaps(laps), trackName, laps);
            if (_multiplayerRaceRuntime.PendingStart)
                StartMultiplayerRace();
        }
    }
}
