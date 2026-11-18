using System;
using System.IO;

namespace TopSpeed.Updater.Archive
{
    internal static class Installer
    {
        public static void Install(Options options, Log log)
        {
            using (var payload = new Payload(options, log))
            {
                // Fully decompress and validate the package before changing any installed file.
                payload.Extract(options);
                log.Write("Archive validated.");
                foreach (var relative in payload.Files)
                {
                    Replacement.Install(Paths.Within(payload.Root, relative), Paths.Within(options.TargetDir, relative), log);
                }
            }
            try { File.Delete(options.ZipPath); }
            catch (IOException error) { log.Write("Could not remove the installed update archive: " + error.Message); }
            catch (UnauthorizedAccessException error) { log.Write("Could not remove the installed update archive: " + error.Message); }
            RemoveLegacyUpdater(options.TargetDir, log);
            log.Write("Update installed.");
        }

        private static void RemoveLegacyUpdater(string root, Log log)
        {
            var names = new[]
            {
                Platform.Executable(Platform.LegacyName), Platform.LegacyName + ".exe.config",
                Platform.LegacyName + ".dll", Platform.LegacyName + ".deps.json",
                Platform.LegacyName + ".runtimeconfig.json", Platform.LegacyName + ".pdb"
            };
            foreach (var name in names)
            {
                try { File.Delete(Path.Combine(root, name)); }
                catch (IOException error) { log.Write("Could not remove legacy updater: " + error.Message); }
                catch (UnauthorizedAccessException error) { log.Write("Could not remove legacy updater: " + error.Message); }
            }
        }
    }
}
