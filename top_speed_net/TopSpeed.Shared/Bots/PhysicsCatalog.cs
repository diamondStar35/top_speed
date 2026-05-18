using System;
using TopSpeed.Physics.Powertrain;
using TopSpeed.Physics.Torque;
using TopSpeed.Protocol;
using TopSpeed.Vehicles;

namespace TopSpeed.Bots
{
    public static class BotPhysicsCatalog
    {
        public static BotPhysicsConfig Get(CarType car)
        {
            if (car == CarType.CustomVehicle)
                car = CarType.Vehicle1;

            var index = (int)car;
            var spec = OfficialVehicleCatalog.Get(index);
            return Create(spec);
        }

        private static BotPhysicsConfig Create(OfficialVehicleSpec spec)
        {
            var wheelRadiusM = Math.Max(0.01f, spec.TireCircumferenceM / (2.0f * (float)Math.PI));
            var torqueCurve = BuildTorqueCurve(spec);

            return new BotPhysicsConfig(
                spec.SurfaceTractionFactor,
                spec.TopSpeed,
                spec.MassKg,
                spec.DrivetrainEfficiency,
                spec.EngineBrakingTorqueNm,
                spec.TireGripCoefficient,
                spec.BrakeStrength,
                wheelRadiusM,
                spec.EngineBraking,
                spec.IdleRpm,
                spec.RevLimiter,
                spec.FinalDriveRatio,
                spec.PowerFactor,
                spec.PeakTorqueNm,
                spec.PeakTorqueRpm,
                spec.IdleTorqueNm,
                spec.RedlineTorqueNm,
                spec.DragCoefficient,
                spec.FrontalAreaM2,
                spec.SideAreaM2,
                spec.RollingResistanceCoefficient,
                spec.RollingResistanceSpeedFactor,
                spec.WheelSideDragBaseN,
                spec.WheelSideDragLinearNPerMps,
                spec.LaunchRpm,
                spec.ReversePowerFactor,
                spec.ReverseGearRatio,
                spec.EngineInertiaKgm2,
                spec.EngineFrictionTorqueNm,
                spec.DrivelineCouplingRate,
                spec.LateralGripCoefficient,
                spec.HighSpeedStability,
                spec.WheelbaseM,
                spec.WidthM,
                spec.LengthM,
                spec.MaxSteerDeg,
                spec.Steering,
                spec.HighSpeedSteerGain,
                spec.HighSpeedSteerStartKph,
                spec.HighSpeedSteerFullKph,
                spec.CombinedGripPenalty,
                spec.SlipAnglePeakDeg,
                spec.SlipAngleFalloff,
                spec.TurnResponse,
                spec.MassSensitivity,
                spec.DownforceGripGain,
                spec.CornerStiffnessFront,
                spec.CornerStiffnessRear,
                spec.YawInertiaScale,
                spec.SteeringCurve,
                spec.TransientDamping,
                spec.Gears,
                torqueCurve,
                spec.GearRatios,
                spec.TransmissionPolicy,
                spec.PrimaryTransmissionType,
                spec.AutomaticTuning,
                coupledDrivelineDragNm: spec.CoupledDrivelineDragNm,
                coupledDrivelineViscousDragNmPerKrpm: spec.CoupledDrivelineViscousDragNmPerKrpm,
                frictionLinearNmPerKrpm: spec.FrictionLinearNmPerKrpm,
                frictionQuadraticNmPerKrpm2: spec.FrictionQuadraticNmPerKrpm2,
                idleControlWindowRpm: spec.IdleControlWindowRpm,
                idleControlGainNmPerRpm: spec.IdleControlGainNmPerRpm,
                minCoupledRiseIdleRpmPerSecond: spec.MinCoupledRiseIdleRpmPerSecond,
                minCoupledRiseFullRpmPerSecond: spec.MinCoupledRiseFullRpmPerSecond,
                engineOverrunIdleLossFraction: spec.EngineOverrunIdleLossFraction,
                overrunCurveExponent: spec.OverrunCurveExponent,
                engineBrakeTransferEfficiency: spec.EngineBrakeTransferEfficiency,
                tireWearConfig: spec.TireWearConfig);
        }

        private static CurveProfile BuildTorqueCurve(OfficialVehicleSpec spec)
        {
            if (spec.TorqueCurveRpm != null
                && spec.TorqueCurveTorqueNm != null
                && spec.TorqueCurveRpm.Length >= 2
                && spec.TorqueCurveRpm.Length == spec.TorqueCurveTorqueNm.Length)
            {
                var points = new CurvePoint[spec.TorqueCurveRpm.Length];
                for (var i = 0; i < points.Length; i++)
                    points[i] = new CurvePoint(spec.TorqueCurveRpm[i], spec.TorqueCurveTorqueNm[i]);
                return CurveFactory.FromPoints(
                    points,
                    spec.IdleRpm,
                    spec.RevLimiter,
                    spec.PeakTorqueRpm,
                    spec.IdleTorqueNm,
                    spec.PeakTorqueNm,
                    spec.RedlineTorqueNm);
            }

            if (!string.IsNullOrWhiteSpace(spec.TorqueCurvePreset)
                && PresetCatalog.TryNormalize(spec.TorqueCurvePreset, out var presetName))
            {
                var presetPoints = CurveFactory.BuildPreset(
                    presetName,
                    spec.IdleRpm,
                    spec.RevLimiter,
                    spec.PeakTorqueRpm,
                    spec.IdleTorqueNm,
                    spec.PeakTorqueNm,
                    spec.RedlineTorqueNm);
                return CurveFactory.FromPoints(
                    presetPoints,
                    spec.IdleRpm,
                    spec.RevLimiter,
                    spec.PeakTorqueRpm,
                    spec.IdleTorqueNm,
                    spec.PeakTorqueNm,
                    spec.RedlineTorqueNm);
            }

            return CurveFactory.FromLegacy(
                spec.IdleRpm,
                spec.RevLimiter,
                spec.PeakTorqueRpm,
                spec.IdleTorqueNm,
                spec.PeakTorqueNm,
                spec.RedlineTorqueNm);
        }
    }
}


