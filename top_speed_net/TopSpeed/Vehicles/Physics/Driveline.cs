using System;
using TopSpeed.Input.Devices.Vibration;
using TopSpeed.Physics.Powertrain;

namespace TopSpeed.Vehicles
{
    internal partial class Car
    {
        private float UpdateDriveline(float elapsed, float speedMps, float throttle, bool inReverse, int clutchInput)
        {
            var type = EffectiveTransmissionType();
            if (TransmissionTypes.IsAutomaticFamily(type))
            {
                if (IsNeutralGear())
                {
                    _drivelineCouplingFactor = 0f;
                    _drivelineState = DrivelineState.Disengaged;
                    _effectiveDriveRatioOverride = 0f;
                    _automaticCreepAccelMps2 = 0f;
                    return _drivelineCouplingFactor;
                }

                UpdateAutomaticDriveline(type, elapsed, speedMps, throttle, inReverse);
                return _drivelineCouplingFactor;
            }

            if (_engineStalled)
            {
                _drivelineCouplingFactor = 0f;
                _drivelineState = DrivelineState.Disengaged;
                _effectiveDriveRatioOverride = 0f;
                _automaticCreepAccelMps2 = 0f;
                return _drivelineCouplingFactor;
            }

            if (IsNeutralGear())
            {
                _drivelineCouplingFactor = 0f;
                _drivelineState = DrivelineState.Disengaged;
                _effectiveDriveRatioOverride = 0f;
                _automaticCreepAccelMps2 = 0f;
                return _drivelineCouplingFactor;
            }

            _effectiveDriveRatioOverride = 0f;
            _automaticCreepAccelMps2 = 0f;
            var clutch = Math.Max(0f, Math.Min(100f, clutchInput)) / 100f;
            _drivelineCouplingFactor = _switchingGear != 0 ? 0f : 1f - clutch;
            if (_drivelineCouplingFactor <= 0.05f)
                _drivelineState = DrivelineState.Disengaged;
            else if (_drivelineCouplingFactor >= 0.98f)
                _drivelineState = DrivelineState.Locked;
            else
                _drivelineState = DrivelineState.Slipping;

            return _drivelineCouplingFactor;
        }

        private void UpdateAutomaticDriveline(TransmissionType type, float elapsed, float speedMps, float throttle, bool inReverse)
        {
            if (_engineStalled)
            {
                _drivelineCouplingFactor = 0f;
                _drivelineState = DrivelineState.Disengaged;
                _automaticCreepAccelMps2 = 0f;
                _effectiveDriveRatioOverride = 0f;
                return;
            }

            var brake = Math.Max(0f, Math.Min(100f, -_currentBrake)) / 100f;
            var output = AutomaticDrivelineModel.Step(
                type,
                _automaticTuning,
                new AutomaticDrivelineInput(
                    elapsed,
                    speedMps,
                    throttle,
                    brake,
                    shifting: _switchingGear != 0,
                    wheelCircumferenceM: _wheelRadiusM * 2f * (float)Math.PI,
                    finalDriveRatio: _finalDriveRatio,
                    idleRpm: _idleRpm,
                    revLimiter: _revLimiter,
                    launchRpm: _launchRpm,
                    currentEngineRpm: _engine.Rpm),
                new AutomaticDrivelineState(_drivelineCouplingFactor, _cvtRatio));

            _drivelineCouplingFactor = output.CouplingFactor;
            _cvtRatio = output.CvtRatio > 0f ? output.CvtRatio : _cvtRatio;
            _effectiveDriveRatioOverride = inReverse ? 0f : output.EffectiveDriveRatio;
            _automaticCreepAccelMps2 = inReverse ? 0f : output.CreepAccelerationMps2;

            if (_drivelineCouplingFactor <= 0.05f)
                _drivelineState = DrivelineState.Disengaged;
            else if (_drivelineCouplingFactor >= 0.98f)
                _drivelineState = DrivelineState.Locked;
            else
                _drivelineState = DrivelineState.Slipping;
        }

        private TransmissionType EffectiveTransmissionType()
        {
            return _activeTransmissionType;
        }

        private void UpdateStallState(float elapsed, float speedMps, float throttle, int clutchInput)
        {
            var stallResult = EngineStateRuntime.EvaluateManualStall(
                new ManualStallRuntimeInput(
                    EffectiveTransmissionType(),
                    _switchingGear,
                    IsNeutralGear(),
                    _engineStalled,
                    elapsed,
                    _engine.Rpm,
                    _engine.StallRpm,
                    _speed,
                    throttle,
                    Math.Max(0f, Math.Min(100f, clutchInput)) / 100f,
                    _drivelineCouplingFactor,
                    ComputeRawCoupledRpm(speedMps, inReverse: _gear == ReverseGear),
                    _gear > FirstForwardGear,
                    _gear == ReverseGear,
                    _stallTimer));
            _stallTimer = stallResult.StallTimerSeconds;
            if (stallResult.ShouldStall)
                StallEngine();
        }

        private float ComputeRawCoupledRpm(float speedMps, bool inReverse)
        {
            var wheelCircumference = _wheelRadiusM * 2.0f * (float)Math.PI;
            if (wheelCircumference <= 0.001f)
                return 0f;

            var gearRatio = inReverse ? _reverseGearRatio : _engine.GetGearRatio(GetDriveGear());
            return (speedMps / wheelCircumference) * 60f * gearRatio * _finalDriveRatio;
        }

        private void StallEngine(bool playFailureCue = true)
        {
            _engineStalled = true;
            _stallTimer = 0f;
            _drivelineCouplingFactor = 0f;
            _drivelineState = DrivelineState.Disengaged;
            _effectiveDriveRatioOverride = 0f;
            _automaticCreepAccelMps2 = 0f;
            _combustionState = EngineCombustionState.Off;
            if (_soundThrottle != null && _soundThrottle.IsPlaying)
                _soundThrottle.Stop();

            _vibration?.StopEffect(VibrationEffectType.Engine);
            if (playFailureCue)
                _soundBadSwitch.Play(loop: false);
        }

        private void ClearStallState()
        {
            _engineStalled = false;
            _stallTimer = 0f;
            _drivelineCouplingFactor = 1f;
            _drivelineState = DrivelineState.Locked;
            _cvtRatio = _automaticTuning.Cvt.RatioMax;
            _effectiveDriveRatioOverride = 0f;
            _automaticCreepAccelMps2 = 0f;
        }

        private void ApplyStalledDecel(float elapsed)
        {
            var brakeInput = Math.Max(0f, Math.Min(100f, -_currentBrake)) / 100f;
            var brakeDecel = CalculateBrakeDecel(brakeInput, ResolveSurfaceBrakeModifier());
            var resistance = ResistanceModel.Compute(
                _powertrainConfiguration,
                Math.Max(0f, _speed / 3.6f),
                ResolveSurfaceRollingResistanceModifier(),
                applyDrivelineDrag: false,
                drivelineDragParticipation: 0f,
                gear: GetDriveGear(),
                inReverse: _gear == ReverseGear,
                isNeutral: true,
                _track.GetResistanceEnvironment(),
                massKgOverride: _massKg);
            var passiveDecel = ((resistance.AerodynamicForceN + resistance.RollingResistanceForceN + resistance.WheelSideDragForceN) / Math.Max(1f, _massKg)) * 3.6f;
            _speedDiff = -(brakeDecel + passiveDecel) * elapsed;
            _lastDriveRpm = 0f;
        }
    }
}

