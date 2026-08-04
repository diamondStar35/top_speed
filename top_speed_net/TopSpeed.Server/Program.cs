using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using TopSpeed.Localization;
using TopSpeed.Protocol;
using TopSpeed.Server.Commands;
using TopSpeed.Server.Config;
using TopSpeed.Server.Logging;
using TopSpeed.Server.Network;
using TopSpeed.Server.Updates;

namespace TopSpeed.Server
{
    internal static partial class Program
    {
        private static int Main(string[] args)
        {
            LocalizationBootstrap.Configure("en", LocalizationBootstrap.ServerCatalogGroup);

            if (IsHelpRequested(args))
            {
                ShowHelp();
                return 0;
            }

            using var timerResolution = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                ? new WindowsTimerResolution(1)
                : null;

            var loggingEnabled = args.Length > 0;
            var levels = loggingEnabled ? ParseLogLevels(args) : LogLevel.None;
            var configuredLogFile = GetArgumentValue(args, "--log-file");
            var logFile = loggingEnabled && !string.IsNullOrWhiteSpace(configuredLogFile)
                ? BuildLogFilePath(configuredLogFile!)
                : null;
            using var logger = new Logger(levels, logFile, writeToConsole: loggingEnabled);
            var serverRelease = $"{ReleaseVersionInfo.ServerYear}.{ReleaseVersionInfo.ServerMonth}.{ReleaseVersionInfo.ServerDay} (r{ReleaseVersionInfo.ServerRevision})";
            if (loggingEnabled)
            {
                logger.Raw(LocalizationService.Format(
                    LocalizationService.Mark("Logging enabled. Levels: {0}. File: {1}."),
                    FormatLogLevels(levels),
                    string.IsNullOrWhiteSpace(logFile)
                        ? LocalizationService.Translate(LocalizationService.Mark("none"))
                        : logFile));
                logger.Raw(LocalizationService.Format(LocalizationService.Mark("Server release: {0}."), serverRelease));
                logger.Raw(LocalizationService.Format(LocalizationService.Mark("Protocol current: {0}. Supported: {1}."), ProtocolProfile.Current, ProtocolProfile.ServerSupported));
                logger.Info(LocalizationService.Mark("TopSpeed Server starting."));
            }
            else
            {
                ConsoleSink.WriteLine(LocalizationService.Mark("TopSpeed Server starting..."));
                ConsoleSink.WriteLineFormat(LocalizationService.Mark("Server release: {0}"), serverRelease);
                ConsoleSink.WriteLineFormat(LocalizationService.Mark("Protocol version: {0}"), ProtocolProfile.Current);
            }

            var settingsPath = Path.Combine(AppContext.BaseDirectory, "settings.json");
            var store = new ServerSettingsStore(settingsPath);
            var settings = store.LoadOrCreate(logger);
            LocalizationBootstrap.Configure(settings.Language, LocalizationBootstrap.ServerCatalogGroup);
            ApplyArgumentOverrides(settings, args, logger);
            store.Save(settings, logger);
            var updater = new ServerUpdateRunner(ServerUpdateConfig.Create(settings.UpdateRuntimeAssetTag), logger);
            if (settings.CheckForUpdatesOnStartup && updater.RunInteractiveCheck())
                return 0;

            var config = new RaceServerConfig
            {
                Port = settings.Port,
                DiscoveryPort = settings.DiscoveryPort,
                MaxPlayers = settings.MaxPlayers,
                Motd = settings.Motd,
                Features = settings.Features.Clone(),
                Moderation = settings.Moderation.Clone()
            };
            if (loggingEnabled)
                logger.Info(LocalizationService.Format(
                    LocalizationService.Mark("Server configuration: port={0}, discoveryPort={1}, maxPlayers={2}, features.custom_tracks={3}, features.custom_vehicles={4}, features.text_chat={5}, features.voice_chat={6}, moderation.maxNameLength={7}, moderation.blockRepeatedLettersInName={8}, moderation.allowDuplicateNames={9}."),
                    config.Port,
                    config.DiscoveryPort,
                    config.MaxPlayers,
                    config.Features.CustomTracks,
                    config.Features.CustomVehicles,
                    config.Features.TextChat,
                    config.Features.VoiceChat,
                    config.Moderation.MaxNameLength,
                    config.Moderation.BlockRepeatedLettersInName,
                    config.Moderation.AllowDuplicateNames));

            using var server = new RaceServer(config, logger);
            using var discovery = new ServerDiscoveryService(server, config, logger);
            using var cts = new CancellationTokenSource();
            using var commandHost = new CommandHost(server, settings, store, logger, cts, updater);
            Console.CancelKeyPress += (_, e) =>
            {
                e.Cancel = true;
                cts.Cancel();
            };

            server.Start();
            discovery.Start();
            commandHost.Start();
            if (!loggingEnabled)
                ConsoleSink.WriteLine(LocalizationService.Mark("Server started. Press Ctrl+C to stop."));
            RunLoop(server, cts.Token);
            discovery.Stop();
            server.Stop();
            if (loggingEnabled)
                logger.Info(LocalizationService.Mark("TopSpeed Server stopped."));
            else
                ConsoleSink.WriteLine(LocalizationService.Mark("Server stopped."));
            return 0;
        }
    }
}




