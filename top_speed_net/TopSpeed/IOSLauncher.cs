using System;
using System.Threading;

namespace TopSpeed
{
    public static class IOSLauncher
    {
        private static int _mainThreadId;

        public static void MarkMainThread()
        {
            Interlocked.CompareExchange(ref _mainThreadId, Environment.CurrentManagedThreadId, 0);
        }

        public static bool IsOnMainThread()
        {
            var capturedMainThreadId = Volatile.Read(ref _mainThreadId);
            return capturedMainThreadId != 0 && capturedMainThreadId == Environment.CurrentManagedThreadId;
        }

        public static void SetAssetRoot(string? path)
        {
            MobileLauncher.SetAssetRoot(path);
        }

        public static void SetUserContentRoot(string? path)
        {
            MobileLauncher.SetUserContentRoot(path);
        }

        public static void Run()
        {
            MobileLauncher.Run();
        }

        public static void RequestClose()
        {
            MobileLauncher.RequestClose();
        }
    }
}
