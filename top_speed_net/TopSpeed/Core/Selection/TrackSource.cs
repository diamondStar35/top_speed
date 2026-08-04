using System;
using System.Collections.Generic;
using System.IO;
using TopSpeed.Data;
using TopSpeed.Localization;

namespace TopSpeed.Core
{
    internal sealed class TrackSource : SourceBase<TrackInfo>
    {
        public TrackSource()
            : base("Tracks", "*.tsm")
        {
        }

        protected override string GetKey(TrackInfo info)
        {
            return info.Key;
        }

        protected override string GetDisplay(TrackInfo info)
        {
            return info.Display;
        }

        protected override TrackInfo WithDisplay(TrackInfo info, string display)
        {
            return new TrackInfo(info.Key, display);
        }

        protected override (bool Success, TrackInfo Value) ParseCore(string file)
        {
            if (!TrackTsmParser.TryLoadFromFile(file, out var parsed, out var issues))
            {
                AppendIssues(file, issues);
                return (false, default);
            }

            // The track loaded, but it may still have non-fatal warnings (e.g. pit_area = false
            // alongside defined pit segments). Surface those the same way custom vehicles do.
            if (issues != null && issues.Count > 0)
                AppendIssues(file, issues);

            var display = string.IsNullOrWhiteSpace(parsed.Name)
                ? ResolveFolderName(file)
                : parsed.Name!;
            if (string.IsNullOrWhiteSpace(display))
                display = LocalizationService.Mark("Custom track");

            return (true, new TrackInfo(file, display));
        }

        private void AppendIssues(string file, IReadOnlyList<TrackTsmIssue> issues)
        {
            AddFileIssue(file);

            if (issues == null || issues.Count == 0)
            {
                AddIssue(LocalizationService.Mark("Failed to load this track file."));
                return;
            }

            // TrackTsmIssue.ToString() already prefixes the severity (e.g. "Warning (line 12): ...").
            for (var i = 0; i < issues.Count; i++)
                AddIssue(issues[i].ToString());
        }

        private static string ResolveFolderName(string file)
        {
            var directory = Path.GetDirectoryName(file);
            if (string.IsNullOrWhiteSpace(directory))
                return Path.GetFileNameWithoutExtension(file);
            var name = Path.GetFileName(directory);
            return string.IsNullOrWhiteSpace(name) ? Path.GetFileNameWithoutExtension(file) : name;
        }
    }
}

