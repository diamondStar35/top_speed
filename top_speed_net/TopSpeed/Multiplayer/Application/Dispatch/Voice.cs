using TopSpeed.Network;
using TopSpeed.Protocol;

namespace TopSpeed.Game
{
    internal sealed partial class Game
    {
        private sealed partial class MultiplayerDispatch
        {
            private void RegisterVoice()
            {
                _reg.Add("voice", Command.PlayerVoiceStart, HandlePlayerVoiceStart);
                _reg.Add("voice", Command.PlayerVoiceFrame, HandlePlayerVoiceFrame);
                _reg.Add("voice", Command.PlayerVoiceStop, HandlePlayerVoiceStop);
            }

            private bool HandlePlayerVoiceStart(IncomingPacket packet)
            {
                if (ClientPacketSerializer.TryReadPlayerVoiceStart(packet.Payload, out var start))
                    _owner._multiplayerCommunicatorRuntime.ApplyRemoteVoiceStart(start, packet.ReceivedUtcTicks);
                return true;
            }

            private bool HandlePlayerVoiceFrame(IncomingPacket packet)
            {
                if (ClientPacketSerializer.TryReadPlayerVoiceFrame(packet.Payload, out var frame))
                    _owner._multiplayerCommunicatorRuntime.ApplyRemoteVoiceFrame(frame, packet.ReceivedUtcTicks);
                return true;
            }

            private bool HandlePlayerVoiceStop(IncomingPacket packet)
            {
                if (ClientPacketSerializer.TryReadPlayerVoiceStop(packet.Payload, out var stop))
                    _owner._multiplayerCommunicatorRuntime.ApplyRemoteVoiceStop(stop);
                return true;
            }
        }
    }
}
