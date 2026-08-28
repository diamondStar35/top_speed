using System;

namespace TopSpeed.Bots
{
    /// <summary>
    /// Intelligent Driver Model car following.
    /// <para>
    /// The previous planner cut its speed target by a multiple of the gap deficit, which
    /// over-shoots downwards: the car behind sees a bigger deficit and brakes harder still, so a
    /// small lift at the front of the field grows into a pile-up at the back. IDM is string
    /// stable with these parameters — a disturbance decays as it travels upstream instead of
    /// amplifying — which is exactly what stops the chain of bots slamming into each other.
    /// </para>
    /// </summary>
    internal static class BotFollowModel
    {
        public const float MinGapM = 5.5f;
        private const float SpeedExponent = 4f;

        /// <summary>
        /// Sensitivity of the gap-keeping term, deliberately far below the car's real braking
        /// authority. The model is asymmetric on purpose: accelerate with everything the car has,
        /// but respond to a closing gap gently. Reacting to a small gap error with the full 9 m/s2
        /// the brakes can deliver is precisely what makes a queue of cars amplify one driver's lift
        /// into a pile-up at the back. Genuine emergencies are not this term's job -
        /// <see cref="BotSafety"/> owns those.
        /// </summary>
        private const float FollowResponseMps2 = 2.4f;

        /// <summary>
        /// Desired acceleration. <paramref name="gapM"/> is the bumper-to-bumper gap; pass
        /// <c>float.PositiveInfinity</c> when there is no car ahead.
        /// <para>
        /// IDM's <c>a</c> is the driver's maximum acceleration, so it is the car's real envelope
        /// here, not a fixed constant. A constant would silently cap every bot at the same modest
        /// acceleration and make a supercar pull away no harder than a van.
        /// </para>
        /// </summary>
        public static float Acceleration(
            float speedMps,
            float desiredSpeedMps,
            float gapM,
            float leadSpeedMps,
            float headwaySeconds,
            float maxAccelMps2,
            float maxDecelMps2)
        {
            var v = Math.Max(0f, speedMps);
            var v0 = Math.Max(1f, desiredSpeedMps);
            var a = Math.Max(0.5f, maxAccelMps2);
            var b = Math.Max(0.5f, maxDecelMps2);
            var free = a * (1f - (float)Math.Pow(v / v0, SpeedExponent));

            if (float.IsPositiveInfinity(gapM))
                return free;

            var s = Math.Max(0.5f, gapM);
            var closing = v - Math.Max(0f, leadSpeedMps);
            var dynamicGap = (v * headwaySeconds) + ((v * closing) / (2f * BotMath.Sqrt(FollowResponseMps2 * b)));
            var desiredGap = MinGapM + Math.Max(0f, dynamicGap);
            var interaction = (desiredGap / s) * (desiredGap / s);

            return free - (FollowResponseMps2 * interaction);
        }

        /// <summary>
        /// Steady-state speed this follower would settle at behind a leader at the given gap.
        /// Used to score how much progress a candidate lane actually costs.
        /// </summary>
        public static float SettledSpeedMps(float desiredSpeedMps, float gapM, float leadSpeedMps, float headwaySeconds)
        {
            if (float.IsPositiveInfinity(gapM))
                return desiredSpeedMps;

            // At equilibrium the follower matches the leader once the gap supports it.
            var supported = Math.Max(0f, (Math.Max(0.5f, gapM) - MinGapM) / Math.Max(0.2f, headwaySeconds));
            return Math.Min(desiredSpeedMps, Math.Max(leadSpeedMps, Math.Min(leadSpeedMps + 3f, supported)));
        }
    }
}
