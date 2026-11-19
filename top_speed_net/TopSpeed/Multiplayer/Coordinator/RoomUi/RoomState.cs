using System.Collections.Generic;
using TopSpeed.Core.Multiplayer.Chat;
using TopSpeed.Protocol;

namespace TopSpeed.Core.Multiplayer
{
    internal sealed partial class MultiplayerCoordinator
    {
        private sealed partial class RoomUi
        {
            public void HandleRoomState(RoomStateChange change)
            {
                if (!change.Applied)
                    return;

                if (_owner._state.Rooms.CurrentRoom.InRoom)
                    _owner.HandleAuthoritativeRoomGameRulesChanged();

                var effects = new List<PacketEffect>();

                AddRoomJoinLeaveEffects(effects, change.WasInRoom, change.PreviousRoomId);

                if (!_owner._state.Rooms.CurrentRoom.InRoom || !_owner._state.Rooms.CurrentRoom.IsHost)
                    effects.Add(PacketEffect.CancelRoomOptions());

                AddRoomStateNavigationEffects(effects, change.WasInRoom, change.PreviousRoomId);

                var roomControlsChanged =
                    change.WasInRoom != _owner._state.Rooms.CurrentRoom.InRoom ||
                    change.PreviousIsHost != _owner._state.Rooms.CurrentRoom.IsHost ||
                    change.PreviousRoomType != _owner._state.Rooms.CurrentRoom.RoomType;
                if (roomControlsChanged)
                    AddRoomMenuRebuildEffects(effects);

                AddLoadoutCatchUpEffects(effects);
                effects.Add(PacketEffect.RebuildRoomPlayers());
                _owner.DispatchPacketEffects(effects);
            }

            private void AddRoomJoinLeaveEffects(List<PacketEffect> effects, bool wasInRoom, uint previousRoomId)
            {
                if (_owner._state.Rooms.CurrentRoom.InRoom)
                {
                    if (!wasInRoom || previousRoomId != _owner._state.Rooms.CurrentRoom.RoomId)
                    {
                        effects.Add(PacketEffect.PlaySound("room_join.ogg"));
                        effects.Add(PacketEffect.AddRoomEventHistory(HistoryText.JoinedRoom(_owner._state.Rooms.CurrentRoom.RoomName)));
                    }

                    return;
                }

                if (!wasInRoom)
                    return;

                effects.Add(PacketEffect.PlaySound("room_leave.ogg"));
                var leaveText = HistoryText.LeftRoom();
                effects.Add(PacketEffect.Speak(leaveText));
                effects.Add(PacketEffect.AddRoomEventHistory(leaveText));
            }

            private void AddRoomStateNavigationEffects(List<PacketEffect> effects, bool wasInRoom, uint previousRoomId)
            {
                if (_owner._state.Rooms.CurrentRoom.InRoom && (!wasInRoom || previousRoomId != _owner._state.Rooms.CurrentRoom.RoomId))
                {
                    effects.Add(PacketEffect.ShowRoot(MultiplayerMenuKeys.RoomControls));
                }
                else if (!_owner._state.Rooms.CurrentRoom.InRoom && wasInRoom)
                {
                    effects.Add(PacketEffect.ShowRoot(MultiplayerMenuKeys.Lobby));
                }
            }

            private static void AddRoomMenuRebuildEffects(List<PacketEffect> effects)
            {
                effects.Add(PacketEffect.RebuildRoomControls());
                effects.Add(PacketEffect.RebuildRoomOptions());
                effects.Add(PacketEffect.RebuildRoomGameRules());
            }

            private void AddLoadoutCatchUpEffects(List<PacketEffect> effects)
            {
                var inLoadoutMenu = _owner._menu.CurrentId == MultiplayerMenuKeys.LoadoutVehicle
                    || _owner._menu.CurrentId == MultiplayerMenuKeys.LoadoutTransmission;
                var shouldBeInLoadout = _owner._state.Rooms.CurrentRoom.InRoom
                    && _owner._state.Rooms.CurrentRoom.RaceState == RoomRaceState.Preparing;

                if (shouldBeInLoadout && !inLoadoutMenu)
                {
                    effects.Add(PacketEffect.BeginRaceLoadout());
                    return;
                }

                if (!shouldBeInLoadout && inLoadoutMenu)
                    effects.Add(PacketEffect.ShowRoot(MultiplayerMenuKeys.RoomControls));
            }
        }
    }
}
