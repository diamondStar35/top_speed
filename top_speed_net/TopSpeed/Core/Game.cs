using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SharpDX.DirectInput;
using TopSpeed.Audio;
using TopSpeed.Common;
using TopSpeed.Data;
using TopSpeed.Input;
using TopSpeed.Menu;
using TopSpeed.Network;
using TopSpeed.Protocol;
using TopSpeed.Race;
using TopSpeed.Speech;
using TopSpeed.Windowing;

namespace TopSpeed.Core
{
    internal sealed class Game : IDisposable
    {
        private enum AppState
        {
            Logo,
            Menu,
            TimeTrial,
            SingleRace,
            MultiplayerRace,
            Paused
        }

        private enum InputMappingMode
        {
            Keyboard,
            Joystick
        }

        private enum MappingAction
        {
            SteerLeft,
            SteerRight,
            Throttle,
            Brake,
            GearUp,
            GearDown,
            Horn,
            RequestInfo,
            CurrentGear,
            CurrentLapNr,
            CurrentRacePerc,
            CurrentLapPerc,
            CurrentRaceTime
        }

        private readonly struct TrackInfo
        {
            public TrackInfo(string key, string display, string soundFile)
            {
                Key = key;
                Display = display;
                SoundFile = soundFile;
            }

            public string Key { get; }
            public string Display { get; }
            public string SoundFile { get; }
        }

        private static readonly TrackInfo[] RaceTracks =
        {
            new TrackInfo("america", "America", Path.Combine("Tracks", "america.ogg")),
            new TrackInfo("austria", "Austria", Path.Combine("Tracks", "austria.ogg")),
            new TrackInfo("belgium", "Belgium", Path.Combine("Tracks", "belgium.ogg")),
            new TrackInfo("brazil", "Brazil", Path.Combine("Tracks", "brazil.ogg")),
            new TrackInfo("china", "China", Path.Combine("Tracks", "china.ogg")),
            new TrackInfo("england", "England", Path.Combine("Tracks", "england.ogg")),
            new TrackInfo("finland", "Finland", Path.Combine("Tracks", "finland.ogg")),
            new TrackInfo("france", "France", Path.Combine("Tracks", "france.ogg")),
            new TrackInfo("germany", "Germany", Path.Combine("Tracks", "germany.ogg")),
            new TrackInfo("ireland", "Ireland", Path.Combine("Tracks", "ireland.ogg")),
            new TrackInfo("italy", "Italy", Path.Combine("Tracks", "italy.ogg")),
            new TrackInfo("netherlands", "Netherlands", Path.Combine("Tracks", "netherlands.ogg")),
            new TrackInfo("portugal", "Portugal", Path.Combine("Tracks", "portugal.ogg")),
            new TrackInfo("russia", "Russia", Path.Combine("Tracks", "russia.ogg")),
            new TrackInfo("spain", "Spain", Path.Combine("Tracks", "spain.ogg")),
            new TrackInfo("sweden", "Sweden", Path.Combine("Tracks", "sweden.ogg")),
            new TrackInfo("switserland", "Switserland", Path.Combine("Tracks", "switserland.ogg"))
        };

        private static readonly TrackInfo[] AdventureTracks =
        {
            new TrackInfo("advHills", "Rally hills", Path.Combine("Tracks", "rallyhills.ogg")),
            new TrackInfo("advCoast", "French coast", Path.Combine("Tracks", "frenchcoast.ogg")),
            new TrackInfo("advCountry", "English country", Path.Combine("Tracks", "englishcountry.ogg")),
            new TrackInfo("advAirport", "Ride airport", Path.Combine("Tracks", "rideairport.ogg")),
            new TrackInfo("advDesert", "Rally desert", Path.Combine("Tracks", "rallydesert.ogg")),
            new TrackInfo("advRush", "Rush hour", Path.Combine("Tracks", "rushhour.ogg")),
            new TrackInfo("advEscape", "Polar escape", Path.Combine("Tracks", "polarescape.ogg"))
        };

        private readonly GameWindow _window;
        private readonly AudioManager _audio;
        private readonly SpeechService _speech;
        private readonly InputManager _input;
        private readonly MenuManager _menu;
        private readonly RaceSettings _settings;
        private readonly RaceInput _raceInput;
        private readonly RaceSetup _setup;
        private readonly SettingsManager _settingsManager;
        private readonly MultiplayerConnector _connector = new MultiplayerConnector();
        private MultiplayerSession? _session;
        private bool _mappingActive;
        private InputMappingMode _mappingMode;
        private MappingAction _mappingAction;
        private bool _mappingNeedsInstruction;
        private JoystickStateSnapshot _mappingPrevJoystick;
        private bool _mappingHasPrevJoystick;
        private LogoScreen? _logo;
        private AppState _state;
        private AppState _pausedState;
        private bool _pendingRaceStart;
        private RaceMode _pendingMode;
        private bool _pauseKeyReleased = true;
        private LevelTimeTrial? _timeTrial;
        private LevelSingleRace? _singleRace;
        private LevelMultiplayer? _multiplayerRace;
        private bool _textInputActive;
        private Action<string>? _textInputHandler;
        private Action? _textInputCancelled;
        private Task<IReadOnlyList<ServerInfo>>? _discoveryTask;
        private CancellationTokenSource? _discoveryCts;
        private Task<ConnectResult>? _connectTask;
        private CancellationTokenSource? _connectCts;
        private ServerInfo? _pendingServer;
        private string _pendingServerAddress = string.Empty;
        private int _pendingServerPort;
        private string _pendingCallSign = string.Empty;
        private TrackData? _pendingMultiplayerTrack;
        private string _pendingMultiplayerTrackName = string.Empty;
        private int _pendingMultiplayerLaps;
        private bool _pendingMultiplayerStart;

        public event Action? ExitRequested;

        public Game(GameWindow window)
        {
            _window = window ?? throw new ArgumentNullException(nameof(window));
            _settingsManager = new SettingsManager();
            _settings = _settingsManager.Load();
            _audio = new AudioManager(_settings.ThreeDSound);
            _speech = new SpeechService();
            _input = new InputManager(_window.Handle);
            _raceInput = new RaceInput(_settings);
            _setup = new RaceSetup();
            _menu = new MenuManager(_audio, _speech);
            RegisterMenus();
        }

        public void Initialize()
        {
            _logo = new LogoScreen(_audio);
            _logo.Start();
            _state = AppState.Logo;
        }

        public void Update(float deltaSeconds)
        {
            _input.Update();
            if (_input.TryGetJoystickState(out var joystick))
                _raceInput.Run(_input.Current, joystick);
            else
                _raceInput.Run(_input.Current);

            switch (_state)
            {
                case AppState.Logo:
                    if (_logo == null || _logo.Update(_input, deltaSeconds))
                    {
                        _logo?.Dispose();
                        _logo = null;
                        _menu.ShowRoot("main");
                        _speech.Speak("Main menu", interrupt: true);
                        _state = AppState.Menu;
                    }
                    break;
                case AppState.Menu:
                    if (UpdateModalOperations())
                        break;

                    if (_session != null)
                    {
                        ProcessMultiplayerPackets();
                        if (_state != AppState.Menu)
                            break;
                    }

                    if (_mappingActive)
                    {
                        UpdateMapping();
                        break;
                    }

                    var action = _menu.Update(_input);
                    HandleMenuAction(action);
                    break;
                case AppState.TimeTrial:
                    RunTimeTrial(deltaSeconds);
                    break;
                case AppState.SingleRace:
                    RunSingleRace(deltaSeconds);
                    break;
                case AppState.MultiplayerRace:
                    RunMultiplayerRace(deltaSeconds);
                    break;
                case AppState.Paused:
                    UpdatePaused();
                    break;
            }

            if (_pendingRaceStart)
            {
                _pendingRaceStart = false;
                StartRace(_pendingMode);
            }

            _audio.Update();
        }

        private void HandleMenuAction(MenuAction action)
        {
            switch (action)
            {
                case MenuAction.Exit:
                    ExitRequested?.Invoke();
                    break;
                case MenuAction.QuickStart:
                    PrepareQuickStart();
                    QueueRaceStart(RaceMode.QuickStart);
                    break;
                default:
                    break;
            }
        }

