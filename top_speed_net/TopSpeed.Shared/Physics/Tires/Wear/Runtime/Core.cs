using System;

namespace TopSpeed.Physics.Tires.Wear
{
    internal static class TireWearRuntimeCore
    {
        public static TireWearRuntimeResult Resolve(TireWearConfig config, in TireWearState state)
        {
            var ambientTemperatureC = TireWearEnvironment.ResolveAmbientTemperature(config, config.AmbientTemperatureC);
            var sanitizedState = TireWearStateSanitizer.Sanitize(config, state, ambientTemperatureC, ambientTemperatureC);
            return TireWearResultBuilder.Build(config, sanitizedState, sanitizedState.SmoothedSlipNormalized, heatingRateCPerSecond: 0f, coolingRateCPerSecond: 0f);
        }

        public static TireWearRuntimeResult Step(TireWearConfig config, in TireWearState state, in TireWearInput input)
        {
            var ambientTemperatureC = TireWearEnvironment.ResolveAmbientTemperature(config, input.AmbientTemperatureC);
            var surfaceTemperatureC = TireWearEnvironment.ResolveSurfaceTemperature(ambientTemperatureC, input.SurfaceTemperatureC);
            var wetnessNormalized = TireWearMath.Clamp01(input.WetnessNormalized);

            var sanitizedState = TireWearStateSanitizer.Sanitize(config, state, ambientTemperatureC, surfaceTemperatureC);
            var stepInput = TireWearStepInput.Create(input);
            if (stepInput.ElapsedSeconds <= 0f)
                return TireWearResultBuilder.Build(config, sanitizedState, sanitizedState.SmoothedSlipNormalized, heatingRateCPerSecond: 0f, coolingRateCPerSecond: 0f);

            var integrated = TireWearIntegrator.Integrate(
                config,
                sanitizedState,
                stepInput,
                ambientTemperatureC,
                surfaceTemperatureC,
                wetnessNormalized);

            return TireWearResultBuilder.Build(
                config,
                integrated.State,
                stepInput.RawSlipNormalized,
                integrated.HeatingRateCPerSecond,
                integrated.CoolingRateCPerSecond);
        }
    }
}
