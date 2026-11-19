using System;
using TopSpeed.Protocol;

namespace TopSpeed.Network.Session
{
    // Shared state machine for client-side opus stream emitters. Live (radio
    // streaming) and Voice (communicator) packets follow the same start/frame/stop
    // protocol, so the duplication in their TrySendStart / TrySendFrame /
    // TrySendStop methods lived here as identical code blocks. The only
    // per-stream variation is the actual packet wire format, which subclasses
    // supply by overriding the WriteXxxPacket hooks.
    internal abstract class OutboundStreamSend
    {
        private readonly Sender _sender;
        private readonly PacketStream _stream;
        private bool _active;
        private uint _streamId;

        protected OutboundStreamSend(Sender sender, PacketStream stream)
        {
            _sender = sender ?? throw new ArgumentNullException(nameof(sender));
            _stream = stream;
        }

        public bool IsActive => _active;
        public uint ActiveStreamId => _streamId;

        public bool TrySendStop(uint playerId, byte playerNumber, uint streamId)
        {
            if (!_active || _streamId != streamId)
                return true;

            var sent = _sender.TrySend(
                WriteStopPacket(playerId, playerNumber, streamId),
                _stream,
                PacketDeliveryKind.ReliableOrdered);

            if (!sent)
                return false;

            _active = false;
            _streamId = 0;
            return true;
        }

        public void Reset()
        {
            _active = false;
            _streamId = 0;
        }

        protected bool TrySendStartCore(uint playerId, byte playerNumber, uint streamId, byte[] startPacket)
        {
            if (streamId == 0)
                return false;

            if (_active && _streamId == streamId)
                return true;

            if (_active)
            {
                if (!TrySendStop(playerId, playerNumber, _streamId))
                {
                    // Local state went stale: drop the active flag so the next start
                    // can recover instead of being shadowed forever by a phantom stop.
                    _active = false;
                    _streamId = 0;
                }
            }

            if (!_sender.TrySend(startPacket, _stream, PacketDeliveryKind.ReliableOrdered))
                return false;

            _active = true;
            _streamId = streamId;
            return true;
        }

        protected bool TrySendFrameCore(uint playerId, byte playerNumber, uint streamId, in LiveOpusFrame frame)
        {
            if (!_active || _streamId != streamId)
                return false;

            return _sender.TrySend(
                WriteFramePacket(playerId, playerNumber, streamId, in frame),
                _stream,
                PacketDeliveryKind.Sequenced);
        }

        protected abstract byte[] WriteFramePacket(uint playerId, byte playerNumber, uint streamId, in LiveOpusFrame frame);
        protected abstract byte[] WriteStopPacket(uint playerId, byte playerNumber, uint streamId);
    }
}
