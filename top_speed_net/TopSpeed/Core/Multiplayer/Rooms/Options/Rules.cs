using TopSpeed.Localization;
using TopSpeed.Protocol;

namespace TopSpeed.Core.Multiplayer
{
    internal sealed partial class MultiplayerCoordinator
    {
        private void AnnounceCurrentRoomGameRules()
        {
            if (!_state.Rooms.CurrentRoom.InRoom)
            {
                _speech.Speak(LocalizationService.Mark("You are not currently inside a game room."));
                return;
            }

            var room = _state.Rooms.CurrentRoom;
            _speech.Speak(FormatGameRulesSummary(
                room.GameRulesFlags,
                room.Track,
                room.TrackName,
                room.Laps,
                room.PlayersToStart));
        }

        private static string FormatGameRulesSummary(
            uint gameRulesFlags,
            TrackPackageRef track,
            string trackName,
            int laps,
            byte playersToStart)
        {
            var ghostEnabled = (gameRulesFlags & (uint)RoomGameRules.GhostMode) != 0u;
            var customTracksEnabled = (gameRulesFlags & (uint)RoomGameRules.CustomTracks) != 0u;
            var fuelEnabled = (gameRulesFlags & (uint)RoomGameRules.FuelConsumption) != 0u;
            var tireWearEnabled = (gameRulesFlags & (uint)RoomGameRules.TireWear) != 0u;
            var trackDisplay = ResolveTrackAnnouncement(track, trackName);
            var normalizedLaps = laps > 0 ? laps : 1;
            var normalizedPlayers = playersToStart >= 2 ? playersToStart : (byte)2;
            var lapsText = LocalizationService.Format(
                normalizedLaps == 1
                    ? LocalizationService.Mark("{0} lap")
                    : LocalizationService.Mark("{0} laps"),
                normalizedLaps);
            var playersText = LocalizationService.Format(
                normalizedPlayers == 1
                    ? LocalizationService.Mark("{0} player")
                    : LocalizationService.Mark("{0} players"),
                normalizedPlayers);
            return LocalizationService.Format(
                LocalizationService.Mark("Ghost mode is {0}. Custom tracks are {1}. Fuel consumption is {2}. Tire wear is {3}. The chosen track is {4}. The game will run for {5}. This room is limited to {6}."),
                ghostEnabled
                    ? LocalizationService.Translate(LocalizationService.Mark("enabled"))
                    : LocalizationService.Translate(LocalizationService.Mark("disabled")),
                customTracksEnabled
                    ? LocalizationService.Translate(LocalizationService.Mark("enabled"))
                    : LocalizationService.Translate(LocalizationService.Mark("disabled")),
                fuelEnabled
                    ? LocalizationService.Translate(LocalizationService.Mark("enabled"))
                    : LocalizationService.Translate(LocalizationService.Mark("disabled")),
                tireWearEnabled
                    ? LocalizationService.Translate(LocalizationService.Mark("enabled"))
                    : LocalizationService.Translate(LocalizationService.Mark("disabled")),
                trackDisplay,
                lapsText,
                playersText);
        }

        private static uint NormalizeRoomOptionsGameRulesFlags(uint flags)
        {
            return flags & ((uint)RoomGameRules.GhostMode
                | (uint)RoomGameRules.CustomTracks
                | (uint)RoomGameRules.FuelConsumption
                | (uint)RoomGameRules.TireWear);
        }

        private void HandleAuthoritativeRoomGameRulesChanged()
        {
            var authoritativeFlags = NormalizeRoomOptionsGameRulesFlags(_state.Rooms.CurrentRoom.GameRulesFlags);
            _state.RoomDrafts.RoomOptionsAppliedGameRulesFlags = authoritativeFlags;

            if (!_state.RoomDrafts.RoomTrackTypeOpenPending)
                return;

            if (!_state.RoomDrafts.RoomOptionsDraftActive || !_state.Rooms.CurrentRoom.InRoom || !_state.Rooms.CurrentRoom.IsHost)
            {
                _state.RoomDrafts.RoomTrackTypeOpenPending = false;
                return;
            }

            var inRoomOptionsFlow = string.Equals(_menu.CurrentId, MultiplayerMenuKeys.RoomOptions, System.StringComparison.Ordinal)
                || string.Equals(_menu.CurrentId, MultiplayerMenuKeys.RoomGameRules, System.StringComparison.Ordinal);
            if (!inRoomOptionsFlow)
            {
                _state.RoomDrafts.RoomTrackTypeOpenPending = false;
                return;
            }

            var desiredFlags = NormalizeRoomOptionsGameRulesFlags(_state.RoomDrafts.RoomOptionsGameRulesFlags);
            if (authoritativeFlags != desiredFlags)
                return;

            OpenRoomTrackTypeMenuCore();
        }
    }
}
