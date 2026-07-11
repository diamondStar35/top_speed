using System.Collections.Generic;
using TopSpeed.Vehicles;

namespace TopSpeed.Vehicles.Parsing
{
    internal static partial class VehicleTsvParser
    {
        private static void ValidateResolvedValues(
            ParsedSections sections,
            ParsedValues values,
            List<VehicleTsvIssue> issues)
        {
            ValidateGearRatios(sections.Gears, values.GearCount, values.GearRatios, issues);
            ValidateTransmissionTypes(
                sections.Transmission,
                values.PrimaryTransmissionType,
                values.SupportedTransmissionTypes,
                issues);
            ValidateShiftOnDemandWarnings(
                sections.Transmission,
                values.SupportedTransmissionTypes,
                values.ShiftOnDemand,
                issues);

            if (values.MaxRpm < values.IdleRpm
                && TryEntryLine(sections.Engine, "max_rpm", out var maxRpmLine))
            {
                issues.Add(new VehicleTsvIssue(
                    VehicleTsvIssueSeverity.Error,
                    maxRpmLine,
                    Localized("max_rpm must be greater than or equal to idle_rpm.")));
            }

            if ((values.RevLimiter < values.IdleRpm || values.RevLimiter > values.MaxRpm)
                && TryEntryLine(sections.Engine, "rev_limiter", out var revLimiterLine))
            {
                issues.Add(new VehicleTsvIssue(
                    VehicleTsvIssueSeverity.Error,
                    revLimiterLine,
                    Localized("rev_limiter must be between idle_rpm and max_rpm.")));
            }

            if (values.AutoShiftRpm > 0f
                && (values.AutoShiftRpm < values.IdleRpm || values.AutoShiftRpm > values.RevLimiter)
                && TryEntryLine(sections.Engine, "auto_shift_rpm", out var autoShiftRpmLine))
            {
                issues.Add(new VehicleTsvIssue(
                    VehicleTsvIssueSeverity.Error,
                    autoShiftRpmLine,
                    Localized("auto_shift_rpm must be 0 or between idle_rpm and rev_limiter.")));
            }

            if ((values.PeakTorqueRpm < values.IdleRpm || values.PeakTorqueRpm > values.RevLimiter)
                && TryEntryLine(sections.Torque, "peak_torque_rpm", out var peakTorqueRpmLine))
            {
                issues.Add(new VehicleTsvIssue(
                    VehicleTsvIssueSeverity.Error,
                    peakTorqueRpmLine,
                    Localized("peak_torque_rpm must be between idle_rpm and rev_limiter.")));
            }

            if (values.LaunchRpm > values.RevLimiter
                && TryEntryLine(sections.Engine, "launch_rpm", out var launchRpmLine))
            {
                issues.Add(new VehicleTsvIssue(
                    VehicleTsvIssueSeverity.Error,
                    launchRpmLine,
                    Localized("launch_rpm must not exceed rev_limiter.")));
            }

            if (values.TopFreq < values.IdleFreq
                && TryEntryLine(sections.Sounds, "top_freq", out var topFreqLine))
            {
                issues.Add(new VehicleTsvIssue(
                    VehicleTsvIssueSeverity.Error,
                    topFreqLine,
                    Localized("top_freq must be greater than or equal to idle_freq.")));
            }

            if ((values.ShiftFreq < values.IdleFreq || values.ShiftFreq > values.TopFreq)
                && TryEntryLine(sections.Sounds, "shift_freq", out var shiftFreqLine))
            {
                issues.Add(new VehicleTsvIssue(
                    VehicleTsvIssueSeverity.Error,
                    shiftFreqLine,
                    Localized("shift_freq must be between idle_freq and top_freq.")));
            }

            if ((float.IsNaN(values.PitchCurveExponent)
                || float.IsInfinity(values.PitchCurveExponent)
                || values.PitchCurveExponent < VehicleDefinition.PitchCurveExponentMin
                || values.PitchCurveExponent > VehicleDefinition.PitchCurveExponentMax)
                && TryEntryLine(sections.Sounds, "pitch_curve_exponent", out var pitchCurveLine))
            {
                issues.Add(new VehicleTsvIssue(
                    VehicleTsvIssueSeverity.Error,
                    pitchCurveLine,
                    Localized(
                        "pitch_curve_exponent must be between {0} and {1}.",
                        VehicleDefinition.PitchCurveExponentMin,
                        VehicleDefinition.PitchCurveExponentMax)));
            }

            if (values.HighSpeedSteerFullKph <= values.HighSpeedSteerStartKph
                && TryEntryLine(sections.Steering, "high_speed_steer_full_kph", out var highSpeedSteerLine))
            {
                issues.Add(new VehicleTsvIssue(
                    VehicleTsvIssueSeverity.Error,
                    highSpeedSteerLine,
                    Localized("high_speed_steer_full_kph must be greater than high_speed_steer_start_kph.")));
            }
        }

        // Resolves the source line for a key so a cross-field validation error can point at it.
        // Returns false when the key is absent: in that case RequireFloatRange/RequireIntRange has
        // already recorded a "missing required key" error (and optional keys fall back to a valid
        // default), so the cross-field check is skipped rather than indexing a missing entry and
        // throwing KeyNotFoundException.
        private static bool TryEntryLine(Section section, string key, out int line)
        {
            if (section.Entries.TryGetValue(key, out var entry))
            {
                line = entry.Line;
                return true;
            }

            line = 0;
            return false;
        }
    }
}
