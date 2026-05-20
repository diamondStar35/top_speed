using System;
using TopSpeed.Physics.Powertrain;
using TopSpeed.Physics.Tires.Wear;
using TopSpeed.Physics.Torque;
using TopSpeed.Vehicles;

namespace TopSpeed.Bots
{
    public sealed class BotPhysicsConfig
    {
        public BotPhysicsConfig(
            float surfaceTractionFactor,
            float topSpeedKph,
            float massKg,
            float drivetrainEfficiency,
            float engineBrakingTorqueNm,
            float tireGripCoefficient,
            float brakeStrength,
            float wheelRadiusM,
            float engineBraking,
            float idleRpm,
            float revLimiter,
            float finalDriveRatio,
            float powerFactor,
            float peakTorqueNm,
            float peakTorqueRpm,
            float idleTorqueNm,
            float redlineTorqueNm,
            float dragCoefficient,
            float frontalAreaM2,
            float sideAreaM2,
            float rollingResistanceCoefficient,
            float rollingResistanceSpeedFactor,
            float wheelSideDragBaseN,
            float wheelSideDragLinearNPerMps,
            float launchRpm,
            float reversePowerFactor,
            float reverseGearRatio,
            float engineInertiaKgm2,
            float engineFrictionTorqueNm,
            float drivelineCouplingRate,
            float lateralGripCoefficient,
            float highSpeedStability,
            float wheelbaseM,
            float widthM,
            float lengthM,
            float maxSteerDeg,
            float steering,
            float highSpeedSteerGain,
            float highSpeedSteerStartKph,
            float highSpeedSteerFullKph,
            float combinedGripPenalty,
            float slipAnglePeakDeg,
            float slipAngleFalloff,
            float turnResponse,
            float massSensitivity,
            float downforceGripGain,
            float cornerStiffnessFront,
            float cornerStiffnessRear,
            float yawInertiaScale,
            float steeringCurve,
            float transientDamping,
            int gears,
            CurveProfile torqueCurve,
            float[]? gearRatios = null,
            TransmissionPolicy? transmissionPolicy = null,
            TransmissionType activeTransmissionType = TransmissionType.Atc,
            AutomaticDrivelineTuning? automaticTuning = null,
            float coupledDrivelineDragNm = -1f,
            float coupledDrivelineViscousDragNmPerKrpm = -1f,
            float frictionLinearNmPerKrpm = -1f,
            float frictionQuadraticNmPerKrpm2 = -1f,
            float idleControlWindowRpm = -1f,
            float idleControlGainNmPerRpm = -1f,
            float minCoupledRiseIdleRpmPerSecond = -1f,
            float minCoupledRiseFullRpmPerSecond = -1f,
            float engineOverrunIdleLossFraction = -1f,
            float overrunCurveExponent = -1f,
            float engineBrakeTransferEfficiency = -1f,
            TireWearConfig? tireWearConfig = null)
        {
            var build = PowertrainBuild.Create(
                new BuildInput(
                    massKg: massKg,
                    drivetrainEfficiency: drivetrainEfficiency,
                    engineBrakingTorqueNm: engineBrakingTorqueNm,
                    tireGripCoefficient: tireGripCoefficient,
                    brakeStrength: brakeStrength,
                    wheelRadiusM: wheelRadiusM,
                    engineBraking: engineBraking,
                    idleRpm: idleRpm,
                    revLimiter: revLimiter,
                    finalDriveRatio: finalDriveRatio,
                    powerFactor: powerFactor,
                    peakTorqueNm: peakTorqueNm,
                    peakTorqueRpm: peakTorqueRpm,
                    idleTorqueNm: idleTorqueNm,
                    redlineTorqueNm: redlineTorqueNm,
                    dragCoefficient: dragCoefficient,
                    frontalAreaM2: frontalAreaM2,
                    sideAreaM2: sideAreaM2,
                    rollingResistanceCoefficient: rollingResistanceCoefficient,
                    rollingResistanceSpeedFactor: rollingResistanceSpeedFactor,
                    wheelSideDragBaseN: wheelSideDragBaseN,
                    wheelSideDragLinearNPerMps: wheelSideDragLinearNPerMps,
                    launchRpm: launchRpm,
                    reversePowerFactor: reversePowerFactor,
                    reverseGearRatio: reverseGearRatio,
                    reverseMaxSpeedKph: 35f,
                    engineInertiaKgm2: engineInertiaKgm2,
                    engineFrictionTorqueNm: engineFrictionTorqueNm,
                    drivelineCouplingRate: drivelineCouplingRate,
                    gears: gears,
                    torqueCurve: torqueCurve,
                    gearRatios: gearRatios,
                    coupledDrivelineDragNm: coupledDrivelineDragNm,
                    coupledDrivelineViscousDragNmPerKrpm: coupledDrivelineViscousDragNmPerKrpm,
                    frictionLinearNmPerKrpm: frictionLinearNmPerKrpm,
                    frictionQuadraticNmPerKrpm2: frictionQuadraticNmPerKrpm2,
                    idleControlWindowRpm: idleControlWindowRpm,
                    idleControlGainNmPerRpm: idleControlGainNmPerRpm,
                    minCoupledRiseIdleRpmPerSecond: minCoupledRiseIdleRpmPerSecond,
                    minCoupledRiseFullRpmPerSecond: minCoupledRiseFullRpmPerSecond,
                    engineOverrunIdleLossFraction: engineOverrunIdleLossFraction,
                    overrunCurveExponent: overrunCurveExponent,
                    engineBrakeTransferEfficiency: engineBrakeTransferEfficiency));

            SurfaceTractionFactor = Math.Max(0.01f, surfaceTractionFactor);
            TopSpeedKph = Math.Max(1f, topSpeedKph);
            MassKg = build.Powertrain.MassKg;
            DrivetrainEfficiency = build.Powertrain.DrivetrainEfficiency;
            EngineBrakingTorqueNm = build.Powertrain.EngineBrakingTorqueNm;
            TireGripCoefficient = build.Powertrain.TireGripCoefficient;
            BrakeStrength = build.Powertrain.BrakeStrength;
            WheelRadiusM = build.Powertrain.WheelRadiusM;
            EngineBraking = build.Powertrain.EngineBraking;
            IdleRpm = build.Powertrain.IdleRpm;
            RevLimiter = build.Powertrain.RevLimiter;
            FinalDriveRatio = build.Powertrain.FinalDriveRatio;
            PowerFactor = build.Powertrain.PowerFactor;
            PeakTorqueNm = build.Powertrain.PeakTorqueNm;
            PeakTorqueRpm = build.Powertrain.PeakTorqueRpm;
            IdleTorqueNm = build.Powertrain.IdleTorqueNm;
            RedlineTorqueNm = build.Powertrain.RedlineTorqueNm;
            DragCoefficient = build.Powertrain.DragCoefficient;
            FrontalAreaM2 = build.Powertrain.FrontalAreaM2;
            SideAreaM2 = build.Powertrain.SideAreaM2;
            RollingResistanceCoefficient = build.Powertrain.RollingResistanceCoefficient;
            RollingResistanceSpeedFactor = build.Powertrain.RollingResistanceSpeedFactor;
            WheelSideDragBaseN = build.WheelSideDragBaseN;
            WheelSideDragLinearNPerMps = build.WheelSideDragLinearNPerMps;
            LaunchRpm = build.Powertrain.LaunchRpm;
            ReversePowerFactor = build.Powertrain.ReversePowerFactor;
            ReverseGearRatio = build.Powertrain.ReverseGearRatio;
            EngineInertiaKgm2 = build.Powertrain.EngineInertiaKgm2;
            EngineFrictionTorqueNm = build.Powertrain.EngineFrictionTorqueNm;
            DrivelineCouplingRate = build.Powertrain.DrivelineCouplingRate;
            CoupledDrivelineDragNm = build.CoupledDrivelineDragNm;
            CoupledDrivelineViscousDragNmPerKrpm = build.CoupledDrivelineViscousDragNmPerKrpm;
            EngineFrictionLinearNmPerKrpm = build.FrictionLinearNmPerKrpm;
            EngineFrictionQuadraticNmPerKrpm2 = build.FrictionQuadraticNmPerKrpm2;
            IdleControlWindowRpm = build.IdleControlWindowRpm;
            IdleControlGainNmPerRpm = build.IdleControlGainNmPerRpm;
            MinCoupledRiseIdleRpmPerSecond = build.MinCoupledRiseIdleRpmPerSecond;
            MinCoupledRiseFullRpmPerSecond = build.MinCoupledRiseFullRpmPerSecond;
            EngineOverrunIdleLossFraction = build.EngineOverrunIdleLossFraction;
            OverrunCurveExponent = build.OverrunCurveExponent;
            EngineBrakeTransferEfficiency = build.EngineBrakeTransferEfficiency;
            LateralGripCoefficient = Math.Max(0.1f, lateralGripCoefficient);
            HighSpeedStability = Math.Max(0f, Math.Min(1.0f, highSpeedStability));
            WheelbaseM = Math.Max(0.5f, wheelbaseM);
            WidthM = Math.Max(0.5f, widthM);
            LengthM = Math.Max(0.5f, lengthM);
            MaxSteerDeg = Math.Max(5f, Math.Min(60f, maxSteerDeg));
            Steering = steering;
            HighSpeedSteerGain = Math.Max(0.7f, Math.Min(1.6f, highSpeedSteerGain));
            HighSpeedSteerStartKph = Math.Max(60f, Math.Min(260f, highSpeedSteerStartKph));
            HighSpeedSteerFullKph = Math.Max(100f, Math.Min(350f, highSpeedSteerFullKph));
            if (HighSpeedSteerFullKph <= HighSpeedSteerStartKph)
                HighSpeedSteerFullKph = HighSpeedSteerStartKph + 1f;
            CombinedGripPenalty = Math.Max(0f, Math.Min(1f, combinedGripPenalty));
            SlipAnglePeakDeg = Math.Max(0.5f, Math.Min(20f, slipAnglePeakDeg));
            SlipAngleFalloff = Math.Max(0.01f, Math.Min(5f, slipAngleFalloff));
            TurnResponse = Math.Max(0.2f, Math.Min(2.5f, turnResponse));
            MassSensitivity = Math.Max(0f, Math.Min(1f, massSensitivity));
            DownforceGripGain = Math.Max(0f, Math.Min(1f, downforceGripGain));
            CornerStiffnessFront = Math.Max(0.2f, Math.Min(3f, cornerStiffnessFront));
            CornerStiffnessRear = Math.Max(0.2f, Math.Min(3f, cornerStiffnessRear));
            YawInertiaScale = Math.Max(0.5f, Math.Min(2f, yawInertiaScale));
            SteeringCurve = Math.Max(0.5f, Math.Min(2f, steeringCurve));
            TransientDamping = Math.Max(0f, Math.Min(6f, transientDamping));
            Gears = build.Powertrain.Gears;
            GearRatios = build.GearRatios;
            TransmissionPolicy = transmissionPolicy ?? TransmissionPolicy.Default;
            ActiveTransmissionType = activeTransmissionType;
            AutomaticTuning = automaticTuning ?? AutomaticDrivelineTuning.Default;
            TorqueCurve = torqueCurve ?? throw new ArgumentNullException(nameof(torqueCurve));
            Powertrain = build.Powertrain;
            TireWearConfig = tireWearConfig
                ?? TireWearProfiles.CreateFromVehicle(
                    TireGripCoefficient,
                    MassKg,
                    WheelRadiusM * 2f * (float)Math.PI,
                    LateralGripCoefficient);
        }

