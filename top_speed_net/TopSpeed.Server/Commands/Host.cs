using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using TopSpeed.Localization;
using TopSpeed.Protocol;
using TopSpeed.Server.Config;
using TopSpeed.Server.Logging;
using TopSpeed.Server.Network;
using TopSpeed.Server.Localization;
using TopSpeed.Server.Updates;
using TopSpeed.Server.Commands.Options;

namespace TopSpeed.Server.Commands
{
    internal sealed class CommandHost : IDisposable
    {
        private readonly RaceServer _server;
        private readonly ServerSettings _settings;
        private readonly ServerSettingsStore _settingsStore;
        private readonly Logger _logger;
        private readonly CancellationTokenSource _shutdownSource;
        private ServerUpdateRunner _updater;
        private readonly CommandRegistry _registry;
        private readonly OptionMenu _serverOptionsMenu;
        private readonly OptionMenu _featureOptionsMenu;
        private readonly OptionMenu _moderationOptionsMenu;
        private Thread? _thread;
        private bool _stopRequested;

        public CommandHost(
            RaceServer server,
            ServerSettings settings,
            ServerSettingsStore settingsStore,
            Logger logger,
            CancellationTokenSource shutdownSource,
            ServerUpdateRunner updater)
        {
            _server = server ?? throw new ArgumentNullException(nameof(server));
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _settingsStore = settingsStore ?? throw new ArgumentNullException(nameof(settingsStore));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _shutdownSource = shutdownSource ?? throw new ArgumentNullException(nameof(shutdownSource));
            _updater = updater ?? throw new ArgumentNullException(nameof(updater));
            _settings.Moderation ??= new ServerModerationSettings();
            _settings.Features ??= new ServerFeaturesSettings();
            _registry = new CommandRegistry(new[]
            {
                new CommandDefinition("help", LocalizationService.Mark("Show available server commands."), ExecuteHelp),
                new CommandDefinition("options", LocalizationService.Mark("Open server options menu."), ExecuteOptions),
                new CommandDefinition("players", LocalizationService.Mark("List connected players and protocol versions."), ExecutePlayers),
                new CommandDefinition("version", LocalizationService.Mark("Display server and protocol versions."), ExecuteVersion),
                new CommandDefinition("update", LocalizationService.Mark("Manually check for server updates."), ExecuteUpdate),
                new CommandDefinition("shutdown", LocalizationService.Mark("Shutdown the server."), ExecuteShutdown)
            });
            _featureOptionsMenu = CreateFeatureOptionsMenu();
            _moderationOptionsMenu = CreateModerationOptionsMenu();
            _serverOptionsMenu = CreateServerOptionsMenu();
        }

        public bool Start()
        {
            if (!IsInputAvailable())
            {
                var message = LocalizationService.Mark("Standard input is not available. Server commands are disabled.");
                _logger.Warning(message);
                ConsoleSink.WriteLine(message);
                return false;
            }

            ConsoleSink.WriteLine(LocalizationService.Mark("Server command interface ready. Type \"help\" to get the list of commands."));
            _thread = new Thread(RunLoop)
            {
                IsBackground = true,
                Name = "TopSpeed.Server.Commands"
            };
            _thread.Start();
            return true;
        }

        public void Dispose()
        {
            _stopRequested = true;
        }

        private void RunLoop()
        {
            while (!_stopRequested && !_shutdownSource.IsCancellationRequested)
            {
                if (!CommandInput.TryReadLine(">", out var raw))
                {
                    DisableCommands(LocalizationService.Mark("Standard input is no longer available. Server commands are disabled."));
                    return;
                }

                var input = raw.Trim();
                if (input.Length == 0)
                    continue;

                var commandName = ParseCommandName(input);
                if (!_registry.TryGet(commandName, out var command))
                {
                    ConsoleSink.WriteLineFormat(LocalizationService.Mark("Invalid command \"{0}\". Type \"help\" for the list of commands."), commandName);
                    continue;
                }

                try
                {
                    command.Execute();
                }
                catch (Exception ex)
                {
                    _logger.Error(LocalizationService.Format(
                        LocalizationService.Mark("Command '{0}' failed: {1}"),
                        command.Name,
                        ex.Message));
                    ConsoleSink.WriteLine(LocalizationService.Mark("Command failed. Check server logs for details."));
                }
            }
        }

