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
            var speedNormalized = TireWearMath.Clamp(input.SpeedMps / 72f, 0f, 1.35f);
            var airflowSpeedNormalized = TireWearMath.Clamp(input.SpeedMps / 88f, 0f, 1.6f);
            var speedHeatActivity = TireWearMath.Clamp01(input.SpeedMps / 11f);
            var slipActivity = TireWearMath.Clamp01(input.SpeedMps / 5f);
            var thermalLoadScale = 0.58f + (0.68f * input.LoadNormalized);
            var thermalControl = TireWearThermalControl.Resolve(config, state);
            var corneringUtilizationSignal = TireWearMath.Pow(input.CorneringUtilizationNormalized, 1.05f);
            var corneringSlideSignal = TireWearMath.Pow(input.CorneringSlipNormalized, 1.28f);
            var lateralSlideSignal = TireWearMath.Pow(TireWearMath.Clamp01(input.LateralSlipNormalized / 1.20f), 1.25f);
            var longitudinalStressSignal = TireWearMath.Pow(input.LongitudinalSlipNormalized, 1.08f);
            var longitudinalSlideSignal = TireWearMath.Pow(input.LongitudinalSlideNormalized, 1.20f);
            var corneringSlideComposite = Math.Max(corneringSlideSignal, lateralSlideSignal);
            var slideSeverity = TireWearMath.Clamp01(
                Math.Max(corneringSlideComposite, longitudinalSlideSignal));
            var highSpeedRecovery = TireWearMath.Clamp01((speedNormalized - 0.35f) / 0.75f);
            var lowSlideRecovery = TireWearMath.Clamp01((0.24f - slideSeverity) / 0.24f);
            var thermalSurplusNormalized = TireWearMath.Clamp01(
                (state.TemperatureC - config.OptimalEndTemperatureC) / 22f);
            var wearFreshness = 1f - TireWearMath.Clamp01(state.WearFraction);

            var corneringHeatRate = config.CorneringHeatCPerSecond
                * ((0.22f * corneringUtilizationSignal) + (0.78f * corneringSlideComposite))
                * thermalLoadScale
                * (0.38f + (0.62f * slipActivity));
            var longitudinalHeatRate = config.LongitudinalHeatCPerSecond
                * ((0.40f * longitudinalStressSignal) + (0.60f * longitudinalSlideSignal))
                * (0.52f + (0.62f * input.LoadNormalized))
                * (0.36f + (0.64f * slipActivity));
            corneringHeatRate *= 1f + (0.62f * TireWearMath.Pow(corneringSlideComposite, 1.20f));
            longitudinalHeatRate *= 1f + (0.40f * TireWearMath.Pow(longitudinalSlideSignal, 1.10f));
            var loadHeatRate = config.LoadHeatCPerSecond
                * (0.42f + (0.58f * input.LoadNormalized))
                * (0.45f + (0.55f * speedNormalized));
            var rollingHeatRate = config.RollingHeatCPerSecond
                * (0.46f + (0.54f * input.RollingResistanceNormalized))
                * (0.40f + (0.60f * speedNormalized));
            var cruiseFlexHeatBase = (config.LoadHeatCPerSecond * 0.55f) + (config.RollingHeatCPerSecond * 0.92f);
            var cruiseFlexHeatRate = cruiseFlexHeatBase
                * TireWearMath.Pow(speedNormalized, 0.92f)
                * speedHeatActivity
                * (0.50f + (0.50f * input.LoadNormalized))
                * (0.72f + (0.28f * (1f - slideSeverity)));
            var catastrophicWear = TireWearMath.Clamp01((state.WearFraction - 0.95f) / 0.05f);
            var structuralFailureHeatRate = config.RollingHeatCPerSecond
                * (0.12f + (0.88f * catastrophicWear))
                * (0.40f + (0.60f * speedNormalized))
                * (0.70f + (0.30f * lowSlideRecovery));
            var heatingRateCPerSecond = corneringHeatRate
                + longitudinalHeatRate
                + loadHeatRate
                + rollingHeatRate
                + cruiseFlexHeatRate
                + structuralFailureHeatRate;

            // At speed and low slip, fresh tires should naturally stabilize near the working band.
            var overOptimalNormalized = TireWearMath.Clamp01(
                (state.TemperatureC - config.OptimalStartTemperatureC)
                / Math.Max(8f, config.OptimalEndTemperatureC - config.OptimalStartTemperatureC));
            var thermalStability = wearFreshness
                * (0.40f + (0.60f * lowSlideRecovery))
                * (0.30f + (0.70f * speedNormalized));
            var stabilityHeatScale = 1f - (0.28f * thermalStability * overOptimalNormalized);
            heatingRateCPerSecond *= TireWearMath.Clamp(stabilityHeatScale, 0.70f, 1f);
            var freshOverOptimal = wearFreshness * TireWearMath.Clamp01(
                (state.TemperatureC - config.OptimalEndTemperatureC) / 24f);
            var saturationHeatScale = 1f - (0.22f * freshOverOptimal);
            heatingRateCPerSecond *= TireWearMath.Clamp(saturationHeatScale, 0.62f, 1f);
            heatingRateCPerSecond *= thermalControl.HeatingScale;

            var roadExchangeGain = (config.RoadExchangePerCPerSecond + (wetnessNormalized * config.WetRoadExchangePerCPerSecond))
                * thermalControl.CoolingScale;
            var ambientExchangeRate = (ambientTemperatureC - state.TemperatureC)
                * config.AmbientExchangePerCPerSecond
                * thermalControl.CoolingScale;
            var roadExchangeRate = (surfaceTemperatureC - state.TemperatureC) * roadExchangeGain;
            var airflowSpeedGain = 1f
                + (0.52f * airflowSpeedNormalized)
                + (0.95f * airflowSpeedNormalized * airflowSpeedNormalized);
            var thermalRecoveryWindow = TireWearMath.Clamp01(
                (state.TemperatureC - (config.OptimalStartTemperatureC - 4f)) / 24f);
            var belowOptimalNormalized = TireWearMath.Clamp01(
                (config.OptimalStartTemperatureC - state.TemperatureC) / 22f);
            var airflowWindowScale = 1f - (0.32f * belowOptimalNormalized);
            var lowSlideCoolingBoost = 1f + (0.70f * highSpeedRecovery * lowSlideRecovery * thermalRecoveryWindow);
            var overheatRecoveryBoost = 1f + (1.35f * highSpeedRecovery * lowSlideRecovery * thermalSurplusNormalized);
            var overheatVentilationBoost = 1f + (0.32f * highSpeedRecovery * thermalSurplusNormalized);
            var airflowCoolingRate = Math.Max(0f, state.TemperatureC - ambientTemperatureC)
                * input.SpeedMps
                * config.AirflowCoolingPerMpsPerCPerSecond
                * airflowSpeedGain
                * airflowWindowScale
                * lowSlideCoolingBoost
                * overheatRecoveryBoost
                * overheatVentilationBoost
                * thermalControl.CoolingScale;

            var ambientCoolingRate = Math.Max(0f, state.TemperatureC - ambientTemperatureC)
                * config.AmbientExchangePerCPerSecond
                * thermalControl.CoolingScale;
            var roadCoolingRate = Math.Max(0f, state.TemperatureC - surfaceTemperatureC) * roadExchangeGain;
            var coolingRateCPerSecond = ambientCoolingRate + roadCoolingRate + airflowCoolingRate;

            var netTemperatureRateCPerSecond = heatingRateCPerSecond + ambientExchangeRate + roadExchangeRate - airflowCoolingRate;
            var temperatureC = state.TemperatureC + (netTemperatureRateCPerSecond * elapsedSeconds);
            var maxTemperatureC = Math.Max(surfaceTemperatureC + 110f, config.OverheatEndTemperatureC + 45f);
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
