using System;
using System.IO;
using System.Windows.Forms;
using TopSpeed.Core;

namespace TopSpeed
{
    internal static class Program
    {
        [STAThread]
        private static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
            Application.ThreadException += (_, args) => HandleException(args.Exception);
            AppDomain.CurrentDomain.UnhandledException += (_, args) =>
                HandleException(args.ExceptionObject as Exception ?? new Exception("Unknown exception."));

            using (var app = new GameApp())
            {
                app.Run();
            }
        }

        private static void HandleException(Exception exception)
        {
            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            var logName = $"topspeed_error_{timestamp}.log";
            try
            {
                var path = Path.Combine(Directory.GetCurrentDirectory(), logName);
                File.WriteAllText(path, exception.ToString());
            }
            catch
            {
                // Ignore logging failures.
            }

            try
            {
                MessageBox.Show(
                    $"An unexpected error occurred. A log file was created: {logName}",
                    "Top Speed",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
            catch
            {
                // Ignore UI failures.
            }
        }
    }
}
