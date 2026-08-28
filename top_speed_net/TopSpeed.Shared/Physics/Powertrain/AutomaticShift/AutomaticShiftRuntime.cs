using System;
using TopSpeed.Vehicles;

namespace TopSpeed.Physics.Powertrain
{
    public static class AutomaticShiftRuntime
    {
        public static AutomaticShiftRuntimeResult Step(in AutomaticShiftRuntimeInput input)
        {
            if (input.Gears <= 1)
                return new AutomaticShiftRuntimeResult(false, input.CurrentGear, 0f);

            if (input.CurrentGear < 1 || input.CurrentGear > input.Gears)
                return new AutomaticShiftRuntimeResult(false, input.CurrentGear, 0f);

            if (input.ShiftOnDemandActive)
                return new AutomaticShiftRuntimeResult(false, input.CurrentGear, 0f);

            if (input.TransmissionType == TransmissionType.Cvt)
            {
                if (input.CurrentGear == 1)
                    return new AutomaticShiftRuntimeResult(false, 1, 0f);

                var direction = input.CurrentGear > 1 ? -1 : 1;
                return new AutomaticShiftRuntimeResult(true, 1, 0f, direction, 0f);
            }

            var cooldown = Math.Max(0f, input.CooldownSeconds);
            if (cooldown > 0f)
            {
                cooldown = Math.Max(0f, cooldown - Math.Max(0f, input.ElapsedSeconds));
                return new AutomaticShiftRuntimeResult(false, input.CurrentGear, cooldown);
            }

            var currentAccel = ComputeNetAccelForGear(in input, input.CurrentGear, input.DriveRatioOverride);
            var currentRpm = Calculator.RpmAtSpeed(
                input.PowertrainConfig,
                input.SpeedMps,
                input.CurrentGear,
                input.DriveRatioOverride);
            var upAccel = input.CurrentGear < input.Gears
                ? ComputeNetAccelForGear(in input, input.CurrentGear + 1, driveRatioOverride: null)
                : float.NegativeInfinity;
            var downAccel = input.CurrentGear > 1
                ? ComputeNetAccelForGear(in input, input.CurrentGear - 1, driveRatioOverride: null)
                : float.NegativeInfinity;

            var decision = AutomaticTransmissionLogic.Decide(
                new AutomaticShiftInput(
                    input.CurrentGear,
                    input.Gears,
                    input.SpeedMps,
                    input.ReferenceTopSpeedMps,
                    input.PowertrainConfig.IdleRpm,
                    input.PowertrainConfig.RevLimiter,
                    currentRpm,
                    currentAccel,
                    upAccel,
                    downAccel),
                input.TransmissionPolicy);

            if (!decision.Changed)
                return new AutomaticShiftRuntimeResult(false, input.CurrentGear, 0f);

            var shiftDirection = decision.NewGear > input.CurrentGear ? 1 : -1;
            var inGearDelaySeconds = shiftDirection > 0
                ? Math.Max(0.2f, decision.CooldownSeconds)
                : 0.2f;
            return new AutomaticShiftRuntimeResult(
                true,
                decision.NewGear,
                Math.Max(0f, decision.CooldownSeconds),
                shiftDirection,
                inGearDelaySeconds);
        }

        /// <summary>
        /// Nominal step used purely to evaluate the longitudinal model; the acceleration it
        /// reports does not depend on it.
        /// </summary>
        private const float ProbeElapsedSeconds = 1f / 120f;

        /// <summary>
        /// Acceleration the car would actually get in this gear.
        /// <para>
        /// This must go through <see cref="LongitudinalStep"/> rather than
        /// <see cref="Calculator.DriveAccel"/>. The latter omits coupled driveline drag, which
        /// grows with gear ratio and engine speed, so near a gear's ceiling it reports over a
        /// m/s2 of acceleration for a car that has in fact stopped accelerating. Comparing gears
        /// on those numbers left the transmission convinced the current gear was still pulling
        /// hard, so it never upshifted and the car stayed pinned at partial speed - Vehicle3
        /// never left second gear. Sharing one model is what keeps the decision honest.
        /// </para>
        /// </summary>
        private static float ComputeNetAccelForGear(
            in AutomaticShiftRuntimeInput input,
            int gear,
            float? driveRatioOverride)
        {
            var config = input.PowertrainConfig;
            var rpm = Calculator.RpmAtSpeed(config, input.SpeedMps, gear, driveRatioOverride);
            if (rpm <= 0f)
                return float.NegativeInfinity;
            if (rpm > config.RevLimiter && gear < input.Gears)
                return float.NegativeInfinity;

            var result = LongitudinalStep.Compute(new LongitudinalStepInput(
                config,
                ProbeElapsedSeconds,
                input.SpeedMps,
                input.Throttle,
                brake: 0f,
                input.SurfaceTractionModifier,
                surfaceBrakeModifier: 1f,
                surfaceRollingResistanceModifier: 1f,
                input.LongitudinalGripFactor,
                gear,
                inReverse: false,
                isNeutral: false,
                input.TransmissionType,
                input.DrivelineCouplingFactor,
                creepAccelerationMps2: 0f,
                currentEngineRpm: rpm,
                requestDrive: true,
                requestBrake: false,
                applyEngineBraking: true,
                resistanceEnvironment: ResistanceEnvironment.Calm,
                driveRatioOverride));

            return result.DriveAccelerationMps2;
        }
    }
}
