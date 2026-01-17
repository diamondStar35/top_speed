namespace TopSpeed.Tracks.Sectors
{
    public enum TrackSectorType
    {
        Undefined = 0,
        Straight,
        Curve,
        Intersection,
        Merge,
        Split,
        Chicane,
        Start,
        Finish,
        Checkpoint,
        PitLane,
        PitBox,
        Service,
        Hazard,
        Zone,
        OffTrack,
        SafeZone,
        Wall,
        Building
    }

    [System.Flags]
    public enum TrackSectorFlags
    {
        None = 0,
        Fuel = 1 << 0,
        Parking = 1 << 1,
        Boarding = 1 << 2,
        Service = 1 << 3,
        Pit = 1 << 4,
        SafeZone = 1 << 5,
        Hazard = 1 << 6,
        Closed = 1 << 7,
        Restricted = 1 << 8
    }
}
