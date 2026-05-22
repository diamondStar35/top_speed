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
            var wearDegradation = TireWearMath.Clamp01((wearFraction - 0.70f) / 0.30f);
            var lateWear = TireWearMath.Clamp01((wearFraction - 0.88f) / 0.12f);
            var severeWear = TireWearMath.Pow(lateWear, 1.30f);
            var overheatStress = TireWearMath.Pow(overheatNormalized, 1.25f);
            var thermalDamage = TireWearMath.Clamp01(
                (wearDegradation * 0.52f)
                + (severeWear * 0.32f)
                + (overheatStress * 0.16f));
            var runawaySignal = severeWear * TireWearMath.Clamp01((overheatNormalized - 0.20f) / 0.80f);
            var runawayEscalation = runawaySignal * runawaySignal;

            // Fresh tires self-regulate heat better; heavy wear progressively removes that buffer.
            var heatingScale = TireWearMath.Lerp(1.00f, 1.08f, thermalDamage) + (0.36f * runawayEscalation);
            var coolingScale = TireWearMath.Lerp(1.08f, 0.92f, thermalDamage);
            coolingScale *= TireWearMath.Lerp(1f, 0.86f, runawaySignal);
            coolingScale *= TireWearMath.Lerp(1f, 0.74f, runawayEscalation);

            return new TireWearThermalControl(
                TireWearMath.Clamp(heatingScale, 0.84f, 1.72f),
                TireWearMath.Clamp(coolingScale, 0.60f, 1.26f));
        }
    }
}
