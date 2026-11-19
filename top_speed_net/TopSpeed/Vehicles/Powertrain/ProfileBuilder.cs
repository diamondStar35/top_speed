using TopSpeed.Physics.Powertrain;
using TopSpeed.Physics.Torque;

namespace TopSpeed.Vehicles
{
    internal static class PowertrainProfileBuilder
    {
        public static CurveProfile Build(VehicleDefinition definition)
        {
            if (definition.TorqueCurveRpm != null
                && definition.TorqueCurveTorqueNm != null
                && definition.TorqueCurveRpm.Length >= 2
                && definition.TorqueCurveRpm.Length == definition.TorqueCurveTorqueNm.Length)
            {
                var points = new CurvePoint[definition.TorqueCurveRpm.Length];
                for (var i = 0; i < points.Length; i++)
                    points[i] = new CurvePoint(definition.TorqueCurveRpm[i], definition.TorqueCurveTorqueNm[i]);

                return CurveFactory.FromPoints(
                    points,
                    definition.IdleRpm,
                    definition.RevLimiter,
                    definition.PeakTorqueRpm,
                    definition.IdleTorqueNm,
                    definition.PeakTorqueNm,
                    definition.RedlineTorqueNm);
            }

            if (!string.IsNullOrWhiteSpace(definition.TorqueCurvePreset)
                && PresetCatalog.TryNormalize(definition.TorqueCurvePreset, out var presetName))
            {
                var presetPoints = CurveFactory.BuildPreset(
                    presetName,
                    definition.IdleRpm,
                    definition.RevLimiter,
                    definition.PeakTorqueRpm,
                    definition.IdleTorqueNm,
                    definition.PeakTorqueNm,
                    definition.RedlineTorqueNm);
                return CurveFactory.FromPoints(
                    presetPoints,
                    definition.IdleRpm,
                    definition.RevLimiter,
                    definition.PeakTorqueRpm,
                    definition.IdleTorqueNm,
                    definition.PeakTorqueNm,
                    definition.RedlineTorqueNm);
            }

            return CurveFactory.FromLegacy(
                definition.IdleRpm,
                definition.RevLimiter,
                definition.PeakTorqueRpm,
                definition.IdleTorqueNm,
                definition.PeakTorqueNm,
                definition.RedlineTorqueNm);
        }
    }
}



