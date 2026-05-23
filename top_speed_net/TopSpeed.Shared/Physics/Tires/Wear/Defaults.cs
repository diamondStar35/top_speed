namespace TopSpeed.Physics.Tires.Wear
{
    public static class TireWearDefaults
    {
        // Balanced compound, three-node cascade. Tuned against issue #84 with
        // the simulator at /home/ubuntu/tire_sim/sim.py at 26 °C ambient/road:
        //  - Warm-up superspeedway lap (avg ~150 mph banked oval) cold→82 °C: 4.6 mi
        //  - Cruise 100 mph steady (low slip, low load):                      85.6 °C
        //  - Cruise 160 mph steady:                                           94.9 °C
        //  - Cruise 200 mph steady:                                           98.4 °C
        //  - Race cruise drift over 10 min hold:                              +0.7 °C
        //  - Austria hairpin peak (10 s of cornering on warm tire):           115.9 °C
        //  - 14 s straight recovery after a hairpin spike:                    −10.1 °C
        //  - 85 % wear vs 10 % wear corner spike:                             +14.6 °C
        //  - 95 % wear 5-min straight cruise:                                 226 °C (overheat)
        //  - Cold 10 °C ambient / 8 °C road cruise:                           69.2 °C (depressed)
        //  - Hot 30 °C ambient / 50 °C road cruise:                           92.6 °C (elevated)
        //
        // Three-node time constants:
        //  - τ_corner   ≈ 4 s   — surface heats up in seconds in a corner
        //  - τ_recovery ≈ 17 s  — spike fade on the next straight
        //  - τ_warmup   ≈ 240 s — bulk soak from cold to operating temperature
        public static TireWearConfig Balanced { get; } = new TireWearConfig
        {
            BaseWearPerKilometer = 0.0024f,
            SlipWearRatePerSecond = 0.00025f,
            CorneringSlipWearWeight = 0.48f,
            LongitudinalSlipWearWeight = 0.62f,
            LoadWearGain = 0.95f,
            WearHotStartTemperatureC = 102f,
            WearHotGainPerC = 0.020f,
            WearColdStartTemperatureC = 34f,
            WearColdGainPerC = 0.005f,
            ColdEndTemperatureC = 50f,
            OptimalStartTemperatureC = 82f,
            OptimalEndTemperatureC = 128f,
            OverheatEndTemperatureC = 140f,
            GripAtVeryCold = 0.72f,
            GripAtColdEnd = 0.94f,
            GripAtOptimal = 1.0f,
            GripAtOverheatEnd = 0.80f,
            GripAtCooked = 0.65f,
            GripAtFullWear = 0.78f,
            CorneringHeatCPerSecond = 0.16f,
            LongitudinalHeatCPerSecond = 0.24f,
            LoadHeatCPerSecond = 0.040f,
            RollingHeatCPerSecond = 0.018f,
            AirflowCoolingPerMpsPerCPerSecond = 0.00035f,
            AmbientExchangePerCPerSecond = 0.0022f,
            RoadExchangePerCPerSecond = 0.0030f,
            WetRoadExchangePerCPerSecond = 0.0050f,
            SurfaceToTreadConductancePerSecond = 0.16f,
            TreadToCarcassConductancePerSecond = 0.040f,
            TreadMassRatio = 1.0f,
            CarcassMassRatio = 3.5f,
            SlipSmoothingTimeConstantSeconds = 1.4f,
        };

        // Cold tire: surface, tread, and carcass all start at the same
        // ambient-blended temperature. The cascade will warm them in
        // staggered fashion once heat input begins.
        public static TireWearState CreateInitialState(float temperatureC)
        {
            return new TireWearState(0f, temperatureC, temperatureC, temperatureC, 0f);
        }
    }
}
