using System;
using TopSpeed.Localization;
using TopSpeed.Protocol;

namespace TopSpeed.Core.Multiplayer
{
    internal sealed partial class MultiplayerCoordinator
    {
        private int GetRoomOptionsLaps()
        {
            if (!_state.RoomDrafts.RoomOptionsDraftActive)
                BeginRoomOptionsDraft();

            var laps = _state.RoomDrafts.RoomOptionsLaps < 1 ? 1 : _state.RoomDrafts.RoomOptionsLaps;
            return Math.Max(1, Math.Min(500, laps));
        }

        private void SetRoomOptionsLaps(int laps)
        {
            if (!_state.RoomDrafts.RoomOptionsDraftActive)
                BeginRoomOptionsDraft();

            if (laps < 1 || laps > 500)
                return;
            _state.RoomDrafts.RoomOptionsLaps = laps;
        }

        private int GetRoomOptionsPlayersToStartIndex()
        {
            if (!_state.RoomDrafts.RoomOptionsDraftActive)
                BeginRoomOptionsDraft();

            var playersToStart = _state.RoomDrafts.RoomOptionsPlayersToStart < 2 ? (byte)2 : _state.RoomDrafts.RoomOptionsPlayersToStart;
            return Math.Max(0, Math.Min(RoomCapacityOptions.Length - 1, playersToStart - 2));
        }

        private void SetRoomOptionsPlayersToStart(byte playersToStart)
        {
            if (!_state.RoomDrafts.RoomOptionsDraftActive)
                BeginRoomOptionsDraft();

            if (_state.Rooms.CurrentRoom.RoomType == GameRoomType.OneOnOne)
            {
                _state.RoomDrafts.RoomOptionsPlayersToStart = 2;
                return;
            }

            if (playersToStart < 2 || playersToStart > ProtocolConstants.MaxRoomPlayersToStart)
                return;

            _state.RoomDrafts.RoomOptionsPlayersToStart = playersToStart;
        }

        private void OpenRoomGameRulesMenu()
        {
            if (!_state.RoomDrafts.RoomOptionsDraftActive)
                BeginRoomOptionsDraft();

            RebuildRoomGameRulesMenu();
            _menu.Push(MultiplayerMenuKeys.RoomGameRules);
        }

        private bool GetRoomOptionsGhostModeEnabled()
        {
            if (!_state.RoomDrafts.RoomOptionsDraftActive)
                BeginRoomOptionsDraft();

            return (_state.RoomDrafts.RoomOptionsGameRulesFlags & (uint)RoomGameRules.GhostMode) != 0u;
        }

        private void SetRoomOptionsGhostModeEnabled(bool enabled)
        {
            if (!_state.RoomDrafts.RoomOptionsDraftActive)
                BeginRoomOptionsDraft();

            var flags = NormalizeRoomOptionsGameRulesFlags(_state.RoomDrafts.RoomOptionsGameRulesFlags);
            if (enabled)
                flags |= (uint)RoomGameRules.GhostMode;
            else
                flags &= ~(uint)RoomGameRules.GhostMode;

            _state.RoomDrafts.RoomOptionsGameRulesFlags = flags;
        }

        private bool GetRoomOptionsFuelConsumptionEnabled()
        {
            if (!_state.RoomDrafts.RoomOptionsDraftActive)
                BeginRoomOptionsDraft();

            return (_state.RoomDrafts.RoomOptionsGameRulesFlags & (uint)RoomGameRules.FuelConsumption) != 0u;
        }

        private void SetRoomOptionsFuelConsumptionEnabled(bool enabled)
        {
            if (!_state.RoomDrafts.RoomOptionsDraftActive)
                BeginRoomOptionsDraft();

            var flags = NormalizeRoomOptionsGameRulesFlags(_state.RoomDrafts.RoomOptionsGameRulesFlags);
            if (enabled)
                flags |= (uint)RoomGameRules.FuelConsumption;
            else
                flags &= ~(uint)RoomGameRules.FuelConsumption;

            _state.RoomDrafts.RoomOptionsGameRulesFlags = flags;
        }

        private bool GetRoomOptionsTireWearEnabled()
        {
            if (!_state.RoomDrafts.RoomOptionsDraftActive)
                BeginRoomOptionsDraft();

            return (_state.RoomDrafts.RoomOptionsGameRulesFlags & (uint)RoomGameRules.TireWear) != 0u;
        }

        private void SetRoomOptionsTireWearEnabled(bool enabled)
        {
            if (!_state.RoomDrafts.RoomOptionsDraftActive)
                BeginRoomOptionsDraft();

            var flags = NormalizeRoomOptionsGameRulesFlags(_state.RoomDrafts.RoomOptionsGameRulesFlags);
            if (enabled)
                flags |= (uint)RoomGameRules.TireWear;
            else
                flags &= ~(uint)RoomGameRules.TireWear;

            _state.RoomDrafts.RoomOptionsGameRulesFlags = flags;
        }

        private bool GetRoomOptionsCustomTracksEnabled()
        {
            if (!_state.RoomDrafts.RoomOptionsDraftActive)
                BeginRoomOptionsDraft();

            return (_state.RoomDrafts.RoomOptionsGameRulesFlags & (uint)RoomGameRules.CustomTracks) != 0u;
        }

        private bool IsCurrentRoomCustomTracksEnabled()
        {
            var authoritativeFlags = NormalizeRoomOptionsGameRulesFlags(_state.Rooms.CurrentRoom.GameRulesFlags);
            return (authoritativeFlags & (uint)RoomGameRules.CustomTracks) != 0u;
        }

        private void SetRoomOptionsCustomTracksEnabled(bool enabled)
        {
            if (!_state.RoomDrafts.RoomOptionsDraftActive)
                BeginRoomOptionsDraft();

            var flags = NormalizeRoomOptionsGameRulesFlags(_state.RoomDrafts.RoomOptionsGameRulesFlags);
            if (enabled)
                flags |= (uint)RoomGameRules.CustomTracks;
            else
                flags &= ~(uint)RoomGameRules.CustomTracks;

            _state.RoomDrafts.RoomOptionsGameRulesFlags = flags;
        }
    }
}
