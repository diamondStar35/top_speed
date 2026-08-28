using System;
using TopSpeed.Physics.Powertrain;
using TopSpeed.Physics.Surface;

namespace TopSpeed.Bots
{
    /// <summary>
    /// Turns a demanded acceleration into pedals by inverting the car's own physics, instead of
    /// mapping a speed error through hand-tuned gains. Two consequences matter:
    /// the same driver works correctly on a 160 kph van and a 350 kph supercar, and the driver
    /// can tell the difference between "I need to slow down" and "I need to brake" — if lifting
    /// off already sheds enough speed, no brake is applied at all.
    /// </summary>
    internal static class BotLongitudinalControl
    {
        private const float BrakeDeadband = 0.05f;
        private const float MinThrottleProbe = 0.02f;
        private const int BisectionSteps = 6;

        public static void Resolve(
            in BotDrivingInput input,
            float demandMps2,
            out float throttle,
            out float brake)
        {
            var capabilities = input.Capabilities;
            var powertrain = capabilities.Powertrain;
            var road = input.Road[0];
            var speedMps = input.SpeedMps;

            if (powertrain == null)
            {
                // No physics available (synthetic inputs): fall back to a plain proportional law.
                if (demandMps2 >= 0f)
                {
                    throttle = BotMath.Clamp(demandMps2 * 40f, 0f, 100f);
                    brake = 0f;
                }
                else
                {
                    throttle = 0f;
                    brake = -BotMath.Clamp(-demandMps2 * 25f, 0f, 100f);
                }
                return;
            }

            var surface = SurfaceModel.Resolve(road.Surface, capabilities.SurfaceTractionFactor);
            var tractionMod = surface.Traction / (capabilities.SurfaceTractionFactor > 0f ? capabilities.SurfaceTractionFactor : 1f);
            var coastAccel = -BotSpeedProfile.CoastDecelMps2(powertrain, in road, speedMps, in capabilities);

            if (demandMps2 > coastAccel + 0.05f)
            {
                throttle = SolveThrottle(powertrain, input.Ego.Gear, speedMps, demandMps2, tractionMod, surface.RollingResistance, DriveRatio(in input)) * 100f;
                brake = 0f;
                return;
            }

            var extraDecel = coastAccel - demandMps2;
            var surfaceBrake = surface.Brake > 0f ? surface.Brake : 1f;
            var fullBrakeMps2 = Calculator.BrakeDecelKph(powertrain, 1f, surfaceBrake) / 3.6f;
            var fraction = fullBrakeMps2 > 0.01f ? extraDecel / fullBrakeMps2 : 0f;

            throttle = 0f;
            brake = fraction < BrakeDeadband ? 0f : -BotMath.Clamp01(fraction) * 100f;
        }

        /// <summary>
        /// Smallest throttle that delivers the demanded acceleration. <see cref="Calculator.DriveAccel"/>
        /// is monotonic in throttle and already nets off drag and rolling resistance, so a short
        /// bisection is exact enough and far cheaper than a table.
        /// </summary>
        private static float SolveThrottle(Config powertrain, int gear, float speedMps, float demandMps2, float tractionMod, float rollingMod, float? driveRatio)
        {
            var full = Accel(powertrain, gear, speedMps, 1f, tractionMod, rollingMod, driveRatio);
            if (full <= demandMps2)
                return 1f;

            var minimum = Accel(powertrain, gear, speedMps, MinThrottleProbe, tractionMod, rollingMod, driveRatio);
            if (minimum >= demandMps2)
                return 0f;

            var lo = MinThrottleProbe;
            var hi = 1f;
            for (var i = 0; i < BisectionSteps; i++)
            {
                var mid = (lo + hi) * 0.5f;
                if (Accel(powertrain, gear, speedMps, mid, tractionMod, rollingMod, driveRatio) < demandMps2)
                    lo = mid;
                else
                    hi = mid;
            }

            return hi;
        }

        /// <summary>Acceleration available at full throttle right now, and the braking to match.</summary>
        public static void ResolveEnvelope(in BotDrivingInput input, float brakeConfidence, out float maxAccelMps2, out float maxDecelMps2)
        {
            var capabilities = input.Capabilities;
            var powertrain = capabilities.Powertrain;
            var road = input.Road[0];
            if (powertrain == null)
            {
                maxAccelMps2 = 3.5f;
                maxDecelMps2 = 6f;
                return;
            }

            var surface = SurfaceModel.Resolve(road.Surface, capabilities.SurfaceTractionFactor);
            var tractionMod = surface.Traction / (capabilities.SurfaceTractionFactor > 0f ? capabilities.SurfaceTractionFactor : 1f);
            maxAccelMps2 = Math.Max(
                0.5f,
                Accel(powertrain, input.Ego.Gear, input.SpeedMps, 1f, tractionMod, surface.RollingResistance, DriveRatio(in input)));
            maxDecelMps2 = BotSpeedProfile.BrakingDecelMps2(powertrain, in road, input.SpeedMps, brakeConfidence, in capabilities);
        }

        private static float? DriveRatio(in BotDrivingInput input)
        {
            var ratio = input.Ego.EffectiveDriveRatio;
            return ratio > 0f ? ratio : (float?)null;
        }

        private static float Accel(Config powertrain, int gear, float speedMps, float throttle, float tractionMod, float rollingMod, float? driveRatio)
        {
            return Calculator.DriveAccel(
                powertrain,
                gear,
                speedMps,
                throttle,
                tractionMod,
                longitudinalGripFactor: 1f,
                rollingResistanceModifier: rollingMod,
                resistanceEnvironment: ResistanceEnvironment.Calm,
                driveRatioOverride: driveRatio);
        }
    }
}
