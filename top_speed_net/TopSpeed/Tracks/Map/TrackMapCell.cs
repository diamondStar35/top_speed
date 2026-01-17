using TopSpeed.Data;

namespace TopSpeed.Tracks.Map
{
    internal sealed class TrackMapCell
    {
        public MapExits Exits { get; set; }
        public TrackSurface Surface { get; set; }
        public TrackNoise Noise { get; set; }
        public float WidthMeters { get; set; }
        public bool IsSafeZone { get; set; }
        public string? Zone { get; set; }
    }
}
