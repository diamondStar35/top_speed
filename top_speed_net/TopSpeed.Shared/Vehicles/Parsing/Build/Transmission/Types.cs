using System;
using System.Collections.Generic;

namespace TopSpeed.Vehicles.Parsing
{
    public static partial class VehicleTsvParser
    {
        private static TransmissionType RequireTransmissionType(Section section, string key, List<VehicleTsvIssue> issues)
        {
            if (!section.Entries.TryGetValue(key, out var entry))
            {
                issues.Add(new VehicleTsvIssue(
                    VehicleTsvIssueSeverity.Error,
                    section.Line,
                    Localized("Missing required key '{0}' in section [{1}].", key, section.Name)));
                return TransmissionType.Atc;
            }

            var raw = entry.Value.Trim();
            if (!TransmissionTypes.TryParse(raw, out var type))
            {
                issues.Add(new VehicleTsvIssue(
                    VehicleTsvIssueSeverity.Error,
                    entry.Line,
                    Localized("Unsupported transmission type '{0}'. Valid values: atc, cvt, dct, manual.", raw)));
                return TransmissionType.Atc;
            }

            return type;
        }

        private static IReadOnlyList<TransmissionType> RequireTransmissionTypes(Section section, string key, List<VehicleTsvIssue> issues)
        {
            if (!section.Entries.TryGetValue(key, out var entry))
            {
                issues.Add(new VehicleTsvIssue(
                    VehicleTsvIssueSeverity.Error,
                    section.Line,
                    Localized("Missing required key '{0}' in section [{1}].", key, section.Name)));
                return Array.Empty<TransmissionType>();
            }

            var tokens = entry.Value.Split(',');
            var values = new List<TransmissionType>(tokens.Length);
            for (var i = 0; i < tokens.Length; i++)
            {
                var token = tokens[i].Trim();
                if (token.Length == 0)
                    continue;

                if (!TransmissionTypes.TryParse(token, out var type))
                {
                    issues.Add(new VehicleTsvIssue(
                        VehicleTsvIssueSeverity.Error,
                        entry.Line,
                        Localized("Unsupported transmission type '{0}' in supported_types. Valid values: atc, cvt, dct, manual.", token)));
                    continue;
                }

                values.Add(type);
            }

            if (values.Count == 0)
            {
                issues.Add(new VehicleTsvIssue(
                    VehicleTsvIssueSeverity.Error,
                    entry.Line,
                    Localized("Key '{0}' must contain at least one transmission type.", key)));
            }

            return values;
        }

        private static void ValidateTransmissionTypes(
            Section transmissionSection,
            TransmissionType primaryType,
            IReadOnlyList<TransmissionType> supportedTypes,
            List<VehicleTsvIssue> issues)
        {
            if (!transmissionSection.Entries.ContainsKey("primary_type")
                || !transmissionSection.Entries.ContainsKey("supported_types"))
            {
                return;
            }

            var line = transmissionSection.Line;
            if (transmissionSection.Entries.TryGetValue("supported_types", out var supportedEntry))
                line = supportedEntry.Line;
            else if (transmissionSection.Entries.TryGetValue("primary_type", out var primaryEntry))
                line = primaryEntry.Line;

            if (!TransmissionTypes.TryValidate(primaryType, supportedTypes, out var validationError))
            {
                issues.Add(new VehicleTsvIssue(
                    VehicleTsvIssueSeverity.Error,
                    line,
                    Localized(validationError)));
            }
        }
    }
}

