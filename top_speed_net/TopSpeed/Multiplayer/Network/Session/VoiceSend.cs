using TopSpeed.Protocol;

namespace TopSpeed.Network.Session
{
    internal sealed class VoiceSend : OutboundStreamSend
    {
        public VoiceSend(Sender sender)
            : base(sender, PacketStream.Voice)
        {
        }

        public bool TrySendStart(
            uint playerId,
            byte playerNumber,
            uint streamId,
            LiveAudioProfile profile,
            ushort frequencyTenths,
            bool pushToTalk)
        {
            var packet = ClientPacketSerializer.WritePlayerVoiceStart(
                playerId,
                playerNumber,
                streamId,
                profile.Codec,
                profile.SampleRate,
                profile.Channels,
                profile.FrameMs,
                frequencyTenths,
                pushToTalk);
            return TrySendStartCore(playerId, playerNumber, streamId, packet);
        }

        public bool TrySendFrame(uint playerId, byte playerNumber, uint streamId, in LiveOpusFrame frame)
        {
            return TrySendFrameCore(playerId, playerNumber, streamId, in frame);
        }

        protected override byte[] WriteFramePacket(uint playerId, byte playerNumber, uint streamId, in LiveOpusFrame frame)
        {
            return ClientPacketSerializer.WritePlayerVoiceFrame(
                playerId,
                playerNumber,
                streamId,
                frame.Sequence,
                frame.Timestamp,
                frame.Payload);
        }

        protected override byte[] WriteStopPacket(uint playerId, byte playerNumber, uint streamId)
        {
            return ClientPacketSerializer.WritePlayerVoiceStop(playerId, playerNumber, streamId);
        }
    }
}
