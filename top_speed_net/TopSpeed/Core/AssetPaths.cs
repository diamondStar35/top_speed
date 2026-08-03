using System;
using System.Collections.Generic;
using System.IO;
using TopSpeed.Runtime;

namespace TopSpeed.Core
{
    internal static class AssetPaths
    {
        private static string? _root;
        private static string? _userContentRoot;

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

        // Where content the player acquired lives (vehicles and tracks kept from a server), as
        // opposed to what shipped with the game. On desktop this is the same folder as Root, so
        // nothing changes there. On platforms that unpack their assets at startup it is a separate
        // folder the unpack step never deletes, so an app update cannot take the player's content
        // with it when it refreshes the shipped files.
        public static string UserContentRoot => _userContentRoot ?? Root;

        // Every root that can hold vehicles or tracks, shipped first. Collapses to a single entry
        // when the two roots are the same folder, so desktop scans once and cannot list an entry
        // twice.
        public static IReadOnlyList<string> ContentRoots => BuildContentRoots(Root, UserContentRoot);

        internal static IReadOnlyList<string> BuildContentRoots(string shipped, string user)
        {
            return IsSameFolder(shipped, user)
                ? new[] { shipped }
                : new[] { shipped, user };
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

        internal static void SetUserContentRoot(string? rootPath)
        {
            if (rootPath is null)
                return;

            var trimmedPath = rootPath.Trim();
            if (trimmedPath.Length == 0)
                return;

            _userContentRoot = trimmedPath;
        }

        private static bool IsSameFolder(string left, string right)
        {
            if (string.Equals(left, right, StringComparison.Ordinal))
                return true;

            try
            {
                var leftFull = Path.GetFullPath(left).TrimEnd(Path.DirectorySeparatorChar);
                var rightFull = Path.GetFullPath(right).TrimEnd(Path.DirectorySeparatorChar);
                return string.Equals(leftFull, rightFull, StringComparison.OrdinalIgnoreCase);
            }
            catch (ArgumentException)
            {
                return false;
            }
            catch (NotSupportedException)
            {
                return false;
            }
            catch (PathTooLongException)
            {
                return false;
            }
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

        // Resolves a non-spoken race cue (e.g. curve announcement tones) under Sounds/racecues.
        // These are language independent, so unlike copilot speech there is no language segment.
        public static string? ResolveRaceCueSoundPath(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
                return null;

            var relative = key.Replace('\\', Path.DirectorySeparatorChar).Replace('/', Path.DirectorySeparatorChar);
            if (string.IsNullOrWhiteSpace(Path.GetExtension(relative)))
                relative += ".ogg";
            return ResolveExistingPath("Sounds", "racecues", relative);
        }
    }
}
