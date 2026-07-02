using System.Collections.Generic;

namespace TopSpeed.Vehicles.Parsing
{
    public static partial class VehicleTsvParser
    {
        private static void ParseTransmissionValues(ParsedSections sections, ParsedValues values, List<VehicleTsvIssue> issues)
        {
            values.PrimaryTransmissionType = RequireTransmissionType(sections.Transmission, "primary_type", issues);
            values.SupportedTransmissionTypes = RequireTransmissionTypes(sections.Transmission, "supported_types", issues);
            values.ShiftOnDemand = OptionalBoolInt(sections.Transmission, "shift_on_demand", issues) ?? false;

            values.AutomaticTuning = BuildAutomaticTuning(
                sections.Transmission,
                sections.TransmissionAtc,
                sections.TransmissionDct,
                sections.TransmissionCvt,
                values.SupportedTransmissionTypes,
                values.IdleRpm,
                values.RevLimiter,
                issues);
        }
    }
}

