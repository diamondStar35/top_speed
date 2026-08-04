using System;

namespace TopSpeed.Protocol
{
    [Flags]
    public enum RoomGameRules : uint
    {
        None = 0,
        GhostMode = 1u << 0,
        CustomTracks = 1u << 1,
        FuelConsumption = 1u << 2,
        TireWear = 1u << 3,
        CustomVehicles = 1u << 4
    }
}
