namespace TopSpeed.Physics.Tires.Wear
{
    public static class TireWearDefaults
    {
        public static TireWearConfig Balanced { get; } = new TireWearConfig(
            baseWearPerKilometer: 0.0024f,
            slipWearRatePerSecond: 0.00025f,
            corneringSlipWearWeight: 0.48f,
            longitudinalSlipWearWeight: 0.62f,
            loadWearGain: 0.95f,
            wearHotStartTemperatureC: 102f,
            wearHotGainPerC: 0.020f,
            wearColdStartTemperatureC: 34f,
            wearColdGainPerC: 0.005f,
            ambientTemperatureC: 22f,
            coldEndTemperatureC: 50f,
            optimalStartTemperatureC: 82f,
            optimalEndTemperatureC: 128f,
            overheatEndTemperatureC: 140f,
            gripAtVeryCold: 0.72f,
            gripAtColdEnd: 0.94f,
            gripAtOptimal: 1.0f,
            gripAtOverheatEnd: 0.80f,
            gripAtCooked: 0.65f,
            gripAtFullWear: 0.78f,
            corneringHeatCPerSecond: 1.35f,
            longitudinalHeatCPerSecond: 1.20f,
            loadHeatCPerSecond: 0.40f,
            rollingHeatCPerSecond: 0.20f,
            airflowCoolingPerMpsPerCPerSecond: 0.000072f,
            ambientExchangePerCPerSecond: 0.0021f,
            roadExchangePerCPerSecond: 0.0038f,
            wetRoadExchangePerCPerSecond: 0.0062f,
            slipSmoothingTimeConstantSeconds: 1.4f);

        public static TireWearState CreateInitialState(float temperatureC)
        {
            return new TireWearState(0f, temperatureC, 0f);
        }
    }
}