        private void RegisterMenus()
        {
            var mainMenu = _menu.CreateMenu("main", new[]
            {
                new MenuItem("Quick start", MenuAction.QuickStart, "quickstart.ogg"),
                new MenuItem("Time trial", MenuAction.None, "timetrial.ogg", nextMenuId: "time_trial_type", onActivate: () => PrepareMode(RaceMode.TimeTrial)),
                new MenuItem("Single race", MenuAction.None, "singlerace.ogg", nextMenuId: "single_race_type", onActivate: () => PrepareMode(RaceMode.SingleRace)),
                new MenuItem("MultiPlayer game", MenuAction.None, "multiplayergame.ogg", nextMenuId: "multiplayer"),
                new MenuItem("Options", MenuAction.None, "options.ogg", nextMenuId: "options_main"),
                new MenuItem("Exit Game", MenuAction.Exit, "exitgame.ogg")
            }, "Main menu");
            mainMenu.MusicFile = "theme1.ogg";
            mainMenu.MusicVolume = _settings.MusicVolume;
            mainMenu.MusicVolumeChanged = SaveMusicVolume;
            _menu.Register(mainMenu);

            _menu.Register(BuildMultiplayerMenu());
            _menu.Register(BuildMultiplayerServersMenu());
            _menu.Register(BuildMultiplayerLobbyMenu());

            _menu.Register(BuildTrackTypeMenu("time_trial_type", RaceMode.TimeTrial));
            _menu.Register(BuildTrackTypeMenu("single_race_type", RaceMode.SingleRace));

            _menu.Register(BuildTrackMenu("time_trial_tracks_race", RaceMode.TimeTrial, TrackCategory.RaceTrack));
            _menu.Register(BuildTrackMenu("time_trial_tracks_adventure", RaceMode.TimeTrial, TrackCategory.StreetAdventure));
            _menu.Register(BuildTrackMenu("single_race_tracks_race", RaceMode.SingleRace, TrackCategory.RaceTrack));
            _menu.Register(BuildTrackMenu("single_race_tracks_adventure", RaceMode.SingleRace, TrackCategory.StreetAdventure));

            _menu.Register(BuildVehicleMenu("time_trial_vehicles", RaceMode.TimeTrial));
            _menu.Register(BuildVehicleMenu("single_race_vehicles", RaceMode.SingleRace));

            _menu.Register(BuildTransmissionMenu("time_trial_transmission", RaceMode.TimeTrial));
            _menu.Register(BuildTransmissionMenu("single_race_transmission", RaceMode.SingleRace));

            _menu.Register(BuildOptionsMenu());
            _menu.Register(BuildOptionsGameSettingsMenu());
            _menu.Register(BuildOptionsControlsMenu());
            _menu.Register(BuildOptionsControlsDeviceMenu());
            _menu.Register(BuildOptionsControlsKeyboardMenu());
            _menu.Register(BuildOptionsControlsJoystickMenu());
            _menu.Register(BuildOptionsRaceSettingsMenu());
            _menu.Register(BuildOptionsAutomaticInfoMenu());
            _menu.Register(BuildOptionsCopilotMenu());
            _menu.Register(BuildOptionsLapsMenu());
            _menu.Register(BuildOptionsComputersMenu());
            _menu.Register(BuildOptionsDifficultyMenu());
            _menu.Register(BuildOptionsRestoreMenu());
            _menu.Register(BuildOptionsServerSettingsMenu());
        }

        private MenuScreen BuildTrackTypeMenu(string id, RaceMode mode)
        {
            var items = new List<MenuItem>
            {
                new MenuItem("Race track", MenuAction.None, "racetrack.ogg", nextMenuId: TrackMenuId(mode, TrackCategory.RaceTrack), onActivate: () => _setup.TrackCategory = TrackCategory.RaceTrack),
                new MenuItem("Street adventure", MenuAction.None, "streetadventure.ogg", nextMenuId: TrackMenuId(mode, TrackCategory.StreetAdventure), onActivate: () => _setup.TrackCategory = TrackCategory.StreetAdventure),
                new MenuItem("Random", MenuAction.None, "random.ogg", onActivate: () => PushRandomTrackType(mode)),
                BackItem()
            };
            var title = "Choose track type";
            return _menu.CreateMenu(id, items, title);
        }

        private MenuScreen BuildMultiplayerMenu()
        {
            var items = new List<MenuItem>
            {
                new MenuItem("Join a game on the local network", MenuAction.None, null, onActivate: StartServerDiscovery, suppressPostActivateAnnouncement: true),
                new MenuItem("Enter the IP address or domain manually", MenuAction.None, null, onActivate: BeginManualServerEntry, suppressPostActivateAnnouncement: true),
                BackItem()
            };
            return _menu.CreateMenu("multiplayer", items, "Multiplayer");
        }

        private MenuScreen BuildMultiplayerServersMenu()
        {
            var items = new List<MenuItem>
            {
                BackItem()
            };
            return _menu.CreateMenu("multiplayer_servers", items, "Available servers");
        }

        private MenuScreen BuildMultiplayerLobbyMenu()
        {
            var items = new List<MenuItem>
            {
                new MenuItem("Create a new game", MenuAction.None, null, onActivate: SpeakNotImplemented),
                new MenuItem("Join an existing game", MenuAction.None, null, onActivate: SpeakNotImplemented),
                new MenuItem("Who is online", MenuAction.None, null, onActivate: SpeakNotImplemented),
                new MenuItem("Options", MenuAction.None, null, nextMenuId: "options_main"),
                new MenuItem("Disconnect", MenuAction.None, null, onActivate: DisconnectFromServer)
            };
            return _menu.CreateMenu("multiplayer_lobby", items, string.Empty);
        }

        private MenuScreen BuildTrackMenu(string id, RaceMode mode, TrackCategory category)
        {
            var items = new List<MenuItem>();
            var trackList = category == TrackCategory.RaceTrack ? RaceTracks : AdventureTracks;
            var nextMenuId = VehicleMenuId(mode);

            foreach (var track in trackList)
            {
                var key = track.Key;
                items.Add(new MenuItem(track.Display, MenuAction.None, track.SoundFile, nextMenuId: nextMenuId, onActivate: () => SelectTrack(category, key)));
            }

            items.Add(new MenuItem("Random", MenuAction.None, "random.ogg", nextMenuId: nextMenuId, onActivate: () => SelectRandomTrack(category)));
            items.Add(BackItem());
            var title = "Choose track";
            return _menu.CreateMenu(id, items, title);
        }

        private MenuScreen BuildVehicleMenu(string id, RaceMode mode)
        {
            var items = new List<MenuItem>();
            var nextMenuId = TransmissionMenuId(mode);

            for (var i = 0; i < VehicleCatalog.VehicleCount; i++)
            {
                var index = i;
                var name = $"Vehicle {i + 1}";
                var soundFile = Path.Combine("Vehicles", $"vehicle{i + 1}.ogg");
                items.Add(new MenuItem(name, MenuAction.None, soundFile, nextMenuId: nextMenuId, onActivate: () => SelectVehicle(index)));
            }

            foreach (var file in GetCustomVehicleFiles())
            {
                var filePath = file;
                var fileName = Path.GetFileNameWithoutExtension(filePath) ?? "Custom vehicle";
                items.Add(new MenuItem(fileName, MenuAction.None, null, nextMenuId: nextMenuId, onActivate: () => SelectCustomVehicle(filePath)));
            }

            items.Add(new MenuItem("Random", MenuAction.None, "random.ogg", nextMenuId: nextMenuId, onActivate: SelectRandomVehicle));
            items.Add(BackItem());
            var title = "Choose vehicle";
            return _menu.CreateMenu(id, items, title);
        }

        private MenuScreen BuildTransmissionMenu(string id, RaceMode mode)
        {
            var items = new List<MenuItem>
            {
                new MenuItem("Automatic", MenuAction.None, "automatictransmission.ogg", onActivate: () => CompleteTransmission(mode, TransmissionMode.Automatic)),
                new MenuItem("Manual", MenuAction.None, "manualtransmission.ogg", onActivate: () => CompleteTransmission(mode, TransmissionMode.Manual)),
                new MenuItem("Random", MenuAction.None, "random.ogg", onActivate: () => CompleteTransmission(mode, Algorithm.RandomInt(2) == 0 ? TransmissionMode.Automatic : TransmissionMode.Manual)),
                BackItem()
            };
            var title = "Choose transmission";
            return _menu.CreateMenu(id, items, title);
        }

        private MenuScreen BuildOptionsMenu()
        {
            var items = new List<MenuItem>
            {
                new MenuItem("Game settings", MenuAction.None, "gamesettings.ogg", nextMenuId: "options_game"),
                new MenuItem("Controls", MenuAction.None, "controls.ogg", nextMenuId: "options_controls"),
                new MenuItem("Race settings", MenuAction.None, "racesettings.ogg", nextMenuId: "options_race"),
                new MenuItem("Server settings", MenuAction.None, null, nextMenuId: "options_server"),
                new MenuItem("Restore default settings", MenuAction.None, "restoredefaults.ogg", nextMenuId: "options_restore"),
                BackItem()
            };
            return _menu.CreateMenu("options_main", items, "Options");
        }

        private MenuScreen BuildOptionsGameSettingsMenu()
        {
            var items = new List<MenuItem>
            {
                new MenuItem(() => $"Include custom tracks in randomization: {FormatOnOff(_settings.RandomCustomTracks)}", MenuAction.None, "randomcustomtracks.ogg", onActivate: () => ToggleSetting(() => _settings.RandomCustomTracks = !_settings.RandomCustomTracks)),
                new MenuItem(() => $"Include custom vehicles in randomization: {FormatOnOff(_settings.RandomCustomVehicles)}", MenuAction.None, "randomcustomvehicles.ogg", onActivate: () => ToggleSetting(() => _settings.RandomCustomVehicles = !_settings.RandomCustomVehicles)),
                new MenuItem(() => $"Enable Three-D sound: {FormatOnOff(_settings.ThreeDSound)}", MenuAction.None, "threed.ogg", onActivate: () => ToggleSetting(() => _settings.ThreeDSound = !_settings.ThreeDSound)),
                BackItem()
            };
            return _menu.CreateMenu("options_game", items, "Game settings");
        }

