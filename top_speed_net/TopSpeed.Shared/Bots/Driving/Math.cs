using System;

namespace TopSpeed.Bots
{
    internal static class BotMath
    {
        public static float Clamp(float value, float min, float max) => value < min ? min : value > max ? max : value;

        public static float Clamp01(float value) => Clamp(value, 0f, 1f);

        public static float Lerp(float a, float b, float t) => a + ((b - a) * Clamp01(t));

        public static float Sign(float value) => value > 0f ? 1f : value < 0f ? -1f : 0f;

        public static float Sqrt(float value) => value <= 0f ? 0f : (float)Math.Sqrt(value);

        /// <summary>Deterministic xorshift used for the bot's stable personality offsets.</summary>
        public static float SignedRandom(ref uint state)
        {
            state ^= state << 13;
            state ^= state >> 17;
            state ^= state << 5;
            return ((state & 0x00ffffffu) / 8388607.5f) - 1f;
        }

        /// <summary>
        /// First-order lag toward <paramref name="target"/> with the given time constant, plus a
        /// hard rate limit. Frame-rate independent, which keeps 8 ms server steps and variable
        /// client steps producing the same behaviour.
        /// </summary>
        public static float Approach(float current, float target, float elapsedSeconds, float timeConstantSeconds, float maxRatePerSecond)
        {
            if (elapsedSeconds <= 0f)
                return current;

            var next = target;
            if (timeConstantSeconds > 0f)
            {
                var alpha = Clamp01(elapsedSeconds / timeConstantSeconds);
                next = current + ((target - current) * alpha);
            }

            var maxDelta = maxRatePerSecond * elapsedSeconds;
            var delta = next - current;
            if (delta > maxDelta)
                delta = maxDelta;
            else if (delta < -maxDelta)
                delta = -maxDelta;
            return current + delta;
        }
    }
}
