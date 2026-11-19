using TopSpeed.Drive.Session;

namespace TopSpeed.Drive.Multiplayer
{
    internal static class Incoming
    {
        public static readonly ExternalEventId RaceSnapshot = new("multiplayer.raceSnapshot");
        public static readonly ExternalEventId PlayerBumped = new("multiplayer.playerBumped");
        public static readonly ExternalEventId PlayerCrashed = new("multiplayer.playerCrashed");
        public static readonly ExternalEventId PlayerDisconnected = new("multiplayer.playerDisconnected");
        public static readonly ExternalEventId RoomRacePlayerFinished = new("multiplayer.roomRacePlayerFinished");
        public static readonly ExternalEventId RoomRaceCompleted = new("multiplayer.roomRaceCompleted");
        public static readonly ExternalEventId RoomRaceAborted = new("multiplayer.roomRaceAborted");
        public static readonly ExternalEventId RoomParticipantSync = new("multiplayer.roomParticipantSync");
        public static readonly ExternalEventId LiveStart = new("multiplayer.liveStart");
        public static readonly ExternalEventId LiveFrame = new("multiplayer.liveFrame");
        public static readonly ExternalEventId LiveStop = new("multiplayer.liveStop");
        public static readonly ExternalEventId MediaBegin = new("multiplayer.mediaBegin");
        public static readonly ExternalEventId MediaChunk = new("multiplayer.mediaChunk");
        public static readonly ExternalEventId MediaEnd = new("multiplayer.mediaEnd");
    }
}
