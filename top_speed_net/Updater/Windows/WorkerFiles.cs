using System;
using System.IO;

namespace TopSpeed.Updater.Windows
{
    internal static class WorkerFiles
    {
        private const string DirectoryPrefix = ".topspeed-worker-";
        private static readonly string[] Names =
        {
            Platform.UpdaterName + ".exe", Platform.UpdaterName + ".exe.config",
            Platform.UpdaterName + ".dll", Platform.UpdaterName + ".deps.json",
            Platform.UpdaterName + ".runtimeconfig.json"
        };

        public static string Create(string target)
        {
            var directory = Archive.Paths.Within(target, DirectoryPrefix + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            return directory;
        }

        public static void ValidateDirectory(string target, string directory)
        {
            var name = Path.GetFileName(directory);
            if (!name.StartsWith(DirectoryPrefix, StringComparison.Ordinal)
                || !Guid.TryParseExact(name.Substring(DirectoryPrefix.Length), "N", out _)
                || !string.Equals(Archive.Paths.Within(target, name), Path.GetFullPath(directory), Platform.PathComparison))
                throw new InvalidOperationException("Invalid updater worker directory.");
        }

        public static void Copy(string source, string destination)
        {
            foreach (var name in Names)
            {
                var path = Path.Combine(source, name);
                if (File.Exists(path))
                    File.Copy(path, Path.Combine(destination, name), overwrite: true);
            }
        }

        public static void Delete(string target, string directory)
        {
            ValidateDirectory(target, directory);
            foreach (var name in Names)
                File.Delete(Path.Combine(directory, name));
            // No recursive deletion: anything this worker did not create must be left alone.
            Directory.Delete(directory);
        }
    }
}
