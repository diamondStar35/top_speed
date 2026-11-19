using System;
using System.Collections.Generic;
using System.Globalization;
using TopSpeed.Localization;
using TopSpeed.Menu;

namespace TopSpeed.Core.Multiplayer
{
    internal sealed partial class MultiplayerCoordinator
    {
        private void ShowTrackUploadProgressDialog()
        {
            var upload = _pendingTrackUpload;
            if (upload == null)
                return;

            var total = upload.Bytes.Length;
            var uploaded = Math.Max(0, Math.Min(upload.Offset, total));
            var percent = total == 0 ? 0 : (int)Math.Round((double)uploaded * 100d / total, MidpointRounding.AwayFromZero);
            if (percent < 0)
                percent = 0;
            if (percent > 100)
                percent = 100;

            var items = new List<DialogItem>
            {
                new DialogItem(LocalizationService.Format(LocalizationService.Mark("Track: {0}"), upload.DisplayName)),
                new DialogItem(LocalizationService.Format(LocalizationService.Mark("File size: {0}"), FormatTransferBytes(total))),
                new DialogItem(LocalizationService.Format(LocalizationService.Mark("Uploaded size: {0}"), FormatTransferBytes(uploaded))),
                new DialogItem(LocalizationService.Format(LocalizationService.Mark("Percentage: {0}%"), percent))
            };

            var dialog = new Dialog(
                LocalizationService.Mark("Uploading track..."),
                null,
                QuestionId.Close,
                items,
                onResult: _ => RequestTrackUploadCancel(),
                new DialogButton(QuestionId.Close, LocalizationService.Mark("Cancel")));
            _dialogs.Show(dialog);
        }

        private static string FormatTransferBytes(long bytes)
        {
            if (bytes < 0)
                bytes = 0;

            var units = new[]
            {
                LocalizationService.Mark("B"),
                LocalizationService.Mark("KB"),
                LocalizationService.Mark("MB"),
                LocalizationService.Mark("GB")
            };
            var index = 0;
            var value = (double)bytes;
            while (value >= 1024d && index < units.Length - 1)
            {
                value /= 1024d;
                index++;
            }

            return value.ToString("0.##", CultureInfo.InvariantCulture) + " " + LocalizationService.Translate(units[index]);
        }
    }
}
