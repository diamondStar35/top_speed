namespace TopSpeed.Drive.Multiplayer
{
    internal sealed partial class MultiplayerSession
    {
        private uint _nextMediaId
        {
            get => _runtime.NextMediaId;
            set => _runtime.NextMediaId = value;
        }

        private Tracks.Track.Road _currentRoad
        {
            get => _runtime.CurrentRoad;
            set => _runtime.CurrentRoad = value;
        }

        private Vehicles.CarState _lastRecordedCarState
        {
            get => _runtime.LastRecordedCarState;
            set => _runtime.LastRecordedCarState = value;
        }

        private int _lap
        {
            get => _runtime.Lap;
            set => _runtime.Lap = value;
        }

        private int _raceTime
        {
            get => _runtime.RaceTime;
            set => _runtime.RaceTime = value;
        }

        private int _localCrashCount
        {
            get => _runtime.LocalCrashCount;
            set => _runtime.LocalCrashCount = value;
        }

        private int _unkeyQueue
        {
            get => _runtime.UnkeyQueue;
            set => _runtime.UnkeyQueue = value;
        }

        private int _positionFinish
        {
            get => _runtime.PositionFinish;
            set => _runtime.PositionFinish = value;
        }

        private int _position
        {
            get => _runtime.Position;
            set => _runtime.Position = value;
        }

        private int _positionComment
        {
            get => _runtime.PositionComment;
            set => _runtime.PositionComment = value;
        }

        private float _speakTime
        {
            get => _runtime.SpeakTime;
            set => _runtime.SpeakTime = value;
        }

        private bool _started
        {
            get => _runtime.Started;
            set => _runtime.Started = value;
        }

        private bool _finished
        {
            get => _runtime.Finished;
            set => _runtime.Finished = value;
        }

        private bool _exitWhenQueueIdle
        {
            get => _runtime.ExitWhenQueueIdle;
            set => _runtime.ExitWhenQueueIdle = value;
        }

        private bool _requirePostFinishStopBeforeExit
        {
            get => _runtime.RequirePostFinishStopBeforeExit;
            set => _runtime.RequirePostFinishStopBeforeExit = value;
        }

        private bool _sentStart
        {
            get => _runtime.SentStart;
            set => _runtime.SentStart = value;
        }

        private bool _sentFinish
        {
            get => _runtime.SentFinish;
            set => _runtime.SentFinish = value;
        }

        private bool _serverStopReceived
        {
            get => _runtime.ServerStopReceived;
            set => _runtime.ServerStopReceived = value;
        }

        private Protocol.PlayerState _currentState
        {
            get => _runtime.CurrentState;
            set => _runtime.CurrentState = value;
        }

        private Vehicles.CarState _lastCarState
        {
            get => _runtime.LastCarState;
            set => _runtime.LastCarState = value;
        }

        private bool _sendFailureAnnounced
        {
            get => _runtime.SendFailureAnnounced;
            set => _runtime.SendFailureAnnounced = value;
        }

        private bool _liveFailureAnnounced
        {
            get => _runtime.LiveFailureAnnounced;
            set => _runtime.LiveFailureAnnounced = value;
        }

        private bool _hostPaused
        {
            get => _runtime.HostPaused;
            set => _runtime.HostPaused = value;
        }

        private DriveResultSummary? _pendingResultSummary
        {
            get => _runtime.PendingResultSummary;
            set => _runtime.PendingResultSummary = value;
        }

        private uint _lastRaceSnapshotSequence
        {
            get => _snapshots.LastSequence;
            set => _snapshots.LastSequence = value;
        }

        private uint _lastRaceSnapshotTick
        {
            get => _snapshots.LastTick;
            set => _snapshots.LastTick = value;
        }

        private bool _hasRaceSnapshotSequence
        {
            get => _snapshots.HasSequence;
            set => _snapshots.HasSequence = value;
        }

        private float _snapshotTickNow
        {
            get => _snapshots.TickNow;
            set => _snapshots.TickNow = value;
        }

        private bool _hasSnapshotTickNow
        {
            get => _snapshots.HasTickNow;
            set => _snapshots.HasTickNow = value;
        }

        private System.Collections.Generic.List<SnapshotFrame> _snapshotFrames => _snapshots.Frames;
        private System.Collections.Generic.List<byte> _missingSnapshotPlayers => _snapshots.MissingPlayers;

        private System.Collections.Generic.Dictionary<byte, RemotePlayer> _remotePlayers => _participants.RemotePlayers;
        private System.Collections.Generic.Dictionary<byte, MediaTransfer> _remoteMediaTransfers => _participants.RemoteMediaTransfers;
        private System.Collections.Generic.Dictionary<byte, LiveState> _remoteLiveStates => _participants.RemoteLiveStates;
        private System.Collections.Generic.List<byte> _expiredLivePlayers => _participants.ExpiredLivePlayers;
        private bool[] _disconnectedPlayerSlots => _participants.DisconnectedSlots;
    }
}
