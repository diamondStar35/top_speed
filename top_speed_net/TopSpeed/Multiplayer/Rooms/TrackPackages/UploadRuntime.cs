using System;
using TopSpeed.Localization;
using TopSpeed.Protocol;

namespace TopSpeed.Core.Multiplayer
{
    internal sealed partial class MultiplayerCoordinator
    {
        public void HandleTrackPackageUploadResult(PacketTrackPackageUploadResult result)
        {
            _roomsFlow.HandleTrackPackageUploadResult(result);
        }

        internal void HandleTrackPackageUploadResultCore(PacketTrackPackageUploadResult result)
        {
            if (result == null)
                return;

            var upload = _pendingTrackUpload;
            if (upload == null || result.UploadId != upload.UploadId)
            {
                if (!string.IsNullOrWhiteSpace(result.Message))
                    _speech.Speak(result.Message);
                return;
            }

            var wasCanceled = upload.CancelRequested;
            var shouldReopenCatalog = _state.RoomDrafts.RoomTrackUploadReturnToCatalog;
            _state.RoomDrafts.RoomTrackUploadReturnToCatalog = false;
            _trackUploadProgressOpen = false;
            _pendingTrackUpload = null;
            _dialogs.CloseActive();

            if (wasCanceled)
            {
                _speech.Speak(LocalizationService.Mark("Track upload canceled."));
                return;
            }

            if (result.Status == TrackPackageUploadStatus.Accepted || result.Status == TrackPackageUploadStatus.Reused)
            {
                _speech.Speak(string.IsNullOrWhiteSpace(result.Message)
                    ? LocalizationService.Mark("Track package uploaded successfully.")
                    : result.Message);
                RequestRoomTrackCatalog(openOnResponse: shouldReopenCatalog);
                return;
            }

            _speech.Speak(string.IsNullOrWhiteSpace(result.Message)
                ? LocalizationService.Mark("Track package upload failed.")
                : result.Message);
        }

        private void StartLocalTrackUpload(string trackFile, string displayName)
        {
            if (_pendingTrackUpload != null)
            {
                _speech.Speak(LocalizationService.Mark("A track upload is already in progress."));
                return;
            }

            if (!TryBuildTrackUpload(trackFile, displayName, out var pending, out var error))
            {
                _speech.Speak(string.IsNullOrWhiteSpace(error)
                    ? LocalizationService.Mark("Unable to prepare custom track upload.")
                    : error);
                return;
            }

            _pendingTrackUpload = pending;
            _trackUploadProgressOpen = true;
            _state.RoomDrafts.RoomTrackUploadReturnToCatalog = true;
            ShowTrackUploadProgressDialog();
        }

        private bool UpdateTrackPackageUploadOperation()
        {
            var upload = _pendingTrackUpload;
            if (upload == null)
                return false;

            if (_trackUploadProgressOpen)
                ShowTrackUploadProgressDialog();

            var session = SessionOrNull();
            if (session == null)
            {
                _trackUploadProgressOpen = false;
                _pendingTrackUpload = null;
                _state.RoomDrafts.RoomTrackUploadReturnToCatalog = false;
                _speech.Speak(LocalizationService.Mark("Track upload canceled because the connection was lost."));
                return false;
            }

            if (!upload.BeginSent)
            {
                if (!TrySend(session.SendTrackPackageUploadBegin(new PacketTrackPackageUploadBegin
                    {
                        UploadId = upload.UploadId,
                        TrackId = upload.Track.TrackId,
                        Version = upload.Track.Version,
                        Hash = upload.Track.Hash,
                        TotalBytes = (uint)upload.Bytes.Length
                    }),
                    LocalizationService.Mark("track upload start")))
                {
                    _trackUploadProgressOpen = false;
                    _pendingTrackUpload = null;
                    _state.RoomDrafts.RoomTrackUploadReturnToCatalog = false;
                    return false;
                }

                upload.BeginSent = true;
                return true;
            }

            if (upload.WaitingForResult)
                return true;

            if (upload.CancelRequested)
            {
                if (!upload.EndSent)
                {
                    if (!TrySend(session.SendTrackPackageUploadEnd(new PacketTrackPackageUploadEnd { UploadId = upload.UploadId }), LocalizationService.Mark("track upload cancel")))
                    {
                        _trackUploadProgressOpen = false;
                        _pendingTrackUpload = null;
                        _state.RoomDrafts.RoomTrackUploadReturnToCatalog = false;
                        return false;
                    }

                    upload.EndSent = true;
                    upload.WaitingForResult = true;
                }

                return true;
            }

            var chunksSent = 0;
            while (upload.Offset < upload.Bytes.Length && chunksSent < 8)
            {
                var remaining = upload.Bytes.Length - upload.Offset;
                var length = Math.Min(ProtocolConstants.MaxTrackPackageChunkBytes, remaining);
                var chunkBytes = new byte[length];
                Buffer.BlockCopy(upload.Bytes, upload.Offset, chunkBytes, 0, length);

                if (!TrySend(session.SendTrackPackageUploadChunk(new PacketTrackPackageUploadChunk
                    {
                        UploadId = upload.UploadId,
                        ChunkIndex = upload.NextChunkIndex,
                        Data = chunkBytes
                    }),
                    LocalizationService.Mark("track upload chunk")))
                {
                    _trackUploadProgressOpen = false;
                    _pendingTrackUpload = null;
                    _state.RoomDrafts.RoomTrackUploadReturnToCatalog = false;
                    return false;
                }

                upload.Offset += length;
                upload.NextChunkIndex++;
                chunksSent++;
            }

            if (upload.Offset >= upload.Bytes.Length && !upload.EndSent)
            {
                if (!TrySend(session.SendTrackPackageUploadEnd(new PacketTrackPackageUploadEnd { UploadId = upload.UploadId }), LocalizationService.Mark("track upload completion")))
                {
                    _trackUploadProgressOpen = false;
                    _pendingTrackUpload = null;
                    _state.RoomDrafts.RoomTrackUploadReturnToCatalog = false;
                    return false;
                }

                upload.EndSent = true;
                upload.WaitingForResult = true;
            }

            return true;
        }

        private void RequestTrackUploadCancel()
        {
            var upload = _pendingTrackUpload;
            if (upload == null)
                return;

            upload.CancelRequested = true;
        }
    }
}
