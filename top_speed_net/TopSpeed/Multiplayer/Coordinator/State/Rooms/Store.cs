using System;
using TopSpeed.Localization;
using TopSpeed.Protocol;

namespace TopSpeed.Core.Multiplayer
{
    internal sealed partial class RoomStore
    {
        private uint _latestRoomEventSequence;
        private uint _latestRoomStateSequence;
        private uint _latestRoomSequenceRoomId;

        public RoomListInfo RoomList = new RoomListInfo();
        public RoomSnapshot CurrentRoom = new RoomSnapshot { InRoom = false, Players = Array.Empty<RoomParticipant>() };
        public MultiplayerClientState ClientState = MultiplayerClientState.Disconnected;
        public bool WasInRoom;
        public uint LastRoomId;
        public bool WasHost;
        public OnlineListInfo OnlinePlayers = new OnlineListInfo();

        public void Reset()
        {
            RoomList = new RoomListInfo();
            CurrentRoom = new RoomSnapshot { InRoom = false, Players = Array.Empty<RoomParticipant>() };
            ClientState = MultiplayerClientState.Disconnected;
            WasInRoom = false;
            LastRoomId = 0;
            WasHost = false;
            OnlinePlayers = new OnlineListInfo();
            _latestRoomEventSequence = 0;
            _latestRoomStateSequence = 0;
            _latestRoomSequenceRoomId = 0;
        }

        public RoomStateChange ApplyRoomState(PacketRoomState roomState)
        {
            var change = new RoomStateChange(WasInRoom, LastRoomId, WasHost, CurrentRoom.RoomType);
            if (IsStaleRoomState(roomState))
                return change.WithApplied(false);

            var nextRoomId = roomState != null && roomState.InRoom ? roomState.RoomId : 0u;
            if (_latestRoomSequenceRoomId != nextRoomId)
            {
                _latestRoomSequenceRoomId = nextRoomId;
                _latestRoomEventSequence = 0;
                _latestRoomStateSequence = 0;
            }

            CurrentRoom = RoomMap.ToSnapshot(roomState);
            if (roomState != null && roomState.EventSequence != 0)
                _latestRoomStateSequence = roomState.EventSequence;
            CurrentRoom.EventSequence = Math.Max(_latestRoomEventSequence, CurrentRoom.EventSequence);
            WasInRoom = CurrentRoom.InRoom;
            LastRoomId = CurrentRoom.RoomId;
            WasHost = CurrentRoom.IsHost;
            UpdateClientStateFromRoom();
            return change.WithApplied(true);
        }

        public RoomRaceChange ApplyRaceState(PacketRoomRaceStateChanged roomRaceStateChanged)
        {
            if (roomRaceStateChanged == null || roomRaceStateChanged.RoomId == 0)
                return default;

            var beginLoadout = false;
            var leaveLoadout = false;

            if (CurrentRoom.InRoom && CurrentRoom.RoomId == roomRaceStateChanged.RoomId)
            {
                if (IsStaleEvent(roomRaceStateChanged.EventSequence, roomRaceStateChanged.RoomId))
                    return new RoomRaceChange(beginLoadout, leaveLoadout, applied: false);
                if (roomRaceStateChanged.RoomVersion != 0 && CurrentRoom.RoomVersion > roomRaceStateChanged.RoomVersion)
                    return new RoomRaceChange(beginLoadout, leaveLoadout, applied: false);

                var previousRaceState = CurrentRoom.RaceState;
                if (roomRaceStateChanged.EventSequence != 0)
                    AdvanceEventSequence(roomRaceStateChanged.RoomId, roomRaceStateChanged.EventSequence);
                CurrentRoom.RoomVersion = roomRaceStateChanged.RoomVersion;
                CurrentRoom.RaceInstanceId = roomRaceStateChanged.RaceInstanceId;
                var nextState = RoomRules.NormalizeRaceState(roomRaceStateChanged.State);
                CurrentRoom.RaceState = nextState;
                UpdateClientStateFromRoom();
                beginLoadout = nextState == RoomRaceState.Preparing && previousRaceState != RoomRaceState.Preparing;
                leaveLoadout = previousRaceState == RoomRaceState.Preparing && nextState != RoomRaceState.Preparing;
            }

            UpdateRoomListRaceState(roomRaceStateChanged.RoomId, roomRaceStateChanged.RoomVersion, RoomRules.NormalizeRaceState(roomRaceStateChanged.State));
            return new RoomRaceChange(beginLoadout, leaveLoadout, applied: true);
        }

