using System;

namespace TopSpeed.Physics.Tires.Wear
{
    internal static class TireWearHeatModel
    {
        public static TireWearHeatBalance StepTemperature(
            TireWearConfig config,
            in TireWearState state,
            in TireWearStepInput input,
            float elapsedSeconds,
            float ambientTemperatureC,
            float surfaceTemperatureC,
            float wetnessNormalized)
        {
            var speedNormalized = TireWearMath.Clamp01(input.SpeedMps / 55f);
            var speedHeatActivity = TireWearMath.Clamp01(input.SpeedMps / 15f);
            var slipActivity = TireWearMath.Clamp01(input.SpeedMps / 5f);
            var thermalLoadScale = 0.65f + (0.75f * input.LoadNormalized);
            var corneringHeatSignal = TireWearMath.Pow(input.CorneringSlipNormalized, 1.35f);
            var lateralHeatSignal = TireWearMath.Pow(input.LateralSlipNormalized, 1.25f);
            var longitudinalHeatSignal = TireWearMath.Pow(input.LongitudinalSlipNormalized, 1.30f);

            var corneringHeatRate = config.CorneringHeatCPerSecond * corneringHeatSignal * thermalLoadScale * slipActivity;
            var lateralSlipHeatRate = config.CorneringHeatCPerSecond * 0.18f * lateralHeatSignal * thermalLoadScale * slipActivity;
            var longitudinalHeatRate = config.LongitudinalHeatCPerSecond * longitudinalHeatSignal * thermalLoadScale * slipActivity;
            var loadHeatRate = config.LoadHeatCPerSecond
                * (0.35f + (0.65f * input.LoadNormalized))
                * speedHeatActivity;
            var rollingHeatRate = config.RollingHeatCPerSecond
                * (0.45f + (0.55f * input.RollingResistanceNormalized))
                * speedHeatActivity;
            var powertrainWorkHeatRate = config.LongitudinalHeatCPerSecond
                * (0.25f + (0.75f * TireWearMath.Pow(input.LongitudinalSlipNormalized, 1.05f)))
                * (0.55f + (0.45f * speedNormalized))
                * (0.60f + (0.40f * input.LoadNormalized))
                * speedHeatActivity;
            var cruiseFlexHeatRate = ((config.LoadHeatCPerSecond * 0.55f) + (config.RollingHeatCPerSecond * 0.85f))
                * speedNormalized
                * speedHeatActivity
                * (0.65f + (0.35f * input.LoadNormalized));
            var heatingRateCPerSecond = corneringHeatRate
                + lateralSlipHeatRate
                + longitudinalHeatRate
                + loadHeatRate
                + rollingHeatRate
                + powertrainWorkHeatRate
                + cruiseFlexHeatRate;

            var exchangeScale = TireWearMath.Lerp(1.0f, 0.70f, speedNormalized);
            var roadExchangeGain = (config.RoadExchangePerCPerSecond + (wetnessNormalized * config.WetRoadExchangePerCPerSecond)) * exchangeScale;
            var ambientExchangeRate = (ambientTemperatureC - state.TemperatureC) * config.AmbientExchangePerCPerSecond * exchangeScale;
            var roadExchangeRate = (surfaceTemperatureC - state.TemperatureC) * roadExchangeGain;
            var airflowCoolingRate = Math.Max(0f, state.TemperatureC - ambientTemperatureC)
                * input.SpeedMps
                * config.AirflowCoolingPerMpsPerCPerSecond;

            var ambientCoolingRate = Math.Max(0f, state.TemperatureC - ambientTemperatureC) * config.AmbientExchangePerCPerSecond * exchangeScale;
            var roadCoolingRate = Math.Max(0f, state.TemperatureC - surfaceTemperatureC) * roadExchangeGain;
            var coolingRateCPerSecond = ambientCoolingRate + roadCoolingRate + airflowCoolingRate;

            var netTemperatureRateCPerSecond = heatingRateCPerSecond + ambientExchangeRate + roadExchangeRate - airflowCoolingRate;
            var temperatureC = state.TemperatureC + (netTemperatureRateCPerSecond * elapsedSeconds);
            var maxTemperatureC = Math.Max(surfaceTemperatureC + 90f, config.OverheatEndTemperatureC + 35f);
            temperatureC = TireWearMath.Clamp(temperatureC, ambientTemperatureC - 35f, maxTemperatureC);

            return new TireWearHeatBalance(temperatureC, heatingRateCPerSecond, coolingRateCPerSecond);
        }
    }

    internal readonly struct TireWearHeatBalance
    {
        public TireWearHeatBalance(float temperatureC, float heatingRateCPerSecond, float coolingRateCPerSecond)
        {
            TemperatureC = temperatureC;
            HeatingRateCPerSecond = heatingRateCPerSecond;
            CoolingRateCPerSecond = coolingRateCPerSecond;
        }

        public float TemperatureC { get; }
        public float HeatingRateCPerSecond { get; }
        public float CoolingRateCPerSecond { get; }
    }
}
