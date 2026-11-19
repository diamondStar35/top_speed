namespace TopSpeed.Core.Multiplayer
{
    internal sealed partial class MultiplayerCoordinator
    {
        public MultiplayerClientState ClientState => _state.Connection.ClientState;

        public void SetClientState(MultiplayerClientState state)
        {
            _state.Connection.ClientState = state;
            _state.Rooms.ClientState = state;
        }

        private void SyncClientStateFromRoomStore()
        {
            _state.Connection.ClientState = _state.Rooms.ClientState;
        }
    }
}
