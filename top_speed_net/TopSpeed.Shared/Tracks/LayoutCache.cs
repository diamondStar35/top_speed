using System;
using System.Collections.Generic;

namespace TopSpeed.Tracks.Geometry
{
    public sealed class TrackLayoutCache
    {
        private readonly Dictionary<string, CacheEntry> _cache = new Dictionary<string, CacheEntry>(StringComparer.OrdinalIgnoreCase);

        public bool TryGet(string identifier, out TrackLayoutLoadResult? result)
        {
            result = null;
            if (string.IsNullOrWhiteSpace(identifier))
                return false;

            if (_cache.TryGetValue(identifier, out var entry))
            {
                if (entry.LastModifiedUtc.HasValue && entry.Result.SourceName != null)
                {
                    return ValidateTimestamp(entry, ref result);
                }

                result = entry.Result;
                return true;
            }

            return false;
        }

        public void Store(string identifier, DateTimeOffset? lastModifiedUtc, TrackLayoutLoadResult result)
        {
            if (string.IsNullOrWhiteSpace(identifier))
                return;
            if (result == null)
                return;

            _cache[identifier] = new CacheEntry(result, lastModifiedUtc);
        }

        public void Clear()
        {
            _cache.Clear();
        }

        private static bool ValidateTimestamp(CacheEntry entry, ref TrackLayoutLoadResult? result)
        {
            if (entry.LastModifiedUtc == null)
            {
                result = entry.Result;
                return true;
            }

            if (entry.Result.SourceName == null)
                return false;

            try
            {
                var info = new System.IO.FileInfo(entry.Result.SourceName);
                if (!info.Exists)
                    return false;
                var current = info.LastWriteTimeUtc;
                if (current <= entry.LastModifiedUtc.Value.UtcDateTime)
                {
                    result = entry.Result;
                    return true;
                }
            }
            catch
            {
                return false;
            }

            return false;
        }

        private sealed class CacheEntry
        {
            public TrackLayoutLoadResult Result { get; }
            public DateTimeOffset? LastModifiedUtc { get; }

            public CacheEntry(TrackLayoutLoadResult result, DateTimeOffset? lastModifiedUtc)
            {
                Result = result;
                LastModifiedUtc = lastModifiedUtc;
            }
        }
    }
}
