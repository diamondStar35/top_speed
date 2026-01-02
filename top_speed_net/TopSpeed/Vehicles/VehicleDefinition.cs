using TopSpeed.Protocol;

namespace TopSpeed.Vehicles
{
    internal sealed class VehicleDefinition
    {
        public CarType CarType { get; set; }
        public string Name { get; set; } = "Vehicle";
        public bool UserDefined { get; set; }
        public string? CustomFile { get; set; }
        /// <summary>
        /// Base traction scaling used for surface modifiers (higher = more grip).
        /// </summary>
        public float SurfaceTractionFactor { get; set; }
        public float Deceleration { get; set; }
        public float TopSpeed { get; set; }
        public int IdleFreq { get; set; }
        public int TopFreq { get; set; }
        public int ShiftFreq { get; set; }
        public int Gears { get; set; }
        public float Steering { get; set; }
        public int SteeringFactor { get; set; }
        public int HasWipers { get; set; }

        // Engine simulation parameters
        public float IdleRpm { get; set; } = 800f;
        public float MaxRpm { get; set; } = 7000f;
        public float RevLimiter { get; set; } = 6500f;
        public float AutoShiftRpm { get; set; } = 0f;
        public float EngineBraking { get; set; } = 0.3f;
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
        public float RollingResistanceCoefficient { get; set; } = 0.015f;       
        public float LaunchRpm { get; set; } = 1800f;
        public float FinalDriveRatio { get; set; } = 3.5f;
        public float TireCircumferenceM { get; set; } = 2.0f;
        public float LateralGripCoefficient { get; set; } = 1.0f;
        public float HighSpeedStability { get; set; } = 0.0f;
        public float WheelbaseM { get; set; } = 2.7f;
        public float MaxSteerDeg { get; set; } = 35f;
        
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
        
        /// <summary>
        /// Brake strength multiplier (0.5 = weak brakes, 1.0 = normal, 2.0 = strong).
        /// Affects how quickly the vehicle decelerates when braking.
        /// </summary>
        public float BrakeStrength { get; set; } = 1.0f;

        private readonly string?[] _sounds = new string?[8];

        public string? GetSoundPath(VehicleAction action) => _sounds[(int)action];
        public void SetSoundPath(VehicleAction action, string? path) => _sounds[(int)action] = path;
    }
}
