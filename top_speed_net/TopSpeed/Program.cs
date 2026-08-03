using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
#if WINDOWS
using System.Windows.Forms;
#else
using Eto.Forms;
#endif
using TopSpeed.Game;
using TopSpeed.Localization;
using TopSpeed.Runtime;
#if WINDOWS
using TopSpeed.Windowing.WinForms;
#else
using TopSpeed.Windowing.Eto;
#endif

namespace TopSpeed
{
    internal static class Program
    {
        private static int _exceptionHandled;

        [STAThread]
        private static void Main()
        {
            RegisterGlobalExceptionHandlers();

            try
            {
#if WINDOWS
                using var timerResolution = new WindowsTimerResolution(1);
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
#endif

                NativeLibraryBootstrap.Initialize();

#if WINDOWS
                var window = new WindowHost();
                using (var app = new GameApp(
                           window,
                           window,
                           new LoopHost(),
                           new FileDialogService(),
                           new ClipboardService()))
#else
                var window = new WindowHost();
                if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                    SpeechThreadRuntime.SetDispatcher(new UiThreadSpeechDispatcher());
                var textInput = new TextInputService(window);
                using (var app = new GameApp(
                           window,
                           textInput,
                           new LoopHost(),
                           new FileDialogService(window),
                           new ClipboardService()))
#endif
                {
                    app.Run();
                }
            }
            catch (Exception ex)
            {
                HandleException(ex);
            }
        }

        private static void RegisterGlobalExceptionHandlers()
        {
#if WINDOWS
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
            Application.ThreadException += (_, args) =>
                HandleException(args.Exception);
#endif
            AppDomain.CurrentDomain.UnhandledException += (_, args) =>
                HandleException(args.ExceptionObject as Exception ?? new Exception(LocalizationService.Mark("Unknown exception.")));
            TaskScheduler.UnobservedTaskException += (_, args) =>
            {
                HandleException(args.Exception);
                args.SetObserved();
            };
        }

#if WINDOWS
        private sealed class WindowsTimerResolution : IDisposable
        {
            private readonly uint _milliseconds;
            private readonly bool _active;

            public WindowsTimerResolution(uint milliseconds)
            {
                _milliseconds = milliseconds;
                try
                {
                    _active = timeBeginPeriod(_milliseconds) == 0;
                }
                catch
                {
                    _active = false;
                }
            }

            public void Dispose()
            {
                if (!_active)
                    return;

                try
                {
                    timeEndPeriod(_milliseconds);
                }
                catch
                {
                }
            }

            [DllImport("winmm.dll", EntryPoint = "timeBeginPeriod")]
            private static extern uint timeBeginPeriod(uint uPeriod);

            [DllImport("winmm.dll", EntryPoint = "timeEndPeriod")]
            private static extern uint timeEndPeriod(uint uPeriod);
        }
#endif

        private static void HandleException(Exception exception)
        {
            if (Interlocked.Exchange(ref _exceptionHandled, 1) != 0)
                return;

            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            var logName = $"topspeed_error_{timestamp}.log";
            var logPath = TryWriteLogFile(logName, BuildLogContents(exception));
            var logReference = string.IsNullOrWhiteSpace(logPath) ? logName : logPath;

#if WINDOWS
            try
            {
                MessageBox.Show(
                    LocalizationService.Format(
                        LocalizationService.Mark("An unexpected error occurred. A log file was created: {0}"),
                        logReference),
                    LocalizationService.Translate(LocalizationService.Mark("Top Speed")),
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
            catch
            {
                // Ignore UI failures.
            }
#else
            try
            {
                var message = LocalizationService.Format(
                    LocalizationService.Mark("An unexpected error occurred. A log file was created: {0}"),
                    logReference);
                var title = LocalizationService.Translate(LocalizationService.Mark("Top Speed"));
                var application = ApplicationFactory.GetOrCreate();

                void ShowDialog()
                {
                    MessageBox.Show(
                        message,
                        title,
                        MessageBoxType.Error);
                }

                if (Application.Instance != null)
                    application.Invoke(ShowDialog);
                else
                    ShowDialog();
            }
            catch
            {
                // Ignore UI failures.
            }
#endif
        }

        private static string BuildLogContents(Exception exception)
        {
            var builder = new StringBuilder();
            builder.AppendLine("Top Speed startup error log");
            builder.AppendLine($"TimestampUtc: {DateTime.UtcNow:O}");
            builder.AppendLine($"ProcessId: {Environment.ProcessId}");
            builder.AppendLine($"ProcessArch: {RuntimeInformation.ProcessArchitecture}");
            builder.AppendLine($"OS: {RuntimeInformation.OSDescription}");
            builder.AppendLine($"BaseDirectory: {AppContext.BaseDirectory}");
            builder.AppendLine();
            builder.AppendLine(exception.ToString());
            return builder.ToString();
        }

        private static string? TryWriteLogFile(string logName, string contents)
        {
            var locations = new[]
            {
                AppContext.BaseDirectory,
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "TopSpeed", "Logs")
            };

            for (var i = 0; i < locations.Length; i++)
            {
                var location = locations[i];
                if (string.IsNullOrWhiteSpace(location))
                    continue;

                try
                {
                    Directory.CreateDirectory(location);
                    var path = Path.Combine(location, logName);
                    File.WriteAllText(path, contents, Encoding.UTF8);
                    return path;
                }
                catch
                {
                    // Try the next fallback location.
                }
            }

            return null;
        }
    }
}


