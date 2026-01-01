using System;
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
        public float AutoShiftRpm { get; }
        public float EngineBraking { get; }
        public float FinalDriveRatio { get; }
        public float TireCircumferenceM { get; }
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
            float autoShiftRpm = 0f,
            float engineBraking = 0.3f,
            float finalDriveRatio = 3.5f,
            float tireCircumferenceM = 2.0f,
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
            AutoShiftRpm = autoShiftRpm;
            EngineBraking = engineBraking;
            FinalDriveRatio = finalDriveRatio;
            TireCircumferenceM = tireCircumferenceM;
            PowerFactor = powerFactor;
            GearRatios = gearRatios;
            BrakeStrength = brakeStrength;
        }
    }

    internal static class VehicleCatalog
    {
        public const int VehicleCount = 12;

        private static float TireCircumferenceM(int widthMm, int aspectPercent, int rimInches)
        {
            var sidewallMm = widthMm * (aspectPercent / 100f);
            var diameterMm = (rimInches * 25.4f) + (2f * sidewallMm);
            return (float)(Math.PI * (diameterMm / 1000f));
        }

                // Real-world gear ratios per vehicle (see sources in project notes)
        private static readonly float[] GtrRatios = new[] { 4.06f, 2.30f, 1.59f, 1.25f, 1.00f, 0.80f };
        private static readonly float[] Gt3RsRatios = new[] { 3.75f, 2.38f, 1.72f, 1.34f, 1.11f, 0.96f, 0.84f };
        private static readonly float[] Fiat500Ratios = new[] { 3.909f, 2.238f, 1.520f, 1.156f, 0.872f };
        private static readonly float[] MiniCooperSRatios = new[] { 3.92f, 2.14f, 1.39f, 1.09f, 0.89f, 0.76f };
        private static readonly float[] Mustang69Ratios = new[] { 2.32f, 1.69f, 1.29f, 1.00f };
        private static readonly float[] CamryRatios = new[] { 5.25f, 3.03f, 1.95f, 1.46f, 1.22f, 1.00f, 0.81f, 0.67f };
        private static readonly float[] AventadorRatios = new[] { 3.91f, 2.44f, 1.81f, 1.46f, 1.19f, 0.97f, 0.89f };
        private static readonly float[] Bmw3SeriesRatios = new[] { 4.71f, 3.14f, 2.11f, 1.67f, 1.29f, 1.00f, 0.84f, 0.67f };
        private static readonly float[] SprinterRatios = new[] { 4.3772f, 2.8586f, 1.9206f, 1.3684f, 1.0000f, 0.8204f, 0.7276f };
        private static readonly float[] Zx10rRatios = new[] { 2.600f, 2.222f, 1.944f, 1.722f, 1.550f, 1.391f };
        private static readonly float[] PanigaleV4Ratios = new[] { 2.40f, 2.00f, 1.7368f, 1.5238f, 1.3636f, 1.2273f };
        private static readonly float[] R1Ratios = new[] { 2.533f, 2.063f, 1.762f, 1.522f, 1.364f, 1.269f };

        public static readonly VehicleParameters[] Vehicles =
        {
            // Vehicle 1: Racing car - fast but still keyboard-friendly (takes ~15 seconds to top speed)
            new VehicleParameters("Nissan GT-R Nismo", null, null, null, null, null, null, null, null,
                hasWipers: 1, acceleration: 0.06f, deceleration: 0.40f, topSpeed: 315.0f,
                idleFreq: 22050, topFreq: 55000, shiftFreq: 26000, gears: 6, steering: 1.60f, steeringFactor: 60,
                idleRpm: 900f, maxRpm: 8000f, revLimiter: 7600f, autoShiftRpm: 7600f * 0.92f, engineBraking: 0.25f,
                finalDriveRatio: 3.70f, tireCircumferenceM: TireCircumferenceM(285, 35, 20),
                powerFactor: 0.7f, gearRatios: GtrRatios),

            // Vehicle 2: Racing car - very responsive, high-revving
            new VehicleParameters("Porsche 911 GT3 RS", null, null, null, null, null, null, null, null,
                hasWipers: 1, acceleration: 0.07f, deceleration: 0.45f, topSpeed: 312.0f,
                idleFreq: 22050, topFreq: 60000, shiftFreq: 35000, gears: 7, steering: 1.50f, steeringFactor: 55,
                idleRpm: 950f, maxRpm: 9000f, revLimiter: 8500f, autoShiftRpm: 8500f * 0.92f, engineBraking: 0.22f,
                finalDriveRatio: 3.97f, tireCircumferenceM: TireCircumferenceM(325, 30, 21),
                powerFactor: 0.75f, gearRatios: Gt3RsRatios),

            // Vehicle 3: Small car - slow acceleration, economical
            new VehicleParameters("Fiat 500", null, null, null, null, null, null, null, null,
                hasWipers: 1, acceleration: 0.035f, deceleration: 0.30f, topSpeed: 160.0f,
                idleFreq: 6000, topFreq: 25000, shiftFreq: 19000, gears: 5, steering: 1.50f, steeringFactor: 72,
                idleRpm: 750f, maxRpm: 6000f, revLimiter: 5500f, autoShiftRpm: 5500f * 0.92f, engineBraking: 0.40f,
                finalDriveRatio: 3.353f, tireCircumferenceM: TireCircumferenceM(195, 45, 16),
                powerFactor: 0.35f, gearRatios: Fiat500Ratios),

            // Vehicle 4: Small sporty car - better than Fiat but not racing
            new VehicleParameters("Mini Cooper S", null, null, null, null, null, null, null, null,
                hasWipers: 1, acceleration: 0.045f, deceleration: 0.35f, topSpeed: 235.0f,
                idleFreq: 6000, topFreq: 27000, shiftFreq: 20000, gears: 6, steering: 1.40f, steeringFactor: 56,
                idleRpm: 800f, maxRpm: 6500f, revLimiter: 6000f, autoShiftRpm: 6000f * 0.92f, engineBraking: 0.32f,
                finalDriveRatio: 3.59f, tireCircumferenceM: TireCircumferenceM(195, 55, 16),
                powerFactor: 0.45f, gearRatios: MiniCooperSRatios),

            // Vehicle 5: Classic muscle car - torquey but heavy
            new VehicleParameters("Ford Mustang 1969", null, null, null, null, null, null, null, null,
                hasWipers: 1, acceleration: 0.04f, deceleration: 0.35f, topSpeed: 200.0f,
                idleFreq: 6000, topFreq: 33000, shiftFreq: 27500, gears: 4, steering: 2.30f, steeringFactor: 80,
                idleRpm: 650f, maxRpm: 5500f, revLimiter: 5000f, autoShiftRpm: 5000f * 0.92f, engineBraking: 0.35f,
                finalDriveRatio: 3.25f, tireCircumferenceM: TireCircumferenceM(215, 70, 14),
                powerFactor: 0.4f, gearRatios: Mustang69Ratios),

            // Vehicle 6: Common sedan - comfortable, not sporty
            new VehicleParameters("Toyota Camry", null, null, null, null, null, null, null, null,
                hasWipers: 1, acceleration: 0.035f, deceleration: 0.30f, topSpeed: 210.0f,
                idleFreq: 7025, topFreq: 40000, shiftFreq: 32500, gears: 8, steering: 2.20f, steeringFactor: 95,
                idleRpm: 700f, maxRpm: 6000f, revLimiter: 5500f, autoShiftRpm: 5500f * 0.92f, engineBraking: 0.38f,
                finalDriveRatio: 2.80f, tireCircumferenceM: TireCircumferenceM(215, 55, 17),
                powerFactor: 0.35f, gearRatios: CamryRatios),

            // Vehicle 7: Supercar - fastest acceleration, high power
            new VehicleParameters("Lamborghini Aventador", null, null, null, null, null, null, null, null,
                hasWipers: 1, acceleration: 0.08f, deceleration: 0.80f, topSpeed: 350.0f,
                idleFreq: 6000, topFreq: 26000, shiftFreq: 21000, gears: 7, steering: 2.10f, steeringFactor: 65,
                idleRpm: 1000f, maxRpm: 8500f, revLimiter: 8000f, autoShiftRpm: 8000f * 0.92f, engineBraking: 0.20f,
                finalDriveRatio: 2.86f, tireCircumferenceM: TireCircumferenceM(355, 25, 21),
                powerFactor: 0.8f, gearRatios: AventadorRatios),

            // Vehicle 8: Premium sedan - balanced performance
            new VehicleParameters("BMW 3 Series", null, null, null, null, null, null, null, null,
                hasWipers: 1, acceleration: 0.045f, deceleration: 0.40f, topSpeed: 250.0f,
                idleFreq: 10000, topFreq: 45000, shiftFreq: 34000, gears: 8, steering: 2.00f, steeringFactor: 70,
                idleRpm: 750f, maxRpm: 6500f, revLimiter: 6000f, autoShiftRpm: 6000f * 0.92f, engineBraking: 0.30f,
                finalDriveRatio: 3.15f, tireCircumferenceM: TireCircumferenceM(225, 50, 17),
                powerFactor: 0.45f, gearRatios: Bmw3SeriesRatios),

            // Vehicle 9: Bus/Van - very slow acceleration, heavy
            new VehicleParameters("Mercedes Sprinter", null, null, null, null, null, null, null, null,
                hasWipers: 1, acceleration: 0.02f, deceleration: 0.20f, topSpeed: 160.0f,
                idleFreq: 22050, topFreq: 30550, shiftFreq: 22550, gears: 7, steering: 1.50f, steeringFactor: 85,
                idleRpm: 600f, maxRpm: 4500f, revLimiter: 4000f, autoShiftRpm: 4000f * 0.92f, engineBraking: 0.45f,
                finalDriveRatio: 3.923f, tireCircumferenceM: TireCircumferenceM(245, 75, 16),
                powerFactor: 0.2f, gearRatios: SprinterRatios),

            // Vehicle 10: Sport motorcycle - quick, light, high-revving
            new VehicleParameters("Kawasaki Ninja ZX-10R", null, null, null, null, null, null, null, null,
                hasWipers: 0, acceleration: 0.09f, deceleration: 0.50f, topSpeed: 299.0f,
                idleFreq: 22050, topFreq: 60000, shiftFreq: 35000, gears: 6, steering: 1.40f, steeringFactor: 50,
                idleRpm: 1100f, maxRpm: 14000f, revLimiter: 13500f, autoShiftRpm: 13500f * 0.92f, engineBraking: 0.28f,
                finalDriveRatio: 3.8562f, tireCircumferenceM: TireCircumferenceM(190, 55, 17),
                powerFactor: 0.85f, gearRatios: Zx10rRatios),

            // Vehicle 11: Superbike - fastest motorcycle
            new VehicleParameters("Ducati Panigale V4", null, null, null, null, null, null, null, null,
                hasWipers: 0, acceleration: 0.10f, deceleration: 0.55f, topSpeed: 310.0f,
                idleFreq: 22050, topFreq: 60000, shiftFreq: 35000, gears: 6, steering: 1.30f, steeringFactor: 50,
                idleRpm: 1200f, maxRpm: 15000f, revLimiter: 14500f, autoShiftRpm: 14500f * 0.92f, engineBraking: 0.25f,
                finalDriveRatio: 4.6125f, tireCircumferenceM: TireCircumferenceM(200, 60, 17),
                powerFactor: 0.9f, gearRatios: PanigaleV4Ratios),

            // Vehicle 12: Sport motorcycle - balanced
            new VehicleParameters("Yamaha YZF-R1", null, null, null, null, null, null, null, null,
                hasWipers: 0, acceleration: 0.085f, deceleration: 0.48f, topSpeed: 299.0f,
                idleFreq: 22050, topFreq: 27550, shiftFreq: 23550, gears: 6, steering: 1.50f, steeringFactor: 66,
                idleRpm: 1100f, maxRpm: 14500f, revLimiter: 14000f, autoShiftRpm: 14000f * 0.92f, engineBraking: 0.30f,
                finalDriveRatio: 4.1807f, tireCircumferenceM: TireCircumferenceM(190, 55, 17),
                powerFactor: 0.8f, gearRatios: R1Ratios)
        };
    }
}

