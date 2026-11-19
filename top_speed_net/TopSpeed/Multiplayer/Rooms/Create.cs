using System;
using System.Collections.Generic;
using TopSpeed.Menu;
using TopSpeed.Network;
using TopSpeed.Protocol;
using TopSpeed.Speech;

using TopSpeed.Localization;
namespace TopSpeed.Core.Multiplayer
{
    internal sealed partial class MultiplayerCoordinator
    {
        private void RebuildCreateRoomMenu()
        {
            RebuildCreateRoomMenu(preserveSelection: false);
        }

        private void RebuildCreateRoomMenu(bool preserveSelection)
        {
            var maxPlayersItem = new RadioButton(LocalizationService.Mark("Maximum players allowed in this room"),
                RoomCapacityOptions,
                GetCreateRoomPlayersToStartIndex,
                SetCreateRoomPlayersToStart,
                hintProvider: () => InteractionHints.ForPlatform(
                    LocalizationService.Mark("Choose the player capacity from 2 to 10."),
                    LocalizationService.Mark("Use LEFT or RIGHT to change."),
                    LocalizationService.Mark("Swipe left or right with two fingers to change.")))
            {
                Hidden = _state.RoomDrafts.CreateRoomType == GameRoomType.OneOnOne
            };

            var items = new List<MenuItem>
            {
                new RadioButton(LocalizationService.Mark("Game type"),
                    RoomTypeOptions,
                    GetCreateRoomTypeIndex,
                    SetCreateRoomType,
                    hintProvider: () => InteractionHints.ForPlatform(
                        LocalizationService.Mark("Choose whether this room is a race with bots, a multiplayer race without bots, or a one-on-one game."),
                        LocalizationService.Mark("Use LEFT or RIGHT to change."),
                        LocalizationService.Mark("Swipe left or right with two fingers to change."))),
                maxPlayersItem,
                new MenuItem(
                    () => string.IsNullOrWhiteSpace(_state.RoomDrafts.CreateRoomName)
                        ? LocalizationService.Mark("Room name, currently automatic")
                        : LocalizationService.Format(
                            LocalizationService.Mark("Room name, currently {0}"),
                            _state.RoomDrafts.CreateRoomName),
                    MenuAction.None,
                    onActivate: UpdateCreateRoomName,
                    hintProvider: () => InteractionHints.ForPlatform(
                        LocalizationService.Mark("Enter a room name. Leave it empty to use an automatic name."),
                        LocalizationService.Mark("Press ENTER to enter a room name."),
                        LocalizationService.Mark("Swipe up to enter a room name."))),
                new MenuItem(LocalizationService.Mark("Create this game room"), MenuAction.None, onActivate: ConfirmCreateRoom),
                new MenuItem(LocalizationService.Mark("Cancel room creation"), MenuAction.Back)
            };

            _menu.UpdateItems(MultiplayerMenuKeys.CreateRoom, items, preserveSelection);
        }

        private void OpenCreateRoomMenu()
        {
            if (SessionOrNull() == null)
            {
                _speech.Speak(LocalizationService.Mark("Not connected to a server."));
                return;
            }

            ResetCreateRoomDraft();
            RebuildCreateRoomMenu();
            _menu.Push(MultiplayerMenuKeys.CreateRoom);
        }

        private void UpdateCreateRoomName()
        {
            _promptTextInput(
                LocalizationService.Mark("Enter a room name. Leave this field empty to use an automatic room name."),
                _state.RoomDrafts.CreateRoomName,
                SpeechService.SpeakFlag.None,
                true,
                result =>
                {
                    if (result.Cancelled)
                        return;

                    _state.RoomDrafts.CreateRoomName = (result.Text ?? string.Empty).Trim();
                    RebuildCreateRoomMenu();

                    if (string.IsNullOrWhiteSpace(_state.RoomDrafts.CreateRoomName))
                    {
                        _speech.Speak(LocalizationService.Mark("Automatic room name selected."));
                        return;
                    }

                    _speech.Speak(
                        LocalizationService.Format(
                            LocalizationService.Mark("Room name set to {0}."),
                            _state.RoomDrafts.CreateRoomName));
                });
        }

        private void ConfirmCreateRoom()
        {
            var session = SessionOrNull();
            if (session == null)
            {
                _speech.Speak(LocalizationService.Mark("Not connected to a server."));
                return;
            }

            var playersToStart = _state.RoomDrafts.CreateRoomPlayersToStart;
            if (playersToStart < 2 || playersToStart > ProtocolConstants.MaxRoomPlayersToStart)
                playersToStart = 2;
            if (_state.RoomDrafts.CreateRoomType == GameRoomType.OneOnOne)
                playersToStart = 2;

            if (!TrySend(
                    session.SendRoomCreate(_state.RoomDrafts.CreateRoomName, _state.RoomDrafts.CreateRoomType, playersToStart),
                    LocalizationService.Mark("room create request")))
                return;
            _menu.ShowRoot(MultiplayerMenuKeys.Lobby);
        }

        private int GetCreateRoomTypeIndex()
        {
            return _state.RoomDrafts.CreateRoomType switch
            {
                GameRoomType.PlayersRace => 1,
                GameRoomType.OneOnOne => 2,
                _ => 0
            };
        }

        private void SetCreateRoomType(int index)
        {
            _state.RoomDrafts.CreateRoomType = index switch
            {
                2 => GameRoomType.OneOnOne,
                1 => GameRoomType.PlayersRace,
                _ => GameRoomType.BotsRace
            };

            if (_state.RoomDrafts.CreateRoomType == GameRoomType.OneOnOne)
                _state.RoomDrafts.CreateRoomPlayersToStart = 2;

            if (string.Equals(_menu.CurrentId, MultiplayerMenuKeys.CreateRoom, StringComparison.Ordinal))
                RebuildCreateRoomMenu(preserveSelection: true);
        }

        private int GetCreateRoomPlayersToStartIndex()
        {
            var playersToStart = _state.RoomDrafts.CreateRoomPlayersToStart;
            if (_state.RoomDrafts.CreateRoomType == GameRoomType.OneOnOne)
                playersToStart = 2;
            if (playersToStart < 2 || playersToStart > ProtocolConstants.MaxRoomPlayersToStart)
                playersToStart = 2;
            return playersToStart - 2;
        }

        private void SetCreateRoomPlayersToStart(int index)
        {
            if (_state.RoomDrafts.CreateRoomType == GameRoomType.OneOnOne)
            {
                _state.RoomDrafts.CreateRoomPlayersToStart = 2;
                return;
            }

            var playersToStart = (byte)(index + 2);
            if (playersToStart < 2 || playersToStart > ProtocolConstants.MaxRoomPlayersToStart)
                return;
            _state.RoomDrafts.CreateRoomPlayersToStart = playersToStart;
        }

        private void ResetCreateRoomDraft()
        {
            _state.RoomDrafts.CreateRoomType = GameRoomType.BotsRace;
            _state.RoomDrafts.CreateRoomPlayersToStart = 2;
            _state.RoomDrafts.CreateRoomName = string.Empty;
        }
    }
}








