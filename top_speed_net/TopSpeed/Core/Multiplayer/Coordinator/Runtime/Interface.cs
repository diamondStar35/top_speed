namespace TopSpeed.Core.Multiplayer
{
    internal sealed partial class MultiplayerCoordinator
    {
        internal bool IsCommunicatorEnabled => _state.Communicator.Enabled;
        internal bool IsCommunicatorVoiceActivationEnabled => _state.Communicator.VoiceActivationEnabled;
        internal ushort CommunicatorFrequencyTenths => _state.Communicator.FrequencyTenths;

        bool IMultiplayerRuntime.CommunicatorEnabled => IsCommunicatorEnabled;
        bool IMultiplayerRuntime.CommunicatorVoiceActivationEnabled => IsCommunicatorVoiceActivationEnabled;
        ushort IMultiplayerRuntime.CommunicatorFrequencyTenths => CommunicatorFrequencyTenths;

        void IMultiplayerRuntime.NextChatCategory()
        {
            NextChatCategory();
        }

        void IMultiplayerRuntime.PreviousChatCategory()
        {
            PreviousChatCategory();
        }

        void IMultiplayerRuntime.NextChatItem()
        {
            NextChatItem();
        }

        void IMultiplayerRuntime.PreviousChatItem()
        {
            PreviousChatItem();
        }

        bool IMultiplayerRuntime.TryHandleRaceLoopHistoryShortcuts(Input.IInputService input)
        {
            return TryHandleRaceLoopHistoryShortcuts(input);
        }

        void IMultiplayerRuntime.CheckPing()
        {
            CheckCurrentPing();
        }

        void IMultiplayerRuntime.OpenGlobalChatHotkey()
        {
            OpenGlobalChatHotkey();
        }

        void IMultiplayerRuntime.OpenRoomChatHotkey()
        {
            OpenRoomChatHotkey();
        }

        void IMultiplayerRuntime.StartConnectingSoundPulse()
        {
            StartConnectingPulse();
        }

        void IMultiplayerRuntime.StopConnectingSoundPulse()
        {
            StopConnectingPulse();
        }

        bool IMultiplayerMenuTouch.HasActiveOverlayQuestion => _questions.HasActiveOverlayQuestion;

        void IMultiplayerMenuTouch.NextChatCategory()
        {
            NextChatCategory();
        }

        void IMultiplayerMenuTouch.PreviousChatCategory()
        {
            PreviousChatCategory();
        }

        void IMultiplayerMenuTouch.NextChatItem()
        {
            NextChatItem();
        }

        void IMultiplayerMenuTouch.PreviousChatItem()
        {
            PreviousChatItem();
        }

        void IMultiplayerMenuTouch.CheckPing()
        {
            CheckCurrentPing();
        }

        void IMultiplayerMenuTouch.OpenGlobalChatHotkey()
        {
            OpenGlobalChatHotkey();
        }

        void IMultiplayerMenuTouch.OpenRoomChatHotkey()
        {
            OpenRoomChatHotkey();
        }

        void IMultiplayerMenuTouch.ToggleCommunicator()
        {
            ToggleCommunicator();
        }

        void IMultiplayerMenuTouch.ToggleCommunicatorVoiceActivation()
        {
            ToggleCommunicatorVoiceActivation();
        }

        void IMultiplayerMenuTouch.BeginCommunicatorFrequencyInput()
        {
            BeginCommunicatorFrequencyInput();
        }

        void IMultiplayerMenuTouch.AnnounceCommunicatorFrequency()
        {
            AnnounceCommunicatorFrequency();
        }

        string IMultiplayerRuntime.ResolvePlayerName(byte playerNumber)
        {
            return ResolvePlayerName(playerNumber);
        }

        uint IMultiplayerRuntime.GetCurrentRaceEffectiveGameRules()
        {
            return GetCurrentRaceEffectiveGameRules();
        }
    }
}

