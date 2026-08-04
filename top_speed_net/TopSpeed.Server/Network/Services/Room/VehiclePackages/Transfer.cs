using System;
using TopSpeed.Protocol;
using TopSpeed.Server.Protocol;

namespace TopSpeed.Server.Network
{
    internal sealed partial class RaceServer
    {
        private void DistributeVehiclePackageToRoom(GameRoom room, VehiclePackageRecord record)
        {
            if (room == null || record == null || record.Bytes == null || record.Bytes.Length == 0)
                return;

            foreach (var playerId in room.PlayerIds)
            {
                if (_players.TryGetValue(playerId, out var player) && player != null)
                    SendVehiclePackageToPlayer(player, record);
            }
        }

        private void BroadcastPlayerVehicle(GameRoom room, byte playerNumber, string hash)
        {
            if (room == null)
                return;

            var payload = PacketSerializer.WriteRoomPlayerVehicle(new PacketRoomPlayerVehicle
            {
                PlayerNumber = playerNumber,
                Hash = VehiclePackageRef.NormalizeHash(hash)
            });
            _notify.ToRoom(room, payload, PacketStream.Room);
        }

        private void SendVehiclePackageToPlayer(PlayerConnection player, VehiclePackageRecord record)
        {
            if (player == null || record == null || record.Bytes == null || record.Bytes.Length == 0)
                return;

            SendStream(player, PacketSerializer.WriteVehiclePackageTransferBegin(new PacketVehiclePackageTransferBegin
            {
                VehicleId = record.Ref.VehicleId,
                Version = record.Ref.Version,
                Hash = record.Ref.Hash,
                TotalBytes = (uint)record.Bytes.Length
            }), PacketStream.Room);

            var chunkSize = ProtocolConstants.MaxVehiclePackageChunkBytes;
            var chunkIndex = 0;
            var offset = 0;
            while (offset < record.Bytes.Length)
            {
                var length = Math.Min(chunkSize, record.Bytes.Length - offset);
                var chunk = new byte[length];
                Buffer.BlockCopy(record.Bytes, offset, chunk, 0, length);
                SendStream(player, PacketSerializer.WriteVehiclePackageTransferChunk(new PacketVehiclePackageTransferChunk
                {
                    Hash = record.Ref.Hash,
                    ChunkIndex = (ushort)chunkIndex,
                    Data = chunk
                }), PacketStream.Room);
                offset += length;
                chunkIndex++;
            }

            SendStream(player, PacketSerializer.WriteVehiclePackageTransferEnd(new PacketVehiclePackageTransferEnd
            {
                Hash = record.Ref.Hash
            }), PacketStream.Room);
        }
    }
}
