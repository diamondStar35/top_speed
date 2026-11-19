using TopSpeed.Data;
using TopSpeed.Protocol;

namespace TopSpeed.Network
{
    internal sealed partial class MultiplayerSession
    {
        public bool SendPlayerState(uint raceInstanceId, PlayerState state)
        {
            var payload = ClientPacketSerializer.WriteRacePlayerState(Command.PlayerState, raceInstanceId, PlayerId, PlayerNumber, state);
            return _sender.TrySend(payload, PacketStream.Control);
        }

        public bool SendPlayerData(
            uint raceInstanceId,
            PlayerRaceData raceData,
            CarType car,
            PlayerState state,
            bool engine,
            bool braking,
            bool horning,
            bool backfiring,
            bool radioLoaded,
            bool radioPlaying,
            uint radioMediaId,
            byte radioVolumePercent)
        {
            var payload = ClientPacketSerializer.WriteRacePlayerDataToServer(
                raceInstanceId,
                PlayerId,
                PlayerNumber,
                car,
                raceData,
                state,
                engine,
                braking,
                horning,
                backfiring,
                radioLoaded,
                radioPlaying,
                radioMediaId,
                radioVolumePercent);
            return _sender.TrySend(payload, PacketStream.RaceState, PacketDeliveryKind.Sequenced);
        }

        public bool SendPlayerStarted(uint raceInstanceId)
        {
            return _sender.TrySend(
                ClientPacketSerializer.WriteRacePlayer(Command.PlayerStarted, raceInstanceId, PlayerId, PlayerNumber),
                PacketStream.RaceEvent);
        }

        public bool SendPlayerFinalize(uint raceInstanceId, PlayerState state)
        {
            return _sender.TrySend(
                ClientPacketSerializer.WriteRacePlayerState(Command.PlayerFinalize, raceInstanceId, PlayerId, PlayerNumber, state),
                PacketStream.Control);
        }

        public bool SendPlayerCrashed(uint raceInstanceId)
        {
            return _sender.TrySend(
                ClientPacketSerializer.WriteRacePlayer(Command.PlayerCrashed, raceInstanceId, PlayerId, PlayerNumber),
                PacketStream.RaceEvent);
        }
    }
}

