using System;
using System.IO;
using TopSpeed.Runtime;

namespace TopSpeed.Core
{
    internal static class AssetPaths
    {
        private static string? _root;

        public static string Root
        {
            get
            {
                if (_root != null)
                    return _root;

                var baseDir = AppContext.BaseDirectory;
                _root = baseDir;
                return _root!;
            }
        }

        internal static void SetRoot(string? rootPath)
        {
            if (rootPath is null)
                return;

            var trimmedPath = rootPath.Trim();
            if (trimmedPath.Length == 0)
                return;

            _root = trimmedPath;
        }

        public static string SoundsRoot => Path.Combine(Root, "Sounds");

        public static string? ResolveExistingPath(params string[] segments)
        {
            return RuntimeAssetPathResolver.ResolveExistingPath(Root, segments);
        }

        public static string? ResolveLanguageSoundPath(string language, string key)
        {
            if (string.IsNullOrWhiteSpace(language) || string.IsNullOrWhiteSpace(key))
                return null;

            var relative = key.Replace('\\', Path.DirectorySeparatorChar).Replace('/', Path.DirectorySeparatorChar);
            if (string.IsNullOrWhiteSpace(Path.GetExtension(relative)))
                relative += ".ogg";
            return ResolveExistingPath("Sounds", language, relative);
        }

        public static string? ResolveLanguageSoundPathWithFallback(string language, string key, string fallbackLanguage = "en")
        {
            var path = ResolveLanguageSoundPath(language, key);
            if (path != null)
                return path;

            if (string.IsNullOrWhiteSpace(fallbackLanguage))
                return null;
            if (string.Equals(language, fallbackLanguage, StringComparison.OrdinalIgnoreCase))
                return null;

            return ResolveLanguageSoundPath(fallbackLanguage, key);
        }

        public static string? ResolveLegacySoundPath(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
                return null;

            return ResolveExistingPath("Sounds", "Legacy", fileName);
        }

        public static string? ResolvePitSoundPath(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
                return null;

            return ResolveExistingPath("Sounds", "pit", fileName);
        }
    }
}
