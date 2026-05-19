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
            var airflowSpeedNormalized = TireWearMath.Clamp(input.SpeedMps / 95f, 0f, 1.8f);
            var speedHeatActivity = TireWearMath.Clamp01(input.SpeedMps / 15f);
            var slipActivity = TireWearMath.Clamp01(input.SpeedMps / 5f);
            var thermalLoadScale = 0.60f + (0.68f * input.LoadNormalized);
            var thermalControl = TireWearThermalControl.Resolve(config, state);
            var corneringUtilizationSignal = TireWearMath.Pow(input.CorneringUtilizationNormalized, 1.08f);
            var corneringSlideSignal = TireWearMath.Pow(input.CorneringSlipNormalized, 1.35f);
            var lateralSlideSignal = TireWearMath.Pow(TireWearMath.Clamp01(input.LateralSlipNormalized / 1.25f), 1.25f);
            var longitudinalHeatSignal = TireWearMath.Pow(input.LongitudinalSlipNormalized, 1.30f);
            var longitudinalSlideSignal = TireWearMath.Pow(input.LongitudinalSlideNormalized, 1.20f);

            var corneringHeatRate = config.CorneringHeatCPerSecond
                * ((0.26f * corneringUtilizationSignal) + (0.74f * corneringSlideSignal))
                * thermalLoadScale
                * slipActivity;
            var lateralSlipHeatRate = config.CorneringHeatCPerSecond
                * 0.15f
                * lateralSlideSignal
                * thermalLoadScale
                * slipActivity;
            var longitudinalHeatRate = config.LongitudinalHeatCPerSecond * longitudinalHeatSignal * thermalLoadScale * slipActivity;
            var loadHeatRate = config.LoadHeatCPerSecond
                * (0.30f + (0.58f * input.LoadNormalized))
                * speedHeatActivity;
            var rollingHeatRate = config.RollingHeatCPerSecond
                * (0.34f + (0.52f * input.RollingResistanceNormalized))
                * speedHeatActivity;
            var powertrainSlipHeatSignal = TireWearMath.Pow((input.LongitudinalSlipNormalized * 0.45f) + (longitudinalSlideSignal * 0.55f), 1.05f);
            var powertrainWorkHeatRate = config.LongitudinalHeatCPerSecond
                * (0.08f + (0.92f * powertrainSlipHeatSignal))
                * (0.52f + (0.48f * speedNormalized))
                * (0.46f + (0.54f * input.LoadNormalized))
                * speedHeatActivity
                * 0.78f;
            var cruiseFlexHeatBase = (config.LoadHeatCPerSecond * 0.28f) + (config.RollingHeatCPerSecond * 0.44f);
            var cruiseSlipFactor = 0.30f + (0.70f * input.LongitudinalSlipNormalized);
            var cruiseFlexHeatRate = cruiseFlexHeatBase
                * TireWearMath.Pow(speedNormalized, 1.12f)
                * speedHeatActivity
                * (0.50f + (0.50f * input.LoadNormalized))
                * cruiseSlipFactor;
            var heatingRateCPerSecond = corneringHeatRate
                + lateralSlipHeatRate
                + longitudinalHeatRate
                + loadHeatRate
                + rollingHeatRate
                + powertrainWorkHeatRate
                + cruiseFlexHeatRate;
            heatingRateCPerSecond *= thermalControl.HeatingScale;

            var exchangeScale = 1f;
            var roadExchangeGain = (config.RoadExchangePerCPerSecond + (wetnessNormalized * config.WetRoadExchangePerCPerSecond))
                * exchangeScale
                * thermalControl.CoolingScale;
            var ambientExchangeRate = (ambientTemperatureC - state.TemperatureC)
                * config.AmbientExchangePerCPerSecond
                * exchangeScale
                * thermalControl.CoolingScale;
            var roadExchangeRate = (surfaceTemperatureC - state.TemperatureC) * roadExchangeGain;
            var airflowSpeedGain = 1f
                + (0.40f * airflowSpeedNormalized)
                + (0.90f * airflowSpeedNormalized * airflowSpeedNormalized);
            var airflowCoolingRate = Math.Max(0f, state.TemperatureC - ambientTemperatureC)
                * input.SpeedMps
                * config.AirflowCoolingPerMpsPerCPerSecond
                * airflowSpeedGain
                * thermalControl.CoolingScale;

            var ambientCoolingRate = Math.Max(0f, state.TemperatureC - ambientTemperatureC)
                * config.AmbientExchangePerCPerSecond
                * exchangeScale
                * thermalControl.CoolingScale;
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
