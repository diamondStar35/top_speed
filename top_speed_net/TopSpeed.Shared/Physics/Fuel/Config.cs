using System;

namespace TopSpeed.Physics.Fuel
{
    public sealed class FuelConfig
    {
        public FuelConfig(
            float tankCapacityLiters,
            float engineDisplacementLiters,
            float fuelDensityKgPerLiter = 0.745f,
            float baseIdleBurnLitersPerHourPerLiter = 0.30f,
            float bsfcLowLoadGPerKwh = 360f,
            float bsfcHighLoadGPerKwh = 250f,
            float displacementEfficiencyPenaltyPerLiter = 0.045f,
            float referencePowerKw = 180f,
            float lowFuelFraction = 0.12f,
            float leanStartFuelFraction = 0.06f,
            float emptyFuelPowerScale = 0.30f,
            float burnSmoothingTimeConstantSeconds = 2.5f)
        {
            TankCapacityLiters = Math.Max(FuelDefaults.MinTankCapacityLiters, Math.Min(FuelDefaults.MaxTankCapacityLiters, tankCapacityLiters));
            EngineDisplacementLiters = Math.Max(FuelDefaults.MinEngineDisplacementLiters, Math.Min(FuelDefaults.MaxEngineDisplacementLiters, engineDisplacementLiters));
            FuelDensityKgPerLiter = Math.Max(0.5f, Math.Min(1.2f, fuelDensityKgPerLiter));
            BaseIdleBurnLitersPerHourPerLiter = Math.Max(0f, Math.Min(5f, baseIdleBurnLitersPerHourPerLiter));
            BsfcLowLoadGPerKwh = Math.Max(120f, Math.Min(800f, bsfcLowLoadGPerKwh));
            BsfcHighLoadGPerKwh = Math.Max(120f, Math.Min(800f, bsfcHighLoadGPerKwh));
            if (BsfcHighLoadGPerKwh > BsfcLowLoadGPerKwh)
            {
                var swap = BsfcHighLoadGPerKwh;
                BsfcHighLoadGPerKwh = BsfcLowLoadGPerKwh;
                BsfcLowLoadGPerKwh = swap;
            }

            DisplacementEfficiencyPenaltyPerLiter = Math.Max(-0.2f, Math.Min(0.3f, displacementEfficiencyPenaltyPerLiter));
            ReferencePowerKw = Math.Max(20f, Math.Min(1000f, referencePowerKw));
            LowFuelFraction = Math.Max(0f, Math.Min(1f, lowFuelFraction));
            LeanStartFuelFraction = Math.Max(0f, Math.Min(1f, leanStartFuelFraction));
            if (LeanStartFuelFraction > LowFuelFraction)
                LeanStartFuelFraction = LowFuelFraction;
            EmptyFuelPowerScale = Math.Max(0f, Math.Min(1f, emptyFuelPowerScale));
            BurnSmoothingTimeConstantSeconds = Math.Max(0.1f, Math.Min(60f, burnSmoothingTimeConstantSeconds));
        }

        public float TankCapacityLiters { get; }
        public float EngineDisplacementLiters { get; }
        public float FuelDensityKgPerLiter { get; }
        public float BaseIdleBurnLitersPerHourPerLiter { get; }
        public float BsfcLowLoadGPerKwh { get; private set; }
        public float BsfcHighLoadGPerKwh { get; private set; }
        public float DisplacementEfficiencyPenaltyPerLiter { get; }
        public float ReferencePowerKw { get; }
        public float LowFuelFraction { get; }
        public float LeanStartFuelFraction { get; private set; }
        public float EmptyFuelPowerScale { get; }
        public float BurnSmoothingTimeConstantSeconds { get; }
    }
}
