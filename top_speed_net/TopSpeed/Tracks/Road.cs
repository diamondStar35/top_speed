using TopSpeed.Data;

namespace TopSpeed.Tracks
{
    internal struct TrackRoad
    {
        public float Left;
        public float Right;
        public TrackSurface Surface;
        public TrackType Type;
        public float Length;
        public bool IsSafeZone;
        public bool IsOutOfBounds;
        public bool IsClosed;
        public bool IsRestricted;
        public bool RequiresStop;
        public bool RequiresYield;
        public float? MinSpeedKph;
        public float? MaxSpeedKph;
    }
}
