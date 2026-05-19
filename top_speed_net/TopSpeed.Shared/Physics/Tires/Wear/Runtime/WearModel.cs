namespace TopSpeed.Physics.Tires.Wear
{
    internal static class TireWearWearModel
    {
        public static float ResolveWearDelta(
            TireWearConfig config,
            in TireWearStepInput input,
            float smoothedSlipNormalized,
            float wearFraction,
            float temperatureC,
            float elapsedSeconds)
        {
            var distanceKilometers = (input.SpeedMps * elapsedSeconds) / 1000f;
            var speedNormalized = TireWearMath.Clamp01(input.SpeedMps / 62f);
            var baseWear = distanceKilometers * config.BaseWearPerKilometer;
            var workloadWear = distanceKilometers
                * config.BaseWearPerKilometer
                * (0.26f + (0.62f * input.LoadNormalized))
                * (0.36f + (0.64f * speedNormalized))
                * ((input.CorneringUtilizationNormalized * 0.38f) + (input.LongitudinalSlipNormalized * 0.27f) + 0.35f);
            var corneringWearSignal = TireWearMath.Pow(
                (input.CorneringUtilizationNormalized * 0.28f) + (input.CorneringSlipNormalized * 0.72f),
                1.15f);
            var longitudinalWearSignal = TireWearMath.Pow(
                (input.LongitudinalSlipNormalized * 0.35f) + (input.LongitudinalSlideNormalized * 0.65f),
                1.10f);
            var slipWearSignal = TireWearMath.Clamp(
                (corneringWearSignal * config.CorneringSlipWearWeight)
                + (longitudinalWearSignal * config.LongitudinalSlipWearWeight),
                0f,
                2.5f);
            var slipWear = smoothedSlipNormalized * slipWearSignal * config.SlipWearRatePerSecond * elapsedSeconds;
            var enduranceWear = elapsedSeconds
                * config.SlipWearRatePerSecond
                * (0.06f + (0.20f * input.CorneringUtilizationNormalized))
                * (0.35f + (0.65f * speedNormalized));
            var loadWearMultiplier = 1f + (input.LoadNormalized * config.LoadWearGain);
            var temperatureWearMultiplier = TireWearTemperature.ResolveWearMultiplier(config, temperatureC);
            var wearFractionNormalized = TireWearMath.Clamp01(wearFraction);
            var endOfLifeRunaway = TireWearMath.Clamp01((wearFractionNormalized - 0.72f) / 0.28f);
            var overheatNormalized = TireWearTemperature.ResolveOverheat(config, temperatureC);
            var agingWearMultiplier = 1f + (2.2f * endOfLifeRunaway * endOfLifeRunaway);
            agingWearMultiplier += 2.6f * (endOfLifeRunaway * overheatNormalized * overheatNormalized);

            return (baseWear + workloadWear + slipWear + enduranceWear)
                * loadWearMultiplier
                * temperatureWearMultiplier
                * TireWearMath.Clamp(agingWearMultiplier, 1f, 5f);
        }
    }
}
