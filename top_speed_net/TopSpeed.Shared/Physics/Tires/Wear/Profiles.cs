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
                0.0022f + (0.0019f * compoundAggression) + (0.0012f * massNorm) - (0.0003f * sizeNorm),
                0.0016f,
                0.0075f);
            var slipWearRatePerSecond = Clamp(0.00024f + (0.00022f * compoundAggression) + (0.00010f * massNorm), 0.00014f, 0.00072f);
            var corneringSlipWearWeight = Clamp(0.38f + (0.14f * compoundAggression), 0.24f, 0.62f);
            var longitudinalSlipWearWeight = Clamp(0.62f + (0.08f * compoundAggression), 0.45f, 0.86f);
            var loadWearGain = Clamp(0.72f + (0.95f * massNorm), 0.60f, 1.95f);
            var wearHotStartTemperatureC = Clamp(102f + (7f * compoundAggression), 96f, 116f);
            var wearHotGainPerC = Clamp(0.014f + (0.016f * compoundAggression), 0.008f, 0.045f);
            var wearColdStartTemperatureC = Clamp(40f - (6f * compoundAggression), 30f, 46f);
            var wearColdGainPerC = Clamp(0.0025f + (0.0025f * (1f - compoundAggression)), 0.001f, 0.009f);
            var coldEndTemperatureC = Clamp(54f - (10f * compoundAggression), 46f, 60f);
            var optimalStartTemperatureC = Clamp(81f + (7f * compoundAggression), 79f, 90f);
            var optimalEndTemperatureC = Clamp(126f + (10f * compoundAggression), 122f, 136f);
            var overheatEndTemperatureC = Clamp(139f + (11f * compoundAggression), 136f, 148f);
            var gripAtVeryCold = Clamp(0.74f - (0.05f * compoundAggression), 0.60f, 0.82f);
            var gripAtColdEnd = Clamp(0.93f - (0.03f * compoundAggression), 0.80f, 0.97f);
            var gripAtOptimal = 1.0f;
            var gripAtOverheatEnd = Clamp(0.79f - (0.08f * compoundAggression), 0.58f, 0.86f);
            var gripAtCooked = Clamp(0.64f - (0.06f * compoundAggression), 0.48f, 0.74f);
            var gripAtFullWear = Clamp(0.79f - (0.14f * compoundAggression) + (0.03f * sizeNorm), 0.52f, 0.90f);
            var corneringHeatCPerSecond = Clamp(1.55f + (0.95f * compoundAggression) + (0.55f * massNorm), 1.10f, 3.20f);
            var longitudinalHeatCPerSecond = Clamp(1.30f + (0.82f * compoundAggression) + (0.48f * massNorm), 1.00f, 2.90f);
            var loadHeatCPerSecond = Clamp(0.40f + (0.52f * massNorm), 0.24f, 1.05f);
            var rollingHeatCPerSecond = Clamp(0.22f + (0.25f * massNorm) + (0.08f * (1f - sizeNorm)), 0.14f, 0.60f);
            var airflowCoolingPerMpsPerCPerSecond = Clamp(0.000056f + (0.000034f * (1f - compoundAggression)) + (0.000022f * sizeNorm), 0.000040f, 0.000150f);
            var ambientExchangePerCPerSecond = Clamp(0.0019f + (0.0009f * (1f - compoundAggression)), 0.0013f, 0.0038f);
            var roadExchangePerCPerSecond = Clamp(0.0034f + (0.0015f * (1f - compoundAggression)) + (0.0008f * (1f - sizeNorm)), 0.0024f, 0.0078f);
            var wetRoadExchangePerCPerSecond = Clamp(0.0052f + (0.0030f * compoundAggression) + (0.0017f * (1f - sizeNorm)), 0.0040f, 0.0140f);
            var slipSmoothingTau = Clamp(1.55f - (0.68f * compoundAggression), 0.55f, 2.50f);

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
