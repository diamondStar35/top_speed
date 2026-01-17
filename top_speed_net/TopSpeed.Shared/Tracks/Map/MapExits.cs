using System;

namespace TopSpeed.Tracks.Map
{
    [Flags]
    public enum MapExits
    {
        None = 0,
        North = 1,
        East = 2,
        South = 4,
        West = 8
    }
}
