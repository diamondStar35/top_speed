using TopSpeed.Data;

namespace TopSpeed.Protocol
{
    public sealed class TrackPackageManifest
    {
        public string TrackId = string.Empty;
        public string Version = string.Empty;
        public string Hash = string.Empty;
        public string DefaultWeatherProfileId = TrackWeatherProfile.DefaultProfileId;
        public TrackAmbience Ambience;
        public int Laps;
    }
}
