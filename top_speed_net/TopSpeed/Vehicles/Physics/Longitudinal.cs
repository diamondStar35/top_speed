using System;
using TopSpeed.Physics.Powertrain;

namespace TopSpeed.Vehicles
{
    internal partial class Car
    {
        private int ResolveThrust()
        {
            return LongitudinalStep.ResolveThrust(_currentThrottle, _currentBrake);
        }

        private void ApplyThrottleDrive(
            float elapsed,
            float speedMpsCurrent,
            float throttle,
            bool inReverse,
            bool reverseBlockedAtLapStart,
            float surfaceTractionMod,
            float drivelineCouplingFactor,
            ref float longitudinalGripFactor)
        {
            if (reverseBlockedAtLapStart)
            {
                _speedDiff = 0f;
                _lastDriveRpm = 0f;
                return;
            }

            if (ShouldForceOverspeedCoast(inReverse, drivelineCouplingFactor))
            {
                ApplyOverspeedCoastDecel(elapsed, speedMpsCurrent, inReverse, drivelineCouplingFactor);
                return;
            }

            var tireOutput = SolveTireModel(elapsed, speedMpsCurrent, _currentSteering, surfaceTractionMod, 1f, commitState: false);
            longitudinalGripFactor = tireOutput.LongitudinalGripFactor;
            var result = LongitudinalStep.Compute(
                new LongitudinalStepInput(
                    _powertrainConfiguration,
                    elapsed,
                    speedMpsCurrent,
                    throttle,
                    brake: 0f,
                    surfaceTractionModifier: surfaceTractionMod,
                    surfaceBrakeModifier: ResolveSurfaceBrakeModifier(),
                    surfaceRollingResistanceModifier: ResolveSurfaceRollingResistanceModifier(),
                    longitudinalGripFactor,
                    GetDriveGear(),
                    inReverse,
                    isNeutral: false,
                    transmissionType: EffectiveTransmissionType(),
                    drivelineCouplingFactor,
                    creepAccelerationMps2: 0f,
                    currentEngineRpm: _engine.Rpm,
                    requestDrive: true,
                    requestBrake: false,
                    applyEngineBraking: false,
                    resistanceEnvironment: _track.GetResistanceEnvironment(),
                    driveRatioOverride: _effectiveDriveRatioOverride > 0f ? _effectiveDriveRatioOverride : (float?)null,
                    driveAccelerationScale: (_factor1 / 100f) * _fuelPowerScale,
                    gearPathEngaged: HasSelectedGearPath(),
                    effectiveMassKg: _massKg));
            _speedDiff = result.SpeedDeltaKph;
            _lastDriveRpm = result.CoupledDriveRpm;
            if (_backfirePlayed)
                _backfirePlayed = false;
        }

        private bool ShouldForceOverspeedCoast(bool inReverse, float drivelineCouplingFactor)
        {
            if (inReverse)
                return false;
            if (_gear < FirstForwardGear)
                return false;

            var gearMax = _engine.GetGearMaxSpeedKmh(_gear);
            return GearSpeedLimiter.ShouldForceOverspeedCoast(
                _speed,
                gearMax,
                drivelineCouplingFactor,
                forwardDriveGearActive: true,
                manualShiftControlActive: _manualTransmission || IsShiftOnDemandActive());
        }

        private void ApplyOverspeedCoastDecel(float elapsed, float speedMpsCurrent, bool inReverse, float drivelineCouplingFactor)
        {
            var result = LongitudinalStep.Compute(
                new LongitudinalStepInput(
                    _powertrainConfiguration,
                    elapsed,
                    Math.Max(0f, speedMpsCurrent),
                    throttle: 0f,
                    brake: 0f,
                    surfaceTractionModifier: 1f,
                    surfaceBrakeModifier: ResolveSurfaceBrakeModifier(),
                    surfaceRollingResistanceModifier: ResolveSurfaceRollingResistanceModifier(),
                    longitudinalGripFactor: 1f,
                    GetDriveGear(),
                    inReverse,
                    isNeutral: false,
                    transmissionType: EffectiveTransmissionType(),
                    drivelineCouplingFactor,
                    creepAccelerationMps2: 0f,
                    currentEngineRpm: _engine.Rpm,
                    requestDrive: false,
                    requestBrake: false,
                    applyEngineBraking: true,
                    resistanceEnvironment: _track.GetResistanceEnvironment(),
                    driveRatioOverride: _effectiveDriveRatioOverride > 0f ? _effectiveDriveRatioOverride : (float?)null,
                    gearPathEngaged: HasSelectedGearPath(),
                    effectiveMassKg: _massKg));
            _speedDiff = result.SpeedDeltaKph;
            _lastDriveRpm = 0f;
        }

