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
            var wearDegradation = TireWearMath.Clamp01((wearFraction - 0.45f) / 0.55f);
            var lateWear = TireWearMath.Clamp01((wearFraction - 0.78f) / 0.22f);
            var severeWear = TireWearMath.Pow(lateWear, 1.35f);
            var overheatStress = TireWearMath.Pow(overheatNormalized, 1.20f);
            var thermalDamage = TireWearMath.Clamp01(
                (wearDegradation * 0.62f)
                + (severeWear * 0.28f)
                + (overheatStress * 0.10f));
            var runawaySignal = severeWear * TireWearMath.Clamp01((overheatNormalized - 0.35f) / 0.65f);
            var runawayEscalation = runawaySignal * runawaySignal;

            // Fresh tires self-regulate heat better; heavy wear progressively removes that buffer.
            var heatingScale = TireWearMath.Lerp(1.02f, 1.12f, thermalDamage) + (0.30f * runawayEscalation);
            var coolingScale = TireWearMath.Lerp(1.06f, 0.90f, thermalDamage);
            coolingScale *= TireWearMath.Lerp(1f, 0.86f, runawaySignal);
            coolingScale *= TireWearMath.Lerp(1f, 0.78f, runawayEscalation);

            return new TireWearThermalControl(
                TireWearMath.Clamp(heatingScale, 0.82f, 1.70f),
                TireWearMath.Clamp(coolingScale, 0.62f, 1.25f));
        }
    }
}
