using TopSpeed.Network;
using TopSpeed.Protocol;

namespace TopSpeed.Game
{
    internal sealed partial class Game
    {
        private sealed partial class MultiplayerDispatch
        {
            private void RegisterRace()
            {
                _reg.Add("race_state", Command.RaceSnapshot, HandleRaceSnapshot);
                _reg.Add("race_event", Command.StartRace, HandleStartRace);
                _reg.Add("race_event", Command.RaceAborted, HandleRaceAborted);
                _reg.Add("race_event", Command.PlayerBumped, HandlePlayerBumped);
                _reg.Add("race_event", Command.PlayerCrashed, HandlePlayerCrashed);
                _reg.Add("race_event", Command.PlayerDisconnected, HandlePlayerDisconnected);
            }

            private bool HandleRaceSnapshot(IncomingPacket packet)
            {
                if (_owner._multiplayerRaceRuntime.Mode == null)
                    return true;

                if (ClientPacketSerializer.TryReadRaceSnapshot(packet.Payload, out var snapshot))
                    _owner._multiplayerRaceRuntime.Mode.ApplyRaceSnapshot(snapshot);
                return true;
            }

            private bool HandleStartRace(IncomingPacket _)
            {
                _owner.StartMultiplayerRace();
                return true;
            }

            private bool HandleRaceAborted(IncomingPacket _)
            {
                if (_owner._state == AppState.MultiplayerRace)
                    _owner.EndMultiplayerRace();
                return true;
            }

            private bool HandlePlayerBumped(IncomingPacket packet)
            {
                if (_owner._multiplayerRaceRuntime.Mode == null)
                    return true;

                if (ClientPacketSerializer.TryReadPlayerBumped(packet.Payload, out var bump))
                    _owner._multiplayerRaceRuntime.Mode.ApplyBump(bump);
                return true;
            }

            private bool HandlePlayerCrashed(IncomingPacket packet)
            {
                if (_owner._multiplayerRaceRuntime.Mode == null)
                    return true;

                if (ClientPacketSerializer.TryReadPlayer(packet.Payload, out var crashed))
                    _owner._multiplayerRaceRuntime.Mode.ApplyRemoteCrash(crashed);
                return true;
            }

            private bool HandlePlayerDisconnected(IncomingPacket packet)
            {
                if (_owner._multiplayerRaceRuntime.Mode == null)
                    return true;

                if (ClientPacketSerializer.TryReadPlayer(packet.Payload, out var disconnected))
                    _owner._multiplayerRaceRuntime.Mode.RemoveRemotePlayer(disconnected.PlayerNumber);
                return true;
            }
        }
    }
}
