using TS.Audio;

namespace TopSpeed.Drive.Multiplayer
{
    internal sealed partial class MultiplayerSession
    {
        public void Dispose()
        {
            _soundQueue.Clear();
            _raceInfoQueue.Clear();
            _trackInfoQueue.Clear();
            _liveTx.Dispose();
            _panelManager.Dispose();
            _localRadio.Dispose();
            _car.Dispose();
            _track.Dispose();
            DisposeSound(_soundStart);
            DisposeSound(_soundPause);
            DisposeSound(_soundResume);
            DisposeSound(_soundTurnEndDing);
            DisposeSound(_soundYouAre);
            DisposeSound(_soundPlayer);
            for (var i = 0; i < _soundNumbers.Length; i++)
                DisposeSound(_soundNumbers[i]);
            for (var i = 0; i < _soundLaps.Length; i++)
                DisposeSound(_soundLaps[i]);
            for (var i = 0; i < _soundUnkey.Length; i++)
                DisposeSound(_soundUnkey[i]);
            for (var i = 0; i < _soundPlayerNr.Length; i++)
            {
                DisposeSound(_soundPlayerNr[i]);
                DisposeSound(_soundPlayerNrInfo[i]);
                DisposeSound(_soundPosition[i]);
                DisposeSound(_soundFinished[i]);
            }
            DisposeSound(_soundFinishedLast);
            DisposeSound(_soundPositionLast);
            for (var i = 0; i < _randomSounds.Length; i++)
            {
                var count = _totalRandomSounds[i];
                for (var j = 0; j < count && j < _randomSounds[i].Length; j++)
                    DisposeSound(_randomSounds[i][j]);
            }
        }

        private void FinalizeRemotePlayers()
        {
            foreach (var remote in _remotePlayers.Values)
            {
                remote.Player.StopLiveStream();
                remote.Player.FinalizePlayer();
                remote.Player.Dispose();
            }

            _remotePlayers.Clear();
        }

        private static void DisposeSound(Source? sound)
        {
            if (sound == null)
                return;

            sound.Stop();
            sound.Dispose();
        }
    }
}
