using System;

namespace TopSpeed.Updater
{
    internal static class Program
    {
        private static int Main(string[] args)
        {
            Log? log = null;
            try
            {
                var options = Arguments.Parse(args);
                log = new Log(options);
                if (options.Finalize)
                    return Windows.Handoff.Finalize(options, log);
                if (Platform.IsWindows && options.WorkerProcessId == 0)
                {
                    Windows.Handoff.Start(options, log);
                    return 0;
                }
                if (Platform.IsWindows)
                    return Windows.Handoff.RunWorker(options, log);
                Processes.Wait(options.ProcessId);
                Archive.Installer.Install(options, log);
                Processes.StartGame(options, log);
                return 0;
            }
            catch (Exception error)
            {
                log?.Write("Update failed: " + error);
                Console.Error.WriteLine(error.Message);
                return 1;
            }
        }
    }
}
