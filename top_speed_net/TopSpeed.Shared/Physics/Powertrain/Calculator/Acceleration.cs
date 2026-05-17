using System;

namespace TopSpeed.Physics.Powertrain
{
    public static partial class Calculator
    {
        public static float DriveAccel(
            Config config,
            int gear,
            float speedMps,
            float throttle,
            float surfaceTractionModifier,
            float longitudinalGripFactor,
            float rollingResistanceModifier,
            ResistanceEnvironment resistanceEnvironment,
            float? driveRatioOverride = null,
            float? massKgOverride = null)
        {
            return DriveAccelCore(
                config,
                gear,
                inReverse: false,
                speedMps,
                throttle,
                surfaceTractionModifier,
                longitudinalGripFactor,
                rollingResistanceModifier,
                resistanceEnvironment,
                driveRatioOverride,
                massKgOverride);
        }

        public static float ReverseAccel(
            Config config,
            float speedMps,
            float throttle,
            float surfaceTractionModifier,
            float longitudinalGripFactor,
            float rollingResistanceModifier,
            ResistanceEnvironment resistanceEnvironment,
            float? massKgOverride = null)
        {
            return DriveAccelCore(
                config,
                1,
                inReverse: true,
                speedMps,
                throttle,
                surfaceTractionModifier,
                longitudinalGripFactor,
                rollingResistanceModifier,
                resistanceEnvironment,
                massKgOverride: massKgOverride);
        }

        private static float DriveAccelCore(
            Config config,
            int gear,
            bool inReverse,
            float speedMps,
            float throttle,
            float surfaceTractionModifier,
            float longitudinalGripFactor,
            float rollingResistanceModifier,
            ResistanceEnvironment resistanceEnvironment,
            float? driveRatioOverride = null,
            float? massKgOverride = null)
        {
            if (config == null)
                throw new ArgumentNullException(nameof(config));

            var clampedThrottle = Clamp(throttle, 0f, 1f);
            if (clampedThrottle <= 0f)
                return 0f;

            var driveRpm = DriveRpm(config, gear, speedMps, clampedThrottle, inReverse, driveRatioOverride);
            var engineTorque = EngineTorque(config, driveRpm) * clampedThrottle * config.PowerFactor;
            var ratio = inReverse
                ? config.ReverseGearRatio
                : (driveRatioOverride.HasValue && driveRatioOverride.Value > 0f
                    ? driveRatioOverride.Value
                    : config.GetGearRatio(gear));
            var wheelTorque = engineTorque * ratio * config.FinalDriveRatio * config.DrivetrainEfficiency;
            var wheelForce = wheelTorque / config.WheelRadiusM;
            var massKg = Math.Max(1f, massKgOverride.GetValueOrDefault(config.MassKg));
            var tractionLimit = config.TireGripCoefficient * surfaceTractionModifier * massKg * Gravity;
            if (wheelForce > tractionLimit)
                wheelForce = tractionLimit;
            wheelForce *= Clamp(longitudinalGripFactor, 0f, 1f);
            if (inReverse)
                wheelForce *= config.ReversePowerFactor;

            var passiveResistance = ResistanceModel.Compute(
                config,
                speedMps,
                rollingResistanceModifier,
                applyDrivelineDrag: false,
                drivelineDragParticipation: 0f,
                gear,
                inReverse,
                isNeutral: false,
                resistanceEnvironment,
                driveRatioOverride,
                massKgOverride: massKg);
            var netForce = wheelForce
                - passiveResistance.AerodynamicForceN
                - passiveResistance.RollingResistanceForceN
                - passiveResistance.WheelSideDragForceN;
            return netForce / massKg;
        }
    }
}
