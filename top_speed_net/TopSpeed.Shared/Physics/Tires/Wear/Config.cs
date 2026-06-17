using System;

namespace TopSpeed.Physics.Tires.Wear
{
    // Configuration for the tire wear / heat / grip model. All properties are
    // init-only; build instances with object initializer syntax. Values are
    // sanitized lazily at the call site (see `TireWearProfiles` / `TireWearDefaults`)
    // — there is no defensive clamping inside the type itself.
    public sealed class TireWearConfig
    {
        // --- Wear inputs -----------------------------------------------------

        // Distance-based wear at unit load, per kilometer travelled.
        public float BaseWearPerKilometer { get; init; }

        // Slip-driven wear rate; scaled by the squared slip signals below.
        public float SlipWearRatePerSecond { get; init; }
        public float CorneringSlipWearWeight { get; init; }
        public float LongitudinalSlipWearWeight { get; init; }

        // Multiplier on distance wear at full normalized load.
        public float LoadWearGain { get; init; }

        // Temperature penalties: extra wear when running hot or cold.
        public float WearHotStartTemperatureC { get; init; }
        public float WearHotGainPerC { get; init; }
        public float WearColdStartTemperatureC { get; init; }
        public float WearColdGainPerC { get; init; }

        // --- Grip curve (piecewise) ------------------------------------------

        public float ColdEndTemperatureC { get; init; }
        public float OptimalStartTemperatureC { get; init; }
        public float OptimalEndTemperatureC { get; init; }
        public float OverheatEndTemperatureC { get; init; }

        public float GripAtVeryCold { get; init; }
        public float GripAtColdEnd { get; init; }
        public float GripAtOptimal { get; init; }
        public float GripAtOverheatEnd { get; init; }
        public float GripAtCooked { get; init; }
        public float GripAtFullWear { get; init; }

        // --- Heat balance (three-node cascade) -------------------------------
        //
        // Surface (contact patch, smallest mass) — heat input lands here and
        // drives the grip curve / player's gauge:
        //   dT_s/dt = wear_amp * (flex_heat + friction_heat)
        //           - h_total(v) * (T_s - T_eff)
        //           - k_st * (T_s - T_t)
        //
        // Tread (bulk tread + belts, intermediate mass) — holds in-corner
        // spikes for tens of seconds; transports heat between surface and
        // carcass:
        //   dT_t/dt = ( k_st * (T_s - T_t) - k_tc * (T_t - T_c) ) / m_tread
        //
        // Carcass (sidewall + rim soak, largest mass) — sets the warm-up
        // time scale:
        //   dT_c/dt = k_tc * (T_t - T_c) / m_carcass
        //
        // flex_heat is load·speed driven; friction_heat is load·speed·slip² driven;
        // h_total = ambient + (airflow * v) + road. All heat values are in °C/s
        // when multiplied with their respective normalized signals.
        //
        // The three coupled time constants are independent:
        //   τ_corner   ≈ 1 / (k_st + h_air(v))   — in-corner heat-up (~1–5 s)
        //   τ_recovery ≈ m_tread / k_st          — spike fade on the straight (~10–30 s)
        //   τ_warmup   ≈ m_carcass / k_tc        — cold-tire soak (~200–400 s)

        public float CorneringHeatCPerSecond { get; init; }
        public float AccelerationHeatCPerSecond { get; init; }
        public float BrakeHeatCPerSecond { get; init; }

        // Fraction of brake heat injected at the surface node (lockup/slip
        // "flash" that fades fast); the remainder soaks into the tread node
        // (rotor/hub heat that lingers but still sheds to air on the straight).
        public float BrakeSurfaceHeatFraction { get; init; }
        public float LoadHeatCPerSecond { get; init; }
        public float RollingHeatCPerSecond { get; init; }

        public float AirflowCoolingPerMpsPerCPerSecond { get; init; }
        public float AmbientExchangePerCPerSecond { get; init; }
        public float RoadExchangePerCPerSecond { get; init; }
        public float WetRoadExchangePerCPerSecond { get; init; }

        // Surface ↔ tread thermal conductance. Controls in-corner heat-up
        // and surface spike recovery. Larger k_st → surface drains into the
        // tread faster, lowering spike peaks and shortening recovery on a
        // straight, but also bleeds tread heat back into the surface during
        // warm-up.
        public float SurfaceToTreadConductancePerSecond { get; init; }

        // Tread ↔ carcass thermal conductance. Controls how fast the bulk
        // tire absorbs heat from the tread; together with `CarcassMassRatio`
        // this sets the warm-up time constant.
        public float TreadToCarcassConductancePerSecond { get; init; }

        // Tread heat capacity as a multiple of the (unit) surface node.
        // Larger m_tread → tread temperature changes more slowly, which
        // lengthens spike recovery and the warm-up cascade.
        public float TreadMassRatio { get; init; }

        // Carcass heat capacity as a multiple of the (unit) surface node.
        // Together with `TreadToCarcassConductancePerSecond` this is the
        // dominant control over warm-up time; it should not perceptibly
        // affect in-corner spike behavior because the carcass barely moves
        // over a 10 s corner.
        public float CarcassMassRatio { get; init; }

        // --- Smoothing -------------------------------------------------------

        public float SlipSmoothingTimeConstantSeconds { get; init; }

        // --- Environment fallback --------------------------------------------

        // Used only when the per-step ambient is non-finite (e.g. NaN from a
        // crashed weather source). Tracks supply their real ambient through
        // `TireWearInput.AmbientTemperatureC` for every step.
        public float FallbackAmbientTemperatureC { get; init; } = 22f;
    }
}
