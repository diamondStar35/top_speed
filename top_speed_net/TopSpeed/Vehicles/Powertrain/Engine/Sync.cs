using System;
using TopSpeed.Physics.Powertrain;

namespace TopSpeed.Vehicles
{
    internal sealed partial class EngineModel
    {
        public void SyncFromSpeed(
            float speedGameUnits,
            int gear,
            float elapsed,
            int throttleInput = 0,
            bool inReverse = false,
            float reverseGearRatio = 3.2f,
            EngineCouplingMode couplingMode = EngineCouplingMode.Blended,
            float couplingFactor = 1f,
            float? driveRatioOverride = null,
            float minimumCoupledRpm = 0f,
            bool combustionEnabled = true)
        {
            var clampedGear = Math.Max(1, Math.Min(_gearCount, gear));
            var throttle = Math.Max(0, throttleInput) / 100f;
            var speedMps = speedGameUnits / 3.6f;
            var wheelCircumference = _tireCircumferenceM;
            var lockToDriveline = couplingMode == EngineCouplingMode.Locked;
            var disengaged = couplingMode == EngineCouplingMode.Disengaged;
            var minimumOperationalRpm = combustionEnabled ? _stallRpm : 0f;
            var gearRatio = inReverse
                ? Math.Max(0.1f, reverseGearRatio)
                : (driveRatioOverride.HasValue && driveRatioOverride.Value > 0f
                    ? driveRatioOverride.Value
                    : _gearRatios[clampedGear - 1]);
            var coupledRpm = wheelCircumference > 0f
                ? (speedMps / wheelCircumference) * 60f * gearRatio * _finalDriveRatio
                : _idleRpm;

            var clampedMinimumCoupledRpm = Math.Max(0f, Math.Min(_revLimiter, minimumCoupledRpm));
            var effectiveMinimumCoupledRpm = clampedMinimumCoupledRpm;
            if (!lockToDriveline && effectiveMinimumCoupledRpm > 0f)
            {
                var riseRate = _minCoupledRiseIdleRpmPerSecond + ((_minCoupledRiseFullRpmPerSecond - _minCoupledRiseIdleRpmPerSecond) * throttle);
                var rampLimit = _rpm + (riseRate * Math.Max(0f, elapsed));
                if (effectiveMinimumCoupledRpm > rampLimit)
                    effectiveMinimumCoupledRpm = rampLimit;

                if (coupledRpm < effectiveMinimumCoupledRpm)
                    coupledRpm = effectiveMinimumCoupledRpm;
            }

            coupledRpm = Math.Max(minimumOperationalRpm, Math.Min(_revLimiter, coupledRpm));
            var clampedCouplingFactor = Math.Max(0f, Math.Min(1f, couplingFactor));
            var canBackDriveThroughCoupling = !disengaged && clampedCouplingFactor > 0.001f;
            var baseRpm = lockToDriveline
                ? coupledRpm
                : (_rpm > 0f
                    ? _rpm
                    : (canBackDriveThroughCoupling ? coupledRpm : 0f));
            var clampedBaseRpm = Math.Max(minimumOperationalRpm, Math.Min(_revLimiter, baseRpm));
            var torqueAvailable = _torqueCurve.EvaluateTorque(clampedBaseRpm);
            var maximumEngineTorque = torqueAvailable * _powerFactor;
            var requestedEngineTorque = combustionEnabled ? maximumEngineTorque * throttle : 0f;
            var grossEngineTorque = requestedEngineTorque;

            var parasiticFrictionTorque = Calculator.EngineLossTorqueNm(
                clampedBaseRpm,
                _idleRpm,
                _revLimiter,
                _engineFrictionTorqueNm,
                _engineFrictionLinearNmPerKrpm,
                _engineFrictionQuadraticNmPerKrpm2,
                _engineBrakingTorqueNm,
                _engineBraking,
                _engineOverrunIdleLossFraction,
                _overrunCurveExponent,
                closedThrottle: false);
            var idleControlActive = combustionEnabled
                && throttle <= 0.10f
                && clampedBaseRpm <= _idleRpm + _idleControlWindowRpm;
            if (idleControlActive)
            {
                var idleRpmDeficit = Math.Max(0f, _idleRpm - clampedBaseRpm);
                var idleTargetTorque = parasiticFrictionTorque + (idleRpmDeficit * _idleControlGainNmPerRpm);
                var idleCompensationTorque = Math.Min(maximumEngineTorque, idleTargetTorque);
                if (grossEngineTorque < idleCompensationTorque)
                    grossEngineTorque = idleCompensationTorque;
            }

            var freeRevRpmThreshold = _idleRpm + Math.Max(80f, _idleControlWindowRpm * 0.35f);
            var freeRevOverrunActive = disengaged && clampedBaseRpm > freeRevRpmThreshold;
            var drivelineOverrunActive = !disengaged && clampedCouplingFactor > 0.05f;
            var applyClosedThrottleOverrun = combustionEnabled
                ? throttle <= 0.1f && (drivelineOverrunActive || freeRevOverrunActive)
                : clampedBaseRpm > minimumOperationalRpm + 1f && (drivelineOverrunActive || freeRevOverrunActive || disengaged);
            var lossTorque = Calculator.EngineLossTorqueNm(
                clampedBaseRpm,
                _idleRpm,
                _revLimiter,
                _engineFrictionTorqueNm,
                _engineFrictionLinearNmPerKrpm,
                _engineFrictionQuadraticNmPerKrpm2,
                _engineBrakingTorqueNm,
                _engineBraking,
                _engineOverrunIdleLossFraction,
                _overrunCurveExponent,
                closedThrottle: applyClosedThrottleOverrun);

            var netEngineTorque = grossEngineTorque - lossTorque;
            var rpmPerSecond = (netEngineTorque / _engineInertiaKgm2) * (60f / (2f * (float)Math.PI));
            var torqueIntegratedRpm = clampedBaseRpm + (rpmPerSecond * elapsed);
            torqueIntegratedRpm = Math.Max(minimumOperationalRpm, Math.Min(_maxRpm, torqueIntegratedRpm));

            if (lockToDriveline)
            {
                _rpm = coupledRpm;
            }
            else if (disengaged || clampedCouplingFactor <= 0.001f)
            {
                _rpm = torqueIntegratedRpm;
            }
            else
            {
                var couplingAlpha = Math.Max(0f, Math.Min(1f, _drivelineCouplingRate * elapsed * clampedCouplingFactor));
                var blendedRpm = torqueIntegratedRpm + ((coupledRpm - torqueIntegratedRpm) * couplingAlpha);
                _rpm = Math.Max(minimumOperationalRpm, Math.Min(_maxRpm, blendedRpm));
            }

            if (combustionEnabled
                && !inReverse
                && !lockToDriveline
                && effectiveMinimumCoupledRpm > 0f
                && _rpm < effectiveMinimumCoupledRpm)
                _rpm = effectiveMinimumCoupledRpm;

            if (_rpm > _revLimiter)
                _rpm = _revLimiter;
            if (_rpm < 1f)
                _rpm = 0f;

            _grossHorsepower = Calculator.Horsepower(Math.Max(0f, grossEngineTorque), _rpm);
            _netHorsepower = Calculator.Horsepower(Math.Max(0f, netEngineTorque), _rpm);

            _distanceMeters += speedMps * elapsed;
            _speedMps = speedMps;
        }
    }
}



