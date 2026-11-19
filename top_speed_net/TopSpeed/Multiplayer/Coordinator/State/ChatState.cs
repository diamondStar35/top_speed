using TopSpeed.Core.Multiplayer.Chat;

namespace TopSpeed.Core.Multiplayer
{
    internal sealed class CoordinatorChatState
    {
        public readonly HistoryBuffers History = new HistoryBuffers(100);
    }
}

