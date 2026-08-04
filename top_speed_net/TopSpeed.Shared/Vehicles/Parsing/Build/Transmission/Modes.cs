using System.Collections.Generic;

namespace TopSpeed.Vehicles.Parsing
{
    public static partial class VehicleTsvParser
    {
        private static AtcDrivelineTuning BuildAtcTuning(
            Section transmission,
            Section? transmissionAtc,
            IReadOnlyList<TransmissionType> supportedTypes,
            List<VehicleTsvIssue> issues)
        {
            var supportsAtc = SupportsTransmissionType(supportedTypes, TransmissionType.Atc);
            if (!supportsAtc)
            {
                if (transmissionAtc != null)
                {
                    AddTransmissionWarning(
                        issues,
                        transmissionAtc,
                        Localized("Section [transmission_atc] is unused because supported_types does not include 'atc'."));
                }

                return AutomaticDrivelineTuning.Default.Atc;
            }

            if (transmissionAtc == null)
            {
                AddTransmissionIssue(
                    issues,
                    transmission,
                    key: null,
                    Localized("Missing required section [transmission_atc] for supported type 'atc'."));
                return AutomaticDrivelineTuning.Default.Atc;
            }

            var atc = new AtcDrivelineTuning(
                creepAccelKphPerSecond: RequireFloatRange(transmissionAtc, "creep_accel_kphps", 0f, 12f, issues),
                launchCouplingMin: RequireFloatRange(transmissionAtc, "launch_coupling_min", 0f, 1f, issues),
                launchCouplingMax: RequireFloatRange(transmissionAtc, "launch_coupling_max", 0f, 1f, issues),
                lockSpeedKph: RequireFloatRange(transmissionAtc, "lock_speed_kph", 2f, 300f, issues),
                lockThrottleMin: RequireFloatRange(transmissionAtc, "lock_throttle_min", 0f, 1f, issues),
                shiftReleaseCoupling: RequireFloatRange(transmissionAtc, "shift_release_coupling", 0f, 1f, issues),
                engageRate: RequireFloatRange(transmissionAtc, "engage_rate", 0.1f, 80f, issues),
                disengageRate: RequireFloatRange(transmissionAtc, "disengage_rate", 0.1f, 80f, issues));

            if (atc.LaunchCouplingMin > atc.LaunchCouplingMax)
            {
                AddTransmissionIssue(
                    issues,
                    transmissionAtc,
                    "launch_coupling_max",
                    Localized("launch_coupling_max must be greater than or equal to launch_coupling_min in [transmission_atc]."));
            }

            return atc;
        }

        private static DctDrivelineTuning BuildDctTuning(
            Section transmission,
            Section? transmissionDct,
            IReadOnlyList<TransmissionType> supportedTypes,
            List<VehicleTsvIssue> issues)
        {
            var supportsDct = SupportsTransmissionType(supportedTypes, TransmissionType.Dct);
            if (!supportsDct)
            {
                if (transmissionDct != null)
                {
                    AddTransmissionWarning(
                        issues,
                        transmissionDct,
                        Localized("Section [transmission_dct] is unused because supported_types does not include 'dct'."));
                }

                return AutomaticDrivelineTuning.Default.Dct;
            }

            if (transmissionDct == null)
            {
                AddTransmissionIssue(
                    issues,
                    transmission,
                    key: null,
                    Localized("Missing required section [transmission_dct] for supported type 'dct'."));
                return AutomaticDrivelineTuning.Default.Dct;
            }

            var dct = new DctDrivelineTuning(
                launchCouplingMin: RequireFloatRange(transmissionDct, "launch_coupling_min", 0f, 1f, issues),
                launchCouplingMax: RequireFloatRange(transmissionDct, "launch_coupling_max", 0f, 1f, issues),
                lockSpeedKph: RequireFloatRange(transmissionDct, "lock_speed_kph", 2f, 300f, issues),
                lockThrottleMin: RequireFloatRange(transmissionDct, "lock_throttle_min", 0f, 1f, issues),
                shiftOverlapCoupling: RequireFloatRange(transmissionDct, "shift_overlap_coupling", 0f, 1f, issues),
                engageRate: RequireFloatRange(transmissionDct, "engage_rate", 0.1f, 80f, issues),
                disengageRate: RequireFloatRange(transmissionDct, "disengage_rate", 0.1f, 80f, issues));

            if (dct.LaunchCouplingMin > dct.LaunchCouplingMax)
            {
                AddTransmissionIssue(
                    issues,
                    transmissionDct,
                    "launch_coupling_max",
                    Localized("launch_coupling_max must be greater than or equal to launch_coupling_min in [transmission_dct]."));
            }

            return dct;
        }

