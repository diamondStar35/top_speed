using TopSpeed.Network;

namespace TopSpeed.Game
{
    internal sealed partial class Game
    {
        private void ProcessMultiplayerPackets()
        {
            _multiplayerDispatch.Process();
        }

        private void ClearQueuedMultiplayerPackets()
        {
            _multiplayerDispatch.Clear();
        }
    }
}

