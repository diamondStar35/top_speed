using System;
using TopSpeed.Localization;

namespace TopSpeed.Core.Multiplayer
{
    internal sealed partial class MultiplayerCoordinator
    {
        private sealed partial class RoomUi
        {
            public void HandleOnlinePlayers()
            {
                if (!_owner._state.RoomDrafts.IsOnlinePlayersOpenPending)
                    return;

                _owner._state.RoomDrafts.IsOnlinePlayersOpenPending = false;
                if (!string.Equals(_owner._menu.CurrentId, MultiplayerMenuKeys.Lobby, StringComparison.Ordinal))
                    return;

                var players = _owner._state.Rooms.OnlinePlayers.Players ?? Array.Empty<OnlinePlayerInfo>();
                if (players.Length < 2)
                {
                    _owner._speech.Speak(LocalizationService.Mark("Only you are connected right now."));
                    return;
                }

                _owner.RebuildOnlinePlayersMenu();
                _owner._menu.Push(MultiplayerMenuKeys.OnlinePlayers);
            }
        }
    }
}
