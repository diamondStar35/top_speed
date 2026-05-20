using System;

namespace TopSpeed.Physics.Tires.Wear
{
    internal static class TireWearMath
    {
        public static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }

        public static float Clamp01(float value)
        {
            return Clamp(value, 0f, 1f);
        }

        public static float Clamp(float value, float min, float max)
        {
            if (!IsFinite(value))
                return min;
            if (value < min)
                return min;
            if (value > max)
                return max;
            return value;
        }

        public static float Lerp(float from, float to, float t)
        {
            return from + ((to - from) * Clamp01(t));
        }

        public static float Pow(float value, float exponent)
        {
            return (float)Math.Pow(Clamp01(value), exponent);
        }

        public static float ResolveExpAlpha(float elapsedSeconds, float timeConstantSeconds)
        {
            var tau = Math.Max(0.0001f, timeConstantSeconds);
            var alpha = 1f - (float)Math.Exp(-elapsedSeconds / tau);
            return Clamp01(alpha);
        }
    }
}
