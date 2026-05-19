namespace TopSpeed.Physics.Tires.Wear
{
    internal readonly struct TireWearThermalControl
    {
        public TireWearThermalControl(float heatingScale, float coolingScale)
        {
            HeatingScale = heatingScale;
            CoolingScale = coolingScale;
        }

        public float HeatingScale { get; }
        public float CoolingScale { get; }

        public static TireWearThermalControl Resolve(TireWearConfig config, in TireWearState state)
        {
            var wearFraction = TireWearMath.Clamp01(state.WearFraction);
            var overheatNormalized = TireWearTemperature.ResolveOverheat(config, state.TemperatureC);
            var wearDegradation = TireWearMath.Clamp01((wearFraction - 0.55f) / 0.45f);
            var criticalWear = TireWearMath.Clamp01((wearFraction - 0.78f) / 0.18f);
            var overheatSignal = TireWearMath.Pow(overheatNormalized, 1.15f);
            var thermalDamage = TireWearMath.Clamp01(
                (wearDegradation * 0.68f)
                + (criticalWear * 0.22f)
                + (overheatSignal * 0.10f));
            var runawaySignal = criticalWear * TireWearMath.Clamp01((overheatNormalized - 0.30f) / 0.70f);
            var runawayEscalation = runawaySignal * runawaySignal;

            var heatingScale = TireWearMath.Lerp(0.98f, 1.12f, thermalDamage) + (0.62f * runawayEscalation);
            var coolingScale = TireWearMath.Lerp(1.05f, 0.92f, thermalDamage);
            coolingScale *= TireWearMath.Lerp(1f, 0.84f, runawaySignal);
            coolingScale *= TireWearMath.Lerp(1f, 0.81f, runawayEscalation);

            return new TireWearThermalControl(
                TireWearMath.Clamp(heatingScale, 0.90f, 1.95f),
                TireWearMath.Clamp(coolingScale, 0.56f, 1.25f));
        }
    }
}
