namespace TopSpeed.Core.Multiplayer
{
    internal sealed class CoordinatorState
    {
        public CoordinatorConnectionState Connection { get; } = new CoordinatorConnectionState();
        public RoomStore Rooms { get; } = new RoomStore();
        public RoomDraftState RoomDrafts { get; } = new RoomDraftState();
        public CoordinatorAudioState Audio { get; } = new CoordinatorAudioState();
        public CoordinatorChatState Chat { get; } = new CoordinatorChatState();
        public CommunicatorState Communicator { get; } = new CommunicatorState();
        public CoordinatorSavedServersState SavedServers { get; } = new CoordinatorSavedServersState();
    }
}

