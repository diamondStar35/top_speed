using System.Collections.Generic;
using TopSpeed.Localization;
using TopSpeed.Physics.Fuel;
using TopSpeed.Protocol;

namespace TopSpeed.Vehicles
{
    internal sealed class VehicleDefinition
    {
        public const float PitchCurveExponentDefault = 0.85f;
        public const float PitchCurveExponentMin = 0.5f;
        public const float PitchCurveExponentMax = 1.5f;
        public const float FuelTankCapacityDefaultLiters = FuelDefaults.DefaultTankCapacityLiters;
        public const float EngineDisplacementDefaultLiters = FuelDefaults.DefaultEngineDisplacementLiters;

        public CarType CarType { get; set; }
        public string Name { get; set; } = LocalizationService.Mark("Vehicle");
        public bool UserDefined { get; set; }
        public string? CustomFile { get; set; }
        public string? CustomVersion { get; set; }
        public string? CustomDescription { get; set; }
        /// <summary>
        /// Base traction scaling used for surface modifiers (higher = more grip).
        /// </summary>
        public float SurfaceTractionFactor { get; set; }
        public float TopSpeed { get; set; }
        public int IdleFreq { get; set; }
        public int TopFreq { get; set; }
        public int ShiftFreq { get; set; }
        public float PitchCurveExponent { get; set; } = PitchCurveExponentDefault;
        public int Gears { get; set; }
        public float Steering { get; set; }
        public TransmissionType PrimaryTransmissionType { get; set; } = TransmissionType.Atc;
        public TransmissionType[] SupportedTransmissionTypes { get; set; } = new[] { TransmissionType.Atc };
        public bool ShiftOnDemand { get; set; }
        public AutomaticDrivelineTuning AutomaticTuning { get; set; } = AutomaticDrivelineTuning.Default;
        public int HasWipers { get; set; }

        // Engine simulation parameters
        public float IdleRpm { get; set; } = 800f;
        public float MaxRpm { get; set; } = 7000f;
        public float RevLimiter { get; set; } = 6500f;
        public float AutoShiftRpm { get; set; } = 0f;
        public float EngineBraking { get; set; } = 0.3f;
        public float FuelTankCapacityLiters { get; set; } = FuelTankCapacityDefaultLiters;
        public float EngineDisplacementLiters { get; set; } = EngineDisplacementDefaultLiters;
        public float MassKg { get; set; } = 1500f;
        public float DrivetrainEfficiency { get; set; } = 0.85f;
        public float EngineBrakingTorqueNm { get; set; } = 150f;
        public float TireGripCoefficient { get; set; } = 0.9f;
        public float PeakTorqueNm { get; set; } = 200f;
        public float PeakTorqueRpm { get; set; } = 4000f;
        public float IdleTorqueNm { get; set; } = 60f;
        public float RedlineTorqueNm { get; set; } = 140f;
        public float DragCoefficient { get; set; } = 0.30f;
        public float FrontalAreaM2 { get; set; } = 2.2f;
        public float SideAreaM2 { get; set; } = -1f;
        public float RollingResistanceCoefficient { get; set; } = 0.015f;
        public float RollingResistanceSpeedFactor { get; set; } = -1f;
        public float WheelSideDragBaseN { get; set; } = -1f;
        public float WheelSideDragLinearNPerMps { get; set; } = -1f;
        public float LaunchRpm { get; set; } = 1800f;
        public float CoupledDrivelineDragNm { get; set; } = -1f;
        public float CoupledDrivelineViscousDragNmPerKrpm { get; set; } = -1f;
        public float EngineInertiaKgm2 { get; set; } = 0.24f;
        public float EngineFrictionTorqueNm { get; set; } = 20f;
        public float EngineFrictionLinearNmPerKrpm { get; set; } = -1f;
        public float EngineFrictionQuadraticNmPerKrpm2 { get; set; } = -1f;
        public float DrivelineCouplingRate { get; set; } = 12f;
        public float IdleControlWindowRpm { get; set; } = -1f;
        public float IdleControlGainNmPerRpm { get; set; } = -1f;
        public float MinCoupledRiseIdleRpmPerSecond { get; set; } = -1f;
        public float MinCoupledRiseFullRpmPerSecond { get; set; } = -1f;
        public float EngineOverrunIdleLossFraction { get; set; } = -1f;
        public float OverrunCurveExponent { get; set; } = -1f;
        public float EngineBrakeTransferEfficiency { get; set; } = -1f;
        public float FinalDriveRatio { get; set; } = 3.5f;
        public float ReverseMaxSpeedKph { get; set; } = 35f;
        public float ReversePowerFactor { get; set; } = 0.55f;
        public float ReverseGearRatio { get; set; } = 3.2f;
        public float TireCircumferenceM { get; set; } = 2.0f;
        public float LateralGripCoefficient { get; set; } = 1.0f;
        public float HighSpeedStability { get; set; } = 0.0f;
        public float WheelbaseM { get; set; } = 2.7f;
        public float MaxSteerDeg { get; set; } = 35f;
        public float HighSpeedSteerGain { get; set; } = 1.08f;
        public float HighSpeedSteerStartKph { get; set; } = 140f;
        public float HighSpeedSteerFullKph { get; set; } = 240f;
        public float CombinedGripPenalty { get; set; } = 0.72f;
        public float SlipAnglePeakDeg { get; set; } = 8f;
        public float SlipAngleFalloff { get; set; } = 1.25f;
        public float TurnResponse { get; set; } = 1.0f;
        public float MassSensitivity { get; set; } = 0.75f;
        public float DownforceGripGain { get; set; } = 0.05f;
        public float CornerStiffnessFront { get; set; } = 1.0f;
        public float CornerStiffnessRear { get; set; } = 1.0f;
        public float YawInertiaScale { get; set; } = 1.0f;
        public float SteeringCurve { get; set; } = 1.0f;
        public float TransientDamping { get; set; } = 1.0f;
        public float WidthM { get; set; } = 1.8f;
        public float LengthM { get; set; } = 4.5f;
        
