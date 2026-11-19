using TopSpeed.Protocol;

namespace TopSpeed.Core.Multiplayer
{
    internal sealed class ChatFlow : IChatFlow
    {
        private readonly MultiplayerCoordinator _owner;

        public ChatFlow(MultiplayerCoordinator owner)
        {
            _owner = owner;
        }

        public void NextCategory()
        {
            _owner.NextChatCategoryCore();
        }

        public void PreviousCategory()
        {
            _owner.PreviousChatCategoryCore();
        }

        public void NextItem()
        {
            _owner.NextChatItemCore();
        }

        public void PreviousItem()
        {
            _owner.PreviousChatItemCore();
        }

        public void FirstItem()
        {
            _owner.FirstChatItemCore();
        }

        public void LastItem()
        {
            _owner.LastChatItemCore();
        }

        public void CopyFocusedItem()
        {
            _owner.CopyFocusedChatItemCore();
        }

        public void OpenGlobalChatHotkey()
        {
            _owner.OpenGlobalChatHotkeyCore();
        }

        public void OpenRoomChatHotkey()
        {
            _owner.OpenRoomChatHotkeyCore();
        }

        public void HandleProtocolMessage(PacketProtocolMessage message)
        {
            _owner.HandleProtocolMessageCore(message);
        }
    }
}


