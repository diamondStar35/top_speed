using System;

namespace TopSpeed.Physics.Powertrain
{
    public static class LongitudinalStep
    {
        private const float TwoPi = (float)(Math.PI * 2.0);
        private const float CoastStopSnapSpeedMps = 0.03f;

        public static int ResolveThrust(int throttleInput, int brakeInput)
        {
            if (throttleInput == 0)
                return brakeInput;
            if (brakeInput == 0)
                return throttleInput;
            return -brakeInput > throttleInput ? brakeInput : throttleInput;
        }

        public static LongitudinalStepResult Compute(in LongitudinalStepInput input)
        {
            if (input.ElapsedSeconds <= 0f)
                return new LongitudinalStepResult(0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f);

            if (input.RequestDrive)
                return ComputeDrive(in input);

            return ComputeCoast(in input);
        }

        private static LongitudinalStepResult ComputeDrive(in LongitudinalStepInput input)
        {
            var throttle = Clamp(input.Throttle, 0f, 1f);
            var coupling = Clamp(input.DrivelineCouplingFactor, 0f, 1f);
            var driveScale = Math.Max(0f, input.DriveAccelerationScale);
            var effectiveMassKg = ResolveEffectiveMassKg(input.Config, input.EffectiveMassKg);
            var participation = TransmissionLossModel.Resolve(
                input.TransmissionType,
                input.IsNeutral,
                input.GearPathEngaged,
                input.DrivelineCouplingFactor);
            var accelMps2 = input.InReverse
                ? Calculator.ReverseAccel(
                    input.Config,
                    input.SpeedMps,
                    throttle,
                    input.SurfaceTractionModifier,
                    input.LongitudinalGripFactor,
                    input.SurfaceRollingResistanceModifier,
                    input.ResistanceEnvironment,
                    massKgOverride: effectiveMassKg)
                : Calculator.DriveAccel(
                    input.Config,
                    input.Gear,
                    input.SpeedMps,
                    throttle,
                    input.SurfaceTractionModifier,
                    input.LongitudinalGripFactor,
                    input.SurfaceRollingResistanceModifier,
                    input.ResistanceEnvironment,
                    input.DriveRatioOverride,
                    massKgOverride: effectiveMassKg);

            var resistance = ResistanceModel.Compute(
                input.Config,
                input.SpeedMps,
                input.SurfaceRollingResistanceModifier,
                applyDrivelineDrag: true,
                participation.DrivelineDragParticipation,
                input.Gear,
                input.InReverse,
                input.IsNeutral,
                input.ResistanceEnvironment,
                input.DriveRatioOverride,
                massKgOverride: effectiveMassKg);
            var aerodynamicDecelKph = ForceToKphPerSecond(effectiveMassKg, resistance.AerodynamicForceN);
            var rollingResistanceDecelKph = ForceToKphPerSecond(effectiveMassKg, resistance.RollingResistanceForceN);
            var wheelSideDragDecelKph = ForceToKphPerSecond(effectiveMassKg, resistance.WheelSideDragForceN);
            var coupledDrivelineDragDecelKph = ForceToKphPerSecond(effectiveMassKg, resistance.CoupledDrivelineDragForceN);
            accelMps2 = (accelMps2 * coupling * driveScale) - ForceToMps2(effectiveMassKg, resistance.CoupledDrivelineDragForceN);

            var newSpeedMps = Math.Max(0f, input.SpeedMps + (accelMps2 * input.ElapsedSeconds));
            var speedDeltaKph = (newSpeedMps - input.SpeedMps) * 3.6f;
            var coupledDriveRpm = CoupledRpm(input.Config, input.Gear, newSpeedMps, input.InReverse, input.DriveRatioOverride);
            return new LongitudinalStepResult(
                speedDeltaKph,
                coupledDriveRpm,
                accelMps2,
                totalDecelKph: Math.Max(0f, aerodynamicDecelKph + rollingResistanceDecelKph + wheelSideDragDecelKph + coupledDrivelineDragDecelKph),
                brakeDecelKph: 0f,
                engineBrakeDecelKph: 0f,
                aerodynamicDecelKph,
                rollingResistanceDecelKph,
                wheelSideDragDecelKph,
                coupledDrivelineDragDecelKph);
        }

