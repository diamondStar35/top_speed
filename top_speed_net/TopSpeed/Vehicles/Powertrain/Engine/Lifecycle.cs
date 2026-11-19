using System;
using TopSpeed.Physics.Powertrain;

namespace TopSpeed.Vehicles
{
    internal sealed partial class EngineModel
    {
        public void Reset()
        {
            _rpm = 0f;
            _speedMps = 0f;
            _distanceMeters = 0f;
        }

        public void ResetForCrash()
        {
            _rpm = 0f;
            _speedMps = 0f;
        }

        public void ResetForEngineStart()
        {
            _rpm = 0f;
            _speedMps = 0f;
        }

        public void StartEngine()
        {
            _rpm = _idleRpm;
        }

        public void StopEngine()
        {
            _rpm = 0f;
            _grossHorsepower = 0f;
            _netHorsepower = 0f;
        }

        public void StepShutdown(float speedGameUnits, float elapsed)
        {
            var dt = Math.Max(0f, elapsed);
            var speedMps = Math.Max(0f, speedGameUnits / 3.6f);
            _speedMps = speedMps;
            _distanceMeters += speedMps * dt;
            _grossHorsepower = 0f;
            _netHorsepower = 0f;

            if (dt <= 0f)
                return;
            if (_rpm <= 0f)
            {
                _rpm = 0f;
                return;
            }

            var clampedRpm = Math.Max(0f, Math.Min(_revLimiter, _rpm));
            var shutdownLossTorque = Calculator.EngineLossTorqueNm(
                clampedRpm,
                _idleRpm,
                _revLimiter,
                _engineFrictionTorqueNm,
                _engineFrictionLinearNmPerKrpm,
                _engineFrictionQuadraticNmPerKrpm2,
                _engineBrakingTorqueNm,
                _engineBraking,
                _engineOverrunIdleLossFraction,
                _overrunCurveExponent,
                closedThrottle: true);
            var rpmDropPerSecond = (shutdownLossTorque / _engineInertiaKgm2) * (60f / (2f * (float)Math.PI));
            if (!IsFinite(rpmDropPerSecond) || rpmDropPerSecond < 0f)
                rpmDropPerSecond = 0f;

            var rpmDrop = rpmDropPerSecond * dt;
            _rpm = Math.Max(0f, _rpm - rpmDrop);
            if (_rpm < 1f)
                _rpm = 0f;
        }

        public void SetSpeed(float speedMps)
        {
            _speedMps = Math.Max(0f, speedMps);
        }

        public void UpdateKinematicsOnly(float speedGameUnits, float elapsed)
        {
            var speedMps = Math.Max(0f, speedGameUnits / 3.6f);
            _speedMps = speedMps;
            _distanceMeters += speedMps * Math.Max(0f, elapsed);
            _grossHorsepower = 0f;
            _netHorsepower = 0f;
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }
    }
}

