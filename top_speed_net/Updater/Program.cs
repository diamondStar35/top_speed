using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Runtime.InteropServices;
using System.Threading;

namespace TopSpeed.Updater
{
    internal static class Program
    {
        private const int ExtractRetryCount = 24;
        private const int ExtractRetryDelayMs = 250;

        /// <summary>
        /// What a file is called while it is being written, beside the one it will replace. The
        /// replacement is a rename, never a rewrite of the existing file: a program that has been
        /// run and is then rewritten in place is refused by macOS however valid its new contents,
        /// and a file rewritten under a process still mapping it kills that process on Linux.
        /// A rename gives the folder a new file and leaves the old one to whoever still holds it.
        /// </summary>
        private const string PendingSuffix = ".new";

        /// <summary>
        /// Where a file goes when it cannot be deleted to make room, which on Windows is a
        /// program that is running: this one, replacing itself. Renaming a running program is
        /// allowed there where deleting it is not. Swept up on the next run.
        /// </summary>
        private const string SupersededSuffix = ".superseded";

        /// <summary>The name this program shipped under before it could replace itself.</summary>
        private const string LegacyUpdaterStem = "Updater";

        private static int Main(string[] args)
        {
            var safeArgs = args ?? Array.Empty<string>();
            var enableLog = HasLogFlag(safeArgs);
            var logPath = enableLog ? ResolveLogPath(safeArgs) : string.Empty;
            Log(enableLog, logPath, "Updater entry.");
            Log(enableLog, logPath, "Args: " + string.Join(" ", safeArgs));
            try
            {
                var options = ParseArgs(safeArgs);
                enableLog = options.EnableLog;
                logPath = enableLog ? Path.Combine(Path.GetFullPath(options.TargetDir), "updater.log") : string.Empty;
                Log(enableLog, logPath, $"Parsed args. pid={options.ProcessId}, zip={options.ZipPath}, dir={options.TargetDir}, game={options.GameExeName}, skip={options.SkipFileName}");
                WaitForProcessExit(options.ProcessId);
                Log(enableLog, logPath, "Waited for game process exit.");
                InstallZip(options, enableLog, logPath);
                Log(enableLog, logPath, "Zip install complete.");
                StartGame(options, enableLog, logPath);
                Log(enableLog, logPath, "Game restart requested successfully.");
                return 0;
            }
            catch (Exception ex)
            {
                Log(enableLog, logPath, "Updater failed: " + ex);
                Console.Error.WriteLine(ex.Message);
                return 1;
            }
        }

        private static UpdaterOptions ParseArgs(string[] args)
        {
            var options = new UpdaterOptions();
            for (var i = 0; i < args.Length; i++)
            {
                var key = args[i] ?? string.Empty;
                if (string.Equals(key, "--log", StringComparison.OrdinalIgnoreCase))
                {
                    options.EnableLog = true;
                    continue;
                }

                var value = i + 1 < args.Length ? (args[i + 1] ?? string.Empty) : string.Empty;
                if (string.IsNullOrWhiteSpace(value))
                    continue;

                switch (key)
                {
                    case "--pid":
                        if (int.TryParse(value, out var pid))
                        {
                            options.ProcessId = pid;
                            i++;
                        }
                        break;
                    case "--zip":
                        options.ZipPath = value;
                        i++;
                        break;
                    case "--dir":
                        options.TargetDir = value;
                        i++;
                        break;
                    case "--game":
                        options.GameExeName = value;
                        i++;
                        break;
                    case "--skip":
                        options.SkipFileName = value;
                        i++;
                        break;
                }
            }

            if (options.ProcessId <= 0)
                throw new InvalidOperationException("Missing or invalid --pid argument.");
            if (string.IsNullOrWhiteSpace(options.ZipPath))
                throw new InvalidOperationException("Missing --zip argument.");
            if (string.IsNullOrWhiteSpace(options.TargetDir))
                throw new InvalidOperationException("Missing --dir argument.");
            if (string.IsNullOrWhiteSpace(options.GameExeName))
                throw new InvalidOperationException("Missing --game argument.");

            return options;
        }

        private static void WaitForProcessExit(int processId)
        {
            try
            {
                var process = Process.GetProcessById(processId);
                process.WaitForExit();
                Thread.Sleep(ExtractRetryDelayMs);
            }
            catch (ArgumentException)
            {
                // Process already exited.
            }
        }

