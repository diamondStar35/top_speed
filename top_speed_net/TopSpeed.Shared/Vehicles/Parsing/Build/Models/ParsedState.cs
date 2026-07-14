using System;
using System.Collections.Generic;

namespace TopSpeed.Vehicles.Parsing
{
    public static partial class VehicleTsvParser
    {
        private sealed class ParsedSections
        {
            public Section Meta { get; set; } = null!;
            public Section Sounds { get; set; } = null!;
            public Section General { get; set; } = null!;
            public Section Engine { get; set; } = null!;
            public Section Torque { get; set; } = null!;
            public Section EngineRot { get; set; } = null!;
            public Section Resistance { get; set; } = null!;
            public Section TorqueCurve { get; set; } = null!;
            public Section Transmission { get; set; } = null!;
            public Section? TransmissionAtc { get; set; }
            public Section? TransmissionDct { get; set; }
            public Section? TransmissionCvt { get; set; }
            public Section Drivetrain { get; set; } = null!;
            public Section Gears { get; set; } = null!;
            public Section Steering { get; set; } = null!;
            public Section TireModel { get; set; } = null!;
            public Section Dynamics { get; set; } = null!;
            public Section Dimensions { get; set; } = null!;
            public Section Tires { get; set; } = null!;
            public Section? Policy { get; set; }
        }

        private sealed class ParsedValues
        {
            public string MetaName { get; set; } = string.Empty;
            public string MetaVersion { get; set; } = string.Empty;
            public string MetaDescription { get; set; } = string.Empty;

            public string EngineSound { get; set; } = string.Empty;
            public string StartSound { get; set; } = string.Empty;
            public string? StopSound { get; set; }
            public string HornSound { get; set; } = string.Empty;
            public string? ThrottleSound { get; set; }
            public IReadOnlyList<string> CrashVariants { get; set; } = Array.Empty<string>();
            public string BrakeSound { get; set; } = string.Empty;
            public IReadOnlyList<string> BackfireVariants { get; set; } = Array.Empty<string>();

            public int IdleFreq { get; set; }
            public int TopFreq { get; set; }
            public int ShiftFreq { get; set; }
            public float PitchCurveExponent { get; set; }

            public float SurfaceTractionFactor { get; set; }
            public float TopSpeed { get; set; }
            public bool HasWipers { get; set; }

            public int GearCount { get; set; }
            public List<float>? GearRatios { get; set; }
            public TransmissionType PrimaryTransmissionType { get; set; }
            public IReadOnlyList<TransmissionType> SupportedTransmissionTypes { get; set; } = Array.Empty<TransmissionType>();
            public bool ShiftOnDemand { get; set; }
            public AutomaticDrivelineTuning AutomaticTuning { get; set; } = AutomaticDrivelineTuning.Default;

            public float IdleRpm { get; set; }
            public float MaxRpm { get; set; }
            public float RevLimiter { get; set; }
            public float AutoShiftRpm { get; set; }
            public float EngineBraking { get; set; }
            public float FuelTankCapacityLiters { get; set; }
            public float EngineDisplacementLiters { get; set; }
            public float MassKg { get; set; }
            public float DrivetrainEfficiency { get; set; }
            public float LaunchRpm { get; set; }

            public float DragCoefficient { get; set; }
            public float FrontalArea { get; set; }
            public float SideArea { get; set; } = -1f;
            public float RollingResistance { get; set; }
            public float WheelSideDragBaseN { get; set; } = -1f;
            public float WheelSideDragLinearNPerMps { get; set; } = -1f;
            public float RollingResistanceSpeedFactor { get; set; } = -1f;
            public float CoupledDrivelineDragNm { get; set; } = -1f;
            public float CoupledDrivelineViscousDragNmPerKrpm { get; set; } = -1f;

