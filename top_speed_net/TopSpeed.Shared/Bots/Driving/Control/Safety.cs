using System;
using TopSpeed.Physics.Powertrain;
using TopSpeed.Physics.Surface;

namespace TopSpeed.Bots
{
    /// <summary>
    /// Last-resort layer. Everything above plans for a tidy race; this runs afterwards and is the
    /// only place a bot is allowed to stand on the brakes or abandon its chosen line.
    /// </summary>
    internal static class BotSafety
    {
        private const float LateralMarginM = 0.35f;
        private const float OffRoadHorizonSeconds = 0.8f;
        private const float OffRoadTrigger = 1.05f;

        /// <summary>
        /// Brake fraction (0..1) needed to avoid hitting whatever is directly ahead in this lane,
        /// derived from the closing speed and the remaining gap rather than a fixed time threshold.
        /// </summary>
        public static float RequiredEmergencyBrake(in BotDrivingInput input, int[] order, int trafficCount)
        {
            var capabilities = input.Capabilities;
            var powertrain = capabilities.Powertrain;
            if (powertrain == null || trafficCount == 0)
                return 0f;

            var road = input.Road[0];
            var egoOffset = input.PositionX - road.Center;
            var egoSpeedMps = input.SpeedMps;
            var egoHalfWidth = input.WidthM * 0.5f;
            var egoHalfLength = input.LengthM * 0.5f;

            var worst = 0f;
            for (var k = 0; k < trafficCount; k++)
            {
                var other = input.Traffic[order[k]];
                if (other.Id == input.VehicleId)
                    continue;

                var dy = other.PositionY - input.PositionY;
                if (dy <= 0f || dy > 160f)
                    continue;

                var otherOffset = other.PositionX - BotRoadGeometry.CenterAt(in input, dy);
                var requiredX = egoHalfWidth + (other.WidthM * 0.5f) + LateralMarginM;
                if (Math.Abs(otherOffset - egoOffset) >= requiredX)
                    continue;

                var closing = egoSpeedMps - (other.SpeedKph / 3.6f);
                if (closing <= 0.5f)
                    continue;

                var gap = dy - egoHalfLength - (other.LengthM * 0.5f) - BotFollowModel.MinGapM;
                var required = (closing * closing) / (2f * Math.Max(0.75f, gap));
                if (required > worst)
                    worst = required;
            }

            if (worst <= 0f)
                return 0f;

            var surface = SurfaceModel.Resolve(road.Surface, capabilities.SurfaceTractionFactor);
            var surfaceBrake = surface.Brake > 0f ? surface.Brake : 1f;
            var fullBrakeMps2 = Calculator.BrakeDecelKph(powertrain, 1f, surfaceBrake) / 3.6f;
            if (fullBrakeMps2 <= 0.01f)
                return 0f;

            return BotMath.Clamp01(worst / fullBrakeMps2);
        }

        /// <summary>
        /// True when the car's current lateral drift will carry it out of the corridor within the
        /// next moment. Recovering the road outranks any line or overtake the planner wanted.
        /// </summary>
        public static bool IsLeavingRoad(in BotDrivingInput input, float measuredLateralMps, out float recoveryOffset)
        {
            var road = input.Road[0];
            var usableHalfWidth = BotRoadGeometry.UsableHalfWidth(in road, input.WidthM, 0f);
            var offset = input.PositionX - road.Center;
            var relativeLateral = measuredLateralMps - (road.DriftPerMeter * input.SpeedMps);
            var predicted = offset + (relativeLateral * OffRoadHorizonSeconds);

            recoveryOffset = 0f;
            if (Math.Abs(predicted) <= usableHalfWidth * OffRoadTrigger && Math.Abs(offset) <= usableHalfWidth)
                return false;

            // Aim back across the center line so the recovery actually overshoots the drift.
            recoveryOffset = -BotMath.Sign(offset) * usableHalfWidth * 0.2f;
            return true;
        }
    }
}