        private static void InstallZip(UpdaterOptions options, bool enableLog, string logPath)
        {
            var zipPath = Path.GetFullPath(options.ZipPath);
            var targetDir = Path.GetFullPath(options.TargetDir);
            Log(enableLog, logPath, $"InstallZip start. zip={zipPath}");
            if (!File.Exists(zipPath))
                throw new FileNotFoundException("Update zip was not found.", zipPath);
            if (!Directory.Exists(targetDir))
                throw new DirectoryNotFoundException($"Target directory was not found: {targetDir}");

            SweepLeftovers(targetDir, enableLog, logPath);

            // The first run after the old updater rewrites every file, changed or not. The old
            // one rewrote files in place, and macOS can refuse to run a program whose file was
            // rewritten that way however correct its contents; the only cure is a new file, which
            // this run gives every file. From then on nothing is ever rewritten in place, so a file
            // that has not changed can safely be left alone.
            var refreshEverything = File.Exists(Path.Combine(targetDir, ResolveExecutableFileName(LegacyUpdaterStem)));
            if (refreshEverything)
                Log(enableLog, logPath, "First run after the old updater: every file is replaced.");

            using (var archive = ZipFile.OpenRead(zipPath))
            {
                var bundlePayloadPrefix = ResolveBundlePayloadPrefix(options, archive, targetDir);
                Log(enableLog, logPath, $"Archive opened. entries={archive.Entries.Count}, bundlePrefix={bundlePayloadPrefix}");
                var extractedCount = 0;
                var unchangedCount = 0;
                for (var i = 0; i < archive.Entries.Count; i++)
                {
                    var entry = archive.Entries[i];
                    if (entry == null || string.IsNullOrWhiteSpace(entry.FullName))
                        continue;
                    if (string.IsNullOrEmpty(entry.Name))
                        continue;

                    var relativePath = ResolveRelativeEntryPath(entry.FullName, bundlePayloadPrefix);
                    if (string.IsNullOrWhiteSpace(relativePath))
                        continue;

                    if (ShouldSkipEntry(options.SkipFileName, entry.Name))
                    {
                        Log(enableLog, logPath, $"Skipped entry: {entry.FullName}");
                        continue;
                    }

                    var destination = Path.GetFullPath(Path.Combine(targetDir, relativePath));
                    if (!destination.StartsWith(targetDir, StringComparison.OrdinalIgnoreCase))
                        throw new InvalidOperationException($"Unsafe entry path: {entry.FullName}");

                    var parent = Path.GetDirectoryName(destination);
                    if (!string.IsNullOrWhiteSpace(parent))
                        Directory.CreateDirectory(parent);

                    if (!refreshEverything && IsAlreadyInPlace(entry, destination))
                    {
                        Log(enableLog, logPath, $"Unchanged, left alone: {entry.FullName}");
                        unchangedCount++;
                        continue;
                    }

                    ExtractEntryWithRetry(entry, destination, enableLog, logPath);
                    extractedCount++;
                }

                Log(enableLog, logPath, $"Archive extraction finished. extracted={extractedCount}, unchanged={unchangedCount}");
            }

            File.Delete(zipPath);
            Log(enableLog, logPath, "Deleted update zip.");

            RemoveLegacyUpdater(targetDir, enableLog, logPath);
        }

        /// <summary>
        /// Deletes what an earlier run left behind: a file renamed aside because it was running at
        /// the time, and a file written but never renamed into place because the run did not finish.
        /// Neither is ever read again.
        /// Only where the file it was made from is sitting beside it, so that nothing is removed
        /// but what this program wrote. A leftover without one belongs to a file that is missing
        /// altogether, and the next update to reach that file restores it and takes the leftover
        /// with it on the run after.
        /// Best effort throughout: anything still held is left for the run after.
        /// </summary>
        private static void SweepLeftovers(string targetDir, bool enableLog, string logPath)
        {
            foreach (var suffix in new[] { PendingSuffix, SupersededSuffix })
            {
                string[] leftovers;
                try
                {
                    leftovers = Directory.GetFiles(targetDir, "*" + suffix, SearchOption.AllDirectories);
                }
                catch (IOException)
                {
                    continue;
                }
                catch (UnauthorizedAccessException)
                {
                    continue;
                }

                foreach (var leftover in leftovers)
                {
                    if (!File.Exists(leftover.Substring(0, leftover.Length - suffix.Length)))
                        continue;

                    try
                    {
                        File.Delete(leftover);
                        Log(enableLog, logPath, $"Removed leftover file: {leftover}");
                    }
                    catch (IOException)
                    {
                    }
                    catch (UnauthorizedAccessException)
                    {
                    }
                }
            }
        }

