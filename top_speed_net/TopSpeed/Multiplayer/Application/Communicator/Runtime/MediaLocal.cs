using System;
using System.IO;
using TopSpeed.Audio;
using TopSpeed.Core.Multiplayer;
using TopSpeed.Input;
using TopSpeed.Localization;
using TopSpeed.Protocol;

namespace TopSpeed.Game.Multiplayer.Communicator
{
    internal sealed partial class MultiplayerCommunicatorRuntime
    {
        private void HandleCommunicatorMediaUpdate(Network.MultiplayerSession session, ushort localAudibleFrequencyTenths)
        {
            ProcessPendingMediaSelection(session);
            ProcessPendingMediaFolderSelection(session);
            HandleMediaShortcuts(session);
            HandleLocalMediaPlaybackEndAdvance(session);
            GateLocalMediaPlayback(localAudibleFrequencyTenths);
            _localMedia.RefreshCategoryVolume();
            TrySendLocalMediaState(session, force: false);
        }

        private void HandleMediaShortcuts(Network.MultiplayerSession session)
        {
            if (_isInputBlocked())
                return;

            if (WasShortcutPressed(CommunicatorShortcutIds.MediaLoadFile))
                OpenCommunicatorMediaFile();

            if (WasShortcutPressed(CommunicatorShortcutIds.MediaLoadFolder))
                OpenCommunicatorMediaFolder();

            if (WasShortcutPressed(CommunicatorShortcutIds.MediaPlayPause))
                ToggleMediaPlayback(session);

            if (WasShortcutPressed(CommunicatorShortcutIds.MediaToggleLoop))
                ToggleMediaLoop();

            if (WasShortcutPressed(CommunicatorShortcutIds.MediaPreviousTrack))
                CycleMediaTrack(session, -1);
            else if (WasShortcutPressed(CommunicatorShortcutIds.MediaNextTrack))
                CycleMediaTrack(session, 1);

            if (WasShortcutPressed(CommunicatorShortcutIds.MediaVolumeUp))
                AdjustMediaVolume(session, MediaVolumeStepPercent);
            else if (WasShortcutPressed(CommunicatorShortcutIds.MediaVolumeDown))
                AdjustMediaVolume(session, -MediaVolumeStepPercent);

            if (WasShortcutPressed(CommunicatorShortcutIds.MediaToggleShuffle))
                ToggleMediaShuffle();
        }

        private void OpenCommunicatorMediaFile()
        {
            if (_mediaPickerInProgress)
                return;

            _mediaPickerInProgress = true;
            _fileDialogs.PickAudioFile(selectedPath =>
            {
                lock (_mediaSelectionLock)
                    _pendingSelectedMediaPath = selectedPath;

                _mediaPickerInProgress = false;
            });
        }

        private void OpenCommunicatorMediaFolder()
        {
            if (_mediaFolderPickerInProgress)
                return;

            _mediaFolderPickerInProgress = true;
            _fileDialogs.PickFolder(_mediaPlaylistFolder, selectedFolder =>
            {
                lock (_mediaSelectionLock)
                    _pendingSelectedMediaFolder = selectedFolder;

                _mediaFolderPickerInProgress = false;
            });
        }

        private void ProcessPendingMediaSelection(Network.MultiplayerSession session)
        {
            string? selectedPath;
            lock (_mediaSelectionLock)
            {
                selectedPath = _pendingSelectedMediaPath;
                _pendingSelectedMediaPath = null;
            }

            if (string.IsNullOrWhiteSpace(selectedPath))
                return;

            string fullPath;
            try
            {
                fullPath = Path.GetFullPath(selectedPath);
            }
            catch
            {
                _announce(LocalizationService.Mark("The selected media file does not exist."));
                return;
            }

            if (!File.Exists(fullPath))
            {
                _announce(LocalizationService.Mark("The selected media file does not exist."));
                return;
            }

            _mediaPlaylist.Clear();
            _mediaPlaylist.Add(fullPath);
            _mediaPlaylistIndex = 0;
            _mediaPlaylistFolder = string.Empty;
            ApplyMediaLoopMode();

            LoadMediaPlaylistEntry(session, _mediaPlaylistIndex, preservePlaybackState: true, announceLoaded: true);
        }

