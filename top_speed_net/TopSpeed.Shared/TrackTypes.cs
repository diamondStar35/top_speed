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

    public readonly struct TrackDefinition
    {
        public TrackType Type { get; }
        public TrackSurface Surface { get; }
        public TrackNoise Noise { get; }
        public int Length { get; }

        public TrackDefinition(TrackType type, TrackSurface surface, TrackNoise noise, int length)
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
        public TrackWeather Weather { get; }
        public TrackAmbience Ambience { get; }
        public TrackDefinition[] Definitions { get; }
        public int Length => Definitions.Length;
        public byte Laps { get; set; }

        public TrackData(bool userDefined, TrackWeather weather, TrackAmbience ambience, TrackDefinition[] definitions, byte laps = 0)
        {
            UserDefined = userDefined;
            Weather = weather;
            Ambience = ambience;
            Definitions = definitions;
            Laps = laps;
        }
    }
}