            public float EngineBrakingTorque { get; set; }
            public float PeakTorque { get; set; }
            public float PeakTorqueRpm { get; set; }
            public float IdleTorque { get; set; }
            public float RedlineTorque { get; set; }
            public float PowerFactor { get; set; }
            public float EngineInertiaKgm2 { get; set; }
            public float EngineFrictionBaseNm { get; set; }
            public float EngineFrictionLinearNmPerKrpm { get; set; } = -1f;
            public float EngineFrictionQuadraticNmPerKrpm2 { get; set; } = -1f;
            public float DrivelineCouplingRate { get; set; }
            public float IdleControlWindowRpm { get; set; } = -1f;
            public float IdleControlGainNmPerRpm { get; set; } = -1f;
            public float MinCoupledRiseIdleRpmPerSecond { get; set; } = -1f;
            public float MinCoupledRiseFullRpmPerSecond { get; set; } = -1f;
            public float EngineOverrunIdleLossFraction { get; set; } = -1f;
            public float OverrunCurveExponent { get; set; } = -1f;
            public float EngineBrakeTransferEfficiency { get; set; } = -1f;

            public float FinalDrive { get; set; }
            public float ReverseMaxSpeed { get; set; }
            public float ReversePowerFactor { get; set; }
            public float ReverseGearRatio { get; set; }
            public float BrakeStrength { get; set; }

            public float SteeringResponse { get; set; }
            public float Wheelbase { get; set; }
            public float MaxSteerDeg { get; set; }
            public float HighSpeedStability { get; set; }
            public float HighSpeedSteerGain { get; set; }
            public float HighSpeedSteerStartKph { get; set; }
            public float HighSpeedSteerFullKph { get; set; }

            public float TireGrip { get; set; }
            public float? TireWearBasePerKilometer { get; set; }
            public float? TireWearSlipRatePerSecond { get; set; }
            public float? TireWearCorneringSlipWeight { get; set; }
            public float? TireWearLongitudinalSlipWeight { get; set; }
            public float? TireWearLoadGain { get; set; }
            public float? TireWearHotStartTemperatureC { get; set; }
            public float? TireWearHotGainPerC { get; set; }
            public float? TireWearColdStartTemperatureC { get; set; }
            public float? TireWearColdGainPerC { get; set; }
            public float? TireTemperatureColdEndC { get; set; }
            public float? TireTemperatureOptimalStartC { get; set; }
            public float? TireTemperatureOptimalEndC { get; set; }
            public float? TireTemperatureOverheatEndC { get; set; }
            public float? TireGripVeryCold { get; set; }
            public float? TireGripColdEnd { get; set; }
            public float? TireGripOptimal { get; set; }
            public float? TireGripOverheatEnd { get; set; }
            public float? TireGripCooked { get; set; }
            public float? TireWearGripAtFullWear { get; set; }
            public float? TireHeatCorneringCPerSecond { get; set; }
            public float? TireHeatAccelerationCPerSecond { get; set; }
            public float? TireHeatBrakingCPerSecond { get; set; }
            public float? TireHeatLoadCPerSecond { get; set; }
            public float? TireHeatRollingCPerSecond { get; set; }
            public float? TireCoolingAirflowPerMpsPerCPerSecond { get; set; }
            public float? TireExchangeAmbientPerCPerSecond { get; set; }
            public float? TireExchangeRoadPerCPerSecond { get; set; }
            public float? TireExchangeWetRoadPerCPerSecond { get; set; }
            public float? TireSurfaceToTreadConductancePerSecond { get; set; }
            public float? TireTreadToCarcassConductancePerSecond { get; set; }
            public float? TireTreadMassRatio { get; set; }
            public float? TireCarcassMassRatio { get; set; }
            public float? TireSlipSmoothingTauSeconds { get; set; }
            public float LateralGrip { get; set; }
            public float CombinedGripPenalty { get; set; }
            public float SlipAnglePeakDeg { get; set; }
            public float SlipAngleFalloff { get; set; }
            public float TurnResponse { get; set; }
            public float MassSensitivity { get; set; }
            public float DownforceGripGain { get; set; }

            public float CornerStiffnessFront { get; set; }
            public float CornerStiffnessRear { get; set; }
            public float YawInertiaScale { get; set; }
            public float SteeringCurve { get; set; }
            public float TransientDamping { get; set; }

            public float WidthM { get; set; }
            public float LengthM { get; set; }

            public float? TireCircumference { get; set; }
            public int? TireWidth { get; set; }
            public int? TireAspect { get; set; }
            public int? TireRim { get; set; }
        }
    }
}

