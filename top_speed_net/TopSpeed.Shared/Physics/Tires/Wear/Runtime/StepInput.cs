using System;

namespace TopSpeed.Physics.Tires.Wear
{
    internal readonly struct TireWearStepInput
    {
        private TireWearStepInput(
            float elapsedSeconds,
            float speedMps,
            float slipAngleNormalized,
            float lateralSlipNormalized,
            float longitudinalSlipNormalized,
            float loadNormalized,
            float rollingResistanceNormalized,
            float corneringSlipNormalized,
            float rawSlipNormalized)
        {
            ElapsedSeconds = elapsedSeconds;
            SpeedMps = speedMps;
            SlipAngleNormalized = slipAngleNormalized;
            LateralSlipNormalized = lateralSlipNormalized;
            LongitudinalSlipNormalized = longitudinalSlipNormalized;
            LoadNormalized = loadNormalized;
            RollingResistanceNormalized = rollingResistanceNormalized;
            CorneringSlipNormalized = corneringSlipNormalized;
            RawSlipNormalized = rawSlipNormalized;
        }

        public float ElapsedSeconds { get; }
        public float SpeedMps { get; }
        public float SlipAngleNormalized { get; }
        public float LateralSlipNormalized { get; }
        public float LongitudinalSlipNormalized { get; }
        public float LoadNormalized { get; }
        public float RollingResistanceNormalized { get; }
        public float CorneringSlipNormalized { get; }
        public float RawSlipNormalized { get; }

        public static TireWearStepInput Create(in TireWearInput input)
        {
            var elapsedSeconds = Math.Max(0f, input.ElapsedSeconds);
            var speedMps = Math.Max(0f, input.SpeedMps);
            var slipAngleNormalized = TireWearMath.Clamp01(input.SlipAngleNormalized);
            var lateralSlipNormalized = TireWearMath.Clamp01(input.LateralSlipNormalized);
            var longitudinalSlipNormalized = TireWearMath.Clamp01(input.LongitudinalSlipNormalized);
            var loadNormalized = TireWearMath.Clamp01(input.LoadNormalized);
            var rollingResistanceNormalized = TireWearMath.Clamp01(input.RollingResistanceNormalized);
            var corneringSlipNormalized = TireWearMath.Clamp01((slipAngleNormalized * 0.78f) + (lateralSlipNormalized * 0.22f));
            var rawSlipNormalized = TireWearMath.Clamp01((corneringSlipNormalized * 0.56f) + (longitudinalSlipNormalized * 0.44f));

            return new TireWearStepInput(
                elapsedSeconds,
                speedMps,
                slipAngleNormalized,
                lateralSlipNormalized,
                longitudinalSlipNormalized,
                loadNormalized,
                rollingResistanceNormalized,
                corneringSlipNormalized,
                rawSlipNormalized);
        }
    }
}
