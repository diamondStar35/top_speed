using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Threading;
using TopSpeed.Server.Logging;

using TopSpeed.Localization;
using TopSpeed.Runtime;
namespace TopSpeed.Server.Updates
{
    internal sealed class ServerUpdateRunner
    {
        private readonly ServerUpdateConfig _config;
        private readonly ServerUpdateService _service;
        private readonly Logger _logger;

        private int _lastProgressPercent = -1;
        private int _lastProgressLineLength;

        public ServerUpdateRunner(ServerUpdateConfig config, Logger logger)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _service = new ServerUpdateService(_config);
        }

        public bool RunInteractiveCheck()
        {
            ConsoleSink.WriteLine(LocalizationService.Mark("Checking for update..."));
            var result = _service
                .CheckAsync(ServerUpdateConfig.CurrentVersion, CancellationToken.None)
                .GetAwaiter()
                .GetResult();

            if (!result.IsSuccess)
            {
                var message = string.IsNullOrWhiteSpace(result.ErrorMessage)
                    ? LocalizationService.Mark("Update check failed.")
                    : result.ErrorMessage;
                _logger.Warning(LocalizationService.Format(
                    LocalizationService.Mark("Server update check failed: {0}"),
                    message));
                ConsoleSink.WriteLine(message);
                return false;
            }

            if (result.Update == null)
            {
                ConsoleSink.WriteLine(LocalizationService.Mark("Server is up-to-date."));
                return false;
            }

            var update = result.Update;
            var currentVersion = ServerUpdateConfig.CurrentVersion.ToMachineString();
            ConsoleSink.WriteLineFormat(LocalizationService.Mark("A new update is available for the server. Your current server version is {0}. Available version: {1}."),
                currentVersion,
                update.VersionText);
            ConsoleSink.WriteLine(LocalizationService.Mark("Changes:"));
            if (update.Changes.Count == 0)
            {
                ConsoleSink.WriteLine(LocalizationService.Mark("No changes were listed for this update."));
            }
            else
            {
                for (var i = 0; i < update.Changes.Count; i++)
                {
                    var change = update.Changes[i];
                    if (string.IsNullOrWhiteSpace(change))
                        continue;
                    ConsoleSink.WriteLine(change.Trim());
                }
            }

            if (!TryPromptYesNo(LocalizationService.Mark("Would you like to download the update? (y/n)"), out var shouldDownload))
            {
                var message = LocalizationService.Mark("Standard input is not available. Update download was skipped.");
                _logger.Warning(message);
                ConsoleSink.WriteLine(message);
                return false;
            }

            if (!shouldDownload)
                return false;

            ConsoleSink.WriteLine(LocalizationService.Mark("Downloading..."));
            ResetProgress();
            var download = _service
                .DownloadAsync(update, AppContext.BaseDirectory, RenderProgress, CancellationToken.None)
                .GetAwaiter()
                .GetResult();
            CompleteProgressLine();

            if (!download.IsSuccess)
            {
                var message = string.IsNullOrWhiteSpace(download.ErrorMessage)
                    ? LocalizationService.Mark("Download failed.")
                    : download.ErrorMessage;
                _logger.Warning(LocalizationService.Format(
                    LocalizationService.Mark("Server update download failed: {0}"),
                    message));
                ConsoleSink.WriteLine(message);
                return false;
            }

            if (!StartUpdater(download.ZipPath))
                return false;

            return true;
        }

