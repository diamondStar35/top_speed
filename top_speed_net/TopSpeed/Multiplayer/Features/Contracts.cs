using TopSpeed.Protocol;

namespace TopSpeed.Core.Multiplayer
{
    internal interface IConnectionFlow
    {
        void BeginManualServerEntry();
        void BeginServerPortEntry();
        void BeginDefaultCallSignEntry();
        void StartServerDiscovery();
        bool UpdatePendingOperations();
        void HandlePingReply(long receivedUtcTicks);
    }

    internal interface IRoomsFlow
    {
        bool IsInRoom { get; }
        bool IsCurrentRoomHost { get; }
        bool IsCurrentRacePaused { get; }

        void ConfigureMenuCloseHandlers();
        void ShowMultiplayerMenuAfterRace();
        void BeginRaceLoadoutSelection();
        void OnSessionCleared();
        void HandleRoomList(PacketRoomList roomList);
        void HandleRoomState(PacketRoomState roomState);
        void HandleRoomEvent(PacketRoomEvent roomEvent);
        void HandleRoomRaceStateChanged(PacketRoomRaceStateChanged roomRaceStateChanged);
        void HandleTrackPackageUploadResult(PacketTrackPackageUploadResult result);
        void HandleTrackPackageCatalog(PacketTrackPackageCatalog catalog);
        void HandleOnlinePlayers(PacketOnlinePlayers onlinePlayers);
    }

    internal interface ISavedServersFlow
    {
        void OpenSavedServersManager();
    }

    internal interface IChatFlow
    {
        void NextCategory();
        void PreviousCategory();
        void NextItem();
        void PreviousItem();
        void FirstItem();
        void LastItem();
        void CopyFocusedItem();
        void OpenGlobalChatHotkey();
        void OpenRoomChatHotkey();
        void HandleProtocolMessage(PacketProtocolMessage message);
    }
}
