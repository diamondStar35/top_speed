using System;
using TopSpeed.Drive.Session;
using TopSpeed.Protocol;
using SessionCommand = TopSpeed.Drive.Session.Command;

namespace TopSpeed.Drive.Multiplayer
{
    internal sealed partial class MultiplayerSession
    {
        public void Initialize()
        {
            FinalizeRemotePlayers();

            _track.Initialize();
            _car.Initialize();
            _car.SetOverrideController(null);
            _car.ManualTransmission = _manualTransmission;
            _panels.Resume();
            _vehicle.Reset();
            _trackAudio.Reset();
            _generalRequests.Reset();
            _sync.Reset();
            _commentary.Reset();
            var road = _currentRoad;
            road.Surface = _track.InitialSurface;
            _currentRoad = road;
            _lastRecordedCarState = _car.State;
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
            _sentStart = false;
            _sentFinish = false;
            _serverStopReceived = false;
            _positionFinish = 0;
            _position = LocalPlayerNumber + 1;
            _positionComment = _position;
            _currentState = PlayerState.AwaitingStart;
            _lastCarState = _car.State;
            _lastRaceSnapshotSequence = 0;
            _lastRaceSnapshotTick = 0;
            _hasRaceSnapshotSequence = false;
            _snapshotFrames.Clear();
            _snapshotTickNow = 0f;
            _hasSnapshotTickNow = false;
            _sendFailureAnnounced = false;
            _liveFailureAnnounced = false;
            _hostPaused = false;
            _remoteMediaTransfers.Clear();
            _remoteLiveStates.Clear();
            _expiredLivePlayers.Clear();
            _missingSnapshotPlayers.Clear();
            Array.Clear(_disconnectedPlayerSlots, 0, _disconnectedPlayerSlots.Length);
            _liveTx.Resume();
            _session.Reset();

            var rowSpacing = Math.Max(10f, _car.LengthM * 1.5f);
            var positionX = CalculateGridStartX(LocalPlayerNumber, _car.WidthM, StartLineY);
            var positionY = CalculateGridStartY(LocalPlayerNumber, rowSpacing, StartLineY);
            _car.SetPosition(positionX, positionY);

            SendPlayerState(sendStarted: false);
            _session.SetPhase(Phase.Countdown);
            QueueRaceIntro();
            _session.QueueEvent(new Event(Events.PlaySound, _soundStart), DefaultStartCueDelaySeconds);
            _session.QueueEvent(new Event(Events.VehicleStart), 3.0f);
            _session.QueueEvent(new Event(Events.ProgressStart), DefaultProgressStartDelaySeconds);
        }

        public void FinalizeSession()
        {
            FinalizeRemotePlayers();
            _remoteMediaTransfers.Clear();
            _remoteLiveStates.Clear();
            _snapshotFrames.Clear();
            _hostPaused = false;
            _panels.Pause();
            _car.FinalizeCar();
            _track.FinalizeTrack();
        }

        public void Pause()
        {
            SetHostPaused(true);
        }

        public void Resume()
        {
            SetHostPaused(false);
        }

        public void SetHostPaused(bool paused)
        {
            if (_serverStopReceived || _hostPaused == paused)
                return;

            _hostPaused = paused;
            _session.ApplyCommand(new SessionCommand(paused ? Commands.Pause : Commands.Resume));
        }

        public void ClearPauseRequest()
        {
            _session.ApplyCommand(new SessionCommand(Commands.ClearPauseRequest));
        }

        // Adopts the authoritative race instance from the server once it is known. After an abort the
        // session can be created before the room/race-state packet re-binds the instance, leaving it
        // at 0; every race packet we send then carries instance 0 and the server silently drops it
        // (including our finish), so the race never completes. Self-healing to the real instance lets
        // our outgoing packets be accepted again.
        public void UpdateRaceInstanceId(uint raceInstanceId)
        {
            if (raceInstanceId != 0)
                _raceInstanceId = raceInstanceId;
        }

        public DriveResultSummary? ConsumeResultSummary()
        {
            var summary = _pendingResultSummary;
            _pendingResultSummary = null;
            return summary;
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
    }
}
