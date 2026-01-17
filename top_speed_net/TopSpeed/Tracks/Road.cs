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
    }
}
