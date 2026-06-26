using System;
using System.Collections.Generic;
using TopSpeed.Localization;
using TopSpeed.Menu;
using TopSpeed.Protocol;

namespace TopSpeed.Core.Multiplayer
{
    internal sealed partial class MultiplayerCoordinator
    {
        private void RebuildRoomOptionsMenu()
        {
            var items = new List<MenuItem>();
            if (!_state.Rooms.CurrentRoom.InRoom)
            {
                items.Add(new MenuItem(LocalizationService.Mark("You are not currently inside a game room."), MenuAction.None));
                _menu.UpdateItems(MultiplayerMenuKeys.RoomOptions, items);
                return;
            }

            if (!_state.Rooms.CurrentRoom.IsHost)
            {
                items.Add(new MenuItem(LocalizationService.Mark("Only the host can change game options."), MenuAction.None));
                _menu.UpdateItems(MultiplayerMenuKeys.RoomOptions, items);
                return;
            }

            items.Add(new MenuItem(LocalizationService.Mark("Game rules"), MenuAction.None, onActivate: OpenRoomGameRulesMenu));

            items.Add(new MenuItem(
                () => GetRoomOptionsTrackText(),
                MenuAction.None,
                onActivate: OpenRoomTrackTypeMenu,
                hintProvider: () => InteractionHints.ForPlatform(
                    LocalizationService.Mark("Change the track used during the race."),
                    LocalizationService.Mark("Press ENTER to change."),
                    LocalizationService.Mark("Swipe up to change."))));

            items.Add(new RadioButton(LocalizationService.Mark("Number of laps"),
                LapCountOptions,
                GetRoomOptionsLapsIndex,
                value => SetRoomOptionsLaps((byte)(value + 1)),
                hintProvider: () => InteractionHints.ForPlatform(
                    LocalizationService.Mark("Choose the number of laps for this room."),
                    LocalizationService.Mark("Use LEFT or RIGHT to change."),
                    LocalizationService.Mark("Swipe left or right with two fingers to change."))));

            var maxPlayersItem = new RadioButton(LocalizationService.Mark("Maximum players allowed in this room"),
                RoomCapacityOptions,
                GetRoomOptionsPlayersToStartIndex,
                value => SetRoomOptionsPlayersToStart((byte)(value + 2)),
                hintProvider: () => InteractionHints.ForPlatform(
                    LocalizationService.Mark("Select the player capacity for this room. The host can start with fewer players than the specified maximum players, so this is not a hard requirement."),
                    LocalizationService.Mark("Use LEFT or RIGHT to change."),
                    LocalizationService.Mark("Swipe left or right with two fingers to change.")))
            {
                Hidden = _state.Rooms.CurrentRoom.RoomType == GameRoomType.OneOnOne
            };
            items.Add(maxPlayersItem);

            items.Add(new MenuItem(LocalizationService.Mark("Confirm game options"), MenuAction.None, onActivate: ConfirmRoomOptionsChanges));
            items.Add(new MenuItem(LocalizationService.Mark("Cancel and discard changes"), MenuAction.Back, onActivate: CancelRoomOptionsChanges));
            var preserveSelection = string.Equals(_menu.CurrentId, MultiplayerMenuKeys.RoomOptions, StringComparison.Ordinal);
            _menu.UpdateItems(MultiplayerMenuKeys.RoomOptions, items, preserveSelection);
        }

        private void RebuildRoomGameRulesMenu()
        {
            var items = new List<MenuItem>();
            if (!_state.Rooms.CurrentRoom.InRoom)
            {
                items.Add(new MenuItem(LocalizationService.Mark("You are not currently inside a game room."), MenuAction.None));
                _menu.UpdateItems(MultiplayerMenuKeys.RoomGameRules, items);
                return;
            }

            if (!_state.Rooms.CurrentRoom.IsHost)
            {
                items.Add(new MenuItem(LocalizationService.Mark("Only the host can change game rules."), MenuAction.None));
                _menu.UpdateItems(MultiplayerMenuKeys.RoomGameRules, items);
                return;
            }

            items.Add(new CheckBox(
                LocalizationService.Mark("Ghost mode"),
                GetRoomOptionsGhostModeEnabled,
                SetRoomOptionsGhostModeEnabled,
                hint: LocalizationService.Mark("When enabled, vehicle collisions are disabled and vehicles can pass through each other.")));

            items.Add(new CheckBox(
                LocalizationService.Mark("Custom tracks"),
                GetRoomOptionsCustomTracksEnabled,
                SetRoomOptionsCustomTracksEnabled,
                hint: LocalizationService.Mark("When enabled, room hosts can upload and select custom tracks from this server.")));

            items.Add(new CheckBox(
                LocalizationService.Mark("Enable fuel consumption"),
                GetRoomOptionsFuelConsumptionEnabled,
                SetRoomOptionsFuelConsumptionEnabled,
                hint: LocalizationService.Mark("When enabled, cars burn fuel and players must pit to refuel.")));

            items.Add(new CheckBox(
                LocalizationService.Mark("Enable tire wear"),
                GetRoomOptionsTireWearEnabled,
                SetRoomOptionsTireWearEnabled,
                hint: LocalizationService.Mark("When enabled, tires wear and lose grip and players must pit to replace them.")));

            var preserveSelection = string.Equals(_menu.CurrentId, MultiplayerMenuKeys.RoomGameRules, StringComparison.Ordinal);
            _menu.UpdateItems(MultiplayerMenuKeys.RoomGameRules, items, preserveSelection);
        }

        private string GetRoomOptionsTrackText()
        {
            if (!_state.RoomDrafts.RoomOptionsDraftActive)
                BeginRoomOptionsDraft();

            if (_state.RoomDrafts.RoomOptionsTrackRandom)
            {
                return LocalizationService.Mark("Track, currently random chosen.");
            }

            var trackName = string.IsNullOrWhiteSpace(_state.RoomDrafts.RoomOptionsTrackDisplayName)
                ? FormatTrackRefDisplay(_state.RoomDrafts.RoomOptionsTrack)
                : _state.RoomDrafts.RoomOptionsTrackDisplayName;
            return LocalizationService.Format(
                LocalizationService.Mark("Track, currently {0}."),
                trackName);
        }
    }
}
