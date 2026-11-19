namespace TopSpeed.Core.Multiplayer
{
    internal sealed class SavedServersFlow : ISavedServersFlow
    {
        private readonly MultiplayerCoordinator _owner;

        public SavedServersFlow(MultiplayerCoordinator owner)
        {
            _owner = owner;
        }

        public void OpenSavedServersManager()
        {
            _owner.OpenSavedServersManagerCore();
        }
    }
}


