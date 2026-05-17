using System;
using TopSpeed.Physics.Fuel;
using TopSpeed.Protocol;

namespace TopSpeed.Vehicles
{
    public sealed class OfficialVehicleSpec
    {
        public OfficialVehicleSpec(
            CarType carType,
            string name,
            int hasWipers,
            float surfaceTractionFactor,
            float topSpeed,
            int idleFreq,
            int topFreq,
            int shiftFreq,
            int gears,
            float steering,
            float idleRpm,
            float maxRpm,
            float revLimiter,
            float autoShiftRpm,
            float engineBraking,
            float massKg,
            float drivetrainEfficiency,
            float engineBrakingTorqueNm,
            float tireGripCoefficient,
            float peakTorqueNm,
            float peakTorqueRpm,
            float idleTorqueNm,
            float redlineTorqueNm,
            float dragCoefficient,
            float frontalAreaM2,
            float rollingResistanceCoefficient,
            float launchRpm,
            float engineInertiaKgm2,
            float engineFrictionTorqueNm,
            float drivelineCouplingRate,
            float finalDriveRatio,
            float reverseMaxSpeedKph,
            float reversePowerFactor,
            float reverseGearRatio,
            float tireCircumferenceM,
            float lateralGripCoefficient,
            float highSpeedStability,
            float wheelbaseM,
            float maxSteerDeg,
            float widthM,
            float lengthM,
            float powerFactor,
            float[] gearRatios,
            float brakeStrength,
            float highSpeedSteerGain = 1.08f,
            float highSpeedSteerStartKph = 140f,
            float highSpeedSteerFullKph = 240f,
            float combinedGripPenalty = 0.72f,
            float slipAnglePeakDeg = 8f,
            float slipAngleFalloff = 1.25f,
            float turnResponse = 1.0f,
            float massSensitivity = 0.75f,
            float downforceGripGain = 0.05f,
            float cornerStiffnessFront = 1.0f,
            float cornerStiffnessRear = 1.0f,
            float yawInertiaScale = 1.0f,
            float steeringCurve = 1.0f,
            float transientDamping = 1.0f,
            float[]? torqueCurveRpm = null,
            float[]? torqueCurveTorqueNm = null,
            string? torqueCurvePreset = null,
            TransmissionType primaryTransmissionType = TransmissionType.Atc,
            TransmissionType[]? supportedTransmissionTypes = null,
            bool shiftOnDemand = false,
            AutomaticDrivelineTuning? automaticTuning = null,
            TransmissionPolicy? transmissionPolicy = null,
            float sideAreaM2 = -1f,
            float rollingResistanceSpeedFactor = -1f,
            float wheelSideDragBaseN = -1f,
            float wheelSideDragLinearNPerMps = -1f,
            float coupledDrivelineDragNm = -1f,
            float coupledDrivelineViscousDragNmPerKrpm = -1f,
            float engineOverrunIdleLossFraction = -1f,
            float engineBrakeTransferEfficiency = -1f,
            float frictionLinearNmPerKrpm = -1f,
            float frictionQuadraticNmPerKrpm2 = -1f,
            float idleControlWindowRpm = -1f,
            float idleControlGainNmPerRpm = -1f,
            float minCoupledRiseIdleRpmPerSecond = -1f,
            float minCoupledRiseFullRpmPerSecond = -1f,
            float overrunCurveExponent = -1f,
            float fuelTankCapacityLiters = -1f,
            float engineDisplacementLiters = -1f)
        {
            CarType = carType;
            Name = name;
            HasWipers = hasWipers;
            SurfaceTractionFactor = surfaceTractionFactor;
            TopSpeed = topSpeed;
            IdleFreq = idleFreq;
            TopFreq = topFreq;
            ShiftFreq = shiftFreq;
            Gears = gears;
            Steering = steering;
            IdleRpm = idleRpm;
            MaxRpm = maxRpm;
            RevLimiter = revLimiter;
            AutoShiftRpm = autoShiftRpm;
            EngineBraking = engineBraking;
            FuelTankCapacityLiters = fuelTankCapacityLiters > 0f
                ? Math.Max(FuelDefaults.MinTankCapacityLiters, Math.Min(FuelDefaults.MaxTankCapacityLiters, fuelTankCapacityLiters))
                : FuelDefaults.DefaultTankCapacityLiters;
            EngineDisplacementLiters = engineDisplacementLiters > 0f
                ? Math.Max(FuelDefaults.MinEngineDisplacementLiters, Math.Min(FuelDefaults.MaxEngineDisplacementLiters, engineDisplacementLiters))
                : ResolveDefaultEngineDisplacementLiters(carType);
            MassKg = massKg;
            DrivetrainEfficiency = drivetrainEfficiency;
            EngineBrakingTorqueNm = engineBrakingTorqueNm;
            TireGripCoefficient = tireGripCoefficient;
            PeakTorqueNm = peakTorqueNm;
            PeakTorqueRpm = peakTorqueRpm;
            IdleTorqueNm = idleTorqueNm;
            RedlineTorqueNm = redlineTorqueNm;
            DragCoefficient = dragCoefficient;
            FrontalAreaM2 = frontalAreaM2;
            SideAreaM2 = sideAreaM2;
            RollingResistanceCoefficient = rollingResistanceCoefficient;
            RollingResistanceSpeedFactor = rollingResistanceSpeedFactor;
            LaunchRpm = launchRpm;
            EngineInertiaKgm2 = engineInertiaKgm2;
            EngineFrictionTorqueNm = engineFrictionTorqueNm;
            DrivelineCouplingRate = drivelineCouplingRate;
            FinalDriveRatio = finalDriveRatio;
            ReverseMaxSpeedKph = reverseMaxSpeedKph;
            ReversePowerFactor = reversePowerFactor;
            ReverseGearRatio = reverseGearRatio;
            TireCircumferenceM = tireCircumferenceM;
            LateralGripCoefficient = lateralGripCoefficient;
            HighSpeedStability = highSpeedStability;
            WheelbaseM = wheelbaseM;
            MaxSteerDeg = maxSteerDeg;
            HighSpeedSteerGain = highSpeedSteerGain;
            HighSpeedSteerStartKph = highSpeedSteerStartKph;
            HighSpeedSteerFullKph = highSpeedSteerFullKph;
            CombinedGripPenalty = combinedGripPenalty;
            SlipAnglePeakDeg = slipAnglePeakDeg;
            SlipAngleFalloff = slipAngleFalloff;
            TurnResponse = turnResponse;
            MassSensitivity = massSensitivity;
            DownforceGripGain = downforceGripGain;
            CornerStiffnessFront = cornerStiffnessFront;
            CornerStiffnessRear = cornerStiffnessRear;
            YawInertiaScale = yawInertiaScale;
            SteeringCurve = steeringCurve;
            TransientDamping = transientDamping;
            WidthM = widthM;
            LengthM = lengthM;
            PowerFactor = powerFactor;
            GearRatios = gearRatios;
            BrakeStrength = brakeStrength;
            TorqueCurveRpm = torqueCurveRpm;
            TorqueCurveTorqueNm = torqueCurveTorqueNm;
            TorqueCurvePreset = torqueCurvePreset;
            supportedTransmissionTypes ??= new[] { primaryTransmissionType };
            if (!TransmissionTypes.TryValidate(primaryTransmissionType, supportedTransmissionTypes, out var validationError))
                throw new ArgumentException(validationError, nameof(supportedTransmissionTypes));
            PrimaryTransmissionType = primaryTransmissionType;
            SupportedTransmissionTypes = (TransmissionType[])supportedTransmissionTypes.Clone();
            ShiftOnDemand = shiftOnDemand;
            AutomaticTuning = automaticTuning ?? AutomaticDrivelineTuning.Default;
            TransmissionPolicy = transmissionPolicy ?? TransmissionPolicy.Default;
            WheelSideDragBaseN = wheelSideDragBaseN;
            WheelSideDragLinearNPerMps = wheelSideDragLinearNPerMps;
            CoupledDrivelineDragNm = coupledDrivelineDragNm;
            CoupledDrivelineViscousDragNmPerKrpm = coupledDrivelineViscousDragNmPerKrpm;
            EngineOverrunIdleLossFraction = engineOverrunIdleLossFraction;
            EngineBrakeTransferEfficiency = engineBrakeTransferEfficiency;
            FrictionLinearNmPerKrpm = frictionLinearNmPerKrpm;
            FrictionQuadraticNmPerKrpm2 = frictionQuadraticNmPerKrpm2;
            IdleControlWindowRpm = idleControlWindowRpm;
            IdleControlGainNmPerRpm = idleControlGainNmPerRpm;
            MinCoupledRiseIdleRpmPerSecond = minCoupledRiseIdleRpmPerSecond;
            MinCoupledRiseFullRpmPerSecond = minCoupledRiseFullRpmPerSecond;
            OverrunCurveExponent = overrunCurveExponent;
        }

