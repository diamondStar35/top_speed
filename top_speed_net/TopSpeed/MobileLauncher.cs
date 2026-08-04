using TopSpeed.Core;
using TopSpeed.Game;
using TopSpeed.Localization;
using TopSpeed.Runtime;
using TopSpeed.Windowing.Sdl;
using System;
using System.IO;

namespace TopSpeed
{
    public static class MobileLauncher
    {
        private static readonly object Sync = new object();
        private static WindowHost? _window;
        private static string? _assetRoot;
        private static string? _userContentRoot;
        private static bool _running;

        public static void SetAssetRoot(string? path)
        {
            lock (Sync)
                _assetRoot = path;
        }

        public static void SetUserContentRoot(string? path)
        {
            lock (Sync)
                _userContentRoot = path;
        }

        public static void Run()
        {
            lock (Sync)
            {
                if (_running)
                    return;
                _running = true;
            }

            try
            {
                Environment.SetEnvironmentVariable("TOPSPEED_TOUCH_HINTS", "1");

                var configuredRoot = _assetRoot;
                if (!string.IsNullOrWhiteSpace(configuredRoot))
                {
                    AssetPaths.SetRoot(configuredRoot);
                    LocalizationBootstrap.SetLanguagesRoot(Path.Combine(configuredRoot!, "languages"));
                }

                // Left unset on platforms that do not separate the two, which keeps the user content
                // root pointing at the asset root exactly as before.
                var configuredUserContentRoot = _userContentRoot;
                if (!string.IsNullOrWhiteSpace(configuredUserContentRoot))
                {
                    Directory.CreateDirectory(configuredUserContentRoot!);
                    AssetPaths.SetUserContentRoot(configuredUserContentRoot);
                }

                NativeLibraryBootstrap.Initialize();
                var window = new WindowHost();
                lock (Sync)
                    _window = window;

                using (var app = new GameApp(
                           window,
                           window,
                           new LoopHost(),
                           new FileDialogService(window),
                           new ClipboardService()))
                {
                    app.Run();
                }
            }
            finally
            {
                lock (Sync)
                {
                    _window = null;
                    _running = false;
                }
            }
        }

        public static void RequestClose()
        {
            lock (Sync)
                _window?.RequestClose();
        }
    }
}
