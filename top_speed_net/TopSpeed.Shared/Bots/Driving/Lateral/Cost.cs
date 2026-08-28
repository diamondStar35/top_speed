using System;

namespace TopSpeed.Bots
{
    /// <summary>
    /// Scores one candidate lane by predicting where every other car will be, rather than by
    /// vetoing any lane that currently has a car anywhere near it. The old veto rejected a lane
    /// if another car sat anywhere in a 73 m window, so inside a pack no lane was ever "safe",
    /// nobody overtook, and the whole field queued up and braked.
    /// </summary>
    internal static class BotLaneCost
    {
        private const float HorizonSeconds = 3.0f;
        private const int PredictionSteps = 6;
        private const float LateralMarginM = 0.60f;
        private const float LongitudinalMarginM = 2.5f;

        private const float CollisionWeight = 22f;
        private const float ProgressWeight = 14f;

        /// <summary>Cost of abandoning the line the driver already committed to.</summary>
        private const float CommitmentWeight = 1.25f;

        /// <summary>Cost of the physical effort of moving away from where the car currently is.</summary>
        private const float ManeuverWeight = 0.35f;

        private const float EdgeWeight = 2.0f;

        /// <summary>
        /// Pull toward the ideal line. Deliberately strong and quadratic: without it the cheapest
        /// option is always "stay exactly where you are", and the car parks wherever it happens to
        /// be instead of returning to the racing line.
        /// </summary>
        private const float LineWeight = 2.0f;

        private const float RecoveryCenterWeight = 12f;

        /// <summary>A driver that has just been hit stops racing for a moment and gets settled.</summary>
        private const float RecoveryProgressFactor = 0.25f;

        public static float Evaluate(
            in BotDrivingInput input,
            in BotSkillProfile skill,
            in BotDriverState state,
            int[] order,
            int trafficCount,
            float candidateOffset,
            float currentOffset,
            float desiredSpeedMps,
            float lineOffset,
            float usableHalfWidth,
            out float laneSpeedMps)
        {
            var traffic = input.Traffic;
            var egoSpeedMps = input.SpeedMps;
            var egoHalfWidth = input.WidthM * 0.5f;
            var egoHalfLength = input.LengthM * 0.5f;

            // How long a lane change to this candidate would take.
            var maxLateralMps = Math.Max(0.6f, egoSpeedMps * BotCornerLimit.MaxDriftRatio(input.SpeedKph, input.Capabilities.HighSpeedStability) * 0.6f);
            var shift = Math.Abs(candidateOffset - currentOffset);
            var moveSeconds = BotMath.Clamp(shift / maxLateralMps, 0.25f, 2.0f);

            var collision = 0f;
            var leaderGap = float.PositiveInfinity;
            var leaderSpeedMps = 0f;

            for (var k = 0; k < trafficCount; k++)
            {
                var other = traffic[order[k]];
                if (other.Id == input.VehicleId)
                    continue;

                var dy0 = other.PositionY - input.PositionY;
                if (dy0 > 260f || dy0 < -90f)
                    continue;

                var otherOffset = other.PositionX - BotRoadGeometry.CenterAt(in input, dy0);
                var otherSpeedMps = other.SpeedKph / 3.6f;
                var otherDrift = BotRoadGeometry.DriftAt(in input, dy0);
                var otherLaneChangeMps = other.LateralVelocityMps - (otherDrift * otherSpeedMps);
                otherLaneChangeMps = BotMath.Clamp(otherLaneChangeMps, -4f, 4f);

                var requiredX = egoHalfWidth + (other.WidthM * 0.5f) + LateralMarginM;
                var requiredY = egoHalfLength + (other.LengthM * 0.5f) + LongitudinalMarginM;
                var yield = YieldFactor(in input, in other, dy0);

                for (var s = 1; s <= PredictionSteps; s++)
                {
                    var t = HorizonSeconds * s / PredictionSteps;
                    var egoAt = currentOffset + ((candidateOffset - currentOffset) * Math.Min(1f, t / moveSeconds));
                    var otherAt = otherOffset + (otherLaneChangeMps * t);
                    var dy = dy0 + ((otherSpeedMps - egoSpeedMps) * t);

                    var overlapX = 1f - (Math.Abs(otherAt - egoAt) / requiredX);
                    if (overlapX <= 0f)
                        continue;
                    var overlapY = 1f - (Math.Abs(dy) / requiredY);
                    if (overlapY <= 0f)
                        continue;

                    // Sooner conflicts hurt more than distant ones.
                    var urgency = 1f - (t / (HorizonSeconds + 1f));
                    collision += CollisionWeight * overlapX * overlapY * urgency * yield / PredictionSteps;
                }

                // Leader in this lane, for the progress term.
                if (dy0 <= 0f || Math.Abs(otherOffset - candidateOffset) >= requiredX)
                    continue;
                var gap = dy0 - egoHalfLength - (other.LengthM * 0.5f);
                if (gap >= leaderGap)
                    continue;
                leaderGap = gap;
                leaderSpeedMps = otherSpeedMps;
            }

            laneSpeedMps = BotFollowModel.SettledSpeedMps(desiredSpeedMps, leaderGap, leaderSpeedMps, skill.HeadwaySeconds);
            var recovering = state.RecoverySecondsRemaining > 0f;
            var progressWeight = recovering ? ProgressWeight * RecoveryProgressFactor : ProgressWeight;
            var progress = progressWeight * BotMath.Clamp01((desiredSpeedMps - laneSpeedMps) / Math.Max(1f, desiredSpeedMps));

            var boldness = BotMath.Clamp(skill.OvertakeBoldness, 0.2f, 1f);
            var span = Math.Max(0.5f, usableHalfWidth);
            var commitment = (CommitmentWeight / boldness) * (Math.Abs(candidateOffset - state.TargetOffsetM) / span);
            var maneuver = ManeuverWeight * (shift / span);

            var edgeUse = Math.Abs(candidateOffset) / Math.Max(0.05f, usableHalfWidth);
            var edgeExcess = Math.Max(0f, edgeUse - 0.70f) / 0.30f;
            var edge = EdgeWeight * edgeExcess * edgeExcess;

            var lineError = (candidateOffset - lineOffset) / span;
            var line = LineWeight * lineError * lineError;

            var recovery = 0f;
            if (recovering)
            {
                var centreError = candidateOffset / span;
                recovery = RecoveryCenterWeight * centreError * centreError;
            }

            return collision + progress + commitment + maneuver + edge + line + recovery;
        }

        /// <summary>
        /// Bots defer to whoever has the better claim on a piece of road: the player first, then
        /// whoever is ahead, then the lower id. Every bot runs this same pure rule on the same
        /// observations, so two of them never decide to take the same gap.
        /// </summary>
        private static float YieldFactor(in BotDrivingInput input, in BotVehicleObservation other, float dy)
        {
            if (other.IsHuman)
                return 1.60f;
            if (dy > 0.5f)
                return 1.35f;
            if (dy < -0.5f)
                return 0.85f;
            return other.Id < input.VehicleId ? 1.25f : 0.90f;
        }
    }
}
