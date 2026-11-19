using System;
using TopSpeed.Drive.Multiplayer;
using TopSpeed.Input;
using TopSpeed.Protocol;
using TopSpeed.Vehicles;
using TS.Audio;

namespace TopSpeed.Game.Multiplayer.Communicator
{
    internal sealed partial class MultiplayerCommunicatorRuntime
    {
        public void ApplyRemoteMediaBegin(PacketPlayerMediaBegin begin)
        {
            if (begin == null || !PacketValidation.IsValidCommunicatorMediaBegin(begin))
                return;

            if (_boundSession != null && begin.PlayerId == _boundSession.PlayerId)
                return;

            _remoteMediaTransfers[begin.PlayerId] = new MediaTransfer
            {
                MediaId = begin.MediaId,
                TransferId = begin.TransferId,
                State = MediaTransferState.Receiving,
                Extension = begin.FileExtension,
                Data = new byte[begin.TotalBytes],
                Offset = 0,
                NextChunkIndex = 0
            };
        }

        public void ApplyRemoteMediaChunk(PacketPlayerMediaChunk chunk)
        {
            if (chunk == null || !PacketValidation.IsValidMediaChunk(chunk))
                return;

            if (!_remoteMediaTransfers.TryGetValue(chunk.PlayerId, out var transfer))
                return;
            if (transfer.MediaId != chunk.MediaId || transfer.TransferId != chunk.TransferId)
                return;
            if (transfer.NextChunkIndex != chunk.ChunkIndex)
                return;
            if (chunk.Data == null || chunk.Data.Length == 0)
                return;

            var remaining = transfer.Data.Length - transfer.Offset;
            if (chunk.Data.Length > remaining)
            {
                transfer.State = MediaTransferState.Cancelled;
                _remoteMediaTransfers.Remove(chunk.PlayerId);
                return;
            }

            Buffer.BlockCopy(chunk.Data, 0, transfer.Data, transfer.Offset, chunk.Data.Length);
            transfer.Offset += chunk.Data.Length;
            transfer.NextChunkIndex++;
            if (transfer.IsComplete)
                transfer.State = MediaTransferState.Complete;
        }

        public void ApplyRemoteMediaEnd(PacketPlayerMediaEnd end)
        {
            if (end == null || !PacketValidation.IsValidMediaEnd(end))
                return;

            if (!_remoteMediaTransfers.TryGetValue(end.PlayerId, out var transfer))
                return;
            if (transfer.MediaId != end.MediaId || transfer.TransferId != end.TransferId)
                return;
            if (!transfer.IsComplete)
                return;

            ApplyCompletedRemoteMediaTransfer(end.PlayerId, transfer);
            _remoteMediaTransfers.Remove(end.PlayerId);
        }

        public void ApplyRemoteMediaState(PacketPlayerCommunicatorMediaState state)
        {
            if (state == null || !PacketValidation.IsValidCommunicatorMediaState(state))
                return;

            if (_boundSession != null && state.PlayerId == _boundSession.PlayerId)
                return;

            var normalized = CloneMediaState(state);
            if (!normalized.MediaLoaded)
            {
                _remoteMediaTransfers.Remove(normalized.PlayerId);
                _remoteMediaStates.Remove(normalized.PlayerId);
                if (_remoteMediaControllers.TryGetValue(normalized.PlayerId, out var stale))
                {
                    stale.Dispose();
                    _remoteMediaControllers.Remove(normalized.PlayerId);
                }

                return;
            }

            _remoteMediaStates[normalized.PlayerId] = normalized;

            if (!_remoteMediaControllers.TryGetValue(normalized.PlayerId, out var controller))
                return;

            controller.SetVolumePercent(normalized.VolumePercent);
            controller.RefreshCategoryVolume();
            if (normalized.MediaId == 0)
            {
                controller.ClearMedia();
                return;
            }

            var localFrequencyTenths = _multiplayer.CommunicatorEnabled
                ? _multiplayer.CommunicatorFrequencyTenths
                : (ushort)0;
            var shouldPlay = controller.HasMedia
                             && controller.MediaId == normalized.MediaId
                             && normalized.MediaPlaying
                             && IsAudibleForLocalFrequency(normalized.FrequencyTenths, localFrequencyTenths);
            controller.SetPlayback(shouldPlay);
        }

        private void ApplyCompletedRemoteMediaTransfer(uint playerId, MediaTransfer transfer)
        {
            var controller = GetOrCreateRemoteMediaController(playerId);
            if (!controller.TryLoadFromBytes(transfer.Data, transfer.Extension, transfer.MediaId, preservePlaybackState: false, out _))
                return;

            if (!_remoteMediaStates.TryGetValue(playerId, out var state))
            {
                controller.SetVolumePercent(100);
                controller.SetPlayback(false);
                return;
            }

            controller.SetVolumePercent(state.VolumePercent);
            controller.RefreshCategoryVolume();
            var localFrequencyTenths = _multiplayer.CommunicatorEnabled
                ? _multiplayer.CommunicatorFrequencyTenths
                : (ushort)0;
            var shouldPlay = state.MediaLoaded
                             && state.MediaPlaying
                             && state.MediaId == transfer.MediaId
                             && IsAudibleForLocalFrequency(state.FrequencyTenths, localFrequencyTenths);
            controller.SetPlayback(shouldPlay);
        }

        private PacketPlayerCommunicatorMediaState CloneMediaState(PacketPlayerCommunicatorMediaState source)
        {
            var loaded = source.MediaLoaded && source.MediaId != 0;
            var playing = loaded && source.MediaPlaying;
            return new PacketPlayerCommunicatorMediaState
            {
                PlayerId = source.PlayerId,
                PlayerNumber = source.PlayerNumber,
                MediaId = loaded ? source.MediaId : 0u,
                FrequencyTenths = source.FrequencyTenths,
                MediaLoaded = loaded,
                MediaPlaying = playing,
                VolumePercent = source.VolumePercent
            };
        }

        private void UpdateRemoteMediaAudibility(ushort localFrequencyTenths)
        {
            foreach (var pair in _remoteMediaControllers)
            {
                var playerId = pair.Key;
                var controller = pair.Value;
                controller.RefreshCategoryVolume();

                if (!_remoteMediaStates.TryGetValue(playerId, out var state))
                {
                    controller.SetPlayback(false);
                    continue;
                }

                var shouldPlay = state.MediaLoaded
                                 && state.MediaPlaying
                                 && controller.HasMedia
                                 && controller.MediaId == state.MediaId
                                 && IsAudibleForLocalFrequency(state.FrequencyTenths, localFrequencyTenths);
                controller.SetPlayback(shouldPlay);
            }
        }

        private VehicleRadioController GetOrCreateRemoteMediaController(uint playerId)
        {
            if (_remoteMediaControllers.TryGetValue(playerId, out var existing))
                return existing;

            var created = new VehicleRadioController(
                _audio,
                new VehicleRadioController.PlaybackOptions(
                    AudioEngineOptions.RadioBusName,
                    spatialize: false,
                    allowHrtf: false,
                    _settings,
                    AudioVolumeCategory.Communicator,
                    "CommunicatorRemote"));
            _remoteMediaControllers[playerId] = created;
            return created;
        }

        private void ClearRemoteMedia()
        {
            foreach (var controller in _remoteMediaControllers.Values)
                controller.Dispose();

            _remoteMediaControllers.Clear();
            _remoteMediaStates.Clear();
            _remoteMediaTransfers.Clear();
        }
    }
}