        private void ProcessPendingMediaFolderSelection(Network.MultiplayerSession session)
        {
            string? selectedFolder;
            lock (_mediaSelectionLock)
            {
                selectedFolder = _pendingSelectedMediaFolder;
                _pendingSelectedMediaFolder = null;
            }

            if (string.IsNullOrWhiteSpace(selectedFolder))
                return;

            if (!BuildMediaPlaylistFromFolder(selectedFolder, preserveCurrentMedia: false, announceErrors: true))
                return;

            if (!LoadMediaPlaylistEntry(session, _mediaPlaylistIndex, preservePlaybackState: true, announceLoaded: true))
                return;

            _announce(_mediaShuffleMode
                ? LocalizationService.Mark("Shuffle mode on.")
                : LocalizationService.Mark("Shuffle mode off."));
        }

        private bool BuildMediaPlaylistFromFolder(string folderPath, bool preserveCurrentMedia, bool announceErrors)
        {
            if (!MediaPlaylist.TryBuildFromFolder(
                    folderPath,
                    _mediaShuffleMode,
                    _random,
                    out var fullFolder,
                    out var files,
                    out var error))
            {
                if (announceErrors)
                    AnnounceMediaFolderError(error);
                return false;
            }

            var currentPath = preserveCurrentMedia ? _localMedia.MediaPath : null;

            _mediaPlaylist.Clear();
            _mediaPlaylist.AddRange(files);
            _mediaPlaylistFolder = fullFolder;
            _mediaPlaylistIndex = 0;
            if (!string.IsNullOrWhiteSpace(currentPath))
            {
                var idx = _mediaPlaylist.FindIndex(path => string.Equals(path, currentPath, StringComparison.OrdinalIgnoreCase));
                if (idx >= 0)
                    _mediaPlaylistIndex = idx;
            }

            _settings.RadioLastFolder = _mediaPlaylistFolder;
            _settings.RadioShuffle = _mediaShuffleMode;
            _saveSettings();
            ApplyMediaLoopMode();
            return true;
        }

        private void TryRestoreMediaFolderPlaylist()
        {
            if (string.IsNullOrWhiteSpace(_settings.RadioLastFolder))
                return;

            BuildMediaPlaylistFromFolder(_settings.RadioLastFolder, preserveCurrentMedia: false, announceErrors: false);
        }

        private void ToggleMediaPlayback(Network.MultiplayerSession session)
        {
            if (_mediaPlaylist.Count == 0 && !_localMedia.HasMedia)
            {
                _announce(LocalizationService.Mark("No communicator media loaded."));
                return;
            }

            if (!_localMedia.HasMedia)
            {
                SelectMediaPlaylistIndexFromCurrentMedia();
                if (_mediaPlaylistIndex < 0)
                    _mediaPlaylistIndex = 0;
                if (!LoadMediaPlaylistEntry(session, _mediaPlaylistIndex, preservePlaybackState: false, announceLoaded: true))
                    return;
                _localMedia.SetPlayback(true);
                _announce(LocalizationService.Mark("playing"));
                TrySendLocalMediaState(session, force: true);
                _lastObservedMediaPlaying = _localMedia.HasMedia && _localMedia.IsPlaying;
                return;
            }

            _localMedia.TogglePlayback();
            _announce(_localMedia.DesiredPlaying
                ? LocalizationService.Mark("playing")
                : LocalizationService.Mark("paused"));
            TrySendLocalMediaState(session, force: true);
        }

        private void CycleMediaTrack(Network.MultiplayerSession session, int delta)
        {
            if (_mediaPlaylist.Count == 0)
            {
                _announce(LocalizationService.Mark("No folder playlist loaded."));
                return;
            }

            SelectMediaPlaylistIndexFromCurrentMedia();
            if (!StepMediaPlaylistIndex(delta))
                return;

            LoadMediaPlaylistEntry(session, _mediaPlaylistIndex, preservePlaybackState: true, announceLoaded: true);
        }