        /// <summary>
        /// Once this program runs under its current name, the copy under the old one has done
        /// its last job: the old copy skipped itself when it unpacked updates, so the only way a
        /// folder ever got this one was through that copy, and nothing starts it again after.
        /// Only when this process is not itself the old copy, which is also what keeps this from
        /// touching a game folder, where the updater still goes by the old name.
        /// </summary>
        private static void RemoveLegacyUpdater(string targetDir, bool enableLog, string logPath)
        {
            string? ownPath;
            try
            {
                ownPath = Process.GetCurrentProcess().MainModule?.FileName;
            }
            catch (Exception)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(ownPath))
                return;

            var ownStem = Path.GetFileNameWithoutExtension(ownPath);
            if (string.Equals(ownStem, LegacyUpdaterStem, StringComparison.OrdinalIgnoreCase))
                return;

            var legacyExecutable = Path.Combine(targetDir, ResolveExecutableFileName(LegacyUpdaterStem));
            if (!File.Exists(legacyExecutable))
                return;

            foreach (var name in new[] { legacyExecutable, legacyExecutable + ".config", Path.Combine(targetDir, LegacyUpdaterStem + ".pdb") })
            {
                try
                {
                    if (File.Exists(name))
                    {
                        File.Delete(name);
                        Log(enableLog, logPath, $"Removed legacy updater file: {name}");
                    }
                }
                catch (IOException)
                {
                }
                catch (UnauthorizedAccessException)
                {
                }
            }

            var legacySymbols = Path.Combine(targetDir, LegacyUpdaterStem + ".dSYM");
            try
            {
                if (Directory.Exists(legacySymbols))
                    Directory.Delete(legacySymbols, recursive: true);
            }
            catch (IOException)
            {
            }
            catch (UnauthorizedAccessException)
            {
            }
        }

        private static string ResolveBundlePayloadPrefix(UpdaterOptions options, ZipArchive archive, string targetDir)
        {
            if (archive == null || string.IsNullOrWhiteSpace(targetDir))
                return string.Empty;

            var normalizedTargetDir = NormalizeZipStylePath(targetDir).TrimEnd('/');
            if (!normalizedTargetDir.EndsWith("/Contents/MacOS", StringComparison.OrdinalIgnoreCase))
                return string.Empty;

            var bundlePrefix = $"{options.GameExeName}.app/Contents/MacOS/";
            for (var i = 0; i < archive.Entries.Count; i++)
            {
                var entry = archive.Entries[i];
                if (entry == null || string.IsNullOrWhiteSpace(entry.FullName))
                    continue;

                var normalizedEntryPath = NormalizeZipStylePath(entry.FullName);
                if (normalizedEntryPath.StartsWith(bundlePrefix, StringComparison.OrdinalIgnoreCase))
                    return bundlePrefix;
            }

            return string.Empty;
        }

        private static string ResolveRelativeEntryPath(string entryFullName, string bundlePayloadPrefix)
        {
            var normalizedEntryPath = NormalizeZipStylePath(entryFullName);
            if (string.IsNullOrWhiteSpace(normalizedEntryPath))
                return string.Empty;

            if (!string.IsNullOrEmpty(bundlePayloadPrefix))
            {
                if (!normalizedEntryPath.StartsWith(bundlePayloadPrefix, StringComparison.OrdinalIgnoreCase))
                    return string.Empty;

                normalizedEntryPath = normalizedEntryPath.Substring(bundlePayloadPrefix.Length);
                if (string.IsNullOrWhiteSpace(normalizedEntryPath))
                    return string.Empty;
            }

            return normalizedEntryPath.Replace('/', Path.DirectorySeparatorChar);
        }

        private static string NormalizeZipStylePath(string path)
        {
            return (path ?? string.Empty).Replace('\\', '/');
        }

