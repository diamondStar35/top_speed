namespace TopSpeed.Data
{
    public readonly struct TrackDefinition
    {
        public TrackType Type { get; }
        public TrackSurface Surface { get; }
        public TrackNoise Noise { get; }
        public float Length { get; }

        public TrackDefinition(TrackType type, TrackSurface surface, TrackNoise noise, float length)
        {
            Type = type;
            Surface = surface;
            Noise = noise;
            Length = length;
        }
    }

    public sealed class TrackData
    {
        public bool UserDefined { get; }
        public string? Name { get; }
        public TrackWeather Weather { get; }
        public TrackAmbience Ambience { get; }
        public TrackDefinition[] Definitions { get; }
        public int Length => Definitions.Length;
        public byte Laps { get; set; }

        public TrackData(
            bool userDefined,
            TrackWeather weather,
            TrackAmbience ambience,
            TrackDefinition[] definitions,
            byte laps = 0,
            string? name = null)
        {
            UserDefined = userDefined;
            Weather = weather;
            Ambience = ambience;
            Definitions = definitions;
            Laps = laps;
            var trimmedName = name?.Trim();
            Name = string.IsNullOrWhiteSpace(trimmedName) ? null : trimmedName;
        }
    }
}
