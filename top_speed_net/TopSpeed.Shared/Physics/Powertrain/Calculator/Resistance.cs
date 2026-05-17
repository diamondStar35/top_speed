using System;

namespace TopSpeed.Physics.Powertrain
{
    public static partial class Calculator
    {
        public static float BrakeDecelKph(
            Config config,
            float brakeInput,
            float surfaceBrakeModifier)
        {
            if (config == null)
                throw new ArgumentNullException(nameof(config));
            if (brakeInput <= 0f)
                return 0f;

            var grip = Math.Max(0.1f, config.TireGripCoefficient * surfaceBrakeModifier);
            var decelMps2 = Clamp(brakeInput, 0f, 1f) * config.BrakeStrength * grip * Gravity;
            return decelMps2 * 3.6f;
        }

        public static float AerodynamicDecelKph(
            Config config,
            float speedMps,
            in ResistanceEnvironment environment,
            float? massKgOverride = null)
        {
            if (config == null)
                throw new ArgumentNullException(nameof(config));
            var massKg = Math.Max(1f, massKgOverride.GetValueOrDefault(config.MassKg));
            return (ResistanceModel.AerodynamicForce(config, speedMps, in environment) / massKg) * 3.6f;
        }

        public static float RollingResistanceDecelKph(
            Config config,
            float speedMps,
            float rollingResistanceModifier,
            float? massKgOverride = null)
        {
            if (config == null)
                throw new ArgumentNullException(nameof(config));
            var massKg = Math.Max(1f, massKgOverride.GetValueOrDefault(config.MassKg));
            return (ResistanceModel.RollingResistanceForce(config, speedMps, rollingResistanceModifier, massKg) / massKg) * 3.6f;
        }

        public static float WheelSideDragDecelKph(
            Config config,
            float speedMps,
            float? massKgOverride = null)
        {
            if (config == null)
                throw new ArgumentNullException(nameof(config));
            var massKg = Math.Max(1f, massKgOverride.GetValueOrDefault(config.MassKg));
            return (ResistanceModel.WheelSideDragForce(config, speedMps) / massKg) * 3.6f;
        }

        public static float EngineBrakeDecelKph(
            Config config,
            int gear,
            bool inReverse,
            float speedMps,
            float surfaceBrakeModifier,
            float currentEngineRpm,
            float? driveRatioOverride = null,
            float? massKgOverride = null)
        {
            if (config == null)
                throw new ArgumentNullException(nameof(config));
            var massKg = Math.Max(1f, massKgOverride.GetValueOrDefault(config.MassKg));
            if (config.EngineBrakingTorqueNm <= 0f || massKg <= 0f || config.WheelRadiusM <= 0f)
                return 0f;

            var rpmRange = config.RevLimiter - config.IdleRpm;
            if (rpmRange <= 0f)
                return 0f;

            var ratio = inReverse
                ? config.ReverseGearRatio
                : (driveRatioOverride.HasValue && driveRatioOverride.Value > 0f
                    ? driveRatioOverride.Value
                    : config.GetGearRatio(gear));
            var speedBasedRpm = RpmForRatio(config, speedMps, ratio);
            var effectiveRpm = Math.Max(config.IdleRpm, Math.Max(currentEngineRpm, speedBasedRpm));

            var engineLossTorque = EngineLossTorqueNm(config, effectiveRpm, closedThrottle: true);
            if (engineLossTorque <= 0f)
                return 0f;

            var wheelTorque = engineLossTorque * ratio * config.FinalDriveRatio * config.DrivetrainEfficiency * config.EngineBrakeTransferEfficiency;
            var wheelForce = wheelTorque / config.WheelRadiusM;
            var totalDriveRatio = ratio * config.FinalDriveRatio;
            var reflectedEngineInertia = config.EngineInertiaKgm2 * totalDriveRatio * totalDriveRatio;
            var equivalentMassFromEngineInertia = reflectedEngineInertia / Math.Max(0.0001f, config.WheelRadiusM * config.WheelRadiusM);
            var effectiveMass = massKg + Math.Max(0f, equivalentMassFromEngineInertia);
            var decelMps2 = (wheelForce / effectiveMass) * surfaceBrakeModifier;
            return Math.Max(0f, decelMps2 * 3.6f);
        }

        private static float RpmForRatio(Config config, float speedMps, float ratio)
        {
            var wheelCircumference = config.WheelRadiusM * TwoPi;
            if (wheelCircumference <= 0f || ratio <= 0f)
                return 0f;
            return (speedMps / wheelCircumference) * 60f * ratio * config.FinalDriveRatio;
        }
    }
}
