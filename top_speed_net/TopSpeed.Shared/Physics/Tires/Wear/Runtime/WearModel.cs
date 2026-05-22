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
                * (0.30f + (0.70f * input.LoadNormalized))
                * (0.44f + (0.56f * speedNormalized))
                * ((input.CorneringUtilizationNormalized * 0.42f) + (input.LongitudinalSlipNormalized * 0.30f) + 0.28f);
            var corneringWearSignal = TireWearMath.Pow(
                (input.CorneringUtilizationNormalized * 0.28f) + (input.CorneringSlipNormalized * 0.72f),
                1.10f);
            var longitudinalWearSignal = TireWearMath.Pow(
                (input.LongitudinalSlipNormalized * 0.35f) + (input.LongitudinalSlideNormalized * 0.65f),
                1.08f);
            var slipWearSignal = TireWearMath.Clamp(
                (corneringWearSignal * config.CorneringSlipWearWeight)
                + (longitudinalWearSignal * config.LongitudinalSlipWearWeight),
                0f,
                2.8f);
            var slipWear = smoothedSlipNormalized * slipWearSignal * config.SlipWearRatePerSecond * elapsedSeconds;
            var enduranceWear = elapsedSeconds
                * config.SlipWearRatePerSecond
                * (0.08f + (0.28f * input.CorneringUtilizationNormalized) + (0.10f * input.LongitudinalSlipNormalized))
                * (0.40f + (0.60f * speedNormalized));
            var loadWearMultiplier = 1f + (input.LoadNormalized * config.LoadWearGain);
            var temperatureWearMultiplier = TireWearTemperature.ResolveWearMultiplier(config, temperatureC);
            var wearFractionNormalized = TireWearMath.Clamp01(wearFraction);
            var endOfLifeRunaway = TireWearMath.Clamp01((wearFractionNormalized - 0.75f) / 0.25f);
            var criticalWear = TireWearMath.Clamp01((wearFractionNormalized - 0.90f) / 0.10f);
            var overheatNormalized = TireWearTemperature.ResolveOverheat(config, temperatureC);
            var agingWearMultiplier = 1f
                + (1.9f * endOfLifeRunaway * endOfLifeRunaway)
                + (2.8f * criticalWear * criticalWear);
            agingWearMultiplier += 3.2f * (endOfLifeRunaway * overheatNormalized * overheatNormalized);
            agingWearMultiplier += 3.0f * (criticalWear * overheatNormalized);

            return (baseWear + workloadWear + slipWear + enduranceWear)
                * loadWearMultiplier
                * temperatureWearMultiplier
                * TireWearMath.Clamp(agingWearMultiplier, 1f, 8f);
        }
    }
}