        public float SurfaceTractionFactor { get; }
        public float TopSpeedKph { get; }
        public float MassKg { get; }
        public float DrivetrainEfficiency { get; }
        public float EngineBrakingTorqueNm { get; }
        public float TireGripCoefficient { get; }
        public float BrakeStrength { get; }
        public float WheelRadiusM { get; }
        public float EngineBraking { get; }
        public float IdleRpm { get; }
        public float RevLimiter { get; }
        public float FinalDriveRatio { get; }
        public float PowerFactor { get; }
        public float PeakTorqueNm { get; }
        public float PeakTorqueRpm { get; }
        public float IdleTorqueNm { get; }
        public float RedlineTorqueNm { get; }
        public float DragCoefficient { get; }
        public float FrontalAreaM2 { get; }
        public float SideAreaM2 { get; }
        public float RollingResistanceCoefficient { get; }
        public float RollingResistanceSpeedFactor { get; }
        public float WheelSideDragBaseN { get; }
        public float WheelSideDragLinearNPerMps { get; }
        public float LaunchRpm { get; }
        public float ReversePowerFactor { get; }
        public float ReverseGearRatio { get; }
        public float EngineInertiaKgm2 { get; }
        public float EngineFrictionTorqueNm { get; }
        public float DrivelineCouplingRate { get; }
        public float CoupledDrivelineDragNm { get; }
        public float CoupledDrivelineViscousDragNmPerKrpm { get; }
        public float EngineFrictionLinearNmPerKrpm { get; }
        public float EngineFrictionQuadraticNmPerKrpm2 { get; }
        public float IdleControlWindowRpm { get; }
        public float IdleControlGainNmPerRpm { get; }
        public float MinCoupledRiseIdleRpmPerSecond { get; }
        public float MinCoupledRiseFullRpmPerSecond { get; }
        public float EngineOverrunIdleLossFraction { get; }
        public float OverrunCurveExponent { get; }
        public float EngineBrakeTransferEfficiency { get; }
        public float LateralGripCoefficient { get; }
        public float HighSpeedStability { get; }
        public float WheelbaseM { get; }
        public float WidthM { get; }
        public float LengthM { get; }
        public float MaxSteerDeg { get; }
        public float Steering { get; }
        public float HighSpeedSteerGain { get; }
        public float HighSpeedSteerStartKph { get; }
        public float HighSpeedSteerFullKph { get; }
        public float CombinedGripPenalty { get; }
        public float SlipAnglePeakDeg { get; }
        public float SlipAngleFalloff { get; }
        public float TurnResponse { get; }
        public float MassSensitivity { get; }
        public float DownforceGripGain { get; }
        public float CornerStiffnessFront { get; }
        public float CornerStiffnessRear { get; }
        public float YawInertiaScale { get; }
        public float SteeringCurve { get; }
        public float TransientDamping { get; }
        public int Gears { get; }
        public float[] GearRatios { get; }
        public CurveProfile TorqueCurve { get; }
        public TransmissionPolicy TransmissionPolicy { get; }
        public TransmissionType ActiveTransmissionType { get; }
        public AutomaticDrivelineTuning AutomaticTuning { get; }
        public Config Powertrain { get; }
        public TireWearConfig TireWearConfig { get; }

        public float GetGearRatio(int gear)
        {
            var clamped = Math.Max(1, Math.Min(Gears, gear));
            return GearRatios[clamped - 1];
        }
    }
}

