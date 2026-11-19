using System;
using System.Collections.Generic;
using TopSpeed.Localization;
using TopSpeed.Menu;

namespace TopSpeed.Core.Multiplayer
{
    internal sealed partial class MultiplayerCoordinator
    {
        private void UpdateHistoryScreens()
        {
            var items = BuildHistoryItems(_state.Chat.History.GetCurrentEntries());
            TryUpdateChatScreen(MultiplayerMenuKeys.Lobby, items);
            TryUpdateChatScreen(MultiplayerMenuKeys.RoomControls, items);
        }

        private void TryUpdateChatScreen(string menuId, IEnumerable<MenuItem> items)
        {
            try
            {
                _menu.UpdateItems(menuId, MultiplayerScreenKeys.SharedLobbyChat, items, preserveSelection: true);
            }
            catch (InvalidOperationException)
            {
                // Menus may not be registered yet during startup.
            }
        }

        private static string? NormalizeChatMessage(string message)
        {
            var text = (message ?? string.Empty).Trim();
            return string.IsNullOrWhiteSpace(text) ? null : text;
        }

        private List<MenuItem> BuildHistoryItems(IReadOnlyList<string> entries)
        {
            var items = new List<MenuItem>();
            if (entries == null || entries.Count == 0)
            {
                var emptyText = LocalizationService.Mark("No messages yet.");
                items.Add(new MenuItem(emptyText, MenuAction.None, onActivate: () => CopyHistoryItemFromScreen(emptyText)));
                return items;
            }

            for (var i = 0; i < entries.Count; i++)
            {
                var entry = entries[i] ?? string.Empty;
                items.Add(new MenuItem(entry, MenuAction.None, onActivate: () => CopyHistoryItemFromScreen(entry)));
            }

            return items;
        }
    }
}



