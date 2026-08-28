using System;
using TopSpeed.Physics.Powertrain;
using TopSpeed.Physics.Surface;

namespace TopSpeed.Bots
{
    /// <summary>
    /// Solves the fastest speed the road allows at the car's current position.
    /// <para>
    /// Each lookahead sample gets a corner speed ceiling, then a backward pass walks the ladder
    /// from the far end inwards, lowering each sample to whatever speed can still be shed in time
    /// using the car's <em>real</em> braking authority. The result is the latest legal braking
    /// point rather than a guess, which is what stops bots braking when they do not need to.
    /// </para>
    /// </summary>
    internal static class BotSpeedProfile
    {
        public static float Resolve(in BotDrivingInput input, in BotSkillProfile skill, float[] scratch)
        {
            var road = input.Road;
            if (road.Length == 0)
                return 0f;

            var capabilities = input.Capabilities;
            var maxKph = capabilities.TopSpeedKph * skill.StraightPace;
            var count = Math.Min(road.Length, scratch.Length);
            var usableHalfWidth = BotRoadGeometry.UsableHalfWidth(in road[0], capabilities.WidthM, 0.35f);
            var currentOffset = input.PositionX - road[0].Center;

            for (var i = 0; i < count; i++)
                scratch[i] = BotCornerLimit.SampleLimitKph(
                    in road[i],
                    in capabilities,
                    skill.GripConfidence,
                    maxKph,
                    WidthBudget(in road[i], currentOffset, usableHalfWidth));

            var powertrain = capabilities.Powertrain;
            if (powertrain == null)
            {
                for (var i = count - 2; i >= 0; i--)
                    scratch[i] = Math.Min(scratch[i], scratch[i + 1]);
                return scratch[0];
            }

            for (var i = count - 2; i >= 0; i--)
            {
                var ds = road[i + 1].DistanceAheadM - road[i].DistanceAheadM;
                if (ds <= 0.01f)
                {
                    scratch[i] = Math.Min(scratch[i], scratch[i + 1]);
                    continue;
                }

                var exitMps = scratch[i + 1] / 3.6f;
                var decel = BrakingDecelMps2(powertrain, in road[i], exitMps, skill.BrakeConfidence, capabilities);
                var entryMps = BotMath.Sqrt((exitMps * exitMps) + (2f * decel * ds));
                var entryKph = entryMps * 3.6f;
                if (entryKph < scratch[i])
                    scratch[i] = entryKph;
            }

            return scratch[0];
        }

        /// <summary>
        /// How much road the car has to spend on the side it will slide toward.
        /// <para>
        /// A car that lags the corridor falls toward the outside of the corner, so sitting on the
        /// inside buys room and therefore corner speed - which is exactly why the racing line
        /// matters here. Close to the car the budget is whatever it actually has right now; further
        /// ahead it blends toward what a well-placed car would have, because by then the driver
        /// will have repositioned.
        /// </para>
        /// </summary>
        private static float WidthBudget(in BotRoadPreview road, float currentOffset, float usableHalfWidth)
        {
            var drift = road.DriftPerMeter;
            if (Math.Abs(drift) <= 0.0001f)
                return usableHalfWidth;

            var immediate = usableHalfWidth + (BotMath.Sign(drift) * currentOffset);
            var placed = usableHalfWidth * 1.5f;
            var t = BotMath.Clamp01(road.DistanceAheadM / 120f);
            return Math.Max(0.3f, BotMath.Lerp(immediate, placed, t));
        }

        /// <summary>
        /// Deceleration actually available at this speed: brakes at the driver's confidence level,
        /// plus the drag and rolling resistance the car gets for free.
        /// </summary>
        public static float BrakingDecelMps2(Config powertrain, in BotRoadPreview road, float speedMps, float brakeConfidence, in BotCapabilities capabilities)
        {
            var surface = SurfaceModel.Resolve(road.Surface, capabilities.SurfaceTractionFactor);
            var surfaceBrake = surface.Brake > 0f ? surface.Brake : 1f;
            var brakeKph = Calculator.BrakeDecelKph(powertrain, 1f, surfaceBrake) * BotMath.Clamp(brakeConfidence, 0.2f, 1f);
            var aeroKph = Calculator.AerodynamicDecelKph(powertrain, speedMps, ResistanceEnvironment.Calm);
            var rollKph = Calculator.RollingResistanceDecelKph(powertrain, speedMps, surface.RollingResistance);
            return Math.Max(0.5f, (brakeKph + aeroKph + rollKph) / 3.6f);
        }

        /// <summary>Deceleration the car sheds by lifting off, with no brake applied.</summary>
        public static float CoastDecelMps2(Config powertrain, in BotRoadPreview road, float speedMps, in BotCapabilities capabilities)
        {
            var surface = SurfaceModel.Resolve(road.Surface, capabilities.SurfaceTractionFactor);
            var aeroKph = Calculator.AerodynamicDecelKph(powertrain, speedMps, ResistanceEnvironment.Calm);
            var rollKph = Calculator.RollingResistanceDecelKph(powertrain, speedMps, surface.RollingResistance);
            var sideKph = Calculator.WheelSideDragDecelKph(powertrain, speedMps);
            return Math.Max(0f, (aeroKph + rollKph + sideKph) / 3.6f);
        }
    }
}
