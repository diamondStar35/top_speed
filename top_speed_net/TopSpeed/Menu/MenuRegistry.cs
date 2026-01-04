using System;
using System.Collections.Generic;
using System.IO;
using TopSpeed.Common;
using TopSpeed.Core;
using TopSpeed.Data;
using TopSpeed.Input;
using TopSpeed.Network;

namespace TopSpeed.Menu
{
    internal interface IMenuActions
    {
        void SaveMusicVolume(float volume);
        void QueueRaceStart(RaceMode mode);
        void StartServerDiscovery();
        void BeginManualServerEntry();
        void DisconnectFromServer();
        void SpeakNotImplemented();
        void BeginServerPortEntry();
        void RestoreDefaults();
        void RecalibrateScreenReaderRate();
        void SetDevice(InputDeviceMode mode);
        void ToggleCurveAnnouncements();
        void ToggleSetting(Action update);
        void UpdateSetting(Action update);
        void BeginMapping(InputMappingMode mode, InputAction action);
        string FormatMappingValue(InputAction action, InputMappingMode mode);
    }

    internal sealed class MenuRegistry
    {
        private readonly MenuManager _menu;
        private readonly RaceSettings _settings;
        private readonly RaceSetup _setup;
        private readonly RaceInput _raceInput;
        private readonly RaceSelection _selection;
        private readonly IMenuActions _actions;

        public MenuRegistry(
            MenuManager menu,
            RaceSettings settings,
            RaceSetup setup,
            RaceInput raceInput,
            RaceSelection selection,
            IMenuActions actions)
        {
            _menu = menu ?? throw new ArgumentNullException(nameof(menu));
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _setup = setup ?? throw new ArgumentNullException(nameof(setup));
            _raceInput = raceInput ?? throw new ArgumentNullException(nameof(raceInput));
            _selection = selection ?? throw new ArgumentNullException(nameof(selection));
            _actions = actions ?? throw new ArgumentNullException(nameof(actions));
        }

        public void RegisterAll()
        {
            var mainMenu = _menu.CreateMenu("main", new[]
            {
                new MenuItem("Quick start", MenuAction.QuickStart),
                new MenuItem("Time trial", MenuAction.None, nextMenuId: "time_trial_type", onActivate: () => PrepareMode(RaceMode.TimeTrial)),
                new MenuItem("Single race", MenuAction.None, nextMenuId: "single_race_type", onActivate: () => PrepareMode(RaceMode.SingleRace)),
                new MenuItem("MultiPlayer game", MenuAction.None, nextMenuId: "multiplayer"),
                new MenuItem("Options", MenuAction.None, nextMenuId: "options_main"),
                new MenuItem("Exit Game", MenuAction.Exit)
            }, "Main menu", titleProvider: MainMenuTitle);
            mainMenu.MusicFile = "theme1.ogg";
            mainMenu.MusicVolume = _settings.MusicVolume;
            mainMenu.MusicVolumeChanged = _actions.SaveMusicVolume;
            _menu.Register(mainMenu);

            _menu.Register(BuildMultiplayerMenu());
            _menu.Register(BuildMultiplayerServersMenu());
            _menu.Register(BuildMultiplayerLobbyMenu());

            _menu.Register(BuildTrackTypeMenu("time_trial_type", RaceMode.TimeTrial));
            _menu.Register(BuildTrackTypeMenu("single_race_type", RaceMode.SingleRace));

            _menu.Register(BuildTrackMenu("time_trial_tracks_race", RaceMode.TimeTrial, TrackCategory.RaceTrack));
            _menu.Register(BuildTrackMenu("time_trial_tracks_adventure", RaceMode.TimeTrial, TrackCategory.StreetAdventure));
            _menu.Register(BuildCustomTrackMenu("time_trial_tracks_custom", RaceMode.TimeTrial));
            _menu.Register(BuildTrackMenu("single_race_tracks_race", RaceMode.SingleRace, TrackCategory.RaceTrack));
            _menu.Register(BuildTrackMenu("single_race_tracks_adventure", RaceMode.SingleRace, TrackCategory.StreetAdventure));
            _menu.Register(BuildCustomTrackMenu("single_race_tracks_custom", RaceMode.SingleRace));

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

        private void PrepareMode(RaceMode mode)
        {
            _setup.Mode = mode;
            _setup.ClearSelection();
        }

        private void CompleteTransmission(RaceMode mode, TransmissionMode transmission)
        {
            _setup.Transmission = transmission;
            _actions.QueueRaceStart(mode);
        }

        private MenuScreen BuildTrackTypeMenu(string id, RaceMode mode)
        {
            var items = new List<MenuItem>
            {
                new MenuItem("Race track", MenuAction.None, nextMenuId: TrackMenuId(mode, TrackCategory.RaceTrack), onActivate: () => _setup.TrackCategory = TrackCategory.RaceTrack),
                new MenuItem("Street adventure", MenuAction.None, nextMenuId: TrackMenuId(mode, TrackCategory.StreetAdventure), onActivate: () => _setup.TrackCategory = TrackCategory.StreetAdventure),
                new MenuItem("Custom track", MenuAction.None, nextMenuId: TrackMenuId(mode, TrackCategory.CustomTrack), onActivate: () =>
                {
                    _setup.TrackCategory = TrackCategory.CustomTrack;
                    RefreshCustomTrackMenu(mode);
                }),
                new MenuItem("Random", MenuAction.None, onActivate: () => PushRandomTrackType(mode)),
                BackItem()
            };
            var title = "Choose track type";
            return _menu.CreateMenu(id, items, title);
        }

        private MenuScreen BuildMultiplayerMenu()
        {
            var items = new List<MenuItem>
            {
                new MenuItem("Join a game on the local network", MenuAction.None, onActivate: _actions.StartServerDiscovery),
                new MenuItem("Enter the IP address or domain manually", MenuAction.None, onActivate: _actions.BeginManualServerEntry),
                BackItem()
            };
            return _menu.CreateMenu("multiplayer", items);
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
                new MenuItem("Create a new game", MenuAction.None, onActivate: _actions.SpeakNotImplemented),
                new MenuItem("Join an existing game", MenuAction.None, onActivate: _actions.SpeakNotImplemented),
                new MenuItem("Who is online", MenuAction.None, onActivate: _actions.SpeakNotImplemented),
                new MenuItem("Options", MenuAction.None, nextMenuId: "options_main"),
                new MenuItem("Disconnect", MenuAction.None, onActivate: _actions.DisconnectFromServer)
            };
            return _menu.CreateMenu("multiplayer_lobby", items, string.Empty);
        }

