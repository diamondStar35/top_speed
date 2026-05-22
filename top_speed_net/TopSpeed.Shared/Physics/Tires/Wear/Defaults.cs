namespace TopSpeed.Physics.Tires.Wear
{
    public static class TireWearDefaults
    {
        // Balanced compound, 26 °C ambient/road reference:
        //  - 100 mph cruise (load=0.30, slip=0.05): T_eq ≈ 92 °C (198 °F)
        //  - 160 mph cruise:                          T_eq ≈ 99 °C (210 °F)
        //  - Superspeedway turn peaks:                T   ≈ 100 °C (212 °F)
        //  - Austria hairpin peaks:                   T   ≈ 115 °C (239 °F)
        //  - Warm-up from 30 °C @ 100 mph to 82 °C:   ~2 mi
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
            SlipSmoothingTimeConstantSeconds = 1.4f,
        };

        public static TireWearState CreateInitialState(float temperatureC)
        {
            return new TireWearState(0f, temperatureC, 0f);
        }
    }
}
