using System;
using System.Collections.Generic;
using TopSpeed.Menu;
using TopSpeed.Protocol;

using TopSpeed.Localization;
namespace TopSpeed.Core.Multiplayer
{
    internal sealed partial class MultiplayerCoordinator
    {
        private const string OnlinePlayersScreenId = "online_players_main";
        private static readonly string MainRoomName = LocalizationService.Mark("main room");

        private void OpenOnlinePlayersMenu()
        {
            var session = SessionOrNull();
            if (session == null)
            {
                _speech.Speak(LocalizationService.Mark("Not connected to a server."));
                return;
            }

            if (_state.RoomDrafts.IsOnlinePlayersOpenPending)
                return;

            _state.RoomDrafts.IsOnlinePlayersOpenPending = true;
            if (!TrySend(session.SendOnlinePlayersRequest(), LocalizationService.Mark("online players request")))
                _state.RoomDrafts.IsOnlinePlayersOpenPending = false;
        }

        private void RebuildOnlinePlayersMenu()
        {
            var players = _state.Rooms.OnlinePlayers.Players ?? Array.Empty<OnlinePlayerInfo>();
            var items = new List<MenuItem>();
            for (var i = 0; i < players.Length; i++)
            {
                items.Add(new MenuItem(FormatOnlinePlayerLabel(players[i]), MenuAction.None));
            }

            _menu.SetScreens(
                MultiplayerMenuKeys.OnlinePlayers,
                new[]
                {
                    new MenuView(
                        OnlinePlayersScreenId,
                        items,
                        LocalizationService.Format(
                            LocalizationService.Mark("{0} people are connected."),
                            players.Length),
                        spec: ScreenSpec.Back)
                },
                OnlinePlayersScreenId);
        }

        private static string FormatOnlinePlayerLabel(OnlinePlayerInfo player)
        {
            var name = string.IsNullOrWhiteSpace(player.Name)
                ? LocalizationService.Format(LocalizationService.Mark("Player {0}"), player.PlayerNumber + 1)
                : player.Name;
            var roomName = string.IsNullOrWhiteSpace(player.RoomName)
                ? LocalizationService.Translate(MainRoomName)
                : player.RoomName;
            var state = player.PresenceState switch
            {
                OnlinePresenceState.PreparingToRace => LocalizationService.Translate(LocalizationService.Mark("Preparing to race")),
                OnlinePresenceState.Racing => LocalizationService.Translate(LocalizationService.Mark("racing")),
                _ => LocalizationService.Translate(LocalizationService.Mark("available"))
            };
            return $"{name}, {state}: {roomName}";
        }
    }
}