        private void ToggleMediaShuffle()
        {
            _mediaShuffleMode = !_mediaShuffleMode;
            _settings.RadioShuffle = _mediaShuffleMode;
            _saveSettings();

            var lastFolder = _settings.RadioLastFolder ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(lastFolder))
                BuildMediaPlaylistFromFolder(lastFolder, preserveCurrentMedia: true, announceErrors: false);

            _announce(_mediaShuffleMode
                ? LocalizationService.Mark("Shuffle mode on.")
                : LocalizationService.Mark("Shuffle mode off."));
        }

        private void ToggleMediaLoop()
        {
            _mediaLoopMode = !_mediaLoopMode;
            ApplyMediaLoopMode();
            _announce(_mediaLoopMode
                ? LocalizationService.Mark("Loop mode on.")
                : LocalizationService.Mark("Loop mode off."));
        }

        private void AdjustMediaVolume(Network.MultiplayerSession session, int deltaPercent)
        {
            var current = _localMedia.VolumePercent;
            var next = current + deltaPercent;
            if (next < 0)
                next = 0;
            else if (next > 100)
                next = 100;

            if (next == current)
                return;

            _localMedia.SetVolumePercent(next);
            _announce(LocalizationService.Format(LocalizationService.Mark("{0} percent."), next));
            if (deltaPercent > 0)
                PlayVolumeFeedback(increase: true);
            else if (deltaPercent < 0)
                PlayVolumeFeedback(increase: false);
            TrySendLocalMediaState(session, force: true);
        }

        private bool LoadMediaPlaylistEntry(
            Network.MultiplayerSession session,
            int index,
            bool preservePlaybackState,
            bool announceLoaded)
        {
            if (index < 0 || index >= _mediaPlaylist.Count)
                return false;

            var mediaPath = _mediaPlaylist[index];
            _mediaPlaylistIndex = index;
            ApplyMediaLoopMode();

            var mediaId = NextMediaId();
            if (!_localMedia.TryLoadFromFile(mediaPath, mediaId, preservePlaybackState, out var error))
            {
                _announce(LocalizationService.Format(
                    LocalizationService.Mark("Failed to load communicator media. {0}"),
                    error));
                return false;
            }

            if (!session.SendCommunicatorMediaStreamed(mediaId, mediaPath, _multiplayer.CommunicatorFrequencyTenths))
                _announce(LocalizationService.Mark("Failed to stream communicator media to the server."));

            if (announceLoaded)
            {
                var fileName = MediaPlaylist.GetDisplayName(mediaPath);
                _announce(fileName);
            }

            TrySendLocalMediaState(session, force: true);
            _lastObservedMediaPlaying = _localMedia.HasMedia && _localMedia.IsPlaying;
            return true;
        }

        private void SelectMediaPlaylistIndexFromCurrentMedia()
        {
            if (_mediaPlaylist.Count == 0)
            {
                _mediaPlaylistIndex = -1;
                return;
            }

            var currentPath = _localMedia.MediaPath;
            if (!string.IsNullOrWhiteSpace(currentPath))
            {
                var idx = _mediaPlaylist.FindIndex(path => string.Equals(path, currentPath, StringComparison.OrdinalIgnoreCase));
                if (idx >= 0)
                {
                    _mediaPlaylistIndex = idx;
                    return;
                }
            }

            if (_mediaPlaylistIndex < 0 || _mediaPlaylistIndex >= _mediaPlaylist.Count)
                _mediaPlaylistIndex = 0;
        }

        private bool StepMediaPlaylistIndex(int delta)
        {
            if (_mediaPlaylist.Count == 0)
                return false;

            if (_mediaPlaylistIndex < 0 || _mediaPlaylistIndex >= _mediaPlaylist.Count)
                _mediaPlaylistIndex = 0;
            else
                _mediaPlaylistIndex += delta;

            while (_mediaPlaylistIndex < 0)
                _mediaPlaylistIndex += _mediaPlaylist.Count;
            while (_mediaPlaylistIndex >= _mediaPlaylist.Count)
                _mediaPlaylistIndex -= _mediaPlaylist.Count;

            return true;
        }

        private void ApplyMediaLoopMode()
        {
            _localMedia.SetLoopPlayback(_mediaLoopMode || _mediaPlaylist.Count <= 1);
        }

