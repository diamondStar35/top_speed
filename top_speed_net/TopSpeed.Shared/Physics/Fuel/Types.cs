namespace TopSpeed.Physics.Fuel
{
    public readonly struct FuelRuntimeState
    {
        public FuelRuntimeState(float remainingLiters, float smoothedBurnLitersPerSecond)
        {
            RemainingLiters = remainingLiters;
            SmoothedBurnLitersPerSecond = smoothedBurnLitersPerSecond;
        }

        public float RemainingLiters { get; }
        public float SmoothedBurnLitersPerSecond { get; }
    }

    public readonly struct FuelRuntimeInput
    {
        public FuelRuntimeInput(
            float elapsedSeconds,
            bool combustionActive,
            float throttleNormalized,
            float netPowerKw,
            float speedMps)
        {
            ElapsedSeconds = elapsedSeconds;
            CombustionActive = combustionActive;
            ThrottleNormalized = throttleNormalized;
            NetPowerKw = netPowerKw;
            SpeedMps = speedMps;
        }

        public float ElapsedSeconds { get; }
        public bool CombustionActive { get; }
        public float ThrottleNormalized { get; }
        public float NetPowerKw { get; }
        public float SpeedMps { get; }
    }

    public readonly struct FuelRuntimeResult
    {
        public FuelRuntimeResult(
            FuelRuntimeState state,
            float burnLitersPerSecond,
            float burnLitersPerHour,
            float fuelPercent,
            bool lowFuel,
            bool emptyFuel,
            float powerScale,
            float estimatedRangeMeters,
            float efficiencyLitersPer100Km,
            float efficiencyMpg)
        {
            State = state;
            BurnLitersPerSecond = burnLitersPerSecond;
            BurnLitersPerHour = burnLitersPerHour;
            FuelPercent = fuelPercent;
            LowFuel = lowFuel;
            EmptyFuel = emptyFuel;
            PowerScale = powerScale;
            EstimatedRangeMeters = estimatedRangeMeters;
            EfficiencyLitersPer100Km = efficiencyLitersPer100Km;
            EfficiencyMpg = efficiencyMpg;
        }

        public FuelRuntimeState State { get; }
        public float BurnLitersPerSecond { get; }
        public float BurnLitersPerHour { get; }
        public float FuelPercent { get; }
        public bool LowFuel { get; }
        public bool EmptyFuel { get; }
        public float PowerScale { get; }
        public float EstimatedRangeMeters { get; }
        public float EfficiencyLitersPer100Km { get; }
        public float EfficiencyMpg { get; }
    }
}
