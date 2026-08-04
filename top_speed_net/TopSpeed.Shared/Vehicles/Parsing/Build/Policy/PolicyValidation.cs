using System;
using System.Collections.Generic;
using System.Globalization;

namespace TopSpeed.Vehicles.Parsing
{
    public static partial class VehicleTsvParser
    {
        private static bool ValidatePolicy(
            Section? policy,
            int gears,
            float idleRpm,
            float revLimiter,
            List<VehicleTsvIssue> issues)
        {
            if (policy == null)
                return true;

            if (policy.Entries.TryGetValue("top_speed_gear", out var topGear))
            {
                if (!int.TryParse(topGear.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var g) || g < 1 || g > gears)
                    issues.Add(new VehicleTsvIssue(VehicleTsvIssueSeverity.Error, topGear.Line, Localized("top_speed_gear must be within 1..{0}.", gears)));
            }

            ValidateOptionalPolicyFloat(policy, "base_auto_shift_cooldown", 0f, 2f, issues);
            ValidateOptionalPolicyFloat(policy, "upshift_delay_default", 0f, 2f, issues);
            ValidateOptionalPolicyFloat(policy, "auto_upshift_rpm_fraction", 0.05f, 1.0f, issues);
            ValidateOptionalPolicyFloat(policy, "auto_downshift_rpm_fraction", 0.05f, 0.95f, issues);
            ValidateOptionalPolicyFloat(policy, "top_speed_pursuit_speed_fraction", 0.50f, 1.20f, issues);
            ValidateOptionalPolicyFloat(policy, "upshift_hysteresis", 0f, 2f, issues);
            ValidateOptionalPolicyFloat(policy, "min_upshift_net_accel_mps2", -20f, 20f, issues);

            if (policy.Entries.TryGetValue("auto_upshift_rpm", out var upAbs))
            {
                if (!TryParseFloat(upAbs.Value, out var rpm) || rpm < 0f || (rpm > 0f && (rpm < idleRpm || rpm > revLimiter)))
                    issues.Add(new VehicleTsvIssue(VehicleTsvIssueSeverity.Error, upAbs.Line, Localized("auto_upshift_rpm must be 0 or between idle_rpm and rev_limiter.")));
            }

            if (policy.Entries.TryGetValue("auto_downshift_rpm", out var downAbs))
            {
                if (!TryParseFloat(downAbs.Value, out var rpm) || rpm < 0f || (rpm > 0f && (rpm < idleRpm || rpm > revLimiter)))
                    issues.Add(new VehicleTsvIssue(VehicleTsvIssueSeverity.Error, downAbs.Line, Localized("auto_downshift_rpm must be 0 or between idle_rpm and rev_limiter.")));
            }

            foreach (var kvp in policy.Entries)
            {
                if (!kvp.Key.StartsWith("upshift_delay_", StringComparison.OrdinalIgnoreCase))
                    continue;

                if (!string.Equals(kvp.Key, "upshift_delay_default", StringComparison.OrdinalIgnoreCase))
                {
                    if (kvp.Key.StartsWith("upshift_delay_g", StringComparison.OrdinalIgnoreCase))
                    {
                        var rawGear = kvp.Key.Substring("upshift_delay_g".Length);
                        if (!int.TryParse(rawGear, NumberStyles.Integer, CultureInfo.InvariantCulture, out var sourceGear) ||
                            sourceGear < 1 || sourceGear > gears)
                        {
                            issues.Add(new VehicleTsvIssue(VehicleTsvIssueSeverity.Error, kvp.Value.Line, Localized("Invalid key '{0}'. Source gear must be within 1..{1}.", kvp.Key, gears)));
                        }
                    }
                    else
                    {
                        var suffix = kvp.Key.Substring("upshift_delay_".Length);
                        var parts = suffix.Split('_');
                        if (parts.Length != 2 ||
                            !int.TryParse(parts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out var g1) ||
                            !int.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var g2) ||
                            g1 < 1 || g1 > gears || g2 != g1 + 1 || g2 > gears)
                        {
                            issues.Add(new VehicleTsvIssue(VehicleTsvIssueSeverity.Error, kvp.Value.Line, Localized("Invalid key '{0}'. Use upshift_delay_X_Y for adjacent gears.", kvp.Key)));
                        }
                    }
                }

                if (!TryParseFloat(kvp.Value.Value, out var delay) || delay < 0f || delay > 2f)
                    issues.Add(new VehicleTsvIssue(VehicleTsvIssueSeverity.Error, kvp.Value.Line, Localized("{0} must be a float between 0 and 2 seconds.", kvp.Key)));
            }

            return !HasErrors(issues);
        }

        private static void ValidateOptionalPolicyFloat(Section policy, string key, float min, float max, List<VehicleTsvIssue> issues)
        {
            if (!policy.Entries.TryGetValue(key, out var entry))
                return;
            if (!TryParseFloat(entry.Value, out var value) || value < min || value > max)
            {
                issues.Add(new VehicleTsvIssue(VehicleTsvIssueSeverity.Error, entry.Line,
                    Localized(
                        "{0} must be between {1} and {2}.",
                        key,
                        min.ToString(CultureInfo.InvariantCulture),
                        max.ToString(CultureInfo.InvariantCulture))));
            }
        }
    }
}

