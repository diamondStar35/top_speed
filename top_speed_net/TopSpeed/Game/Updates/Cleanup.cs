using System;
using System.IO;

namespace TopSpeed.Game
{
    internal sealed partial class Game
    {
        private void CleanupAndroidUpdatePackages()
        {
            if (!OperatingSystem.IsAndroid())
                return;

            DeleteApkFiles(AppContext.BaseDirectory);
        }

        private static void DeleteApkFiles(string directory)
        {
            if (string.IsNullOrWhiteSpace(directory) || !Directory.Exists(directory))
                return;

            string[] apkFiles;
            try
            {
                apkFiles = Directory.GetFiles(directory, "*.apk", SearchOption.TopDirectoryOnly);
            }
            catch (IOException)
            {
                return;
            }
            catch (UnauthorizedAccessException)
            {
                return;
            }
            catch (System.Security.SecurityException)
            {
                return;
            }

            for (var i = 0; i < apkFiles.Length; i++)
            {
                try
                {
                    File.Delete(apkFiles[i]);
                }
                catch (IOException)
                {
                    // Keep startup resilient if another process already removed the file.
                }
                catch (UnauthorizedAccessException)
                {
                    // Keep startup resilient if another process already removed the file.
                }
            }
        }
    }
}
