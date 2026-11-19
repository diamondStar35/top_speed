using TopSpeed.Drive;
using TopSpeed.Protocol;
using TopSpeed.Tracks;
using TopSpeed.Vehicles;

namespace TopSpeed.Drive.Multiplayer
{
    internal sealed class RuntimeState
    {
        public uint NextMediaId { get; set; }
        public Track.Road CurrentRoad { get; set; }
        public CarState LastRecordedCarState { get; set; }
        public int Lap { get; set; }
        public int RaceTime { get; set; }
        public int LocalCrashCount { get; set; }
        public int UnkeyQueue { get; set; }
        public int PositionFinish { get; set; }
        public int Position { get; set; }
        public int PositionComment { get; set; }
        public float SpeakTime { get; set; }
        public bool Started { get; set; }
        public bool Finished { get; set; }
        public bool ExitWhenQueueIdle { get; set; }
        public bool RequirePostFinishStopBeforeExit { get; set; }
        public bool SentStart { get; set; }
        public bool SentFinish { get; set; }
        public bool ServerStopReceived { get; set; }
        public PlayerState CurrentState { get; set; }
        public CarState LastCarState { get; set; }
        public bool SendFailureAnnounced { get; set; }
        public bool LiveFailureAnnounced { get; set; }
        public bool HostPaused { get; set; }
        public DriveResultSummary? PendingResultSummary { get; set; }
    }
}
