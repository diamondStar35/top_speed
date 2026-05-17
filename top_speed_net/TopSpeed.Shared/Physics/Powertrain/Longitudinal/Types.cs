using System;
using TopSpeed.Vehicles;

namespace TopSpeed.Physics.Powertrain
{
    public readonly struct LongitudinalStepInput
    {
        public LongitudinalStepInput(
            Config config,
            float elapsedSeconds,
            float speedMps,
            float throttle,
            float brake,
            float surfaceTractionModifier,
            float surfaceBrakeModifier,
            float surfaceRollingResistanceModifier,
            float longitudinalGripFactor,
            int gear,
            bool inReverse,
            bool isNeutral,
            TransmissionType transmissionType,
            float drivelineCouplingFactor,
            float creepAccelerationMps2,
            float currentEngineRpm,
            bool requestDrive,
            bool requestBrake,
            bool applyEngineBraking,
            ResistanceEnvironment resistanceEnvironment,
            float? driveRatioOverride = null,
            float driveAccelerationScale = 1f,
            bool? gearPathEngaged = null,
            float? effectiveMassKg = null)
        {
            Config = config ?? throw new ArgumentNullException(nameof(config));
            ElapsedSeconds = elapsedSeconds;
            SpeedMps = speedMps;
            Throttle = throttle;
            Brake = brake;
            SurfaceTractionModifier = surfaceTractionModifier;
            SurfaceBrakeModifier = surfaceBrakeModifier;
            SurfaceRollingResistanceModifier = surfaceRollingResistanceModifier;
            LongitudinalGripFactor = longitudinalGripFactor;
            Gear = gear;
            InReverse = inReverse;
            IsNeutral = isNeutral;
            TransmissionType = transmissionType;
            DrivelineCouplingFactor = drivelineCouplingFactor;
            CreepAccelerationMps2 = creepAccelerationMps2;
            CurrentEngineRpm = currentEngineRpm;
            RequestDrive = requestDrive;
            RequestBrake = requestBrake;
            ApplyEngineBraking = applyEngineBraking;
            ResistanceEnvironment = resistanceEnvironment;
            DriveRatioOverride = driveRatioOverride;
            DriveAccelerationScale = driveAccelerationScale;
            GearPathEngaged = gearPathEngaged ?? !isNeutral;
            EffectiveMassKg = effectiveMassKg;
        }

        public Config Config { get; }
        public float ElapsedSeconds { get; }
        public float SpeedMps { get; }
        public float Throttle { get; }
        public float Brake { get; }
        public float SurfaceTractionModifier { get; }
        public float SurfaceBrakeModifier { get; }
        public float SurfaceRollingResistanceModifier { get; }
        public float LongitudinalGripFactor { get; }
        public int Gear { get; }
        public bool InReverse { get; }
        public bool IsNeutral { get; }
        public TransmissionType TransmissionType { get; }
        public float DrivelineCouplingFactor { get; }
        public float CreepAccelerationMps2 { get; }
        public float CurrentEngineRpm { get; }
        public bool RequestDrive { get; }
        public bool RequestBrake { get; }
        public bool ApplyEngineBraking { get; }
        public ResistanceEnvironment ResistanceEnvironment { get; }
        public float? DriveRatioOverride { get; }
        public float DriveAccelerationScale { get; }
        public bool GearPathEngaged { get; }
        public float? EffectiveMassKg { get; }
    }

    public readonly struct LongitudinalStepResult
    {
        public LongitudinalStepResult(
            float speedDeltaKph,
            float coupledDriveRpm,
            float driveAccelerationMps2,
            float totalDecelKph,
            float brakeDecelKph,
            float engineBrakeDecelKph,
            float aerodynamicDecelKph,
            float rollingResistanceDecelKph,
            float wheelSideDragDecelKph,
            float coupledDrivelineDragDecelKph)
        {
            SpeedDeltaKph = speedDeltaKph;
            CoupledDriveRpm = coupledDriveRpm;
            DriveAccelerationMps2 = driveAccelerationMps2;
            TotalDecelKph = totalDecelKph;
            BrakeDecelKph = brakeDecelKph;
            EngineBrakeDecelKph = engineBrakeDecelKph;
            AerodynamicDecelKph = aerodynamicDecelKph;
            RollingResistanceDecelKph = rollingResistanceDecelKph;
            WheelSideDragDecelKph = wheelSideDragDecelKph;
            CoupledDrivelineDragDecelKph = coupledDrivelineDragDecelKph;
        }

        public float SpeedDeltaKph { get; }
        public float CoupledDriveRpm { get; }
        public float DriveAccelerationMps2 { get; }
        public float TotalDecelKph { get; }
        public float BrakeDecelKph { get; }
        public float EngineBrakeDecelKph { get; }
        public float AerodynamicDecelKph { get; }
        public float RollingResistanceDecelKph { get; }
        public float WheelSideDragDecelKph { get; }
        public float CoupledDrivelineDragDecelKph { get; }
    }
}
