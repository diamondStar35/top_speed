using System;
using TopSpeed.Physics.Powertrain;
using TopSpeed.Physics.Surface;
using TopSpeed.Physics.Tires;
using TopSpeed.Physics.Tires.Wear;
using TopSpeed.Vehicles;

namespace TopSpeed.Bots
{
    public static partial class BotPhysics
    {
        public static void Step(BotPhysicsConfig config, ref BotPhysicsState state, in BotPhysicsInput input)
        {
            if (config == null)
                throw new ArgumentNullException(nameof(config));
            if (input.ElapsedSeconds <= 0f)
                return;

            if (state.Gear < 1 || state.Gear > config.Gears)
                state.Gear = 1;
            if (state.AutomaticCouplingFactor <= 0f)
                state.AutomaticCouplingFactor = 1f;
            if (state.CvtRatio <= 0f)
                state.CvtRatio = config.AutomaticTuning.Cvt.RatioMax;

            var ambientTemperatureC = ResolveAmbientTemperatureC(input.AmbientTemperatureC, config.TireWearConfig.AmbientTemperatureC);
            state.SurfaceTemperatureC = StepSurfaceTemperature(
                state.SurfaceTemperatureC,
                input.ElapsedSeconds,
                input.Surface,
                ambientTemperatureC,
                input.RainGain,
                input.StormGain,
                input.WindGain);
            var weatherWetness = ResolveWeatherWetness(input.RainGain, input.StormGain);

            var surface = SurfaceModel.Resolve(input.Surface, config.SurfaceTractionFactor);
            var surfaceTraction = surface.Traction;
            var surfaceBrake = surface.Brake;
            var surfaceRollingResistance = surface.RollingResistance;

            var thrust = LongitudinalStep.ResolveThrust(input.Throttle, input.Brake);

            var speedKph = Math.Max(0f, state.SpeedKph);
            var speedMpsCurrent = speedKph / 3.6f;
            var throttle = Math.Max(0f, Math.Min(100f, input.Throttle)) / 100f;
            var brake = Math.Max(0f, Math.Min(100f, -input.Brake)) / 100f;
            var steeringInput = input.Steering;
            var surfaceTractionMod = surfaceTraction / config.SurfaceTractionFactor;
            var wearState = new TireWearState(state.TireWearFraction, state.TireTemperatureC, state.TireSmoothedSlipNormalized);
            var wearRuntime = TireWearRuntime.Resolve(config.TireWearConfig, wearState);
            var longitudinalGripFactor = 1.0f;
            var speedDiffKph = 0f;
            var tireState = new TireModelState(state.LateralVelocityMps, state.YawRateRad);
            var activeTransmissionType = config.ActiveTransmissionType;
            var automaticFamily = TransmissionTypes.IsAutomaticFamily(activeTransmissionType);
            var autoOutput = default(AutomaticDrivelineOutput);
            var driveRatioOverride = 0f;
            if (automaticFamily)
            {
                var currentEngineRpmEstimate = Calculator.RpmAtSpeed(config.Powertrain, speedMpsCurrent, state.Gear);
                autoOutput = AutomaticDrivelineModel.Step(
                    activeTransmissionType,
                    config.AutomaticTuning,
                    new AutomaticDrivelineInput(
                        input.ElapsedSeconds,
                        speedMpsCurrent,
                        throttle,
                        brake,
                        shifting: state.AutoShiftCooldownSeconds > 0f,
                        wheelCircumferenceM: config.WheelRadiusM * 2f * (float)Math.PI,
                        finalDriveRatio: config.FinalDriveRatio,
                        idleRpm: config.IdleRpm,
                        revLimiter: config.RevLimiter,
                        launchRpm: config.LaunchRpm,
                        currentEngineRpm: currentEngineRpmEstimate),
                    new AutomaticDrivelineState(state.AutomaticCouplingFactor, state.CvtRatio));
                state.AutomaticCouplingFactor = autoOutput.CouplingFactor;
                state.CvtRatio = autoOutput.CvtRatio;
                driveRatioOverride = autoOutput.EffectiveDriveRatio;
                if (activeTransmissionType == TransmissionType.Cvt)
                    state.Gear = 1;
            }
            else
            {
                state.AutomaticCouplingFactor = 1f;
                state.EffectiveDriveRatio = 0f;
            }

            var driveRequested = thrust > 10f;
            if (driveRequested)
            {
                var tireOutput = SolveTireModel(config, input.ElapsedSeconds, speedMpsCurrent, steeringInput, surfaceTractionMod, 1f, tireState);
                longitudinalGripFactor = tireOutput.LongitudinalGripFactor * wearRuntime.TractionGripScale;
            }

            var surfaceBrakeMod = (surfaceBrake > 0f ? surfaceBrake : 1f) * wearRuntime.BrakeGripScale;
            var couplingFactor = automaticFamily ? state.AutomaticCouplingFactor : 1f;
            var engineRpmEstimate = Calculator.RpmAtSpeed(
                config.Powertrain,
                speedMpsCurrent,
                state.Gear,
                driveRatioOverride > 0f ? driveRatioOverride : (float?)null);
            var longitudinalResult = LongitudinalStep.Compute(
                new LongitudinalStepInput(
                    config.Powertrain,
                    input.ElapsedSeconds,
                    speedMpsCurrent,
                    throttle,
                    brake,
                    surfaceTractionMod,
                    surfaceBrakeMod,
                    surfaceRollingResistance,
                    longitudinalGripFactor,
                    state.Gear,
                    inReverse: false,
                    isNeutral: false,
                    transmissionType: activeTransmissionType,
                    couplingFactor,
                    automaticFamily ? autoOutput.CreepAccelerationMps2 : 0f,
                    engineRpmEstimate,
                    requestDrive: driveRequested,
                    requestBrake: thrust < -10f,
                    applyEngineBraking: true,
                    resistanceEnvironment: ResistanceEnvironment.Calm,
                    driveRatioOverride: driveRatioOverride > 0f ? driveRatioOverride : (float?)null));
            speedDiffKph = longitudinalResult.SpeedDeltaKph;

            speedKph += speedDiffKph;
            var safetySpeed = ResolveForwardSafetySpeedKph(config.TopSpeedKph);
            if (speedKph > safetySpeed)
                speedKph = safetySpeed;
            if (speedKph < 0f)
                speedKph = 0f;

            if (activeTransmissionType != TransmissionType.Cvt)
            {
                UpdateAutomaticGear(
                    config,
                    ref state,
                    input.ElapsedSeconds,
                    speedKph / 3.6f,
                    throttle,
                    surfaceTractionMod,
                    longitudinalGripFactor,
                    driveRatioOverride > 0f ? driveRatioOverride : (float?)null);
            }
            else
            {
                state.AutoShiftCooldownSeconds = 0f;
            }

            if (thrust < -50f && speedKph > 0f)
                steeringInput = steeringInput * 2 / 3;

            var speedMps = speedKph / 3.6f;
            state.PositionY += speedMps * input.ElapsedSeconds;
            state.SpeedKph = speedKph;
            state.EffectiveDriveRatio = driveRatioOverride;

            var surfaceTractionModLat = surfaceTraction / config.SurfaceTractionFactor;
            var lateralOutput = SolveTireModel(config, input.ElapsedSeconds, speedMps, steeringInput, surfaceTractionModLat, surface.LateralSpeedMultiplier * wearRuntime.LateralGripScale, tireState);
            state.PositionX += lateralOutput.LateralSpeedMps * input.ElapsedSeconds;
            state.LateralVelocityMps = lateralOutput.State.LateralVelocityMps;
            state.YawRateRad = lateralOutput.State.YawRateRad;

            var longitudinalSlipNormalized = TireWearInputSignals.ResolveLongitudinalSlipNormalized(
                longitudinalResult.DriveAccelerationMps2,
                longitudinalResult.BrakeDecelKph / 3.6f);
            var loadNormalized = TireWearInputSignals.ResolveLoadNormalized(
                config.MassKg,
                lateralOutput.LateralLoadRatio,
                longitudinalSlipNormalized);
            var rollingResistanceNormalized = TireWearInputSignals.ResolveRollingResistanceNormalized(
                config.RollingResistanceCoefficient,
                surfaceRollingResistance,
                speedMps);
            wearRuntime = TireWearRuntime.Step(
                config.TireWearConfig,
                wearState,
                new TireWearInput(
                    input.ElapsedSeconds,
                    speedMps,
                    slipAngleNormalized: lateralOutput.SlipAngleNormalized,
                    lateralSlipNormalized: lateralOutput.LateralSlipNormalized,
                    longitudinalSlipNormalized: longitudinalSlipNormalized,
                    loadNormalized: loadNormalized,
                    rollingResistanceNormalized: rollingResistanceNormalized,
                    ambientTemperatureC: ambientTemperatureC,
                    surfaceTemperatureC: state.SurfaceTemperatureC,
                    wetnessNormalized: weatherWetness));
            state.TireWearFraction = wearRuntime.State.WearFraction;
            state.TireTemperatureC = wearRuntime.State.TemperatureC;
            state.TireSmoothedSlipNormalized = wearRuntime.State.SmoothedSlipNormalized;
        }
    }
}
