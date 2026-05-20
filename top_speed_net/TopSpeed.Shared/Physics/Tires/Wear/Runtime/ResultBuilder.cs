using System;

namespace TopSpeed.Physics.Tires.Wear
{
    internal static class TireWearResultBuilder
    {
        private const float MinimumGripScale = 0.45f;

        public static TireWearRuntimeResult Build(
            TireWearConfig config,
            in TireWearState state,
            float slipNormalized,
            float heatingRateCPerSecond,
            float coolingRateCPerSecond)
        {
            var temperatureGrip = TireWearTemperature.ResolveGrip(config, state.TemperatureC);
            var wearGrip = TireWearMath.Lerp(1f, config.GripAtFullWear, state.WearFraction);
            var combinedGrip = TireWearMath.Clamp(temperatureGrip * wearGrip, MinimumGripScale, 1f);
            var tractionGrip = combinedGrip;
            var lateralGrip = TireWearMath.Clamp(combinedGrip * TireWearMath.Lerp(1.03f, 0.97f, state.WearFraction), MinimumGripScale, 1f);
            var brakeGrip = TireWearMath.Clamp(combinedGrip * TireWearMath.Lerp(1.00f, 0.94f, state.WearFraction), MinimumGripScale, 1f);

            return new TireWearRuntimeResult(
                state,
                tractionGrip,
                lateralGrip,
                brakeGrip,
                TireWearMath.Clamp((tractionGrip * 0.36f) + (lateralGrip * 0.44f) + (brakeGrip * 0.20f), MinimumGripScale, 1f),
                TireWearTemperature.ResolveNormalized(config, state.TemperatureC),
                TireWearMath.Clamp01(slipNormalized),
                TireWearTemperature.ResolveOverheat(config, state.TemperatureC),
                Math.Max(0f, heatingRateCPerSecond),
                Math.Max(0f, coolingRateCPerSecond));
        }
    }
}
