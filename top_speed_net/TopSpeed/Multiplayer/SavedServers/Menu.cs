using System;
using System.Collections.Generic;
using TopSpeed.Input;
using TopSpeed.Menu;

using TopSpeed.Localization;
namespace TopSpeed.Core.Multiplayer
{
    internal sealed partial class MultiplayerCoordinator
    {
        private void RebuildSavedServersMenu()
        {
            var items = new List<MenuItem>();
            var servers = SavedServers;
            for (var i = 0; i < servers.Count; i++)
            {
                var index = i;
                var server = servers[i];
                var displayName = string.IsNullOrWhiteSpace(server.Name)
                    ? $"{server.Host}:{ResolveSavedServerPort(server)}"
                    : $"{server.Name}, {server.Host}:{ResolveSavedServerPort(server)}";

                items.Add(new MenuItem(
                    displayName,
                    MenuAction.None,
                    onActivate: () => ConnectUsingSavedServer(index),
                    actions: new[]
                    {
                        new MenuItemAction(LocalizationService.Mark("Edit"), () => OpenEditSavedServerForm(index)),
                        new MenuItemAction(LocalizationService.Mark("Delete"), () => OpenDeleteSavedServerConfirm(index))
                    }));
            }

            items.Add(new MenuItem(LocalizationService.Mark("Add a new server"), MenuAction.None, onActivate: OpenAddSavedServerForm));
            _menu.UpdateItems(MultiplayerMenuKeys.SavedServers, items, preserveSelection: true);
        }

        private void OpenAddSavedServerForm()
        {
            _state.SavedServers.EditIndex = -1;
            _state.SavedServers.Original = null;
            _state.SavedServers.Draft = new SavedServerEntry();
            RebuildSavedServerFormMenu();
            _menu.Push(MultiplayerMenuKeys.SavedServerForm);
        }

        private void OpenEditSavedServerForm(int index)
        {
            var servers = SavedServers;
            if (index < 0 || index >= servers.Count)
                return;

            var source = servers[index];
            _state.SavedServers.EditIndex = index;
            _state.SavedServers.Original = CloneSavedServer(source);
            _state.SavedServers.Draft = CloneSavedServer(source);
            RebuildSavedServerFormMenu();
            _menu.Push(MultiplayerMenuKeys.SavedServerForm);
        }

        private void ConnectUsingSavedServer(int index)
        {
            var servers = SavedServers;
            if (index < 0 || index >= servers.Count)
                return;

            var server = servers[index];
            var host = (server.Host ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(host))
            {
                _speech.Speak(LocalizationService.Mark("Saved server host is empty."));
                return;
            }

            _state.Connection.PendingServerAddress = host;
            _state.Connection.PendingServerPort = ResolveSavedServerPort(server);
            BeginCallSignInput();
        }
    }
}






