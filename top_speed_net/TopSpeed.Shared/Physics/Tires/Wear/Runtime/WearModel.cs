namespace TopSpeed.Physics.Tires.Wear
{
    // Wear advances from three sources:
    //   1. Distance — a fixed wear per kilometer at the configured load.
    //   2. Slip   — quadratic in slip, scaled by the per-axis weights so that
    //               cornering and longitudinal slip can be tuned independently.
    //   3. Temperature — a small additive penalty when the tire is too hot or
    //                    too cold (Arrhenius-ish degradation outside the band).
    //
    // Past ~75% wear an exponential aging multiplier kicks in so the final
    // 25% of life burns up much faster than the first 75%.
    internal static class TireWearWearModel
    {
        private const float AgingStart = 0.75f;
        private const float AgingMaxMultiplier = 4.5f;

        public static float ResolveWearDelta(
            TireWearConfig config,
            in TireWearStepInput input,
            in TireWearSmoothedInputs smoothed,
            float wearFraction,
            float temperatureC,
            float elapsedSeconds)
        {
            var distanceKilometers = (input.SpeedMps * elapsedSeconds) / 1000f;
            var load = TireWearMath.Clamp01(smoothed.Load);

            var distanceWear = distanceKilometers * config.BaseWearPerKilometer * (1f + (config.LoadWearGain * load));

            // Smooth-then-square: the slip terms are already low-passed by the
            // shared input filter, so a brief tap averages out before being
            // squared. (The old design squared the instantaneous slip and then
            // gated it by a separately smoothed slip — two filters, one signal.)
            var corneringSlip = TireWearMath.Clamp01(smoothed.CorneringSlip);
            var longitudinalSlip = TireWearMath.Clamp01(smoothed.LongitudinalSlip);
            var slipWearSignal = (config.CorneringSlipWearWeight * corneringSlip * corneringSlip)
                + (config.LongitudinalSlipWearWeight * longitudinalSlip * longitudinalSlip);
            var slipWear = slipWearSignal
                * config.SlipWearRatePerSecond
                * elapsedSeconds;

            var temperatureWear = ResolveTemperaturePenalty(config, temperatureC) * elapsedSeconds;

            var agingMultiplier = ResolveAgingMultiplier(wearFraction);
            return (distanceWear + slipWear + temperatureWear) * agingMultiplier;
        }

        private static float ResolveTemperaturePenalty(TireWearConfig config, float temperatureC)
        {
            var penalty = 0f;
            if (temperatureC > config.WearHotStartTemperatureC)
                penalty += (temperatureC - config.WearHotStartTemperatureC) * config.WearHotGainPerC;
            if (temperatureC < config.WearColdStartTemperatureC)
                penalty += (config.WearColdStartTemperatureC - temperatureC) * config.WearColdGainPerC;
            return penalty * config.SlipWearRatePerSecond;
        }

        private static float ResolveAgingMultiplier(float wearFraction)
        {
            var clamped = TireWearMath.Clamp01(wearFraction);
            if (clamped <= AgingStart)
                return 1f;
            var t = (clamped - AgingStart) / (1f - AgingStart);
            return 1f + ((AgingMaxMultiplier - 1f) * t * t);
        }
    }
}