        private static LongitudinalStepResult ComputeCoast(in LongitudinalStepInput input)
        {
            var brakeInput = Clamp(input.Brake, 0f, 1f);
            var effectiveMassKg = ResolveEffectiveMassKg(input.Config, input.EffectiveMassKg);
            var participation = TransmissionLossModel.Resolve(
                input.TransmissionType,
                input.IsNeutral,
                input.GearPathEngaged,
                input.DrivelineCouplingFactor);
            var brakeDecel = input.RequestBrake
                ? Calculator.BrakeDecelKph(input.Config, brakeInput, input.SurfaceBrakeModifier)
                : 0f;
            var engineBrakeDecel = input.ApplyEngineBraking && participation.EngineBrakeParticipation > 0f
                ? Calculator.EngineBrakeDecelKph(
                    input.Config,
                    input.Gear,
                    input.InReverse,
                    input.SpeedMps,
                    input.SurfaceBrakeModifier,
                    input.CurrentEngineRpm,
                    input.DriveRatioOverride,
                    massKgOverride: effectiveMassKg) * participation.EngineBrakeParticipation
                : 0f;
            var resistance = ResistanceModel.Compute(
                input.Config,
                input.SpeedMps,
                input.SurfaceRollingResistanceModifier,
                applyDrivelineDrag: true,
                participation.DrivelineDragParticipation,
                input.Gear,
                input.InReverse,
                input.IsNeutral,
                input.ResistanceEnvironment,
                input.DriveRatioOverride,
                massKgOverride: effectiveMassKg);
            var aerodynamicDecelKph = ForceToKphPerSecond(effectiveMassKg, resistance.AerodynamicForceN);
            var rollingResistanceDecelKph = ForceToKphPerSecond(effectiveMassKg, resistance.RollingResistanceForceN);
            var wheelSideDragDecelKph = ForceToKphPerSecond(effectiveMassKg, resistance.WheelSideDragForceN);
            var coupledDrivelineDragDecelKph = ForceToKphPerSecond(effectiveMassKg, resistance.CoupledDrivelineDragForceN);
            var totalDecelKph = aerodynamicDecelKph + rollingResistanceDecelKph + wheelSideDragDecelKph + coupledDrivelineDragDecelKph + engineBrakeDecel + brakeDecel;
            var creepDeltaKph = Math.Max(0f, input.CreepAccelerationMps2) * input.ElapsedSeconds * 3.6f;
            var speedDeltaKph = (-totalDecelKph * input.ElapsedSeconds) + creepDeltaKph;
            var newSpeedMps = Math.Max(0f, input.SpeedMps + (speedDeltaKph / 3.6f));
            if (creepDeltaKph <= 0.0001f
                && totalDecelKph > 0f
                && newSpeedMps <= CoastStopSnapSpeedMps)
            {
                newSpeedMps = 0f;
            }

            speedDeltaKph = (newSpeedMps - input.SpeedMps) * 3.6f;
            return new LongitudinalStepResult(
                speedDeltaKph,
                coupledDriveRpm: 0f,
                driveAccelerationMps2: 0f,
                totalDecelKph,
                brakeDecelKph: brakeDecel,
                engineBrakeDecelKph: engineBrakeDecel,
                aerodynamicDecelKph,
                rollingResistanceDecelKph,
                wheelSideDragDecelKph,
                coupledDrivelineDragDecelKph);
        }

        private static float CoupledRpm(
            Config config,
            int gear,
            float speedMps,
            bool inReverse,
            float? driveRatioOverride)
        {
            var wheelCircumference = config.WheelRadiusM * TwoPi;
            if (wheelCircumference <= 0f)
                return config.IdleRpm;

            var ratio = inReverse
                ? config.ReverseGearRatio
                : (driveRatioOverride.HasValue && driveRatioOverride.Value > 0f
                    ? driveRatioOverride.Value
                    : config.GetGearRatio(gear));
            var coupledRpm = (speedMps / wheelCircumference) * 60f * ratio * config.FinalDriveRatio;
            return Clamp(coupledRpm, config.IdleRpm, config.RevLimiter);
        }

        private static float Clamp(float value, float min, float max)
        {
            if (value < min)
                return min;
            if (value > max)
                return max;
            return value;
        }

        private static float ResolveEffectiveMassKg(Config config, float? effectiveMassKgOverride)
        {
            return Math.Max(1f, effectiveMassKgOverride.GetValueOrDefault(config.MassKg));
        }

        private static float ForceToMps2(float massKg, float forceN)
        {
            return massKg > 0f ? forceN / massKg : 0f;
        }

        private static float ForceToKphPerSecond(float massKg, float forceN)
        {
            return ForceToMps2(massKg, forceN) * 3.6f;
        }
    }
}
