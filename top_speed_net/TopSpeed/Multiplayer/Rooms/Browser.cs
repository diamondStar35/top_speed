using System;
using System.Collections.Generic;
using TopSpeed.Localization;
using TopSpeed.Menu;
using TopSpeed.Protocol;

namespace TopSpeed.Core.Multiplayer
{
    internal sealed partial class MultiplayerCoordinator
    {
        private void OpenRoomBrowser()
        {
            var session = SessionOrNull();
            if (session == null)
            {
                _speech.Speak(LocalizationService.Mark("Not connected to a server."));
                return;
            }

            if (_state.RoomDrafts.IsRoomBrowserOpenPending)
                return;

            _state.RoomDrafts.IsRoomBrowserOpenPending = true;
            if (!TrySend(session.SendRoomListRequest(), LocalizationService.Mark("room list request")))
                _state.RoomDrafts.IsRoomBrowserOpenPending = false;
        }

        private void JoinRoom(uint roomId)
        {
            var session = SessionOrNull();
            if (session == null)
            {
                _speech.Speak(LocalizationService.Mark("Not connected to a server."));
                return;
            }

            TrySend(session.SendRoomJoin(roomId), LocalizationService.Mark("room join request"));
        }

        private void UpdateRoomBrowserMenu()
        {
            var items = new List<MenuItem>();
            var rooms = _state.Rooms.RoomList.Rooms ?? Array.Empty<RoomSummaryInfo>();
            if (rooms.Length == 0)
            {
                items.Add(new MenuItem(LocalizationService.Mark("No game rooms found"), MenuAction.None));
            }
            else
            {
                foreach (var room in rooms)
                {
                    var roomCopy = room;
                    var label = BuildRoomBrowserLabel(roomCopy);
                    items.Add(new MenuItem(label, MenuAction.None, onActivate: () => JoinRoom(roomCopy.RoomId)));
                }
            }
            _menu.UpdateItems(MultiplayerMenuKeys.RoomBrowser, items);
        }

        private static string BuildRoomBrowserLabel(RoomSummaryInfo room)
        {
            var typeText = room.RoomType switch
            {
                GameRoomType.OneOnOne => LocalizationService.Translate(LocalizationService.Mark("one-on-one")),
                GameRoomType.PlayersRace => LocalizationService.Translate(LocalizationService.Mark("race without bots")),
                _ => LocalizationService.Translate(LocalizationService.Mark("race with bots"))
            };

            var label = string.IsNullOrWhiteSpace(room.RoomName)
                ? LocalizationService.Format(
                    LocalizationService.Mark("{0} game with {1} people, maximum {2} players"),
                    typeText,
                    room.PlayerCount,
                    room.PlayersToStart)
                : LocalizationService.Format(
                    LocalizationService.Mark("{0}, {1} game with {2} people, maximum {3} players"),
                    typeText,
                    room.RoomName,
                    room.PlayerCount,
                    room.PlayersToStart);
            if (room.RaceState == RoomRaceState.Preparing || room.RaceState == RoomRaceState.Racing)
                label = LocalizationService.Format(LocalizationService.Mark("{0}, in progress"), label);
            else if (room.PlayerCount >= room.PlayersToStart)
                label = LocalizationService.Format(LocalizationService.Mark("{0}, room is full"), label);
            return label;
        }
    }
}


