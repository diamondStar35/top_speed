using System;

namespace TopSpeed.Physics.Tires.Wear
{
    public static class TireWearProfiles
    {
        public static TireWearConfig Balanced => TireWearDefaults.Balanced;

        public static TireWearConfig CreateFromVehicle(
            float tireGripCoefficient,
            float massKg,
            float tireCircumferenceM,
            float lateralGripCoefficient)
        {
            var gripNorm = Clamp01((tireGripCoefficient - 0.80f) / 0.40f);
            var lateralNorm = Clamp01((lateralGripCoefficient - 0.80f) / 0.45f);
            var compoundAggression = Clamp01((gripNorm * 0.65f) + (lateralNorm * 0.35f));
            var massNorm = Clamp01((massKg - 350f) / 1850f);
            var sizeNorm = Clamp01((tireCircumferenceM - 1.6f) / 0.8f);

            var baseWearPerKilometer = Clamp(
                0.0010f + (0.0011f * compoundAggression) + (0.0007f * massNorm) - (0.0003f * sizeNorm),
                0.0007f,
                0.0045f);
            var slipWearRatePerSecond = Clamp(0.00011f + (0.00010f * compoundAggression) + (0.00004f * massNorm), 0.00006f, 0.00035f);
            var corneringSlipWearWeight = Clamp(0.38f + (0.14f * compoundAggression), 0.24f, 0.62f);
            var longitudinalSlipWearWeight = Clamp(0.62f + (0.08f * compoundAggression), 0.45f, 0.86f);
            var loadWearGain = Clamp(0.58f + (0.75f * massNorm), 0.45f, 1.55f);
            var wearHotStartTemperatureC = Clamp(70f + (8f * compoundAggression), 64f, 86f);
            var wearHotGainPerC = Clamp(0.020f + (0.016f * compoundAggression), 0.012f, 0.060f);
            var wearColdStartTemperatureC = Clamp(22f - (4f * compoundAggression), 12f, 28f);
            var wearColdGainPerC = Clamp(0.005f + (0.004f * (1f - compoundAggression)), 0.002f, 0.020f);
            var coldEndTemperatureC = Clamp(32f - (4f * compoundAggression), 24f, 36f);
            var optimalStartTemperatureC = Clamp(50f + (10f * compoundAggression), 44f, 66f);
            var optimalEndTemperatureC = Clamp(74f + (10f * compoundAggression), 66f, 90f);
            var overheatEndTemperatureC = Clamp(108f + (12f * compoundAggression), 96f, 130f);
            var gripAtVeryCold = Clamp(0.78f - (0.05f * compoundAggression), 0.64f, 0.86f);
            var gripAtColdEnd = Clamp(0.95f - (0.03f * compoundAggression), 0.82f, 0.98f);
            var gripAtOptimal = 1.0f;
            var gripAtOverheatEnd = Clamp(0.77f - (0.08f * compoundAggression), 0.58f, 0.85f);
            var gripAtCooked = Clamp(0.67f - (0.06f * compoundAggression), 0.50f, 0.75f);
            var gripAtFullWear = Clamp(0.81f - (0.12f * compoundAggression) + (0.03f * sizeNorm), 0.55f, 0.90f);
            var corneringHeatCPerSecond = Clamp(0.72f + (0.45f * compoundAggression) + (0.28f * massNorm), 0.45f, 1.65f);
            var longitudinalHeatCPerSecond = Clamp(0.64f + (0.42f * compoundAggression) + (0.25f * massNorm), 0.40f, 1.50f);
            var loadHeatCPerSecond = Clamp(0.14f + (0.22f * massNorm), 0.08f, 0.48f);
            var rollingHeatCPerSecond = Clamp(0.07f + (0.10f * massNorm) + (0.04f * (1f - sizeNorm)), 0.05f, 0.28f);
            var airflowCoolingPerMpsPerCPerSecond = Clamp(0.00007f + (0.00005f * (1f - compoundAggression)) + (0.00003f * sizeNorm), 0.00004f, 0.00020f);
            var ambientExchangePerCPerSecond = Clamp(0.0022f + (0.0012f * (1f - compoundAggression)), 0.0016f, 0.0048f);
            var roadExchangePerCPerSecond = Clamp(0.0042f + (0.0018f * (1f - compoundAggression)) + (0.0010f * (1f - sizeNorm)), 0.0028f, 0.0085f);
            var wetRoadExchangePerCPerSecond = Clamp(0.0064f + (0.0032f * compoundAggression) + (0.0016f * (1f - sizeNorm)), 0.0048f, 0.0140f);
            var slipSmoothingTau = Clamp(1.8f - (0.75f * compoundAggression), 0.6f, 2.6f);

            return new TireWearConfig(
                baseWearPerKilometer: baseWearPerKilometer,
                slipWearRatePerSecond: slipWearRatePerSecond,
                corneringSlipWearWeight: corneringSlipWearWeight,
                longitudinalSlipWearWeight: longitudinalSlipWearWeight,
                loadWearGain: loadWearGain,
                wearHotStartTemperatureC: wearHotStartTemperatureC,
                wearHotGainPerC: wearHotGainPerC,
                wearColdStartTemperatureC: wearColdStartTemperatureC,
                wearColdGainPerC: wearColdGainPerC,
                ambientTemperatureC: 22f,
                coldEndTemperatureC: coldEndTemperatureC,
                optimalStartTemperatureC: optimalStartTemperatureC,
                optimalEndTemperatureC: optimalEndTemperatureC,
                overheatEndTemperatureC: overheatEndTemperatureC,
                gripAtVeryCold: gripAtVeryCold,
                gripAtColdEnd: gripAtColdEnd,
                gripAtOptimal: gripAtOptimal,
                gripAtOverheatEnd: gripAtOverheatEnd,
                gripAtCooked: gripAtCooked,
                gripAtFullWear: gripAtFullWear,
                corneringHeatCPerSecond: corneringHeatCPerSecond,
                longitudinalHeatCPerSecond: longitudinalHeatCPerSecond,
                loadHeatCPerSecond: loadHeatCPerSecond,
                rollingHeatCPerSecond: rollingHeatCPerSecond,
                airflowCoolingPerMpsPerCPerSecond: airflowCoolingPerMpsPerCPerSecond,
                ambientExchangePerCPerSecond: ambientExchangePerCPerSecond,
                roadExchangePerCPerSecond: roadExchangePerCPerSecond,
                wetRoadExchangePerCPerSecond: wetRoadExchangePerCPerSecond,
                slipSmoothingTimeConstantSeconds: slipSmoothingTau);
        }

        private static float Clamp01(float value)
        {
            return Clamp(value, 0f, 1f);
        }

        private static float Clamp(float value, float min, float max)
        {
            if (float.IsNaN(value) || float.IsInfinity(value))
                return min;
            if (value < min)
                return min;
            if (value > max)
                return max;
            return value;
        }
    }
}
