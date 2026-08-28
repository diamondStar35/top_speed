using System;

namespace TopSpeed.Bots
{
    /// <summary>
    /// Where in the corridor the driver would ideally sit if traffic did not matter.
    /// <para>
    /// Because a corner here is the road sliding sideways rather than a radius, there is no apex
    /// to clip. What there is: a car that cannot quite match the drift falls behind the corridor
    /// and drifts toward the outside edge. So the ideal line pre-positions toward the side the
    /// road is moving <em>into</em>, buying margin for exactly that lag.
    /// </para>
    /// </summary>
    internal static class BotRacingLine
    {
        private const float LookaheadSeconds = 1.6f;
        private const float MaxLineFraction = 0.80f;

        public static float Offset(in BotDrivingInput input, in BotSkillProfile skill, float usableHalfWidth)
        {
            var road = input.Road;
            if (road.Length == 0 || skill.LineWeight <= 0f)
                return 0f;

            var horizon = Math.Max(25f, input.SpeedMps * LookaheadSeconds);
            var drift = 0f;
            for (var i = 0; i < road.Length; i++)
            {
                if (road[i].DistanceAheadM > horizon)
                    break;
                if (Math.Abs(road[i].DriftPerMeter) > Math.Abs(drift))
                    drift = road[i].DriftPerMeter;
            }

            if (Math.Abs(drift) <= 0.0001f)
                return 0f;

            // How demanding this corner is relative to what the tires can hold right now.
            var capacity = BotCornerLimit.AchievableDriftRatio(input.SpeedKph, input.Capabilities.HighSpeedStability, input.Capabilities.TurnResponse);
            var demand = BotMath.Clamp01(Math.Abs(drift) / Math.Max(0.0001f, capacity));

            var fraction = MaxLineFraction * demand * BotMath.Clamp01(skill.LineWeight);
            return BotMath.Sign(drift) * fraction * usableHalfWidth;
        }
    }
}