        public CarType CarType { get; }
        public string Name { get; }
        public int HasWipers { get; }
        public float SurfaceTractionFactor { get; }
        public float TopSpeed { get; }
        public int IdleFreq { get; }
        public int TopFreq { get; }
        public int ShiftFreq { get; }
        public int Gears { get; }
        public float Steering { get; }
        public float IdleRpm { get; }
        public float MaxRpm { get; }
        public float RevLimiter { get; }
        public float AutoShiftRpm { get; }
        public float EngineBraking { get; }
        public float FuelTankCapacityLiters { get; }
        public float EngineDisplacementLiters { get; }
        public float MassKg { get; }
        public float DrivetrainEfficiency { get; }
        public float EngineBrakingTorqueNm { get; }
        public float TireGripCoefficient { get; }
        public float PeakTorqueNm { get; }
        public float PeakTorqueRpm { get; }
        public float IdleTorqueNm { get; }
        public float RedlineTorqueNm { get; }
        public float DragCoefficient { get; }
        public float FrontalAreaM2 { get; }
        public float SideAreaM2 { get; }
        public float RollingResistanceCoefficient { get; }
        public float RollingResistanceSpeedFactor { get; }
        public float LaunchRpm { get; }
        public float WheelSideDragBaseN { get; }
        public float WheelSideDragLinearNPerMps { get; }
        public float CoupledDrivelineDragNm { get; }
        public float CoupledDrivelineViscousDragNmPerKrpm { get; }
        public float EngineInertiaKgm2 { get; }
        public float EngineFrictionTorqueNm { get; }
        public float DrivelineCouplingRate { get; }
        public float EngineOverrunIdleLossFraction { get; }
        public float EngineBrakeTransferEfficiency { get; }
        public float FrictionLinearNmPerKrpm { get; }
        public float FrictionQuadraticNmPerKrpm2 { get; }
        public float IdleControlWindowRpm { get; }
        public float IdleControlGainNmPerRpm { get; }
        public float MinCoupledRiseIdleRpmPerSecond { get; }
        public float MinCoupledRiseFullRpmPerSecond { get; }
        public float OverrunCurveExponent { get; }
        public float FinalDriveRatio { get; }
        public float ReverseMaxSpeedKph { get; }
        public float ReversePowerFactor { get; }
        public float ReverseGearRatio { get; }
        public float TireCircumferenceM { get; }
        public float LateralGripCoefficient { get; }
        public float HighSpeedStability { get; }
        public float WheelbaseM { get; }
        public float MaxSteerDeg { get; }
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
        public float WidthM { get; }
        public float LengthM { get; }
        public float PowerFactor { get; }
        public float[] GearRatios { get; }
        public float BrakeStrength { get; }
        public float[]? TorqueCurveRpm { get; }
        public float[]? TorqueCurveTorqueNm { get; }
        public string? TorqueCurvePreset { get; }
        public TransmissionType PrimaryTransmissionType { get; }
        public TransmissionType[] SupportedTransmissionTypes { get; }
        public bool ShiftOnDemand { get; }
        public AutomaticDrivelineTuning AutomaticTuning { get; }
        public TransmissionPolicy TransmissionPolicy { get; }

        private static float ResolveDefaultEngineDisplacementLiters(CarType carType)
        {
            switch (carType)
            {
                case CarType.Vehicle1:
                    return 3.8f;
                case CarType.Vehicle2:
                    return 4.0f;
                case CarType.Vehicle3:
                    return 1.4f;
                case CarType.Vehicle4:
                    return 2.0f;
                case CarType.Vehicle5:
                    return 7.0f;
                case CarType.Vehicle6:
                    return 2.5f;
                case CarType.Vehicle7:
                    return 6.5f;
                case CarType.Vehicle8:
                    return 3.0f;
                case CarType.Vehicle9:
                    return 2.1f;
                case CarType.Vehicle10:
                    return 1.0f;
                case CarType.Vehicle11:
                    return 1.1f;
                case CarType.Vehicle12:
                    return 1.0f;
                default:
                    return FuelDefaults.DefaultEngineDisplacementLiters;
            }
        }
    }
}
