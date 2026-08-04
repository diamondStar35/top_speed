using System;
using System.Collections.Generic;

namespace TopSpeed.Vehicles.Parsing
{
    public static partial class VehicleTsvParser
    {
        private static TransmissionPolicy BuildTransmissionPolicy(Section? policy, int gears, float idleRpm, float revLimiter, float autoShiftRpm)
        {
            if (policy == null)
                return TransmissionPolicy.Default;

            var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var kvp in policy.Entries)
                values[$"policy.{kvp.Key}"] = kvp.Value.Value;

            var resolvedGears = Math.Max(1, gears);
            var intendedTopSpeedGear = ReadInt(values, "policy.top_speed_gear", resolvedGears);
            var allowOverdrive = ReadBool(values, "policy.allow_overdrive_above_game_top_speed", false);

            var baseCooldown = ReadFloat(values, "policy.base_auto_shift_cooldown", 0.15f);
            var fallbackUpshiftDelay = ReadFloat(values, "policy.upshift_delay_default", baseCooldown);
            var perGearUpshiftDelays = new float[resolvedGears];
            for (var gear = 1; gear <= resolvedGears; gear++)
            {
                perGearUpshiftDelays[gear - 1] = fallbackUpshiftDelay;
                if (gear >= resolvedGears)
                    continue;
                var transitionKey = $"policy.upshift_delay_{gear}_{gear + 1}";
                var sourceGearKey = $"policy.upshift_delay_g{gear}";
                var overrideDelay = ReadFloat(values, transitionKey, float.NaN);
                if (!float.IsNaN(overrideDelay))
                {
                    perGearUpshiftDelays[gear - 1] = overrideDelay;
                    continue;
                }

                overrideDelay = ReadFloat(values, sourceGearKey, float.NaN);
                if (!float.IsNaN(overrideDelay))
                    perGearUpshiftDelays[gear - 1] = overrideDelay;
            }

            var defaultUpshiftFraction = 0.92f;
            if (revLimiter > idleRpm && autoShiftRpm > 0f)
                defaultUpshiftFraction = Math.Max(0.05f, Math.Min(1.0f, (autoShiftRpm - idleRpm) / (revLimiter - idleRpm)));

            var upshiftRpmFraction = ReadFloat(values, "policy.auto_upshift_rpm_fraction", defaultUpshiftFraction);
            var upshiftRpmAbsolute = ReadFloat(values, "policy.auto_upshift_rpm", 0f);
            if (upshiftRpmAbsolute > 0f && revLimiter > idleRpm)
                upshiftRpmFraction = (upshiftRpmAbsolute - idleRpm) / (revLimiter - idleRpm);

            var downshiftRpmFraction = ReadFloat(values, "policy.auto_downshift_rpm_fraction", 0.35f);
            var downshiftRpmAbsolute = ReadFloat(values, "policy.auto_downshift_rpm", 0f);
            if (downshiftRpmAbsolute > 0f && revLimiter > idleRpm)
                downshiftRpmFraction = (downshiftRpmAbsolute - idleRpm) / (revLimiter - idleRpm);

            return new TransmissionPolicy(
                intendedTopSpeedGear: intendedTopSpeedGear,
                allowOverdriveAboveGameTopSpeed: allowOverdrive,
                upshiftRpmFraction: upshiftRpmFraction,
                downshiftRpmFraction: downshiftRpmFraction,
                upshiftHysteresis: ReadFloat(values, "policy.upshift_hysteresis", 0.05f),
                baseAutoShiftCooldownSeconds: baseCooldown,
                minUpshiftNetAccelerationMps2: ReadFloat(values, "policy.min_upshift_net_accel_mps2", -0.05f),
                topSpeedPursuitSpeedFraction: ReadFloat(values, "policy.top_speed_pursuit_speed_fraction", 0.97f),
                preferIntendedTopSpeedGearNearLimit: ReadBool(values, "policy.prefer_intended_top_speed_gear_near_limit", true),
                upshiftCooldownBySourceGear: perGearUpshiftDelays);
        }
    }
}

