namespace TopSpeed.Physics.Tires.Wear
{
    internal static class TireWearWearModel
    {
        public static float ResolveWearDelta(
            TireWearConfig config,
            in TireWearStepInput input,
            float smoothedSlipNormalized,
            float temperatureC,
            float elapsedSeconds)
        {
            var distanceKilometers = (input.SpeedMps * elapsedSeconds) / 1000f;
            var baseWear = distanceKilometers * config.BaseWearPerKilometer;
            var corneringWearSignal = TireWearMath.Pow(input.CorneringSlipNormalized, 1.15f);
            var longitudinalWearSignal = TireWearMath.Pow(input.LongitudinalSlipNormalized, 1.10f);
            var slipWearSignal = TireWearMath.Clamp(
                (corneringWearSignal * config.CorneringSlipWearWeight)
                + (longitudinalWearSignal * config.LongitudinalSlipWearWeight),
                0f,
                2.5f);
            var slipWear = smoothedSlipNormalized * slipWearSignal * config.SlipWearRatePerSecond * elapsedSeconds;
            var loadWearMultiplier = 1f + (input.LoadNormalized * config.LoadWearGain);
            var temperatureWearMultiplier = TireWearTemperature.ResolveWearMultiplier(config, temperatureC);
            return (baseWear + slipWear) * loadWearMultiplier * temperatureWearMultiplier;
        }
    }
}
