namespace TopSpeed.Tracks.Topology
{
    public enum ShapeType
    {
        Undefined = 0,
        Rectangle,
        Circle,
        Polygon,
        Polyline
    }

    public enum PortalRole
    {
        Undefined = 0,
        Entry,
        Exit,
        EntryExit
    }

    public enum LinkDirection
    {
        TwoWay = 0,
        OneWay
    }

    public enum PathType
    {
        Undefined = 0,
        Road,
        Curve,
        Intersection,
        Connector,
        Lane,
        Branch,
        Merge,
        Split,
        PitLane
    }
}
