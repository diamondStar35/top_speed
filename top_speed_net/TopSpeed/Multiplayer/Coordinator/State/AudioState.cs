using System.Threading;
using TS.Audio;

namespace TopSpeed.Core.Multiplayer
{
    internal sealed class CoordinatorAudioState
    {
        public CancellationTokenSource? ConnectingPulseCts;
        public SoundAsset? ConnectingSound;
        public SoundAsset? ConnectedSound;
        public SoundAsset? OnlineSound;
        public SoundAsset? OfflineSound;
        public SoundAsset? PingStartSound;
        public SoundAsset? PingSound;
        public SoundAsset? RoomCreatedSound;
        public SoundAsset? RoomJoinSound;
        public SoundAsset? RoomLeaveSound;
        public SoundAsset? ChatSound;
        public SoundAsset? RoomChatSound;
        public SoundAsset? BufferSwitchSound;
        public SoundAsset? CommunicatorFrequencyAdjustSound;
    }
}