        private MenuScreen BuildTrackMenu(string id, RaceMode mode, TrackCategory category)
        {
            var items = new List<MenuItem>();
            var trackList = TrackList.GetTracks(category);
            var nextMenuId = VehicleMenuId(mode);

            foreach (var track in trackList)
            {
                var key = track.Key;
                items.Add(new MenuItem(track.Display, MenuAction.None, nextMenuId: nextMenuId, onActivate: () => _selection.SelectTrack(category, key)));
            }

            items.Add(new MenuItem("Random", MenuAction.None, nextMenuId: nextMenuId, onActivate: () => _selection.SelectRandomTrack(category)));
            items.Add(BackItem());
            return _menu.CreateMenu(id, items, "Select a track");
        }

        private MenuScreen BuildCustomTrackMenu(string id, RaceMode mode)
        {
            var items = BuildCustomTrackItems(mode);
            var title = "Select a custom track";
            return _menu.CreateMenu(id, items, title);
        }

        private void RefreshCustomTrackMenu(RaceMode mode)
        {
            var id = TrackMenuId(mode, TrackCategory.CustomTrack);
            _menu.UpdateItems(id, BuildCustomTrackItems(mode));
        }

        private List<MenuItem> BuildCustomTrackItems(RaceMode mode)
        {
            var items = new List<MenuItem>();
            var nextMenuId = VehicleMenuId(mode);
            var customTracks = _selection.GetCustomTrackInfo();
            if (customTracks.Count == 0)
            {
                items.Add(new MenuItem("No custom tracks found", MenuAction.None));
                items.Add(BackItem());
                return items;
            }

            foreach (var track in customTracks)
            {
                var key = track.Key;
                items.Add(new MenuItem(track.Display, MenuAction.None, nextMenuId: nextMenuId,
                    onActivate: () => _selection.SelectTrack(TrackCategory.CustomTrack, key)));
            }

            items.Add(new MenuItem("Random", MenuAction.None, nextMenuId: nextMenuId, onActivate: _selection.SelectRandomCustomTrack));
            items.Add(BackItem());
            return items;
        }

