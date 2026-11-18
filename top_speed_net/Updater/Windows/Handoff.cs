using System;
using System.Diagnostics;
using System.IO;
using System.Threading;

namespace TopSpeed.Updater.Windows
{
    internal static class Handoff
    {
        public static void Start(Options options, Log log)
        {
            RequireExecutable(options.TargetDir);
            var directory = WorkerFiles.Create(options.TargetDir);
            try
            {
                WorkerFiles.Copy(options.TargetDir, directory);
                using (var process = Process.GetCurrentProcess())
                    options.WorkerProcessId = process.Id;
                Processes.StartUpdater(Path.Combine(directory, Platform.Executable(Platform.UpdaterName)), directory, options);
                log.Write("Windows worker started.");
            }
            catch
            {
                WorkerFiles.Delete(options.TargetDir, directory);
                throw;
            }
        }

        public static int RunWorker(Options options, Log log)
        {
            var directory = Path.GetDirectoryName(Platform.CurrentExecutable())!;
            WorkerFiles.ValidateDirectory(options.TargetDir, directory);
            Processes.Wait(options.WorkerProcessId);
            try
            {
                Processes.Wait(options.ProcessId);
                Archive.Installer.Install(options, log);
                StartFinalizer(options, directory, string.Empty);
                return 0;
            }
            catch (Exception failure)
            {
                log.Write("Update failed; restoring the updater: " + failure);
                // The worker is a complete copy of the last updater, so a partial replacement
                // cannot leave the installation without a working updater/finalizer.
                WorkerFiles.Copy(directory, options.TargetDir);
                StartFinalizer(options, directory, failure.Message);
                return 1;
            }
        }

        private static void StartFinalizer(Options options, string directory, string error)
        {
            using (var process = Process.GetCurrentProcess())
                options.ProcessId = process.Id;
            options.Finalize = true;
            options.WorkerDirectory = directory;
            options.Error = error;
            Processes.StartUpdater(Path.Combine(options.TargetDir, Platform.Executable(Platform.UpdaterName)), options.TargetDir, options);
        }

        public static int Finalize(Options options, Log log)
        {
            RequireExecutable(options.TargetDir);
            WorkerFiles.ValidateDirectory(options.TargetDir, options.WorkerDirectory);
            Processes.Wait(options.ProcessId);
            DeleteWorker(options, log);
            if (options.Error.Length > 0)
            {
                log.Write("Update was not installed completely: " + options.Error);
                Console.Error.WriteLine(options.Error);
            }
            Processes.StartGame(options, log);
            return options.Error.Length == 0 ? 0 : 1;
        }

        private static void RequireExecutable(string target)
        {
            var expected = Path.Combine(target, Platform.Executable(Platform.UpdaterName));
            if (!string.Equals(Path.GetFullPath(Platform.CurrentExecutable()), Path.GetFullPath(expected), Platform.PathComparison))
                throw new InvalidOperationException("Launch the installed updater executable directly.");
        }

        private static void DeleteWorker(Options options, Log log)
        {
            for (var attempt = 0; attempt < 20; attempt++)
            {
                try
                {
                    WorkerFiles.Delete(options.TargetDir, options.WorkerDirectory);
                    return;
                }
                catch (IOException error) { log.Write("Worker cleanup: " + error.Message); }
                catch (UnauthorizedAccessException error) { log.Write("Worker cleanup: " + error.Message); }
                Thread.Sleep(100);
            }
            log.Write("The temporary updater could not be removed; the update itself is installed.");
        }
    }
}
