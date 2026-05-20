using System;

namespace TopSpeed.Physics.Tires.Wear
{
    public static class TireWearInputSignals
    {
        private const float DriveStressReferenceMps2 = 6f;
        private const float BrakeStressReferenceMps2 = 9f;

        public static float ResolveLongitudinalSlipNormalized(float driveAccelerationMps2, float brakeDecelMps2)
        {
            var driveStress = NormalizeUnit(driveAccelerationMps2, DriveStressReferenceMps2);
            var brakeStress = NormalizeUnit(brakeDecelMps2, BrakeStressReferenceMps2);

            var driveSlide = SmoothStep((driveStress - 0.52f) / 0.48f);
            var brakeSlide = SmoothStep((brakeStress - 0.44f) / 0.56f);
            var slideSignal = Clamp01((driveSlide * 0.52f) + (brakeSlide * 0.48f));

            // Preserve a small stress contribution so repeated hard acceleration/braking still warms tires.
            var stressSignal = Clamp01((driveStress * 0.14f) + (brakeStress * 0.18f));
            return Clamp01(slideSignal + (stressSignal * 0.22f));
        }

        public static float ResolveLoadNormalized(float massKg, float lateralLoadRatio, float longitudinalSlipNormalized)
        {
            var massNormalized = Clamp01((massKg - 700f) / 1800f);
            return Clamp01(
                (lateralLoadRatio * 0.56f)
                + (Clamp01(longitudinalSlipNormalized) * 0.24f)
                + (massNormalized * 0.20f));
        }

        public static float ResolveRollingResistanceNormalized(
            float rollingResistanceCoefficient,
            float surfaceRollingResistanceFactor,
            float speedMps)
        {
            var speedFactor = Clamp01(speedMps / 45f);
            var normalizedRollingCoefficient = Clamp01(
                (rollingResistanceCoefficient * Math.Max(0.1f, surfaceRollingResistanceFactor)) / 0.030f);
            return Clamp01(normalizedRollingCoefficient * speedFactor);
        }

        private static float NormalizeUnit(float value, float reference)
        {
            if (reference <= 0f)
                return 0f;
            return Clamp01(value / reference);
        }

        private static float SmoothStep(float value)
        {
            var t = Clamp01(value);
            return t * t * (3f - (2f * t));
        }

        private static float Clamp01(float value)
        {
            return Clamp(value, 0f, 1f);
        }

        private static float Clamp(float value, float min, float max)
        {
            if (value < min)
                return min;
            if (value > max)
                return max;
            return value;
        }
    }
}
