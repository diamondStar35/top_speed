using System;
using TopSpeed.Drive.Session;
using TS.Audio;

namespace TopSpeed.Drive.Single
{
    internal sealed partial class SingleSession
    {
        public void Initialize(int playerNumber)
        {
            _track.Initialize();
            _car.Initialize();
            _car.SetOverrideController(null);
            _car.ManualTransmission = _manualTransmission;
            _panels.Resume();
            _playerNumber = playerNumber;
            _position = playerNumber + 1;
            _positionComment = playerNumber + 1;
            _positionFinish = 0;
            _botsScheduled = false;
            _finishTimesMs.Clear();
            _finishOrder.Clear();
            _currentRoad.Surface = _track.InitialSurface;
            _lastRecordedCarState = _car.State;
            PreloadRaceSpeechSources();
            _listener.Reset();
            _trackAudio.Reset();
            _generalRequests.Reset();
            _commentary.Reset();
            _collisions.Reset();
            _exitWhenQueueIdle = false;
            _requirePostFinishStopBeforeExit = false;
            _pendingResultSummary = null;
            _localCrashCount = 0;
            _soundQueue.Clear();
            _raceInfoQueue.Clear();
            _trackInfoQueue.Clear();
            _unkeyQueue = 0;
            _speakTime = 0f;
            _lap = 0;
            _raceTime = 0;
            _started = false;
            _finished = false;
            _session.Reset();
            CreateComputerPlayers();
            PositionGrid();
            _session.SetPhase(Phase.Countdown);
            QueueRaceIntro();
            _session.QueueEvent(new Event(Events.PlaySound, _soundStart), DefaultStartCueDelaySeconds);
            _session.QueueEvent(new Event(Events.VehicleStart), 3.0f);
            _session.QueueEvent(new Event(Events.ProgressStart), DefaultProgressStartDelaySeconds);
        }

        public void FinalizeSession()
        {
            for (var i = 0; i < _nComputerPlayers; i++)
            {
                _computerPlayers[i]?.FinalizePlayer();
                _computerPlayers[i]?.Dispose();
                _computerPlayers[i] = null;
            }

            _panels.Pause();
            _car.FinalizeCar();
            _track.FinalizeTrack();
        }

        public void Pause()
        {
            _session.ApplyCommand(new Command(Commands.Pause));
        }

        public void Resume()
        {
            _session.ApplyCommand(new Command(Commands.Resume));
        }

        public void ClearPauseRequest()
        {
            _session.ApplyCommand(new Command(Commands.ClearPauseRequest));
        }

        public DriveResultSummary? ConsumeResultSummary()
        {
            var summary = _pendingResultSummary;
            _pendingResultSummary = null;
            return summary;
        }

        public void Dispose()
        {
            _soundQueue.Clear();
            _raceInfoQueue.Clear();
            _trackInfoQueue.Clear();
            _panelManager.Dispose();
            _localRadio.Dispose();
            _car.Dispose();
            _track.Dispose();
            DisposeSound(_soundStart);
            DisposeSound(_soundTheme);
            DisposeSound(_soundPause);
            DisposeSound(_soundResume);
            DisposeSound(_soundTurnEndDing);
            DisposeSound(_soundYouAre);
            DisposeSound(_soundPlayer);

            for (var i = 0; i < _soundNumbers.Length; i++)
                DisposeSound(_soundNumbers[i]);
            for (var i = 0; i < _soundUnkey.Length; i++)
                DisposeSound(_soundUnkey[i]);
            for (var i = 0; i < _soundLaps.Length; i++)
                DisposeSound(_soundLaps[i]);
            for (var i = 0; i < _soundPlayerNr.Length; i++)
            {
                DisposeSound(_soundPlayerNr[i]);
                DisposeSound(_soundPlayerNrInfo[i]);
                DisposeSound(_soundPosition[i]);
                DisposeSound(_soundFinished[i]);
            }
            for (var i = 0; i < _randomSounds.Length; i++)
            {
                var count = _totalRandomSounds[i];
                for (var j = 0; j < count && j < _randomSounds[i].Length; j++)
                    DisposeSound(_randomSounds[i][j]);
            }
        }

