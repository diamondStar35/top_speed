using System;

namespace TopSpeed.Bots
{
    /// <summary>
    /// Lateral controller.
    /// <para>
    /// Three things the previous controller got wrong are fixed here. The sign: positive steering
    /// moves the car toward +X, so the correction must be <c>+(target - x)</c>. The feed-forward:
    /// a corner is the corridor sliding sideways, so holding a line needs a standing lateral
    /// velocity of <c>drift * speed</c> that no proportional term can supply. And the damping: a
    /// term on the car's measured lateral velocity, without which the loop weaves.
    /// </para>
    /// </summary>
    internal static class BotSteeringControl
    {
        private const float PositionGain = 1.6f;
        private const float VelocityGain = 0.7f;

        // Mirrors TireStep: a command at or below this magnitude triggers the fast recenter, which
        // would bleed away the lateral velocity a corner needs. Never sit inside it while turning.
        private const float NeutralSteerBand = 8f;
        private const float MinHoldingSteer = 9f;
        private const float MinHoldingDemandMps = 0.15f;

        private const float LowSpeedGain = 0.20f;
        private const float HighSpeedGain = 0.09f;
        private const float SharpStartKph = 90f;
        private const float SharpWindowKph = 130f;

        public static float Resolve(in BotDrivingInput input, float targetX, float measuredLateralMps)
        {
            var road = input.Road[0];
            var speedMps = input.SpeedMps;
            var forward = Math.Max(1f, speedMps);
            var maxLateral = forward * BotCornerLimit.MaxDriftRatio(input.SpeedKph, input.Capabilities.HighSpeedStability);

            var correction = BotMath.Clamp(PositionGain * (targetX - input.PositionX), -maxLateral * 0.8f, maxLateral * 0.8f);
            var feedForward = road.DriftPerMeter * forward;
            var demandVy = BotMath.Clamp(correction + feedForward, -maxLateral, maxLateral);

            var sharp = BotMath.Clamp01((input.SpeedKph - SharpStartKph) / SharpWindowKph);
            var gain = BotMath.Lerp(LowSpeedGain, HighSpeedGain, sharp);

            var closedLoop = demandVy + (VelocityGain * (demandVy - measuredLateralMps));
            var command = BotMath.Clamp((closedLoop / (forward * gain)) * 100f, -100f, 100f);

            // Only a drifting corridor needs a standing lateral velocity, and only that case must
            // escape the recenter band. Forcing the floor for small position corrections as well
            // makes the car overshoot its line and hunt around it.
            if (Math.Abs(feedForward) > MinHoldingDemandMps)
            {
                if (Math.Abs(command) < MinHoldingSteer)
                    command = BotMath.Sign(demandVy) * MinHoldingSteer;
            }
            else if (Math.Abs(command) < NeutralSteerBand)
            {
                command = 0f;
            }

            return command;
        }
    }
}
