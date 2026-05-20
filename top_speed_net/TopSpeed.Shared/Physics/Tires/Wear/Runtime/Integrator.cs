using System;

namespace TopSpeed.Physics.Tires.Wear
{
    internal static class TireWearIntegrator
    {
        private const float IntegrationStepSeconds = 0.25f;
        private const int MaxIntegrationSteps = 240;

        public static TireWearIntegrationResult Integrate(
            TireWearConfig config,
            in TireWearState state,
            in TireWearStepInput input,
            float ambientTemperatureC,
            float surfaceTemperatureC,
            float wetnessNormalized)
        {
            var stepCount = ResolveIntegrationStepCount(input.ElapsedSeconds);
            var stepSeconds = input.ElapsedSeconds / stepCount;
            var currentState = state;
            var heatingRateAccumulator = 0f;
            var coolingRateAccumulator = 0f;

            for (var stepIndex = 0; stepIndex < stepCount; stepIndex++)
            {
                currentState = TireWearStepper.Step(
                    config,
                    currentState,
                    input,
                    stepSeconds,
                    ambientTemperatureC,
                    surfaceTemperatureC,
                    wetnessNormalized,
                    out var heatingRateCPerSecond,
                    out var coolingRateCPerSecond);

                heatingRateAccumulator += heatingRateCPerSecond;
                coolingRateAccumulator += coolingRateCPerSecond;
            }

            return new TireWearIntegrationResult(
                currentState,
                heatingRateAccumulator / stepCount,
                coolingRateAccumulator / stepCount);
        }

        private static int ResolveIntegrationStepCount(float elapsedSeconds)
        {
            if (elapsedSeconds <= IntegrationStepSeconds)
                return 1;

            var requestedSteps = (int)Math.Ceiling(elapsedSeconds / IntegrationStepSeconds);
            return Math.Max(1, Math.Min(MaxIntegrationSteps, requestedSteps));
        }
    }

    internal readonly struct TireWearIntegrationResult
    {
        public TireWearIntegrationResult(TireWearState state, float heatingRateCPerSecond, float coolingRateCPerSecond)
        {
            State = state;
            HeatingRateCPerSecond = heatingRateCPerSecond;
            CoolingRateCPerSecond = coolingRateCPerSecond;
        }

        public TireWearState State { get; }
        public float HeatingRateCPerSecond { get; }
        public float CoolingRateCPerSecond { get; }
    }
}
