namespace TopSpeed.Core.Multiplayer
{
    internal sealed class ConnectionFlow : IConnectionFlow
    {
        private readonly MultiplayerCoordinator _owner;

        public ConnectionFlow(MultiplayerCoordinator owner)
        {
            _owner = owner;
        }

        public void BeginManualServerEntry()
        {
            _owner.BeginManualServerEntryCore();
        }

        public void BeginServerPortEntry()
        {
            _owner.BeginServerPortEntryCore();
        }

        public void BeginDefaultCallSignEntry()
        {
            _owner.BeginDefaultCallSignEntryCore();
        }

        public void StartServerDiscovery()
        {
            _owner.StartServerDiscoveryCore();
        }

        public bool UpdatePendingOperations()
        {
            return _owner.UpdatePendingOperationsCore();
        }

        public void HandlePingReply(long receivedUtcTicks)
        {
            _owner.HandlePingReplyCore(receivedUtcTicks);
        }
    }
}


