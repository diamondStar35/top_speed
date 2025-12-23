namespace TopSpeed.Data
{
    internal sealed class VehicleParameters
    {
        public string? EngineSound { get; }
        public string? StartSound { get; }
        public string? HornSound { get; }
        public string? ThrottleSound { get; }
        public string? CrashSound { get; }
        public string? MonoCrashSound { get; }
        public string? BrakeSound { get; }
        public string? BackfireSound { get; }
        public int HasWipers { get; }
        public int Acceleration { get; }
        public int Deceleration { get; }
        public int TopSpeed { get; }
        public int IdleFreq { get; }
        public int TopFreq { get; }
        public int ShiftFreq { get; }
        public int Gears { get; }
        public int Steering { get; }
        public int SteeringFactor { get; }

        public VehicleParameters(
            string? engineSound,
            string? startSound,
            string? hornSound,
            string? throttleSound,
            string? crashSound,
            string? monoCrashSound,
            string? brakeSound,
            string? backfireSound,
            int hasWipers,
            int acceleration,
            int deceleration,
            int topSpeed,
            int idleFreq,
            int topFreq,
            int shiftFreq,
            int gears,
            int steering,
            int steeringFactor)
        {
            EngineSound = engineSound;
            StartSound = startSound;
            HornSound = hornSound;
            ThrottleSound = throttleSound;
            CrashSound = crashSound;
            MonoCrashSound = monoCrashSound;
            BrakeSound = brakeSound;
            BackfireSound = backfireSound;
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
        }
    }

    internal static class VehicleCatalog
    {
        public const int VehicleCount = 12;

        public static readonly VehicleParameters[] Vehicles =
        {
            new VehicleParameters("vehicle1_e.wav", "vehicle1_s.wav", "vehicle1_h.wav", "vehicle1_t.wav", "vehicle1_c.wav", "vehicle1_cm.wav", "vehicle1_b.wav", null, 1, 11, 40, 17500, 22050, 55000, 26000, 5, 160, 60),
            new VehicleParameters("vehicle2_e.wav", "vehicle2_s.wav", "vehicle2_h.wav", "vehicle2_t.wav", "vehicle1_c.wav", "vehicle1_cm.wav", "vehicle1_b.wav", null, 1, 13, 35, 18500, 22050, 60000, 35000, 5, 150, 55),
            new VehicleParameters("vehicle3_e.wav", "vehicle1_s.wav", "vehicle3_h.wav", null, "vehicle3_c.wav", "vehicle3_cm.wav", "vehicle3_b.wav", null, 1, 10, 35, 15100, 6000, 25000, 19000, 4, 150, 72),
            new VehicleParameters("vehicle4_e.wav", "vehicle1_s.wav", "vehicle4_h.wav", null, "vehicle3_c.wav", "vehicle3_cm.wav", "vehicle3_b.wav", null, 1, 12, 40, 17200, 6000, 27000, 20000, 6, 140, 56),
            new VehicleParameters("vehicle5_e.wav", "vehicle1_s.wav", "vehicle5_h.wav", null, "vehicle1_c.wav", "vehicle1_cm.wav", "vehicle1_b.wav", null, 1, 12, 60, 24000, 6000, 33000, 27500, 4, 230, 80),
            new VehicleParameters("vehicle6_e.wav", "vehicle1_s.wav", "vehicle6_h.wav", null, "vehicle1_c.wav", "vehicle1_cm.wav", "vehicle6_b.wav", null, 1, 9, 90, 26000, 7025, 40000, 32500, 6, 220, 95),
            new VehicleParameters("vehicle7_e.wav", "vehicle1_s.wav", "vehicle3_h.wav", null, "vehicle1_c.wav", "vehicle1_cm.wav", "vehicle3_b.wav", null, 1, 13, 70, 21000, 6000, 26000, 21000, 5, 210, 65),
            new VehicleParameters("vehicle8_e.wav", "vehicle1_s.wav", "vehicle6_h.wav", null, "vehicle1_c.wav", "vehicle1_cm.wav", "vehicle6_b.wav", null, 1, 11, 55, 23000, 10000, 45000, 34000, 5, 200, 70),
            new VehicleParameters("vehicle9_e.wav", "vehicle9_s.wav", "vehicle9_h.wav", "vehicle9_t.wav", "vehicle9_c.wav", "vehicle9_cm.wav", "vehicle9_b.wav", "vehicle9_f.wav", 1, 8, 25, 18000, 22050, 30550, 22550, 5, 150, 85),
            new VehicleParameters("vehicle10_e.wav", "vehicle10_s.wav", "vehicle10_h.wav", null, "vehicle10_c.wav", "vehicle10_cm.wav", "vehicle1_b.wav", null, 0, 15, 45, 20000, 22050, 60000, 35000, 5, 140, 50),
            new VehicleParameters("vehicle11_e.wav", "vehicle11_s.wav", "vehicle10_h.wav", null, "vehicle10_c.wav", "vehicle10_cm.wav", "vehicle1_b.wav", null, 0, 17, 40, 22000, 22050, 60000, 35000, 5, 130, 50),
            new VehicleParameters("vehicle12_e.wav", "vehicle12_s.wav", "vehicle12_h.wav", "vehicle12_t.wav", "vehicle10_c.wav", "vehicle10_cm.wav", "vehicle1_b.wav", "vehicle12_f.wav", 0, 13, 45, 24000, 22050, 27550, 23550, 5, 150, 66)
        };
    }
}
