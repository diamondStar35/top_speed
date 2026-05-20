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
            var speedHeatActivity = TireWearMath.Clamp01(input.SpeedMps / 12f);
            var slipActivity = TireWearMath.Clamp01(input.SpeedMps / 6f);
            var thermalLoadScale = 0.58f + (0.70f * input.LoadNormalized);
            var thermalControl = TireWearThermalControl.Resolve(config, state);
            var corneringUtilizationSignal = TireWearMath.Pow(input.CorneringUtilizationNormalized, 1.15f);
            var corneringSlideSignal = TireWearMath.Pow(input.CorneringSlipNormalized, 1.40f);
            var lateralSlideSignal = TireWearMath.Pow(TireWearMath.Clamp01(input.LateralSlipNormalized / 1.30f), 1.30f);
            var longitudinalStressSignal = TireWearMath.Pow(input.LongitudinalSlipNormalized, 1.15f);
            var longitudinalSlideSignal = TireWearMath.Pow(input.LongitudinalSlideNormalized, 1.30f);
            var corneringSlideComposite = Math.Max(corneringSlideSignal, lateralSlideSignal);
            var slideSeverity = TireWearMath.Clamp01(
                Math.Max(corneringSlideComposite, longitudinalSlideSignal));
            var highSpeedRecovery = TireWearMath.Clamp01((speedNormalized - 0.45f) / 0.55f);
            var lowSlideRecovery = TireWearMath.Clamp01((0.28f - slideSeverity) / 0.28f);
            var thermalSurplusNormalized = TireWearMath.Clamp01(
                (state.TemperatureC - config.OptimalEndTemperatureC) / 24f);
            var wearFreshness = 1f - TireWearMath.Clamp01(state.WearFraction);

            var corneringHeatRate = config.CorneringHeatCPerSecond
                * ((0.08f * corneringUtilizationSignal) + (0.92f * corneringSlideComposite))
                * thermalLoadScale
                * slipActivity;
            var longitudinalHeatRate = config.LongitudinalHeatCPerSecond
                * ((0.35f * longitudinalStressSignal) + (0.65f * longitudinalSlideSignal))
                * (0.52f + (0.62f * input.LoadNormalized))
                * slipActivity;
            var loadHeatRate = config.LoadHeatCPerSecond
                * (0.40f + (0.60f * input.LoadNormalized))
                * (0.55f + (0.45f * speedNormalized));
            var rollingHeatRate = config.RollingHeatCPerSecond
                * (0.45f + (0.55f * input.RollingResistanceNormalized))
                * (0.45f + (0.55f * speedNormalized));
            var cruiseFlexHeatBase = (config.LoadHeatCPerSecond * 0.42f) + (config.RollingHeatCPerSecond * 0.74f);
            var cruiseFlexHeatRate = cruiseFlexHeatBase
                * TireWearMath.Pow(speedNormalized, 1.05f)
                * speedHeatActivity
                * (0.48f + (0.52f * input.LoadNormalized))
                * (0.45f + (0.55f * (1f - slideSeverity)));
            var heatingRateCPerSecond = corneringHeatRate
                + longitudinalHeatRate
                + loadHeatRate
                + rollingHeatRate
                + cruiseFlexHeatRate;

            // At speed and low slip, fresh tires should naturally stabilize near the working band.
            var overOptimalNormalized = TireWearMath.Clamp01(
                (state.TemperatureC - config.OptimalStartTemperatureC)
                / Math.Max(6f, (config.OptimalEndTemperatureC - config.OptimalStartTemperatureC) + 6f));
            var thermalStability = wearFreshness
                * (0.35f + (0.65f * lowSlideRecovery))
                * (0.25f + (0.75f * speedNormalized));
            var stabilityHeatScale = 1f - (0.24f * thermalStability * overOptimalNormalized);
            heatingRateCPerSecond *= TireWearMath.Clamp(stabilityHeatScale, 0.72f, 1f);
            heatingRateCPerSecond *= thermalControl.HeatingScale;

            var roadExchangeGain = (config.RoadExchangePerCPerSecond + (wetnessNormalized * config.WetRoadExchangePerCPerSecond))
                * thermalControl.CoolingScale;
            var ambientExchangeRate = (ambientTemperatureC - state.TemperatureC)
                * config.AmbientExchangePerCPerSecond
                * thermalControl.CoolingScale;
            var roadExchangeRate = (surfaceTemperatureC - state.TemperatureC) * roadExchangeGain;
            var airflowSpeedGain = 1f
                + (0.75f * airflowSpeedNormalized)
                + (1.65f * airflowSpeedNormalized * airflowSpeedNormalized);
            var thermalRecoveryWindow = TireWearMath.Clamp01(
                (state.TemperatureC - config.OptimalStartTemperatureC) / 18f);
            var lowSlideCoolingBoost = 1f + (1.25f * highSpeedRecovery * lowSlideRecovery * thermalRecoveryWindow);
            var overheatRecoveryBoost = 1f + (1.25f * highSpeedRecovery * lowSlideRecovery * thermalSurplusNormalized);
            var airflowCoolingRate = Math.Max(0f, state.TemperatureC - ambientTemperatureC)
                * input.SpeedMps
                * config.AirflowCoolingPerMpsPerCPerSecond
                * airflowSpeedGain
                * lowSlideCoolingBoost
                * overheatRecoveryBoost
                * thermalControl.CoolingScale;

            var ambientCoolingRate = Math.Max(0f, state.TemperatureC - ambientTemperatureC)
                * config.AmbientExchangePerCPerSecond
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
