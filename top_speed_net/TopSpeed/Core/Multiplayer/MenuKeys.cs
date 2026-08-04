namespace TopSpeed.Core.Multiplayer
{
    internal readonly struct MenuId
    {
        public MenuId(string value)
        {
            Value = value ?? string.Empty;
        }

        public string Value { get; }

        public override string ToString() => Value;

        public static implicit operator string(MenuId id) => id.Value;
    }

    internal readonly struct MenuScreenId
    {
        public MenuScreenId(string value)
        {
            Value = value ?? string.Empty;
        }

        public string Value { get; }

        public override string ToString() => Value;

        public static implicit operator string(MenuScreenId id) => id.Value;
    }

    internal static class MultiplayerMenuKeys
    {
        public static readonly MenuId Lobby = new MenuId("multiplayer_lobby");
        public static readonly MenuId RoomControls = new MenuId("multiplayer_room_controls");
        public static readonly MenuId RoomOptions = new MenuId("multiplayer_room_options");
        public static readonly MenuId RoomGameRules = new MenuId("multiplayer_room_game_rules");
        public static readonly MenuId RoomTrackType = new MenuId("multiplayer_room_track_type");
        public static readonly MenuId RoomTrackRace = new MenuId("multiplayer_room_tracks_race");
        public static readonly MenuId RoomTrackAdventure = new MenuId("multiplayer_room_tracks_adventure");
        public static readonly MenuId RoomTrackCustom = new MenuId("multiplayer_room_tracks_custom");
        public static readonly MenuId RoomTrackLocalCustom = new MenuId("multiplayer_room_tracks_local_custom");
        public static readonly MenuId RoomPlayers = new MenuId("multiplayer_room_players");
        public static readonly MenuId OnlinePlayers = new MenuId("multiplayer_online_players");
        public static readonly MenuId RoomBrowser = new MenuId("multiplayer_rooms");
        public static readonly MenuId CreateRoom = new MenuId("multiplayer_create_room");
        public static readonly MenuId LoadoutVehicle = new MenuId("multiplayer_loadout_vehicle");
        public static readonly MenuId LoadoutVehicleCustom = new MenuId("multiplayer_loadout_vehicle_custom");
        public static readonly MenuId LoadoutTransmission = new MenuId("multiplayer_loadout_transmission");
        public static readonly MenuId SavedServers = new MenuId("multiplayer_saved_servers");
        public static readonly MenuId SavedServerForm = new MenuId("multiplayer_saved_server_form");
        public static readonly MenuId DiscoveredServers = new MenuId("multiplayer_servers");
    }

    internal static class MultiplayerScreenKeys
    {
        public static readonly MenuScreenId SharedLobbyChat = new MenuScreenId("shared_lobby_chat");
    }
}

