using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TopSpeed.Network;

namespace TopSpeed.Core.Multiplayer
{
    internal sealed class CoordinatorConnectionState
    {
        public MultiplayerClientState ClientState = MultiplayerClientState.Disconnected;
        public Task<IReadOnlyList<ServerInfo>>? DiscoveryTask;
        public CancellationTokenSource? DiscoveryCts;
        public Task<ConnectResult>? ConnectTask;
        public CancellationTokenSource? ConnectCts;
        public string PendingServerAddress = string.Empty;
        public int PendingServerPort;
        public string PendingCallSign = string.Empty;
        public bool IsPingPending;
        public long PingStartedAtTicks;
        public bool HasPendingCompatibilityResult;
        public ConnectResult PendingCompatibilityResult;
    }
}

