using System;
using System.IO;

namespace TopSpeed.Updater
{
    internal sealed class Log
    {
        private readonly bool _enabled;
        private readonly string _path;

        public Log(Options options)
        {
            _enabled = options.EnableLog;
            _path = Path.Combine(options.TargetDir, "updater.log");
        }

        public void Write(string message)
        {
            if (!_enabled)
                return;
            try
            {
                File.AppendAllText(_path, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] {message}{Environment.NewLine}");
            }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }
    }
}
