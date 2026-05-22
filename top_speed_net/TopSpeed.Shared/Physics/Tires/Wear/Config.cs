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

        // --- Heat balance (two-node) -----------------------------------------
        //
        // Surface (contact patch, low thermal mass):
        //   dT_s/dt = wear_amp * (flex_heat + friction_heat)
        //           - h_total(v) * (T_s - T_eff)
        //           - k_int * (T_s - T_c)
        //
        // Carcass (bulk rubber + belts, high thermal mass):
        //   dT_c/dt = (k_int / mass_ratio) * (T_s - T_c)
        //
        // flex_heat is load·speed driven; friction_heat is load·speed·slip² driven;
        // h_total = ambient + (airflow * v) + road. All heat values are in °C/s
        // when multiplied with their respective normalized signals.

        public float CorneringHeatCPerSecond { get; init; }
        public float LongitudinalHeatCPerSecond { get; init; }
        public float LoadHeatCPerSecond { get; init; }
        public float RollingHeatCPerSecond { get; init; }

        public float AirflowCoolingPerMpsPerCPerSecond { get; init; }
        public float AmbientExchangePerCPerSecond { get; init; }
        public float RoadExchangePerCPerSecond { get; init; }
        public float WetRoadExchangePerCPerSecond { get; init; }

        // Internal coupling between the surface and carcass nodes. Higher
        // conductance → spikes drain into the carcass faster (faster recovery)
        // but also warm the carcass faster (faster overall warm-up).
        public float InternalConductancePerSecond { get; init; }

        // Carcass heat capacity expressed as a multiple of the surface node.
        // Larger ratio → carcass is harder to move → surface spikes recover
        // quickly into a still-cool reservoir, but warm-up takes longer.
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
