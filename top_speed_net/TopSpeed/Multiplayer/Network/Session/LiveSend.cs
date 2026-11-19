using TopSpeed.Protocol;

namespace TopSpeed.Network.Session
{
    internal sealed class LiveSend : OutboundStreamSend
    {
        public LiveSend(Sender sender)
            : base(sender, PacketStream.Live)
        {
        }

        public bool TrySendStart(uint playerId, byte playerNumber, uint streamId, LiveAudioProfile profile)
        {
            var packet = ClientPacketSerializer.WritePlayerLiveStart(
                playerId,
                playerNumber,
                streamId,
                profile.Codec,
                profile.SampleRate,
                profile.Channels,
                profile.FrameMs);
            return TrySendStartCore(playerId, playerNumber, streamId, packet);
        }

        public bool TrySendFrame(uint playerId, byte playerNumber, uint streamId, in LiveOpusFrame frame)
        {
            return TrySendFrameCore(playerId, playerNumber, streamId, in frame);
        }

        protected override byte[] WriteFramePacket(uint playerId, byte playerNumber, uint streamId, in LiveOpusFrame frame)
        {
            return ClientPacketSerializer.WritePlayerLiveFrame(
                playerId,
                playerNumber,
                streamId,
                frame.Sequence,
                frame.Timestamp,
                frame.Payload);
        }

        protected override byte[] WriteStopPacket(uint playerId, byte playerNumber, uint streamId)
        {
            return ClientPacketSerializer.WritePlayerLiveStop(playerId, playerNumber, streamId);
        }
    }
}