        private static CvtDrivelineTuning BuildCvtTuning(
            Section transmission,
            Section? transmissionCvt,
            IReadOnlyList<TransmissionType> supportedTypes,
            float idleRpm,
            float revLimiter,
            List<VehicleTsvIssue> issues)
        {
            var supportsCvt = SupportsTransmissionType(supportedTypes, TransmissionType.Cvt);
            if (!supportsCvt)
            {
                if (transmissionCvt != null)
                {
                    AddTransmissionWarning(
                        issues,
                        transmissionCvt,
                        Localized("Section [transmission_cvt] is unused because supported_types does not include 'cvt'."));
                }

                return AutomaticDrivelineTuning.Default.Cvt;
            }

            if (transmissionCvt == null)
            {
                AddTransmissionIssue(
                    issues,
                    transmission,
                    key: null,
                    Localized("Missing required section [transmission_cvt] for supported type 'cvt'."));
                return AutomaticDrivelineTuning.Default.Cvt;
            }

            var cvt = new CvtDrivelineTuning(
                ratioMin: RequireFloatRange(transmissionCvt, "ratio_min", 0.1f, 8f, issues),
                ratioMax: RequireFloatRange(transmissionCvt, "ratio_max", 0.2f, 10f, issues),
                targetRpmLow: RequireFloatRange(transmissionCvt, "target_rpm_low", idleRpm, revLimiter, issues),
                targetRpmHigh: RequireFloatRange(transmissionCvt, "target_rpm_high", idleRpm, revLimiter, issues),
                ratioChangeRate: RequireFloatRange(transmissionCvt, "ratio_change_rate", 0.1f, 20f, issues),
                launchCouplingMin: RequireFloatRange(transmissionCvt, "launch_coupling_min", 0f, 1f, issues),
                launchCouplingMax: RequireFloatRange(transmissionCvt, "launch_coupling_max", 0f, 1f, issues),
                lockSpeedKph: RequireFloatRange(transmissionCvt, "lock_speed_kph", 2f, 300f, issues),
                lockThrottleMin: RequireFloatRange(transmissionCvt, "lock_throttle_min", 0f, 1f, issues),
                creepAccelKphPerSecond: RequireFloatRange(transmissionCvt, "creep_accel_kphps", 0f, 12f, issues),
                shiftHoldCoupling: RequireFloatRange(transmissionCvt, "shift_hold_coupling", 0f, 1f, issues),
                engageRate: RequireFloatRange(transmissionCvt, "engage_rate", 0.1f, 80f, issues),
                disengageRate: RequireFloatRange(transmissionCvt, "disengage_rate", 0.1f, 80f, issues));

            if (cvt.RatioMax < cvt.RatioMin)
            {
                AddTransmissionIssue(
                    issues,
                    transmissionCvt,
                    "ratio_max",
                    Localized("ratio_max must be greater than or equal to ratio_min in [transmission_cvt]."));
            }

            if (cvt.TargetRpmHigh < cvt.TargetRpmLow)
            {
                AddTransmissionIssue(
                    issues,
                    transmissionCvt,
                    "target_rpm_high",
                    Localized("target_rpm_high must be greater than or equal to target_rpm_low in [transmission_cvt]."));
            }

            if (cvt.LaunchCouplingMin > cvt.LaunchCouplingMax)
            {
                AddTransmissionIssue(
                    issues,
                    transmissionCvt,
                    "launch_coupling_max",
                    Localized("launch_coupling_max must be greater than or equal to launch_coupling_min in [transmission_cvt]."));
            }

            return cvt;
        }
    }
}

