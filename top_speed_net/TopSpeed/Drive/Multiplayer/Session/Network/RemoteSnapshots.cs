using TopSpeed.Protocol;
using TopSpeed.Vehicles;

namespace TopSpeed.Drive.Multiplayer
{
    internal sealed partial class MultiplayerSession
    {
        private void ApplyRemoteData(PacketPlayerData data)
        {
            ApplyRemoteDataCore(
                data.PlayerNumber,
                data.Car,
                data.State,
                data.RaceData.PositionX,
                data.RaceData.PositionY,
                data.RaceData.Speed,
                data.RaceData.Frequency,
                data.EngineRunning,
                data.Braking,
                data.Horning,
                data.Backfiring,
                data.MediaLoaded,
                data.MediaPlaying,
                data.MediaId,
                data.RadioVolumePercent);
        }

        private void ApplyRemoteDataCore(
            byte playerNumber,
            CarType car,
            PlayerState state,
            float positionX,
            float positionY,
            ushort speed,
            int frequency,
            bool engineRunning,
            bool braking,
            bool horning,
            bool backfiring,
            bool mediaLoaded,
            bool mediaPlaying,
            uint mediaId,
            byte radioVolumePercent)
        {
            if (playerNumber == LocalPlayerNumber)
                return;
            if (playerNumber < _disconnectedPlayerSlots.Length && _disconnectedPlayerSlots[playerNumber])
                return;

            var raceDistance = GetSpatialTrackLength();

            var remote = GetOrCreateRemotePlayer(playerNumber, car, positionX, positionY);
            remote.State = state;
            if (state == PlayerState.Finished && !remote.Finished)
            {
                remote.Finished = true;
                remote.Player.MarkFinished(raceDistance);
                _progress.AnnounceRemoteFinish(playerNumber);
            }

            remote.Player.ApplyNetworkState(
                positionX,
                positionY,
                speed,
                frequency,
                engineRunning,
                braking,
                horning,
                backfiring,
                mediaLoaded,
                mediaPlaying,
                mediaId,
                radioVolumePercent,
                _car.PositionX,
                _car.PositionY,
                GetSpatialTrackLength());
            TryApplyPendingRemoteMedia(playerNumber, remote);
        }

        private RemotePlayer GetOrCreateRemotePlayer(byte playerNumber, CarType car, float positionX, float positionY)
        {
            if (_remotePlayers.TryGetValue(playerNumber, out var existing))
                return existing;

            var vehicleIndex = car == CarType.CustomVehicle ? 0 : (int)car;
            var customVehicleFile = car == CarType.CustomVehicle ? _resolveRemoteVehicleFile?.Invoke(playerNumber) : null;
            var bot = new ComputerPlayer(_audio, _raceAudio, _track, _settings, vehicleIndex, playerNumber, () => _session.Context.RuntimeSeconds, () => _started, debugSpeak: null, customVehicleFile: customVehicleFile);
            bot.Initialize(positionX, positionY, GetSpatialTrackLength());
            var remote = new RemotePlayer(bot);
            _remotePlayers[playerNumber] = remote;
            return remote;
        }

        private void TryApplyPendingRemoteMedia(byte playerNumber, RemotePlayer remote)
        {
            if (_remoteLiveStates.TryGetValue(playerNumber, out var live) && live.StreamId != 0)
                return;
            if (!_remoteMediaTransfers.TryGetValue(playerNumber, out var transfer))
                return;
            if (!transfer.IsComplete)
                return;

            remote.Player.ApplyRadioMedia(transfer.MediaId, transfer.Extension, transfer.Data);
            _remoteMediaTransfers.Remove(playerNumber);
        }
    }
}
