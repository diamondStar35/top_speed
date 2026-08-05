using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using TopSpeed.Localization;

namespace TopSpeed.Menu
{
    internal static class DialogAnnouncement
    {
        private static readonly string TitleTemplate = LocalizationService.Mark("{0}  dialog");
        private static readonly string TitleCaptionTemplate = LocalizationService.Mark("{0}  dialog  {1}");

        // Reads the dialog's body lines out as part of the opening announcement. Without this the lines
        // are only reachable by arrowing through the dialog, so all you hear on open is the button.
        public static string Compose(string? title, string? caption, IReadOnlyList<DialogItem>? items)
        {
            var announcement = Compose(title, caption);
            if (items == null || items.Count == 0)
                return announcement;

            var builder = new StringBuilder(announcement);
            for (var i = 0; i < items.Count; i++)
            {
                var line = LocalizationService.Translate(items[i]?.Text);
                if (string.IsNullOrWhiteSpace(line))
                    continue;

                builder.Append("  ").Append(line);
            }

            return builder.ToString();
        }

        public static string Compose(string? title, string? caption)
        {
            var titleText = LocalizationService.Translate(title);
            var captionText = LocalizationService.Translate(caption);
            var hasCaption = !string.IsNullOrWhiteSpace(captionText);
            var announcement = hasCaption
                ? LocalizationService.Format(TitleCaptionTemplate, titleText, captionText)
                : LocalizationService.Format(TitleTemplate, titleText);

            if (!NeedsFallback(announcement, titleText, captionText, hasCaption))
                return announcement;

            return hasCaption
                ? string.Format(CultureInfo.CurrentCulture, TitleCaptionTemplate, titleText, captionText)
                : string.Format(CultureInfo.CurrentCulture, TitleTemplate, titleText);
        }

        private static bool NeedsFallback(string announcement, string title, string caption, bool hasCaption)
        {
            if (string.IsNullOrWhiteSpace(announcement))
                return true;

            if (string.Equals(announcement, title, StringComparison.Ordinal))
                return true;

            if (announcement.IndexOf(title, StringComparison.Ordinal) < 0)
                return true;

            if (hasCaption && announcement.IndexOf(caption, StringComparison.Ordinal) < 0)
                return true;

            return false;
        }
    }
}

