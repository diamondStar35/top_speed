namespace TopSpeed.Vehicles
{
    /// <summary>
    /// Per-race switches for the optional physics models. Resolved at race start from the
    /// single-player <c>DriveSettings</c> or the multiplayer effective race-instance rules, and
    /// threaded into the <see cref="Car"/>. Both default on; turning one off short-circuits its
    /// update so wear stays at zero / grip stays neutral and fuel stays full.
    /// </summary>
    internal readonly struct RacePhysicsToggles
    {
        public RacePhysicsToggles(bool tireWearEnabled, bool fuelConsumptionEnabled)
        {
            TireWearEnabled = tireWearEnabled;
            FuelConsumptionEnabled = fuelConsumptionEnabled;
        }

        public bool TireWearEnabled { get; }

        public bool FuelConsumptionEnabled { get; }

        public static RacePhysicsToggles AllEnabled => new RacePhysicsToggles(true, true);
    }
}
