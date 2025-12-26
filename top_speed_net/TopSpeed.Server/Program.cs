using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Threading;
using TopSpeed.Server.Config;
using TopSpeed.Server.Logging;
using TopSpeed.Server.Network;
using TopSpeed.Protocol;

namespace TopSpeed.Server
{
    internal static class Program
    {
        private static int Main(string[] args)
        {
            if (IsHelpRequested(args))
            {
                ShowHelp();
                return 0;
            }

            var loggingEnabled = args.Length > 0;
            var levels = loggingEnabled ? ParseLogLevels(args) : LogLevel.None;
            var logFile = loggingEnabled ? BuildLogFilePath() : null;
            using var logger = new Logger(levels, logFile, writeToConsole: loggingEnabled);
            if (loggingEnabled)
            {
                logger.InfoAlways($"Logging enabled. Levels: {FormatLogLevels(levels)}.");
                logger.Info("TopSpeed.Server starting.");
            }

            var settingsPath = Path.Combine(AppContext.BaseDirectory, "settings.json");
            var store = new ServerSettingsStore(settingsPath);
            var settings = store.LoadOrCreate(logger);
            ApplyArgumentOverrides(settings, args, logger);
            store.Save(settings, logger);

            var config = new RaceServerConfig
            {
                Port = settings.Port,
                DiscoveryPort = settings.DiscoveryPort,
                MaxPlayers = settings.MaxPlayers,
                ServerNumber = settings.ServerNumber,
                Name = settings.Name,
                Motd = settings.Motd,
                EnableDiscovery = settings.EnableDiscovery
            };

            using var server = new RaceServer(config, logger);
            using var discovery = new ServerDiscoveryService(server, config, logger);
            using var cts = new CancellationTokenSource();
            Console.CancelKeyPress += (_, e) =>
            {
                e.Cancel = true;
                cts.Cancel();
            };

            server.Start();
            if (config.EnableDiscovery)
                discovery.Start();
            RunLoop(server, cts.Token);
            discovery.Stop();
            server.Stop();

            if (loggingEnabled)
                logger.Info("TopSpeed.Server stopped.");
            return 0;
        }

        private static void RunLoop(RaceServer server, CancellationToken token)
        {
            var stopwatch = Stopwatch.StartNew();
            var last = stopwatch.Elapsed;
            while (!token.IsCancellationRequested)
            {
                var now = stopwatch.Elapsed;
                var deltaSeconds = (float)(now - last).TotalSeconds;
                last = now;
                server.Update(deltaSeconds);
                Thread.Sleep(1);
            }
        }

        private static LogLevel ParseLogLevels(string[] args)
        {
            var value = GetArgumentValue(args, "--log");
            if (string.IsNullOrWhiteSpace(value))
                return LogLevel.Error | LogLevel.Warning | LogLevel.Info;

            var levels = LogLevel.None;
            var parts = value.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var part in parts)
            {
                var token = part.Trim().ToLowerInvariant();
                switch (token)
                {
                    case "error":
                        levels |= LogLevel.Error;
                        break;
                    case "warning":
                        levels |= LogLevel.Warning;
                        break;
                    case "info":
                        levels |= LogLevel.Info;
                        break;
                    case "debug":
                        levels |= LogLevel.Debug;
                        break;
                    case "all":
                        levels = LogLevel.All;
                        break;
                }
            }

            return levels == LogLevel.None
                ? LogLevel.Error | LogLevel.Warning | LogLevel.Info
                : levels;
        }

        private static bool IsHelpRequested(string[] args)
        {
            foreach (var arg in args)
            {
                if (string.Equals(arg, "-h", StringComparison.OrdinalIgnoreCase))
                    return true;
                if (string.Equals(arg, "--help", StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            return false;
        }

        private static void ShowHelp()
        {
            Console.WriteLine("TopSpeed.Server usage:");
            Console.WriteLine("  TopSpeed.Server [options]");
            Console.WriteLine();
            Console.WriteLine("Options:");
            Console.WriteLine("  --port <number>         Server port (1-65535).");
            Console.WriteLine("  --max-players <number>  Max players (1-8).");
            Console.WriteLine("  --motd <text>           Message of the day.");
            Console.WriteLine("  --log <levels>          Comma-separated levels: error,warning,info,debug,all.");
            Console.WriteLine("  -h, --help              Show this help.");
        }

        private static string FormatLogLevels(LogLevel levels)
        {
            if (levels == LogLevel.None)
                return "none";
            if (levels == LogLevel.All)
                return "all";

            var parts = new System.Collections.Generic.List<string>();
            if ((levels & LogLevel.Error) != 0)
                parts.Add("error");
            if ((levels & LogLevel.Warning) != 0)
                parts.Add("warning");
            if ((levels & LogLevel.Info) != 0)
                parts.Add("info");
            if ((levels & LogLevel.Debug) != 0)
                parts.Add("debug");
            return parts.Count == 0 ? "none" : string.Join(",", parts);
        }

        private static string? GetArgumentValue(string[] args, string key)
        {
            for (var i = 0; i < args.Length; i++)
            {
                var arg = args[i];
                if (string.Equals(arg, key, StringComparison.OrdinalIgnoreCase))
                {
                    if (i + 1 < args.Length)
                        return args[i + 1];
                    return null;
                }

                if (arg.StartsWith(key + "=", StringComparison.OrdinalIgnoreCase))
                    return arg.Substring(key.Length + 1);
            }

            return null;
        }

        private static string BuildLogFilePath()
        {
            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss", CultureInfo.InvariantCulture);
            var logsRoot = Path.Combine(AppContext.BaseDirectory, "Logs");
            return Path.Combine(logsRoot, $"server_{timestamp}.log");
        }

        private static void ApplyArgumentOverrides(ServerSettings settings, string[] args, Logger logger)
        {
            if (TryGetIntArg(args, "--port", out var port))
            {
                if (port >= 1 && port <= 65535)
                    settings.Port = port;
                else
                    logger.Warning("Invalid --port value. Using configured port.");
            }

            if (TryGetIntArg(args, "--max-players", out var maxPlayers))
            {
                if (maxPlayers >= 1 && maxPlayers <= ProtocolConstants.MaxPlayers)
                    settings.MaxPlayers = maxPlayers;
                else
                    logger.Warning("Invalid --max-players value. Using configured max players.");
            }

            var motd = GetArgumentValue(args, "--motd");
            if (!string.IsNullOrWhiteSpace(motd))
                settings.Motd = motd.Trim();
        }

        private static bool TryGetIntArg(string[] args, string key, out int value)
        {
            value = 0;
            var raw = GetArgumentValue(args, key);
            if (string.IsNullOrWhiteSpace(raw))
                return false;
            return int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out value);
        }
    }
}
