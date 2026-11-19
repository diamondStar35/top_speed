using TopSpeed.Data;
using TopSpeed.Network;

namespace TopSpeed.Game
{
    internal sealed partial class Game
    {
        private void SetSession(MultiplayerSession session)
        {
            _session = session;
            _multiplayerCommunicatorRuntime.BindSession(session);
            ResetMultiplayerTrackPackageState();
            _multiplayerRaceRuntime.ResetSession();
            ClearQueuedMultiplayerPackets();
            session.SetPacketSink(packet => _multiplayerDispatch.Enqueue(session, packet));
        }

        private MultiplayerSession? GetSession()
        {
            return _session;
        }

        private void ClearSession()
        {
            CancelSessionReconnect();
            var session = _session;
            _multiplayerCommunicatorRuntime.BindSession(null);
            if (session != null)
                session.SetPacketSink(null);
            session?.Dispose();
            _session = null;
            ResetMultiplayerTrackPackageState();
            _multiplayerRaceRuntime.ResetSession();
            ClearQueuedMultiplayerPackets();
            _multiplayerCoordinator.OnSessionCleared();
        }

        private void ResetPendingMultiplayerState()
        {
            _multiplayerRaceRuntime.ResetPending();
        }

        private void SetMultiplayerLoadout(int vehicleIndex, bool automaticTransmission)
        {
            _multiplayerRaceRuntime.SetLoadout(vehicleIndex, automaticTransmission);
        }

        private void DisconnectFromServer()
        {
            CancelSessionReconnect();
            _multiplayerRaceRuntime.Disconnect();
            ClearSession();
            _state = AppState.Menu;
            _menu.ShowRoot("main");
            _menu.FadeInMenuMusic();
        }
    }
}

