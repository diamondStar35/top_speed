namespace TopSpeed.Data
{
    public enum TrackType
    {
        Straight = 0,
        EasyLeft = 1,
        Left = 2,
        HardLeft = 3,
        HairpinLeft = 4,
        EasyRight = 5,
        Right = 6,
        HardRight = 7,
        HairpinRight = 8
    }

    public enum TrackSurface
    {
        Asphalt = 0,
        Gravel = 1,
        Water = 2,
        Sand = 3,
        Snow = 4
    }

    public enum TrackNoise
    {
        NoNoise = 0,
        Crowd = 1,
        Ocean = 2,
        Runway = 3,
        Clock = 4,
        Jet = 5,
        Thunder = 6,
        Pile = 7,
        Construction = 8,
        River = 9,
        Helicopter = 10,
        Owl = 11
    }

    public enum TrackWeather
    {
        Sunny = 0,
        Rain = 1,
        Wind = 2,
        Storm = 3
    }

    public enum TrackAmbience
    {
        NoAmbience = 0,
        Desert = 1,
        Airport = 2
    }
}

namespace TopSpeed.Tracks.Geometry
{
    public enum TrackLayoutIssueSeverity
    {
        Warning = 0,
        Error = 1
    }

    public enum TrackCurveDirection
    {
        Straight = 0,
        Left = 1,
        Right = 2
    }

    public enum TrackCurveSeverity
    {
        Easy = 1,
        Normal = 2,
        Hard = 3,
        Hairpin = 4
    }

    public enum TrackGeometrySpanKind
    {
        Straight = 0,
        Arc = 1,
        Clothoid = 2
    }

    public enum TrackTurnDirection
    {
        Unknown = 0,
        Left = 1,
        Right = 2,
        Straight = 3,
        UTurn = 4
    }

    public enum TrackBoundarySide
    {
        Left = 0,
        Right = 1,
        Both = 2
    }

    public enum TrackBoundaryType
    {
        Unknown = 0,
        Wall = 1,
        Guardrail = 2,
        Curb = 3,
        Grass = 4,
        Gravel = 5,
        Barrier = 6,
        Fence = 7,
        Cliff = 8,
        Water = 9,
        TreeLine = 10
    }

    public enum TrackIntersectionShape
    {
        Unspecified = 0,
        Circle = 1,
        Box = 2,
        Cross = 3,
        Roundabout = 4,
        Custom = 5
    }

    public enum TrackIntersectionControl
    {
        None = 0,
        Stop = 1,
        Yield = 2,
        Signal = 3
    }

    public enum TrackIntersectionLegType
    {
        Entry = 0,
        Exit = 1,
        Both = 2
    }

    public enum TrackLaneOwnerKind
    {
        Leg = 0,
        Connector = 1,
        StartFinish = 2,
        Custom = 3
    }

    public enum TrackLaneDirection
    {
        Forward = 0,
        Reverse = 1,
        Both = 2
    }

    public enum TrackLaneType
    {
        Travel = 0,
        TurnLeft = 1,
        TurnRight = 2,
        Merge = 3,
        Exit = 4,
        Entry = 5,
        Shoulder = 6,
        Bike = 7,
        Bus = 8,
        Pit = 9,
        Parking = 10,
        Emergency = 11,
        Custom = 12
    }

    public enum TrackLaneMarking
    {
        None = 0,
        Solid = 1,
        Dashed = 2,
        DoubleSolid = 3,
        DoubleDashed = 4,
        SolidDashed = 5,
        DashedSolid = 6
    }

    public enum TrackLaneGroupKind
    {
        Leg = 0,
        Connector = 1,
        Custom = 2
    }

    public enum TrackIntersectionAreaShape
    {
        Circle = 0,
        Box = 1,
        Polygon = 2
    }

    public enum TrackIntersectionAreaKind
    {
        Core = 0,
        Conflict = 1,
        Island = 2,
        Crosswalk = 3,
        StopLine = 4,
        StartLine = 5,
        FinishLine = 6,
        GridBox = 7,
        TimingGate = 8,
        Median = 9,
        Sidewalk = 10,
        Shoulder = 11,
        Custom = 12
    }

    public enum TrackIntersectionAreaOwnerKind
    {
        None = 0,
        Leg = 1,
        Connector = 2,
        LaneGroup = 3,
        Custom = 4
    }

    public enum TrackStartFinishKind
    {
        Start = 0,
        Finish = 1,
        StartFinish = 2,
        Split = 3,
        Custom = 4
    }

    public enum TrackRacingLineKind
    {
        Dry = 0,
        Wet = 1,
        Defensive = 2,
        Custom = 3
    }
}
