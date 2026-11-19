using System.Collections.Generic;
using TopSpeed.Localization;
using TopSpeed.Menu;
using TopSpeed.Protocol;

namespace TopSpeed.Core.Multiplayer
{
    internal sealed partial class MultiplayerCoordinator
    {
        private void RebuildLobbyMenu()
        {
            var items = new List<MenuItem>
            {
                new MenuItem(LocalizationService.Mark("Create a new game room"), MenuAction.None, onActivate: OpenCreateRoomMenu),
                new MenuItem(LocalizationService.Mark("Join an existing game"), MenuAction.None, onActivate: OpenRoomBrowser),
                new MenuItem(LocalizationService.Mark("Who is online"), MenuAction.None, onActivate: OpenOnlinePlayersMenu),
                new MenuItem(LocalizationService.Mark("Options"), MenuAction.None, nextMenuId: "options_main"),
                new MenuItem(LocalizationService.Mark("Disconnect from server"), MenuAction.None, flags: MenuItemFlags.Close)
            };

            _menu.UpdateItems(MultiplayerMenuKeys.Lobby, items);
        }

        private void RebuildRoomControlsMenu()
        {
            var items = new List<MenuItem>();
            if (!_state.Rooms.CurrentRoom.InRoom)
            {
                items.Add(new MenuItem(LocalizationService.Mark("You are not currently inside a game room."), MenuAction.None));
                _menu.UpdateItems(MultiplayerMenuKeys.RoomControls, items);
                return;
            }

            if (_state.Rooms.CurrentRoom.IsHost)
                items.Add(new MenuItem(LocalizationService.Mark("Start the game"), MenuAction.None, onActivate: StartGame));
            if (_state.Rooms.CurrentRoom.IsHost)
                items.Add(new MenuItem(LocalizationService.Mark("Change game options"), MenuAction.None, onActivate: OpenRoomOptionsMenu));
            if (_state.Rooms.CurrentRoom.IsHost && _state.Rooms.CurrentRoom.RoomType == GameRoomType.BotsRace)
                items.Add(new MenuItem(LocalizationService.Mark("Add a bot"), MenuAction.None, onActivate: AddBotToRoom));
            if (_state.Rooms.CurrentRoom.IsHost && _state.Rooms.CurrentRoom.RoomType == GameRoomType.BotsRace)
                items.Add(new MenuItem(LocalizationService.Mark("Remove a bot"), MenuAction.None, onActivate: RemoveLastBotFromRoom));
            items.Add(new MenuItem(LocalizationService.Mark("View game rules"), MenuAction.None, onActivate: AnnounceCurrentRoomGameRules));
            items.Add(new MenuItem(LocalizationService.Mark("Who is currently present in this game room"), MenuAction.None, onActivate: OpenRoomPlayersMenu));
            items.Add(new MenuItem(LocalizationService.Mark("Leave this game room"), MenuAction.None, flags: MenuItemFlags.Close));
            _menu.UpdateItems(MultiplayerMenuKeys.RoomControls, items);
        }
    }
}
