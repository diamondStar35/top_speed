using System;

namespace TopSpeed.Physics.Tires.Wear
{
    internal static class TireWearTemperature
    {
        public static float ResolveWearMultiplier(TireWearConfig config, float temperatureC)
        {
            var multiplier = 1f;
            if (temperatureC > config.WearHotStartTemperatureC)
                multiplier += (temperatureC - config.WearHotStartTemperatureC) * config.WearHotGainPerC;
            if (temperatureC < config.WearColdStartTemperatureC)
                multiplier += (config.WearColdStartTemperatureC - temperatureC) * config.WearColdGainPerC;
            return Math.Max(0.2f, multiplier);
        }

        public static float ResolveGrip(TireWearConfig config, float temperatureC)
        {
            if (temperatureC <= config.ColdEndTemperatureC)
            {
                var coldSpan = Math.Max(0.001f, config.ColdEndTemperatureC);
                var t = TireWearMath.Clamp01(temperatureC / coldSpan);
                return TireWearMath.Lerp(config.GripAtVeryCold, config.GripAtColdEnd, t);
            }

            if (temperatureC <= config.OptimalStartTemperatureC)
            {
                var t = TireWearMath.Clamp01((temperatureC - config.ColdEndTemperatureC)
                    / Math.Max(0.001f, config.OptimalStartTemperatureC - config.ColdEndTemperatureC));
                return TireWearMath.Lerp(config.GripAtColdEnd, config.GripAtOptimal, t);
            }

            if (temperatureC <= config.OptimalEndTemperatureC)
                return config.GripAtOptimal;

            if (temperatureC <= config.OverheatEndTemperatureC)
            {
                var t = TireWearMath.Clamp01((temperatureC - config.OptimalEndTemperatureC)
                    / Math.Max(0.001f, config.OverheatEndTemperatureC - config.OptimalEndTemperatureC));
                return TireWearMath.Lerp(config.GripAtOptimal, config.GripAtOverheatEnd, t);
            }

            var cookedSpan = Math.Max(1f, config.OverheatEndTemperatureC * 0.5f);
            var cookedT = TireWearMath.Clamp01((temperatureC - config.OverheatEndTemperatureC) / cookedSpan);
            return TireWearMath.Lerp(config.GripAtOverheatEnd, config.GripAtCooked, cookedT);
        }

        public static float ResolveOverheat(TireWearConfig config, float temperatureC)
        {
            var range = Math.Max(0.001f, config.OverheatEndTemperatureC - config.OptimalEndTemperatureC);
            return TireWearMath.Clamp01((temperatureC - config.OptimalEndTemperatureC) / range);
        }

        public static float ResolveNormalized(TireWearConfig config, float temperatureC)
        {
            var low = config.ColdEndTemperatureC;
            var high = config.OverheatEndTemperatureC;
            return TireWearMath.Clamp01((temperatureC - low) / Math.Max(0.001f, high - low));
        }
    }
}
