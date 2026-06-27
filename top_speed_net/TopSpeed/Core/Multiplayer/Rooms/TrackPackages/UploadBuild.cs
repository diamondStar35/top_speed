using System;
using TopSpeed.Localization;
using TopSpeed.Protocol;

namespace TopSpeed.Core.Multiplayer
{
    internal sealed partial class MultiplayerCoordinator
    {
        private bool TryBuildTrackUpload(string trackFile, string displayName, out PendingTrackUpload pending, out string error)
        {
            pending = null!;
            error = string.Empty;

            if (string.IsNullOrWhiteSpace(trackFile))
            {
                error = LocalizationService.Mark("Custom track file path is empty.");
                return false;
            }

            var fallbackLaps = _state.RoomDrafts.RoomOptionsLaps;
            if (!PackageBuild.TryBuildPackageFromTrackFile(trackFile, displayName, fallbackLaps, out var payload, out var bytes, out error))
                return false;

            var hash = TrackPackageRef.NormalizeHash(payload.Manifest.Hash);
            if (!string.IsNullOrWhiteSpace(hash))
            {
                // Remember this custom track's pit-area status (from its parsed [meta]) so the
                // host's no-pit-area start-race warning can resolve it from the room's hash-only ref.
                _customTrackHasPitAreaByHash[hash] =
                    TrackPackageCodec.ToTrackData(payload, userDefined: true, sourcePath: string.Empty).HasPitArea;
            }

            var trackId = payload.Manifest.TrackId ?? string.Empty;
            var version = payload.Manifest.Version ?? string.Empty;
            var resolvedDisplayName = (displayName ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(resolvedDisplayName)
                && payload.Metadata != null
                && payload.Metadata.TryGetValue("name", out var metadataName))
            {
                resolvedDisplayName = (metadataName ?? string.Empty).Trim();
            }

            if (string.IsNullOrWhiteSpace(resolvedDisplayName))
                resolvedDisplayName = payload.Manifest.TrackId ?? string.Empty;

            pending = new PendingTrackUpload
            {
                UploadId = NextTrackUploadId(),
                Track = TrackPackageRef.Custom(trackId, version, hash),
                DisplayName = resolvedDisplayName,
                Bytes = bytes,
                Offset = 0,
                NextChunkIndex = 0
            };
            return true;
        }
    }
}