        private MenuScreen BuildOptionsServerSettingsMenu()
        {
            var items = new List<MenuItem>
            {
                new MenuItem(() => $"Custom server port: {FormatServerPort(_settings.ServerPort)}", MenuAction.None, null, onActivate: BeginServerPortEntry),
                BackItem()
            };
            return _menu.CreateMenu("options_server", items, "Server settings");
        }

        private bool UpdateModalOperations()
        {
            if (_textInputActive)
            {
                UpdateTextInput();
                return true;
            }

            if (_connectTask != null)
            {
                if (!_connectTask.IsCompleted)
                    return true;
                var result = _connectTask.IsFaulted || _connectTask.IsCanceled
                    ? ConnectResult.CreateFail("Connection attempt failed.")
                    : _connectTask.GetAwaiter().GetResult();
                _connectTask = null;
                _connectCts?.Dispose();
                _connectCts = null;
                HandleConnectResult(result);
                return false;
            }

            if (_discoveryTask != null)
            {
                if (!_discoveryTask.IsCompleted)
                    return true;
                IReadOnlyList<ServerInfo> servers;
                if (_discoveryTask.IsFaulted || _discoveryTask.IsCanceled)
                    servers = Array.Empty<ServerInfo>();
                else
                    servers = _discoveryTask.GetAwaiter().GetResult();
                _discoveryTask = null;
                _discoveryCts?.Dispose();
                _discoveryCts = null;
                HandleDiscoveryResult(servers);
                return false;
            }

            return false;
        }

        private void UpdateTextInput()
        {
            if (!_window.TryConsumeTextInput(out var result))
                return;

            _textInputActive = false;
            if (result.Cancelled)
            {
                _textInputCancelled?.Invoke();
            }
            else
            {
                _textInputHandler?.Invoke(result.Text ?? string.Empty);
            }

            if (!_textInputActive)
                _input.Resume();
        }

        private void BeginTextInput(string prompt, string? initialValue, Action<string> onSubmit, Action? onCancel = null)
        {
            _textInputHandler = onSubmit;
            _textInputCancelled = onCancel;
            _textInputActive = true;
            _input.Suspend();
            _window.ShowTextInput(initialValue);
            _speech.Speak(prompt, interrupt: true);
        }

        private void StartServerDiscovery()
        {
            if (_discoveryTask != null && !_discoveryTask.IsCompleted)
                return;

            _speech.Speak("Please wait. Scanning for servers on the local network.", interrupt: true);
            _discoveryCts?.Cancel();
            _discoveryCts?.Dispose();
            _discoveryCts = new CancellationTokenSource();
            _discoveryTask = Task.Run(async () =>
            {
                using var client = new DiscoveryClient();
                return await client.ScanAsync(ClientProtocol.DefaultDiscoveryPort, TimeSpan.FromSeconds(2), _discoveryCts.Token);
            }, _discoveryCts.Token);
        }

        private void HandleDiscoveryResult(IReadOnlyList<ServerInfo> servers)
        {
            if (servers == null || servers.Count == 0)
            {
                _speech.Speak("No servers were found on the local network. You can enter an address manually.", interrupt: true);
                return;
            }

            UpdateServerListMenu(servers);
            _menu.Push("multiplayer_servers");
        }

        private void UpdateServerListMenu(IReadOnlyList<ServerInfo> servers)
        {
            var items = new List<MenuItem>();
            foreach (var server in servers)
            {
                var info = server;
                var label = $"{info.Address}:{info.Port}";
                items.Add(new MenuItem(label, MenuAction.None, null, onActivate: () => SelectDiscoveredServer(info), suppressPostActivateAnnouncement: true));
            }
            items.Add(BackItem());
            _menu.UpdateItems("multiplayer_servers", items);
        }

        private void SelectDiscoveredServer(ServerInfo server)
        {
            _pendingServerAddress = server.Address.ToString();
            _pendingServerPort = server.Port;
            _pendingServer = server;
            BeginCallSignInput();
        }

        private void BeginManualServerEntry()
        {
            BeginTextInput("Enter the server IP address or domain.", _settings.LastServerAddress, HandleServerAddressInput);
        }

        private void SpeakNotImplemented()
        {
            _speech.Speak("Not implemented yet.", interrupt: true);
        }

        private void HandleServerAddressInput(string text)
        {
            var trimmed = (text ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(trimmed))
            {
                _speech.Speak("Please enter a server address.", interrupt: true);
                BeginManualServerEntry();
                return;
            }

            var host = trimmed;
            int? overridePort = null;
            var lastColon = trimmed.LastIndexOf(':');
            if (lastColon > 0 && lastColon < trimmed.Length - 1)
            {
                var portPart = trimmed.Substring(lastColon + 1);
                if (int.TryParse(portPart, out var parsedPort))
                {
                    host = trimmed.Substring(0, lastColon);
                    overridePort = parsedPort;
                }
            }

            _settings.LastServerAddress = host;
            SaveSettings();
            _pendingServerAddress = host;
            _pendingServerPort = overridePort ?? ResolveServerPort();
            BeginCallSignInput();
        }

        private void BeginCallSignInput()
        {
            BeginTextInput("Enter your call sign.", null, HandleCallSignInput);
        }

        private void HandleCallSignInput(string text)
        {
            var trimmed = (text ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(trimmed))
            {
                _speech.Speak("Call sign cannot be empty.", interrupt: true);
                BeginCallSignInput();
                return;
            }

            _pendingCallSign = trimmed;
            AttemptConnect(_pendingServerAddress, _pendingServerPort, _pendingCallSign);
        }

        private void DisconnectFromServer()
        {
            _multiplayerRace?.FinalizeLevelMultiplayer();
            _multiplayerRace?.Dispose();
            _multiplayerRace = null;

            _pendingMultiplayerTrack = null;
            _pendingMultiplayerTrackName = string.Empty;
            _pendingMultiplayerLaps = 0;
            _pendingMultiplayerStart = false;

            _session?.Dispose();
            _session = null;

            _state = AppState.Menu;
            _menu.ShowRoot("main");
            _speech.Speak("Main menu", interrupt: true);
        }

        private void AttemptConnect(string host, int port, string callSign)
        {
            _speech.Speak("Attempting to connect, please wait...", interrupt: true);
            _session?.Dispose();
            _session = null;
            _connectCts?.Cancel();
            _connectCts?.Dispose();
            _connectCts = new CancellationTokenSource();
            _connectTask = _connector.ConnectAsync(host, port, callSign, TimeSpan.FromSeconds(3), _connectCts.Token);
        }

        private void HandleConnectResult(ConnectResult result)
        {
            if (result.Success)
            {
                _session = result.Session;
                _pendingMultiplayerTrack = null;
                _pendingMultiplayerTrackName = string.Empty;
                _pendingMultiplayerLaps = 0;
                _pendingMultiplayerStart = false;
                _session?.SendPlayerState(PlayerState.NotReady);

                var welcome = "You are now in the lobby.";
                if (!string.IsNullOrWhiteSpace(result.Motd))
                    welcome += $" Message of the day: {result.Motd}.";
                _speech.Speak(welcome, interrupt: true);
                _menu.ShowRoot("multiplayer_lobby");
                _state = AppState.Menu;
                return;
            }

            _speech.Speak($"Failed to connect: {result.Message}", interrupt: true);
            _state = AppState.Menu;
            _menu.ShowRoot("main");
            _speech.Speak("Main menu", interrupt: true);
        }

        private void BeginServerPortEntry()
        {
            var current = _settings.ServerPort > 0 ? _settings.ServerPort.ToString() : string.Empty;
            BeginTextInput("Enter a custom server port, or leave empty for default.", current, HandleServerPortInput);
        }

        private void HandleServerPortInput(string text)
        {
            var trimmed = (text ?? string.Empty).Trim();
            if (string.IsNullOrEmpty(trimmed))
            {
                _settings.ServerPort = 0;
                SaveSettings();
                _speech.Speak("Server port cleared. The default port will be used.", interrupt: true);
                return;
            }

            if (!int.TryParse(trimmed, out var port) || port < 1 || port > 65535)
            {
                _speech.Speak("Invalid port. Enter a number between 1 and 65535.", interrupt: true);
                BeginServerPortEntry();
                return;
            }

            _settings.ServerPort = port;
            SaveSettings();
            _speech.Speak($"Server port set to {port}.", interrupt: true);
        }

        private int ResolveServerPort()
        {
            return _settings.ServerPort > 0 ? _settings.ServerPort : ClientProtocol.DefaultServerPort;
        }

