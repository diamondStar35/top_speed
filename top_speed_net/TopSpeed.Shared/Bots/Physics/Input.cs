using TopSpeed.Data;

namespace TopSpeed.Bots
{
    public readonly struct BotPhysicsInput
    {
        public BotPhysicsInput(
            float elapsedSeconds,
            TrackSurface surface,
            int throttle,
            int brake,
            int steering,
            float ambientTemperatureC,
            float rainGain,
            float stormGain,
            float windGain)
        {
            ElapsedSeconds = elapsedSeconds;
            Surface = surface;
            Throttle = throttle;
            Brake = brake;
            Steering = steering;
            AmbientTemperatureC = ambientTemperatureC;
            RainGain = rainGain;
            StormGain = stormGain;
            WindGain = windGain;
        }

        public float ElapsedSeconds { get; }
        public TrackSurface Surface { get; }
        public int Throttle { get; }
        public int Brake { get; }
        public int Steering { get; }
        public float AmbientTemperatureC { get; }
        public float RainGain { get; }
        public float StormGain { get; }
        public float WindGain { get; }
    }
}
