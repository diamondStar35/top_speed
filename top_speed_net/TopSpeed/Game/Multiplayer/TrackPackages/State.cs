using System;
using System.Collections.Generic;
using TopSpeed.Localization;
using TopSpeed.Protocol;

namespace TopSpeed.Game
{
    internal sealed partial class Game
    {
        private readonly Dictionary<string, TrackPackagePayload> _multiplayerTrackPackageCache = new Dictionary<string, TrackPackagePayload>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, IncomingTrackPackageTransfer> _multiplayerTrackPackageTransfers = new Dictionary<string, IncomingTrackPackageTransfer>(StringComparer.OrdinalIgnoreCase);
        private string _multiplayerTrackDownloadHash = string.Empty;
        private bool _multiplayerTrackDownloadProgressOpen;
        private int _multiplayerCurrentRoomLaps = 3;

        private sealed class IncomingTrackPackageTransfer
        {
            public string TrackId = string.Empty;
            public string Version = string.Empty;
            public string Hash = string.Empty;
            public string DisplayName = string.Empty;
            public byte[] Bytes = Array.Empty<byte>();
            public int Offset;
            public ushort NextChunkIndex;
        }

        private void ResetPendingTrackPackageTransfers()
        {
            _multiplayerTrackPackageTransfers.Clear();
            CloseTrackDownloadProgressDialog();
        }

        private void SetMultiplayerRoomLaps(int laps)
        {
            if (laps > 0)
                _multiplayerCurrentRoomLaps = laps;
        }

        private void ResetMultiplayerTrackPackageState()
        {
            _multiplayerCurrentRoomLaps = 3;
            ResetPendingTrackPackageTransfers();
        }

        private void HandleTrackPackageUploadResult(PacketTrackPackageUploadResult packet)
        {
            if (packet == null || string.IsNullOrWhiteSpace(packet.Message))
                return;

            _speech.Speak(packet.Message);
        }

        private static string ResolveTrackPackageDisplayName(string? trackId, string? version)
        {
            var id = (trackId ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(id))
                id = LocalizationService.Mark("Custom track");

            var ver = (version ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(ver))
                return id;

            return id + " (" + ver + ")";
        }
    }
}
