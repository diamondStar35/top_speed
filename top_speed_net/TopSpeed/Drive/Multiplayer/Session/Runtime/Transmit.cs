using TopSpeed.Protocol;

namespace TopSpeed.Drive.Multiplayer
{
    internal sealed partial class MultiplayerSession
    {
        private uint NextLocalMediaId()
        {
            _nextMediaId++;
            if (_nextMediaId == 0)
                _nextMediaId = 1;
            return _nextMediaId;
        }

        private void HandleLocalRadioMediaLoaded(uint mediaId, string mediaPath)
        {
            if (!_liveTx.SetMedia(mediaId, mediaPath, out var error))
                SpeakText(error);
        }

        private void HandleLocalRadioPlaybackChanged(bool loaded, bool playing, uint mediaId)
        {
            if (!_liveTx.SetPlayback(loaded, playing, mediaId, out var error))
                SpeakText(error);
        }

        private PlayerRaceData BuildRaceData()
        {
            return new PlayerRaceData
            {
                PositionX = _car.PositionX,
                PositionY = _car.PositionY,
                Speed = (ushort)_car.Speed,
                Frequency = _car.Frequency
            };
        }

        private bool SendPlayerData()
        {
            var state = _currentState;
            if (_sentFinish)
                state = PlayerState.Finished;
            else if (_started && !_hostPaused && _currentState != PlayerState.AwaitingStart && _currentState != PlayerState.Pitting)
                state = PlayerState.Racing;

            return TrySendRace(_network.SendPlayerData(
                _raceInstanceId,
                BuildRaceData(),
                _car.CarType,
                state,
                _car.EngineRunning,
                _car.Braking,
                _car.Horning,
                _car.Backfiring(),
                LocalMediaLoaded,
                LocalMediaPlaying,
                LocalMediaId,
                LocalRadioVolumePercent));
        }

        private void SendPlayerState(bool sendStarted)
        {
            if (sendStarted)
                TrySendRace(_network.SendPlayerStarted(_raceInstanceId));

            TrySendRace(_network.SendPlayerState(_raceInstanceId, _currentState));
        }

        private void SendCrash()
        {
            TrySendRace(_network.SendPlayerCrashed(_raceInstanceId));
        }
    }
}
