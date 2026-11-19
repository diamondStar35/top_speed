using TopSpeed.Protocol;

namespace TopSpeed.Network
{
    internal sealed partial class MultiplayerSession
    {
        public bool SendRadioMedia(uint mediaId, string filePath)
        {
            return _media.TrySendBuffered(PlayerId, PlayerNumber, mediaId, filePath);
        }

        public bool SendRadioMediaStreamed(uint mediaId, string filePath)
        {
            return _media.TrySendStreamed(PlayerId, PlayerNumber, mediaId, filePath);
        }

        public bool SendCommunicatorMediaStreamed(uint mediaId, string filePath, ushort frequencyTenths)
        {
            return _media.TrySendCommunicatorStreamed(PlayerId, PlayerNumber, mediaId, filePath, frequencyTenths);
        }

        public bool SendCommunicatorMediaState(PacketPlayerCommunicatorMediaState state)
        {
            if (state == null)
                return false;

            state.PlayerId = PlayerId;
            state.PlayerNumber = PlayerNumber;
            return _sender.TrySend(ClientPacketSerializer.WritePlayerCommunicatorMediaState(state), PacketStream.Media);
        }

        public bool SendLiveStart(uint streamId, LiveAudioProfile profile)
        {
            return _live.TrySendStart(PlayerId, PlayerNumber, streamId, profile);
        }

        public bool SendLiveFrame(uint streamId, in LiveOpusFrame frame)
        {
            return _live.TrySendFrame(PlayerId, PlayerNumber, streamId, frame);
        }

        public bool SendLiveStop(uint streamId)
        {
            return _live.TrySendStop(PlayerId, PlayerNumber, streamId);
        }

        public bool SendVoiceStart(uint streamId, LiveAudioProfile profile, ushort frequencyTenths, bool pushToTalk)
        {
            var sent = _voice.TrySendStart(PlayerId, PlayerNumber, streamId, profile, frequencyTenths, pushToTalk);
            if (sent)
                NotifyVoiceTransmissionState(true);
            return sent;
        }

        public bool SendVoiceFrame(uint streamId, in LiveOpusFrame frame)
        {
            return _voice.TrySendFrame(PlayerId, PlayerNumber, streamId, frame);
        }

        public bool SendVoiceStop(uint streamId)
        {
            var sent = _voice.TrySendStop(PlayerId, PlayerNumber, streamId);
            if (sent)
                NotifyVoiceTransmissionState(false);
            return sent;
        }

        public bool SendChatMessage(string text)
        {
            var packet = new PacketProtocolMessage
            {
                Code = ProtocolMessageCode.Chat,
                Message = text ?? string.Empty
            };
            return _sender.TrySend(ClientPacketSerializer.WriteProtocolMessage(packet), PacketStream.Chat);
        }

        public bool SendRoomChatMessage(string text)
        {
            var packet = new PacketProtocolMessage
            {
                Code = ProtocolMessageCode.RoomChat,
                Message = text ?? string.Empty
            };
            return _sender.TrySend(ClientPacketSerializer.WriteProtocolMessage(packet), PacketStream.Chat);
        }
    }
}