        private MenuScreen BuildOptionsControlsMenu()
        {
            var items = new List<MenuItem>
            {
                new MenuItem(() => $"Select device: {DeviceLabel(_settings.DeviceMode)}", MenuAction.None, "selectdevice.ogg", nextMenuId: "options_controls_device"),
                new MenuItem(() => $"Force feedback: {FormatOnOff(_settings.ForceFeedback)}", MenuAction.None, "forcefeedback.ogg", onActivate: () => ToggleSetting(() => _settings.ForceFeedback = !_settings.ForceFeedback)),
                new MenuItem("Map keyboard keys", MenuAction.None, "assignkeyboard.ogg", nextMenuId: "options_controls_keyboard"),
                new MenuItem("Map joystick keys", MenuAction.None, "assignjoystick.ogg", nextMenuId: "options_controls_joystick"),
                BackItem()
            };
            return _menu.CreateMenu("options_controls", items, "Controls");
        }

        private MenuScreen BuildOptionsControlsDeviceMenu()
        {
            var items = new List<MenuItem>
            {
                new MenuItem("Keyboard", MenuAction.Back, "keyboard.ogg", onActivate: () => SetDevice(InputDeviceMode.Keyboard)),
                new MenuItem("Joystick", MenuAction.Back, "joystickorwheel.ogg", onActivate: () => SetDevice(InputDeviceMode.Joystick)),
                new MenuItem("Both", MenuAction.Back, null, onActivate: () => SetDevice(InputDeviceMode.Both)),
                BackItem()
            };
            return _menu.CreateMenu("options_controls_device", items, "Choose control device");
        }

        private MenuScreen BuildOptionsControlsKeyboardMenu()
        {
            var items = BuildMappingItems(InputMappingMode.Keyboard);
            return _menu.CreateMenu("options_controls_keyboard", items, "Map keyboard keys");
        }

        private MenuScreen BuildOptionsControlsJoystickMenu()
        {
            var items = BuildMappingItems(InputMappingMode.Joystick);
            return _menu.CreateMenu("options_controls_joystick", items, "Map joystick keys");
        }

        private List<MenuItem> BuildMappingItems(InputMappingMode mode)
        {
            var items = new List<MenuItem>
            {
                new MenuItem(() => $"{ActionLabel(MappingAction.SteerLeft)}: {FormatMappingValue(MappingAction.SteerLeft, mode)}", MenuAction.None, "steerleft.ogg", onActivate: () => BeginMapping(mode, MappingAction.SteerLeft)),
                new MenuItem(() => $"{ActionLabel(MappingAction.SteerRight)}: {FormatMappingValue(MappingAction.SteerRight, mode)}", MenuAction.None, "steerright.ogg", onActivate: () => BeginMapping(mode, MappingAction.SteerRight)),
                new MenuItem(() => $"{ActionLabel(MappingAction.Throttle)}: {FormatMappingValue(MappingAction.Throttle, mode)}", MenuAction.None, "throttle.ogg", onActivate: () => BeginMapping(mode, MappingAction.Throttle)),
                new MenuItem(() => $"{ActionLabel(MappingAction.Brake)}: {FormatMappingValue(MappingAction.Brake, mode)}", MenuAction.None, "brake.ogg", onActivate: () => BeginMapping(mode, MappingAction.Brake)),
                new MenuItem(() => $"{ActionLabel(MappingAction.GearUp)}: {FormatMappingValue(MappingAction.GearUp, mode)}", MenuAction.None, "shiftgearup.ogg", onActivate: () => BeginMapping(mode, MappingAction.GearUp)),
                new MenuItem(() => $"{ActionLabel(MappingAction.GearDown)}: {FormatMappingValue(MappingAction.GearDown, mode)}", MenuAction.None, "shiftgeardown.ogg", onActivate: () => BeginMapping(mode, MappingAction.GearDown)),
                new MenuItem(() => $"{ActionLabel(MappingAction.Horn)}: {FormatMappingValue(MappingAction.Horn, mode)}", MenuAction.None, "usehorn.ogg", onActivate: () => BeginMapping(mode, MappingAction.Horn)),
                new MenuItem(() => $"{ActionLabel(MappingAction.RequestInfo)}: {FormatMappingValue(MappingAction.RequestInfo, mode)}", MenuAction.None, "requestinfo.ogg", onActivate: () => BeginMapping(mode, MappingAction.RequestInfo)),
                new MenuItem(() => $"{ActionLabel(MappingAction.CurrentGear)}: {FormatMappingValue(MappingAction.CurrentGear, mode)}", MenuAction.None, "currentgear.ogg", onActivate: () => BeginMapping(mode, MappingAction.CurrentGear)),
                new MenuItem(() => $"{ActionLabel(MappingAction.CurrentLapNr)}: {FormatMappingValue(MappingAction.CurrentLapNr, mode)}", MenuAction.None, "currentlapnr.ogg", onActivate: () => BeginMapping(mode, MappingAction.CurrentLapNr)),
                new MenuItem(() => $"{ActionLabel(MappingAction.CurrentRacePerc)}: {FormatMappingValue(MappingAction.CurrentRacePerc, mode)}", MenuAction.None, "currentracepercentage.ogg", onActivate: () => BeginMapping(mode, MappingAction.CurrentRacePerc)),
                new MenuItem(() => $"{ActionLabel(MappingAction.CurrentLapPerc)}: {FormatMappingValue(MappingAction.CurrentLapPerc, mode)}", MenuAction.None, "currentlappercentage.ogg", onActivate: () => BeginMapping(mode, MappingAction.CurrentLapPerc)),
                new MenuItem(() => $"{ActionLabel(MappingAction.CurrentRaceTime)}: {FormatMappingValue(MappingAction.CurrentRaceTime, mode)}", MenuAction.None, "currentracetime.ogg", onActivate: () => BeginMapping(mode, MappingAction.CurrentRaceTime)),
                BackItem()
            };
            return items;
        }

        private MenuScreen BuildOptionsRaceSettingsMenu()
        {
            var items = new List<MenuItem>
            {
                new MenuItem(() => $"Copilot: {CopilotLabel(_settings.Copilot)}", MenuAction.None, "copilot.ogg", nextMenuId: "options_race_copilot"),
                new MenuItem(() => $"Curve announcements: {CurveLabel(_settings.CurveAnnouncement)}", MenuAction.None, "curveannouncement.ogg", onActivate: ToggleCurveAnnouncements),
                new MenuItem(() => $"Automatic race information: {AutomaticInfoLabel(_settings.AutomaticInfo)}", MenuAction.None, "automaticinformation.ogg", nextMenuId: "options_race_info"),
                new MenuItem(() => $"Number of laps: {_settings.NrOfLaps}", MenuAction.None, "nroflaps.ogg", nextMenuId: "options_race_laps"),
                new MenuItem(() => $"Number of computer players: {_settings.NrOfComputers}", MenuAction.None, "nrofcomputers.ogg", nextMenuId: "options_race_computers"),
                new MenuItem(() => $"Single race difficulty: {DifficultyLabel(_settings.Difficulty)}", MenuAction.None, "difficulty.ogg", nextMenuId: "options_race_difficulty"),
                BackItem()
            };
            return _menu.CreateMenu("options_race", items, "Race settings");    
        }

        private MenuScreen BuildOptionsAutomaticInfoMenu()
        {
            var items = new List<MenuItem>
            {
                new MenuItem("Off", MenuAction.Back, "off.ogg", onActivate: () => UpdateSetting(() => _settings.AutomaticInfo = AutomaticInfoMode.Off)),
                new MenuItem("Laps only", MenuAction.Back, "lapsonly.ogg", onActivate: () => UpdateSetting(() => _settings.AutomaticInfo = AutomaticInfoMode.LapsOnly)),
                new MenuItem("On", MenuAction.Back, "on.ogg", onActivate: () => UpdateSetting(() => _settings.AutomaticInfo = AutomaticInfoMode.On)),
                BackItem()
            };
            return _menu.CreateMenu("options_race_info", items, "Automatic information");
        }

        private MenuScreen BuildOptionsCopilotMenu()
        {
            var items = new List<MenuItem>
            {
                new MenuItem("Off", MenuAction.Back, "off.ogg", onActivate: () => UpdateSetting(() => _settings.Copilot = CopilotMode.Off)),
                new MenuItem("Curves only", MenuAction.Back, "curvesonly.ogg", onActivate: () => UpdateSetting(() => _settings.Copilot = CopilotMode.CurvesOnly)),
                new MenuItem("All", MenuAction.Back, "all.ogg", onActivate: () => UpdateSetting(() => _settings.Copilot = CopilotMode.All)),
                BackItem()
            };
            return _menu.CreateMenu("options_race_copilot", items, "Copilot settings");
        }

