using System;
using System.Collections.Generic;
using TopSpeed.Localization;
using TopSpeed.Vehicles;

namespace TopSpeed.Vehicles.Parsing
{
    internal enum VehicleTsvIssueSeverity
    {
        Warning = 0,
        Error = 1
    }

    internal readonly struct VehicleTsvIssue
    {
        public VehicleTsvIssue(VehicleTsvIssueSeverity severity, int line, string message)
        {
            Severity = severity;
            Line = line;
            Message = message ?? string.Empty;
        }

        public VehicleTsvIssueSeverity Severity { get; }
        public int Line { get; }
        public string Message { get; }

        public override string ToString()
        {
            return Line > 0
                ? LocalizationService.Format(LocalizationService.Mark("Line {0}: {1}"), Line, Message)
                : Message;
        }
    }

    internal sealed class CustomVehicleMeta
    {
        public CustomVehicleMeta(string name, string version, string description)
        {
            Name = name;
            Version = version;
            Description = description;
        }

        public string Name { get; }
        public string Version { get; }
        public string Description { get; }
    }

    internal sealed class CustomVehicleSounds
    {
        public string Engine { get; set; } = string.Empty;
        public string Start { get; set; } = string.Empty;
        public string? Stop { get; set; }
        public string Horn { get; set; } = string.Empty;
        public string? Throttle { get; set; }
        public IReadOnlyList<string> CrashVariants { get; set; } = Array.Empty<string>();
        public string Brake { get; set; } = string.Empty;
        public IReadOnlyList<string> BackfireVariants { get; set; } = Array.Empty<string>();
    }

    internal sealed class CustomVehicleTsvData
    {
        public string SourcePath { get; set; } = string.Empty;
        public string SourceDirectory { get; set; } = string.Empty;
        public CustomVehicleMeta Meta { get; set; } = new CustomVehicleMeta("Vehicle", "1", string.Empty);
        public CustomVehicleSounds Sounds { get; set; } = new CustomVehicleSounds();

        public float SurfaceTractionFactor { get; set; }
        public float TopSpeed { get; set; }
        public int HasWipers { get; set; }

        public int IdleFreq { get; set; }
        public int TopFreq { get; set; }
        public int ShiftFreq { get; set; }
        public float PitchCurveExponent { get; set; } = VehicleDefinition.PitchCurveExponentDefault;

        public int Gears { get; set; }
        public float[] GearRatios { get; set; } = Array.Empty<float>();
        public TransmissionType PrimaryTransmissionType { get; set; } = TransmissionType.Atc;
        public TransmissionType[] SupportedTransmissionTypes { get; set; } = new[] { TransmissionType.Atc };
        public bool ShiftOnDemand { get; set; }
        public AutomaticDrivelineTuning AutomaticTuning { get; set; } = AutomaticDrivelineTuning.Default;

        public float IdleRpm { get; set; }
        public float MaxRpm { get; set; }
        public float RevLimiter { get; set; }
        public float AutoShiftRpm { get; set; }
        public float EngineBraking { get; set; }
        public float FuelTankCapacityLiters { get; set; } = VehicleDefinition.FuelTankCapacityDefaultLiters;
        public float EngineDisplacementLiters { get; set; } = VehicleDefinition.EngineDisplacementDefaultLiters;
        public float MassKg { get; set; }
        public float DrivetrainEfficiency { get; set; }
        public float EngineBrakingTorqueNm { get; set; }
        public float PeakTorqueNm { get; set; }
        public float PeakTorqueRpm { get; set; }
        public float IdleTorqueNm { get; set; }
        public float RedlineTorqueNm { get; set; }
        public float DragCoefficient { get; set; }
        public float FrontalAreaM2 { get; set; }
        public float SideAreaM2 { get; set; } = -1f;
        public float RollingResistanceCoefficient { get; set; }
        public float WheelSideDragBaseN { get; set; } = -1f;
        public float WheelSideDragLinearNPerMps { get; set; } = -1f;
        public float RollingResistanceSpeedFactor { get; set; } = -1f;
        public float LaunchRpm { get; set; }
        public float CoupledDrivelineDragNm { get; set; } = -1f;
        public float CoupledDrivelineViscousDragNmPerKrpm { get; set; } = -1f;
        public float EngineInertiaKgm2 { get; set; }
        public float EngineFrictionTorqueNm { get; set; }
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
        public float PowerFactor { get; set; }
        public string? TorqueCurvePreset { get; set; }
        public float[] TorqueCurveRpm { get; set; } = Array.Empty<float>();
        public float[] TorqueCurveTorqueNm { get; set; } = Array.Empty<float>();

        public float FinalDriveRatio { get; set; }
        public float ReverseMaxSpeedKph { get; set; }
        public float ReversePowerFactor { get; set; }
        public float ReverseGearRatio { get; set; }
        public float BrakeStrength { get; set; }

        public float Steering { get; set; }
        public float TireGripCoefficient { get; set; }
        public float LateralGripCoefficient { get; set; }
        public float HighSpeedStability { get; set; }
        public float WheelbaseM { get; set; }
        public float MaxSteerDeg { get; set; }
        public float HighSpeedSteerGain { get; set; }
        public float HighSpeedSteerStartKph { get; set; }
        public float HighSpeedSteerFullKph { get; set; }
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
        public float TireCircumferenceM { get; set; }

        public TransmissionPolicy TransmissionPolicy { get; set; } = TransmissionPolicy.Default;
    }
}


