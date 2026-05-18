namespace TopSpeed.Physics.Tires
{
    public readonly struct TireModelOutput
    {
        public TireModelOutput(
            float longitudinalGripFactor,
            float lateralSpeedMps,
            float lateralLoadRatio,
            float slipAngleNormalized,
            float lateralSlipNormalized,
            TireModelState state)
        {
            LongitudinalGripFactor = longitudinalGripFactor;
            LateralSpeedMps = lateralSpeedMps;
            LateralLoadRatio = lateralLoadRatio;
            SlipAngleNormalized = slipAngleNormalized;
            LateralSlipNormalized = lateralSlipNormalized;
            State = state;
        }

        public float LongitudinalGripFactor { get; }
        public float LateralSpeedMps { get; }
        public float LateralLoadRatio { get; }
        public float SlipAngleNormalized { get; }
        public float LateralSlipNormalized { get; }
        public TireModelState State { get; }
    }
}
