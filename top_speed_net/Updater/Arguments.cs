using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace TopSpeed.Updater
{
    internal static class Arguments
    {
        public static Options Parse(string[] args)
        {
            var options = new Options();
            for (var i = 0; i < args.Length; i++)
            {
                var key = args[i];
                if (key == "--log")
                    options.EnableLog = true;
                else if (key == "--finalize")
                    options.Finalize = true;
                else
                {
                    if (++i >= args.Length)
                        throw new ArgumentException("Missing value for " + key);
                    var value = args[i];
                    switch (key)
                    {
                        case "--pid": options.ProcessId = ParseProcessId(value); break;
                        case "--zip": options.ZipPath = Path.GetFullPath(value); break;
                        case "--dir": options.TargetDir = Path.GetFullPath(value); break;
                        case "--game": options.GameName = value; break;
                        case "--skip": options.SkipName = value; break;
                        case "--worker-pid": options.WorkerProcessId = ParseProcessId(value); break;
                        case "--worker-dir": options.WorkerDirectory = Path.GetFullPath(value); break;
                        case "--error": options.Error = value; break;
                        default: throw new ArgumentException("Unknown updater argument: " + key);
                    }
                }
            }
            if (options.ProcessId <= 0 || string.IsNullOrWhiteSpace(options.TargetDir)
                || string.IsNullOrWhiteSpace(options.GameName))
                throw new ArgumentException("The --pid, --dir and --game arguments are required.");
            if (Path.GetFileName(options.GameName) != options.GameName)
                throw new ArgumentException("The game name must be an executable stem, not a path.");
            if (!Directory.Exists(options.TargetDir))
                throw new DirectoryNotFoundException("The installation directory was not found.");
            if (options.Finalize)
            {
                if (!Platform.IsWindows || string.IsNullOrWhiteSpace(options.WorkerDirectory))
                    throw new ArgumentException("Finalization requires a Windows worker directory.");
            }
            else if (string.IsNullOrWhiteSpace(options.ZipPath)
                || (options.WorkerProcessId == 0 && !File.Exists(options.ZipPath)))
                throw new FileNotFoundException("The update archive was not found.", options.ZipPath);
            return options;
        }

        public static List<string> Build(Options options)
        {
            var args = new List<string>
            {
                "--pid", options.ProcessId.ToString(CultureInfo.InvariantCulture),
                "--dir", options.TargetDir, "--game", options.GameName
            };
            if (options.Finalize)
            {
                args.Add("--finalize");
                args.Add("--worker-dir");
                args.Add(options.WorkerDirectory);
                if (options.Error.Length > 0)
                {
                    args.Add("--error");
                    args.Add(options.Error);
                }
            }
            else
            {
                args.Add("--zip");
                args.Add(options.ZipPath);
                if (options.SkipName.Length > 0)
                {
                    args.Add("--skip");
                    args.Add(options.SkipName);
                }
                if (options.WorkerProcessId > 0)
                {
                    args.Add("--worker-pid");
                    args.Add(options.WorkerProcessId.ToString(CultureInfo.InvariantCulture));
                }
            }
            if (options.EnableLog)
                args.Add("--log");
            return args;
        }

        private static int ParseProcessId(string value)
        {
            if (!int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out var id) || id <= 0)
                throw new ArgumentException("Invalid process ID: " + value);
            return id;
        }
    }
}