        private void HandleLocalMediaPlaybackEndAdvance(Network.MultiplayerSession session)
        {
            var isPlaying = _localMedia.HasMedia && _localMedia.IsPlaying;
            if (_localMedia.HasMedia && _localMedia.DesiredPlaying && !_localMedia.IsPaused && _lastObservedMediaPlaying && !isPlaying)
            {
                if (_mediaPlaylist.Count > 1 && !_localMedia.LoopPlayback)
                {
                    if (StepMediaPlaylistIndex(1))
                        LoadMediaPlaylistEntry(session, _mediaPlaylistIndex, preservePlaybackState: true, announceLoaded: true);
                }
            }

            _lastObservedMediaPlaying = isPlaying;
        }

        private void GateLocalMediaPlayback(ushort localAudibleFrequencyTenths)
        {
            if (_localMedia.HasMedia && _localMedia.DesiredPlaying && localAudibleFrequencyTenths == 0)
                _localMedia.SetPlayback(false);
        }

        private void PauseLocalMedia()
        {
            if (_localMedia.HasMedia && _localMedia.DesiredPlaying)
                _localMedia.SetPlayback(false);
        }

        private void TrySendLocalMediaState(Network.MultiplayerSession session, bool force)
        {
            var mediaLoaded = _localMedia.HasMedia && _localMedia.MediaId != 0;
            var frequencyTenths = _multiplayer.CommunicatorFrequencyTenths;
            var mediaPlaying = mediaLoaded
                               && _localMedia.DesiredPlaying
                               && _multiplayer.CommunicatorEnabled
                               && frequencyTenths != 0;
            var mediaId = mediaLoaded ? _localMedia.MediaId : 0u;
            var volumePercent = (byte)Math.Clamp(_localMedia.VolumePercent, 0, 100);

            if (!force
                && _lastSentMediaStateValid
                && _lastSentMediaLoaded == mediaLoaded
                && _lastSentMediaPlaying == mediaPlaying
                && _lastSentMediaId == mediaId
                && _lastSentMediaFrequencyTenths == frequencyTenths
                && _lastSentMediaVolumePercent == volumePercent)
            {
                return;
            }

            if (!session.SendCommunicatorMediaState(new PacketPlayerCommunicatorMediaState
            {
                MediaId = mediaId,
                FrequencyTenths = frequencyTenths,
                MediaLoaded = mediaLoaded,
                MediaPlaying = mediaPlaying,
                VolumePercent = volumePercent
            }))
            {
                return;
            }

            _lastSentMediaStateValid = true;
            _lastSentMediaLoaded = mediaLoaded;
            _lastSentMediaPlaying = mediaPlaying;
            _lastSentMediaId = mediaId;
            _lastSentMediaFrequencyTenths = frequencyTenths;
            _lastSentMediaVolumePercent = volumePercent;
        }

        private void ResetLocalMediaTransmissionState()
        {
            _lastSentMediaStateValid = false;
            _lastSentMediaLoaded = false;
            _lastSentMediaPlaying = false;
            _lastSentMediaId = 0;
            _lastSentMediaFrequencyTenths = 0;
            _lastSentMediaVolumePercent = 100;
        }

        private void AnnounceMediaFolderError(MediaPlaylistFolderLoadError error)
        {
            switch (error)
            {
                case MediaPlaylistFolderLoadError.EmptyPath:
                    _announce(LocalizationService.Mark("No folder was selected."));
                    return;
                case MediaPlaylistFolderLoadError.InvalidPath:
                    _announce(LocalizationService.Mark("The selected folder path is invalid."));
                    return;
                case MediaPlaylistFolderLoadError.NotFound:
                    _announce(LocalizationService.Mark("The selected folder does not exist."));
                    return;
                case MediaPlaylistFolderLoadError.ReadFailed:
                    _announce(LocalizationService.Mark("Could not read files from the selected folder."));
                    return;
                case MediaPlaylistFolderLoadError.NoSupportedFiles:
                    _announce(LocalizationService.Mark("No supported audio files were found in the selected folder."));
                    return;
            }
        }
    }
}
