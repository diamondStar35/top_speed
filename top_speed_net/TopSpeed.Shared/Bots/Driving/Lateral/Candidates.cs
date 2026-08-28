using System;

namespace TopSpeed.Bots
{
    /// <summary>
    /// Picks a lateral target by scoring a continuous spread of lines across the corridor.
    /// The previous planner only ever considered three (center, far left, far right), so every
    /// bot in a pack aimed at the same three x values and they converged into each other.
    /// </summary>
    internal static class BotLaneChooser
    {
        public const int CandidateCount = 11;
        private const float RoadMarginM = 0.35f;

        /// <summary>A new lane must beat the committed one by this much before the driver commits.</summary>
        private const float SwitchMargin = 0.45f;

        public static void Choose(
            in BotDrivingInput input,
            in BotSkillProfile skill,
            ref BotDriverState state,
            float desiredSpeedMps,
            out float targetOffset,
            out float laneSpeedMps)
        {
            var road = input.Road[0];
            var usableHalfWidth = BotRoadGeometry.UsableHalfWidth(in road, input.WidthM, RoadMarginM);
            var currentOffset = input.PositionX - road.Center;
            var order = ResolveTrafficOrder(in input, ref state, out var trafficCount);
            var lineOffset = BotRacingLine.Offset(in input, in skill, usableHalfWidth) + state.LaneBiasM;

            if (state.RecoverySecondsRemaining > 0f)
                lineOffset = 0f;
            lineOffset = BotMath.Clamp(lineOffset, -usableHalfWidth, usableHalfWidth);

            var committed = BotMath.Clamp(state.TargetOffsetM, -usableHalfWidth, usableHalfWidth);
            var committedCost = BotLaneCost.Evaluate(
                in input, in skill, in state, order, trafficCount,
                committed, currentOffset, desiredSpeedMps, lineOffset, usableHalfWidth, out var committedSpeed);

            var bestOffset = committed;
            var bestCost = committedCost;
            var bestSpeed = committedSpeed;

            for (var i = 0; i < CandidateCount; i++)
            {
                var t = CandidateCount == 1 ? 0.5f : (float)i / (CandidateCount - 1);
                var offset = BotMath.Lerp(-usableHalfWidth, usableHalfWidth, t);
                var cost = BotLaneCost.Evaluate(
                    in input, in skill, in state, order, trafficCount,
                    offset, currentOffset, desiredSpeedMps, lineOffset, usableHalfWidth, out var speed);

                if (cost >= bestCost)
                    continue;
                bestCost = cost;
                bestOffset = offset;
                bestSpeed = speed;
            }

            // Hysteresis on the decision itself rather than a fixed time lock, so a committed line
            // is never held past the point where it stopped making sense.
            if (bestCost > committedCost - SwitchMargin)
            {
                bestOffset = committed;
                bestCost = committedCost;
                bestSpeed = committedSpeed;
            }

            targetOffset = BotMath.Clamp(bestOffset, -usableHalfWidth, usableHalfWidth);
            laneSpeedMps = bestSpeed;

            var delta = targetOffset - currentOffset;
            state.Maneuver = delta < -0.35f
                ? BotManeuver.PassLeft
                : delta > 0.35f ? BotManeuver.PassRight : BotManeuver.Follow;
        }

        /// <summary>
        /// Traffic sorted by id. Float sums are order dependent, so a stable iteration order is
        /// what makes the planner's decision independent of how the host happened to build the
        /// snapshot — and keeps client and server bit-identical.
        /// </summary>
        internal static int[] ResolveTrafficOrder(in BotDrivingInput input, ref BotDriverState state, out int count)
        {
            var traffic = input.Traffic;
            count = traffic.Length;
            var order = state.OrderScratch;
            if (order == null || order.Length < count)
            {
                order = new int[Math.Max(8, count)];
                state.OrderScratch = order;
            }

            for (var i = 0; i < count; i++)
                order[i] = i;

            for (var i = 1; i < count; i++)
            {
                var value = order[i];
                var key = traffic[value].Id;
                var j = i - 1;
                while (j >= 0 && traffic[order[j]].Id > key)
                {
                    order[j + 1] = order[j];
                    j--;
                }
                order[j + 1] = value;
            }

            return order;
        }
    }
}
