using TopSpeed.Protocol;

namespace TopSpeed.Core.Multiplayer
{
    internal sealed class RoomsFlow : IRoomsFlow
    {
        private readonly MultiplayerCoordinator _owner;

        public RoomsFlow(MultiplayerCoordinator owner)
        {
            _owner = owner;
        }

        public bool IsInRoom => _owner.IsInRoomCore;
        public bool IsCurrentRoomHost => _owner.IsCurrentRoomHost;
        public bool IsCurrentRacePaused => _owner.IsCurrentRacePaused;

        public void ConfigureMenuCloseHandlers()
        {
            _owner.ConfigureMenuCloseHandlersCore();
        }

        public void ShowMultiplayerMenuAfterRace()
        {
            _owner.ShowMultiplayerMenuAfterRaceCore();
        }

        public void BeginRaceLoadoutSelection()
        {
            _owner.BeginRaceLoadoutSelectionCore();
        }

        public void OnSessionCleared()
        {
            _owner.OnSessionClearedCore();
        }

        public void HandleRoomList(PacketRoomList roomList)
        {
            _owner.HandleRoomListCore(roomList);
        }

        public void HandleRoomState(PacketRoomState roomState)
        {
            _owner.HandleRoomStateCore(roomState);
        }

        public void HandleRoomEvent(PacketRoomEvent roomEvent)
        {
            _owner.HandleRoomEventCore(roomEvent);
        }

        public void HandleRoomRaceStateChanged(PacketRoomRaceStateChanged roomRaceStateChanged)
        {
            _owner.HandleRoomRaceStateChangedCore(roomRaceStateChanged);
        }

        public void HandleTrackPackageUploadResult(PacketTrackPackageUploadResult result)
        {
            _owner.HandleTrackPackageUploadResultCore(result);
        }

        public void HandleTrackPackageCatalog(PacketTrackPackageCatalog catalog)
        {
            _owner.HandleTrackPackageCatalogCore(catalog);
        }

        public void HandleOnlinePlayers(PacketOnlinePlayers onlinePlayers)
        {
            _owner.HandleOnlinePlayersCore(onlinePlayers);
        }
    }
}


