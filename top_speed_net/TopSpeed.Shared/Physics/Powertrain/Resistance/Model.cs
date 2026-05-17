using System;

namespace TopSpeed.Physics.Powertrain
{
    public static class ResistanceModel
    {
        private const float Gravity = 9.80665f;
        private const float TwoPi = (float)(Math.PI * 2.0);

        public static ResistanceBreakdown Compute(
            Config config,
            float speedMps,
            float rollingResistanceModifier,
            bool applyDrivelineDrag,
            float drivelineDragParticipation,
            int gear,
            bool inReverse,
            bool isNeutral,
            in ResistanceEnvironment environment,
            float? driveRatioOverride = null,
            float? massKgOverride = null)
        {
            if (config == null)
                throw new ArgumentNullException(nameof(config));

            var massKg = ResolveMassKg(config, massKgOverride);
            var aeroForce = AerodynamicForce(config, speedMps, in environment);
            var rollingForce = RollingResistanceForce(config, speedMps, rollingResistanceModifier, massKg);
            var wheelSideForce = WheelSideDragForce(config, speedMps);
            var coupledDrivelineForce = applyDrivelineDrag
                ? CoupledDrivelineDragForce(
                    config,
                    speedMps,
                    drivelineDragParticipation,
                    gear,
                    inReverse,
                    isNeutral,
                    driveRatioOverride)
                : 0f;
            return new ResistanceBreakdown(aeroForce, rollingForce, wheelSideForce, coupledDrivelineForce);
        }

        public static float AerodynamicForce(Config config, float speedMps, in ResistanceEnvironment environment)
        {
            var relativeLongitudinalMps = speedMps + environment.LongitudinalWindMps;
            var relativeLateralMps = environment.LateralWindMps;
            var relativeAirSpeedMps = (float)Math.Sqrt(
                (relativeLongitudinalMps * relativeLongitudinalMps) +
                (relativeLateralMps * relativeLateralMps));
            if (relativeAirSpeedMps <= 0.001f)
                return 0f;

            var absLongitudinal = Math.Abs(relativeLongitudinalMps);
            var absLateral = Math.Abs(relativeLateralMps);
            var projectedAreaM2 =
                ((config.FrontalAreaM2 * absLongitudinal) + (config.SideAreaM2 * absLateral)) /
                Math.Max(0.001f, relativeAirSpeedMps);
            var dragMagnitudeN = 0.5f
                * environment.AirDensityKgPerM3
                * config.DragCoefficient
                * projectedAreaM2
                * relativeAirSpeedMps
                * relativeAirSpeedMps
                * environment.DraftingFactor;
            return dragMagnitudeN * (relativeLongitudinalMps / relativeAirSpeedMps);
        }

        public static float RollingResistanceForce(Config config, float speedMps, float rollingResistanceModifier, float? massKgOverride = null)
        {
            var massKg = ResolveMassKg(config, massKgOverride);
            var speedFactor = 1f + (Math.Max(0f, config.RollingResistanceSpeedFactor) * Math.Max(0f, speedMps));
            return config.RollingResistanceCoefficient
                * massKg
                * Gravity
                * speedFactor
                * Math.Max(0f, rollingResistanceModifier);
        }

        public static float WheelSideDragForce(Config config, float speedMps)
        {
            if (speedMps <= 0f)
                return 0f;

            return config.WheelSideDragBaseN + (config.WheelSideDragLinearNPerMps * Math.Max(0f, speedMps));
        }

        public static float CoupledDrivelineDragForce(
            Config config,
            float speedMps,
            float drivelineDragParticipation,
            int gear,
            bool inReverse,
            bool isNeutral,
            float? driveRatioOverride = null)
        {
            if (isNeutral || drivelineDragParticipation <= 0f || speedMps <= 0f || config.WheelRadiusM <= 0f)
                return 0f;

            var ratio = inReverse
                ? config.ReverseGearRatio
                : (driveRatioOverride.HasValue && driveRatioOverride.Value > 0f
                    ? driveRatioOverride.Value
                    : config.GetGearRatio(gear));
            if (ratio <= 0f)
                return 0f;

            var wheelCircumference = config.WheelRadiusM * TwoPi;
            if (wheelCircumference <= 0f)
                return 0f;

            var coupledRpm = (speedMps / wheelCircumference) * 60f * ratio * config.FinalDriveRatio;
            var coupledKrpm = Math.Max(0f, coupledRpm / 1000f);
            var engineLossTorqueNm = config.CoupledDrivelineDragNm + (config.CoupledDrivelineViscousDragNmPerKrpm * coupledKrpm);
            if (engineLossTorqueNm <= 0f)
                return 0f;

            var wheelTorqueNm = engineLossTorqueNm * ratio * config.FinalDriveRatio * config.DrivetrainEfficiency;
            return Math.Max(0f, wheelTorqueNm / config.WheelRadiusM) * Math.Max(0f, drivelineDragParticipation);
        }

        private static float ResolveMassKg(Config config, float? massKgOverride)
        {
            var massKg = massKgOverride.GetValueOrDefault(config.MassKg);
            return Math.Max(1f, massKg);
        }
    }
}
