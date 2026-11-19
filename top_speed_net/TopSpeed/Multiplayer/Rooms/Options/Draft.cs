using TopSpeed.Data;
using TopSpeed.Localization;
using TopSpeed.Protocol;

namespace TopSpeed.Core.Multiplayer
{
    internal sealed partial class MultiplayerCoordinator
    {
        private void OpenRoomOptionsMenu()
        {
            if (!_state.Rooms.CurrentRoom.InRoom)
            {
                _speech.Speak(LocalizationService.Mark("You are not currently inside a game room."));
                return;
            }

            if (!_state.Rooms.CurrentRoom.IsHost)
            {
                _speech.Speak(LocalizationService.Mark("Only the host can change game options."));
                return;
            }

            BeginRoomOptionsDraft();
            RebuildRoomOptionsMenu();
            _menu.Push(MultiplayerMenuKeys.RoomOptions);
        }

        private void BeginRoomOptionsDraft()
        {
            _state.RoomDrafts.RoomOptionsDraftActive = true;
            _state.RoomDrafts.RoomOptionsTrackRandom = false;
            var currentTrack = _state.Rooms.CurrentRoom.Track;
            if (currentTrack == null || !PacketValidation.IsValidTrackPackageRef(currentTrack))
                currentTrack = TrackPackageRef.BuiltIn(_state.Rooms.CurrentRoom.TrackName);
            if (currentTrack == null || !PacketValidation.IsValidTrackPackageRef(currentTrack))
                currentTrack = TrackPackageRef.BuiltIn(TrackList.RaceTracks[0].Key);

            _state.RoomDrafts.RoomOptionsTrack = CloneTrackRef(currentTrack);
            _state.RoomDrafts.RoomOptionsTrackRandom = _state.RoomDrafts.RoomOptionsTrack.IsRandomBuiltIn;
            _state.RoomDrafts.RoomOptionsTrackName = _state.RoomDrafts.RoomOptionsTrack.IsBuiltIn
                ? _state.RoomDrafts.RoomOptionsTrack.BuiltInTrackKey
                : _state.RoomDrafts.RoomOptionsTrack.TrackId;
            _state.RoomDrafts.RoomOptionsTrackDisplayName = FormatTrackRefDisplay(_state.RoomDrafts.RoomOptionsTrack);
            _state.RoomDrafts.RoomOptionsLaps = _state.Rooms.CurrentRoom.Laps > 0 ? _state.Rooms.CurrentRoom.Laps : 1;
            _state.RoomDrafts.RoomOptionsPlayersToStart = _state.Rooms.CurrentRoom.PlayersToStart >= 2 ? _state.Rooms.CurrentRoom.PlayersToStart : (byte)2;
            var gameRules = NormalizeRoomOptionsGameRulesFlags(_state.Rooms.CurrentRoom.GameRulesFlags);
            _state.RoomDrafts.RoomOptionsGameRulesFlags = gameRules;
            _state.RoomDrafts.RoomOptionsAppliedGameRulesFlags = gameRules;
            if (_state.Rooms.CurrentRoom.RoomType == GameRoomType.OneOnOne)
                _state.RoomDrafts.RoomOptionsPlayersToStart = 2;
        }

        private void CancelRoomOptionsChanges()
        {
            _state.RoomDrafts.RoomOptionsDraftActive = false;
            _state.RoomDrafts.RoomOptionsTrackRandom = false;
            _state.RoomDrafts.RoomOptionsTrack = TrackPackageRef.BuiltIn(string.Empty);
            _state.RoomDrafts.RoomOptionsTrackDisplayName = string.Empty;
            _state.RoomDrafts.RoomOptionsGameRulesFlags = 0;
            _state.RoomDrafts.RoomOptionsAppliedGameRulesFlags = 0;
            _state.RoomDrafts.RoomTrackTypeOpenPending = false;
            _state.RoomDrafts.RoomTrackCatalogOpenPending = false;
            _state.RoomDrafts.RoomTrackUploadReturnToCatalog = false;
        }

