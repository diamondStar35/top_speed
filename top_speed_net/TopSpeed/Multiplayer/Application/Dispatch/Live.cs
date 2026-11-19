using TopSpeed.Network;
using TopSpeed.Protocol;

namespace TopSpeed.Game
{
    internal sealed partial class Game
    {
        private sealed partial class MultiplayerDispatch
        {
            private void RegisterLive()
            {
                _reg.Add("live", Command.PlayerLiveStart, HandlePlayerLiveStart);
                _reg.Add("live", Command.PlayerLiveFrame, HandlePlayerLiveFrame);
                _reg.Add("live", Command.PlayerLiveStop, HandlePlayerLiveStop);
            }

            private bool HandlePlayerLiveStart(IncomingPacket packet)
            {
                if (_owner._multiplayerRaceRuntime.Mode == null)
                    return true;

                if (ClientPacketSerializer.TryReadPlayerLiveStart(packet.Payload, out var start))
                    _owner._multiplayerRaceRuntime.Mode.ApplyRemoteLiveStart(start, packet.ReceivedUtcTicks);
                return true;
            }

            private bool HandlePlayerLiveFrame(IncomingPacket packet)
            {
                if (_owner._multiplayerRaceRuntime.Mode == null)
                    return true;

                if (ClientPacketSerializer.TryReadPlayerLiveFrame(packet.Payload, out var frame))
                    _owner._multiplayerRaceRuntime.Mode.ApplyRemoteLiveFrame(frame, packet.ReceivedUtcTicks);
                return true;
            }

            private bool HandlePlayerLiveStop(IncomingPacket packet)
            {
                if (_owner._multiplayerRaceRuntime.Mode == null)
                    return true;

                if (ClientPacketSerializer.TryReadPlayerLiveStop(packet.Payload, out var stop))
                    _owner._multiplayerRaceRuntime.Mode.ApplyRemoteLiveStop(stop);
                return true;
            }
        }
    }
}