        private static void StartGame(UpdaterOptions options, bool enableLog, string logPath)
        {
            var gamePath = ResolveGamePath(options.TargetDir, options.GameExeName);
            Log(enableLog, logPath, $"Resolved game path: {gamePath}");
            if (string.IsNullOrWhiteSpace(gamePath) || !File.Exists(gamePath))
                throw new FileNotFoundException(
                    "Updated game executable was not found.",
                    Path.Combine(options.TargetDir, ResolveExecutableFileName(options.GameExeName)));

            var workingDirectory = Path.GetDirectoryName(gamePath);
            if (string.IsNullOrWhiteSpace(workingDirectory))
                workingDirectory = options.TargetDir;

            var process = Process.Start(new ProcessStartInfo
            {
                FileName = gamePath,
                WorkingDirectory = workingDirectory,
                UseShellExecute = true
            });
            Log(enableLog, logPath, process == null
                ? "Process.Start returned null for game restart."
                : $"Game restart process started. pid={process.Id}");
        }

        private static void ExtractEntryWithRetry(ZipArchiveEntry entry, string destination, bool enableLog, string logPath)
        {
            Exception? lastError = null;
            for (var attempt = 1; attempt <= ExtractRetryCount; attempt++)
            {
                try
                {
                    var pending = destination + PendingSuffix;
                    entry.ExtractToFile(pending, overwrite: true);
                    MoveIntoPlace(pending, destination);
                    if (attempt > 1)
                        Log(enableLog, logPath, $"Extract retry succeeded for {destination} on attempt {attempt}.");
                    return;
                }
                catch (IOException ex)
                {
                    lastError = ex;
                    Log(enableLog, logPath, $"Extract retry {attempt}/{ExtractRetryCount} failed for {destination}: {ex.Message}");
                }
                catch (UnauthorizedAccessException ex)
                {
                    lastError = ex;
                    Log(enableLog, logPath, $"Extract retry {attempt}/{ExtractRetryCount} failed for {destination}: {ex.Message}");
                }

                Thread.Sleep(ExtractRetryDelayMs);
            }

            throw new IOException(
                $"Failed to extract '{entry.FullName}' to '{destination}' after {ExtractRetryCount} attempts.",
                lastError);
        }

        /// <summary>
        /// Whether the file already on disk is the one in the archive, byte for byte, so that a
        /// file that has not changed between releases is not written again. Most files in a
        /// release are such files: the runtime, the libraries, the sounds. Leaving them alone
        /// means fewer writes, and it means a program still running from one of them, or still
        /// mapping it, is never disturbed for nothing.
        /// </summary>
        private static bool IsAlreadyInPlace(ZipArchiveEntry entry, string destination)
        {
            try
            {
                var existing = new FileInfo(destination);
                if (!existing.Exists || existing.Length != entry.Length)
                    return false;

                using (var fromArchive = entry.Open())
                using (var onDisk = existing.OpenRead())
                {
                    var a = new byte[81920];
                    var b = new byte[81920];
                    while (true)
                    {
                        var readA = ReadFully(fromArchive, a);
                        var readB = ReadFully(onDisk, b);
                        if (readA != readB)
                            return false;
                        if (readA == 0)
                            return true;
                        for (var i = 0; i < readA; i++)
                        {
                            if (a[i] != b[i])
                                return false;
                        }
                    }
                }
            }
            catch (IOException)
            {
                return false;
            }
            catch (UnauthorizedAccessException)
            {
                return false;
            }
        }

        /// <summary>Fills the buffer as far as the stream allows; a short read is only the end.</summary>
        private static int ReadFully(Stream stream, byte[] buffer)
        {
            var total = 0;
            while (total < buffer.Length)
            {
                var read = stream.Read(buffer, total, buffer.Length - total);
                if (read <= 0)
                    break;
                total += read;
            }

            return total;
        }

        /// <summary>
        /// Puts the freshly written file where the old one was, by renaming. The old file is
        /// deleted or renamed aside rather than overwritten, so the folder only ever holds whole
        /// files and a program still running from the old one keeps it until it exits.
        /// </summary>
        private static void MoveIntoPlace(string pending, string destination)
        {
#if NET472
            if (File.Exists(destination))
                MakeRoom(destination);

            File.Move(pending, destination);
#else
            try
            {
                File.Move(pending, destination, overwrite: true);
            }
            catch (IOException) when (File.Exists(destination))
            {
                // Windows will not replace a program that is running, and the one program that
                // may be running here is this one. It will move, though, where it will not go.
                MakeRoom(destination);
                File.Move(pending, destination);
            }
            catch (UnauthorizedAccessException) when (File.Exists(destination))
            {
                MakeRoom(destination);
                File.Move(pending, destination);
            }
#endif
        }

