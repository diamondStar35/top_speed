using TopSpeed.Network.Live;
using TopSpeed.Protocol;

namespace TopSpeed.Drive.Multiplayer
{
    internal sealed partial class MultiplayerSession
    {
        private void ApplyRemoteLiveStartCore(PacketPlayerLiveStart start, long receivedUtcTicks)
        {
            if (!CanApplyRemoteLive(start.PlayerNumber))
                return;
            if (!IsValidLiveStart(start))
                return;

            _remoteLiveStates[start.PlayerNumber] = new LiveState(start, receivedUtcTicks);
            if (_remotePlayers.TryGetValue(start.PlayerNumber, out var remote))
                remote.Player.ApplyLiveStart(start.StreamId, start.Codec, start.SampleRate, start.Channels, start.FrameMs);
        }

        private void ApplyRemoteLiveFrameCore(PacketPlayerLiveFrame frame, long receivedUtcTicks)
        {
            if (!CanApplyRemoteLive(frame.PlayerNumber))
                return;
            if (_remoteLiveStates.TryGetValue(frame.PlayerNumber, out var live))
                live.TryPush(frame, receivedUtcTicks);
        }

        private void ApplyRemoteLiveStopCore(PacketPlayerLiveStop stop)
        {
            if (!CanApplyRemoteLive(stop.PlayerNumber))
                return;
            if (!_remoteLiveStates.TryGetValue(stop.PlayerNumber, out var live))
                return;
            if (live.StreamId != stop.StreamId)
                return;

            if (_remotePlayers.TryGetValue(stop.PlayerNumber, out var remote))
                remote.Player.ApplyLiveStop(stop.StreamId);
            _remoteLiveStates.Remove(stop.PlayerNumber);
        }

        private bool CanApplyRemoteLive(byte playerNumber)
        {
            if (playerNumber == LocalPlayerNumber)
                return false;
            if (playerNumber < _disconnectedPlayerSlots.Length && _disconnectedPlayerSlots[playerNumber])
                return false;
            return true;
        }

        private static bool IsValidLiveStart(PacketPlayerLiveStart start)
        {
            if (start.StreamId == 0)
                return false;
            if (start.Codec != LiveCodec.Opus)
                return false;
            if (start.SampleRate != ProtocolConstants.LiveSampleRate)
                return false;
            if (start.FrameMs != ProtocolConstants.LiveFrameMs)
                return false;
            if (start.Channels < ProtocolConstants.LiveChannelsMin || start.Channels > ProtocolConstants.LiveChannelsMax)
                return false;
            return true;
        }
    }
}
