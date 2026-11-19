using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TopSpeed.Network;
using TopSpeed.Localization;

namespace TopSpeed.Core.Multiplayer
{
    internal sealed partial class MultiplayerCoordinator
    {
        public bool UpdatePendingOperations()
        {
            return _connectionFlow.UpdatePendingOperations();
        }

        internal bool UpdatePendingOperationsCore()
        {
            if (_state.Connection.ConnectTask != null)
            {
                var connectTask = _state.Connection.ConnectTask;
                if (connectTask == null)
                    return true;
                if (!connectTask.IsCompleted)
                    return true;

                var result = BuildConnectFailureResult(connectTask);
                _lifetime.CompleteConnectOperation();
                HandleConnectResult(result);
                return false;
            }

            if (_state.Connection.DiscoveryTask != null)
            {
                if (!_state.Connection.DiscoveryTask.IsCompleted)
                    return true;

                IReadOnlyList<ServerInfo> servers;
                if (_state.Connection.DiscoveryTask.IsFaulted || _state.Connection.DiscoveryTask.IsCanceled)
                    servers = Array.Empty<ServerInfo>();
                else
                    servers = _state.Connection.DiscoveryTask.GetAwaiter().GetResult();

                _lifetime.CompleteDiscoveryOperation();
                HandleDiscoveryResult(servers);
                return false;
            }

            if (UpdateTrackPackageUploadOperation())
                return true;

            return false;
        }

        private static ConnectResult BuildConnectFailureResult(Task<ConnectResult> connectTask)
        {
            if (connectTask.IsCanceled)
                return ConnectResult.CreateFail(LocalizationService.Mark("Connection attempt canceled."));

            if (connectTask.IsFaulted)
            {
                var message = connectTask.Exception?.GetBaseException().Message;
                if (!string.IsNullOrWhiteSpace(message))
                {
                    return ConnectResult.CreateFail(LocalizationService.Format(
                        LocalizationService.Mark("Connection attempt failed: {0}"),
                        message));
                }

                return ConnectResult.CreateFail(LocalizationService.Mark("Connection attempt failed."));
            }

            return connectTask.GetAwaiter().GetResult();
        }
    }
}