        private void ExecuteHelp()
        {
            ConsoleSink.WriteLine(LocalizationService.Mark("Available commands:"));
            var commands = _registry.Commands;
            for (var i = 0; i < commands.Count; i++)
            {
                var command = commands[i];
                ConsoleSink.WriteLine(
                    "\"" + command.Name + "\": " + LocalizationService.Translate(command.Description));
            }
        }

        private void ExecutePlayers()
        {
            var players = _server.GetPlayersSnapshot();
            ConsoleSink.WriteLineFormat(LocalizationService.Mark("{0} players are connected:"), players.Length);
            for (var i = 0; i < players.Length; i++)
            {
                var player = players[i];
                ConsoleSink.WriteLineFormat(LocalizationService.Mark("{0}, using protocol version {1}"), player.Name, player.ProtocolVersion);
            }
        }

        private void ExecuteShutdown()
        {
            ConsoleSink.WriteLine(LocalizationService.Mark("shutting down..."));
            _server.ShutdownByHost(LocalizationService.Mark("The server will be shut down immediately by the host."));
            _stopRequested = true;
            _shutdownSource.Cancel();
        }

        private void ExecuteVersion()
        {
            ConsoleSink.WriteLineFormat(LocalizationService.Mark("Server version: {0}"), ServerUpdateConfig.CurrentVersion.ToMachineString());
            ConsoleSink.WriteLineFormat(LocalizationService.Mark("Protocol version: {0}"), ProtocolProfile.Current.ToMachineString());
            ConsoleSink.WriteLineFormat(LocalizationService.Mark("Protocol supported range: {0} to {1}"),
                ProtocolProfile.ServerSupported.MinSupported.ToMachineString(),
                ProtocolProfile.ServerSupported.MaxSupported.ToMachineString());
        }

        private void ExecuteUpdate()
        {
            if (_updater.RunInteractiveCheck())
                ExecuteShutdown();
        }

        private void ExecuteOptions()
        {
            ShowOptionsMenu(_serverOptionsMenu);
        }

        private OptionMenu CreateServerOptionsMenu()
        {
            return new OptionMenu(
                LocalizationService.Mark("Server options:"),
                new List<OptionItem>
                {
                    new OptionItem("language", LocalizationService.Mark("Language"), OptionValueType.Choice, EditLanguage, CurrentLanguageLabel),
                    new OptionItem("motd", LocalizationService.Mark("Message of the day"), OptionValueType.Text, EditMotd, () => FormatMotd(_settings.Motd)),
                    new OptionItem("server_port", LocalizationService.Mark("Server port"), OptionValueType.Numeric, EditServerPort, () => _settings.Port.ToString()),
                    new OptionItem("discovery_port", LocalizationService.Mark("Discovery port"), OptionValueType.Numeric, EditDiscoveryPort, () => _settings.DiscoveryPort.ToString()),
                    new OptionItem("max_players", LocalizationService.Mark("Max players"), OptionValueType.Numeric, EditMaxPlayers, () => _settings.MaxPlayers.ToString()),
                    new OptionItem("features", LocalizationService.Mark("Features"), OptionValueType.Menu, () => ShowOptionsMenu(_featureOptionsMenu)),
                    new OptionItem("server_architecture", LocalizationService.Mark("Server architecture"), OptionValueType.Choice, EditRuntimeArchitecture, CurrentRuntimeAssetLabel),
                    new OptionItem("check_updates_on_startup", LocalizationService.Mark("Check for updates on startup"), OptionValueType.Bool, ToggleCheckForUpdatesOnStartup, () => CommandInput.FormatOnOff(_settings.CheckForUpdatesOnStartup)),
                    new OptionItem("moderation", LocalizationService.Mark("Moderation"), OptionValueType.Menu, () => ShowOptionsMenu(_moderationOptionsMenu))
                });
        }

        private OptionMenu CreateFeatureOptionsMenu()
        {
            return new OptionMenu(
                LocalizationService.Mark("Feature options:"),
                new List<OptionItem>
                {
                    new OptionItem("custom_tracks", LocalizationService.Mark("Custom tracks"), OptionValueType.Bool, ToggleCustomTracks, () => CommandInput.FormatOnOff(_settings.Features.CustomTracks)),
                    new OptionItem("text_chat", LocalizationService.Mark("Text chat"), OptionValueType.Bool, ToggleTextChat, () => CommandInput.FormatOnOff(_settings.Features.TextChat)),
                    new OptionItem("voice_chat", LocalizationService.Mark("Voice chat"), OptionValueType.Bool, ToggleVoiceChat, () => CommandInput.FormatOnOff(_settings.Features.VoiceChat))
                });
        }

