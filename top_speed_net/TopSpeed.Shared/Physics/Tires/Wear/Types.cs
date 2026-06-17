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
    public readonly struct TireWearState
    {
        public TireWearState(
            float wearFraction,
            float temperatureC,
            float treadTemperatureC,
            float carcassTemperatureC,
            float smoothedSlipNormalized)
        {
            WearFraction = wearFraction;
            TemperatureC = temperatureC;
            TreadTemperatureC = treadTemperatureC;
            CarcassTemperatureC = carcassTemperatureC;
            SmoothedSlipNormalized = smoothedSlipNormalized;
        }

        public float WearFraction { get; }
        public float TemperatureC { get; }            // surface node
        public float TreadTemperatureC { get; }       // tread node
        public float CarcassTemperatureC { get; }     // carcass + rim node
        public float SmoothedSlipNormalized { get; }
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
