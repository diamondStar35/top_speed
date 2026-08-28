using System;

namespace TopSpeed.Bots
{
    /// <summary>
    /// Corner speed limits derived from the simulation's own lateral envelope.
    /// <para>
    /// A curve in this game is the whole road corridor translating sideways at
    /// <c>DriftPerMeter</c> meters per meter travelled. Staying on the road therefore requires a
    /// lateral velocity of <c>drift * speed</c>, i.e. a lateral-to-forward speed <em>ratio</em>
    /// equal to the drift. The ratio a car can actually produce shrinks as speed rises, so the
    /// corner's real speed limit is the speed at which the car can still generate the drift.
    /// </para>
    /// </summary>
    internal static class BotCornerLimit
    {
        // These mirror the lateral solver in TopSpeed.Shared/Physics/Tires/{Yaw,Step}.cs. They must
        // stay in sync with it: the point of deriving limits instead of tabulating them is that the
        // driver plans against the envelope the simulation will actually give it.
        private const float SharpStartKph = 90f;
        private const float SharpWindowKph = 130f;
        private const float LowSpeedRatio = 0.18f;
        private const float HighSpeedRatioSoft = 0.11f;
        private const float HighSpeedRatioStiff = 0.07f;
        private const float DirectSteerGainLow = 0.18f;
        private const float DirectSteerGainHigh = 0.07f;
        private const float StabilityScaleLow = 0.72f;
        private const float StabilityScaleHigh = 0.48f;
        private const float MinTurnResponse = 0.45f;

        private const float MinCornerSpeedKph = 25f;
        private const int SolveSteps = 14;

        /// <summary>Hard ceiling the tire model imposes on lateral/forward speed ratio.</summary>
        public static float MaxDriftRatio(float speedKph, float highSpeedStability)
        {
            var sharp = BotMath.Clamp01((speedKph - SharpStartKph) / SharpWindowKph);
            var high = BotMath.Lerp(HighSpeedRatioSoft, HighSpeedRatioStiff, highSpeedStability);
            return BotMath.Lerp(LowSpeedRatio, high, sharp);
        }

        /// <summary>
        /// Lateral/forward speed ratio the car can actually hold at full lock.
        /// <para>
        /// In steady cornering the solver's transient lateral velocity is almost entirely damped
        /// out, so what carries the car around a corner is the direct steering authority term.
        /// Sizing corners off the hard clamp instead would let a driver plan for grip it never
        /// gets, and it would run out of road on the exit.
        /// </para>
        /// </summary>
        public static float AchievableDriftRatio(float speedKph, float highSpeedStability, float turnResponse)
        {
            var sharp = BotMath.Clamp01((speedKph - SharpStartKph) / SharpWindowKph);
            var stabilityScale = BotMath.Lerp(
                1f,
                BotMath.Lerp(StabilityScaleLow, StabilityScaleHigh, highSpeedStability),
                sharp);
            var direct = BotMath.Lerp(DirectSteerGainLow, DirectSteerGainHigh, sharp)
                * Math.Max(MinTurnResponse, turnResponse)
                * stabilityScale;
            return Math.Min(direct, MaxDriftRatio(speedKph, highSpeedStability));
        }

        /// <summary>
        /// Highest speed at which the car can still generate a lateral ratio of
        /// <paramref name="required"/>. <see cref="AchievableDriftRatio"/> falls monotonically with
        /// speed, so a bisection finds the crossing point for any car without needing a closed
        /// form per parameter set.
        /// </summary>
        public static float SpeedForRatioKph(float required, in BotCapabilities capabilities, float maxKph)
        {
            if (required <= 0.0001f)
                return maxKph;

            var stability = capabilities.HighSpeedStability;
            var response = capabilities.TurnResponse;

            if (AchievableDriftRatio(maxKph, stability, response) >= required)
                return maxKph;
            if (AchievableDriftRatio(MinCornerSpeedKph, stability, response) < required)
                return MinCornerSpeedKph;

            var lo = MinCornerSpeedKph;
            var hi = maxKph;
            for (var i = 0; i < SolveSteps; i++)
            {
                var mid = (lo + hi) * 0.5f;
                if (AchievableDriftRatio(mid, stability, response) >= required)
                    lo = mid;
                else
                    hi = mid;
            }

            return lo;
        }

        /// <summary>
        /// Speed ceiling for one road sample.
        /// <para>
        /// No official car can hold a hairpin's drift ratio outright — the measured envelope tops
        /// out near 0.18 and a hairpin demands 0.15 on a 5 m half-width road, less than the margin
        /// a real driver needs. What makes hairpins drivable is road width: the car runs a small
        /// lateral deficit and pays for it by sliding across the corridor. So the limit is the
        /// speed at which the deficit, accumulated over what is left of the corner, still fits in
        /// the road the driver has to spend.
        /// </para>
        /// </summary>
        public static float SampleLimitKph(
            in BotRoadPreview road,
            in BotCapabilities capabilities,
            float gripConfidence,
            float maxKph,
            float widthBudgetM)
        {
            var confidence = BotMath.Clamp(gripConfidence, 0.2f, 1f);
            var drift = Math.Abs(road.DriftPerMeter);

            float limit;
            if (drift <= 0.0001f)
            {
                limit = maxKph;
            }
            else
            {
                var budget = Math.Max(0.3f, widthBudgetM) * confidence;
                var allowedDeficit = budget / Math.Max(20f, road.SegmentRemainingM);
                var required = Math.Max(0.01f, drift - allowedDeficit);
                limit = SpeedForRatioKph(required, in capabilities, maxKph) * confidence;
            }

            var surface = BotSharedModel.GetSurfaceSpeedFactor(road.Surface);
            if (surface < 1f)
                limit *= surface;

            // A narrow corridor leaves less room to absorb a lagging line.
            var halfWidth = road.HalfWidth;
            if (halfWidth < 4f)
                limit *= BotMath.Lerp(0.82f, 1f, BotMath.Clamp01((halfWidth - 2f) / 2f));

            return Math.Max(18f, limit);
        }
    }
}
