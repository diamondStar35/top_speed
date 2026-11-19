namespace TopSpeed.Core.Multiplayer
{
    internal interface IMultiplayerMenuTouch
    {
        bool HasActiveOverlayQuestion { get; }

        void NextChatCategory();
        void PreviousChatCategory();
        void NextChatItem();
        void PreviousChatItem();
        void CheckPing();
        void OpenGlobalChatHotkey();
        void OpenRoomChatHotkey();
        void ToggleCommunicator();
        void ToggleCommunicatorVoiceActivation();
        void BeginCommunicatorFrequencyInput();
        void AnnounceCommunicatorFrequency();
    }
}
