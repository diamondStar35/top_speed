using System;
using System.IO;
using TopSpeed.Protocol;
using TopSpeed.Vehicles;

namespace TopSpeed.Data
{
    internal sealed class VehicleParameters
    {
        private readonly string?[] _sounds = new string?[8];

        public string? GetSoundPath(VehicleAction action) => _sounds[(int)action];

        public string Name { get; }
        public int HasWipers { get; }
        public float SurfaceTractionFactor { get; }
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
        public float RollingResistanceCoefficient { get; }
        public float LaunchRpm { get; }
        public float FinalDriveRatio { get; }
        public float TireCircumferenceM { get; }
        public float LateralGripCoefficient { get; }
        public float HighSpeedStability { get; }
        public float WheelbaseM { get; }
        public float MaxSteerDeg { get; }
        public float TrackWidthM { get; }
        public float WidthM { get; }
        public float LengthM { get; }
        public VehicleDynamicsModel DynamicsModel { get; }
        public float PowerFactor { get; }
        public float[]? GearRatios { get; }
        public float BrakeStrength { get; }
        public float SteerInputRate { get; }
        public float SteerReturnRate { get; }
        public float SteerGamma { get; }
        public float MaxSteerLowDeg { get; }
        public float MaxSteerHighDeg { get; }
        public float SteerSpeedKph { get; }
        public float SteerSpeedExponent { get; }
        public float CorneringStiffnessFront { get; }
        public float CorneringStiffnessRear { get; }
        public float YawInertiaKgM2 { get; }
        public float CgToFrontAxleM { get; }
        public float CgToRearAxleM { get; }
        public float CgHeightM { get; }
        public float WeightDistributionFront { get; }
        public float BrakeBiasFront { get; }
        public float DriveBiasFront { get; }
        public float RollStiffnessFrontFraction { get; }
        public float TireLoadSensitivity { get; }
        public float DownforceCoefficient { get; }
        public float DownforceFrontBias { get; }
        public float LongitudinalStiffnessFront { get; }
        public float LongitudinalStiffnessRear { get; }

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
            float surfaceTractionFactor,
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
            float massKg = 1500f,
            float drivetrainEfficiency = 0.85f,
            float engineBrakingTorqueNm = 150f,
            float tireGripCoefficient = 0.9f,
            float peakTorqueNm = 200f,
            float peakTorqueRpm = 4000f,
            float idleTorqueNm = 60f,
            float redlineTorqueNm = 140f,
            float dragCoefficient = 0.30f,
            float frontalAreaM2 = 2.2f,
            float rollingResistanceCoefficient = 0.015f,
            float launchRpm = 1800f,
            float finalDriveRatio = 3.5f,
            float tireCircumferenceM = 2.0f,
            float lateralGripCoefficient = 1.0f,
            float highSpeedStability = 0.0f,
            float wheelbaseM = 2.7f,
            float maxSteerDeg = 35f,
            float trackWidthM = 0f,
            float widthM = 1.8f,
            float lengthM = 4.5f,
            VehicleDynamicsModel dynamicsModel = VehicleDynamicsModel.FourWheel,
            float powerFactor = 0.5f,
            float[]? gearRatios = null,
            float brakeStrength = 1.0f,
            float steerInputRate = 0.9f,
            float steerReturnRate = 0.7f,
            float steerGamma = 1.8f,
            float maxSteerLowDeg = 32f,
            float maxSteerHighDeg = 9f,
            float steerSpeedKph = 80f,
            float steerSpeedExponent = 1.6f,
            float corneringStiffnessFront = 0f,
            float corneringStiffnessRear = 0f,
            float yawInertiaKgM2 = 0f,
            float cgToFrontAxleM = 0f,
            float cgToRearAxleM = 0f,
            float cgHeightM = 0f,
            float weightDistributionFront = 0f,
            float brakeBiasFront = 0f,
            float driveBiasFront = 0f,
            float rollStiffnessFrontFraction = 0f,
            float tireLoadSensitivity = 0f,
            float downforceCoefficient = 0f,
            float downforceFrontBias = 0f,
            float longitudinalStiffnessFront = 0f,
            float longitudinalStiffnessRear = 0f)
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
            SurfaceTractionFactor = surfaceTractionFactor;
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
            RollingResistanceCoefficient = rollingResistanceCoefficient;        
            LaunchRpm = launchRpm;
            FinalDriveRatio = finalDriveRatio;
            TireCircumferenceM = tireCircumferenceM;
            LateralGripCoefficient = lateralGripCoefficient;
            HighSpeedStability = highSpeedStability;
            WheelbaseM = wheelbaseM;
            MaxSteerDeg = maxSteerDeg;
            TrackWidthM = trackWidthM;
            WidthM = widthM;
            LengthM = lengthM;
            DynamicsModel = dynamicsModel;
            PowerFactor = powerFactor;
            GearRatios = gearRatios;
            BrakeStrength = brakeStrength;
            SteerInputRate = steerInputRate;
            SteerReturnRate = steerReturnRate;
            SteerGamma = steerGamma;
            MaxSteerLowDeg = maxSteerLowDeg;
            MaxSteerHighDeg = maxSteerHighDeg;
            SteerSpeedKph = steerSpeedKph;
            SteerSpeedExponent = steerSpeedExponent;
            CorneringStiffnessFront = corneringStiffnessFront;
            CorneringStiffnessRear = corneringStiffnessRear;
            YawInertiaKgM2 = yawInertiaKgM2;
            CgToFrontAxleM = cgToFrontAxleM;
            CgToRearAxleM = cgToRearAxleM;
            CgHeightM = cgHeightM;
            WeightDistributionFront = weightDistributionFront;
            BrakeBiasFront = brakeBiasFront;
            DriveBiasFront = driveBiasFront;
            RollStiffnessFrontFraction = rollStiffnessFrontFraction;
            TireLoadSensitivity = tireLoadSensitivity;
            DownforceCoefficient = downforceCoefficient;
            DownforceFrontBias = downforceFrontBias;
            LongitudinalStiffnessFront = longitudinalStiffnessFront;
            LongitudinalStiffnessRear = longitudinalStiffnessRear;
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

