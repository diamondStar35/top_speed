namespace TopSpeed.Physics.Tires.Wear
{
    // Three-node thermal state. The model is a cascade:
    //   surface (T_s) — outer tread / contact patch. Smallest mass, drives
    //                   grip and the player's gauge. Heat input lands here.
    //   tread   (T_t) — bulk tread + belts. Intermediate mass. Holds the
    //                   in-corner spike for tens of seconds.
    //   carcass (T_c) — carcass + sidewall + rim soak. Largest mass.
    //                   Sets the warm-up time scale (cold → optimal in
    //                   minutes).
    //
    // Three independent time constants — one per phenomenon:
    //   τ_corner    ≈ 1 / (k_st + h_air(v))   — in-corner heat-up
    //   τ_recovery  ≈ m_tread / k_st          — spike fade on the straight
    //   τ_warmup    ≈ m_carcass / k_tc        — bulk cold-tire soak
    // The driver-stress inputs, low-passed by a single shared filter so that
    // heat and wear both react to the same ~1 s rolling average of what the
    // driver is doing. Smoothing happens before any squaring downstream
    // (smooth-then-square), so a brief tap averages out instead of registering
    // as a hard, sustained input. One filter, one time constant — replaces the
    // old per-consumer smoothing (a 0.8 s heat filter and a 1.4 s wear gate).
    public readonly struct TireWearSmoothedInputs
    {
        public TireWearSmoothedInputs(
            float corneringUtilization,
            float corneringSlip,
            float accelerationStress,
            float brakeStress,
            float engineBrakeStress,
            float longitudinalSlip,
            float load)
        {
            CorneringUtilization = corneringUtilization;
            CorneringSlip = corneringSlip;
            AccelerationStress = accelerationStress;
            BrakeStress = brakeStress;
            EngineBrakeStress = engineBrakeStress;
            LongitudinalSlip = longitudinalSlip;
            Load = load;
        }

        public float CorneringUtilization { get; }
        public float CorneringSlip { get; }
        public float AccelerationStress { get; }
        public float BrakeStress { get; }
        public float EngineBrakeStress { get; }
        public float LongitudinalSlip { get; }
        public float Load { get; }

        // Representative single-axis slip for telemetry/UI, matching the old
        // rawSlip blend (cornering slide + longitudinal slide).
        public float RepresentativeSlip
        {
            get
            {
                var longitudinalSlide = TireWearMath.Clamp01((LongitudinalSlip - 0.52f) / 0.48f);
                return TireWearMath.Clamp01((CorneringSlip * 0.60f) + (longitudinalSlide * 0.40f));
            }
        }
    }

    public readonly struct TireWearState
    {
        public TireWearState(
            float wearFraction,
            float temperatureC,
            float treadTemperatureC,
            float carcassTemperatureC,
            in TireWearSmoothedInputs smoothed)
        {
            WearFraction = wearFraction;
            TemperatureC = temperatureC;
            TreadTemperatureC = treadTemperatureC;
            CarcassTemperatureC = carcassTemperatureC;
            Smoothed = smoothed;
        }

        public float WearFraction { get; }
        public float TemperatureC { get; }            // surface node
        public float TreadTemperatureC { get; }       // tread node
        public float CarcassTemperatureC { get; }     // carcass + rim node
        public TireWearSmoothedInputs Smoothed { get; }

        // Back-compat convenience: the representative smoothed slip.
        public float SmoothedSlipNormalized => Smoothed.RepresentativeSlip;
    }

    public readonly struct TireWearInput
    {
        public TireWearInput(
            float elapsedSeconds,
            float speedMps,
            float slipAngleNormalized,
            float lateralSlipNormalized,
            float longitudinalSlipNormalized,
            float loadNormalized,
            float rollingResistanceNormalized,
            float ambientTemperatureC,
            float surfaceTemperatureC,
            float wetnessNormalized,
            float accelerationHeatStressNormalized = 0f,
            float brakeHeatStressNormalized = 0f,
            float engineBrakeHeatStressNormalized = 0f)
        {
            ElapsedSeconds = elapsedSeconds;
            SpeedMps = speedMps;
            SlipAngleNormalized = slipAngleNormalized;
            LateralSlipNormalized = lateralSlipNormalized;
            LongitudinalSlipNormalized = longitudinalSlipNormalized;
            LoadNormalized = loadNormalized;
            RollingResistanceNormalized = rollingResistanceNormalized;
            AmbientTemperatureC = ambientTemperatureC;
            SurfaceTemperatureC = surfaceTemperatureC;
            WetnessNormalized = wetnessNormalized;
            AccelerationHeatStressNormalized = accelerationHeatStressNormalized;
            BrakeHeatStressNormalized = brakeHeatStressNormalized;
            EngineBrakeHeatStressNormalized = engineBrakeHeatStressNormalized;
        }

        public float ElapsedSeconds { get; }
        public float SpeedMps { get; }
        public float SlipAngleNormalized { get; }
        public float LateralSlipNormalized { get; }
        public float LongitudinalSlipNormalized { get; }
        public float LoadNormalized { get; }
        public float RollingResistanceNormalized { get; }
        public float AmbientTemperatureC { get; }
        public float SurfaceTemperatureC { get; }
        public float WetnessNormalized { get; }
        public float AccelerationHeatStressNormalized { get; }
        public float BrakeHeatStressNormalized { get; }
        public float EngineBrakeHeatStressNormalized { get; }
    }

    public readonly struct TireWearRuntimeResult
    {
        public TireWearRuntimeResult(
            TireWearState state,
            float tractionGripScale,
            float lateralGripScale,
            float brakeGripScale,
            float combinedGripScale,
            float temperatureNormalized,
            float slipNormalized,
            float overheatNormalized,
            float heatingRateCPerSecond,
            float coolingRateCPerSecond)
        {
            State = state;
            TractionGripScale = tractionGripScale;
            LateralGripScale = lateralGripScale;
            BrakeGripScale = brakeGripScale;
            CombinedGripScale = combinedGripScale;
            TemperatureNormalized = temperatureNormalized;
            SlipNormalized = slipNormalized;
            OverheatNormalized = overheatNormalized;
            HeatingRateCPerSecond = heatingRateCPerSecond;
            CoolingRateCPerSecond = coolingRateCPerSecond;
        }

        public TireWearState State { get; }
        public float TractionGripScale { get; }
        public float LateralGripScale { get; }
        public float BrakeGripScale { get; }
        public float CombinedGripScale { get; }
        public float TemperatureNormalized { get; }
        public float SlipNormalized { get; }
        public float OverheatNormalized { get; }
        public float HeatingRateCPerSecond { get; }
        public float CoolingRateCPerSecond { get; }
    }
}
