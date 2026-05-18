using System;

namespace TopSpeed.Physics.Tires.Wear
{
    public sealed class TireWearConfig
    {
        public TireWearConfig(
            float baseWearPerKilometer,
            float slipWearRatePerSecond,
            float corneringSlipWearWeight,
            float longitudinalSlipWearWeight,
            float loadWearGain,
            float wearHotStartTemperatureC,
            float wearHotGainPerC,
            float wearColdStartTemperatureC,
            float wearColdGainPerC,
            float ambientTemperatureC,
            float coldEndTemperatureC,
            float optimalStartTemperatureC,
            float optimalEndTemperatureC,
            float overheatEndTemperatureC,
            float gripAtVeryCold,
            float gripAtColdEnd,
            float gripAtOptimal,
            float gripAtOverheatEnd,
            float gripAtCooked,
            float gripAtFullWear,
            float corneringHeatCPerSecond,
            float longitudinalHeatCPerSecond,
            float loadHeatCPerSecond,
            float rollingHeatCPerSecond,
            float airflowCoolingPerMpsPerCPerSecond,
            float ambientExchangePerCPerSecond,
            float roadExchangePerCPerSecond,
            float wetRoadExchangePerCPerSecond,
            float slipSmoothingTimeConstantSeconds)
        {
            BaseWearPerKilometer = Math.Max(0f, baseWearPerKilometer);
            SlipWearRatePerSecond = Math.Max(0f, slipWearRatePerSecond);
            CorneringSlipWearWeight = Math.Max(0f, corneringSlipWearWeight);
            LongitudinalSlipWearWeight = Math.Max(0f, longitudinalSlipWearWeight);
            LoadWearGain = Math.Max(0f, loadWearGain);
            WearHotStartTemperatureC = wearHotStartTemperatureC;
            WearHotGainPerC = Math.Max(0f, wearHotGainPerC);
            WearColdStartTemperatureC = wearColdStartTemperatureC;
            WearColdGainPerC = Math.Max(0f, wearColdGainPerC);
            AmbientTemperatureC = ambientTemperatureC;
            ColdEndTemperatureC = coldEndTemperatureC;
            OptimalStartTemperatureC = Math.Max(ColdEndTemperatureC + 1f, optimalStartTemperatureC);
            OptimalEndTemperatureC = Math.Max(OptimalStartTemperatureC + 1f, optimalEndTemperatureC);
            OverheatEndTemperatureC = Math.Max(OptimalEndTemperatureC + 1f, overheatEndTemperatureC);
            GripAtVeryCold = Clamp(gripAtVeryCold, 0.35f, 1.25f);
            GripAtColdEnd = Clamp(gripAtColdEnd, 0.35f, 1.25f);
            GripAtOptimal = Clamp(gripAtOptimal, 0.35f, 1.25f);
            GripAtOverheatEnd = Clamp(gripAtOverheatEnd, 0.35f, 1.25f);
            GripAtCooked = Clamp(gripAtCooked, 0.35f, 1.25f);
            GripAtFullWear = Clamp(gripAtFullWear, 0.35f, 1f);
            CorneringHeatCPerSecond = Math.Max(0f, corneringHeatCPerSecond);
            LongitudinalHeatCPerSecond = Math.Max(0f, longitudinalHeatCPerSecond);
            LoadHeatCPerSecond = Math.Max(0f, loadHeatCPerSecond);
            RollingHeatCPerSecond = Math.Max(0f, rollingHeatCPerSecond);
            AirflowCoolingPerMpsPerCPerSecond = Math.Max(0f, airflowCoolingPerMpsPerCPerSecond);
            AmbientExchangePerCPerSecond = Math.Max(0f, ambientExchangePerCPerSecond);
            RoadExchangePerCPerSecond = Math.Max(0f, roadExchangePerCPerSecond);
            WetRoadExchangePerCPerSecond = Math.Max(0f, wetRoadExchangePerCPerSecond);
            SlipSmoothingTimeConstantSeconds = Math.Max(0.01f, slipSmoothingTimeConstantSeconds);
        }

        public float BaseWearPerKilometer { get; }
        public float SlipWearRatePerSecond { get; }
        public float CorneringSlipWearWeight { get; }
        public float LongitudinalSlipWearWeight { get; }
        public float LoadWearGain { get; }
        public float WearHotStartTemperatureC { get; }
        public float WearHotGainPerC { get; }
        public float WearColdStartTemperatureC { get; }
        public float WearColdGainPerC { get; }
        public float AmbientTemperatureC { get; }
        public float ColdEndTemperatureC { get; }
        public float OptimalStartTemperatureC { get; }
        public float OptimalEndTemperatureC { get; }
        public float OverheatEndTemperatureC { get; }
        public float GripAtVeryCold { get; }
        public float GripAtColdEnd { get; }
        public float GripAtOptimal { get; }
        public float GripAtOverheatEnd { get; }
        public float GripAtCooked { get; }
        public float GripAtFullWear { get; }
        public float CorneringHeatCPerSecond { get; }
        public float LongitudinalHeatCPerSecond { get; }
        public float LoadHeatCPerSecond { get; }
        public float RollingHeatCPerSecond { get; }
        public float AirflowCoolingPerMpsPerCPerSecond { get; }
        public float AmbientExchangePerCPerSecond { get; }
        public float RoadExchangePerCPerSecond { get; }
        public float WetRoadExchangePerCPerSecond { get; }
        public float SlipSmoothingTimeConstantSeconds { get; }

        private static float Clamp(float value, float min, float max)
        {
            if (value < min)
                return min;
            if (value > max)
                return max;
            return value;
        }
    }
}
