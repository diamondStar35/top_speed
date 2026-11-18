using System;
using System.IO;
using System.Threading;

namespace TopSpeed.Updater.Archive
{
    internal static class Replacement
    {
        public static void Install(string source, string destination, Log log)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
            Exception? lastError = null;
            for (var attempt = 0; attempt < 24; attempt++)
            {
                try
                {
                    if (Platform.IsWindows)
                        File.Copy(source, destination, overwrite: true);
                    else
                    {
#if NET472
                        throw new PlatformNotSupportedException("The .NET Framework updater is Windows-only.");
#else
                        // Staging is on the destination filesystem: rename creates a fresh inode
                        // without touching code still mapped by this process or macOS's cache.
                        File.Move(source, destination, overwrite: true);
#endif
                    }
                    return;
                }
                catch (IOException error) { lastError = error; }
                catch (UnauthorizedAccessException error) { lastError = error; }
                log.Write($"Replacement attempt {attempt + 1} failed: {destination}: {lastError.Message}");
                Thread.Sleep(250);
            }
            throw new IOException("Could not replace " + destination, lastError);
        }
    }
}
