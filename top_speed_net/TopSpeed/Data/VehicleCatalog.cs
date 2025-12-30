using System.IO;
using TopSpeed.Protocol;

namespace TopSpeed.Data
{
    internal sealed class VehicleParameters
    {
        private readonly string?[] _sounds = new string?[8];

        public string? GetSoundPath(VehicleAction action) => _sounds[(int)action];

        public string Name { get; }
        public int HasWipers { get; }
        public float Acceleration { get; }
        public float Deceleration { get; }
        public float TopSpeed { get; }
        public int IdleFreq { get; }
        public int TopFreq { get; }
        public int ShiftFreq { get; }
        public int Gears { get; }
        public float Steering { get; }
        public int SteeringFactor { get; }

        // Engine simulation parameters
        public float IdleRpm { get; }
        public float MaxRpm { get; }
        public float RevLimiter { get; }
        public float EngineBraking { get; }
        public float PowerFactor { get; }
        public float[]? GearRatios { get; }
        public float BrakeStrength { get; }

        public VehicleParameters(
            string name,
            string? engineSound,
            string? startSound,
            string? hornSound,
            string? throttleSound,
            string? crashSound,
            string? monoCrashSound,
            string? brakeSound,
            string? backfireSound,
            int hasWipers,
            float acceleration,
            float deceleration,
            float topSpeed,
            int idleFreq,
            int topFreq,
            int shiftFreq,
            int gears,
            float steering,
            int steeringFactor,
            float idleRpm = 800f,
            float maxRpm = 7000f,
            float revLimiter = 6500f,
            float engineBraking = 0.3f,
            float powerFactor = 0.5f,
            float[]? gearRatios = null,
            float brakeStrength = 1.0f)
        {
            Name = name;
            _sounds[(int)VehicleAction.Engine] = engineSound;
            _sounds[(int)VehicleAction.Start] = startSound;
            _sounds[(int)VehicleAction.Horn] = hornSound;
            _sounds[(int)VehicleAction.Throttle] = throttleSound;
            _sounds[(int)VehicleAction.Crash] = crashSound;
            _sounds[(int)VehicleAction.CrashMono] = monoCrashSound;
            _sounds[(int)VehicleAction.Brake] = brakeSound;
            _sounds[(int)VehicleAction.Backfire] = backfireSound;

            HasWipers = hasWipers;
            Acceleration = acceleration;
            Deceleration = deceleration;
            TopSpeed = topSpeed;
            IdleFreq = idleFreq;
            TopFreq = topFreq;
            ShiftFreq = shiftFreq;
            Gears = gears;
            Steering = steering;
            SteeringFactor = steeringFactor;

            IdleRpm = idleRpm;
            MaxRpm = maxRpm;
            RevLimiter = revLimiter;
            EngineBraking = engineBraking;
            PowerFactor = powerFactor;
            GearRatios = gearRatios;
            BrakeStrength = brakeStrength;
        }
    }

    internal static class VehicleCatalog
    {
        public const int VehicleCount = 12;

        // Common gear ratio presets
        private static readonly float[] RacingGearRatios = new[] { 3.2f, 2.3f, 1.7f, 1.3f, 1.0f, 0.85f, 0.72f };
        private static readonly float[] SportGearRatios = new[] { 3.5f, 2.4f, 1.8f, 1.4f, 1.1f, 0.9f };
        private static readonly float[] StandardGearRatios = new[] { 3.8f, 2.1f, 1.4f, 1.0f, 0.8f };
        private static readonly float[] ClassicGearRatios = new[] { 4.0f, 2.5f, 1.6f, 1.0f };
        private static readonly float[] TruckGearRatios = new[] { 4.5f, 2.8f, 1.9f, 1.4f, 1.0f, 0.75f };
        private static readonly float[] MotorcycleGearRatios = new[] { 3.0f, 2.2f, 1.7f, 1.3f, 1.05f, 0.9f };

