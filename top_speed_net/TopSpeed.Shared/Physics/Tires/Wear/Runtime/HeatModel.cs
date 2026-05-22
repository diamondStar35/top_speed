using System;

namespace TopSpeed.Physics.Tires.Wear
{
    // Lumped-thermal-mass tire model. Single heat balance per step:
    //
    //   dT/dt = Q_in - h_total(v) * (T - T_eff)
    //
    // Q_in is the heat injected by the contact patch and bulk hysteresis;
    // h_total(v) is convective + conductive coupling to the surrounding air
    // and road (Newton's law of cooling). T_eff is the load-weighted mix of
    // air and road temperature. Wear past ~75% gradually amplifies heat input
    // and adds a small "breakdown" term that warms even an idle tire.
    internal static class TireWearHeatModel
    {
        // Wear amplification kicks in past 75% wear; breakdown is the extra
        // heat term past 90% wear that warms the tire even at low slip.
        private const float WearAmpStart = 0.75f;
        private const float WearAmpGain = 2.5f;
        private const float WearBreakdownStart = 0.90f;
        private const float WearBreakdownHeatCPerSecond = 1.20f;
        // Road coupling weight in the effective-ambient mix (0 = pure air, 1 = pure road).
        private const float RoadCouplingWeight = 0.55f;
        // Cornering signal that always contributes (utilization), and slide
        // bonus that only activates past the grip limit.
        private const float CorneringUtilizationWeight = 0.85f;
        private const float CorneringSlideBonus = 1.50f;
        private const float LongitudinalSlideBonus = 1.20f;

        public static TireWearHeatBalance StepTemperature(
            TireWearConfig config,
            in TireWearState state,
            in TireWearStepInput input,
            float elapsedSeconds,
            float ambientTemperatureC,
            float surfaceTemperatureC,
            float wetnessNormalized)
        {
            var load = TireWearMath.Clamp01(input.LoadNormalized);
            var rolling = TireWearMath.Clamp01(input.RollingResistanceNormalized);
            var corneringUtilization = TireWearMath.Clamp01(input.CorneringUtilizationNormalized);
            var corneringSlide = TireWearMath.Clamp01(input.CorneringSlipNormalized);
            var longitudinalSlip = TireWearMath.Clamp01(input.LongitudinalSlipNormalized);
            var longitudinalSlide = TireWearMath.Clamp01(input.LongitudinalSlideNormalized);

            // Bulk flex heat (rolling hysteresis) — always present when moving.
            // Linear in speed and (offset) load: a still tire generates no flex heat,
            // and a fully loaded tire generates ~3× the heat of a lightly loaded one.
            var flexHeat = ((config.LoadHeatCPerSecond * (0.30f + (0.70f * load)))
                + (config.RollingHeatCPerSecond * (0.30f + (0.70f * rolling))))
                * input.SpeedMps;

            // Friction heat from slip — physical model is P = μ·N·v_slip, which we
            // approximate as proportional to load · speed · slip². Cornering uses
            // the smooth utilization signal plus a slide bonus past the grip limit;
            // longitudinal uses the raw slip plus a slide bonus past the slide threshold.
            var corneringPower = (corneringUtilization * corneringUtilization * CorneringUtilizationWeight)
                + (corneringSlide * corneringSlide * CorneringSlideBonus);
            var longitudinalPower = (longitudinalSlip * longitudinalSlip)
                + (longitudinalSlide * longitudinalSlide * LongitudinalSlideBonus);
            var frictionHeat = input.SpeedMps * load * (
                (config.CorneringHeatCPerSecond * corneringPower)
                + (config.LongitudinalHeatCPerSecond * longitudinalPower));

            // Wear amplification: 1× until 75%, then ramps; past 90% the tire
            // generates extra heat just from rolling so the player still notices
            // a blown tire on the straights.
            var wearOver = TireWearMath.Clamp01((state.WearFraction - WearAmpStart) / (1f - WearAmpStart));
            var wearAmp = 1f + (WearAmpGain * wearOver * wearOver);
            var breakdown = TireWearMath.Clamp01((state.WearFraction - WearBreakdownStart) / (1f - WearBreakdownStart));
            var breakdownHeat = breakdown * breakdown
                * WearBreakdownHeatCPerSecond
                * (0.40f + (0.60f * TireWearMath.Clamp01(input.SpeedMps / 22f)));

            var heatingRateCPerSecond = (wearAmp * (flexHeat + frictionHeat)) + breakdownHeat;

            // Cooling: Newton's law against an effective ambient that mixes air
            // and road. Air coupling scales with airspeed (forced convection),
            // road coupling has a wet-road bonus because water carries heat away
            // faster than dry asphalt.
            var airCoupling = config.AmbientExchangePerCPerSecond
                + (config.AirflowCoolingPerMpsPerCPerSecond * input.SpeedMps);
            var roadCoupling = config.RoadExchangePerCPerSecond
                + (wetnessNormalized * config.WetRoadExchangePerCPerSecond);
            var totalCoupling = airCoupling + roadCoupling;
            var effectiveAmbientC = totalCoupling > 0f
                ? (((1f - RoadCouplingWeight) * airCoupling * ambientTemperatureC)
                   + (RoadCouplingWeight * roadCoupling * surfaceTemperatureC))
                  / (((1f - RoadCouplingWeight) * airCoupling) + (RoadCouplingWeight * roadCoupling))
                : ambientTemperatureC;
            var coolingRateCPerSecond = totalCoupling * (state.TemperatureC - effectiveAmbientC);

            var netRateCPerSecond = heatingRateCPerSecond - coolingRateCPerSecond;
            var temperatureC = state.TemperatureC + (netRateCPerSecond * elapsedSeconds);

            var minTemperatureC = Math.Min(ambientTemperatureC, surfaceTemperatureC) - 8f;
            var maxTemperatureC = Math.Max(surfaceTemperatureC + 120f, config.OverheatEndTemperatureC + 60f);
            temperatureC = TireWearMath.Clamp(temperatureC, minTemperatureC, maxTemperatureC);

            return new TireWearHeatBalance(
                temperatureC,
                heatingRateCPerSecond,
                Math.Max(0f, coolingRateCPerSecond));
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
