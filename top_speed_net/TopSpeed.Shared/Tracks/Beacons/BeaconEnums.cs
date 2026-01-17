namespace TopSpeed.Tracks.Beacons
{
    public enum TrackBeaconType
    {
        Undefined = 0,
        Voice,
        Beep,
        Silent
    }

    public enum TrackBeaconRole
    {
        Undefined = 0,
        Guidance,
        Alignment,
        Entry,
        Exit,
        Center,
        Warning
    }
}