        private OptionMenu CreateModerationOptionsMenu()
        {
            return new OptionMenu(
                LocalizationService.Mark("Moderation options:"),
                new List<OptionItem>
                {
                    new OptionItem("block_repeated_letters_in_name", LocalizationService.Mark("Block repeated letters in call signs"), OptionValueType.Bool, ToggleBlockRepeatedLettersInName, () => CommandInput.FormatOnOff(_settings.Moderation.BlockRepeatedLettersInName)),
                    new OptionItem("max_name_length", LocalizationService.Mark("Maximum call sign length"), OptionValueType.Numeric, EditModerationMaxNameLength, () => _settings.Moderation.MaxNameLength.ToString()),
                    new OptionItem("allow_duplicate_names", LocalizationService.Mark("Allow duplicate call signs"), OptionValueType.Bool, ToggleAllowDuplicateNames, () => CommandInput.FormatOnOff(_settings.Moderation.AllowDuplicateNames))
                });
        }

        private void ShowOptionsMenu(OptionMenu menu)
        {
            if (menu == null)
                return;

            while (!_stopRequested && !_shutdownSource.IsCancellationRequested)
            {
                var options = BuildOptionMenuEntries(menu);
                var backIndex = menu.Items.Count;
                if (!CommandInput.TryPromptMenuChoice(menu.TitleMessageId, options, out var choiceIndex, backOptionIndex: backIndex))
                {
                    DisableCommands(LocalizationService.Mark("Standard input is no longer available. Server commands are disabled."));
                    return;
                }

                if (choiceIndex == backIndex || choiceIndex < 0 || choiceIndex >= menu.Items.Count)
                    return;

                menu.Items[choiceIndex].Activate();
            }
        }

        private static IReadOnlyList<string> BuildOptionMenuEntries(OptionMenu menu)
        {
            var entries = new List<string>(menu.Items.Count + 1);
            for (var i = 0; i < menu.Items.Count; i++)
            {
                var item = menu.Items[i];
                var label = LocalizationService.Translate(item.LabelMessageId);
                if (item.Type == OptionValueType.Menu)
                {
                    entries.Add(label);
                    continue;
                }

                entries.Add(label + ": " + item.GetValueOrEmpty());
            }

            entries.Add(LocalizationService.Translate(LocalizationService.Mark("Back")));
            return entries;
        }

        private string CurrentLanguageLabel()
        {
            var languages = ServerLanguages.Load();
            return ServerLanguages.ResolveDisplayLabel(_settings.Language, languages);
        }

        private string CurrentRuntimeAssetLabel()
        {
            return ServerUpdateConfig.ResolveCurrentRuntimeLabel(_settings.UpdateRuntimeAssetTag);
        }

        private void EditLanguage()
        {
            var languages = ServerLanguages.Load();
            if (languages.Count == 0)
            {
                ConsoleSink.WriteLine(LocalizationService.Mark("No languages are available."));
                return;
            }

            var options = new List<string>(languages.Count + 1);
            for (var i = 0; i < languages.Count; i++)
                options.Add(languages[i].ListLabel);
            options.Add(LocalizationService.Translate(LocalizationService.Mark("Back")));

            if (!CommandInput.TryPromptMenuChoice(LocalizationService.Mark("Choose server language:"), options, out var choiceIndex, backOptionIndex: options.Count - 1))
            {
                DisableCommands(LocalizationService.Mark("Standard input is no longer available. Server commands are disabled."));
                return;
            }

            if (choiceIndex < 0 || choiceIndex >= languages.Count)
                return;

            var selected = languages[choiceIndex];
            var resolvedCode = ServerLanguages.ResolveCode(selected.Code, languages);
            var changed = !string.Equals(_settings.Language, resolvedCode, StringComparison.OrdinalIgnoreCase);
            _settings.Language = resolvedCode;
            LocalizationBootstrap.Configure(_settings.Language, LocalizationBootstrap.ServerCatalogGroup);
            SaveSettings();

            if (changed)
            {
                ConsoleSink.WriteLineFormat(LocalizationService.Mark("Server language set to {0}."), selected.ListLabel);
                return;
            }

            ConsoleSink.WriteLineFormat(LocalizationService.Mark("Server language remains {0}."), selected.ListLabel);
        }

