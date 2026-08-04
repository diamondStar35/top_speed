namespace TopSpeed.Vehicles.Parsing
{
    public static partial class VehicleTsvParser
    {
        private static readonly string[] s_requiredSections =
        {
            "meta", "sounds", "general", "engine", "torque", "engine_rot", "resistance", "torque_curve", "transmission", "drivetrain", "gears", "steering", "tire_model", "dynamics", "dimensions", "tires"
        };
    }
}