        private MenuScreen BuildOptionsLapsMenu()
        {
            var items = new List<MenuItem>();
            for (var laps = 2; laps <= 20; laps++)
            {
                var value = laps;
                var sound = Path.Combine("Numbers", $"{laps}.ogg");
                items.Add(new MenuItem(laps.ToString(), MenuAction.Back, sound, onActivate: () => UpdateSetting(() => _settings.NrOfLaps = value)));
            }
            items.Add(BackItem());
            return _menu.CreateMenu("options_race_laps", items, "Choose lap count");
        }

        private MenuScreen BuildOptionsComputersMenu()
        {
            var items = new List<MenuItem>();
            for (var count = 1; count <= 7; count++)
            {
                var value = count;
                var sound = Path.Combine("Numbers", $"{count}.ogg");
                items.Add(new MenuItem(count.ToString(), MenuAction.Back, sound, onActivate: () => UpdateSetting(() => _settings.NrOfComputers = value)));
            }
            items.Add(BackItem());
            return _menu.CreateMenu("options_race_computers", items, "Choose number of computer players");
        }

        private MenuScreen BuildOptionsDifficultyMenu()
        {
            var items = new List<MenuItem>
            {
                new MenuItem("Easy", MenuAction.Back, "easy.ogg", onActivate: () => UpdateSetting(() => _settings.Difficulty = RaceDifficulty.Easy)),
                new MenuItem("Normal", MenuAction.Back, "normal.ogg", onActivate: () => UpdateSetting(() => _settings.Difficulty = RaceDifficulty.Normal)),
                new MenuItem("Hard", MenuAction.Back, "hard.ogg", onActivate: () => UpdateSetting(() => _settings.Difficulty = RaceDifficulty.Hard)),
                BackItem()
            };
            return _menu.CreateMenu("options_race_difficulty", items, "Choose difficulty");
        }

        private MenuScreen BuildOptionsRestoreMenu()
        {
            var items = new List<MenuItem>
            {
                new MenuItem("Yes", MenuAction.Back, "yes.ogg", onActivate: RestoreDefaults),
                new MenuItem("No", MenuAction.Back, "no.ogg"),
                BackItem()
            };
            return _menu.CreateMenu("options_restore", items, "Restore defaults");
        }

        private void RestoreDefaults()
        {
            _settings.RestoreDefaults();
            _raceInput.SetDevice(_settings.DeviceMode);
            SaveSettings();
            _speech.Speak("Defaults restored.", interrupt: true);
        }

        private void SetDevice(InputDeviceMode mode)
        {
            _settings.DeviceMode = mode;
            _raceInput.SetDevice(mode);
            SaveSettings();
        }

        private void ToggleCurveAnnouncements()
        {
            _settings.CurveAnnouncement = _settings.CurveAnnouncement == CurveAnnouncementMode.FixedDistance
                ? CurveAnnouncementMode.SpeedDependent
                : CurveAnnouncementMode.FixedDistance;
            SaveSettings();
        }

        private void ToggleSetting(Action update)
        {
            update();
            SaveSettings();
        }

        private void UpdateSetting(Action update)
        {
            update();
            SaveSettings();
        }

        private void UpdateMapping()
        {
            if (_mappingNeedsInstruction)
            {
                _mappingNeedsInstruction = false;
                _speech.Speak(GetMappingInstruction(_mappingMode, _mappingAction), interrupt: true);
            }

            if (_input.WasPressed(Key.Escape))
            {
                _mappingActive = false;
                _speech.Speak("Mapping cancelled.", interrupt: true);
                return;
            }

            if (_mappingMode == InputMappingMode.Keyboard)
                TryCaptureKeyboardMapping();
            else
                TryCaptureJoystickMapping();
        }

        private void TryCaptureKeyboardMapping()
        {
            for (var i = 1; i < 256; i++)
            {
                var key = (Key)i;
                if (!_input.WasPressed(key))
                    continue;
                if (IsReservedKey(key))
                {
                    _speech.Speak("That key is reserved.", interrupt: true);
                    return;
                }
                if (IsKeyInUse(key, _mappingAction))
                {
                    _speech.Speak("That key is already in use.", interrupt: true);
                    return;
                }

                ApplyKeyMapping(_mappingAction, key);
                SaveSettings();
                _mappingActive = false;
                _speech.Speak($"{ActionLabel(_mappingAction)} set to {FormatKey(key)}.", interrupt: true);
                return;
            }
        }

        private void TryCaptureJoystickMapping()
        {
            if (!_input.TryGetJoystickState(out var state))
            {
                _mappingActive = false;
                _speech.Speak("No joystick detected.", interrupt: true);
                return;
            }

            if (!_mappingHasPrevJoystick)
            {
                _mappingPrevJoystick = state;
                _mappingHasPrevJoystick = true;
                return;
            }

            var axis = FindTriggeredAxis(state, _mappingPrevJoystick);
            _mappingPrevJoystick = state;
            if (axis == JoystickAxisOrButton.AxisNone)
                return;
            if (IsAxisInUse(axis, _mappingAction))
            {
                _speech.Speak("That control is already in use.", interrupt: true);
                return;
            }

            ApplyAxisMapping(_mappingAction, axis);
            SaveSettings();
            _mappingActive = false;
            _speech.Speak($"{ActionLabel(_mappingAction)} set to {FormatAxis(axis)}.", interrupt: true);
        }

        private JoystickAxisOrButton FindTriggeredAxis(JoystickStateSnapshot current, JoystickStateSnapshot previous)
        {
            for (var i = (int)JoystickAxisOrButton.AxisXNeg; i <= (int)JoystickAxisOrButton.Pov8; i++)
            {
                var axis = (JoystickAxisOrButton)i;
                if (IsAxisActive(axis, current) && !IsAxisActive(axis, previous))
                    return axis;
            }
            return JoystickAxisOrButton.AxisNone;
        }

        private bool IsAxisActive(JoystickAxisOrButton axis, JoystickStateSnapshot state)
        {
            var center = _settings.JoystickCenter;
            const int threshold = 50;
            switch (axis)
            {
                case JoystickAxisOrButton.AxisXNeg:
                    return state.X < center.X - threshold;
                case JoystickAxisOrButton.AxisXPos:
                    return state.X > center.X + threshold;
                case JoystickAxisOrButton.AxisYNeg:
                    return state.Y < center.Y - threshold;
                case JoystickAxisOrButton.AxisYPos:
                    return state.Y > center.Y + threshold;
                case JoystickAxisOrButton.AxisZNeg:
                    return state.Z < center.Z - threshold;
                case JoystickAxisOrButton.AxisZPos:
                    return state.Z > center.Z + threshold;
                case JoystickAxisOrButton.AxisRxNeg:
                    return state.Rx < center.Rx - threshold;
                case JoystickAxisOrButton.AxisRxPos:
                    return state.Rx > center.Rx + threshold;
                case JoystickAxisOrButton.AxisRyNeg:
                    return state.Ry < center.Ry - threshold;
                case JoystickAxisOrButton.AxisRyPos:
                    return state.Ry > center.Ry + threshold;
                case JoystickAxisOrButton.AxisRzNeg:
                    return state.Rz < center.Rz - threshold;
                case JoystickAxisOrButton.AxisRzPos:
                    return state.Rz > center.Rz + threshold;
                case JoystickAxisOrButton.AxisSlider1Neg:
                    return state.Slider1 < center.Slider1 - threshold;
                case JoystickAxisOrButton.AxisSlider1Pos:
                    return state.Slider1 > center.Slider1 + threshold;
                case JoystickAxisOrButton.AxisSlider2Neg:
                    return state.Slider2 < center.Slider2 - threshold;
                case JoystickAxisOrButton.AxisSlider2Pos:
                    return state.Slider2 > center.Slider2 + threshold;
                case JoystickAxisOrButton.Button1:
                    return state.B1;
                case JoystickAxisOrButton.Button2:
                    return state.B2;
                case JoystickAxisOrButton.Button3:
                    return state.B3;
                case JoystickAxisOrButton.Button4:
                    return state.B4;
                case JoystickAxisOrButton.Button5:
                    return state.B5;
                case JoystickAxisOrButton.Button6:
                    return state.B6;
                case JoystickAxisOrButton.Button7:
                    return state.B7;
                case JoystickAxisOrButton.Button8:
                    return state.B8;
                case JoystickAxisOrButton.Button9:
                    return state.B9;
                case JoystickAxisOrButton.Button10:
                    return state.B10;
                case JoystickAxisOrButton.Button11:
                    return state.B11;
                case JoystickAxisOrButton.Button12:
                    return state.B12;
                case JoystickAxisOrButton.Button13:
                    return state.B13;
                case JoystickAxisOrButton.Button14:
                    return state.B14;
                case JoystickAxisOrButton.Button15:
                    return state.B15;
                case JoystickAxisOrButton.Button16:
                    return state.B16;
                case JoystickAxisOrButton.Pov1:
                    return state.Pov1;
                case JoystickAxisOrButton.Pov2:
                    return state.Pov2;
                case JoystickAxisOrButton.Pov3:
                    return state.Pov3;
                case JoystickAxisOrButton.Pov4:
                    return state.Pov4;
                case JoystickAxisOrButton.Pov5:
                    return state.Pov5;
                case JoystickAxisOrButton.Pov6:
                    return state.Pov6;
                case JoystickAxisOrButton.Pov7:
                    return state.Pov7;
                case JoystickAxisOrButton.Pov8:
                    return state.Pov8;
                default:
                    return false;
            }
        }

