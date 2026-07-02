using System.Collections.Generic;

namespace TopSpeed.Vehicles.Parsing
{
    public static partial class VehicleTsvParser
    {
        private static ParsedSections ParseSections(Dictionary<string, Section> sections)
        {
            sections.TryGetValue("transmission_atc", out var transmissionAtc);
            sections.TryGetValue("transmission_dct", out var transmissionDct);
            sections.TryGetValue("transmission_cvt", out var transmissionCvt);
            sections.TryGetValue("policy", out var policy);

            return new ParsedSections
            {
                Meta = sections["meta"],
                Sounds = sections["sounds"],
                General = sections["general"],
                Engine = sections["engine"],
                Torque = sections["torque"],
                EngineRot = sections["engine_rot"],
                Resistance = sections["resistance"],
                TorqueCurve = sections["torque_curve"],
                Transmission = sections["transmission"],
                TransmissionAtc = transmissionAtc,
                TransmissionDct = transmissionDct,
                TransmissionCvt = transmissionCvt,
                Drivetrain = sections["drivetrain"],
                Gears = sections["gears"],
                Steering = sections["steering"],
                TireModel = sections["tire_model"],
                Dynamics = sections["dynamics"],
                Dimensions = sections["dimensions"],
                Tires = sections["tires"],
                Policy = policy
            };
        }

        private static ParsedValues ParseValues(ParsedSections sections, List<VehicleTsvIssue> issues)
        {
            var values = new ParsedValues();

            ParseMetaValues(sections.Meta, values, issues);
            ParseSoundValues(sections.Sounds, values, issues);
            ParseGeneralValues(sections.General, values, issues);
            ParseGearValues(sections.Gears, values, issues);
            ParseEngineValues(sections.Engine, values, issues);
            ParseTorqueValues(sections.Torque, values, issues);
            ParseEngineRotValues(sections.EngineRot, values, issues);
            ParseResistanceValues(sections.Resistance, values, issues);
            ParseDrivetrainValues(sections.Drivetrain, values, issues);
            ParseSteeringValues(sections.Steering, values, issues);
            ParseTireModelValues(sections.TireModel, values, issues);
            ParseDynamicsValues(sections.Dynamics, values, issues);
            ParseDimensionValues(sections.Dimensions, values, issues);
            ParseTireInputValues(sections.Tires, values, issues);
            ParseTransmissionValues(sections, values, issues);

            return values;
        }
    }
}

