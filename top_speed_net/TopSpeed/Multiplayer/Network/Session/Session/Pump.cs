using System;
using TopSpeed.Protocol;

namespace TopSpeed.Network
{
    internal sealed partial class MultiplayerSession
    {
        private void PollEventsSafe()
        {
            try
            {
                _manager.PollEvents();
            }
            catch (ObjectDisposedException)
            {
                // Keep session alive even if the transport reports a transient poll error.
            }
            catch (InvalidOperationException)
            {
                // Keep session alive even if the transport reports a transient poll error.
            }

            EvaluateHeartbeatTimeout();
            QueueSyntheticDisconnectIfNeeded();
        }

        private void SendKeepAlive()
        {
            var nextClientTick = ++_heartbeatTick;
            var heartbeat = ClientPacketSerializer.WriteClientHeartbeat(new PacketClientHeartbeat
            {
                PlayerId = PlayerId,
                SessionId = ResumeToken,
                ClientTick = nextClientTick,
                LastReceivedServerTick = _lastServerHeartbeatTick
            });
            _lastHeartbeatProbeUtc = DateTime.UtcNow;
            if (_sender.TrySend(heartbeat, PacketStream.Control, PacketDeliveryKind.Unreliable))
                return;

            _sender.TrySend(
                new[] { ProtocolConstants.Version, (byte)Command.KeepAlive },
                PacketStream.Control,
                PacketDeliveryKind.Unreliable);
        }

        private void DrainIncomingToSink()
        {
            var sink = _packetSink;
            if (sink == null)
                return;

            while (_incoming.TryDequeue(out var packet))
            {
                try
                {
                    sink(packet);
                }
                catch (ObjectDisposedException)
                {
                    // Keep main-thread packet handling resilient against callback failures.
                }
                catch (InvalidOperationException)
                {
                    // Keep main-thread packet handling resilient against callback failures.
                }
                catch (ArgumentException)
                {
                    // Keep main-thread packet handling resilient against callback failures.
                }
            }
        }
    }
}