        private MenuScreen BuildVehicleMenu(string id, RaceMode mode)
        {
            var items = new List<MenuItem>();
            var nextMenuId = TransmissionMenuId(mode);

            for (var i = 0; i < VehicleCatalog.VehicleCount; i++)
            {
                var index = i;
                var name = VehicleCatalog.Vehicles[i].Name;
                items.Add(new MenuItem(name, MenuAction.None, nextMenuId: nextMenuId, onActivate: () => _selection.SelectVehicle(index)));
            }

            foreach (var file in _selection.GetCustomVehicleFiles())
            {
                var filePath = file;
                var fileName = Path.GetFileNameWithoutExtension(filePath) ?? "Custom vehicle";
                items.Add(new MenuItem(fileName, MenuAction.None, nextMenuId: nextMenuId, onActivate: () => _selection.SelectCustomVehicle(filePath)));
            }

            items.Add(new MenuItem("Random", MenuAction.None, nextMenuId: nextMenuId, onActivate: _selection.SelectRandomVehicle));
            items.Add(BackItem());
            var title = "Select a vehicle";
            return _menu.CreateMenu(id, items, title);
        }

        private MenuScreen BuildTransmissionMenu(string id, RaceMode mode)
        {
            var items = new List<MenuItem>
            {
                new MenuItem("Automatic", MenuAction.None, onActivate: () => CompleteTransmission(mode, TransmissionMode.Automatic)),
                new MenuItem("Manual", MenuAction.None, onActivate: () => CompleteTransmission(mode, TransmissionMode.Manual)),
                new MenuItem("Random", MenuAction.None, onActivate: () => CompleteTransmission(mode, Algorithm.RandomInt(2) == 0 ? TransmissionMode.Automatic : TransmissionMode.Manual)),
                BackItem()
            };
            var title = "Select transmission mode";
            return _menu.CreateMenu(id, items, title);
        }

        private MenuScreen BuildOptionsMenu()
        {
            var items = new List<MenuItem>
            {
                new MenuItem("Game settings", MenuAction.None, nextMenuId: "options_game"),
                new MenuItem("Controls", MenuAction.None, nextMenuId: "options_controls"),
                new MenuItem("Race settings", MenuAction.None, nextMenuId: "options_race"),
                new MenuItem("Server settings", MenuAction.None, nextMenuId: "options_server"),
                new MenuItem("Restore default settings", MenuAction.None, nextMenuId: "options_restore"),
                BackItem()
            };
            return _menu.CreateMenu("options_main", items);
        }

        private MenuScreen BuildOptionsGameSettingsMenu()
        {
            var items = new List<MenuItem>
            {
                new MenuItem(() => $"Include custom tracks in randomization: {FormatOnOff(_settings.RandomCustomTracks)}", MenuAction.None, onActivate: () => _actions.ToggleSetting(() => _settings.RandomCustomTracks = !_settings.RandomCustomTracks)),
                new MenuItem(() => $"Include custom vehicles in randomization: {FormatOnOff(_settings.RandomCustomVehicles)}", MenuAction.None, onActivate: () => _actions.ToggleSetting(() => _settings.RandomCustomVehicles = !_settings.RandomCustomVehicles)),
                new MenuItem(() => $"Enable HRTF Three-D audio: {FormatOnOff(_settings.ThreeDSound)}", MenuAction.None, onActivate: () => _actions.ToggleSetting(() => _settings.ThreeDSound = !_settings.ThreeDSound)),
                new MenuItem(() => $"Units: {UnitsLabel(_settings.Units)}", MenuAction.None, onActivate: () => _actions.ToggleSetting(() => _settings.Units = _settings.Units == UnitSystem.Metric ? UnitSystem.Imperial : UnitSystem.Metric)),
                new MenuItem("Recalibrate screen reader rate", MenuAction.None, onActivate: _actions.RecalibrateScreenReaderRate),
                BackItem()
            };
            return _menu.CreateMenu("options_game", items);
        }

        private MenuScreen BuildOptionsServerSettingsMenu()
        {
            var items = new List<MenuItem>
            {
                new MenuItem(() => $"Custom server port: {FormatServerPort(_settings.ServerPort)}", MenuAction.None, onActivate: _actions.BeginServerPortEntry),
                BackItem()
            };
            return _menu.CreateMenu("options_server", items);
        }

        private MenuScreen BuildOptionsControlsMenu()
        {
            var items = new List<MenuItem>
            {
                new MenuItem(() => $"Select device: {DeviceLabel(_settings.DeviceMode)}", MenuAction.None, nextMenuId: "options_controls_device"),
                new MenuItem(() => $"Force feedback: {FormatOnOff(_settings.ForceFeedback)}", MenuAction.None, onActivate: () => _actions.ToggleSetting(() => _settings.ForceFeedback = !_settings.ForceFeedback)),
                new MenuItem("Map keyboard keys", MenuAction.None, nextMenuId: "options_controls_keyboard"),
                new MenuItem("Map joystick keys", MenuAction.None, nextMenuId: "options_controls_joystick"),
                BackItem()
            };
            return _menu.CreateMenu("options_controls", items);
        }

