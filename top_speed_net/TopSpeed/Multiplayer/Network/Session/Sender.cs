using LiteNetLib;
using TopSpeed.Protocol;

namespace TopSpeed.Network.Session
{
    internal sealed class Sender
    {
        private readonly NetPeer _peer;

        public Sender(NetPeer peer)
        {
            _peer = peer;
        }

        public bool IsConnected
        {
            get
            {
                try
                {
                    return _peer.ConnectionState == ConnectionState.Connected;
                }
                catch
                {
                    return false;
                }
            }
        }

        public bool TrySend(byte[] payload, PacketStream stream)
        {
            var spec = PacketStreams.Get(stream);
            return TrySend(payload, spec.Channel, spec.Delivery);
        }

        public bool TrySend(byte[] payload, PacketStream stream, PacketDeliveryKind deliveryOverride)
        {
            var spec = PacketStreams.Get(stream);
            return TrySend(payload, spec.Channel, deliveryOverride);
        }

        private bool TrySend(byte[] payload, byte channel, PacketDeliveryKind kind)
        {
            try
            {
                if (!IsConnected)
                    return false;

                _peer.Send(payload, channel, ToDelivery(kind));
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static DeliveryMethod ToDelivery(PacketDeliveryKind kind)
        {
            return kind switch
            {
                PacketDeliveryKind.Unreliable => DeliveryMethod.Unreliable,
                PacketDeliveryKind.Sequenced => DeliveryMethod.Sequenced,
                _ => DeliveryMethod.ReliableOrdered
            };
        }
    }
}