        private static bool IsReservedKey(Key key)
        {
            if (key >= Key.F1 && key <= Key.F12)
                return true;
            if (key >= Key.D1 && key <= Key.D8)
                return true;
            return key == Key.LeftAlt;
        }

        private bool IsKeyInUse(Key key, MappingAction ignore)
        {
            foreach (MappingAction action in Enum.GetValues(typeof(MappingAction)))
            {
                if (action == ignore)
                    continue;
                if (GetKeyForAction(action) == key)
                    return true;
            }
            return false;
        }

        private bool IsAxisInUse(JoystickAxisOrButton axis, MappingAction ignore)
        {
            foreach (MappingAction action in Enum.GetValues(typeof(MappingAction)))
            {
                if (action == ignore)
                    continue;
                if (GetAxisForAction(action) == axis)
                    return true;
            }
            return false;
        }

        private void ApplyKeyMapping(MappingAction action, Key key)
        {
            switch (action)
            {
                case MappingAction.SteerLeft:
                    _raceInput.SetLeft(key);
                    break;
                case MappingAction.SteerRight:
                    _raceInput.SetRight(key);
                    break;
                case MappingAction.Throttle:
                    _raceInput.SetThrottle(key);
                    break;
                case MappingAction.Brake:
                    _raceInput.SetBrake(key);
                    break;
                case MappingAction.GearUp:
                    _raceInput.SetGearUp(key);
                    break;
                case MappingAction.GearDown:
                    _raceInput.SetGearDown(key);
                    break;
                case MappingAction.Horn:
                    _raceInput.SetHorn(key);
                    break;
                case MappingAction.RequestInfo:
                    _raceInput.SetRequestInfo(key);
                    break;
                case MappingAction.CurrentGear:
                    _raceInput.SetCurrentGear(key);
                    break;
                case MappingAction.CurrentLapNr:
                    _raceInput.SetCurrentLapNr(key);
                    break;
                case MappingAction.CurrentRacePerc:
                    _raceInput.SetCurrentRacePerc(key);
                    break;
                case MappingAction.CurrentLapPerc:
                    _raceInput.SetCurrentLapPerc(key);
                    break;
                case MappingAction.CurrentRaceTime:
                    _raceInput.SetCurrentRaceTime(key);
                    break;
            }
        }

        private void ApplyAxisMapping(MappingAction action, JoystickAxisOrButton axis)
        {
            switch (action)
            {
                case MappingAction.SteerLeft:
                    _raceInput.SetLeft(axis);
                    break;
                case MappingAction.SteerRight:
                    _raceInput.SetRight(axis);
                    break;
                case MappingAction.Throttle:
                    _raceInput.SetThrottle(axis);
                    break;
                case MappingAction.Brake:
                    _raceInput.SetBrake(axis);
                    break;
                case MappingAction.GearUp:
                    _raceInput.SetGearUp(axis);
                    break;
                case MappingAction.GearDown:
                    _raceInput.SetGearDown(axis);
                    break;
                case MappingAction.Horn:
                    _raceInput.SetHorn(axis);
                    break;
                case MappingAction.RequestInfo:
                    _raceInput.SetRequestInfo(axis);
                    break;
                case MappingAction.CurrentGear:
                    _raceInput.SetCurrentGear(axis);
                    break;
                case MappingAction.CurrentLapNr:
                    _raceInput.SetCurrentLapNr(axis);
                    break;
                case MappingAction.CurrentRacePerc:
                    _raceInput.SetCurrentRacePerc(axis);
                    break;
                case MappingAction.CurrentLapPerc:
                    _raceInput.SetCurrentLapPerc(axis);
                    break;
                case MappingAction.CurrentRaceTime:
                    _raceInput.SetCurrentRaceTime(axis);
                    break;
            }
        }

        private Key GetKeyForAction(MappingAction action)
        {
            return action switch
            {
                MappingAction.SteerLeft => _settings.KeyLeft,
                MappingAction.SteerRight => _settings.KeyRight,
                MappingAction.Throttle => _settings.KeyThrottle,
                MappingAction.Brake => _settings.KeyBrake,
                MappingAction.GearUp => _settings.KeyGearUp,
                MappingAction.GearDown => _settings.KeyGearDown,
                MappingAction.Horn => _settings.KeyHorn,
                MappingAction.RequestInfo => _settings.KeyRequestInfo,
                MappingAction.CurrentGear => _settings.KeyCurrentGear,
                MappingAction.CurrentLapNr => _settings.KeyCurrentLapNr,
                MappingAction.CurrentRacePerc => _settings.KeyCurrentRacePerc,
                MappingAction.CurrentLapPerc => _settings.KeyCurrentLapPerc,
                MappingAction.CurrentRaceTime => _settings.KeyCurrentRaceTime,
                _ => Key.Unknown
            };
        }

        private JoystickAxisOrButton GetAxisForAction(MappingAction action)
        {
            return action switch
            {
                MappingAction.SteerLeft => _settings.JoystickLeft,
                MappingAction.SteerRight => _settings.JoystickRight,
                MappingAction.Throttle => _settings.JoystickThrottle,
                MappingAction.Brake => _settings.JoystickBrake,
                MappingAction.GearUp => _settings.JoystickGearUp,
                MappingAction.GearDown => _settings.JoystickGearDown,
                MappingAction.Horn => _settings.JoystickHorn,
                MappingAction.RequestInfo => _settings.JoystickRequestInfo,
                MappingAction.CurrentGear => _settings.JoystickCurrentGear,
                MappingAction.CurrentLapNr => _settings.JoystickCurrentLapNr,
                MappingAction.CurrentRacePerc => _settings.JoystickCurrentRacePerc,
                MappingAction.CurrentLapPerc => _settings.JoystickCurrentLapPerc,
                MappingAction.CurrentRaceTime => _settings.JoystickCurrentRaceTime,
                _ => JoystickAxisOrButton.AxisNone
            };
        }

        private static string ActionLabel(MappingAction action)
        {
            return action switch
            {
                MappingAction.SteerLeft => "Steer left",
                MappingAction.SteerRight => "Steer right",
                MappingAction.Throttle => "Throttle",
                MappingAction.Brake => "Brake",
                MappingAction.GearUp => "Shift gear up",
                MappingAction.GearDown => "Shift gear down",
                MappingAction.Horn => "Use horn",
                MappingAction.RequestInfo => "Request info",
                MappingAction.CurrentGear => "Current gear",
                MappingAction.CurrentLapNr => "Current lap number",
                MappingAction.CurrentRacePerc => "Current race percentage",
                MappingAction.CurrentLapPerc => "Current lap percentage",
                MappingAction.CurrentRaceTime => "Current race time",
                _ => "Action"
            };
        }

        private string FormatMappingValue(MappingAction action, InputMappingMode mode)
        {
            return mode == InputMappingMode.Keyboard
                ? FormatKey(GetKeyForAction(action))
                : FormatAxis(GetAxisForAction(action));
        }

        private static string FormatKey(Key key)
        {
            if ((int)key <= 0)
                return "none";
            return key.ToString();
        }

        private static string FormatAxis(JoystickAxisOrButton axis)
        {
            return axis switch
            {
                JoystickAxisOrButton.AxisNone => "none",
                JoystickAxisOrButton.AxisXNeg => "X-",
                JoystickAxisOrButton.AxisXPos => "X+",
                JoystickAxisOrButton.AxisYNeg => "Y-",
                JoystickAxisOrButton.AxisYPos => "Y+",
                JoystickAxisOrButton.AxisZNeg => "Z-",
                JoystickAxisOrButton.AxisZPos => "Z+",
                JoystickAxisOrButton.AxisRxNeg => "Rx-",
                JoystickAxisOrButton.AxisRxPos => "Rx+",
                JoystickAxisOrButton.AxisRyNeg => "Ry-",
                JoystickAxisOrButton.AxisRyPos => "Ry+",
                JoystickAxisOrButton.AxisRzNeg => "Rz-",
                JoystickAxisOrButton.AxisRzPos => "Rz+",
                JoystickAxisOrButton.AxisSlider1Neg => "Slider1-",
                JoystickAxisOrButton.AxisSlider1Pos => "Slider1+",
                JoystickAxisOrButton.AxisSlider2Neg => "Slider2-",
                JoystickAxisOrButton.AxisSlider2Pos => "Slider2+",
                JoystickAxisOrButton.Button1 => "Button 1",
                JoystickAxisOrButton.Button2 => "Button 2",
                JoystickAxisOrButton.Button3 => "Button 3",
                JoystickAxisOrButton.Button4 => "Button 4",
                JoystickAxisOrButton.Button5 => "Button 5",
                JoystickAxisOrButton.Button6 => "Button 6",
                JoystickAxisOrButton.Button7 => "Button 7",
                JoystickAxisOrButton.Button8 => "Button 8",
                JoystickAxisOrButton.Button9 => "Button 9",
                JoystickAxisOrButton.Button10 => "Button 10",
                JoystickAxisOrButton.Button11 => "Button 11",
                JoystickAxisOrButton.Button12 => "Button 12",
                JoystickAxisOrButton.Button13 => "Button 13",
                JoystickAxisOrButton.Button14 => "Button 14",
                JoystickAxisOrButton.Button15 => "Button 15",
                JoystickAxisOrButton.Button16 => "Button 16",
                JoystickAxisOrButton.Pov1 => "POV 1 up",
                JoystickAxisOrButton.Pov2 => "POV 1 right",
                JoystickAxisOrButton.Pov3 => "POV 1 down",
                JoystickAxisOrButton.Pov4 => "POV 1 left",
                JoystickAxisOrButton.Pov5 => "POV 2 up",
                JoystickAxisOrButton.Pov6 => "POV 2 right",
                JoystickAxisOrButton.Pov7 => "POV 2 down",
                JoystickAxisOrButton.Pov8 => "POV 2 left",
                _ => axis.ToString()
            };
        }

