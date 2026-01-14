using System;
using System.Collections.Generic;
using System.IO;

namespace TopSpeed.Tracks.Geometry
{
    public sealed class TrackLayoutSourceEntry : IDisposable
    {
        public string Identifier { get; }
        public string? SourceName { get; }
        public Stream Stream { get; }
        public DateTimeOffset? LastModifiedUtc { get; }

        public TrackLayoutSourceEntry(string identifier, Stream stream, string? sourceName = null, DateTimeOffset? lastModifiedUtc = null)
        {
            Identifier = identifier;
            Stream = stream ?? throw new ArgumentNullException(nameof(stream));
            SourceName = sourceName;
            LastModifiedUtc = lastModifiedUtc;
        }

        public void Dispose()
        {
            Stream.Dispose();
        }
    }

    public interface ITrackLayoutSource
    {
        bool TryOpen(string identifier, out TrackLayoutSourceEntry entry);
    }

    public sealed class FileTrackLayoutSource : ITrackLayoutSource
    {
        private readonly string[] _roots;
        private readonly string _extension;

        public FileTrackLayoutSource(IEnumerable<string> roots, string extension = TrackLayoutFormat.FileExtension)
        {
            if (roots == null)
                throw new ArgumentNullException(nameof(roots));
            _roots = NormalizeRoots(roots);
            _extension = string.IsNullOrWhiteSpace(extension) ? TrackLayoutFormat.FileExtension : extension;
        }

        public bool TryOpen(string identifier, out TrackLayoutSourceEntry entry)
        {
            entry = null!;

            if (string.IsNullOrWhiteSpace(identifier))
                return false;

            var candidate = ResolvePath(identifier);
            if (candidate == null)
                return false;

            var stream = File.Open(candidate, FileMode.Open, FileAccess.Read, FileShare.Read);
            var info = new FileInfo(candidate);
            entry = new TrackLayoutSourceEntry(identifier, stream, candidate, info.LastWriteTimeUtc);
            return true;
        }

        private string? ResolvePath(string identifier)
        {
            if (File.Exists(identifier))
                return Path.GetFullPath(identifier);

            var name = identifier;
            if (!name.EndsWith(_extension, StringComparison.OrdinalIgnoreCase))
                name += _extension;

            foreach (var root in _roots)
            {
                if (string.IsNullOrWhiteSpace(root))
                    continue;
                var candidate = Path.Combine(root, name);
                if (File.Exists(candidate))
                    return Path.GetFullPath(candidate);
            }

            return null;
        }

        private static string[] NormalizeRoots(IEnumerable<string> roots)
        {
            var list = new List<string>();
            foreach (var root in roots)
            {
                if (string.IsNullOrWhiteSpace(root))
                    continue;
                list.Add(root.Trim());
            }
            return list.ToArray();
        }
    }
}
