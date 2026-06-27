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

            return items
                .OrderBy(GetDisplay, StringComparer.OrdinalIgnoreCase)
                .ToList();
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