        private static string GetMappingInstruction(InputMappingMode mode, MappingAction action)
        {
            var label = ActionLabel(action).ToLowerInvariant();
            return mode == InputMappingMode.Keyboard
                ? $"Press the new key for {label}."
                : $"Move or press the joystick control for {label}.";
        }

        private void BeginMapping(InputMappingMode mode, MappingAction action)
        {
            if (mode == InputMappingMode.Joystick)
            {
                if (_input.Joystick == null || !_input.Joystick.IsAvailable)
                {
                    _speech.Speak("No joystick detected.", interrupt: true);
                    return;
                }
            }

            _mappingActive = true;
            _mappingMode = mode;
            _mappingAction = action;
            _mappingHasPrevJoystick = false;
            _mappingNeedsInstruction = true;
        }

        private void PrepareQuickStart()
        {
            PrepareMode(RaceMode.QuickStart);
            SelectRandomTrackAny(_settings.RandomCustomTracks);
            SelectRandomVehicle();
            _setup.Transmission = TransmissionMode.Automatic;
        }

        private void PrepareMode(RaceMode mode)
        {
            _setup.Mode = mode;
            _setup.ClearSelection();
        }

        private void SelectTrack(TrackCategory category, string trackKey)
        {
            _setup.TrackCategory = category;
            _setup.TrackNameOrFile = trackKey;
        }

        private void SelectRandomTrack(TrackCategory category)
        {
            SelectRandomTrack(category, _settings.RandomCustomTracks);
        }

        private void SelectRandomTrack(TrackCategory category, bool includeCustom)
        {
            _setup.TrackCategory = category;
            _setup.TrackNameOrFile = GetRandomTrack(category, includeCustom);
        }

        private void SelectRandomTrackAny(bool includeCustom)
        {
            var candidates = new List<(string Key, TrackCategory Category)>();
            candidates.AddRange(RaceTracks.Select(track => (track.Key, TrackCategory.RaceTrack)));
            candidates.AddRange(AdventureTracks.Select(track => (track.Key, TrackCategory.StreetAdventure)));
            if (includeCustom)
                candidates.AddRange(GetCustomTrackFiles().Select(file => (file, TrackCategory.RaceTrack)));

            if (candidates.Count == 0)
            {
                _setup.TrackCategory = TrackCategory.RaceTrack;
                _setup.TrackNameOrFile = RaceTracks[0].Key;
                return;
            }

            var pick = candidates[Algorithm.RandomInt(candidates.Count)];
            _setup.TrackCategory = pick.Category;
            _setup.TrackNameOrFile = pick.Key;
        }

        private void SelectVehicle(int index)
        {
            _setup.VehicleIndex = index;
            _setup.VehicleFile = null;
        }

        private void SelectCustomVehicle(string file)
        {
            _setup.VehicleIndex = null;
            _setup.VehicleFile = file;
        }

        private void SelectRandomVehicle()
        {
            var customFiles = _settings.RandomCustomVehicles ? GetCustomVehicleFiles().ToList() : new List<string>();
            var total = VehicleCatalog.VehicleCount + customFiles.Count;
            if (total <= 0)
            {
                SelectVehicle(0);
                return;
            }

            var roll = Algorithm.RandomInt(total);
            if (roll < VehicleCatalog.VehicleCount)
            {
                SelectVehicle(roll);
                return;
            }

            var customIndex = roll - VehicleCatalog.VehicleCount;
            if (customIndex >= 0 && customIndex < customFiles.Count)
                SelectCustomVehicle(customFiles[customIndex]);
            else
                SelectVehicle(0);
        }

        private void CompleteTransmission(RaceMode mode, TransmissionMode transmission)
        {
            _setup.Transmission = transmission;
            QueueRaceStart(mode);
        }

        private void QueueRaceStart(RaceMode mode)
        {
            _pendingRaceStart = true;
            _pendingMode = mode;
        }

        private void PushRandomTrackType(RaceMode mode)
        {
            var category = Algorithm.RandomInt(2) == 0 ? TrackCategory.RaceTrack : TrackCategory.StreetAdventure;
            _setup.TrackCategory = category;
            _menu.Push(TrackMenuId(mode, category));
        }

        private string GetRandomTrack(TrackCategory category, bool includeCustom)
        {
            var candidates = new List<string>();
            var source = category == TrackCategory.RaceTrack ? RaceTracks : AdventureTracks;
            candidates.AddRange(source.Select(t => t.Key));

            if (includeCustom)
                candidates.AddRange(GetCustomTrackFiles());

            if (candidates.Count == 0)
                return RaceTracks[0].Key;

            var index = Algorithm.RandomInt(candidates.Count);
            return candidates[index];
        }

        private IEnumerable<string> GetCustomTrackFiles()
        {
            var root = Path.Combine(AssetPaths.Root, "Tracks");
            if (!Directory.Exists(root))
                return Array.Empty<string>();
            return Directory.EnumerateFiles(root, "*.trk", SearchOption.TopDirectoryOnly);
        }

        private IEnumerable<string> GetCustomVehicleFiles()
        {
            var root = Path.Combine(AssetPaths.Root, "Vehicles");
            if (!Directory.Exists(root))
                return Array.Empty<string>();
            return Directory.EnumerateFiles(root, "*.vhc", SearchOption.TopDirectoryOnly);
        }

        private static string TrackMenuId(RaceMode mode, TrackCategory category)
        {
            var prefix = mode == RaceMode.TimeTrial ? "time_trial" : "single_race";
            return category == TrackCategory.RaceTrack ? $"{prefix}_tracks_race" : $"{prefix}_tracks_adventure";
        }

        private static string VehicleMenuId(RaceMode mode)
        {
            return mode == RaceMode.TimeTrial ? "time_trial_vehicles" : "single_race_vehicles";
        }

        private static string TransmissionMenuId(RaceMode mode)
        {
            return mode == RaceMode.TimeTrial ? "time_trial_transmission" : "single_race_transmission";
        }

        private static MenuItem BackItem()
        {
            return new MenuItem("Go back", MenuAction.Back, "goback.ogg");
        }

        private static string FormatOnOff(bool value) => value ? "on" : "off";

        private static string FormatServerPort(int port)
        {
            return port > 0 ? port.ToString() : $"default ({ClientProtocol.DefaultServerPort})";
        }

        private static string DeviceLabel(InputDeviceMode mode)
        {
            return mode switch
            {
                InputDeviceMode.Keyboard => "keyboard",
                InputDeviceMode.Joystick => "joystick",
                InputDeviceMode.Both => "both",
                _ => "keyboard"
            };
        }

        private static string CopilotLabel(CopilotMode mode)
        {
            return mode switch
            {
                CopilotMode.Off => "off",
                CopilotMode.CurvesOnly => "curves only",
                CopilotMode.All => "all",
                _ => "off"
            };
        }

        private static string CurveLabel(CurveAnnouncementMode mode)
        {
            return mode switch
            {
                CurveAnnouncementMode.FixedDistance => "fixed distance",        
                CurveAnnouncementMode.SpeedDependent => "speed dependent",      
                _ => "fixed distance"
            };
        }

        private static string AutomaticInfoLabel(AutomaticInfoMode mode)
        {
            return mode switch
            {
                AutomaticInfoMode.Off => "off",
                AutomaticInfoMode.LapsOnly => "laps only",
                AutomaticInfoMode.On => "on",
                _ => "on"
            };
        }

        private static string DifficultyLabel(RaceDifficulty difficulty)
        {
            return difficulty switch
            {
                RaceDifficulty.Easy => "easy",
                RaceDifficulty.Normal => "normal",
                RaceDifficulty.Hard => "hard",
                _ => "easy"
            };
        }

