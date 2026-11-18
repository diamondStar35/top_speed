using System;
using System.Diagnostics;
using System.IO;
using TopSpeed.Core.Updates;
using TopSpeed.Localization;
using TopSpeed.Runtime;

namespace TopSpeed.Game
{
    internal sealed partial class Game
    {
        private void LaunchUpdaterAndExit()
        {
            if (TryInstallAndroidPackage())
                return;

            var root = AppContext.BaseDirectory;
            var updaterDir = TrimTrailingDirectorySeparator(root);
            var updaterPath = ResolveExecutablePath(root, _updateConfig.UpdaterEntryName);
            var skipArgument = string.Empty;
            if (!File.Exists(updaterPath))
            {
                // Old installations must still be able to unpack the new updater's filename.
                updaterPath = ResolveExecutablePath(root, UpdateConfig.LegacyUpdaterEntryName);
                skipArgument = $" --skip \"{UpdateConfig.LegacyUpdaterEntryName}\"";
            }
            if (!File.Exists(updaterPath))
            {
                var expectedUpdaterFileName = RuntimeAssetResolver.ResolveExecutableFileName(_updateConfig.UpdaterEntryName);
                ShowMessageDialog(
                    LocalizationService.Mark("Updater not found"),
                    LocalizationService.Mark("The update could not be installed automatically."),
                    new[]
                    {
                        LocalizationService.Format(
                            LocalizationService.Mark("Missing file: {0}"),
                            expectedUpdaterFileName)
                    });
                return;
            }

            if (string.IsNullOrWhiteSpace(_updateZipPath) || !File.Exists(_updateZipPath))
            {
                ShowMessageDialog(
                    LocalizationService.Mark("Update package missing"),
                    LocalizationService.Mark("The update package file was not found."),
                    new[] { LocalizationService.Mark("You can download the update again or install manually.") });
                return;
            }

            try
            {
                var currentProcess = Process.GetCurrentProcess();
                var args =
                    $"--pid {currentProcess.Id} --zip \"{_updateZipPath}\" --dir \"{updaterDir}\" --game \"{_updateConfig.GameEntryName}\"{skipArgument}";
                var startInfo = new ProcessStartInfo
                {
                    FileName = updaterPath,
                    Arguments = args,
                    WorkingDirectory = root,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                Process.Start(startInfo);
                ExitRequested?.Invoke();
            }
            catch (Exception ex)
            {
                ShowMessageDialog(
                    LocalizationService.Mark("Updater launch failed"),
                    LocalizationService.Mark("The updater could not be started."),
                    new[] { ex.Message });
            }
        }

        private bool TryInstallAndroidPackage()
        {
            if (!OperatingSystem.IsAndroid())
                return false;

            if (string.IsNullOrWhiteSpace(_updateZipPath) || !File.Exists(_updateZipPath))
            {
                ShowMessageDialog(
                    LocalizationService.Mark("Update package missing"),
                    LocalizationService.Mark("The update package file was not found."),
                    new[] { LocalizationService.Mark("You can download the update again or install manually.") });
                return true;
            }

            if (!string.Equals(Path.GetExtension(_updateZipPath), ".apk", StringComparison.OrdinalIgnoreCase))
            {
                ShowMessageDialog(
                    LocalizationService.Mark("Update package invalid"),
                    LocalizationService.Mark("Android updates require an APK package."),
                    new[]
                    {
                        LocalizationService.Format(
                            LocalizationService.Mark("Selected file: {0}"),
                            Path.GetFileName(_updateZipPath))
                    });
                return true;
            }

            var installer = UpdatePackageRuntime.GetInstaller();
            if (installer == null)
            {
                ShowMessageDialog(
                    LocalizationService.Mark("Updater unavailable"),
                    LocalizationService.Mark("Android package installer is not available."),
                    new[] { LocalizationService.Mark("Please restart the game and try again.") });
                return true;
            }

            if (!installer.TryInstallPackage(_updateZipPath, out var error))
            {
                ShowMessageDialog(
                    LocalizationService.Mark("Updater launch failed"),
                    LocalizationService.Mark("The updater could not be started."),
                    new[] { string.IsNullOrWhiteSpace(error) ? LocalizationService.Mark("Unknown error.") : error });
                return true;
            }

            ExitRequested?.Invoke();
            return true;
        }

        private static string ResolveExecutablePath(string root, string executableStem)
        {
            // A private worker from an interrupted update is not the installed updater.
            return Path.Combine(root, RuntimeAssetResolver.ResolveExecutableFileName(executableStem));
        }

        private static string TrimTrailingDirectorySeparator(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return string.Empty;

            var fullPath = Path.GetFullPath(path);
            var root = Path.GetPathRoot(fullPath) ?? string.Empty;
            if (string.Equals(fullPath, root, StringComparison.OrdinalIgnoreCase))
                return fullPath;

            return fullPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        }
    }
}

