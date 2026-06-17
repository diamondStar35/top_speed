using System;

namespace TopSpeed.Physics.Tires.Wear
{
    // Three-node lumped-thermal-mass tire cascade. Heat enters the surface
    // node; the surface bleeds into the tread, the tread bleeds into the
    // carcass, and each node bleeds to ambient via Newton's law as well.
    //
    //   dT_s/dt = Q_in - h_total(v) * (T_s - T_eff) - k_st * (T_s - T_t)
    //   dT_t/dt = ( k_st * (T_s - T_t) - k_tc * (T_t - T_c) ) / m_tread
    //   dT_c/dt =   k_tc * (T_t - T_c) / m_carcass
    //
    // Q_in is the wear-amplified contact-patch + flex heat injection;
    // h_total(v) is convective + conductive coupling to the surrounding air
    // and road; T_eff is the load-weighted mix of air and road temperature.
    // Wear past ~75 % gradually amplifies heat input and adds a small
    // "breakdown" term that warms even an idle tire.
    //
    // The cascade gives us three independent time constants, one per spec
    // target:
    //   τ_corner    ≈ 1 / (k_st + h_total(v))  — in-corner heat-up (~1–5 s)
    //   τ_recovery  ≈ m_tread / k_st           — spike fade on the straight (~10–30 s)
    //   τ_warmup    ≈ m_carcass / k_tc         — bulk cold-tire soak (~200–400 s)
    //
    // Each frame is split into substeps no larger than `MaxSubstepSeconds`
    // so the forward-Euler integrator stays well below the smallest stable
    // time constant in the system regardless of frame rate.
    internal static class TireWearHeatModel
    {
        // Wear amplification kicks in past 75 % wear; breakdown is the extra
        // heat term past 90 % wear that warms the tire even at low slip.
        private const float WearAmpStart = 0.75f;
        private const float WearAmpGain = 4.5f;
        private const float WearBreakdownStart = 0.90f;
        private const float WearBreakdownHeatCPerSecond = 1.20f;
        // Road coupling weight in the effective-ambient mix (0 = pure air, 1 = pure road).
        private const float RoadCouplingWeight = 0.55f;
        // Cornering signal that always contributes (utilization), and slide
        // bonus that only activates past the grip limit.
        private const float CorneringUtilizationWeight = 0.85f;
        private const float CorneringSlideBonus = 1.0f;
        private const float LongitudinalSlideBonus = 0.75f;
        // Largest stable substep for the surface node. The fastest
        // representative τ in tuned defaults is ≈ 4 s, so 0.25 s leaves
        // ample margin (forward-Euler stability needs dt ≲ 2 τ).
        private const float MaxSubstepSeconds = 0.25f;
        private const int MaxSubsteps = 240;

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
                + (config.AccelerationHeatCPerSecond * longitudinalPower));

            // Wear amplification: 1× until 75 %, then ramps; past 90 % the tire
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

            var kSt = Math.Max(0f, config.SurfaceToTreadConductancePerSecond);
            var kTc = Math.Max(0f, config.TreadToCarcassConductancePerSecond);
            var mTread = Math.Max(0.1f, config.TreadMassRatio);
            var mCarcass = Math.Max(0.1f, config.CarcassMassRatio);

            var minTemperatureC = Math.Min(ambientTemperatureC, surfaceTemperatureC) - 8f;
            var maxTemperatureC = Math.Max(surfaceTemperatureC + 120f, config.OverheatEndTemperatureC + 60f);

            var surfaceC = state.TemperatureC;
            var treadC = state.TreadTemperatureC;
            var carcassC = state.CarcassTemperatureC;
            var coolingAccumC = 0f;
            var remaining = Math.Max(0f, elapsedSeconds);
            var substeps = 0;
            while (remaining > 0f && substeps < MaxSubsteps)
            {
                var dt = remaining > MaxSubstepSeconds ? MaxSubstepSeconds : remaining;
                var surfaceCooling = totalCoupling * (surfaceC - effectiveAmbientC);
                var surfaceToTread = kSt * (surfaceC - treadC);
                var treadToCarcass = kTc * (treadC - carcassC);
                surfaceC += (heatingRateCPerSecond - surfaceCooling - surfaceToTread) * dt;
                treadC += (surfaceToTread - treadToCarcass) / mTread * dt;
                carcassC += treadToCarcass / mCarcass * dt;
                surfaceC = TireWearMath.Clamp(surfaceC, minTemperatureC, maxTemperatureC);
                treadC = TireWearMath.Clamp(treadC, minTemperatureC, maxTemperatureC);
                carcassC = TireWearMath.Clamp(carcassC, minTemperatureC, maxTemperatureC);
                coolingAccumC += Math.Max(0f, surfaceCooling) * dt;
                remaining -= dt;
                substeps++;
            }

            var coolingRateCPerSecond = elapsedSeconds > 0f
                ? coolingAccumC / elapsedSeconds
                : 0f;

            return new TireWearHeatBalance(
                surfaceC,
                treadC,
                carcassC,
                heatingRateCPerSecond,
                coolingRateCPerSecond);
        }
    }

    internal readonly struct TireWearHeatBalance
    {
        public TireWearHeatBalance(
            float temperatureC,
            float treadTemperatureC,
            float carcassTemperatureC,
            float heatingRateCPerSecond,
            float coolingRateCPerSecond)
        {
            TemperatureC = temperatureC;
            TreadTemperatureC = treadTemperatureC;
            CarcassTemperatureC = carcassTemperatureC;
            HeatingRateCPerSecond = heatingRateCPerSecond;
            CoolingRateCPerSecond = coolingRateCPerSecond;
        }

        public float TemperatureC { get; }
        public float TreadTemperatureC { get; }
        public float CarcassTemperatureC { get; }
        public float HeatingRateCPerSecond { get; }
        public float CoolingRateCPerSecond { get; }
    }
}
