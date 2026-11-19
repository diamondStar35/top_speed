using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TopSpeed.Menu;
using TopSpeed.Network;

using TopSpeed.Localization;
namespace TopSpeed.Core.Multiplayer
{
    internal sealed partial class MultiplayerCoordinator
    {
        public void StartServerDiscovery()
        {
            _connectionFlow.StartServerDiscovery();
        }

        internal void StartServerDiscoveryCore()
        {
            if (_state.Connection.DiscoveryTask != null && !_state.Connection.DiscoveryTask.IsCompleted)
                return;

            _speech.Speak(LocalizationService.Mark("Please wait. Scanning for servers on the local network."));
            var discoveryCts = _lifetime.BeginDiscoveryOperation();
            _lifetime.SetDiscoveryTask(Task.Run(async () =>
            {
                using var client = new DiscoveryClient();
                return await client.ScanAsync(ClientProtocol.DefaultDiscoveryPort, TimeSpan.FromSeconds(2), discoveryCts.Token);
            }, discoveryCts.Token));
        }

        private void HandleDiscoveryResult(IReadOnlyList<ServerInfo> servers)
        {
            if (servers == null || servers.Count == 0)
            {
                _speech.Speak(LocalizationService.Mark("No servers were found on the local network. You can enter an address manually."));
                return;
            }

            var items = new List<MenuItem>();
            foreach (var server in servers)
            {
                var info = server;
                var label = $"{info.Address}:{info.Port}";
                items.Add(new MenuItem(label, MenuAction.None, onActivate: () => SelectDiscoveredServer(info), suppressPostActivateAnnouncement: true));
            }

            _menu.UpdateItems(MultiplayerMenuKeys.DiscoveredServers, items);
            _menu.Push(MultiplayerMenuKeys.DiscoveredServers);
        }

        private void SelectDiscoveredServer(ServerInfo server)
        {
            _state.Connection.PendingServerAddress = server.Address.ToString();
            _state.Connection.PendingServerPort = server.Port;
            BeginCallSignInput();
        }
    }
}






