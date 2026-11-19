using System.Collections.Generic;

namespace TopSpeed.Core.Multiplayer
{
    internal sealed partial class MultiplayerCoordinator
    {
        private void DispatchPacketEffects(IReadOnlyList<PacketEffect> effects)
        {
            if (effects == null || effects.Count == 0)
                return;

            for (var i = 0; i < effects.Count; i++)
                ApplyPacketEffect(effects[i]);
        }
    }
}

