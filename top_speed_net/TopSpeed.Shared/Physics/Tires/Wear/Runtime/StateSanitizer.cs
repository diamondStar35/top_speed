using System;

namespace TopSpeed.Physics.Tires.Wear
{
    internal static class TireWearStateSanitizer
    {
        public static TireWearState Sanitize(TireWearConfig config, in TireWearState state, float ambientTemperatureC, float surfaceTemperatureC)
        {
            var wearFraction = TireWearMath.Clamp01(state.WearFraction);
            var smoothedSlipNormalized = TireWearMath.Clamp01(state.SmoothedSlipNormalized);
            var temperatureC = state.TemperatureC;
            if (!TireWearMath.IsFinite(temperatureC))
                temperatureC = TireWearEnvironment.ResolveInitialTemperature(config, ambientTemperatureC, surfaceTemperatureC);

            var minTemperatureC = ambientTemperatureC - 35f;
            var maxTemperatureC = Math.Max(surfaceTemperatureC + 90f, config.OverheatEndTemperatureC + 30f);
            temperatureC = TireWearMath.Clamp(temperatureC, minTemperatureC, maxTemperatureC);
            return new TireWearState(wearFraction, temperatureC, smoothedSlipNormalized);
        }
    }
}
