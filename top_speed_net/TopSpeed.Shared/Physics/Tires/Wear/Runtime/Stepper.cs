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
            // Single shared low-pass over every driver-stress input. One alpha,
            // one time constant — heat and wear both read these smoothed values,
            // and the squaring downstream happens after this (smooth-then-square).
            var alpha = TireWearMath.ResolveExpAlpha(elapsedSeconds, config.SlipSmoothingTimeConstantSeconds);
            var previous = state.Smoothed;
            var smoothed = new TireWearSmoothedInputs(
                TireWearMath.Lerp(previous.CorneringUtilization, input.CorneringUtilizationNormalized, alpha),
                TireWearMath.Lerp(previous.CorneringSlip, input.CorneringSlipNormalized, alpha),
                TireWearMath.Lerp(previous.AccelerationStress, input.AccelerationHeatStressNormalized, alpha),
                TireWearMath.Lerp(previous.BrakeStress, input.BrakeHeatStressNormalized, alpha),
                TireWearMath.Lerp(previous.EngineBrakeStress, input.EngineBrakeHeatStressNormalized, alpha),
                TireWearMath.Lerp(previous.LongitudinalSlip, input.LongitudinalSlipNormalized, alpha),
                TireWearMath.Lerp(previous.Load, input.LoadNormalized, alpha));

            var heatBalance = TireWearHeatModel.StepTemperature(
                config,
                state,
                input,
                smoothed,
                elapsedSeconds,
                ambientTemperatureC,
                surfaceTemperatureC,
                wetnessNormalized);

            heatingRateCPerSecond = heatBalance.HeatingRateCPerSecond;
            coolingRateCPerSecond = heatBalance.CoolingRateCPerSecond;

            var wearDelta = TireWearWearModel.ResolveWearDelta(
                config,
                input,
                smoothed,
                state.WearFraction,
                heatBalance.TemperatureC,
                elapsedSeconds);
            var wearFraction = TireWearMath.Clamp01(state.WearFraction + wearDelta);

            return new TireWearState(
                wearFraction,
                heatBalance.TemperatureC,
                heatBalance.TreadTemperatureC,
                heatBalance.CarcassTemperatureC,
                smoothed);
        }
    }
}
