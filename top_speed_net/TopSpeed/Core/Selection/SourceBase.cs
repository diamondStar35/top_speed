using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TopSpeed.Localization;

namespace TopSpeed.Core
{
    internal abstract class SourceBase<TInfo>
    {
        private readonly Dictionary<string, (DateTime LastWriteUtc, TInfo Value)> _cache =
            new Dictionary<string, (DateTime LastWriteUtc, TInfo Value)>(StringComparer.OrdinalIgnoreCase);
        // Issues are kept per file so they survive cache hits. The cache (above) skips re-parsing an
        // unchanged file, so issues collected only at parse time would otherwise be lost on every
        // list rebuild after the first; keeping them here lets ConsumeIssues report them each time.
        private readonly Dictionary<string, List<string>> _fileIssues =
            new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        private List<string>? _activeFileIssues;
        private readonly string _rootFolder;
        private readonly string _pattern;

        protected SourceBase(string rootFolder, string pattern)
        {
            _rootFolder = rootFolder;
            _pattern = pattern;
        }

        public IEnumerable<string> GetFiles()
        {
            return GetInfo().Select(GetKey);
        }

        public IReadOnlyList<TInfo> GetInfo()
        {
            var files = Scan.Find(_rootFolder, _pattern);
            if (files.Count == 0)
            {
                _cache.Clear();
                _fileIssues.Clear();
                return Array.Empty<TInfo>();
            }

            var items = new List<TInfo>(files.Count);
            var known = new HashSet<string>(files, StringComparer.OrdinalIgnoreCase);
            for (var i = 0; i < files.Count; i++)
            {
                var file = files[i];
                if (Scan.TryCached(file, _cache, Parse, out var info))
                    items.Add(info);
            }

            Scan.Prune(_cache, known);
            var staleIssues = _fileIssues.Keys.Where(key => !known.Contains(key)).ToList();
            for (var i = 0; i < staleIssues.Count; i++)
                _fileIssues.Remove(staleIssues[i]);

            return Disambiguate(items)
                .OrderBy(GetDisplay, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        // Entries are named by the metadata inside the file, so two files can present the same name
        // ("Chevy Laguna" twice, or several unnamed vehicles all reading "Custom vehicle") with
        // nothing to tell them apart when spoken. Where a name is shared, append each entry's folder
        // to ALL of them: appending to only the "copies" would need a guess about which one is the
        // original, and every member needs the hint for the list to be readable anyway. The scan
        // takes at most one file per folder, so the folder path is unique per entry and this can
        // never leave two entries reading the same.
        private List<TInfo> Disambiguate(List<TInfo> items)
        {
            if (items.Count < 2)
                return items;

            var counts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            for (var i = 0; i < items.Count; i++)
            {
                var display = GetDisplay(items[i]) ?? string.Empty;
                counts.TryGetValue(display, out var seen);
                counts[display] = seen + 1;
            }

            var result = new List<TInfo>(items.Count);
            for (var i = 0; i < items.Count; i++)
            {
                var item = items[i];
                var display = GetDisplay(item) ?? string.Empty;
                if (!counts.TryGetValue(display, out var seen) || seen < 2)
                {
                    result.Add(item);
                    continue;
                }

                var folder = ResolveRelativeFolder(GetKey(item));
                result.Add(string.IsNullOrWhiteSpace(folder)
                    ? item
                    : WithDisplay(item, LocalizationService.Format(
                        LocalizationService.Mark("{0} ({1})"),
                        display,
                        folder)));
            }

            return result;
        }

        // The entry's folder path beneath the source root ("NASCAR/cup car dodge"), which is what
        // distinguishes same-named entries. Falls back to the leaf folder if the file somehow sits
        // outside the root.
        private string ResolveRelativeFolder(string file)
        {
            if (string.IsNullOrWhiteSpace(file))
                return string.Empty;

            var directory = Path.GetDirectoryName(Path.GetFullPath(file)) ?? string.Empty;
            if (directory.Length == 0)
                return string.Empty;

            var root = Path.GetFullPath(Path.Combine(AssetPaths.Root, _rootFolder));
            var prefix = root.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
            if (!directory.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                return Path.GetFileName(directory) ?? string.Empty;

            return directory
                .Substring(prefix.Length)
                .Replace(Path.DirectorySeparatorChar, '/')
                .Replace('\\', '/');
        }

        public IReadOnlyList<string> ConsumeIssues()
        {
            if (_fileIssues.Count == 0)
                return Array.Empty<string>();

            var all = new List<string>();
            foreach (var pair in _fileIssues)
                all.AddRange(pair.Value);
            return all;
        }

        protected abstract string GetKey(TInfo info);
        protected abstract string GetDisplay(TInfo info);
        // Returns a copy of the entry carrying a new display name. The info types are readonly
        // structs, so disambiguation needs each source to rebuild its own.
        protected abstract TInfo WithDisplay(TInfo info, string display);
        protected abstract (bool Success, TInfo Value) ParseCore(string file);

        protected void AddFileIssue(string file)
        {
            AddIssue(LocalizationService.Format(
                LocalizationService.Mark("File: {0}"),
                Path.GetFileName(file)));
        }

        protected void AddIssue(string message)
        {
            if (string.IsNullOrWhiteSpace(message) || _activeFileIssues == null)
                return;
            _activeFileIssues.Add(message);
        }

        private (bool Success, TInfo Value) Parse(string file)
        {
            var fileIssues = new List<string>();
            _activeFileIssues = fileIssues;
            try
            {
                return ParseCore(file);
            }
            catch (IOException ex)
            {
                AddFileIssue(file);
                AddIssue(ex.Message);
                return (false, default!);
            }
            catch (UnauthorizedAccessException ex)
            {
                AddFileIssue(file);
                AddIssue(ex.Message);
                return (false, default!);
            }
            catch (InvalidDataException ex)
            {
                AddFileIssue(file);
                AddIssue(ex.Message);
                return (false, default!);
            }
            catch (FormatException ex)
            {
                AddFileIssue(file);
                AddIssue(ex.Message);
                return (false, default!);
            }
            catch (ArgumentException ex)
            {
                AddFileIssue(file);
                AddIssue(ex.Message);
                return (false, default!);
            }
            finally
            {
                _activeFileIssues = null;
                if (fileIssues.Count > 0)
                    _fileIssues[file] = fileIssues;
                else
                    _fileIssues.Remove(file);
            }
        }
    }
}
