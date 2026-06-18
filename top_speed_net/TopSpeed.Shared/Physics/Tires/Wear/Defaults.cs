namespace TopSpeed.Physics.Tires.Wear
{
    public static class TireWearDefaults
    {
        // Balanced compound, three-node cascade. Tuned against issue #84 with
        // the simulator at /home/ubuntu/tire_sim/sim.py at 26 °C ambient/road:
        //  - Warm-up superspeedway lap (avg ~150 mph banked oval) cold→82 °C: 5.4 mi
        //  - Cruise 100 mph steady (low slip, low load):                      82.4 °C
        //  - Cruise 160 mph steady:                                           92.4 °C
        //  - Cruise 200 mph steady:                                           96.3 °C
        //  - Race cruise drift over 10 min hold:                              +0.8 °C
        //  - Austria hairpin peak (10 s of cornering on warm tire):           113.7 °C
        //  - 14 s straight recovery after a hairpin spike:                    −9.9 °C
        //  - 85 % wear vs 10 % wear corner spike:                             +14.6 °C
        //  - 95 % wear 5-min straight cruise:                                 215 °C (overheat)
        //  - Cold 10 °C ambient / 8 °C road cruise:                           66.1 °C (depressed)
        //  - Hot 30 °C ambient / 50 °C road cruise:                           89.3 °C (elevated)
        //
        // Three-node time constants:
        //  - τ_corner   ≈ 4 s   — surface heats up in seconds in a corner
        //  - τ_recovery ≈ 17 s  — spike fade on the next straight
        //  - τ_warmup   ≈ 285 s — bulk soak from cold to operating temperature
        public static TireWearConfig Balanced { get; } = new TireWearConfig
        {
            BaseWearPerKilometer = 0.002f,
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
            GripAtColdEnd = 0.9f,
            GripAtOptimal = 1.0f,
            GripAtOverheatEnd = 0.80f,
            GripAtCooked = 0.5f,
            GripAtFullWear = 0.5f,
            CorneringHeatCPerSecond = 0.095f,
            AccelerationHeatCPerSecond = 0.10f,
            // Braking is the worse offender: more heat than acceleration. The
            // surface/tread split is a global shape constant in the heat model
            // (BrakeSurfaceHeatFraction). Starting estimate — re-tune against
            // the thermal spec once driving feel is checked.
            BrakeHeatCPerSecond = 0.9f,
            LoadHeatCPerSecond = 0.040f,
            RollingHeatCPerSecond = 0.018f,
            AirflowCoolingPerMpsPerCPerSecond = 0.00165f,
            AmbientExchangePerCPerSecond = 0.0105f,
            RoadExchangePerCPerSecond = 0.0135f,
            WetRoadExchangePerCPerSecond = 0.0150f,
            SurfaceToTreadConductancePerSecond = 0.48f,
            TreadToCarcassConductancePerSecond = 0.160f,
            TreadMassRatio = 0.6f,
            CarcassMassRatio = 2.4f,
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
