namespace TopSpeed.Core.Multiplayer
{
    internal enum MultiplayerClientState
    {
        Disconnected = 0,
        Joining = 1,
        Lobby = 2,
        InRoom = 3,
        Preparing = 4,
        Racing = 5,
        Completed = 6,
        Reconnecting = 7
    }
}
