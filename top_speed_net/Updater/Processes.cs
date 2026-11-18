using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

namespace TopSpeed.Updater
{
    internal static class Processes
    {
        public static void Wait(int processId)
        {
            try
            {
                using (var process = Process.GetProcessById(processId))
                {
                    if (!process.WaitForExit(300_000))
                        throw new TimeoutException("The process did not exit within five minutes.");
                }
                Thread.Sleep(250);
            }
            catch (ArgumentException)
            {
                // The parent has already exited.
            }
        }

        public static void StartUpdater(string executable, string directory, Options options)
        {
            var start = new ProcessStartInfo
            {
                FileName = executable,
                WorkingDirectory = directory,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            var args = Arguments.Build(options);
#if NET472
            start.Arguments = string.Join(" ", args.Select(Quote));
#else
            foreach (var argument in args)
                start.ArgumentList.Add(argument);
#endif
            using (var process = Process.Start(start))
            {
                if (process == null)
                    throw new InvalidOperationException("The updater process could not be started.");
            }
        }

        public static void StartGame(Options options, Log log)
        {
            var executable = Platform.ResolveExecutable(options.TargetDir, options.GameName);
            if (!File.Exists(executable))
                throw new FileNotFoundException("The updated executable was not found.", executable);
            using (var process = Process.Start(new ProcessStartInfo
            {
                FileName = executable,
                WorkingDirectory = Path.GetDirectoryName(executable),
                UseShellExecute = true
            }))
            {
                if (process == null)
                    throw new InvalidOperationException("The updated application could not be started.");
                log.Write("Application restarted.");
            }
        }

        internal static string Quote(string value)
        {
            var result = new StringBuilder("\"");
            var slashes = 0;
            foreach (var character in value)
            {
                if (character == '\\')
                {
                    slashes++;
                    continue;
                }
                result.Append('\\', character == '"' ? slashes * 2 + 1 : slashes);
                result.Append(character);
                slashes = 0;
            }
            result.Append('\\', slashes * 2);
            return result.Append('"').ToString();
        }
    }
}
