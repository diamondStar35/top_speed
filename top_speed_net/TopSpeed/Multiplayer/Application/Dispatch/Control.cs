using System;
using TopSpeed.Localization;
using TopSpeed.Network;
using TopSpeed.Protocol;

namespace TopSpeed.Game
{
    internal sealed partial class Game
    {
        private sealed partial class MultiplayerDispatch
        {
            private void RegisterControl()
            {
                _reg.Add("control", Command.Disconnect, HandleDisconnect);
                _reg.Add("control", Command.PlayerNumber, HandlePlayerNumber);
                _reg.Add("control", Command.Pong, HandlePong);
                _reg.Add("control", Command.ServerHeartbeat, HandleServerHeartbeat);
            }

            private bool HandleDisconnect(IncomingPacket packet)
            {
                var session = _owner._session;
                var message = LocalizationService.Mark("Disconnected from server.");
                var explicitDisconnect = ClientPacketSerializer.TryReadDisconnect(packet.Payload, out var disconnectMessage);
                if (explicitDisconnect && !string.IsNullOrWhiteSpace(disconnectMessage))
                {
                    message = disconnectMessage;
                }

                var allowReconnect = !explicitDisconnect;
                if (session != null && packet.HasDisconnectClassification)
                {
                    session.ApplyDisconnectClassification(packet.DisconnectReason, packet.ConnectionState);
                    allowReconnect = packet.ConnectionState == MultiplayerConnectionState.ConnectionLostSuspected
                                     || packet.ConnectionState == MultiplayerConnectionState.TimedOut;
                }

                _owner.HandleMultiplayerDisconnect(message, explicitDisconnect, allowReconnect);
                return true;
            }

            private bool HandlePlayerNumber(IncomingPacket packet)
            {
                var session = _owner._session;
                if (session == null)
                    return false;

                if (ClientPacketSerializer.TryReadPlayer(packet.Payload, out var assigned) && assigned.PlayerId == session.PlayerId)
                    session.UpdatePlayerNumber(assigned.PlayerNumber);
                return true;
            }

            private bool HandlePong(IncomingPacket packet)
            {
                _owner._multiplayerCoordinator.HandlePingReply(packet.ReceivedUtcTicks);
                return true;
            }

            private bool HandleServerHeartbeat(IncomingPacket packet)
            {
                var session = _owner._session;
                if (session == null)
                    return false;

                if (ClientPacketSerializer.TryReadServerHeartbeat(packet.Payload, out var heartbeat))
                    session.ApplyServerHeartbeat(heartbeat);
                return true;
            }
        }
    }
}
