using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TopSpeed.Audio;
using TopSpeed.Common;
using TopSpeed.Data;
using TopSpeed.Input;
using TopSpeed.Menu;
using TopSpeed.Race;
using TopSpeed.Speech;

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
            Paused
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

        private readonly AudioManager _audio;
        private readonly SpeechService _speech;
        private readonly InputManager _input;
        private readonly MenuManager _menu;
        private readonly RaceSettings _settings;
        private readonly RaceInput _raceInput;
        private readonly RaceSetup _setup;
        private readonly SettingsManager _settingsManager;
        private LogoScreen? _logo;
        private AppState _state;
        private AppState _pausedState;
        private bool _pendingRaceStart;
        private RaceMode _pendingMode;
        private bool _pauseKeyReleased = true;
        private LevelTimeTrial? _timeTrial;
        private LevelSingleRace? _singleRace;

        public event Action? ExitRequested;

        public Game(IntPtr windowHandle)
        {
            _settingsManager = new SettingsManager();
            _settings = _settingsManager.Load();
            _audio = new AudioManager(_settings.ThreeDSound);
            _speech = new SpeechService();
            _input = new InputManager(windowHandle);
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
                    var action = _menu.Update(_input);
                    HandleMenuAction(action);
                    break;
                case AppState.TimeTrial:
                    RunTimeTrial(deltaSeconds);
                    break;
                case AppState.SingleRace:
                    RunSingleRace(deltaSeconds);
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
                case MenuAction.Multiplayer:
                    _speech.Speak("Multiplayer is not implemented yet.", interrupt: true);
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
                new MenuItem("MultiPlayer game", MenuAction.Multiplayer, "multiplayergame.ogg"),
                new MenuItem("Options", MenuAction.None, "options.ogg", nextMenuId: "options_main"),
                new MenuItem("Exit Game", MenuAction.Exit, "exitgame.ogg")
            }, "Main menu");
            mainMenu.MusicFile = "theme1.ogg";
            mainMenu.MusicVolume = _settings.MusicVolume;
            mainMenu.MusicVolumeChanged = SaveMusicVolume;
            _menu.Register(mainMenu);

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
            _menu.Register(BuildOptionsCopilotMenu());
            _menu.Register(BuildOptionsLapsMenu());
            _menu.Register(BuildOptionsComputersMenu());
            _menu.Register(BuildOptionsDifficultyMenu());
            _menu.Register(BuildOptionsRestoreMenu());
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
            var items = BuildMappingItems();
            return _menu.CreateMenu("options_controls_keyboard", items, "Map keyboard keys");
        }

        private MenuScreen BuildOptionsControlsJoystickMenu()
        {
            var items = BuildMappingItems();
            return _menu.CreateMenu("options_controls_joystick", items, "Map joystick keys");
        }

        private List<MenuItem> BuildMappingItems()
        {
            var items = new List<MenuItem>
            {
                new MenuItem("Steer left", MenuAction.None, "steerleft.ogg", onActivate: SpeakMappingNotImplemented),
                new MenuItem("Steer right", MenuAction.None, "steerright.ogg", onActivate: SpeakMappingNotImplemented),
                new MenuItem("Throttle", MenuAction.None, "throttle.ogg", onActivate: SpeakMappingNotImplemented),
                new MenuItem("Brake", MenuAction.None, "brake.ogg", onActivate: SpeakMappingNotImplemented),
                new MenuItem("Shift gear up", MenuAction.None, "shiftgearup.ogg", onActivate: SpeakMappingNotImplemented),
                new MenuItem("Shift gear down", MenuAction.None, "shiftgeardown.ogg", onActivate: SpeakMappingNotImplemented),
                new MenuItem("Use horn", MenuAction.None, "usehorn.ogg", onActivate: SpeakMappingNotImplemented),
                new MenuItem("Request info", MenuAction.None, "requestinfo.ogg", onActivate: SpeakMappingNotImplemented),
                new MenuItem("Current gear", MenuAction.None, "currentgear.ogg", onActivate: SpeakMappingNotImplemented),
                new MenuItem("Current lap number", MenuAction.None, "currentlapnr.ogg", onActivate: SpeakMappingNotImplemented),
                new MenuItem("Current race percentage", MenuAction.None, "currentracepercentage.ogg", onActivate: SpeakMappingNotImplemented),
                new MenuItem("Current lap percentage", MenuAction.None, "currentlappercentage.ogg", onActivate: SpeakMappingNotImplemented),
                new MenuItem("Current race time", MenuAction.None, "currentracetime.ogg", onActivate: SpeakMappingNotImplemented),
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
                new MenuItem(() => $"Automatic race information: {FormatOnOff(_settings.AutomaticInfo == AutomaticInfoMode.On)}", MenuAction.None, "automaticinformation.ogg", onActivate: ToggleAutomaticInfo),
                new MenuItem(() => $"Number of laps: {_settings.NrOfLaps}", MenuAction.None, "nroflaps.ogg", nextMenuId: "options_race_laps"),
                new MenuItem(() => $"Number of computer players: {_settings.NrOfComputers}", MenuAction.None, "nrofcomputers.ogg", nextMenuId: "options_race_computers"),
                new MenuItem(() => $"Single race difficulty: {DifficultyLabel(_settings.Difficulty)}", MenuAction.None, "difficulty.ogg", nextMenuId: "options_race_difficulty"),
                BackItem()
            };
            return _menu.CreateMenu("options_race", items, "Race settings");
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

        private void ToggleAutomaticInfo()
        {
            _settings.AutomaticInfo = _settings.AutomaticInfo == AutomaticInfoMode.On
                ? AutomaticInfoMode.Off
                : AutomaticInfoMode.On;
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

        private void SpeakMappingNotImplemented()
        {
            _speech.Speak("Mapping not implemented yet.", interrupt: true);
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
                    _timeTrial = new LevelTimeTrial(_audio, _settings, _raceInput, track, automatic, _settings.NrOfLaps, vehicleIndex, vehicleFile, _input.Joystick);
                    _timeTrial.Initialize();
                    _state = AppState.TimeTrial;
                    _speech.Speak("Time trial.", interrupt: true);
                    break;
                case RaceMode.QuickStart:
                case RaceMode.SingleRace:
                    _singleRace?.FinalizeLevelSingleRace();
                    _singleRace?.Dispose();
                    _singleRace = new LevelSingleRace(_audio, _settings, _raceInput, track, automatic, _settings.NrOfLaps, vehicleIndex, vehicleFile, _input.Joystick);
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