        public static readonly VehicleParameters[] Vehicles =
        {
            // Vehicle 1: Racing car - fast but still keyboard-friendly (takes ~15 seconds to top speed)
            new VehicleParameters("Nissan GT-R Nismo", null, null, null, null, null, null, null, null,
                hasWipers: 1, acceleration: 0.06f, deceleration: 0.40f, topSpeed: 315.0f,
                idleFreq: 22050, topFreq: 55000, shiftFreq: 26000, gears: 6, steering: 1.60f, steeringFactor: 60,
                idleRpm: 900f, maxRpm: 8000f, revLimiter: 7600f, engineBraking: 0.25f,
                powerFactor: 0.7f, gearRatios: RacingGearRatios),

            // Vehicle 2: Racing car - very responsive, high-revving
            new VehicleParameters("Porsche 911 GT3 RS", null, null, null, null, null, null, null, null,
                hasWipers: 1, acceleration: 0.07f, deceleration: 0.45f, topSpeed: 312.0f,
                idleFreq: 22050, topFreq: 60000, shiftFreq: 35000, gears: 7, steering: 1.50f, steeringFactor: 55,
                idleRpm: 950f, maxRpm: 9000f, revLimiter: 8500f, engineBraking: 0.22f,
                powerFactor: 0.75f, gearRatios: RacingGearRatios),

            // Vehicle 3: Small car - slow acceleration, economical
            new VehicleParameters("Fiat 500", null, null, null, null, null, null, null, null,
                hasWipers: 1, acceleration: 0.035f, deceleration: 0.30f, topSpeed: 160.0f,
                idleFreq: 6000, topFreq: 25000, shiftFreq: 19000, gears: 5, steering: 1.50f, steeringFactor: 72,
                idleRpm: 750f, maxRpm: 6000f, revLimiter: 5500f, engineBraking: 0.40f,
                powerFactor: 0.35f, gearRatios: StandardGearRatios),

            // Vehicle 4: Small sporty car - better than Fiat but not racing
            new VehicleParameters("Mini Cooper S", null, null, null, null, null, null, null, null,
                hasWipers: 1, acceleration: 0.045f, deceleration: 0.35f, topSpeed: 235.0f,
                idleFreq: 6000, topFreq: 27000, shiftFreq: 20000, gears: 6, steering: 1.40f, steeringFactor: 56,
                idleRpm: 800f, maxRpm: 6500f, revLimiter: 6000f, engineBraking: 0.32f,
                powerFactor: 0.45f, gearRatios: SportGearRatios),

            // Vehicle 5: Classic muscle car - torquey but heavy
            new VehicleParameters("Ford Mustang 1969", null, null, null, null, null, null, null, null,
                hasWipers: 1, acceleration: 0.04f, deceleration: 0.35f, topSpeed: 200.0f,
                idleFreq: 6000, topFreq: 33000, shiftFreq: 27500, gears: 4, steering: 2.30f, steeringFactor: 80,
                idleRpm: 650f, maxRpm: 5500f, revLimiter: 5000f, engineBraking: 0.35f,
                powerFactor: 0.4f, gearRatios: ClassicGearRatios),

            // Vehicle 6: Common sedan - comfortable, not sporty
            new VehicleParameters("Toyota Camry", null, null, null, null, null, null, null, null,
                hasWipers: 1, acceleration: 0.035f, deceleration: 0.30f, topSpeed: 210.0f,
                idleFreq: 7025, topFreq: 40000, shiftFreq: 32500, gears: 8, steering: 2.20f, steeringFactor: 95,
                idleRpm: 700f, maxRpm: 6000f, revLimiter: 5500f, engineBraking: 0.38f,
                powerFactor: 0.35f, gearRatios: null), // Uses default 8-gear ratios

            // Vehicle 7: Supercar - fastest acceleration, high power
            new VehicleParameters("Lamborghini Aventador", null, null, null, null, null, null, null, null,
                hasWipers: 1, acceleration: 0.08f, deceleration: 0.80f, topSpeed: 350.0f,
                idleFreq: 6000, topFreq: 26000, shiftFreq: 21000, gears: 7, steering: 2.10f, steeringFactor: 65,
                idleRpm: 1000f, maxRpm: 8500f, revLimiter: 8000f, engineBraking: 0.20f,
                powerFactor: 0.8f, gearRatios: RacingGearRatios),

            // Vehicle 8: Premium sedan - balanced performance
            new VehicleParameters("BMW 3 Series", null, null, null, null, null, null, null, null,
                hasWipers: 1, acceleration: 0.045f, deceleration: 0.40f, topSpeed: 250.0f,
                idleFreq: 10000, topFreq: 45000, shiftFreq: 34000, gears: 8, steering: 2.00f, steeringFactor: 70,
                idleRpm: 750f, maxRpm: 6500f, revLimiter: 6000f, engineBraking: 0.30f,
                powerFactor: 0.45f, gearRatios: null), // Uses default 8-gear ratios

            // Vehicle 9: Bus/Van - very slow acceleration, heavy
            new VehicleParameters("Mercedes Sprinter", null, null, null, null, null, null, null, null,
                hasWipers: 1, acceleration: 0.02f, deceleration: 0.20f, topSpeed: 160.0f,
                idleFreq: 22050, topFreq: 30550, shiftFreq: 22550, gears: 6, steering: 1.50f, steeringFactor: 85,
                idleRpm: 600f, maxRpm: 4500f, revLimiter: 4000f, engineBraking: 0.45f,
                powerFactor: 0.2f, gearRatios: TruckGearRatios),

            // Vehicle 10: Sport motorcycle - quick, light, high-revving
            new VehicleParameters("Kawasaki Ninja ZX-10R", null, null, null, null, null, null, null, null,
                hasWipers: 0, acceleration: 0.09f, deceleration: 0.50f, topSpeed: 299.0f,
                idleFreq: 22050, topFreq: 60000, shiftFreq: 35000, gears: 6, steering: 1.40f, steeringFactor: 50,
                idleRpm: 1100f, maxRpm: 14000f, revLimiter: 13500f, engineBraking: 0.28f,
                powerFactor: 0.85f, gearRatios: MotorcycleGearRatios),

            // Vehicle 11: Superbike - fastest motorcycle
            new VehicleParameters("Ducati Panigale V4", null, null, null, null, null, null, null, null,
                hasWipers: 0, acceleration: 0.10f, deceleration: 0.55f, topSpeed: 310.0f,
                idleFreq: 22050, topFreq: 60000, shiftFreq: 35000, gears: 6, steering: 1.30f, steeringFactor: 50,
                idleRpm: 1200f, maxRpm: 15000f, revLimiter: 14500f, engineBraking: 0.25f,
                powerFactor: 0.9f, gearRatios: MotorcycleGearRatios),

            // Vehicle 12: Sport motorcycle - balanced
            new VehicleParameters("Yamaha YZF-R1", null, null, null, null, null, null, null, null,
                hasWipers: 0, acceleration: 0.085f, deceleration: 0.48f, topSpeed: 299.0f,
                idleFreq: 22050, topFreq: 27550, shiftFreq: 23550, gears: 6, steering: 1.50f, steeringFactor: 66,
                idleRpm: 1100f, maxRpm: 14500f, revLimiter: 14000f, engineBraking: 0.30f,
                powerFactor: 0.8f, gearRatios: MotorcycleGearRatios)
        };
    }
}