        /// <summary>
        /// Power factor controls how fast the vehicle accelerates (0.1 = very slow, 1.0 = fast).
        /// Lower values = more gradual acceleration suitable for keyboard gameplay.
        /// </summary>
        public float PowerFactor { get; set; } = 0.5f;
        
        /// <summary>
        /// Custom gear ratios. If null, uses default calculated ratios.
        /// Each gear ratio affects torque multiplication - higher = more torque, lower speed.
        /// </summary>
        public float[]? GearRatios { get; set; }
        public float[]? TorqueCurveRpm { get; set; }
        public float[]? TorqueCurveTorqueNm { get; set; }
        public string? TorqueCurvePreset { get; set; }
        
        /// <summary>
        /// Brake strength multiplier (0.5 = weak brakes, 1.0 = normal, 2.0 = strong).
        /// Affects how quickly the vehicle decelerates when braking.
        /// </summary>
        public float BrakeStrength { get; set; } = 1.0f;
        public TransmissionPolicy TransmissionPolicy { get; set; } = TransmissionPolicy.Default;

        private readonly string?[] _sounds = new string?[8];
        private readonly Dictionary<VehicleAction, string[]> _soundVariants =
            new Dictionary<VehicleAction, string[]>();

        public string? GetSoundPath(VehicleAction action) => _sounds[(int)action];
        public void SetSoundPath(VehicleAction action, string? path) => _sounds[(int)action] = path;
        public IReadOnlyList<string>? GetSoundPaths(VehicleAction action)
        {
            return _soundVariants.TryGetValue(action, out var values) ? values : null;
        }
        public void SetSoundPaths(VehicleAction action, IReadOnlyList<string> paths)
        {
            if (paths == null || paths.Count == 0)
            {
                _soundVariants.Remove(action);
                _sounds[(int)action] = null;
                return;
            }

            var copy = new string[paths.Count];
            for (var i = 0; i < paths.Count; i++)
                copy[i] = paths[i];
            _soundVariants[action] = copy;
            _sounds[(int)action] = copy[0];
        }

        public static float ClampPitchCurveExponent(float value)
        {
            if (float.IsNaN(value) || float.IsInfinity(value))
                return PitchCurveExponentDefault;
            if (value < PitchCurveExponentMin)
                return PitchCurveExponentMin;
            if (value > PitchCurveExponentMax)
                return PitchCurveExponentMax;
            return value;
        }
    }
}

