using System;

namespace TopSpeed.Physics.Fuel
{
    public static class FuelRuntime
    {
        private const float MetersPerSecondToMph = 2.23693629f;
        private const float LitersToGallons = 0.264172052f;
        private const float MinRangeSpeedMps = 0.5f;

        public static FuelRuntimeResult Step(FuelConfig config, in FuelRuntimeState state, in FuelRuntimeInput input)
        {
            if (config == null)
                throw new ArgumentNullException(nameof(config));

            var elapsedSeconds = Math.Max(0f, input.ElapsedSeconds);
            var remainingLiters = Clamp(state.RemainingLiters, 0f, config.TankCapacityLiters);
            var throttle = Clamp(input.ThrottleNormalized, 0f, 1f);
            var netPowerKw = Math.Max(0f, input.NetPowerKw);
            var speedMps = Math.Max(0f, input.SpeedMps);

            var burnLitersPerSecond = 0f;
            if (input.CombustionActive && elapsedSeconds > 0f && remainingLiters > 0f)
            {
                burnLitersPerSecond = ComputeBurnLitersPerSecond(config, throttle, netPowerKw);
                var burnedLiters = burnLitersPerSecond * elapsedSeconds;
                remainingLiters = Math.Max(0f, remainingLiters - burnedLiters);
            }

            var alpha = 1f - (float)Math.Exp(-elapsedSeconds / config.BurnSmoothingTimeConstantSeconds);
            if (alpha < 0f)
                alpha = 0f;
            if (alpha > 1f)
                alpha = 1f;

            var smoothedBurnLitersPerSecond = state.SmoothedBurnLitersPerSecond + ((burnLitersPerSecond - state.SmoothedBurnLitersPerSecond) * alpha);
            if (smoothedBurnLitersPerSecond < 0f)
                smoothedBurnLitersPerSecond = 0f;

            var fuelPercent = config.TankCapacityLiters > 0f
                ? Clamp(remainingLiters / config.TankCapacityLiters, 0f, 1f)
                : 0f;

            var emptyFuel = remainingLiters <= 0.0001f;
            var lowFuel = fuelPercent <= config.LowFuelFraction;
            var powerScale = ResolvePowerScale(config, fuelPercent, emptyFuel);
            var estimatedRangeMeters = ResolveEstimatedRangeMeters(remainingLiters, smoothedBurnLitersPerSecond, speedMps);
            var efficiencyLitersPer100Km = ResolveLitersPer100Km(smoothedBurnLitersPerSecond, speedMps);
            var efficiencyMpg = ResolveMpg(smoothedBurnLitersPerSecond, speedMps);

            return new FuelRuntimeResult(
                new FuelRuntimeState(remainingLiters, smoothedBurnLitersPerSecond),
                burnLitersPerSecond,
                burnLitersPerSecond * 3600f,
                fuelPercent,
                lowFuel,
                emptyFuel,
                powerScale,
                estimatedRangeMeters,
                efficiencyLitersPer100Km,
                efficiencyMpg);
        }

        private static float ComputeBurnLitersPerSecond(FuelConfig config, float throttleNormalized, float netPowerKw)
        {
            var powerFraction = Clamp(netPowerKw / config.ReferencePowerKw, 0f, 1f);
            var loadFactor = Clamp((throttleNormalized * 0.6f) + (powerFraction * 0.4f), 0f, 1f);
            var bsfc = Lerp(config.BsfcLowLoadGPerKwh, config.BsfcHighLoadGPerKwh, loadFactor);
            var displacementDelta = config.EngineDisplacementLiters - FuelDefaults.DefaultEngineDisplacementLiters;
            var displacementScale = 1f + (displacementDelta * config.DisplacementEfficiencyPenaltyPerLiter);
            displacementScale = Clamp(displacementScale, 0.6f, 2.2f);

            var idleBurnLitersPerHour = config.EngineDisplacementLiters * config.BaseIdleBurnLitersPerHourPerLiter;
            var powerBurnKgPerHour = (netPowerKw * bsfc * displacementScale) / 1000f;
            var powerBurnLitersPerHour = powerBurnKgPerHour / config.FuelDensityKgPerLiter;
            var totalBurnLitersPerHour = Math.Max(0f, idleBurnLitersPerHour + powerBurnLitersPerHour);
            return totalBurnLitersPerHour / 3600f;
        }

        private static float ResolvePowerScale(FuelConfig config, float fuelPercent, bool emptyFuel)
        {
            if (emptyFuel)
                return 0f;

            if (fuelPercent >= config.LeanStartFuelFraction || config.LeanStartFuelFraction <= 0f)
                return 1f;

            var t = Clamp(fuelPercent / config.LeanStartFuelFraction, 0f, 1f);
            return Lerp(config.EmptyFuelPowerScale, 1f, t);
        }

        private static float ResolveEstimatedRangeMeters(float remainingLiters, float smoothedBurnLitersPerSecond, float speedMps)
        {
            if (remainingLiters <= 0f || smoothedBurnLitersPerSecond <= 0.000001f || speedMps < MinRangeSpeedMps)
                return 0f;

            var secondsRemaining = remainingLiters / smoothedBurnLitersPerSecond;
            return Math.Max(0f, secondsRemaining * speedMps);
        }

        private static float ResolveLitersPer100Km(float smoothedBurnLitersPerSecond, float speedMps)
        {
            if (smoothedBurnLitersPerSecond <= 0.000001f || speedMps < MinRangeSpeedMps)
                return 0f;
            return (smoothedBurnLitersPerSecond / speedMps) * 100000f;
        }

        private static float ResolveMpg(float smoothedBurnLitersPerSecond, float speedMps)
        {
            if (smoothedBurnLitersPerSecond <= 0.000001f || speedMps < MinRangeSpeedMps)
                return 0f;

            var mph = speedMps * MetersPerSecondToMph;
            var gallonsPerHour = smoothedBurnLitersPerSecond * 3600f * LitersToGallons;
            if (gallonsPerHour <= 0.000001f)
                return 0f;
            return mph / gallonsPerHour;
        }

        private static float Lerp(float a, float b, float t)
        {
            return a + ((b - a) * t);
        }

        private static float Clamp(float value, float min, float max)
        {
            if (value < min)
                return min;
            if (value > max)
                return max;
            return value;
        }
    }
}
