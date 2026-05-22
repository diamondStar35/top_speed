namespace TopSpeed.Physics.Tires.Wear
{
    public static class TireWearDefaults
    {
        // Balanced compound. Two-node model tuned against issue #84 with the
        // simulator at /home/ubuntu/tire_sim.py. 26 °C ambient + road reference:
        //  - 100 mph cruise (load=0.30, slip=0.05):  T_s_eq ≈ 92 °C (199 °F)
        //  - 160 mph cruise:                           T_s_eq ≈ 99 °C (211 °F)
        //  - 200 mph cruise:                           T_s_eq ≈ 102 °C (216 °F)
        //  - Superspeedway 4 s turn @ 175 mph spike:   T_s   ≈ 105 °C (221 °F)
        //  - Austria hairpin peak (10 s of cornering): T_s   ≈ 108 °C (227 °F)
        //  - Eas-left straight recovery (14 s):        T_s   ≈ 99 °C (210 °F)
        //  - Warm-up from 30 °C @ 100 mph to 82 °C:    ~7.4 mi
        //  - 95% wear straight cruise (5 min):         T_s   ≈ 190 °C (overheat)
        //  - Cold 10 °C ambient vs 26 °C reference:    −8.7 °C
        //  - Hot road 50 °C vs 26 °C reference:       +7.0 °C
        //
        // Surface time constant τ_fast ≈ 7 s (fast spike recovery), carcass
        // τ_slow ≈ 160 s (slow warm-up so the cold tire spends 5–10 mi getting
        // into the optimal band at highway speeds).
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
            InternalConductancePerSecond = 0.08f,
            CarcassMassRatio = 2.0f,
            SlipSmoothingTimeConstantSeconds = 1.4f,
        };

        // Cold tire: surface and carcass both start at the same ambient-blended
        // temperature. The carcass will track the surface lazily once heat input
        // begins.
        public static TireWearState CreateInitialState(float temperatureC)
        {
            return new TireWearState(0f, temperatureC, temperatureC, 0f);
        }
    }
}