        private static float CorneringStiffness(float massKg, float tireGrip, float lateralGrip, float factor)
        {
            return massKg * 9.80665f * tireGrip * lateralGrip * factor;
        }

                // Real-world gear ratios per vehicle
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
                hasWipers: 1, surfaceTractionFactor: 0.06f, deceleration: 0.40f, topSpeed: 315.0f,
                idleFreq: 22050, topFreq: 55000, shiftFreq: 26000, gears: 6, steering: 1.60f, steeringFactor: 60,
                idleRpm: 900f, maxRpm: 8000f, revLimiter: 7600f, autoShiftRpm: 7600f * 0.92f, engineBraking: 0.25f,
                massKg: 1774f, drivetrainEfficiency: 0.80f, engineBrakingTorqueNm: 652f, tireGripCoefficient: 1.0f,
                peakTorqueNm: 652f, peakTorqueRpm: 3600f, idleTorqueNm: 652f * 0.3f, redlineTorqueNm: 652f * 0.6f,
                dragCoefficient: 0.26f, frontalAreaM2: 2.2f, rollingResistanceCoefficient: 0.015f, launchRpm: 2500f,
                finalDriveRatio: 3.70f, tireCircumferenceM: TireCircumferenceM(285, 35, 20),
                wheelbaseM: 2.779f, trackWidthM: 1.600f, widthM: 1.895f, lengthM: 4.689f,
                cgHeightM: 0.52f, weightDistributionFront: 0.55f,
                corneringStiffnessFront: CorneringStiffness(1774f, 1.0f, 1.0f, 6.0f),
                corneringStiffnessRear: CorneringStiffness(1774f, 1.0f, 1.0f, 6.5f),
                maxSteerLowDeg: 32f, maxSteerHighDeg: 8f, steerSpeedKph: 90f,
                powerFactor: 0.7f, gearRatios: GtrRatios),

            // Vehicle 2: Racing car - very responsive, high-revving
            new VehicleParameters("Porsche 911 GT3 RS", null, null, null, null, null, null, null, null,
                hasWipers: 1, surfaceTractionFactor: 0.07f, deceleration: 0.45f, topSpeed: 312.0f,
                idleFreq: 22050, topFreq: 60000, shiftFreq: 35000, gears: 7, steering: 1.50f, steeringFactor: 55,
                idleRpm: 950f, maxRpm: 9000f, revLimiter: 8500f, autoShiftRpm: 8500f * 0.92f, engineBraking: 0.22f,
                massKg: 1450f, drivetrainEfficiency: 0.85f, engineBrakingTorqueNm: 465f, tireGripCoefficient: 1.05f,
                peakTorqueNm: 465f, peakTorqueRpm: 6250f, idleTorqueNm: 465f * 0.3f, redlineTorqueNm: 465f * 0.6f,
                dragCoefficient: 0.33f, frontalAreaM2: 2.0f, rollingResistanceCoefficient: 0.015f, launchRpm: 3000f,
                finalDriveRatio: 3.97f, tireCircumferenceM: TireCircumferenceM(325, 30, 21),
                wheelbaseM: 2.456f, trackWidthM: 1.577f, widthM: 1.852f, lengthM: 4.572f,
                cgHeightM: 0.49f, weightDistributionFront: 0.39f,
                corneringStiffnessFront: CorneringStiffness(1450f, 1.05f, 1.0f, 7.0f),
                corneringStiffnessRear: CorneringStiffness(1450f, 1.05f, 1.0f, 7.5f),
                maxSteerLowDeg: 30f, maxSteerHighDeg: 7f, steerSpeedKph: 90f,
                powerFactor: 0.75f, gearRatios: Gt3RsRatios),

            // Vehicle 3: Small car - slow acceleration, economical
            new VehicleParameters("Fiat 500", null, null, null, null, null, null, null, null,
                hasWipers: 1, surfaceTractionFactor: 0.035f, deceleration: 0.30f, topSpeed: 160.0f,
                idleFreq: 6000, topFreq: 25000, shiftFreq: 19000, gears: 5, steering: 1.50f, steeringFactor: 72,
                idleRpm: 750f, maxRpm: 6000f, revLimiter: 5500f, autoShiftRpm: 5500f * 0.92f, engineBraking: 0.40f,
                massKg: 865f, drivetrainEfficiency: 0.88f, engineBrakingTorqueNm: 102f, tireGripCoefficient: 0.88f,
                peakTorqueNm: 102f, peakTorqueRpm: 3000f, idleTorqueNm: 102f * 0.3f, redlineTorqueNm: 102f * 0.6f,
                dragCoefficient: 0.33f, frontalAreaM2: 2.1f, rollingResistanceCoefficient: 0.015f, launchRpm: 1800f,
                finalDriveRatio: 3.353f, tireCircumferenceM: TireCircumferenceM(195, 45, 16),
                wheelbaseM: 2.300f, trackWidthM: 1.402f, widthM: 1.627f, lengthM: 3.546f,
                cgHeightM: 0.52f, weightDistributionFront: 0.65f,
                corneringStiffnessFront: CorneringStiffness(865f, 0.88f, 1.0f, 4.5f),
                corneringStiffnessRear: CorneringStiffness(865f, 0.88f, 1.0f, 4.8f),
                maxSteerLowDeg: 38f, maxSteerHighDeg: 12f, steerSpeedKph: 70f,
                powerFactor: 0.35f, gearRatios: Fiat500Ratios),

            // Vehicle 4: Small sporty car - better than Fiat but not racing
            new VehicleParameters("Mini Cooper S", null, null, null, null, null, null, null, null,
                hasWipers: 1, surfaceTractionFactor: 0.045f, deceleration: 0.35f, topSpeed: 235.0f,
                idleFreq: 6000, topFreq: 27000, shiftFreq: 20000, gears: 6, steering: 1.40f, steeringFactor: 56,
                idleRpm: 800f, maxRpm: 6500f, revLimiter: 6000f, autoShiftRpm: 6000f * 0.92f, engineBraking: 0.32f,
                massKg: 1265f, drivetrainEfficiency: 0.88f, engineBrakingTorqueNm: 280f, tireGripCoefficient: 0.95f,
                peakTorqueNm: 280f, peakTorqueRpm: 1250f, idleTorqueNm: 280f * 0.3f, redlineTorqueNm: 280f * 0.6f,
                dragCoefficient: 0.33f, frontalAreaM2: 2.1f, rollingResistanceCoefficient: 0.015f, launchRpm: 2200f,
                finalDriveRatio: 3.59f, tireCircumferenceM: TireCircumferenceM(195, 55, 16),
                wheelbaseM: 2.494f, trackWidthM: 1.485f, widthM: 1.744f, lengthM: 3.876f,
                cgHeightM: 0.53f, weightDistributionFront: 0.60f,
                corneringStiffnessFront: CorneringStiffness(1265f, 0.95f, 1.0f, 5.0f),
                corneringStiffnessRear: CorneringStiffness(1265f, 0.95f, 1.0f, 5.3f),
                maxSteerLowDeg: 36f, maxSteerHighDeg: 11f, steerSpeedKph: 75f,
                powerFactor: 0.45f, gearRatios: MiniCooperSRatios),

            // Vehicle 5: Classic muscle car - torquey but heavy
            new VehicleParameters("Ford Mustang 1969", null, null, null, null, null, null, null, null,
                hasWipers: 1, surfaceTractionFactor: 0.04f, deceleration: 0.35f, topSpeed: 200.0f,
                idleFreq: 6000, topFreq: 33000, shiftFreq: 27500, gears: 4, steering: 2.30f, steeringFactor: 80,
                idleRpm: 650f, maxRpm: 5500f, revLimiter: 5000f, autoShiftRpm: 5000f * 0.92f, engineBraking: 0.35f,
                massKg: 1440f, drivetrainEfficiency: 0.85f, engineBrakingTorqueNm: 481f, tireGripCoefficient: 0.90f,
                peakTorqueNm: 481f, peakTorqueRpm: 3000f, idleTorqueNm: 481f * 0.3f, redlineTorqueNm: 481f * 0.6f,
                dragCoefficient: 0.45f, frontalAreaM2: 2.5f, rollingResistanceCoefficient: 0.018f, launchRpm: 2000f,
                finalDriveRatio: 3.25f, tireCircumferenceM: TireCircumferenceM(215, 70, 14),
                wheelbaseM: 2.743f, trackWidthM: 1.486f, widthM: 1.811f, lengthM: 4.760f,
                cgHeightM: 0.55f, weightDistributionFront: 0.55f,
                corneringStiffnessFront: CorneringStiffness(1440f, 0.90f, 1.0f, 4.7f),
                corneringStiffnessRear: CorneringStiffness(1440f, 0.90f, 1.0f, 5.1f),
                maxSteerLowDeg: 35f, maxSteerHighDeg: 9f, steerSpeedKph: 80f,
                powerFactor: 0.4f, gearRatios: Mustang69Ratios),

            // Vehicle 6: Common sedan - comfortable, not sporty
            new VehicleParameters("Toyota Camry", null, null, null, null, null, null, null, null,
                hasWipers: 1, surfaceTractionFactor: 0.035f, deceleration: 0.30f, topSpeed: 210.0f,
                idleFreq: 7025, topFreq: 40000, shiftFreq: 32500, gears: 8, steering: 2.20f, steeringFactor: 95,
                idleRpm: 700f, maxRpm: 6000f, revLimiter: 5500f, autoShiftRpm: 5500f * 0.92f, engineBraking: 0.38f,
                massKg: 1470f, drivetrainEfficiency: 0.88f, engineBrakingTorqueNm: 250f, tireGripCoefficient: 0.90f,
                peakTorqueNm: 250f, peakTorqueRpm: 5000f, idleTorqueNm: 250f * 0.3f, redlineTorqueNm: 250f * 0.6f,
                dragCoefficient: 0.29f, frontalAreaM2: 2.2f, rollingResistanceCoefficient: 0.015f, launchRpm: 2000f,
                finalDriveRatio: 2.80f, tireCircumferenceM: TireCircumferenceM(215, 55, 17),
                wheelbaseM: 2.825f, trackWidthM: 1.608f, widthM: 1.839f, lengthM: 4.879f,
                cgHeightM: 0.55f, weightDistributionFront: 0.60f,
                corneringStiffnessFront: CorneringStiffness(1470f, 0.90f, 1.0f, 4.8f),
                corneringStiffnessRear: CorneringStiffness(1470f, 0.90f, 1.0f, 5.0f),
                maxSteerLowDeg: 36f, maxSteerHighDeg: 9f, steerSpeedKph: 85f,
                powerFactor: 0.5f, gearRatios: CamryRatios),

            // Vehicle 7: Supercar - fastest acceleration, high power
            new VehicleParameters("Lamborghini Aventador", null, null, null, null, null, null, null, null,
                hasWipers: 1, surfaceTractionFactor: 0.08f, deceleration: 0.80f, topSpeed: 350.0f,
                idleFreq: 6000, topFreq: 26000, shiftFreq: 21000, gears: 7, steering: 2.10f, steeringFactor: 65,
                idleRpm: 1000f, maxRpm: 8500f, revLimiter: 8000f, autoShiftRpm: 8000f * 0.92f, engineBraking: 0.20f,
                massKg: 1640f, drivetrainEfficiency: 0.80f, engineBrakingTorqueNm: 720f, tireGripCoefficient: 1.05f,
                peakTorqueNm: 720f, peakTorqueRpm: 5500f, idleTorqueNm: 720f * 0.3f, redlineTorqueNm: 720f * 0.6f,
                dragCoefficient: 0.33f, frontalAreaM2: 2.0f, rollingResistanceCoefficient: 0.015f, launchRpm: 3000f,
                finalDriveRatio: 2.86f, tireCircumferenceM: TireCircumferenceM(355, 25, 21),
                wheelbaseM: 2.700f, trackWidthM: 1.710f, widthM: 2.030f, lengthM: 4.780f,
                cgHeightM: 0.48f, weightDistributionFront: 0.43f,
                corneringStiffnessFront: CorneringStiffness(1640f, 1.05f, 1.0f, 6.5f),
                corneringStiffnessRear: CorneringStiffness(1640f, 1.05f, 1.0f, 7.0f),
                maxSteerLowDeg: 30f, maxSteerHighDeg: 7f, steerSpeedKph: 100f,
                powerFactor: 0.8f, gearRatios: AventadorRatios),

            // Vehicle 8: Premium sedan - balanced performance
            new VehicleParameters("BMW 3 Series", null, null, null, null, null, null, null, null,
                hasWipers: 1, surfaceTractionFactor: 0.045f, deceleration: 0.40f, topSpeed: 250.0f,
                idleFreq: 10000, topFreq: 45000, shiftFreq: 34000, gears: 8, steering: 2.00f, steeringFactor: 70,
                idleRpm: 750f, maxRpm: 6500f, revLimiter: 6000f, autoShiftRpm: 6000f * 0.92f, engineBraking: 0.30f,
                massKg: 1524f, drivetrainEfficiency: 0.85f, engineBrakingTorqueNm: 346f, tireGripCoefficient: 0.93f,
                peakTorqueNm: 350f, peakTorqueRpm: 1250f, idleTorqueNm: 350f * 0.3f, redlineTorqueNm: 350f * 0.6f,
                dragCoefficient: 0.29f, frontalAreaM2: 2.2f, rollingResistanceCoefficient: 0.015f, launchRpm: 2000f,
                finalDriveRatio: 3.15f, tireCircumferenceM: TireCircumferenceM(225, 50, 17),
                wheelbaseM: 2.810f, trackWidthM: 1.597f, widthM: 1.811f, lengthM: 4.624f,
                cgHeightM: 0.55f, weightDistributionFront: 0.50f,
                corneringStiffnessFront: CorneringStiffness(1524f, 0.93f, 1.0f, 5.2f),
                corneringStiffnessRear: CorneringStiffness(1524f, 0.93f, 1.0f, 5.5f),
                maxSteerLowDeg: 35f, maxSteerHighDeg: 10f, steerSpeedKph: 85f,
                powerFactor: 0.55f, gearRatios: Bmw3SeriesRatios),

            // Vehicle 9: Bus/Van - very slow acceleration, heavy
            new VehicleParameters("Mercedes Sprinter", null, null, null, null, null, null, null, null,
                hasWipers: 1, surfaceTractionFactor: 0.02f, deceleration: 0.20f, topSpeed: 160.0f,
                idleFreq: 22050, topFreq: 30550, shiftFreq: 22550, gears: 7, steering: 1.50f, steeringFactor: 85,
                idleRpm: 600f, maxRpm: 4500f, revLimiter: 4000f, autoShiftRpm: 4000f * 0.92f, engineBraking: 0.45f,
                massKg: 1970f, drivetrainEfficiency: 0.85f, engineBrakingTorqueNm: 380f, tireGripCoefficient: 0.82f,
                peakTorqueNm: 440f, peakTorqueRpm: 1400f, idleTorqueNm: 440f * 0.3f, redlineTorqueNm: 440f * 0.6f,
                dragCoefficient: 0.35f, frontalAreaM2: 2.9f, rollingResistanceCoefficient: 0.020f, launchRpm: 1800f,
                finalDriveRatio: 3.923f, tireCircumferenceM: TireCircumferenceM(245, 75, 16),
                wheelbaseM: 3.658f, trackWidthM: 1.720f, widthM: 2.019f, lengthM: 5.931f,
                cgHeightM: 0.75f, weightDistributionFront: 0.55f,
                corneringStiffnessFront: CorneringStiffness(1970f, 0.82f, 1.0f, 3.8f),
                corneringStiffnessRear: CorneringStiffness(1970f, 0.82f, 1.0f, 4.0f),
                maxSteerLowDeg: 40f, maxSteerHighDeg: 9f, steerSpeedKph: 75f,
                powerFactor: 0.3f, gearRatios: SprinterRatios),

            // Vehicle 10: Sport motorcycle - quick, light, high-revving
            new VehicleParameters("Kawasaki Ninja ZX-10R", null, null, null, null, null, null, null, null,
                hasWipers: 0, surfaceTractionFactor: 0.09f, deceleration: 0.50f, topSpeed: 299.0f,
                idleFreq: 22050, topFreq: 60000, shiftFreq: 35000, gears: 6, steering: 1.40f, steeringFactor: 50,
                idleRpm: 1100f, maxRpm: 14000f, revLimiter: 13500f, autoShiftRpm: 13500f * 0.92f, engineBraking: 0.28f,
                massKg: 207f, drivetrainEfficiency: 0.92f, engineBrakingTorqueNm: 114.9f, tireGripCoefficient: 1.10f,
                peakTorqueNm: 114.9f, peakTorqueRpm: 11500f, idleTorqueNm: 114.9f * 0.3f, redlineTorqueNm: 114.9f * 0.6f,
                dragCoefficient: 0.58f, frontalAreaM2: 0.6f, rollingResistanceCoefficient: 0.016f, launchRpm: 4000f,
                finalDriveRatio: 3.8562f, tireCircumferenceM: TireCircumferenceM(190, 55, 17),
                lateralGripCoefficient: 0.80f, highSpeedStability: 0.25f,
                wheelbaseM: 1.450f, widthM: 0.749f, lengthM: 2.085f,
                cgHeightM: 0.55f, weightDistributionFront: 0.50f,
                corneringStiffnessFront: CorneringStiffness(207f, 1.10f, 0.80f, 5.5f),
                corneringStiffnessRear: CorneringStiffness(207f, 1.10f, 0.80f, 5.8f),
                steerInputRate: 1.1f, steerReturnRate: 0.8f, steerGamma: 1.7f,
                maxSteerLowDeg: 32f, maxSteerHighDeg: 8f, steerSpeedKph: 55f, steerSpeedExponent: 1.4f,
                dynamicsModel: VehicleDynamicsModel.Bicycle,
                powerFactor: 0.85f, gearRatios: Zx10rRatios),

            // Vehicle 11: Superbike - fastest motorcycle
            new VehicleParameters("Ducati Panigale V4", null, null, null, null, null, null, null, null,
                hasWipers: 0, surfaceTractionFactor: 0.10f, deceleration: 0.55f, topSpeed: 310.0f,
                idleFreq: 22050, topFreq: 60000, shiftFreq: 35000, gears: 6, steering: 1.30f, steeringFactor: 50,
                idleRpm: 1200f, maxRpm: 15000f, revLimiter: 14500f, autoShiftRpm: 14500f * 0.92f, engineBraking: 0.25f,
                massKg: 191f, drivetrainEfficiency: 0.92f, engineBrakingTorqueNm: 121f, tireGripCoefficient: 1.12f,
                peakTorqueNm: 121f, peakTorqueRpm: 10000f, idleTorqueNm: 121f * 0.3f, redlineTorqueNm: 121f * 0.6f,
                dragCoefficient: 0.55f, frontalAreaM2: 0.6f, rollingResistanceCoefficient: 0.016f, launchRpm: 4000f,
                finalDriveRatio: 4.6125f, tireCircumferenceM: TireCircumferenceM(200, 60, 17),
                lateralGripCoefficient: 0.80f, highSpeedStability: 0.25f,
                wheelbaseM: 1.469f, widthM: 0.806f, lengthM: 2.110f,
                cgHeightM: 0.55f, weightDistributionFront: 0.50f,
                corneringStiffnessFront: CorneringStiffness(191f, 1.12f, 0.80f, 5.6f),
                corneringStiffnessRear: CorneringStiffness(191f, 1.12f, 0.80f, 5.9f),
                steerInputRate: 1.1f, steerReturnRate: 0.8f, steerGamma: 1.7f,
                maxSteerLowDeg: 32f, maxSteerHighDeg: 8f, steerSpeedKph: 55f, steerSpeedExponent: 1.4f,
                dynamicsModel: VehicleDynamicsModel.Bicycle,
                powerFactor: 0.9f, gearRatios: PanigaleV4Ratios),

            // Vehicle 12: Sport motorcycle - balanced
            new VehicleParameters("Yamaha YZF-R1", null, null, null, null, null, null, null, null,
                hasWipers: 0, surfaceTractionFactor: 0.085f, deceleration: 0.48f, topSpeed: 299.0f,
                idleFreq: 22050, topFreq: 27550, shiftFreq: 23550, gears: 6, steering: 1.50f, steeringFactor: 66,
                idleRpm: 1100f, maxRpm: 14500f, revLimiter: 14000f, autoShiftRpm: 14000f * 0.92f, engineBraking: 0.30f,
                massKg: 201f, drivetrainEfficiency: 0.92f, engineBrakingTorqueNm: 113.3f, tireGripCoefficient: 1.10f,
                peakTorqueNm: 112.4f, peakTorqueRpm: 11500f, idleTorqueNm: 112.4f * 0.3f, redlineTorqueNm: 112.4f * 0.6f,
                dragCoefficient: 0.55f, frontalAreaM2: 0.6f, rollingResistanceCoefficient: 0.016f, launchRpm: 4000f,
                finalDriveRatio: 4.1807f, tireCircumferenceM: TireCircumferenceM(190, 55, 17),
                lateralGripCoefficient: 0.80f, highSpeedStability: 0.25f,
                wheelbaseM: 1.405f, widthM: 0.690f, lengthM: 2.055f,
                cgHeightM: 0.55f, weightDistributionFront: 0.50f,
                corneringStiffnessFront: CorneringStiffness(201f, 1.10f, 0.80f, 5.5f),
                corneringStiffnessRear: CorneringStiffness(201f, 1.10f, 0.80f, 5.8f),
                steerInputRate: 1.1f, steerReturnRate: 0.8f, steerGamma: 1.7f,
                maxSteerLowDeg: 32f, maxSteerHighDeg: 8f, steerSpeedKph: 55f, steerSpeedExponent: 1.4f,
                dynamicsModel: VehicleDynamicsModel.Bicycle,
                powerFactor: 0.8f, gearRatios: R1Ratios)
        };
    }
}