        private MenuScreen BuildOptionsControlsDeviceMenu()
        {
            var items = new List<MenuItem>
            {
                new MenuItem("Keyboard", MenuAction.Back, onActivate: () => _actions.SetDevice(InputDeviceMode.Keyboard)),
                new MenuItem("Joystick", MenuAction.Back, onActivate: () => _actions.SetDevice(InputDeviceMode.Joystick)),
                new MenuItem("Both", MenuAction.Back, onActivate: () => _actions.SetDevice(InputDeviceMode.Both)),
                BackItem()
            };
            return _menu.CreateMenu("options_controls_device", items, "Select input device");
        }

        private MenuScreen BuildOptionsControlsKeyboardMenu()
        {
            var items = BuildMappingItems(InputMappingMode.Keyboard);
            return _menu.CreateMenu("options_controls_keyboard", items);
        }

        private MenuScreen BuildOptionsControlsJoystickMenu()
        {
            var items = BuildMappingItems(InputMappingMode.Joystick);
            return _menu.CreateMenu("options_controls_joystick", items);
        }

        private List<MenuItem> BuildMappingItems(InputMappingMode mode)
        {
            var items = new List<MenuItem>();
            foreach (var action in _raceInput.KeyMap.Actions)
            {
                var definition = action;
                items.Add(new MenuItem(
                    () => $"{definition.Label}: {_actions.FormatMappingValue(definition.Action, mode)}",
                    MenuAction.None,
                    onActivate: () => _actions.BeginMapping(mode, definition.Action)));
            }
            items.Add(BackItem());
            return items;
        }

        private MenuScreen BuildOptionsRaceSettingsMenu()
        {
            var items = new List<MenuItem>
            {
                new MenuItem(() => $"Copilot: {CopilotLabel(_settings.Copilot)}", MenuAction.None, nextMenuId: "options_race_copilot"),
                new MenuItem(() => $"Curve announcements: {CurveLabel(_settings.CurveAnnouncement)}", MenuAction.None, onActivate: _actions.ToggleCurveAnnouncements),
                new MenuItem(() => $"Automatic race information: {AutomaticInfoLabel(_settings.AutomaticInfo)}", MenuAction.None, nextMenuId: "options_race_info"),
                new MenuItem(() => $"Number of laps: {_settings.NrOfLaps}", MenuAction.None, nextMenuId: "options_race_laps"),
                new MenuItem(() => $"Number of computer players: {_settings.NrOfComputers}", MenuAction.None, nextMenuId: "options_race_computers"),
                new MenuItem(() => $"Single race difficulty: {DifficultyLabel(_settings.Difficulty)}", MenuAction.None, nextMenuId: "options_race_difficulty"),
                BackItem()
            };
            return _menu.CreateMenu("options_race", items);
        }

        private MenuScreen BuildOptionsAutomaticInfoMenu()
        {
            var items = new List<MenuItem>
            {
                new MenuItem("Off", MenuAction.Back, onActivate: () => _actions.UpdateSetting(() => _settings.AutomaticInfo = AutomaticInfoMode.Off)),
                new MenuItem("Laps only", MenuAction.Back, onActivate: () => _actions.UpdateSetting(() => _settings.AutomaticInfo = AutomaticInfoMode.LapsOnly)),
                new MenuItem("On", MenuAction.Back, onActivate: () => _actions.UpdateSetting(() => _settings.AutomaticInfo = AutomaticInfoMode.On)),
                BackItem()
            };
            return _menu.CreateMenu("options_race_info", items, "Automatic information controls the automatic announcements reported to you during the race, such as lab numbers and player positions.");
        }

        private MenuScreen BuildOptionsCopilotMenu()
        {
            var items = new List<MenuItem>
            {
                new MenuItem("Off", MenuAction.Back, onActivate: () => _actions.UpdateSetting(() => _settings.Copilot = CopilotMode.Off)),
                new MenuItem("Curves only", MenuAction.Back, onActivate: () => _actions.UpdateSetting(() => _settings.Copilot = CopilotMode.CurvesOnly)),
                new MenuItem("All", MenuAction.Back, onActivate: () => _actions.UpdateSetting(() => _settings.Copilot = CopilotMode.All)),
                BackItem()
            };
            return _menu.CreateMenu("options_race_copilot", items, "What information should the copilot report to you during the race.");
        }

