using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;

namespace TopSpeed.Updater.Archive
{
    internal sealed class Payload : IDisposable
    {
        private readonly string _target;
        private readonly Log _log;
        private readonly List<string> _files = new List<string>();
        public string Root { get; }
        public IReadOnlyList<string> Files => _files;

        public Payload(Options options, Log log)
        {
            _target = Path.GetFullPath(options.TargetDir);
            _log = log;
            Root = Paths.Within(_target, ".topspeed-update-" + Guid.NewGuid().ToString("N"));
        }

        public void Extract(Options options)
        {
            using (var archive = ZipFile.OpenRead(options.ZipPath))
            {
                var prefix = Paths.BundlePrefix(options, archive);
                foreach (var entry in archive.Entries)
                {
                    var relative = Paths.Relative(entry.FullName, prefix);
                    if (relative.Length == 0)
                        continue;
                    // Validate directory entries too, before deciding whether to extract them.
                    var staged = Paths.Within(Root, relative);
                    Paths.Within(_target, relative);
                    if (entry.FullName.EndsWith("/", StringComparison.Ordinal)
                        || entry.FullName.EndsWith("\\", StringComparison.Ordinal)
                        || Paths.Skip(options.SkipName, relative))
                        continue;
                    Directory.CreateDirectory(Path.GetDirectoryName(staged)!);
                    entry.ExtractToFile(staged, overwrite: false);
                    _files.Add(relative);
                }
            }
            RequireExecutable(options.GameName);
            RequireExecutable(Platform.UpdaterName);
        }

        private void RequireExecutable(string stem)
        {
            var path = Path.Combine(Root, Platform.Executable(stem));
            if (!File.Exists(path))
                throw new InvalidDataException("The update archive is missing " + Platform.Executable(stem));
            using (var file = File.OpenRead(path))
            {
                if (file.Length == 0 || (Platform.IsWindows && (file.ReadByte() != 'M' || file.ReadByte() != 'Z')))
                    throw new InvalidDataException("The update archive contains an invalid executable: " + stem);
            }
        }

        public void Dispose()
        {
            // Only this instance's private directory is owned; never scan installed user content.
            var root = Paths.Within(_target, Path.GetFileName(Root));
            if (!string.Equals(root, Root, Platform.PathComparison))
                throw new InvalidOperationException("Invalid updater staging directory.");
            try
            {
                if (Directory.Exists(root))
                    Directory.Delete(root, recursive: true);
            }
            catch (IOException error) { _log.Write("Could not remove staging directory: " + error.Message); }
            catch (UnauthorizedAccessException error) { _log.Write("Could not remove staging directory: " + error.Message); }
        }
    }
}
