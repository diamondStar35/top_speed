using System.Collections.Generic;

namespace TopSpeed.Vehicles.Parsing
{
    public static partial class VehicleTsvParser
    {
        private static AutomaticDrivelineTuning BuildAutomaticTuning(
            Section transmission,
            Section? transmissionAtc,
            Section? transmissionDct,
            Section? transmissionCvt,
            IReadOnlyList<TransmissionType> supportedTypes,
            float idleRpm,
            float revLimiter,
            List<VehicleTsvIssue> issues)
        {
            var atc = BuildAtcTuning(transmission, transmissionAtc, supportedTypes, issues);
            var dct = BuildDctTuning(transmission, transmissionDct, supportedTypes, issues);
            var cvt = BuildCvtTuning(transmission, transmissionCvt, supportedTypes, idleRpm, revLimiter, issues);
            return new AutomaticDrivelineTuning(atc, dct, cvt);
        }
    }
}

