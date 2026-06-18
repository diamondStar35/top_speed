using System;

namespace TopSpeed.Physics.Tires.Wear
{
    internal static class TireWearStateSanitizer
    {
        public static TireWearState Sanitize(TireWearConfig config, in TireWearState state, float ambientTemperatureC, float surfaceTemperatureC)
        {
            var wearFraction = TireWearMath.Clamp01(state.WearFraction);
            var smoothed = SanitizeSmoothed(state.Smoothed);
            var fallbackTemperatureC = TireWearEnvironment.ResolveInitialTemperature(config, ambientTemperatureC, surfaceTemperatureC);

            var temperatureC = TireWearMath.IsFinite(state.TemperatureC)
                ? state.TemperatureC
                : fallbackTemperatureC;
            var treadTemperatureC = TireWearMath.IsFinite(state.TreadTemperatureC)
                ? state.TreadTemperatureC
                : fallbackTemperatureC;
            var carcassTemperatureC = TireWearMath.IsFinite(state.CarcassTemperatureC)
                ? state.CarcassTemperatureC
                : fallbackTemperatureC;

            var minTemperatureC = ambientTemperatureC - 35f;
            var maxTemperatureC = Math.Max(surfaceTemperatureC + 90f, config.OverheatEndTemperatureC + 30f);
            temperatureC = TireWearMath.Clamp(temperatureC, minTemperatureC, maxTemperatureC);
            treadTemperatureC = TireWearMath.Clamp(treadTemperatureC, minTemperatureC, maxTemperatureC);
            carcassTemperatureC = TireWearMath.Clamp(carcassTemperatureC, minTemperatureC, maxTemperatureC);
            return new TireWearState(wearFraction, temperatureC, treadTemperatureC, carcassTemperatureC, smoothed);
        }

        private static TireWearSmoothedInputs SanitizeSmoothed(in TireWearSmoothedInputs smoothed)
        {
            return new TireWearSmoothedInputs(
                TireWearMath.Clamp01(smoothed.CorneringUtilization),
                TireWearMath.Clamp01(smoothed.CorneringSlip),
                TireWearMath.Clamp01(smoothed.AccelerationStress),
                TireWearMath.Clamp01(smoothed.BrakeStress),
                TireWearMath.Clamp01(smoothed.EngineBrakeStress),
                TireWearMath.Clamp01(smoothed.LongitudinalSlip),
                TireWearMath.Clamp01(smoothed.Load));
        }
    }
}
