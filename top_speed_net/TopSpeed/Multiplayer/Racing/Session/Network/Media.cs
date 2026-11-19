using System;
using TopSpeed.Protocol;

namespace TopSpeed.Drive.Multiplayer
{
    internal sealed partial class MultiplayerSession
    {
        private void ApplyRemoteMediaBeginCore(PacketPlayerMediaBegin media)
        {
            if (media.PlayerNumber == LocalPlayerNumber)
                return;
            if (media.PlayerNumber < _disconnectedPlayerSlots.Length && _disconnectedPlayerSlots[media.PlayerNumber])
                return;
            if (!PacketValidation.IsValidMediaBegin(media))
                return;

            _remoteMediaTransfers[media.PlayerNumber] = new MediaTransfer
            {
                MediaId = media.MediaId,
                TransferId = media.TransferId,
                State = MediaTransferState.Receiving,
                Extension = media.FileExtension,
                Data = new byte[media.TotalBytes],
                Offset = 0,
                NextChunkIndex = 0
            };
        }

        private void ApplyRemoteMediaChunkCore(PacketPlayerMediaChunk media)
        {
            if (media.PlayerNumber == LocalPlayerNumber)
                return;
            if (media.PlayerNumber < _disconnectedPlayerSlots.Length && _disconnectedPlayerSlots[media.PlayerNumber])
                return;
            if (!PacketValidation.IsValidMediaChunk(media))
                return;
            if (!_remoteMediaTransfers.TryGetValue(media.PlayerNumber, out var transfer))
                return;
            if (transfer.MediaId != media.MediaId || transfer.TransferId != media.TransferId)
                return;
            if (transfer.NextChunkIndex != media.ChunkIndex)
                return;
            if (media.Data == null || media.Data.Length == 0)
                return;

            var remaining = transfer.Data.Length - transfer.Offset;
            if (media.Data.Length > remaining)
            {
                transfer.State = MediaTransferState.Cancelled;
                _remoteMediaTransfers.Remove(media.PlayerNumber);
                return;
            }

            Buffer.BlockCopy(media.Data, 0, transfer.Data, transfer.Offset, media.Data.Length);
            transfer.Offset += media.Data.Length;
            transfer.NextChunkIndex++;
            if (transfer.IsComplete)
                transfer.State = MediaTransferState.Complete;
        }

        private void ApplyRemoteMediaEndCore(PacketPlayerMediaEnd media)
        {
            if (media.PlayerNumber == LocalPlayerNumber)
                return;
            if (media.PlayerNumber < _disconnectedPlayerSlots.Length && _disconnectedPlayerSlots[media.PlayerNumber])
                return;
            if (!PacketValidation.IsValidMediaEnd(media))
                return;
            if (!_remoteMediaTransfers.TryGetValue(media.PlayerNumber, out var transfer))
                return;
            if (transfer.MediaId != media.MediaId || transfer.TransferId != media.TransferId)
                return;
            if (!transfer.IsComplete)
                return;
            transfer.State = MediaTransferState.Complete;
            if (_remoteLiveStates.TryGetValue(media.PlayerNumber, out var live) && live.StreamId != 0)
                return;
            if (!_remotePlayers.TryGetValue(media.PlayerNumber, out var remote))
                return;

            remote.Player.ApplyRadioMedia(transfer.MediaId, transfer.Extension, transfer.Data);
            _remoteMediaTransfers.Remove(media.PlayerNumber);
        }
    }
}