        private static string ModeLabel(RaceMode mode)
        {
            return mode switch
            {
                RaceMode.QuickStart => "Quick start",
                RaceMode.TimeTrial => "Time trial",
                RaceMode.SingleRace => "Single race",
                _ => "Race"
            };
        }

        private void RunTimeTrial(float elapsed)
        {
            if (_timeTrial == null)
            {
                EndRace();
                return;
            }

            _timeTrial.Run(elapsed);
            if (_timeTrial.WantsPause)
                EnterPause(AppState.TimeTrial);
            if (_timeTrial.WantsExit || _input.WasPressed(SharpDX.DirectInput.Key.Escape))
                EndRace();
        }

        private void RunSingleRace(float elapsed)
        {
            if (_singleRace == null)
            {
                EndRace();
                return;
            }

            _singleRace.Run(elapsed);
            if (_singleRace.WantsPause)
                EnterPause(AppState.SingleRace);
            if (_singleRace.WantsExit || _input.WasPressed(SharpDX.DirectInput.Key.Escape))
                EndRace();
        }

        private void RunMultiplayerRace(float elapsed)
        {
            if (_multiplayerRace == null)
            {
                EndMultiplayerRace();
                return;
            }

            ProcessMultiplayerPackets();
            if (_multiplayerRace == null)
                return;
            _multiplayerRace.Run(elapsed);
            if (_multiplayerRace.WantsExit || _input.WasPressed(SharpDX.DirectInput.Key.Escape))
                EndMultiplayerRace();
        }

        private void ProcessMultiplayerPackets()
        {
            if (_session == null)
                return;

            while (_session.TryDequeuePacket(out var packet))
            {
                switch (packet.Command)
                {
                    case Command.PlayerJoined:
                        if (ClientPacketSerializer.TryReadPlayerJoined(packet.Payload, out var joined))
                        {
                            if (joined.PlayerNumber != _session.PlayerNumber)
                            {
                                var name = string.IsNullOrWhiteSpace(joined.Name)
                                    ? $"Player {joined.PlayerNumber + 1}"
                                    : joined.Name;
                                _speech.Speak($"{name} joined.", interrupt: true);
                            }
                        }
                        break;
                    case Command.LoadCustomTrack:
                        if (ClientPacketSerializer.TryReadLoadCustomTrack(packet.Payload, out var track))
                        {
                            var name = string.IsNullOrWhiteSpace(track.TrackName) ? "custom" : track.TrackName;
                            var userDefined = string.Equals(name, "custom", StringComparison.OrdinalIgnoreCase);
                            _pendingMultiplayerTrack = new TrackData(userDefined, track.TrackWeather, track.TrackAmbience, track.Definitions);
                            _pendingMultiplayerTrackName = name;
                            _pendingMultiplayerLaps = track.NrOfLaps;
                            if (_pendingMultiplayerStart)
                                StartMultiplayerRace();
                        }
                        break;
                    case Command.StartRace:
                        StartMultiplayerRace();
                        break;
                    case Command.PlayerData:
                        if (_multiplayerRace != null && ClientPacketSerializer.TryReadPlayerData(packet.Payload, out var playerData))
                            _multiplayerRace.ApplyRemoteData(playerData);
                        break;
                    case Command.PlayerBumped:
                        if (_multiplayerRace != null && ClientPacketSerializer.TryReadPlayerBumped(packet.Payload, out var bump))
                            _multiplayerRace.ApplyBump(bump);
                        break;
                    case Command.PlayerDisconnected:
                        if (_multiplayerRace != null && ClientPacketSerializer.TryReadPlayer(packet.Payload, out var disconnected))
                            _multiplayerRace.RemoveRemotePlayer(disconnected.PlayerNumber);
                        break;
                    case Command.StopRace:
                    case Command.RaceAborted:
                        if (_state == AppState.MultiplayerRace)
                            EndMultiplayerRace();
                        break;
                }
            }
        }

        private void StartMultiplayerRace()
        {
            if (_session == null)
                return;
            if (_multiplayerRace != null)
                return;
            if (_pendingMultiplayerTrack == null)
            {
                _pendingMultiplayerStart = true;
                return;
            }

            _pendingMultiplayerStart = false;
            var trackName = string.IsNullOrWhiteSpace(_pendingMultiplayerTrackName) ? "custom" : _pendingMultiplayerTrackName;
            var laps = _pendingMultiplayerLaps > 0 ? _pendingMultiplayerLaps : _settings.NrOfLaps;
            var vehicleIndex = 0;
            var automatic = true;

            _multiplayerRace?.FinalizeLevelMultiplayer();
            _multiplayerRace?.Dispose();
            _multiplayerRace = new LevelMultiplayer(
                _audio,
                _speech,
                _settings,
                _raceInput,
                _pendingMultiplayerTrack!,
                trackName,
                automatic,
                laps,
                vehicleIndex,
                null,
                _input.Joystick,
                _session,
                _session.PlayerId,
                _session.PlayerNumber);
            _multiplayerRace.Initialize();
            _state = AppState.MultiplayerRace;
        }

        private void EndMultiplayerRace()
        {
            _multiplayerRace?.FinalizeLevelMultiplayer();
            _multiplayerRace?.Dispose();
            _multiplayerRace = null;

            if (_session != null)
            {
                _session.SendPlayerState(PlayerState.NotReady);
                _state = AppState.Menu;
                _menu.ShowRoot("multiplayer_lobby");
            }
            else
            {
                _state = AppState.Menu;
                _menu.ShowRoot("main");
                _speech.Speak("Main menu", interrupt: true);
            }
        }

        private void UpdatePaused()
        {
            if (!_raceInput.GetPause() && !_pauseKeyReleased)
            {
                _pauseKeyReleased = true;
                return;
            }

            if (_raceInput.GetPause() && _pauseKeyReleased)
            {
                _pauseKeyReleased = false;
                switch (_pausedState)
                {
                    case AppState.TimeTrial:
                        _timeTrial?.Unpause();
                        _timeTrial?.StopStopwatchDiff();
                        _state = AppState.TimeTrial;
                        break;
                    case AppState.SingleRace:
                        _singleRace?.Unpause();
                        _singleRace?.StopStopwatchDiff();
                        _state = AppState.SingleRace;
                        break;
                }
            }
        }

        private void EnterPause(AppState state)
        {
            _pausedState = state;
            _pauseKeyReleased = false;
            switch (_pausedState)
            {
                case AppState.TimeTrial:
                    _timeTrial?.StartStopwatchDiff();
                    _timeTrial?.Pause();
                    _state = AppState.Paused;
                    break;
                case AppState.SingleRace:
                    _singleRace?.StartStopwatchDiff();
                    _singleRace?.Pause();
                    _state = AppState.Paused;
                    break;
            }
        }

        private void StartRace(RaceMode mode)
        {
            var track = string.IsNullOrWhiteSpace(_setup.TrackNameOrFile)
                ? RaceTracks[0].Key
                : _setup.TrackNameOrFile!;
            var vehicleIndex = _setup.VehicleIndex ?? 0;
            var vehicleFile = _setup.VehicleFile;
            var automatic = _setup.Transmission == TransmissionMode.Automatic;

            switch (mode)
            {
                case RaceMode.TimeTrial:
                    _timeTrial?.FinalizeLevelTimeTrial();
                    _timeTrial?.Dispose();
                    _timeTrial = new LevelTimeTrial(_audio, _speech, _settings, _raceInput, track, automatic, _settings.NrOfLaps, vehicleIndex, vehicleFile, _input.Joystick);
                    _timeTrial.Initialize();
                    _state = AppState.TimeTrial;
                    _speech.Speak("Time trial.", interrupt: true);
                    break;
                case RaceMode.QuickStart:
                case RaceMode.SingleRace:
                    _singleRace?.FinalizeLevelSingleRace();
                    _singleRace?.Dispose();
                    _singleRace = new LevelSingleRace(_audio, _speech, _settings, _raceInput, track, automatic, _settings.NrOfLaps, vehicleIndex, vehicleFile, _input.Joystick);
                    _singleRace.Initialize(Algorithm.RandomInt(_settings.NrOfComputers + 1));
                    _state = AppState.SingleRace;
                    _speech.Speak(mode == RaceMode.QuickStart ? "Quick start." : "Single race.", interrupt: true);
                    break;
            }
        }

        private void EndRace()
        {
            _timeTrial?.FinalizeLevelTimeTrial();
            _timeTrial?.Dispose();
            _timeTrial = null;

            _singleRace?.FinalizeLevelSingleRace();
            _singleRace?.Dispose();
            _singleRace = null;

            _state = AppState.Menu;
            _menu.ShowRoot("main");
            _speech.Speak("Main menu", interrupt: true);
        }

        public void Dispose()
        {
            _logo?.Dispose();
            _menu.Dispose();
            _input.Dispose();
            _session?.Dispose();
            _speech.Dispose();
            _audio.Dispose();
        }

        private void SaveSettings()
        {
            _settingsManager.Save(_settings);
        }

        private void SaveMusicVolume(float volume)
        {
            _settings.MusicVolume = volume;
            SaveSettings();
        }
    }
}
