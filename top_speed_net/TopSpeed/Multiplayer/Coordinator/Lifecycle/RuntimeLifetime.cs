using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TopSpeed.Network;

namespace TopSpeed.Core.Multiplayer
{
    internal sealed class RuntimeLifetime
    {
        private readonly CoordinatorState _state;

        public RuntimeLifetime(CoordinatorState state)
        {
            _state = state;
        }

        public CancellationTokenSource BeginConnectOperation()
        {
            CancelConnectOperation();
            var cts = new CancellationTokenSource();
            _state.Connection.ConnectCts = cts;
            return cts;
        }

        public void SetConnectTask(Task<ConnectResult> task)
        {
            _state.Connection.ConnectTask = task;
            ObserveFault(task);
        }

        public void CompleteConnectOperation()
        {
            ObserveFault(_state.Connection.ConnectTask);
            _state.Connection.ConnectTask = null;
            DisposeToken(ref _state.Connection.ConnectCts);
        }

        public void CancelConnectOperation()
        {
            ObserveFault(_state.Connection.ConnectTask);
            _state.Connection.ConnectTask = null;
            CancelAndDisposeToken(ref _state.Connection.ConnectCts);
        }

        public CancellationTokenSource BeginDiscoveryOperation()
        {
            CancelDiscoveryOperation();
            var cts = new CancellationTokenSource();
            _state.Connection.DiscoveryCts = cts;
            return cts;
        }

        public void SetDiscoveryTask(Task<IReadOnlyList<ServerInfo>> task)
        {
            _state.Connection.DiscoveryTask = task;
            ObserveFault(task);
        }

        public void CompleteDiscoveryOperation()
        {
            ObserveFault(_state.Connection.DiscoveryTask);
            _state.Connection.DiscoveryTask = null;
            DisposeToken(ref _state.Connection.DiscoveryCts);
        }

        public void CancelDiscoveryOperation()
        {
            ObserveFault(_state.Connection.DiscoveryTask);
            _state.Connection.DiscoveryTask = null;
            CancelAndDisposeToken(ref _state.Connection.DiscoveryCts);
        }

        public CancellationToken BeginConnectingPulse()
        {
            StopConnectingPulse();
            var cts = new CancellationTokenSource();
            _state.Audio.ConnectingPulseCts = cts;
            return cts.Token;
        }

        public void StopConnectingPulse()
        {
            CancelAndDisposeToken(ref _state.Audio.ConnectingPulseCts);
        }

        public void ResetPing()
        {
            _state.Connection.IsPingPending = false;
            _state.Connection.PingStartedAtTicks = 0;
        }

        public void StopNetworkAudio()
        {
        }

        public void CancelAllOperations()
        {
            CancelConnectOperation();
            CancelDiscoveryOperation();
            StopConnectingPulse();
        }

        private static void CancelAndDisposeToken(ref CancellationTokenSource? cts)
        {
            try
            {
                cts?.Cancel();
            }
            catch
            {
            }

            DisposeToken(ref cts);
        }

        private static void DisposeToken(ref CancellationTokenSource? cts)
        {
            try
            {
                cts?.Dispose();
            }
            catch
            {
            }
            finally
            {
                cts = null;
            }
        }

        private static void ObserveFault(Task? task)
        {
            if (task == null)
                return;

            if (task.IsFaulted)
            {
                _ = task.Exception;
                return;
            }

            _ = task.ContinueWith(
                static t => _ = t.Exception,
                CancellationToken.None,
                TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously,
                TaskScheduler.Default);
        }
    }
}

