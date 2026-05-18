namespace TopSpeed.Physics.Tires.Wear
{
    internal static class TireWearStepper
    {
        public static TireWearState Step(
            TireWearConfig config,
            in TireWearState state,
            in TireWearStepInput input,
            float elapsedSeconds,
            float ambientTemperatureC,
            float surfaceTemperatureC,
            float wetnessNormalized,
            out float heatingRateCPerSecond,
            out float coolingRateCPerSecond)
        {
            var slipSmoothingAlpha = TireWearMath.ResolveExpAlpha(elapsedSeconds, config.SlipSmoothingTimeConstantSeconds);
            var smoothedSlipNormalized = state.SmoothedSlipNormalized
                + ((input.RawSlipNormalized - state.SmoothedSlipNormalized) * slipSmoothingAlpha);

            var heatBalance = TireWearHeatModel.StepTemperature(
                config,
                state,
                input,
                elapsedSeconds,
                ambientTemperatureC,
                surfaceTemperatureC,
                wetnessNormalized);

            heatingRateCPerSecond = heatBalance.HeatingRateCPerSecond;
            coolingRateCPerSecond = heatBalance.CoolingRateCPerSecond;

            var wearDelta = TireWearWearModel.ResolveWearDelta(
                config,
                input,
                smoothedSlipNormalized,
                heatBalance.TemperatureC,
                elapsedSeconds);
            var wearFraction = TireWearMath.Clamp01(state.WearFraction + wearDelta);

            return new TireWearState(wearFraction, heatBalance.TemperatureC, smoothedSlipNormalized);
        }
    }
}
