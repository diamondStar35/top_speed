using System;
using System.Collections.Generic;
using Key = TopSpeed.Input.InputKey;
using TopSpeed.Core;
using TopSpeed.Input;
using TopSpeed.Shortcuts;

using TopSpeed.Localization;
namespace TopSpeed.Menu
{
    internal sealed partial class MenuRegistry
    {
        private const string PreviousChatCategoryShortcutActionId = "chat_prev_category";
        private const string NextChatCategoryShortcutActionId = "chat_next_category";

        private readonly MenuManager _menu;
        private readonly DriveSettings _settings;
        private readonly DriveSetup _setup;
        private readonly DriveInput _driveInput;
        private readonly DriveSelection _selection;
        private readonly IMenuUiActions _ui;
        private readonly IMenuDriveActions _driveActions;
        private readonly IMenuServerActions _server;
        private readonly IMenuSettingsActions _settingsActions;
        private readonly IMenuAudioActions _audio;
        private readonly IMenuMappingActions _mapping;
        private readonly IReadOnlyList<string> _menuSoundPresets;
        private readonly MenuView _sharedLobbyChatScreen;

        public MenuRegistry(
            MenuManager menu,
            DriveSettings settings,
            DriveSetup setup,
            DriveInput driveInput,
            DriveSelection selection,
            IMenuUiActions ui,
            IMenuDriveActions driveActions,
            IMenuServerActions server,
            IMenuSettingsActions settingsActions,
            IMenuAudioActions audio,
            IMenuMappingActions mapping)
        {
            _menu = menu ?? throw new ArgumentNullException(nameof(menu));
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _setup = setup ?? throw new ArgumentNullException(nameof(setup));
            _driveInput = driveInput ?? throw new ArgumentNullException(nameof(driveInput));
            _selection = selection ?? throw new ArgumentNullException(nameof(selection));
            _ui = ui ?? throw new ArgumentNullException(nameof(ui));
            _driveActions = driveActions ?? throw new ArgumentNullException(nameof(driveActions));
            _server = server ?? throw new ArgumentNullException(nameof(server));
            _settingsActions = settingsActions ?? throw new ArgumentNullException(nameof(settingsActions));
            _audio = audio ?? throw new ArgumentNullException(nameof(audio));
            _mapping = mapping ?? throw new ArgumentNullException(nameof(mapping));
            _menuSoundPresets = LoadMenuSoundPresets();
            _sharedLobbyChatScreen = new MenuView(
                "shared_lobby_chat",
                new[] { new MenuItem(LocalizationService.Mark("No messages yet."), MenuAction.None) },
                title: LocalizationService.Mark("History"),
                spec: ScreenSpec.KeepSelectionSilent);
        }

        public void RegisterAll()
        {
            RegisterSharedLobbyChatShortcuts();
            RegisterMainMenu();
            _menu.Register(BuildHelpMenu());

            _menu.Register(BuildMultiplayerMenu());
            _menu.Register(BuildMultiplayerServersMenu());
            _menu.Register(BuildMultiplayerSavedServersMenu());
            _menu.Register(BuildMultiplayerSavedServerFormMenu());
            _menu.Register(BuildMultiplayerLobbyMenu());
            _menu.Register(BuildMultiplayerRoomsMenu());
            _menu.Register(BuildMultiplayerCreateRoomMenu());
            _menu.Register(BuildMultiplayerRoomControlsMenu());
            _menu.Register(BuildMultiplayerRoomPlayersMenu());
            _menu.Register(BuildMultiplayerOnlinePlayersMenu());
            _menu.Register(BuildMultiplayerRoomOptionsMenu());
            _menu.Register(BuildMultiplayerRoomGameRulesMenu());
            _menu.Register(BuildMultiplayerRoomTrackTypeMenu());
            _menu.Register(BuildMultiplayerRoomTrackRaceMenu());
            _menu.Register(BuildMultiplayerRoomTrackAdventureMenu());
            _menu.Register(BuildMultiplayerRoomTrackCustomMenu());
            _menu.Register(BuildMultiplayerRoomTrackLocalCustomMenu());
            _menu.Register(BuildMultiplayerLoadoutVehicleMenu());
            _menu.Register(BuildMultiplayerLoadoutVehicleCustomMenu());
            _menu.Register(BuildMultiplayerLoadoutTransmissionMenu());

            _menu.Register(BuildTrackTypeMenu("time_trial_type", DriveMode.TimeTrial));
            _menu.Register(BuildTrackTypeMenu("single_race_type", DriveMode.SingleRace));

            _menu.Register(BuildTrackMenu("time_trial_tracks_race", DriveMode.TimeTrial, TrackCategory.RaceTrack));
            _menu.Register(BuildTrackMenu("time_trial_tracks_adventure", DriveMode.TimeTrial, TrackCategory.StreetAdventure));
            _menu.Register(BuildCustomTrackMenu("time_trial_tracks_custom", DriveMode.TimeTrial));
            _menu.Register(BuildTrackMenu("single_race_tracks_race", DriveMode.SingleRace, TrackCategory.RaceTrack));
            _menu.Register(BuildTrackMenu("single_race_tracks_adventure", DriveMode.SingleRace, TrackCategory.StreetAdventure));
            _menu.Register(BuildCustomTrackMenu("single_race_tracks_custom", DriveMode.SingleRace));

            _menu.Register(BuildVehicleMenu("time_trial_vehicles", DriveMode.TimeTrial));
            _menu.Register(BuildCustomVehicleMenu("time_trial_vehicles_custom", DriveMode.TimeTrial));
            _menu.Register(BuildVehicleMenu("single_race_vehicles", DriveMode.SingleRace));
            _menu.Register(BuildCustomVehicleMenu("single_race_vehicles_custom", DriveMode.SingleRace));

            _menu.Register(BuildTransmissionMenu("time_trial_transmission", DriveMode.TimeTrial));
            _menu.Register(BuildTransmissionMenu("single_race_transmission", DriveMode.SingleRace));

            _menu.Register(BuildOptionsMenu());
            _menu.Register(BuildOptionsGameSettingsMenu());
            _menu.Register(BuildOptionsSpeechSettingsMenu());
            _menu.Register(BuildOptionsAudioSettingsMenu());
            _menu.Register(BuildOptionsVolumeSettingsMenu());
            _menu.Register(BuildOptionsControlsMenu());
            _menu.Register(BuildOptionsControlsDeviceMenu());
            _menu.Register(BuildOptionsControlsKeyboardMenu());
            _menu.Register(BuildOptionsControlsControllerMenu());
            _menu.Register(BuildOptionsControlsShortcutGroupsMenu());
            _menu.Register(BuildOptionsControlsShortcutBindingsMenu());
            _menu.Register(BuildOptionsDriveSettingsMenu());
            _menu.Register(BuildOptionsServerSettingsMenu());
        }

