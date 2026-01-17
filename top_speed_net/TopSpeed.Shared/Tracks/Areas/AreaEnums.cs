using System;

namespace TopSpeed.Tracks.Areas
{
    public enum TrackAreaType
    {
        Undefined = 0,
        SafeZone,
        Intersection,
        Curve,
        Straight,
        Branch,
        Merge,
        Split,
        Start,
        Finish,
        Checkpoint,
        PitLane,
        PitBox,
        Service,
        Hazard,
        Boundary,
        OffTrack,
        Zone
    }

    [Flags]
    public enum TrackAreaFlags
    {
        None = 0,
        SafeZone = 1 << 0,
        Hazard = 1 << 1,
        SlowZone = 1 << 2,
        Closed = 1 << 3,
        Restricted = 1 << 4,
        PitSpeed = 1 << 5
    }
}