        private MenuScreen BuildOptionsLapsMenu()
        {
            var items = new List<MenuItem>();
            for (var laps = 1; laps <= 16; laps++)
            {
                var value = laps;
                items.Add(new MenuItem(laps.ToString(), MenuAction.Back, onActivate: () => _actions.UpdateSetting(() => _settings.NrOfLaps = value)));
            }
            items.Add(BackItem());
            return _menu.CreateMenu("options_race_laps", items, "How many labs should the session be. This applys to single race, time trial and multiPlayer modes.");
        }

        private MenuScreen BuildOptionsComputersMenu()
        {
            var items = new List<MenuItem>();
            for (var count = 1; count <= 7; count++)
            {
                var value = count;
                items.Add(new MenuItem(count.ToString(), MenuAction.Back, onActivate: () => _actions.UpdateSetting(() => _settings.NrOfComputers = value)));
            }
            items.Add(BackItem());
            return _menu.CreateMenu("options_race_computers", items, "Number of computer players");
        }

        private MenuScreen BuildOptionsDifficultyMenu()
        {
            var items = new List<MenuItem>
            {
                new MenuItem("Easy", MenuAction.Back, onActivate: () => _actions.UpdateSetting(() => _settings.Difficulty = RaceDifficulty.Easy)),
                new MenuItem("Normal", MenuAction.Back, onActivate: () => _actions.UpdateSetting(() => _settings.Difficulty = RaceDifficulty.Normal)),
                new MenuItem("Hard", MenuAction.Back, onActivate: () => _actions.UpdateSetting(() => _settings.Difficulty = RaceDifficulty.Hard)),
                BackItem()
            };
            return _menu.CreateMenu("options_race_difficulty", items);
        }

        private MenuScreen BuildOptionsRestoreMenu()
        {
            var items = new List<MenuItem>
            {
                new MenuItem("Yes", MenuAction.Back, onActivate: _actions.RestoreDefaults),
                new MenuItem("No", MenuAction.Back),
                BackItem()
            };
            return _menu.CreateMenu("options_restore", items, "Are you sure you would like to restore all settings to their default values?");
        }

        private void PushRandomTrackType(RaceMode mode)
        {
            var customTracks = _selection.GetCustomTrackInfo();
            var includeCustom = customTracks.Count > 0;
            var rollMax = includeCustom ? 3 : 2;
            var roll = Algorithm.RandomInt(rollMax);
            var category = roll switch
            {
                0 => TrackCategory.RaceTrack,
                1 => TrackCategory.StreetAdventure,
                _ => TrackCategory.CustomTrack
            };

            _setup.TrackCategory = category;
            if (category == TrackCategory.CustomTrack)
                RefreshCustomTrackMenu(mode);
            _menu.Push(TrackMenuId(mode, category));
        }

        private static string TrackMenuId(RaceMode mode, TrackCategory category)
        {
            var prefix = mode == RaceMode.TimeTrial ? "time_trial" : "single_race";
            return category switch
            {
                TrackCategory.RaceTrack => $"{prefix}_tracks_race",
                TrackCategory.StreetAdventure => $"{prefix}_tracks_adventure",
                _ => $"{prefix}_tracks_custom"
            };
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
            return new MenuItem("Go back", MenuAction.Back);
        }

        private string MainMenuTitle()
        {
            const string keyboard = "Main Menu. Use your arrow keys to navigate the options. Press ENTER to select. Press ESCAPE to back out of any menu. Pressing HOME or END will move you to the top or bottom of a menu.";
            const string joystick = "Main Menu. Use the view finder to move through the options. Press up or down to navigate. Press right or button 1 to select. Press left to back out of any menu.";
            const string both = "Main Menu. Use your arrow keys or the view finder to move through the options. Press ENTER or right or button 1 to select. Press ESCAPE or left to back out of any menu. Pressing HOME or END will move you to the top or bottom of a menu.";

            return _settings.DeviceMode switch
            {
                InputDeviceMode.Keyboard => keyboard,
                InputDeviceMode.Joystick => joystick,
                _ => both
            };
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

        private static string UnitsLabel(UnitSystem units)
        {
            return units switch
            {
                UnitSystem.Metric => "metric",
                UnitSystem.Imperial => "imperial",
                _ => "metric"
            };
        }
    }
}
