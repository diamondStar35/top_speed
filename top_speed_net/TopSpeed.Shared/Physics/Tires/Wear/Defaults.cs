namespace TopSpeed.Physics.Tires.Wear
{
    public static class TireWearDefaults
    {
        public static TireWearConfig Balanced { get; } = new TireWearConfig(
            baseWearPerKilometer: 0.0018f,
            slipWearRatePerSecond: 0.00018f,
            corneringSlipWearWeight: 0.45f,
            longitudinalSlipWearWeight: 0.55f,
            loadWearGain: 0.85f,
            wearHotStartTemperatureC: 74f,
            wearHotGainPerC: 0.026f,
            wearColdStartTemperatureC: 22f,
            wearColdGainPerC: 0.007f,
            ambientTemperatureC: 22f,
            coldEndTemperatureC: 32f,
            optimalStartTemperatureC: 54f,
            optimalEndTemperatureC: 78f,
            overheatEndTemperatureC: 112f,
            gripAtVeryCold: 0.78f,
            gripAtColdEnd: 0.95f,
            gripAtOptimal: 1.0f,
            gripAtOverheatEnd: 0.76f,
            gripAtCooked: 0.66f,
            gripAtFullWear: 0.78f,
            corneringHeatCPerSecond: 0.95f,
            longitudinalHeatCPerSecond: 0.85f,
            loadHeatCPerSecond: 0.22f,
            rollingHeatCPerSecond: 0.12f,
            airflowCoolingPerMpsPerCPerSecond: 0.00011f,
            ambientExchangePerCPerSecond: 0.0028f,
            roadExchangePerCPerSecond: 0.0050f,
            wetRoadExchangePerCPerSecond: 0.0070f,
            slipSmoothingTimeConstantSeconds: 1.6f);

        public static TireWearState CreateInitialState(float temperatureC)
        {
            return new TireWearState(0f, temperatureC, 0f);
        }
    }
}
