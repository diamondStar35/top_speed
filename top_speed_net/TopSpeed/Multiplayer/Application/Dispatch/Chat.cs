using TopSpeed.Network;
using TopSpeed.Protocol;

namespace TopSpeed.Game
{
    internal sealed partial class Game
    {
        private sealed partial class MultiplayerDispatch
        {
            private void RegisterChat()
            {
                _reg.Add("chat", Command.ProtocolMessage, HandleProtocolMessage);
            }

            private bool HandleProtocolMessage(IncomingPacket packet)
            {
                if (ClientPacketSerializer.TryReadProtocolMessage(packet.Payload, out var message))
                    _owner._multiplayerCoordinator.HandleProtocolMessage(message);
                return true;
            }
        }
    }
}