        private void EditRuntimeArchitecture()
        {
            var runtimeOptions = ServerUpdateConfig.GetRuntimeOptions();
            var options = new List<string>(runtimeOptions.Count + 1);
            for (var i = 0; i < runtimeOptions.Count; i++)
                options.Add(ServerUpdateConfig.FormatRuntimeOptionLabel(runtimeOptions[i]));
            options.Add(LocalizationService.Translate(LocalizationService.Mark("Back")));

            if (!CommandInput.TryPromptMenuChoice(LocalizationService.Mark("Choose server architecture:"), options, out var choiceIndex, backOptionIndex: options.Count - 1))
            {
                DisableCommands(LocalizationService.Mark("Standard input is no longer available. Server commands are disabled."));
                return;
            }

            if (choiceIndex < 0 || choiceIndex >= runtimeOptions.Count)
                return;

            var selected = runtimeOptions[choiceIndex];
            var changed = !string.Equals(_settings.UpdateRuntimeAssetTag, selected.ShortName, StringComparison.OrdinalIgnoreCase);
            _settings.UpdateRuntimeAssetTag = selected.ShortName;
            SaveSettings();
            _updater = new ServerUpdateRunner(ServerUpdateConfig.Create(_settings.UpdateRuntimeAssetTag), _logger);

            if (changed)
            {
                ConsoleSink.WriteLine(LocalizationService.Format(
                    LocalizationService.Mark("Server architecture set to {0}."),
                    ServerUpdateConfig.FormatRuntimeOptionLabel(selected)));
                return;
            }

            ConsoleSink.WriteLine(LocalizationService.Format(
                LocalizationService.Mark("Server architecture remains {0}."),
                ServerUpdateConfig.FormatRuntimeOptionLabel(selected)));
        }

        private void EditMotd()
        {
            var prompt = LocalizationService.Format(
                LocalizationService.Mark("Enter message of the day (max {0} chars, empty clears value):"),
                ProtocolConstants.MaxMotdLength);
            if (!CommandInput.TryPromptText(prompt, ProtocolConstants.MaxMotdLength, allowEmpty: true, out var motd))
            {
                DisableCommands(LocalizationService.Mark("Standard input is no longer available. Server commands are disabled."));
                return;
            }

            _settings.Motd = motd;
            _server.SetMotd(motd);
            SaveSettings();
            ConsoleSink.WriteLine(LocalizationService.Mark("Message of the day updated."));
        }

        private void EditServerPort()
        {
            if (!CommandInput.TryPromptInt(LocalizationService.Mark("Enter server port (1-65535):"), 1, 65535, out var port))
            {
                DisableCommands(LocalizationService.Mark("Standard input is no longer available. Server commands are disabled."));
                return;
            }

            _settings.Port = port;
            SaveSettings();
            ConsoleSink.WriteLineFormat(LocalizationService.Mark("Server port updated to {0}. Restart required for this change."), port);
        }

        private void EditDiscoveryPort()
        {
            if (!CommandInput.TryPromptInt(LocalizationService.Mark("Enter discovery port (1-65535):"), 1, 65535, out var port))
            {
                DisableCommands(LocalizationService.Mark("Standard input is no longer available. Server commands are disabled."));
                return;
            }

            _settings.DiscoveryPort = port;
            SaveSettings();
            ConsoleSink.WriteLineFormat(LocalizationService.Mark("Discovery port updated to {0}. Restart required for this change."), port);
        }

        private void EditMaxPlayers()
        {
            if (!CommandInput.TryPromptInt(LocalizationService.Mark("Enter max players (1-255):"), 1, byte.MaxValue, out var maxPlayers))
            {
                DisableCommands(LocalizationService.Mark("Standard input is no longer available. Server commands are disabled."));
                return;
            }

            _settings.MaxPlayers = maxPlayers;
            _server.SetMaxPlayers(maxPlayers);
            SaveSettings();
            ConsoleSink.WriteLineFormat(LocalizationService.Mark("Max players updated to {0}."), maxPlayers);
        }

