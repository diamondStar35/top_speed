using System;
using System.IO;
using System.IO.Compression;

namespace TopSpeed.Updater.Archive
{
    internal static class Paths
    {
        public static string Within(string root, string relative)
        {
            var prefix = Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                + Path.DirectorySeparatorChar;
            var destination = Path.GetFullPath(Path.Combine(prefix, relative));
            if (Path.IsPathRooted(relative) || !destination.StartsWith(prefix, Platform.PathComparison))
                throw new InvalidDataException("Unsafe archive path: " + relative);
            return destination;
        }

        public static string BundlePrefix(Options options, ZipArchive archive)
        {
            var target = options.TargetDir.Replace('\\', '/').TrimEnd('/');
            if (!target.EndsWith("/Contents/MacOS", StringComparison.OrdinalIgnoreCase))
                return string.Empty;
            var prefix = options.GameName + ".app/Contents/MacOS/";
            foreach (var entry in archive.Entries)
            {
                if (entry.FullName.Replace('\\', '/').StartsWith(prefix, StringComparison.Ordinal))
                    return prefix;
            }
            return string.Empty;
        }

        public static string Relative(string entry, string bundlePrefix)
        {
            var normalized = entry.Replace('\\', '/');
            if (bundlePrefix.Length > 0)
            {
                if (!normalized.StartsWith(bundlePrefix, StringComparison.Ordinal))
                    return string.Empty;
                normalized = normalized.Substring(bundlePrefix.Length);
            }
            return normalized.Replace('/', Path.DirectorySeparatorChar);
        }

        public static bool Skip(string stem, string entry)
        {
            var name = Path.GetFileName(entry);
            return stem.Length > 0 && (string.Equals(name, Platform.Executable(stem), StringComparison.OrdinalIgnoreCase)
                || string.Equals(Path.GetFileNameWithoutExtension(name), stem, StringComparison.OrdinalIgnoreCase));
        }
    }
}
