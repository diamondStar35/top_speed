using System;

namespace TopSpeed.Physics.Tires.Wear
{
    public static class TireWearInputSignals
    {
        private const float DriveStressReferenceMps2 = 6f;
        private const float BrakeStressReferenceMps2 = 9f;
        // Engine braking is much weaker than the service brakes, so it saturates
        // at a lower deceleration.
        private const float EngineBrakeStressReferenceMps2 = 3.5f;

        // Separate heat-stress signals so braking, acceleration, and engine
        // braking can each drive tire heat independently (the merged
        // longitudinal signal above is kept for wear/load). 0 = none, 1 = the
        // reference stress or beyond.
        public static float ResolveAccelerationHeatStressNormalized(float driveAccelerationMps2)
        {
            return NormalizeUnit(driveAccelerationMps2, DriveStressReferenceMps2);
        }

        public static float ResolveBrakeHeatStressNormalized(float brakeDecelMps2)
        {
            return NormalizeUnit(brakeDecelMps2, BrakeStressReferenceMps2);
        }

        public static float ResolveEngineBrakeHeatStressNormalized(float engineBrakeDecelMps2)
        {
            return NormalizeUnit(engineBrakeDecelMps2, EngineBrakeStressReferenceMps2);
        }

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

        // Contact-patch load proxy. Lateral load transfer (cornering) is the
        // dominant term, but braking/acceleration transfer load too — hard
        // braking slams weight onto the front tires — so the longitudinal term
        // carries real weight, otherwise straight-line braking heats far less
        // than a corner at the same speed. Sum can exceed 1 under combined
        // loading; the Clamp01 saturates it.
        public static float ResolveLoadNormalized(float massKg, float lateralLoadRatio, float longitudinalSlipNormalized)
        {
            var massNormalized = Clamp01((massKg - 700f) / 1800f);
            return Clamp01(
                (lateralLoadRatio * 0.56f)
                + (Clamp01(longitudinalSlipNormalized) * 0.48f)
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
