using System;

namespace TopSpeed.Vehicles
{
    internal static class TireTractionAssist
    {
        public static float ResolveStraightLineTractionScale(
            float baseTractionScale,
            float speedMps,
            int steeringInput,
            float slipAngleNormalized,
            float lateralSlipNormalized,
            float wearFraction,
            float overheatNormalized)
        {
            var clampedBase = Clamp(baseTractionScale, 0.45f, 1f);
            var steerNormalized = Clamp01(Math.Abs(steeringInput) / 100f);
            var straightness = 1f - Clamp01(Math.Max(
                steerNormalized / 0.14f,
                Math.Max(
                    Clamp01(slipAngleNormalized) / 0.18f,
                    Clamp01(lateralSlipNormalized) / 0.22f)));
            var speedGate = Clamp01((Math.Max(0f, speedMps) - 6f) / 18f);
            var assistDemand = straightness * speedGate;
            if (assistDemand <= 0f)
                return clampedBase;

            var damage = Clamp01(Math.Max(Clamp01(wearFraction), Clamp01(overheatNormalized)));
            var severeDamage = Clamp01((damage - 0.72f) / 0.28f);
            var assistStrength = assistDemand * (1f - (0.35f * severeDamage));
            var straightFloor = Lerp(0.92f, 0.78f, severeDamage);
            var assistedScale = clampedBase + ((1f - clampedBase) * assistStrength * 0.85f);

            return Clamp(Math.Max(straightFloor, assistedScale), 0.45f, 1f);
        }

        private static float Lerp(float a, float b, float t)
        {
            return a + ((b - a) * Clamp01(t));
        }

        private static float Clamp01(float value)
        {
            return Clamp(value, 0f, 1f);
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