        private void ConfirmRoomOptionsChanges()
        {
            var session = SessionOrNull();
            if (session == null)
            {
                _speech.Speak(LocalizationService.Mark("Not connected to a server."));
                return;
            }

            if (!_state.Rooms.CurrentRoom.InRoom || !_state.Rooms.CurrentRoom.IsHost || !_state.RoomDrafts.RoomOptionsDraftActive)
            {
                _speech.Speak(LocalizationService.Mark("Only the host can change game options."));
                return;
            }

            var appliedAny = false;
            var currentTrack = _state.Rooms.CurrentRoom.Track;
            if (currentTrack == null || !PacketValidation.IsValidTrackPackageRef(currentTrack))
                currentTrack = TrackPackageRef.BuiltIn(_state.Rooms.CurrentRoom.TrackName);
            if (currentTrack == null || !PacketValidation.IsValidTrackPackageRef(currentTrack))
                currentTrack = TrackPackageRef.BuiltIn(TrackList.RaceTracks[0].Key);

            if (!TrackRefsEqual(currentTrack, _state.RoomDrafts.RoomOptionsTrack))
            {
                if (!TrySend(session.SendRoomSetTrack(_state.RoomDrafts.RoomOptionsTrack), LocalizationService.Mark("track change request")))
                    return;
                appliedAny = true;
            }

            if (_state.Rooms.CurrentRoom.Laps != _state.RoomDrafts.RoomOptionsLaps)
            {
                if (!TrySend(session.SendRoomSetLaps(_state.RoomDrafts.RoomOptionsLaps), LocalizationService.Mark("lap count change request")))
                    return;
                appliedAny = true;
            }

            if (_state.Rooms.CurrentRoom.RoomType != GameRoomType.OneOnOne)
            {
                var playersToStart = _state.RoomDrafts.RoomOptionsPlayersToStart < 2 ? (byte)2 : _state.RoomDrafts.RoomOptionsPlayersToStart;
                if (_state.Rooms.CurrentRoom.PlayersToStart != playersToStart)
                {
                    if (!TrySend(session.SendRoomSetPlayersToStart(playersToStart), LocalizationService.Mark("player count change request")))
                        return;
                    appliedAny = true;
                }
            }

            if (!TryApplyRoomGameRulesDraft(announceNotConnected: false, out var appliedGameRules))
                return;
            if (appliedGameRules)
                appliedAny = true;

            CancelRoomOptionsChanges();
            _menu.ShowRoot(MultiplayerMenuKeys.RoomControls);
            _speech.Speak(appliedAny
                ? LocalizationService.Mark("Room options updated.")
                : LocalizationService.Mark("No option changes to apply."));
        }

        private bool TryApplyRoomGameRulesDraft(bool announceNotConnected, out bool appliedAny)
        {
            appliedAny = false;
            if (!_state.RoomDrafts.RoomOptionsDraftActive || !_state.Rooms.CurrentRoom.InRoom || !_state.Rooms.CurrentRoom.IsHost)
                return true;

            var session = SessionOrNull();
            if (session == null)
            {
                if (announceNotConnected)
                    _speech.Speak(LocalizationService.Mark("Not connected to a server."));
                return false;
            }

            var gameRules = NormalizeRoomOptionsGameRulesFlags(_state.RoomDrafts.RoomOptionsGameRulesFlags);
            _state.RoomDrafts.RoomOptionsGameRulesFlags = gameRules;
            var authoritativeGameRules = NormalizeRoomOptionsGameRulesFlags(_state.Rooms.CurrentRoom.GameRulesFlags);
            _state.RoomDrafts.RoomOptionsAppliedGameRulesFlags = authoritativeGameRules;
            if (authoritativeGameRules == gameRules)
                return true;

            if (!TrySend(session.SendRoomSetGameRules(gameRules), LocalizationService.Mark("game rules change request")))
                return false;

            appliedAny = true;
            return true;
        }
    }
}
