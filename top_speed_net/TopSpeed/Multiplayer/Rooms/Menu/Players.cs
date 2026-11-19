using System;
using System.Collections.Generic;
using TopSpeed.Localization;
using TopSpeed.Menu;

namespace TopSpeed.Core.Multiplayer
{
    internal sealed partial class MultiplayerCoordinator
    {
        private void RebuildRoomPlayersMenu()
        {
            var items = new List<MenuItem>();
            if (!_state.Rooms.CurrentRoom.InRoom)
            {
                items.Add(new MenuItem(LocalizationService.Mark("You are not currently inside a game room."), MenuAction.None));
                _menu.UpdateItems(MultiplayerMenuKeys.RoomPlayers, items);
                return;
            }

            var players = _state.Rooms.CurrentRoom.Players ?? Array.Empty<RoomParticipant>();
            if (players.Length == 0)
            {
                items.Add(new MenuItem(LocalizationService.Mark("No players are currently in this game room."), MenuAction.None));
            }
            else
            {
                foreach (var player in players)
                {
                    var name = string.IsNullOrWhiteSpace(player.Name)
                        ? LocalizationService.Format(LocalizationService.Mark("Player {0}"), player.PlayerNumber + 1)
                        : player.Name;
                    var label = player.PlayerId == _state.Rooms.CurrentRoom.HostPlayerId
                        ? LocalizationService.Format(LocalizationService.Mark("{0}, host"), name)
                        : name;
                    items.Add(new MenuItem(label, MenuAction.None));
                }
            }

            var preserveSelection = string.Equals(_menu.CurrentId, MultiplayerMenuKeys.RoomPlayers, StringComparison.Ordinal);
            _menu.UpdateItems(MultiplayerMenuKeys.RoomPlayers, items, preserveSelection);
        }

        private void OpenRoomPlayersMenu()
        {
            RebuildRoomPlayersMenu();
            _menu.Push(MultiplayerMenuKeys.RoomPlayers);
        }
    }
}
