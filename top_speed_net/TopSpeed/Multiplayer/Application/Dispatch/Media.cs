using TopSpeed.Network;
using TopSpeed.Protocol;

namespace TopSpeed.Game
{
    internal sealed partial class Game
    {
        private sealed partial class MultiplayerDispatch
        {
            private void RegisterMedia()
            {
                _reg.Add("media", Command.PlayerMediaBegin, HandlePlayerMediaBegin);
                _reg.Add("media", Command.PlayerMediaChunk, HandlePlayerMediaChunk);
                _reg.Add("media", Command.PlayerMediaEnd, HandlePlayerMediaEnd);
                _reg.Add("media", Command.PlayerCommunicatorMediaBegin, HandlePlayerCommunicatorMediaBegin);
                _reg.Add("media", Command.PlayerCommunicatorMediaChunk, HandlePlayerCommunicatorMediaChunk);
                _reg.Add("media", Command.PlayerCommunicatorMediaEnd, HandlePlayerCommunicatorMediaEnd);
                _reg.Add("media", Command.PlayerCommunicatorMediaState, HandlePlayerCommunicatorMediaState);
            }

            private bool HandlePlayerMediaBegin(IncomingPacket packet)
            {
                if (_owner._multiplayerRaceRuntime.Mode == null)
                    return true;

                if (ClientPacketSerializer.TryReadPlayerMediaBegin(packet.Payload, out var mediaBegin))
                    _owner._multiplayerRaceRuntime.Mode.ApplyRemoteMediaBegin(mediaBegin);
                return true;
            }

            private bool HandlePlayerMediaChunk(IncomingPacket packet)
            {
                if (_owner._multiplayerRaceRuntime.Mode == null)
                    return true;

                if (ClientPacketSerializer.TryReadPlayerMediaChunk(packet.Payload, out var mediaChunk))
                    _owner._multiplayerRaceRuntime.Mode.ApplyRemoteMediaChunk(mediaChunk);
                return true;
            }

            private bool HandlePlayerMediaEnd(IncomingPacket packet)
            {
                if (_owner._multiplayerRaceRuntime.Mode == null)
                    return true;

                if (ClientPacketSerializer.TryReadPlayerMediaEnd(packet.Payload, out var mediaEnd))
                    _owner._multiplayerRaceRuntime.Mode.ApplyRemoteMediaEnd(mediaEnd);
                return true;
            }

            private bool HandlePlayerCommunicatorMediaBegin(IncomingPacket packet)
            {
                if (ClientPacketSerializer.TryReadPlayerCommunicatorMediaBegin(packet.Payload, out var mediaBegin))
                    _owner._multiplayerCommunicatorRuntime.ApplyRemoteMediaBegin(mediaBegin);
                return true;
            }

            private bool HandlePlayerCommunicatorMediaChunk(IncomingPacket packet)
            {
                if (ClientPacketSerializer.TryReadPlayerCommunicatorMediaChunk(packet.Payload, out var mediaChunk))
                    _owner._multiplayerCommunicatorRuntime.ApplyRemoteMediaChunk(mediaChunk);
                return true;
            }

            private bool HandlePlayerCommunicatorMediaEnd(IncomingPacket packet)
            {
                if (ClientPacketSerializer.TryReadPlayerCommunicatorMediaEnd(packet.Payload, out var mediaEnd))
                    _owner._multiplayerCommunicatorRuntime.ApplyRemoteMediaEnd(mediaEnd);
                return true;
            }

            private bool HandlePlayerCommunicatorMediaState(IncomingPacket packet)
            {
                if (ClientPacketSerializer.TryReadPlayerCommunicatorMediaState(packet.Payload, out var state))
                    _owner._multiplayerCommunicatorRuntime.ApplyRemoteMediaState(state);
                return true;
            }
        }
    }
}
