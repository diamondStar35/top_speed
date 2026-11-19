using System.Collections.Generic;

namespace TopSpeed.Drive.Multiplayer
{
    internal sealed class ParticipantState
    {
        public ParticipantState(int maxPlayers)
        {
            RemotePlayers = new Dictionary<byte, RemotePlayer>();
            RemoteMediaTransfers = new Dictionary<byte, MediaTransfer>();
            RemoteLiveStates = new Dictionary<byte, LiveState>();
            ExpiredLivePlayers = new List<byte>();
            DisconnectedSlots = new bool[maxPlayers];
        }

        public Dictionary<byte, RemotePlayer> RemotePlayers { get; }
        public Dictionary<byte, MediaTransfer> RemoteMediaTransfers { get; }
        public Dictionary<byte, LiveState> RemoteLiveStates { get; }
        public List<byte> ExpiredLivePlayers { get; }
        public bool[] DisconnectedSlots { get; }
    }
}