        public void ApplyRoomList(PacketRoomList roomList)
        {
            RoomList = RoomMap.ToList(roomList);
        }

        public void ApplyOnlinePlayers(PacketOnlinePlayers onlinePlayers)
        {
            OnlinePlayers = OnlineMap.ToList(onlinePlayers);
        }

        public string ResolvePlayerName(byte playerNumber)
        {
            var players = CurrentRoom.Players ?? Array.Empty<RoomParticipant>();
            for (var i = 0; i < players.Length; i++)
            {
                var player = players[i];
                if (player == null || player.PlayerNumber != playerNumber)
                    continue;
                if (!string.IsNullOrWhiteSpace(player.Name))
                    return player.Name.Trim();
                break;
            }

            return LocalizationService.Format(LocalizationService.Mark("Player {0}"), playerNumber + 1);
        }

        private bool IsStaleRoomState(PacketRoomState roomState)
        {
            if (roomState == null)
                return false;
            if (!CurrentRoom.InRoom || !roomState.InRoom)
                return false;
            if (CurrentRoom.RoomId != roomState.RoomId)
                return false;
            if (roomState.EventSequence != 0 && _latestRoomStateSequence > roomState.EventSequence)
                return true;
            if (roomState.RoomVersion == 0)
                return false;
            return CurrentRoom.RoomVersion > roomState.RoomVersion;
        }

        private bool IsStaleEvent(uint eventSequence, uint roomId)
        {
            if (eventSequence == 0)
                return false;
            if (!CurrentRoom.InRoom)
                return false;
            if (roomId == 0 || CurrentRoom.RoomId != roomId)
                return false;
            if (_latestRoomSequenceRoomId != roomId)
                return false;
            if (_latestRoomEventSequence == 0)
                return false;
            return PacketValidation.IsStaleSequence(_latestRoomEventSequence, eventSequence);
        }

        private void AdvanceEventSequence(uint roomId, uint eventSequence)
        {
            if (roomId == 0 || eventSequence == 0)
                return;

            if (_latestRoomSequenceRoomId != roomId)
            {
                _latestRoomSequenceRoomId = roomId;
                _latestRoomEventSequence = 0;
                _latestRoomStateSequence = 0;
            }

            if (_latestRoomEventSequence < eventSequence)
                _latestRoomEventSequence = eventSequence;
            if (CurrentRoom.EventSequence < _latestRoomEventSequence)
                CurrentRoom.EventSequence = _latestRoomEventSequence;
        }

        private void UpdateClientStateFromRoom()
        {
            if (!CurrentRoom.InRoom)
            {
                ClientState = MultiplayerClientState.Lobby;
                return;
            }

            ClientState = RoomRules.NormalizeRaceState(CurrentRoom.RaceState) switch
            {
                RoomRaceState.Preparing => MultiplayerClientState.Preparing,
                RoomRaceState.Racing => MultiplayerClientState.Racing,
                RoomRaceState.Completed => MultiplayerClientState.Completed,
                _ => MultiplayerClientState.InRoom
            };
        }
    }

    internal readonly struct RoomStateChange
    {
        public RoomStateChange(bool wasInRoom, uint previousRoomId, bool previousIsHost, GameRoomType previousRoomType)
            : this(wasInRoom, previousRoomId, previousIsHost, previousRoomType, applied: true)
        {
        }

        private RoomStateChange(bool wasInRoom, uint previousRoomId, bool previousIsHost, GameRoomType previousRoomType, bool applied)
        {
            WasInRoom = wasInRoom;
            PreviousRoomId = previousRoomId;
            PreviousIsHost = previousIsHost;
            PreviousRoomType = previousRoomType;
            Applied = applied;
        }

        public bool WasInRoom { get; }
        public uint PreviousRoomId { get; }
        public bool PreviousIsHost { get; }
        public GameRoomType PreviousRoomType { get; }
        public bool Applied { get; }

        public RoomStateChange WithApplied(bool applied)
        {
            return new RoomStateChange(WasInRoom, PreviousRoomId, PreviousIsHost, PreviousRoomType, applied);
        }
    }

    internal readonly struct RoomRaceChange
    {
        public RoomRaceChange(bool beginLoadout, bool leaveLoadout)
            : this(beginLoadout, leaveLoadout, applied: true)
        {
        }

        public RoomRaceChange(bool beginLoadout, bool leaveLoadout, bool applied)
        {
            BeginLoadout = beginLoadout;
            LeaveLoadout = leaveLoadout;
            Applied = applied;
        }

        public bool BeginLoadout { get; }
        public bool LeaveLoadout { get; }
        public bool Applied { get; }
    }
}
