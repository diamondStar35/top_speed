using System;
using System.Collections.Generic;
using TopSpeed.Common;
using TopSpeed.Data;
using TopSpeed.Localization;
using TopSpeed.Menu;
using TopSpeed.Protocol;

namespace TopSpeed.Core.Multiplayer
{
    internal sealed partial class MultiplayerCoordinator
    {
        private void OpenRoomTrackTypeMenu()
        {
            if (!_state.RoomDrafts.RoomOptionsDraftActive)
                BeginRoomOptionsDraft();

            if (!TryApplyRoomGameRulesDraft(announceNotConnected: true, out var appliedGameRules))
                return;
            if (appliedGameRules)
            {
                _state.RoomDrafts.RoomTrackTypeOpenPending = true;
                var session = SessionOrNull();
                if (session != null)
                    TrySend(session.SendRoomStateRequest(), LocalizationService.Mark("room state refresh request"));
                return;
            }

            OpenRoomTrackTypeMenuCore();
        }

        private void OpenRoomTrackTypeMenuCore()
        {
            _state.RoomDrafts.RoomTrackTypeOpenPending = false;
            RebuildRoomTrackTypeMenu();
            RebuildRoomTrackMenu(MultiplayerMenuKeys.RoomTrackRace, TrackCategory.RaceTrack);
            RebuildRoomTrackMenu(MultiplayerMenuKeys.RoomTrackAdventure, TrackCategory.StreetAdventure);
            RebuildRoomTrackCustomMenu();
            RebuildRoomTrackLocalCustomMenu();
            _menu.Push(MultiplayerMenuKeys.RoomTrackType);
        }

        private void RebuildRoomTrackTypeMenu()
        {
            var items = new List<MenuItem>();
            items.Add(new MenuItem(LocalizationService.Mark("Race track"), MenuAction.None, nextMenuId: MultiplayerMenuKeys.RoomTrackRace));
            items.Add(new MenuItem(LocalizationService.Mark("Street adventure"), MenuAction.None, nextMenuId: MultiplayerMenuKeys.RoomTrackAdventure));
            if (IsCurrentRoomCustomTracksEnabled())
            {
                items.Add(new MenuItem(
                    LocalizationService.Mark("Custom track"),
                    MenuAction.None,
                    onActivate: OpenRoomTrackCustomMenu));
            }

            items.Add(new MenuItem(LocalizationService.Mark("Random"), MenuAction.None, onActivate: SelectRandomRoomTrackAny));

            _menu.UpdateItems(MultiplayerMenuKeys.RoomTrackType, items);
        }

        private void RebuildRoomTrackMenu(string menuId, TrackCategory category)
        {
            var items = new List<MenuItem>();
            var tracks = TrackList.GetTracks(category);
            for (var i = 0; i < tracks.Count; i++)
            {
                var track = tracks[i];
                items.Add(new MenuItem(track.Display, MenuAction.None, onActivate: () => SelectRoomTrack(TrackPackageRef.BuiltIn(track.Key), string.Empty, false)));
            }

            items.Add(new MenuItem(LocalizationService.Mark("Random"), MenuAction.None, onActivate: () => SelectRandomRoomTrackCategory(category)));
            _menu.UpdateItems(menuId, items);
        }

        private void SelectRandomRoomTrackAny()
        {
            if (RoomTrackOptions.Length == 0)
            {
                SelectRoomTrack(TrackList.RaceTracks[0].Key, true);
                return;
            }

            var index = Algorithm.RandomInt(RoomTrackOptions.Length);
            SelectRoomTrack(RoomTrackOptions[index].Key, true);
        }

        private void SelectRandomRoomTrackCategory(TrackCategory category)
        {
            var tracks = TrackList.GetTracks(category);
            if (tracks.Count == 0)
            {
                SelectRandomRoomTrackAny();
                return;
            }

            var index = Algorithm.RandomInt(tracks.Count);
            SelectRoomTrack(tracks[index].Key, true);
        }

        private void SelectRoomTrack(string trackKey, bool randomChosen)
        {
            SelectRoomTrack(TrackPackageRef.BuiltIn(trackKey), string.Empty, randomChosen);
        }

        private void SelectRoomTrack(TrackPackageRef track, string displayName, bool randomChosen)
        {
            var normalized = CloneTrackRef(track);
            if (!PacketValidation.IsValidTrackPackageRef(normalized))
                normalized = TrackPackageRef.BuiltIn(TrackList.RaceTracks[0].Key);

            _state.RoomDrafts.RoomOptionsTrack = normalized;
            _state.RoomDrafts.RoomOptionsTrackName = normalized.IsBuiltIn
                ? normalized.BuiltInTrackKey
                : normalized.TrackId;
            _state.RoomDrafts.RoomOptionsTrackDisplayName = string.IsNullOrWhiteSpace(displayName)
                ? FormatTrackRefDisplay(normalized)
                : displayName;
            _state.RoomDrafts.RoomOptionsTrackRandom = randomChosen;
            ReturnToRoomOptionsMenu();
            _speech.Speak(GetRoomOptionsTrackText());
        }

        private void ReturnToRoomOptionsMenu()
        {
            if (string.Equals(_menu.CurrentId, MultiplayerMenuKeys.RoomOptions, StringComparison.Ordinal))
                return;

            while (_menu.CanPop && !string.Equals(_menu.CurrentId, MultiplayerMenuKeys.RoomOptions, StringComparison.Ordinal))
                _menu.PopToPrevious(announceTitle: false);

            if (!string.Equals(_menu.CurrentId, MultiplayerMenuKeys.RoomOptions, StringComparison.Ordinal))
                _menu.Push(MultiplayerMenuKeys.RoomOptions);
        }
    }
}
