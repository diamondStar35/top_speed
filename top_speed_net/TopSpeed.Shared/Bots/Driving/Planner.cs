using System;

namespace TopSpeed.Bots
{
    /// <summary>
    /// The bot driver. Pure and deterministic: the same state and input always produce the same
    /// control output, so the offline client, the dedicated server and the tests all get identical
    /// behaviour from one implementation.
    /// <para>
    /// Work is split by rate. The heavy decisions - the road's speed profile and which line to
    /// take through traffic - run at the driver's reaction rate. The controllers that keep the car
    /// on that line, and the safety layer, run every tick, so a slow driver is late rather than
    /// blind.
    /// </para>
    /// </summary>
    public static class BotDrivingPlanner
    {
        private const float MinPlanIntervalSeconds = 0.05f;
        private const float ContactRecoverySeconds = 1.6f;
        private const float RecoveryPaceFactor = 0.90f;
        private const float SteeringRatePerSecond = 420f;
        private const float PedalRatePerSecond = 320f;
        private const float LaneBiasSpreadM = 0.45f;

        public static BotControlOutput Step(ref BotDriverState state, in BotDrivingInput input)
        {
            if (input.Road.Length == 0)
                return new BotControlOutput(0f, -100f, 0f, 0f, BotManeuver.Follow);

            var skill = BotSkillProfile.For(input.Difficulty);
            Initialize(ref state, in input);
            UpdateMeasuredLateral(ref state, in input);
            AdvanceTimers(ref state, in input);

            var order = BotLaneChooser.ResolveTrafficOrder(in input, ref state, out var trafficCount);

            if (state.PlanCooldownSeconds <= 0f)
            {
                state.PlanCooldownSeconds = Math.Max(MinPlanIntervalSeconds, skill.ReactionSeconds);
                var roadSpeedMps = BotSpeedProfile.Resolve(in input, in skill, state.SpeedScratch!) / 3.6f;
                if (state.RecoverySecondsRemaining > 0f)
                    roadSpeedMps *= RecoveryPaceFactor;
                state.TargetSpeedKph = roadSpeedMps * 3.6f;
                BotLaneChooser.Choose(in input, in skill, ref state, roadSpeedMps, out var targetOffset, out _);
                state.TargetOffsetM = targetOffset;
            }

            var road = input.Road[0];
            var targetX = road.Center + state.TargetOffsetM;
            if (BotSafety.IsLeavingRoad(in input, state.MeasuredLateralMps, out var recoveryOffset))
            {
                targetX = road.Center + recoveryOffset;
                state.TargetOffsetM = recoveryOffset;
            }

            var demand = ResolveAccelerationDemand(in input, in skill, in state, order, trafficCount);
            BotLongitudinalControl.Resolve(in input, demand, out var rawThrottle, out var rawBrake);

            var emergency = BotSafety.RequiredEmergencyBrake(in input, order, trafficCount);
            if (emergency > 0f)
            {
                var floor = -emergency * 100f;
                if (floor < rawBrake)
                {
                    rawBrake = floor;
                    rawThrottle = 0f;
                }
            }

            var rawSteering = BotSteeringControl.Resolve(in input, targetX, state.MeasuredLateralMps);

            var lag = Math.Max(0.02f, skill.ReactionSeconds * 0.5f);
            state.Throttle = BotMath.Approach(state.Throttle, rawThrottle, input.ElapsedSeconds, lag, PedalRatePerSecond);
            state.Brake = BotMath.Approach(state.Brake, rawBrake, input.ElapsedSeconds, lag, PedalRatePerSecond);
            state.Steering = BotMath.Approach(state.Steering, rawSteering, input.ElapsedSeconds, lag * 0.6f, SteeringRatePerSecond);

            // Emergency braking must not be softened by the reaction lag.
            if (emergency >= 0.999f)
            {
                state.Brake = -100f;
                state.Throttle = 0f;
            }

            return new BotControlOutput(state.Throttle, state.Brake, state.Steering, state.TargetSpeedKph, state.Maneuver);
        }

        /// <summary>
        /// Called by the host when this bot has been hit. Contact wipes the tire model's lateral
        /// state, so the driver calms down for a moment - back to the middle, no overtakes -
        /// instead of immediately diving for a gap and collecting the next car.
        /// </summary>
        public static void NotifyContact(ref BotDriverState state)
        {
            state.RecoverySecondsRemaining = ContactRecoverySeconds;
            state.HasLastPosition = false;
            state.MeasuredLateralMps = 0f;
            state.PlanCooldownSeconds = 0f;
            state.Maneuver = BotManeuver.Follow;
        }

