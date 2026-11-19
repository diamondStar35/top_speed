using System;
using System.Collections.Generic;
using TopSpeed.Core;
using TopSpeed.Protocol;

namespace TopSpeed.Core.Multiplayer
{
    internal sealed partial class MultiplayerCoordinator
    {
        private readonly TrackSource _roomTrackUploadSource = new TrackSource();

        // Host-side memo of whether a custom track (keyed by normalized package hash) has a pit area.
        // Learned when the host builds the upload package, which parses the track's [meta] section.
        // The room's TrackPackageRef carries no metadata, so the no-pit-area start-race warning
        // resolves a custom track's pit status through this map.
        private readonly Dictionary<string, bool> _customTrackHasPitAreaByHash =
            new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
        private PendingTrackUpload? _pendingTrackUpload;
        private uint _nextTrackUploadId = 1;
        private bool _trackUploadProgressOpen;

        private sealed class PendingTrackUpload
        {
            public uint UploadId;
            public TrackPackageRef Track = TrackPackageRef.Custom(string.Empty, string.Empty, string.Empty);
            public string DisplayName = string.Empty;
            public byte[] Bytes = Array.Empty<byte>();
            public int Offset;
            public ushort NextChunkIndex;
            public bool BeginSent;
            public bool EndSent;
            public bool WaitingForResult;
            public bool CancelRequested;
        }

        private void ResetTrackUploadState()
        {
            _pendingTrackUpload = null;
            _trackUploadProgressOpen = false;
            _nextTrackUploadId = 1;
            _state.RoomDrafts.RoomTrackUploadReturnToCatalog = false;
        }

        private uint NextTrackUploadId()
        {
            var id = _nextTrackUploadId++;
            if (id == 0)
            {
                id = _nextTrackUploadId++;
            }

            if (_nextTrackUploadId == 0)
                _nextTrackUploadId = 1;
            return id;
        }
    }
}
