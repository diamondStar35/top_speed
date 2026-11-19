using System;
using System.Collections.Generic;
using TopSpeed.Localization;
using TopSpeed.Menu;
using TopSpeed.Protocol;

namespace TopSpeed.Game
{
    internal sealed partial class Game
    {
        private void ShowTrackDownloadProgressDialog(string hash, IncomingTrackPackageTransfer transfer)
        {
            if (transfer == null)
                return;

            _multiplayerTrackDownloadHash = TrackPackageRef.NormalizeHash(hash);
            _multiplayerTrackDownloadProgressOpen = true;
            UpdateTrackDownloadProgressDialog(hash, transfer);
        }

        private void UpdateTrackDownloadProgressDialog(string hash, IncomingTrackPackageTransfer transfer)
        {
            if (transfer == null || !_multiplayerTrackDownloadProgressOpen)
                return;

            var normalizedHash = TrackPackageRef.NormalizeHash(hash);
            if (!string.Equals(_multiplayerTrackDownloadHash, normalizedHash, StringComparison.OrdinalIgnoreCase))
                return;

            var total = transfer.Bytes.Length;
            var downloaded = Math.Max(0, Math.Min(transfer.Offset, total));
            var percent = total == 0 ? 0 : (int)Math.Round((double)downloaded * 100d / total, MidpointRounding.AwayFromZero);
            if (percent < 0)
                percent = 0;
            if (percent > 100)
                percent = 100;

            var items = new List<DialogItem>
            {
                new DialogItem(LocalizationService.Format(LocalizationService.Mark("Track: {0}"), transfer.DisplayName)),
                new DialogItem(LocalizationService.Format(LocalizationService.Mark("File size: {0}"), FormatBytes(total))),
                new DialogItem(LocalizationService.Format(LocalizationService.Mark("Downloaded size: {0}"), FormatBytes(downloaded))),
                new DialogItem(LocalizationService.Format(LocalizationService.Mark("Percentage: {0}%"), percent))
            };

            var dialog = new Dialog(LocalizationService.Mark("Downloading track..."),
                null,
                QuestionId.Close,
                items,
                null)
            {
                OpenAsOverlay = true,
                IsCancelable = false
            };
            _dialogs.Show(dialog);
        }

        private void CloseTrackDownloadProgressDialog()
        {
            if (!_multiplayerTrackDownloadProgressOpen)
                return;

            _multiplayerTrackDownloadProgressOpen = false;
            _multiplayerTrackDownloadHash = string.Empty;
            _dialogs.CloseActive();
        }

        private void CloseTrackDownloadProgressDialog(string hash)
        {
            if (!_multiplayerTrackDownloadProgressOpen)
                return;

            var normalizedHash = TrackPackageRef.NormalizeHash(hash);
            if (!string.IsNullOrWhiteSpace(normalizedHash)
                && !string.Equals(_multiplayerTrackDownloadHash, normalizedHash, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            CloseTrackDownloadProgressDialog();
        }
    }
}
