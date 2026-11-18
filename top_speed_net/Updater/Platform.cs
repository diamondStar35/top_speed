using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

namespace TopSpeed.Updater
{
    internal static class Platform
    {
        public const string UpdaterName = "TopSpeedUpdater";
        public const string LegacyName = "Updater";
        public static bool IsWindows => RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
        public static StringComparison PathComparison => IsWindows ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
        public static string Executable(string stem) => IsWindows ? stem + ".exe" : stem;

        public static string CurrentExecutable()
        {
            using (var process = Process.GetCurrentProcess())
            {
                return process.MainModule?.FileName
                    ?? throw new InvalidOperationException("The updater executable path is unavailable.");
            }
        }

        public static string ResolveExecutable(string root, string stem)
        {
            var name = Executable(stem);
            var direct = Path.Combine(root, name);
            if (File.Exists(direct))
                return direct;
            var matches = Directory.GetFiles(root, name, SearchOption.AllDirectories);
            Array.Sort(matches, (left, right) =>
            {
                var depth = left.Split(Path.DirectorySeparatorChar).Length.CompareTo(right.Split(Path.DirectorySeparatorChar).Length);
                return depth != 0 ? depth : string.Compare(left, right, PathComparison);
            });
            return matches.Length > 0 ? matches[0] : direct;
        }
    }
}
