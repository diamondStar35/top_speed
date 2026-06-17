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
            float corneringUtilizationNormalized,
            float corneringSlipNormalized,
            float longitudinalSlideNormalized,
            float rawSlipNormalized,
            float accelerationHeatStressNormalized,
            float brakeHeatStressNormalized,
            float engineBrakeHeatStressNormalized)
        {
            ElapsedSeconds = elapsedSeconds;
            SpeedMps = speedMps;
            SlipAngleNormalized = slipAngleNormalized;
            LateralSlipNormalized = lateralSlipNormalized;
            LongitudinalSlipNormalized = longitudinalSlipNormalized;
            LoadNormalized = loadNormalized;
            RollingResistanceNormalized = rollingResistanceNormalized;
            CorneringUtilizationNormalized = corneringUtilizationNormalized;
            CorneringSlipNormalized = corneringSlipNormalized;
            LongitudinalSlideNormalized = longitudinalSlideNormalized;
            RawSlipNormalized = rawSlipNormalized;
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
        public float CorneringUtilizationNormalized { get; }
        public float CorneringSlipNormalized { get; }
        public float LongitudinalSlideNormalized { get; }
        public float RawSlipNormalized { get; }
        public float AccelerationHeatStressNormalized { get; }
        public float BrakeHeatStressNormalized { get; }
        public float EngineBrakeHeatStressNormalized { get; }

        public static TireWearStepInput Create(in TireWearInput input)
        {
            var elapsedSeconds = Math.Max(0f, input.ElapsedSeconds);
            var speedMps = Math.Max(0f, input.SpeedMps);
            var slipAngleNormalized = TireWearMath.Clamp(input.SlipAngleNormalized, 0f, 3f);
            var lateralSlipNormalized = TireWearMath.Clamp(input.LateralSlipNormalized, 0f, 3f);
            var longitudinalSlipNormalized = TireWearMath.Clamp01(input.LongitudinalSlipNormalized);
            var loadNormalized = TireWearMath.Clamp01(input.LoadNormalized);
            var rollingResistanceNormalized = TireWearMath.Clamp01(input.RollingResistanceNormalized);
            var corneringCombined = (slipAngleNormalized * 0.74f) + (lateralSlipNormalized * 0.26f);
            var corneringUtilizationNormalized = TireWearMath.Clamp01(corneringCombined);
            var corneringSlipNormalized = TireWearMath.Clamp01((corneringCombined - 1f) / 1.2f);
            var longitudinalSlideNormalized = TireWearMath.Clamp01((longitudinalSlipNormalized - 0.52f) / 0.48f);
            var rawSlipNormalized = TireWearMath.Clamp01(
                (corneringSlipNormalized * 0.60f)
                + (longitudinalSlideNormalized * 0.40f));
            var accelerationHeatStressNormalized = TireWearMath.Clamp01(input.AccelerationHeatStressNormalized);
            var brakeHeatStressNormalized = TireWearMath.Clamp01(input.BrakeHeatStressNormalized);
            var engineBrakeHeatStressNormalized = TireWearMath.Clamp01(input.EngineBrakeHeatStressNormalized);

            return new TireWearStepInput(
                elapsedSeconds,
                speedMps,
                slipAngleNormalized,
                lateralSlipNormalized,
                longitudinalSlipNormalized,
                loadNormalized,
                rollingResistanceNormalized,
                corneringUtilizationNormalized,
                corneringSlipNormalized,
                longitudinalSlideNormalized,
                rawSlipNormalized,
                accelerationHeatStressNormalized,
                brakeHeatStressNormalized,
                engineBrakeHeatStressNormalized);
        }
    }
}
