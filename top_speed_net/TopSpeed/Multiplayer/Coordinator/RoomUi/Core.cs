using System;

namespace TopSpeed.Core.Multiplayer
{
    internal sealed partial class MultiplayerCoordinator
    {
        private readonly RoomUi _roomUi;

        private sealed partial class RoomUi
        {
            private readonly MultiplayerCoordinator _owner;

            public RoomUi(MultiplayerCoordinator owner)
            {
                _owner = owner ?? throw new ArgumentNullException(nameof(owner));
            }
        }
    }
}
