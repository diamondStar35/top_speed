using System.Collections.Generic;
using TopSpeed.Input;

namespace TopSpeed.Core.Multiplayer
{
    internal sealed partial class MultiplayerCoordinator
    {
        public void OpenSavedServersManager()
        {
            _savedServersFlow.OpenSavedServersManager();
        }

        internal void OpenSavedServersManagerCore()
        {
            RebuildSavedServersMenu();
            _menu.Push(MultiplayerMenuKeys.SavedServers);
        }

        private IReadOnlyList<SavedServerEntry> SavedServers => _settings.SavedServers ?? (_settings.SavedServers = new List<SavedServerEntry>());
    }
}



