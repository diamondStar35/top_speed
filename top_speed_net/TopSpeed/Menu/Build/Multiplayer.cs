using System.Collections.Generic;

using TopSpeed.Localization;
namespace TopSpeed.Menu
{
    internal sealed partial class MenuRegistry
    {
        private MenuScreen BuildMultiplayerMenu()
        {
            var items = new List<MenuItem>
            {
                new MenuItem(LocalizationService.Mark("Join a game on the local network"), MenuAction.None, onActivate: _server.StartServerDiscovery),
                new MenuItem(LocalizationService.Mark("Manage saved servers"), MenuAction.None, onActivate: _server.OpenSavedServersManager),
                new MenuItem(LocalizationService.Mark("Enter the IP address or domain manually"), MenuAction.None, onActivate: _server.BeginManualServerEntry)
            };
            return BackMenu("multiplayer", items);
        }

        private MenuScreen BuildMultiplayerServersMenu()
        {
            return EmptyBackMenu("multiplayer_servers", LocalizationService.Mark("Available servers"));
        }

        private MenuScreen BuildMultiplayerLobbyMenu()
        {
            var items = new List<MenuItem>
            {
                new MenuItem(LocalizationService.Mark("Create a new game"), MenuAction.None, onActivate: _ui.SpeakNotImplemented),
                new MenuItem(LocalizationService.Mark("Join an existing game"), MenuAction.None, onActivate: _ui.SpeakNotImplemented),
                new MenuItem(LocalizationService.Mark("Options"), MenuAction.None, nextMenuId: "options_main"),
                new MenuItem(LocalizationService.Mark("Disconnect"), MenuAction.None, flags: MenuItemFlags.Close)
            };
            return ChatMenu(
                "multiplayer_lobby",
                "lobby_main",
                items,
                LocalizationService.Mark("Multiplayer lobby"),
                viewSpec: ScreenSpec.Silent);
        }

        private MenuScreen BuildMultiplayerSavedServersMenu()
        {
            return EmptyBackMenu("multiplayer_saved_servers", string.Empty);
        }

        private MenuScreen BuildMultiplayerSavedServerFormMenu()
        {
            return EmptyBackMenu("multiplayer_saved_server_form", LocalizationService.Mark("Server details"));
        }

        private MenuScreen BuildMultiplayerRoomsMenu()
        {
            return EmptyBackMenu("multiplayer_rooms", LocalizationService.Mark("Available game rooms"));
        }

        private MenuScreen BuildMultiplayerCreateRoomMenu()
        {
            return EmptyMenu("multiplayer_create_room");
        }

        private MenuScreen BuildMultiplayerRoomControlsMenu()
        {
            return ChatMenu(
                "multiplayer_room_controls",
                "room_controls_main",
                EmptyItems,
                LocalizationService.Mark("Room controls"),
                viewSpec: ScreenSpec.Silent);
        }

        private MenuScreen BuildMultiplayerRoomPlayersMenu()
        {
            return EmptyBackMenu("multiplayer_room_players", LocalizationService.Mark("Players in room"));
        }

        private MenuScreen BuildMultiplayerOnlinePlayersMenu()
        {
            return EmptyBackMenu("multiplayer_online_players", LocalizationService.Mark("Online players"));
        }

        private MenuScreen BuildMultiplayerRoomOptionsMenu()
        {
            return EmptyMenu("multiplayer_room_options");
        }

        private MenuScreen BuildMultiplayerRoomGameRulesMenu()
        {
            return EmptyBackMenu("multiplayer_room_game_rules", string.Empty);
        }

        private MenuScreen BuildMultiplayerRoomTrackTypeMenu()
        {
            return EmptyBackMenu("multiplayer_room_track_type", LocalizationService.Mark("Choose track type"));
        }

        private MenuScreen BuildMultiplayerRoomTrackRaceMenu()
        {
            return EmptyBackMenu("multiplayer_room_tracks_race", LocalizationService.Mark("Select a track"));
        }

        private MenuScreen BuildMultiplayerRoomTrackAdventureMenu()
        {
            return EmptyBackMenu("multiplayer_room_tracks_adventure", LocalizationService.Mark("Select a track"));
        }

        private MenuScreen BuildMultiplayerRoomTrackCustomMenu()
        {
            return EmptyBackMenu("multiplayer_room_tracks_custom", LocalizationService.Mark("Select a custom track"));
        }

        private MenuScreen BuildMultiplayerRoomTrackLocalCustomMenu()
        {
            return EmptyBackMenu("multiplayer_room_tracks_local_custom", LocalizationService.Mark("Upload a local custom track"));
        }

        private MenuScreen BuildMultiplayerLoadoutVehicleMenu()
        {
            return EmptyBackMenu("multiplayer_loadout_vehicle", LocalizationService.Mark("Choose your vehicle"));
        }

        private MenuScreen BuildMultiplayerLoadoutVehicleCustomMenu()
        {
            return EmptyBackMenu("multiplayer_loadout_vehicle_custom", LocalizationService.Mark("Select a custom vehicle"));
        }

        private MenuScreen BuildMultiplayerLoadoutTransmissionMenu()
        {
            return EmptyBackMenu("multiplayer_loadout_transmission", LocalizationService.Mark("Choose your transmission mode"));
        }
    }
}