        private bool StartUpdater(string zipPath)
        {
            var root = AppContext.BaseDirectory;

            // The updater under its current name replaces every file in the folder, itself
            // included, so it is told to skip nothing. A folder installed before it had that name
            // still has the old one, which rewrites files in place and cannot replace itself, so
            // it is told to skip itself as it always was; the archive it unpacks carries the new
            // one under the new name, which it does not skip, and every update after uses that.
            var updaterPath = ResolveExecutablePath(root, _config.UpdaterEntryName);
            var skipEntryName = string.Empty;
            if (!File.Exists(updaterPath))
            {
                updaterPath = ResolveExecutablePath(root, ServerUpdateConfig.LegacyUpdaterEntryName);
                skipEntryName = ServerUpdateConfig.LegacyUpdaterEntryName;
            }

            if (!File.Exists(updaterPath))
            {
                ConsoleSink.WriteLineFormat(
                    LocalizationService.Mark("Updater not found: {0}"),
                    RuntimeAssetResolver.ResolveExecutableFileName(_config.UpdaterEntryName));
                return false;
            }

            try
            {
                var process = Process.GetCurrentProcess();
                var startInfo = new ProcessStartInfo
                {
                    FileName = updaterPath,
                    WorkingDirectory = root,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                startInfo.ArgumentList.Add("--pid");
                startInfo.ArgumentList.Add(process.Id.ToString());
                startInfo.ArgumentList.Add("--zip");
                startInfo.ArgumentList.Add(zipPath);
                startInfo.ArgumentList.Add("--dir");
                startInfo.ArgumentList.Add(root);
                startInfo.ArgumentList.Add("--game");
                startInfo.ArgumentList.Add(_config.ServerEntryName);
                if (skipEntryName.Length > 0)
                {
                    startInfo.ArgumentList.Add("--skip");
                    startInfo.ArgumentList.Add(skipEntryName);
                }

                Process.Start(startInfo);
                return true;
            }
            catch (Exception ex)
            {
                _logger.Warning(LocalizationService.Format(
                    LocalizationService.Mark("Could not launch updater: {0}"),
                    ex.Message));
                ConsoleSink.WriteLineFormat(LocalizationService.Mark("Could not launch updater: {0}"), ex.Message);
                return false;
            }
        }

        private static string ResolveExecutablePath(string root, string executableStem)
        {
            var fileName = RuntimeAssetResolver.ResolveExecutableFileName(executableStem);
            var directPath = Path.Combine(root, fileName);
            if (File.Exists(directPath))
                return directPath;

            var matches = Directory.GetFiles(root, fileName, SearchOption.AllDirectories);
            if (matches.Length == 0)
                return directPath;
            if (matches.Length == 1)
                return matches[0];

            Array.Sort(matches, StringComparer.OrdinalIgnoreCase);
            return matches[0];
        }

        private static bool TryPromptYesNo(string prompt, out bool value)
        {
            value = false;
            while (true)
            {
                if (!ConsoleSink.WriteLine(prompt))
                    return false;

                string? line;
                try
                {
                    line = Console.ReadLine();
                }
                catch (InvalidOperationException)
                {
                    return false;
                }
                catch (IOException)
                {
                    return false;
                }

                if (line == null)
                    return false;

                var text = line.Trim();
                if (text.Equals("y", StringComparison.OrdinalIgnoreCase) ||
                    text.Equals("yes", StringComparison.OrdinalIgnoreCase))
                {
                    value = true;
                    return true;
                }

                if (text.Equals("n", StringComparison.OrdinalIgnoreCase) ||
                    text.Equals("no", StringComparison.OrdinalIgnoreCase))
                {
                    value = false;
                    return true;
                }

                ConsoleSink.WriteLine(LocalizationService.Mark("Invalid input. Enter y or n."));
            }
        }

        private void RenderProgress(ServerDownloadProgress progress)
        {
            var percent = Math.Clamp(progress.Percent, 0, 100);
            if (Console.IsOutputRedirected)
            {
                if (percent == _lastProgressPercent)
                    return;

                _lastProgressPercent = percent;
                ConsoleSink.WriteLine(percent.ToString(CultureInfo.InvariantCulture) + "%");
                return;
            }

            var barWidth = 40;
            var filled = (percent * barWidth) / 100;
            var remaining = barWidth - filled;
            var bar = $"[{new string('#', filled)}{new string('-', remaining)}]";
            var downloadedText = FormatBytes(progress.DownloadedBytes);
            var totalText = progress.TotalBytes > 0
                ? FormatBytes(progress.TotalBytes)
                : "?";
            var line = $"{bar} {percent,3}% {downloadedText}/{totalText}";

            try
            {
                var padded = line.PadRight(_lastProgressLineLength);
                Console.Write('\r');
                Console.Write(padded);
                _lastProgressLineLength = Math.Max(_lastProgressLineLength, padded.Length);
            }
            catch (InvalidOperationException)
            {
                ConsoleSink.WriteLine(percent.ToString(CultureInfo.InvariantCulture) + "%");
            }
            catch (IOException)
            {
                ConsoleSink.WriteLine(percent.ToString(CultureInfo.InvariantCulture) + "%");
            }
        }

        private static string FormatBytes(long bytes)
        {
            if (bytes < 0)
                bytes = 0;

            const double kilobyte = 1024d;
            const double megabyte = 1024d * 1024d;
            const double gigabyte = 1024d * 1024d * 1024d;

            if (bytes >= gigabyte)
                return LocalizationService.Format(LocalizationService.Mark("{0:0.00} GB"), bytes / gigabyte);
            if (bytes >= megabyte)
                return LocalizationService.Format(LocalizationService.Mark("{0:0.00} MB"), bytes / megabyte);
            if (bytes >= kilobyte)
                return LocalizationService.Format(LocalizationService.Mark("{0:0.00} KB"), bytes / kilobyte);
            return LocalizationService.Format(LocalizationService.Mark("{0} B"), bytes);
        }

        private void ResetProgress()
        {
            _lastProgressPercent = -1;
            _lastProgressLineLength = 0;
        }

        private void CompleteProgressLine()
        {
            if (Console.IsOutputRedirected || _lastProgressLineLength <= 0)
                return;

            try
            {
                Console.WriteLine();
            }
            catch (InvalidOperationException)
            {
            }
            catch (IOException)
            {
            }
        }
    }
}




