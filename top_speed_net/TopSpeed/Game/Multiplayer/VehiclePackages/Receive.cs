using System;
using System.Collections.Generic;
using TopSpeed.Protocol;

namespace TopSpeed.Game
{
    internal sealed partial class Game
    {
        // playerNumber -> selected custom vehicle package hash (empty/absent = built-in).
        private readonly Dictionary<byte, string> _multiplayerPlayerVehicleHashes = new Dictionary<byte, string>();

        private void HandleRoomPlayerVehicle(PacketRoomPlayerVehicle packet)
        {
            if (packet == null)
                return;

            var hash = VehiclePackageRef.NormalizeHash(packet.Hash);
            _multiplayerPlayerVehicleHashes.TryGetValue(packet.PlayerNumber, out var previous);
            if (string.IsNullOrWhiteSpace(hash))
                _multiplayerPlayerVehicleHashes.Remove(packet.PlayerNumber);
            else
                _multiplayerPlayerVehicleHashes[packet.PlayerNumber] = hash;

            // The authoritative broadcast at race start (post player-number shuffle) can arrive after
            // a snapshot already built this player's remote car from a stale mapping. If the vehicle
            // for this number changed, drop the remote car so the next snapshot rebuilds it with the
            // correct vehicle. markDisconnected:false so it is allowed to be recreated.
            if (!string.Equals(previous ?? string.Empty, hash ?? string.Empty, StringComparison.OrdinalIgnoreCase))
                _multiplayerRaceRuntime.Mode?.RemoveRemotePlayer(packet.PlayerNumber, markDisconnected: false);
        }

        // Materialized custom-vehicle .tsv path for a remote player, or null when they use a
        // built-in vehicle or the package is not (yet) cached.
        // Remote peers are keyed by their network player number (consistent within a race between
        // the RoomPlayerVehicle broadcast and the race snapshots).
        private string? ResolveRemoteCustomVehicleFile(byte playerNumber)
        {
            if (!_multiplayerPlayerVehicleHashes.TryGetValue(playerNumber, out var hash))
                return null;
            return ResolveCustomVehicleFileByHash(hash);
        }

        // The local player's own car is resolved from its selection hash directly, not the network
        // player number (which is not stable across races).
        private string? ResolveCustomVehicleFileByHash(string? hash)
        {
            var normalizedHash = VehiclePackageRef.NormalizeHash(hash ?? string.Empty);
            if (string.IsNullOrWhiteSpace(normalizedHash))
                return null;
            if (!TryGetCachedVehiclePackage(normalizedHash, out var package))
                return null;
            // A downloaded vehicle lives in the session cache; a locally-reused one lives in the
            // client's own Vehicles folder. Either way use the package's actual .tsv path.
            return string.IsNullOrWhiteSpace(package.TsvPath) ? null : package.TsvPath;
        }

        // Display name for a remote player's custom vehicle, keyed by network player number.
        private string? ResolveRemoteCustomVehicleName(byte playerNumber)
        {
            if (!_multiplayerPlayerVehicleHashes.TryGetValue(playerNumber, out var hash))
                return null;
            return ResolveCustomVehicleNameByHash(hash);
        }

        // The authoritative display name comes from the package manifest / vehicle metadata, not the
        // .tsv filename (downloaded packages are stored as "vehicle.tsv"; kept ones carry a hash suffix).
        private string? ResolveCustomVehicleNameByHash(string? hash)
        {
            var normalizedHash = VehiclePackageRef.NormalizeHash(hash ?? string.Empty);
            if (string.IsNullOrWhiteSpace(normalizedHash))
                return null;
            if (!TryGetCachedVehiclePackage(normalizedHash, out var package))
                return null;
            return string.IsNullOrWhiteSpace(package.DisplayName) ? null : package.DisplayName;
        }

        private sealed class IncomingVehiclePackageTransfer
        {
            public string VehicleId = string.Empty;
            public string Version = string.Empty;
            public string Hash = string.Empty;
            public byte[] Bytes = Array.Empty<byte>();
            public int Offset;
            public ushort NextChunkIndex;
        }

        private readonly Dictionary<string, IncomingVehiclePackageTransfer> _multiplayerVehiclePackageTransfers =
            new Dictionary<string, IncomingVehiclePackageTransfer>(StringComparer.OrdinalIgnoreCase);

