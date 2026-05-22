namespace TopSpeed.Physics.Tires.Wear
{
    internal static class TireWearEnvironment
    {
        public static float ResolveInitialTemperature(TireWearConfig config, float ambientTemperatureC, float surfaceTemperatureC)
        {
            var ambient = ResolveAmbientTemperature(config, ambientTemperatureC);
            var surface = ResolveSurfaceTemperature(ambient, surfaceTemperatureC);
            var bias = surface >= ambient ? 0.62f : 0.45f;
            var initial = ambient + ((surface - ambient) * bias);
            return TireWearMath.Clamp(initial, ambient - 12f, ambient + 28f);
        }

        public static float ResolveAmbientTemperature(TireWearConfig config, float temperatureC)
        {
            if (!TireWearMath.IsFinite(temperatureC))
                return TireWearMath.Clamp(config.FallbackAmbientTemperatureC, -40f, 80f);
            return TireWearMath.Clamp(temperatureC, -40f, 80f);
        }

        public static float ResolveSurfaceTemperature(float ambientTemperatureC, float temperatureC)
        {
            if (!TireWearMath.IsFinite(temperatureC))
                return ambientTemperatureC;
            return TireWearMath.Clamp(temperatureC, ambientTemperatureC - 45f, ambientTemperatureC + 70f);
        }
    }
}
