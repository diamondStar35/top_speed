using TopSpeed.Protocol;
using TopSpeed.Server.Config;

namespace TopSpeed.Server.Network
{
    /// <summary>
    /// Single choke point for "which <see cref="RoomGameRules"/> bits this server permits, and
    /// which default on for a new room". Routing both the allow-mask and the new-room default
    /// through here means a future server-wide switch for fuel consumption or tire wear is a
    /// one-line change: add the bool to <see cref="ServerFeaturesSettings"/> and gate the bit below.
    /// </summary>
    internal static class GameRulesPolicy
    {
        // Bits permitted regardless of server features. Fuel consumption and tire wear are always
        // allowed today; when a server-wide switch is wanted, move them out of here and gate them on
        // the matching ServerFeaturesSettings flags (mirroring CustomTracks below).
        private const uint AlwaysAllowed =
            (uint)RoomGameRules.GhostMode
            | (uint)RoomGameRules.FuelConsumption
            | (uint)RoomGameRules.TireWear;

        // Rules that start enabled for a brand-new room (others default off / absent bit).
        private const uint DefaultOn =
            (uint)RoomGameRules.FuelConsumption
            | (uint)RoomGameRules.TireWear;

        public static uint ResolveAllowedGameRules(ServerFeaturesSettings? features)
        {
            var allowed = AlwaysAllowed;
            if (features != null && features.CustomTracks)
                allowed |= (uint)RoomGameRules.CustomTracks;
            return allowed;
        }

        public static uint ResolveDefaultGameRules(ServerFeaturesSettings? features)
        {
            return DefaultOn & ResolveAllowedGameRules(features);
        }
    }
}