        // Custom vehicles present during the current race, for the post-race "keep?" prompt
        // (deduped). This tracks every package the race used, not only freshly downloaded ones: a
        // vehicle downloaded in an earlier race of the same run still sits in the session cache, so
        // it never downloads again and would otherwise never be offered for keeping even though it
        // is not saved anywhere permanent. Entries already kept on disk are filtered out when the
        // prompt is built.
        private readonly HashSet<string> _multiplayerVehiclePackagesSeenThisRace =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        private void ResetMultiplayerVehiclePackageState()
        {
            _multiplayerVehiclePackageTransfers.Clear();
        }

        private void HandleVehiclePackageTransferBegin(PacketVehiclePackageTransferBegin packet)
        {
            if (packet == null)
                return;

            var hash = VehiclePackageRef.NormalizeHash(packet.Hash);
            if (string.IsNullOrWhiteSpace(hash))
                return;
            if (packet.TotalBytes == 0 || packet.TotalBytes > ProtocolConstants.MaxVehiclePackageBytes)
                return;

            if (TryGetCachedVehiclePackage(hash, out _))
            {
                _multiplayerVehiclePackagesSeenThisRace.Add(hash);
                SendVehiclePackageReady(hash);
                return;
            }

            _multiplayerVehiclePackageTransfers[hash] = new IncomingVehiclePackageTransfer
            {
                VehicleId = packet.VehicleId ?? string.Empty,
                Version = packet.Version ?? string.Empty,
                Hash = hash,
                Bytes = new byte[(int)packet.TotalBytes],
                Offset = 0,
                NextChunkIndex = 0
            };
        }

        private void HandleVehiclePackageTransferChunk(PacketVehiclePackageTransferChunk packet)
        {
            if (packet == null)
                return;

            var hash = VehiclePackageRef.NormalizeHash(packet.Hash);
            if (string.IsNullOrWhiteSpace(hash))
                return;
            if (!_multiplayerVehiclePackageTransfers.TryGetValue(hash, out var transfer))
                return;
            if (packet.ChunkIndex != transfer.NextChunkIndex)
            {
                _multiplayerVehiclePackageTransfers.Remove(hash);
                return;
            }

            var bytes = packet.Data ?? Array.Empty<byte>();
            if (bytes.Length == 0 || transfer.Offset + bytes.Length > transfer.Bytes.Length)
            {
                _multiplayerVehiclePackageTransfers.Remove(hash);
                return;
            }

            Buffer.BlockCopy(bytes, 0, transfer.Bytes, transfer.Offset, bytes.Length);
            transfer.Offset += bytes.Length;
            transfer.NextChunkIndex++;
        }

        private void HandleVehiclePackageTransferEnd(PacketVehiclePackageTransferEnd packet)
        {
            if (packet == null)
                return;

            var hash = VehiclePackageRef.NormalizeHash(packet.Hash);
            if (string.IsNullOrWhiteSpace(hash))
                return;

            if (!_multiplayerVehiclePackageTransfers.TryGetValue(hash, out var transfer))
            {
                if (TryGetCachedVehiclePackage(hash, out _))
                {
                    _multiplayerVehiclePackagesSeenThisRace.Add(hash);
                    SendVehiclePackageReady(hash);
                }

                return;
            }

            _multiplayerVehiclePackageTransfers.Remove(hash);
            if (transfer.Offset != transfer.Bytes.Length)
                return;

            if (!VehiclePackageCodec.TryDeserialize(transfer.Bytes, out var payload, out _))
                return;

            var computedHash = VehiclePackageCodec.ComputeHash(payload);
            if (!string.Equals(computedHash, hash, StringComparison.OrdinalIgnoreCase))
                return;

            payload.Manifest.Hash = computedHash;
            if (!TryMaterializeAndCacheVehiclePackage(computedHash, payload, out _))
                return;

            _multiplayerVehiclePackagesSeenThisRace.Add(computedHash);
            SendVehiclePackageReady(computedHash);
        }

        private void SendVehiclePackageReady(string hash)
        {
            var session = _session;
            if (session == null)
                return;

            var normalizedHash = VehiclePackageRef.NormalizeHash(hash);
            if (string.IsNullOrWhiteSpace(normalizedHash))
                return;
            session.SendVehiclePackageReady(normalizedHash);
        }
    }
}
