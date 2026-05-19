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
            var baseWear = distanceKilometers * config.BaseWearPerKilometer;
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
            var loadWearMultiplier = 1f + (input.LoadNormalized * config.LoadWearGain);
            var temperatureWearMultiplier = TireWearTemperature.ResolveWearMultiplier(config, temperatureC);
            var wearFractionNormalized = TireWearMath.Clamp01(wearFraction);
            var endOfLifeRunaway = TireWearMath.Clamp01((wearFractionNormalized - 0.72f) / 0.28f);
            var overheatNormalized = TireWearTemperature.ResolveOverheat(config, temperatureC);
            var agingWearMultiplier = 1f + (2.2f * endOfLifeRunaway * endOfLifeRunaway);
            agingWearMultiplier += 2.6f * (endOfLifeRunaway * overheatNormalized * overheatNormalized);

            return (baseWear + slipWear)
                * loadWearMultiplier
                * temperatureWearMultiplier
                * TireWearMath.Clamp(agingWearMultiplier, 1f, 5f);
        }
    }
}
