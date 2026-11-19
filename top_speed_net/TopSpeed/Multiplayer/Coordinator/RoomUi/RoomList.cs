using System;
using System.Collections.Generic;
using TopSpeed.Localization;

namespace TopSpeed.Core.Multiplayer
{
    internal sealed partial class MultiplayerCoordinator
    {
        private sealed partial class RoomUi
        {
            public void HandleRoomList()
            {
                var effects = new List<PacketEffect>();
                if (string.Equals(_owner._menu.CurrentId, MultiplayerMenuKeys.RoomBrowser, StringComparison.Ordinal))
                {
                    effects.Add(PacketEffect.UpdateRoomBrowser());
                    _owner.DispatchPacketEffects(effects);
                    return;
                }

                if (!_owner._state.RoomDrafts.IsRoomBrowserOpenPending)
                    return;

                _owner._state.RoomDrafts.IsRoomBrowserOpenPending = false;
                if (!string.Equals(_owner._menu.CurrentId, MultiplayerMenuKeys.Lobby, StringComparison.Ordinal))
                    return;

                var rooms = _owner._state.Rooms.RoomList.Rooms ?? Array.Empty<RoomSummaryInfo>();
                if (rooms.Length == 0)
                {
                    effects.Add(PacketEffect.Speak(LocalizationService.Mark("No game rooms are currently available.")));
                    _owner.DispatchPacketEffects(effects);
                    return;
                }

                effects.Add(PacketEffect.UpdateRoomBrowser());
                effects.Add(PacketEffect.Push(MultiplayerMenuKeys.RoomBrowser));
                _owner.DispatchPacketEffects(effects);
            }
        }
    }
}
