using System;

namespace TopSpeed.Physics.Tires
{
    internal static class TireStep
    {
        public static TireModelOutput Solve(
            in TireModelParameters parameters,
            in TireModelInput input,
            in TireModelState state,
            in TireSteerData steer,
            in TireAxleData axle)
        {
            var dt = Math.Max(0.0001f, input.ElapsedSeconds);
            var massKg = Math.Max(100f, parameters.MassKg);
            var wheelbase = Math.Max(0.5f, axle.Wheelbase);
            var trackWidth = Math.Max(0.45f, axle.TrackWidth);
            var yawInertia = massKg * ((wheelbase * wheelbase) + (trackWidth * trackWidth)) * 0.18f * Math.Max(0.5f, parameters.YawInertiaScale);
            var damping = Math.Max(0f, parameters.TransientDamping);
            var yaw = TireYaw.Resolve(parameters, steer, axle, massKg);
            var steerSign = TireModelMath.Sign(steer.SteerRad);
            var steerMag = TireModelMath.Clamp01(Math.Abs(input.SteeringInput) / 100f);
            var highSpeedStability = TireModelMath.Clamp(parameters.HighSpeedStability, 0f, 1f);
            var highSpeedStabilityScale = TireModelMath.Lerp(
                1f,
                TireModelMath.Lerp(0.72f, 0.48f, highSpeedStability),
                yaw.SpeedSharpness);
            // Recenter damping should only dominate when steering is truly near neutral.
            var neutralSteer = TireModelMath.Clamp01(1f - (steerMag * 4.0f));

            var vyDot = (axle.TotalForce / massKg) - (steer.ForwardSpeed * state.YawRateRad);
            var rDot = ((axle.A * axle.FrontForce) - (axle.B * axle.RearForce)) / yawInertia;
            rDot += (yaw.RateTarget - state.YawRateRad) * yaw.TrackGain;
            if (steerSign != 0f)
            {
                var yawSource = steerSign * Math.Abs(yaw.RateTarget) * yaw.SourceGain;
                rDot += yawSource;

                // Preserve classic "tap steer then release" feel by steering lateral velocity toward input while active.
                var lateralCommandGain = TireModelMath.Lerp(0.28f, 0.12f, yaw.SpeedSharpness) * highSpeedStabilityScale;
                var lateralCommandMps = steerSign * steer.ForwardSpeed * steerMag * lateralCommandGain;
                var lateralTrackGain = TireModelMath.Lerp(2.2f, 1.2f, yaw.SpeedSharpness) + (1.8f * steerMag);
                vyDot += (lateralCommandMps - state.LateralVelocityMps) * lateralTrackGain;
            }

            vyDot -= state.LateralVelocityMps * ((damping * 0.75f) + (0.85f * neutralSteer));
            var yawSpeedDamping = TireModelMath.Lerp(0.35f, 1.50f + (0.90f * highSpeedStability), yaw.SpeedSharpness);
            rDot -= state.YawRateRad * ((damping * 0.55f) + yawSpeedDamping + (1.00f * neutralSteer));
            if (steerSign != 0f)
            {
                var activeYawDamping = TireModelMath.Lerp(0.12f, 1.40f + (0.90f * highSpeedStability), yaw.SpeedSharpness);
                rDot -= state.YawRateRad * activeYawDamping * steerMag;
            }

            var nextVy = state.LateralVelocityMps + (vyDot * dt);
            var nextYawRate = state.YawRateRad + (rDot * dt);

            var neutralInput = Math.Abs(input.SteeringInput) <= 8;
            if (neutralInput)
            {
                // Fast recenter for legacy "release stops steering" feel.
                var lateralDecay = Math.Max(0f, 1f - (24f * dt));
                var yawDecay = Math.Max(0f, 1f - (28f * dt));
                nextVy *= lateralDecay;
                nextYawRate *= yawDecay;
                if (Math.Abs(nextVy) < 0.03f)
                    nextVy = 0f;
                if (Math.Abs(nextYawRate) < 0.02f)
                    nextYawRate = 0f;
            }

            nextVy = TireModelMath.Clamp(nextVy, -steer.ForwardSpeed * 1.6f, steer.ForwardSpeed * 1.6f);
            nextYawRate = TireModelMath.Clamp(nextYawRate, -5f, 5f);

            // Keep direction locking only for deliberate steering so small corrections can naturally recenter.
            var desiredDirection = TireModelMath.Sign(input.SteeringInput);
            var enforceDirection = desiredDirection != 0f
                && steer.SpeedMps > 1f
                && steerMag >= 0.24f;
            if (enforceDirection)
            {
                if (TireModelMath.Sign(nextVy) != desiredDirection)
                    nextVy = desiredDirection * Math.Abs(nextVy);
                if (TireModelMath.Sign(nextYawRate) != desiredDirection)
                    nextYawRate = desiredDirection * Math.Abs(nextYawRate);
            }

            var combinedPenalty = TireModelMath.Clamp(parameters.CombinedGripPenalty, 0f, 1f);
            var lateralLoad = axle.LateralForceRatio * combinedPenalty;
            var longitudinalGripFactor = TireModelMath.Clamp(1f - ((lateralLoad * lateralLoad) * 0.6f), 0.35f, 1f);

            var massRatio = (float)Math.Sqrt(1500f / massKg);
            var agilityMassScale = TireModelMath.Lerp(1f, TireModelMath.Clamp(massRatio, 0.5f, 2.5f), TireModelMath.Clamp01(parameters.MassSensitivity));
            var stabilityPenalty = parameters.HighSpeedStability * steer.SpeedNorm;
            stabilityPenalty *= TireModelMath.Lerp(1f, 1.35f, TireModelMath.Clamp01(1f / Math.Max(0.5f, agilityMassScale) - 0.5f));
            var stabilityScale = TireModelMath.Clamp(1f - stabilityPenalty, 0.5f, 1f);

            var responseScale = Math.Max(0.2f, parameters.TurnResponse) * stabilityScale;
            var directSteer = 0f;
            if (steerSign != 0f)
            {
                // Immediate steering authority term so normal-speed turning is responsive.
                var directSteerGain = TireModelMath.Lerp(0.18f, 0.07f, yaw.SpeedSharpness) * highSpeedStabilityScale;
                directSteer = steerSign * steer.ForwardSpeed * steerMag * directSteerGain * Math.Max(0.45f, parameters.TurnResponse);
            }

            var lateralSpeedMps = (nextVy * responseScale + directSteer) * input.SurfaceLateralMultiplier;
            var maxLateralRatio = TireModelMath.Lerp(
                0.18f,
                TireModelMath.Lerp(0.11f, 0.07f, highSpeedStability),
                yaw.SpeedSharpness);
            var maxLateralSpeed = Math.Max(0.5f, steer.ForwardSpeed * maxLateralRatio);
            lateralSpeedMps = TireModelMath.Clamp(lateralSpeedMps, -maxLateralSpeed, maxLateralSpeed);

            var maxYawRate = TireModelMath.Lerp(
                4.5f,
                TireModelMath.Lerp(2.2f, 1.3f, highSpeedStability),
                yaw.SpeedSharpness);
            nextYawRate = TireModelMath.Clamp(nextYawRate, -maxYawRate, maxYawRate);
            var lateralSlipNormalized = TireModelMath.Clamp(
                Math.Abs(nextVy) / Math.Max(0.5f, steer.ForwardSpeed * 0.30f),
                0f,
                3f);

            var nextState = new TireModelState(nextVy, nextYawRate);
            return new TireModelOutput(
                longitudinalGripFactor,
                lateralSpeedMps,
                axle.LateralForceRatio,
                axle.SlipAngleNormalized,
                lateralSlipNormalized,
                nextState);
        }
    }
}
