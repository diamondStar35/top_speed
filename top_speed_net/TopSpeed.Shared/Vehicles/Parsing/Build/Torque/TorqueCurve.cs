using System;
using System.Collections.Generic;
using TopSpeed.Physics.Torque;

namespace TopSpeed.Vehicles.Parsing
{
    public static partial class VehicleTsvParser
    {
        private static bool TryBuildTorqueCurve(
            Section torqueCurveSection,
            float idleRpm,
            float revLimiter,
            float peakTorqueRpm,
            float idleTorque,
            float peakTorque,
            float redlineTorque,
            List<VehicleTsvIssue> issues,
            out string? presetName,
            out float[] rpmPoints,
            out float[] torquePoints)
        {
            presetName = null;
            var merged = new SortedDictionary<float, float>();

            if (torqueCurveSection.Entries.TryGetValue("preset", out var presetEntry))
            {
                var rawPreset = presetEntry.Value.Trim();
                if (!PresetCatalog.TryNormalize(rawPreset, out var normalizedPreset))
                {
                    issues.Add(new VehicleTsvIssue(
                        VehicleTsvIssueSeverity.Error,
                        presetEntry.Line,
                        Localized("Unknown torque curve preset '{0}'. Valid values: {1}.", rawPreset, PresetCatalog.NamesText)));
                    rpmPoints = Array.Empty<float>();
                    torquePoints = Array.Empty<float>();
                    return false;
                }

                presetName = normalizedPreset;
                var presetPoints = CurveFactory.BuildPreset(
                    normalizedPreset,
                    idleRpm,
                    revLimiter,
                    peakTorqueRpm,
                    idleTorque,
                    peakTorque,
                    redlineTorque);
                for (var i = 0; i < presetPoints.Count; i++)
                    merged[presetPoints[i].Rpm] = presetPoints[i].TorqueNm;
            }

            foreach (var entryPair in torqueCurveSection.Entries)
            {
                if (string.Equals(entryPair.Key, "preset", StringComparison.OrdinalIgnoreCase))
                    continue;

                if (!TryParseRpmKey(entryPair.Key, out var rpm))
                {
                    issues.Add(new VehicleTsvIssue(
                        VehicleTsvIssueSeverity.Error,
                        entryPair.Value.Line,
                        Localized("Invalid torque curve key '{0}'. Use format like 2000rpm=200.", entryPair.Key)));
                    continue;
                }

                if (!TryParseFloat(entryPair.Value.Value, out var torqueNm))
                {
                    issues.Add(new VehicleTsvIssue(
                        VehicleTsvIssueSeverity.Error,
                        entryPair.Value.Line,
                        Localized("Invalid torque value '{0}' for '{1}'.", entryPair.Value.Value, entryPair.Key)));
                    continue;
                }

                if (rpm < 300f || rpm > 25000f)
                {
                    issues.Add(new VehicleTsvIssue(
                        VehicleTsvIssueSeverity.Error,
                        entryPair.Value.Line,
                        Localized("Torque curve RPM '{0:F0}' must be between 300 and 25000.", rpm)));
                    continue;
                }

                if (torqueNm < 0f || torqueNm > 5000f)
                {
                    issues.Add(new VehicleTsvIssue(
                        VehicleTsvIssueSeverity.Error,
                        entryPair.Value.Line,
                        Localized("Torque value '{0:F1}' must be between 0 and 5000 Nm.", torqueNm)));
                    continue;
                }

                merged[rpm] = torqueNm;
            }

            if (HasErrors(issues))
            {
                rpmPoints = Array.Empty<float>();
                torquePoints = Array.Empty<float>();
                return false;
            }

            if (merged.Count < 2)
            {
                issues.Add(new VehicleTsvIssue(
                    VehicleTsvIssueSeverity.Error,
                    torqueCurveSection.Line,
                    Localized("Section [torque_curve] must define at least two RPM points, or a preset with enough points.")));
                rpmPoints = Array.Empty<float>();
                torquePoints = Array.Empty<float>();
                return false;
            }

            rpmPoints = new float[merged.Count];
            torquePoints = new float[merged.Count];
            var index = 0;
            foreach (var point in merged)
            {
                rpmPoints[index] = point.Key;
                torquePoints[index] = point.Value;
                index++;
            }

            return true;
        }

        private static bool TryParseRpmKey(string key, out float rpm)
        {
            rpm = 0f;
            if (string.IsNullOrWhiteSpace(key))
                return false;

            var trimmed = key.Trim();
            if (!trimmed.EndsWith("rpm", StringComparison.OrdinalIgnoreCase))
                return false;

            var numberPart = trimmed.Substring(0, trimmed.Length - 3);
            return TryParseFloat(numberPart, out rpm);
        }
    }
}

