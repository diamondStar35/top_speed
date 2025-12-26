using TopSpeed.Protocol;

namespace TopSpeed.Vehicles
{
    internal sealed class VehicleDefinition
    {
        public CarType CarType { get; set; }
        public bool UserDefined { get; set; }
        public string? CustomFile { get; set; }
        public int Acceleration { get; set; }
        public int Deceleration { get; set; }
        public int TopSpeed { get; set; }
        public int IdleFreq { get; set; }
        public int TopFreq { get; set; }
        public int ShiftFreq { get; set; }
        public int Gears { get; set; }
        public int Steering { get; set; }
        public int SteeringFactor { get; set; }
        public int HasWipers { get; set; }
        public string? EngineSound { get; set; }
        public string? StartSound { get; set; }
        public string? HornSound { get; set; }
        public string? ThrottleSound { get; set; }
        public string? CrashSound { get; set; }
        public string? BrakeSound { get; set; }
        public string? BackfireSound { get; set; }
    }
}
