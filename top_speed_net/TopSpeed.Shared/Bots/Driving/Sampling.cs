using System;

namespace TopSpeed.Bots
{
    /// <summary>
    /// Shared road-preview sampling contract. Both the offline client and the dedicated server
    /// build the planner's lookahead with these helpers so the two hosts see the same road.
    /// </summary>
    public static class BotRoadSampling
    {
        public const int SampleCount = 16;

        /// <summary>How often the lookahead ladder (samples 1..N-1) needs rebuilding.</summary>
        public const float RefreshIntervalSeconds = 0.05f;

        private const float MinHorizonM = 130f;
        private const float MaxHorizonM = 320f;
        private const float HorizonSeconds = 3.2f;
        private const float Bunching = 1.5f;

        /// <summary>
        /// Fills a distance ladder that is dense near the car and sparse far away. The horizon
        /// scales with speed so a fast car still sees far enough ahead to brake in time.
        /// </summary>
        public static void FillDistances(float speedKph, float[] distances)
        {
            if (distances == null)
                throw new ArgumentNullException(nameof(distances));
            if (distances.Length == 0)
                return;

            var horizon = BotMath.Clamp(Math.Max(0f, speedKph) / 3.6f * HorizonSeconds, MinHorizonM, MaxHorizonM);
            distances[0] = 0f;
            if (distances.Length == 1)
                return;

            var last = distances.Length - 1;
            for (var i = 1; i <= last; i++)
            {
                var t = (float)i / last;
                distances[i] = horizon * (float)Math.Pow(t, Bunching);
            }
        }

        public static float[] CreateDistances() => new float[SampleCount];

        public static BotRoadPreview[] CreatePreview() => new BotRoadPreview[SampleCount];
    }
}
