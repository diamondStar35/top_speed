using System;

namespace TopSpeed.Physics.Tires.Wear
{
    internal static class TireWearStateSanitizer
    {
        public static TireWearState Sanitize(TireWearConfig config, in TireWearState state, float ambientTemperatureC, float surfaceTemperatureC)
        {
            var wearFraction = TireWearMath.Clamp01(state.WearFraction);
            var smoothedSlipNormalized = TireWearMath.Clamp01(state.SmoothedSlipNormalized);
            var fallbackTemperatureC = TireWearEnvironment.ResolveInitialTemperature(config, ambientTemperatureC, surfaceTemperatureC);

            var temperatureC = TireWearMath.IsFinite(state.TemperatureC)
                ? state.TemperatureC
                : fallbackTemperatureC;
            var carcassTemperatureC = TireWearMath.IsFinite(state.CarcassTemperatureC)
                ? state.CarcassTemperatureC
                : fallbackTemperatureC;

            var minTemperatureC = ambientTemperatureC - 35f;
            var maxTemperatureC = Math.Max(surfaceTemperatureC + 90f, config.OverheatEndTemperatureC + 30f);
            temperatureC = TireWearMath.Clamp(temperatureC, minTemperatureC, maxTemperatureC);
            carcassTemperatureC = TireWearMath.Clamp(carcassTemperatureC, minTemperatureC, maxTemperatureC);
            return new TireWearState(wearFraction, temperatureC, carcassTemperatureC, smoothedSlipNormalized);
        }
    }
}
