using TopSpeed.Menu;
using TopSpeed.Protocol;
using TopSpeed.Input;

namespace TopSpeed.Core.Multiplayer
{
    internal interface IMultiplayerRuntime
    {
        QuestionDialog Questions { get; }
        bool IsInRoom { get; }
        bool IsCurrentRoomHost { get; }
        bool IsCurrentRacePaused { get; }
        bool CommunicatorEnabled { get; }
        bool CommunicatorVoiceActivationEnabled { get; }
        ushort CommunicatorFrequencyTenths { get; }
        MultiplayerClientState ClientState { get; }

        void ConfigureMenuCloseHandlers();
        void ShowMultiplayerMenuAfterRace();
        void BeginRaceLoadoutSelection();

        void BeginManualServerEntry();
        void BeginServerPortEntry();
        void BeginDefaultCallSignEntry();
        void StartServerDiscovery();
        void OpenSavedServersManager();
        bool UpdatePendingOperations();
        void OnSessionCleared();
        void SetClientState(MultiplayerClientState state);

        void NextChatCategory();
        void PreviousChatCategory();
        void NextChatItem();
        void PreviousChatItem();
        bool TryHandleRaceLoopHistoryShortcuts(IInputService input);
        void CheckPing();
        void OpenGlobalChatHotkey();
        void OpenRoomChatHotkey();

        void HandlePingReply(long receivedUtcTicks = 0);
        void HandleRoomList(PacketRoomList roomList);
        void HandleRoomState(PacketRoomState roomState);
        void HandleRoomEvent(PacketRoomEvent roomEvent);
        void HandleRoomRaceStateChanged(PacketRoomRaceStateChanged roomRaceStateChanged);
        void HandleTrackPackageUploadResult(PacketTrackPackageUploadResult result);
        void HandleTrackPackageCatalog(PacketTrackPackageCatalog catalog);
        void HandleOnlinePlayers(PacketOnlinePlayers onlinePlayers);
        void HandleProtocolMessage(PacketProtocolMessage message);
        void PlayConnectedSound();
        void StartConnectingSoundPulse();
        void StopConnectingSoundPulse();

        string ResolvePlayerName(byte playerNumber);

        /// <summary>
        /// The effective <c>RoomGameRules</c> flags for the current room's race. Today this is the
        /// room's authoritative rules; the per-race transient disable mask (set by the host's no-pit
        /// warning) is folded in here once delivered by the server, so it never mutates the
        /// persistent room rules shown in the rules menu.
        /// </summary>
        uint GetCurrentRaceEffectiveGameRules();
    }
}

