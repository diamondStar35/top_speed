using TopSpeed.Input;

namespace TopSpeed.Core.Multiplayer
{
    internal sealed class CoordinatorSavedServersState
    {
        public SavedServerEntry Draft = new SavedServerEntry();
        public SavedServerEntry? Original;
        public int EditIndex = -1;
        public int PendingDeleteIndex = -1;
    }
}

