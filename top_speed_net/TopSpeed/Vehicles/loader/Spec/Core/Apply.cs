using System;

namespace TopSpeed.Vehicles.Loader
{
    internal static partial class Spec
    {
        public static void Apply(VehicleDefinition def, Common spec)
        {
            def.SurfaceTractionFactor = spec.SurfaceTractionFactor;
            def.TopSpeed = spec.TopSpeed;
            def.IdleFreq = spec.IdleFreq;
            def.TopFreq = spec.TopFreq;
            def.ShiftFreq = spec.ShiftFreq;
            def.PitchCurveExponent = spec.PitchCurveExponent;
            def.Gears = spec.Gears;
            def.Steering = spec.Steering;
            def.PrimaryTransmissionType = spec.PrimaryTransmissionType;
            def.SupportedTransmissionTypes = spec.SupportedTransmissionTypes == null
                ? Array.Empty<TransmissionType>()
                : (TransmissionType[])spec.SupportedTransmissionTypes.Clone();
            def.ShiftOnDemand = spec.ShiftOnDemand;
            def.AutomaticTuning = spec.AutomaticTuning;
            def.HasWipers = spec.HasWipers;
            def.IdleRpm = spec.IdleRpm;
            def.MaxRpm = spec.MaxRpm;
            def.RevLimiter = spec.RevLimiter;
            def.AutoShiftRpm = spec.AutoShiftRpm;
            def.EngineBraking = spec.EngineBraking;
            def.FuelTankCapacityLiters = spec.FuelTankCapacityLiters;
            def.EngineDisplacementLiters = spec.EngineDisplacementLiters;
            def.MassKg = spec.MassKg;
            def.DrivetrainEfficiency = spec.DrivetrainEfficiency;
            def.EngineBrakingTorqueNm = spec.EngineBrakingTorqueNm;
            def.TireGripCoefficient = spec.TireGripCoefficient;
            def.PeakTorqueNm = spec.PeakTorqueNm;
            def.PeakTorqueRpm = spec.PeakTorqueRpm;
            def.IdleTorqueNm = spec.IdleTorqueNm;
            def.RedlineTorqueNm = spec.RedlineTorqueNm;
            def.DragCoefficient = spec.DragCoefficient;
            def.FrontalAreaM2 = spec.FrontalAreaM2;
            def.SideAreaM2 = spec.SideAreaM2;
            def.RollingResistanceCoefficient = spec.RollingResistanceCoefficient;
            def.WheelSideDragBaseN = spec.WheelSideDragBaseN;
            def.WheelSideDragLinearNPerMps = spec.WheelSideDragLinearNPerMps;
            def.RollingResistanceSpeedFactor = spec.RollingResistanceSpeedFactor;
            def.LaunchRpm = spec.LaunchRpm;
            def.CoupledDrivelineDragNm = spec.CoupledDrivelineDragNm;
            def.CoupledDrivelineViscousDragNmPerKrpm = spec.CoupledDrivelineViscousDragNmPerKrpm;
            def.EngineInertiaKgm2 = spec.EngineInertiaKgm2;
            def.EngineFrictionTorqueNm = spec.EngineFrictionTorqueNm;
            def.EngineFrictionLinearNmPerKrpm = spec.EngineFrictionLinearNmPerKrpm;
            def.EngineFrictionQuadraticNmPerKrpm2 = spec.EngineFrictionQuadraticNmPerKrpm2;
            def.DrivelineCouplingRate = spec.DrivelineCouplingRate;
            def.IdleControlWindowRpm = spec.IdleControlWindowRpm;
            def.IdleControlGainNmPerRpm = spec.IdleControlGainNmPerRpm;
            def.MinCoupledRiseIdleRpmPerSecond = spec.MinCoupledRiseIdleRpmPerSecond;
            def.MinCoupledRiseFullRpmPerSecond = spec.MinCoupledRiseFullRpmPerSecond;
            def.EngineOverrunIdleLossFraction = spec.EngineOverrunIdleLossFraction;
            def.OverrunCurveExponent = spec.OverrunCurveExponent;
            def.EngineBrakeTransferEfficiency = spec.EngineBrakeTransferEfficiency;
            def.FinalDriveRatio = spec.FinalDriveRatio;
            def.ReverseMaxSpeedKph = spec.ReverseMaxSpeedKph;
            def.ReversePowerFactor = spec.ReversePowerFactor;
            def.ReverseGearRatio = spec.ReverseGearRatio;
            def.TireCircumferenceM = spec.TireCircumferenceM;
            def.LateralGripCoefficient = spec.LateralGripCoefficient;
            def.HighSpeedStability = spec.HighSpeedStability;
            def.WheelbaseM = spec.WheelbaseM;
            def.MaxSteerDeg = spec.MaxSteerDeg;
            def.HighSpeedSteerGain = spec.HighSpeedSteerGain;
            def.HighSpeedSteerStartKph = spec.HighSpeedSteerStartKph;
            def.HighSpeedSteerFullKph = spec.HighSpeedSteerFullKph;
            def.CombinedGripPenalty = spec.CombinedGripPenalty;
            def.SlipAnglePeakDeg = spec.SlipAnglePeakDeg;
            def.SlipAngleFalloff = spec.SlipAngleFalloff;
            def.TurnResponse = spec.TurnResponse;
            def.MassSensitivity = spec.MassSensitivity;
            def.DownforceGripGain = spec.DownforceGripGain;
            def.CornerStiffnessFront = spec.CornerStiffnessFront;
            def.CornerStiffnessRear = spec.CornerStiffnessRear;
            def.YawInertiaScale = spec.YawInertiaScale;
            def.SteeringCurve = spec.SteeringCurve;
            def.TransientDamping = spec.TransientDamping;
            def.WidthM = spec.WidthM;
            def.LengthM = spec.LengthM;
            def.PowerFactor = spec.PowerFactor;
            def.GearRatios = spec.GearRatios;
            def.TorqueCurveRpm = spec.TorqueCurveRpm;
            def.TorqueCurveTorqueNm = spec.TorqueCurveTorqueNm;
            def.TorqueCurvePreset = spec.TorqueCurvePreset;
            def.BrakeStrength = spec.BrakeStrength;
            def.TransmissionPolicy = spec.TransmissionPolicy;
        }
    }
}

