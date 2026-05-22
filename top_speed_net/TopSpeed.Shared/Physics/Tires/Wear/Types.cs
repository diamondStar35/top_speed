namespace TopSpeed.Physics.Tires.Wear
{
    // Two-node thermal state. `TemperatureC` is the surface (tread / contact
    // patch) — what the player's gauge reads and what drives grip. `CarcassTemperatureC`
    // is the bulk rubber + belts: high thermal mass, slow response, acts as a
    // reservoir that decouples warm-up time from spike-recovery time.
    public readonly struct TireWearState
    {
        public TireWearState(
            float wearFraction,
            float temperatureC,
            float carcassTemperatureC,
            float smoothedSlipNormalized)
        {
            WearFraction = wearFraction;
            TemperatureC = temperatureC;
            CarcassTemperatureC = carcassTemperatureC;
            SmoothedSlipNormalized = smoothedSlipNormalized;
        }

        public float WearFraction { get; }
        public float TemperatureC { get; }
        public float CarcassTemperatureC { get; }
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
            float wetnessNormalized)
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