        private void ApplyCoastDecel(float elapsed)
        {
            var brakeInput = Math.Max(0f, Math.Min(100f, -_currentBrake)) / 100f;
            var result = LongitudinalStep.Compute(
                new LongitudinalStepInput(
                    _powertrainConfiguration,
                    elapsed,
                    Math.Max(0f, _speed / 3.6f),
                    throttle: 0f,
                    brake: brakeInput,
                    surfaceTractionModifier: 1f,
                    surfaceBrakeModifier: ResolveSurfaceBrakeModifier(),
                    surfaceRollingResistanceModifier: ResolveSurfaceRollingResistanceModifier(),
                    longitudinalGripFactor: 1f,
                    GetDriveGear(),
                    _gear == ReverseGear,
                    IsNeutralGear(),
                    EffectiveTransmissionType(),
                    _drivelineCouplingFactor,
                    _automaticCreepAccelMps2,
                    _engine.Rpm,
                    requestDrive: false,
                    requestBrake: _thrust < -10,
                    applyEngineBraking: !IsNeutralGear(),
                    resistanceEnvironment: _track.GetResistanceEnvironment(),
                    driveRatioOverride: _effectiveDriveRatioOverride > 0f ? _effectiveDriveRatioOverride : (float?)null,
                    gearPathEngaged: HasSelectedGearPath(),
                    effectiveMassKg: _massKg));
            _speedDiff = result.SpeedDeltaKph;
            _lastDriveRpm = 0f;
        }

        private bool HasSelectedGearPath()
        {
            if (IsNeutralGear())
                return false;

            if (!_manualTransmission)
                return true;

            return _switchingGear == 0;
        }

        private float ResolveSurfaceBrakeModifier()
        {
            return _currentSurfaceBrakeFactor > 0f ? _currentSurfaceBrakeFactor : 1.0f;
        }

        private float ResolveSurfaceRollingResistanceModifier()
        {
            return _currentSurfaceRollingResistanceFactor > 0f ? _currentSurfaceRollingResistanceFactor : 1.0f;
        }

        private void ClampSpeedAndTransmission(
            float elapsed,
            float throttle,
            bool inReverse,
            bool reverseBlockedAtLapStart,
            float surfaceTractionMod,
            float longitudinalGripFactor)
        {
            var speedBeforeIntegration = _speed;
            _speed += _speedDiff;
            if (!inReverse)
            {
                var safetySpeed = ResolveForwardSafetySpeedKph();
                if (_speed > safetySpeed)
                    _speed = safetySpeed;
            }
            if (_speed < 0f)
                _speed = 0f;
            if (!IsFinite(_speed))
            {
                _speed = 0f;
                _speedDiff = 0f;
            }

            if (!IsFinite(_lastDriveRpm))
                _lastDriveRpm = _idleRpm;

            if (reverseBlockedAtLapStart && _thrust > 10f)
            {
                _speed = 0f;
                _speedDiff = 0f;
                _lastDriveRpm = 0f;
            }

            if (inReverse)
            {
                var reverseMax = Math.Max(5.0f, _reverseMaxSpeedKph);
                if (_speed > reverseMax)
                    _speed = reverseMax;
                return;
            }

            if (_manualTransmission)
            {
                if (_gear >= FirstForwardGear)
                {
                    var gearMax = _engine.GetGearMaxSpeedKmh(_gear);
                    _speed = GearSpeedLimiter.ApplyForwardGearLimit(speedBeforeIntegration, _speed, gearMax);
                }
            }
            else
            {
                if (IsShiftOnDemandActive() && _gear >= FirstForwardGear)
                {
                    var gearMax = _engine.GetGearMaxSpeedKmh(_gear);
                    _speed = GearSpeedLimiter.ApplyForwardGearLimit(speedBeforeIntegration, _speed, gearMax);
                }
                else
                {
                    UpdateAutomaticGear(elapsed, _speed / 3.6f, throttle, surfaceTractionMod, longitudinalGripFactor);
                }
            }
        }
    }
}

