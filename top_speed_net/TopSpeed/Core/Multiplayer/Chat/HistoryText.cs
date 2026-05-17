using TopSpeed.Localization;
using TopSpeed.Protocol;

namespace TopSpeed.Core.Multiplayer.Chat
{
    internal static class HistoryText
    {
        public static string JoinedRoom(string roomName)
        {
            return LocalizationService.Format(
                LocalizationService.Mark("You joined {0}."),
                NormalizeRoomName(roomName));
        }

        public static string LeftRoom()
        {
            return LocalizationService.Mark("You left the game room.");
        }

        public static string BecameHost()
        {
            return LocalizationService.Mark("You are now host of this room.");
        }

        public static string ParticipantJoined(RoomEventInfo roomEvent)
        {
            return LocalizationService.Format(
                LocalizationService.Mark("{0} joined the current room."),
                ResolvePlayerName(roomEvent));
        }

        public static string ParticipantLeft(RoomEventInfo roomEvent)
        {
            return LocalizationService.Format(
                LocalizationService.Mark("{0} left the current room."),
                ResolvePlayerName(roomEvent));
        }

        public static string BotAdded(RoomEventInfo roomEvent)
        {
            return LocalizationService.Format(
                LocalizationService.Mark("{0} was added to the current room."),
                ResolvePlayerName(roomEvent));
        }

        public static string BotRemoved(RoomEventInfo roomEvent)
        {
            return LocalizationService.Format(
                LocalizationService.Mark("{0} was removed from the current room."),
                ResolvePlayerName(roomEvent));
        }

        public static string FromRoomEvent(RoomEventInfo roomEvent)
        {
            switch (roomEvent.Kind)
            {
                case RoomEventKind.RoomCreated:
                    return string.Empty;

                case RoomEventKind.RoomRemoved:
                    return LocalizationService.Format(
                        LocalizationService.Mark("Room removed: {0}."),
                        NormalizeRoomName(roomEvent.RoomName));

                case RoomEventKind.HostChanged:
                    return LocalizationService.Mark("Room host changed.");

                case RoomEventKind.TrackChanged:
                    return LocalizationService.Format(
                        LocalizationService.Mark("Track changed to {0}."),
                        ResolveTrackName(roomEvent.Track, roomEvent.TrackName));

                case RoomEventKind.LapsChanged:
                    return LocalizationService.Format(
                        LocalizationService.Mark("Number of laps changed to {0}."),
                        roomEvent.Laps);

                case RoomEventKind.PlayersToStartChanged:
                    return LocalizationService.Format(
                        LocalizationService.Mark("Maximum players changed to {0}."),
                        roomEvent.PlayersToStart);

                case RoomEventKind.ParticipantJoined:
                    return ParticipantJoined(roomEvent);

                case RoomEventKind.ParticipantLeft:
                    return ParticipantLeft(roomEvent);

                case RoomEventKind.ParticipantStateChanged:
                    return LocalizationService.Format(
                        LocalizationService.Mark("{0} changed ready state."),
                        ResolvePlayerName(roomEvent));

                case RoomEventKind.BotAdded:
                    return BotAdded(roomEvent);

                case RoomEventKind.BotRemoved:
                    return BotRemoved(roomEvent);

                case RoomEventKind.PrepareStarted:
                    return LocalizationService.Mark("Race preparation started.");

                case RoomEventKind.PrepareCancelled:
                    return LocalizationService.Mark("Race preparation was cancelled.");

                case RoomEventKind.RaceStarted:
                    return LocalizationService.Mark("Race started.");

                case RoomEventKind.RaceStopped:
                    return LocalizationService.Mark("The current race was stopped.");

                case RoomEventKind.GameRulesChanged:
                    return LocalizationService.Mark("Game rules changed.");

                case RoomEventKind.RacePaused:
                    return LocalizationService.Mark("Race paused.");

                case RoomEventKind.RaceResumed:
                    return LocalizationService.Mark("Race resumed.");

                default:
                    return string.Empty;
            }
        }

        private static string ResolvePlayerName(RoomEventInfo roomEvent)
        {
            if (!string.IsNullOrWhiteSpace(roomEvent.SubjectPlayerName))
                return roomEvent.SubjectPlayerName.Trim();
            return LocalizationService.Format(
                LocalizationService.Mark("Player {0}"),
                roomEvent.SubjectPlayerNumber + 1);
        }

        private static string NormalizeRoomName(string roomName)
        {
            if (!string.IsNullOrWhiteSpace(roomName))
                return roomName.Trim();
            return LocalizationService.Translate(LocalizationService.Mark("game room"));
        }

        private static string ResolveTrackName(TrackPackageRef track, string trackName)
        {
            var display = MultiplayerCoordinator.FormatTrackDisplayName(track, trackName);
            return string.IsNullOrWhiteSpace(display)
                ? LocalizationService.Translate(LocalizationService.Mark("Unknown"))
                : display;
        }
    }
}