        private void RegisterSharedLobbyChatShortcuts()
        {
            _menu.RegisterShortcutAction(
                PreviousChatCategoryShortcutActionId,
                LocalizationService.Mark("Previous chat category"),
                LocalizationService.Mark("Switches chat history to the previous category in the shared lobby chat view."),
                Key.Left,
                ShortcutModifiers.None,
                _server.PreviousChatCategory);
            _menu.RegisterShortcutAction(
                NextChatCategoryShortcutActionId,
                LocalizationService.Mark("Next chat category"),
                LocalizationService.Mark("Switches chat history to the next category in the shared lobby chat view."),
                Key.Right,
                ShortcutModifiers.None,
                _server.NextChatCategory);
            _menu.SetViewShortcutActions(
                _sharedLobbyChatScreen.Id,
                new[] { PreviousChatCategoryShortcutActionId, NextChatCategoryShortcutActionId },
                LocalizationService.Mark("Shared lobby chat"));
        }

        private void RegisterMainMenu()
        {
            var mainMenu = _menu.CreateMenu("main", new[]
            {
                new MenuItem(LocalizationService.Mark("Quick start"), MenuAction.QuickStart),
                new MenuItem(LocalizationService.Mark("Time trial"), MenuAction.None, nextMenuId: "time_trial_type", onActivate: () => PrepareMode(DriveMode.TimeTrial)),
                new MenuItem(LocalizationService.Mark("Single race"), MenuAction.None, nextMenuId: "single_race_type", onActivate: () => PrepareMode(DriveMode.SingleRace)),
                new MenuItem(LocalizationService.Mark("MultiPlayer game"), MenuAction.None, nextMenuId: "multiplayer"),
                new MenuItem(LocalizationService.Mark("Options"), MenuAction.None, nextMenuId: "options_main"),
                new MenuItem(LocalizationService.Mark("Help"), MenuAction.None, nextMenuId: "help"),
                new MenuItem(LocalizationService.Mark("Check for updates"), MenuAction.None, onActivate: _settingsActions.CheckForUpdates),
                new MenuItem(LocalizationService.Mark("About"), MenuAction.None, onActivate: _settingsActions.ShowAboutDialog),
                new MenuItem(LocalizationService.Mark("Exit Game"), MenuAction.Exit)
            }, LocalizationService.Mark("Main menu"), titleProvider: MainMenuTitle);

            mainMenu.MusicFile = "theme1.ogg";
            mainMenu.MusicVolume = _settings.MusicVolume;
            mainMenu.MusicVolumeChanged = _audio.SaveMusicVolume;
            _menu.Register(mainMenu);
        }

        private MenuScreen BuildHelpMenu()
        {
            return _menu.CreateMenu("help", new[]
            {
                new MenuItem(LocalizationService.Mark("Game guide"), MenuAction.None, onActivate: _settingsActions.OpenGameGuide),
                new MenuItem(LocalizationService.Mark("Track creation guide"), MenuAction.None, onActivate: _settingsActions.OpenTrackCreationGuide),
                new MenuItem(LocalizationService.Mark("Vehicle creation guide"), MenuAction.None, onActivate: _settingsActions.OpenVehicleCreationGuide),
                new MenuItem(LocalizationService.Mark("What's new"), MenuAction.None, onActivate: _settingsActions.ShowLatestChanges),
                new MenuItem(LocalizationService.Mark("Change log file"), MenuAction.None, onActivate: _settingsActions.OpenChangeLogFile)
            }, string.Empty, spec: ScreenSpec.BackSilent);
        }
    }
}