        private void ToggleCustomTracks()
        {
            _settings.Features.CustomTracks = !_settings.Features.CustomTracks;
            ApplyFeatureSettings();
            ConsoleSink.WriteLine(BuildOptionLine(LocalizationService.Mark("Custom tracks"), CommandInput.FormatOnOff(_settings.Features.CustomTracks)));
        }

        private void ToggleCheckForUpdatesOnStartup()
        {
            _settings.CheckForUpdatesOnStartup = !_settings.CheckForUpdatesOnStartup;
            SaveSettings();
            ConsoleSink.WriteLine(BuildOptionLine(LocalizationService.Mark("Check for updates on startup"), CommandInput.FormatOnOff(_settings.CheckForUpdatesOnStartup)));
        }

        private void ToggleBlockRepeatedLettersInName()
        {
            _settings.Moderation.BlockRepeatedLettersInName = !_settings.Moderation.BlockRepeatedLettersInName;
            ApplyModerationSettings();
            ConsoleSink.WriteLine(BuildOptionLine(LocalizationService.Mark("Block repeated letters in call signs"), CommandInput.FormatOnOff(_settings.Moderation.BlockRepeatedLettersInName)));
        }

        private void EditModerationMaxNameLength()
        {
            if (!CommandInput.TryPromptInt(
                    LocalizationService.Format(LocalizationService.Mark("Enter max call sign length (1-{0}):"), ProtocolConstants.MaxPlayerNameLength),
                    1,
                    ProtocolConstants.MaxPlayerNameLength,
                    out var maxNameLength))
            {
                DisableCommands(LocalizationService.Mark("Standard input is no longer available. Server commands are disabled."));
                return;
            }

            _settings.Moderation.MaxNameLength = maxNameLength;
            ApplyModerationSettings();
            ConsoleSink.WriteLineFormat(LocalizationService.Mark("Maximum call sign length updated to {0}."), maxNameLength);
        }

        private void ToggleAllowDuplicateNames()
        {
            _settings.Moderation.AllowDuplicateNames = !_settings.Moderation.AllowDuplicateNames;
            ApplyModerationSettings();
            ConsoleSink.WriteLine(BuildOptionLine(LocalizationService.Mark("Allow duplicate call signs"), CommandInput.FormatOnOff(_settings.Moderation.AllowDuplicateNames)));
        }

        private void ToggleTextChat()
        {
            _settings.Features.TextChat = !_settings.Features.TextChat;
            ApplyFeatureSettings();
            ConsoleSink.WriteLine(BuildOptionLine(LocalizationService.Mark("Text chat"), CommandInput.FormatOnOff(_settings.Features.TextChat)));
        }

        private void ToggleVoiceChat()
        {
            _settings.Features.VoiceChat = !_settings.Features.VoiceChat;
            ApplyFeatureSettings();
            ConsoleSink.WriteLine(BuildOptionLine(LocalizationService.Mark("Voice chat"), CommandInput.FormatOnOff(_settings.Features.VoiceChat)));
        }

        private void ApplyFeatureSettings()
        {
            _server.SetFeatureSettings(_settings.Features);
            SaveSettings();
        }

        private void ApplyModerationSettings()
        {
            _server.SetModerationSettings(_settings.Moderation);
            SaveSettings();
        }

        private void SaveSettings()
        {
            _settingsStore.Save(_settings, _logger);
        }

        private void DisableCommands(string message)
        {
            _stopRequested = true;
            _logger.Warning(message);
            ConsoleSink.WriteLine(message);
        }

        private static string ParseCommandName(string input)
        {
            var index = input.IndexOf(' ');
            if (index < 0)
                return input.Trim();
            return input.Substring(0, index).Trim();
        }

        private static string FormatMotd(string motd)
        {
            return string.IsNullOrWhiteSpace(motd)
                ? LocalizationService.Translate(LocalizationService.Mark("(empty)"))
                : motd;
        }

        private static bool IsInputAvailable()
        {
            if (Console.IsInputRedirected)
                return true;

            try
            {
                _ = Console.KeyAvailable;
                return true;
            }
            catch (InvalidOperationException)
            {
                return false;
            }
            catch (IOException)
            {
                return false;
            }
        }

        private static string BuildOptionLine(string labelMessageId, string value)
        {
            var label = LocalizationService.Translate(labelMessageId);
            var safeValue = value ?? string.Empty;
            return label + ": " + safeValue;
        }
    }
}