        private static void Initialize(ref BotDriverState state, in BotDrivingInput input)
        {
            if (state.SpeedScratch == null || state.SpeedScratch.Length < input.Road.Length)
                state.SpeedScratch = new float[Math.Max(BotRoadSampling.SampleCount, input.Road.Length)];

            if (state.Initialized)
                return;

            state.Initialized = true;
            state.RandomState = input.Seed != 0u ? input.Seed : input.VehicleId + 1u;
            state.TargetOffsetM = 0f;
            state.TargetSpeedKph = 0f;
            state.Maneuver = BotManeuver.Follow;
            state.PlanCooldownSeconds = 0f;

            // A small, stable line preference per bot so a field of them does not stack into one
            // groove. It is a preference, not noise: it never changes during the race.
            state.LaneBiasM = BotMath.SignedRandom(ref state.RandomState) * LaneBiasSpreadM;
        }

        private static void UpdateMeasuredLateral(ref BotDriverState state, in BotDrivingInput input)
        {
            if (state.HasLastPosition && input.ElapsedSeconds > 0.0005f)
            {
                var measured = (input.PositionX - state.LastPositionX) / input.ElapsedSeconds;
                var limit = (input.SpeedMps * 0.5f) + 2f;
                state.MeasuredLateralMps = BotMath.Clamp(measured, -limit, limit);
            }
            else
            {
                state.MeasuredLateralMps = 0f;
            }

            state.LastPositionX = input.PositionX;
            state.HasLastPosition = true;
        }

        private static void AdvanceTimers(ref BotDriverState state, in BotDrivingInput input)
        {
            state.PlanCooldownSeconds -= input.ElapsedSeconds;
            state.RecoverySecondsRemaining = Math.Max(0f, state.RecoverySecondsRemaining - input.ElapsedSeconds);
        }

        /// <summary>
        /// Combines the road's speed profile with car following. The profile says how fast the
        /// track allows; the follower says how fast the car ahead allows. The driver obeys
        /// whichever is lower.
        /// </summary>
        private static float ResolveAccelerationDemand(
            in BotDrivingInput input,
            in BotSkillProfile skill,
            in BotDriverState state,
            int[] order,
            int trafficCount)
        {
            var speedMps = input.SpeedMps;
            var targetMps = state.TargetSpeedKph / 3.6f;

            // Quadratic approach law: the demand a constant-deceleration solve would need over a
            // short relaxation distance. Scales correctly at every speed, unlike a fixed gain.
            var relax = Math.Max(4f, speedMps * 0.4f);
            var profileDemand = ((targetMps * targetMps) - (speedMps * speedMps)) / (2f * relax);

            FindLeader(in input, order, trafficCount, out var gap, out var leadSpeedMps);
            BotLongitudinalControl.ResolveEnvelope(in input, skill.BrakeConfidence, out var maxAccel, out var maxDecel);
            var following = BotFollowModel.Acceleration(
                speedMps,
                Math.Max(1f, targetMps),
                gap,
                leadSpeedMps,
                skill.HeadwaySeconds,
                maxAccel,
                maxDecel);

            return Math.Min(profileDemand, following);
        }

        private static void FindLeader(in BotDrivingInput input, int[] order, int trafficCount, out float gapM, out float leadSpeedMps)
        {
            gapM = float.PositiveInfinity;
            leadSpeedMps = 0f;
            if (trafficCount == 0)
                return;

            var road = input.Road[0];
            var egoOffset = input.PositionX - road.Center;
            var egoHalfWidth = input.WidthM * 0.5f;
            var egoHalfLength = input.LengthM * 0.5f;

            for (var k = 0; k < trafficCount; k++)
            {
                var other = input.Traffic[order[k]];
                if (other.Id == input.VehicleId)
                    continue;

                var dy = other.PositionY - input.PositionY;
                if (dy <= 0f || dy > 220f)
                    continue;

                var otherOffset = other.PositionX - BotRoadGeometry.CenterAt(in input, dy);
                var requiredX = egoHalfWidth + (other.WidthM * 0.5f) + 0.45f;
                if (Math.Abs(otherOffset - egoOffset) >= requiredX)
                    continue;

                var gap = dy - egoHalfLength - (other.LengthM * 0.5f);
                if (gap >= gapM)
                    continue;
                gapM = gap;
                leadSpeedMps = other.SpeedKph / 3.6f;
            }
        }
    }
}