        private void HandleSessionEvent(SessionContext context, Event sessionEvent)
        {
            switch (sessionEvent.Id)
            {
                case Events.VehicleStart:
                    break;
                case Events.ProgressStart:
                    _session.SetPhase(Phase.Running);
                    _raceTime = 0;
                    _lap = 0;
                    _started = true;
                    if (!_botsScheduled)
                    {
                        for (var i = 0; i < _nComputerPlayers; i++)
                            _computerPlayers[i]?.PendingStart(0.0f);
                        _botsScheduled = true;
                    }
                    break;
                case Events.ProgressFinish:
                    _pendingResultSummary = BuildResultSummary();
                    _session.SetPhase(Phase.Finished);
                    _exitWhenQueueIdle = true;
                    break;
                case Events.PlaySound:
                    QueueSound(sessionEvent.Data as Source);
                    break;
                case Events.PlayInfoSound:
                    QueueRaceInfoSound(sessionEvent.Data as Source);
                    break;
                case Events.PlayTrackInfoSound:
                    QueueTrackInfoSound(sessionEvent.Data as Source);
                    break;
                case Events.PlayUnkey:
                    _unkeyQueue--;
                    if (_unkeyQueue == 0)
                        Speak(_soundUnkey[TopSpeed.Common.Algorithm.RandomInt(MaxUnkeys)]);
                    break;
            }
        }

        private void HandlePhaseEvent(SessionContext context, Event sessionEvent)
        {
            if (sessionEvent.Id != Events.PhaseChanged || sessionEvent.Data is not PhaseChanged phaseChanged)
                return;

            _panels.ApplyInputPolicy(context.InputPolicy);

            if (phaseChanged.Current == Phase.Paused)
            {
                _soundQueue.Pause();
                _raceInfoQueue.Pause();
                _trackInfoQueue.Pause();
                _track.PauseAudio();
                _soundTheme?.Play(loop: true);
                FadeInTheme();
                _panels.Pause();
                _car.Pause();
                for (var i = 0; i < _nComputerPlayers; i++)
                    _computerPlayers[i]?.Pause();
                _soundPause?.Play(loop: false);
                return;
            }

            if (phaseChanged.Previous == Phase.Paused)
            {
                _soundQueue.Resume();
                _raceInfoQueue.Resume();
                _trackInfoQueue.Resume();
                _track.ResumeAudio();
                _car.Unpause();
                for (var i = 0; i < _nComputerPlayers; i++)
                    _computerPlayers[i]?.Unpause();
                _panels.Resume();
                FadeOutTheme();
                _soundTheme?.Stop();
                _soundTheme?.SeekToStart();
                _soundResume?.Play(loop: false);
            }
        }

        private void PositionGrid()
        {
            var maxLength = _car.LengthM;
            for (var i = 0; i < _nComputerPlayers; i++)
            {
                var bot = _computerPlayers[i];
                if (bot != null && bot.LengthM > maxLength)
                    maxLength = bot.LengthM;
            }

            var rowSpacing = Math.Max(10.0f, maxLength * 1.5f);
            var playerX = CalculateGridStartX(_playerNumber, _car.WidthM, StartLineY);
            var playerY = CalculateGridStartY(_playerNumber, rowSpacing, StartLineY);
            _car.SetPosition(playerX, playerY);

            for (var i = 0; i < _nComputerPlayers; i++)
            {
                var bot = _computerPlayers[i];
                if (bot == null)
                    continue;
                var botX = CalculateGridStartX(bot.PlayerNumber, bot.WidthM, StartLineY);
                var botY = CalculateGridStartY(bot.PlayerNumber, rowSpacing, StartLineY);
                bot.Initialize(botX, botY, _track.Length);
            }
        }

        private bool UpdateExitWhenQueueIdle()
        {
            if (!_exitWhenQueueIdle)
                return false;
            if (_requirePostFinishStopBeforeExit && !AreVehiclesSettledForExit())
                return false;
            return _soundQueue.IsIdle && _raceInfoQueue.IsIdle && _trackInfoQueue.IsIdle;
        }

        private float CalculateGridStartX(int gridIndex, float vehicleWidth, float startLineY)
        {
            var halfWidth = Math.Max(0.1f, vehicleWidth * 0.5f);
            var margin = 0.3f;
            var laneHalfWidth = _track.LaneHalfWidthAtPosition(startLineY);
            var laneOffset = laneHalfWidth - halfWidth - margin;
            if (laneOffset < 0f)
                laneOffset = 0f;
            return gridIndex % 2 == 1 ? laneOffset : -laneOffset;
        }

        private static float CalculateGridStartY(int gridIndex, float rowSpacing, float startLineY)
        {
            var row = gridIndex / 2;
            return startLineY - (row * rowSpacing);
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