        private static void MakeRoom(string destination)
        {
            try
            {
                File.Delete(destination);
                return;
            }
            catch (IOException)
            {
            }
            catch (UnauthorizedAccessException)
            {
            }

            var aside = destination + SupersededSuffix;
            if (File.Exists(aside))
                File.Delete(aside);
            File.Move(destination, aside);
        }

        private static string ResolveGamePath(string targetDir, string gameExeName)
        {
            var resolvedFileName = ResolveExecutableFileName(gameExeName);
            var directPath = Path.Combine(targetDir, resolvedFileName);
            if (File.Exists(directPath))
                return directPath;

            var matches = Directory.GetFiles(targetDir, resolvedFileName, SearchOption.AllDirectories);
            if (matches.Length == 0)
                return directPath;
            if (matches.Length == 1)
                return matches[0];

            var bestMatch = matches[0];
            var bestDepth = GetPathDepth(bestMatch);
            for (var i = 1; i < matches.Length; i++)
            {
                var candidate = matches[i];
                var candidateDepth = GetPathDepth(candidate);
                if (candidateDepth < bestDepth)
                {
                    bestMatch = candidate;
                    bestDepth = candidateDepth;
                }
            }

            return bestMatch;
        }

        private static bool ShouldSkipEntry(string skipStem, string entryName)
        {
            if (string.IsNullOrWhiteSpace(skipStem) || string.IsNullOrWhiteSpace(entryName))
                return false;

            var runtimeFileName = ResolveExecutableFileName(skipStem);
            if (string.Equals(entryName, runtimeFileName, StringComparison.OrdinalIgnoreCase))
                return true;

            var entryStem = Path.GetFileNameWithoutExtension(entryName);
            return string.Equals(entryStem, skipStem, StringComparison.OrdinalIgnoreCase);
        }

        private static int GetPathDepth(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return int.MaxValue;

            var fullPath = Path.GetFullPath(path);
            var root = Path.GetPathRoot(fullPath) ?? string.Empty;
            var relative = fullPath.Substring(root.Length);
            if (relative.Length == 0)
                return 0;

            var segments = relative.Split(new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar }, StringSplitOptions.RemoveEmptyEntries);
            return segments.Length;
        }

        private static string ResolveExecutableFileName(string stem)
        {
            if (string.IsNullOrWhiteSpace(stem))
                throw new ArgumentException("Executable stem is required.", nameof(stem));

            return RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                ? stem + ".exe"
                : stem;
        }

        private static bool HasLogFlag(string[] args)
        {
            if (args == null || args.Length == 0)
                return false;

            for (var i = 0; i < args.Length; i++)
            {
                if (string.Equals(args[i], "--log", StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }

        private static string ResolveLogPath(string[] args)
        {
            try
            {
                for (var i = 0; i < args.Length - 1; i++)
                {
                    if (!string.Equals(args[i], "--dir", StringComparison.OrdinalIgnoreCase))
                        continue;

                    var dir = args[i + 1];
                    if (string.IsNullOrWhiteSpace(dir))
                        break;

                    return Path.Combine(Path.GetFullPath(dir), "updater.log");
                }
            }
            catch
            {
            }

            return Path.Combine(Path.GetTempPath(), "topspeed_updater.log");
        }

        private static void Log(bool enabled, string path, string message)
        {
            if (!enabled)
                return;

            WriteLog(path, message);
        }

        private static void WriteLog(string path, string message)
        {
            try
            {
                var directory = Path.GetDirectoryName(path);
                if (!string.IsNullOrWhiteSpace(directory))
                    Directory.CreateDirectory(directory);

                var line = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] {message}{Environment.NewLine}";
                File.AppendAllText(path, line);
            }
            catch
            {
                // Ignore logging failures.
            }
        }


        private sealed class UpdaterOptions
        {
            public int ProcessId { get; set; }
            public string ZipPath { get; set; } = string.Empty;
            public string TargetDir { get; set; } = string.Empty;
            public string GameExeName { get; set; } = string.Empty;
            public string SkipFileName { get; set; } = string.Empty;
            public bool EnableLog { get; set; }
        }
    }
}
