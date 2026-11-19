using System;
using TopSpeed.Physics.Powertrain;
using TopSpeed.Physics.Torque;

namespace TopSpeed.Vehicles
{
    /// <summary>
    /// Simulates realistic engine behavior with RPM, throttle response, and engine braking.
    /// </summary>
    internal sealed partial class EngineModel
    {
        private readonly float _idleRpm;
        private readonly float _stallRpm;
        private readonly float _maxRpm;
        private readonly float _revLimiter;
        private readonly float _autoShiftRpm;
        private readonly float _engineBraking;
        private readonly float _engineBrakingTorqueNm;
        private readonly float _powerFactor;
        private readonly float _engineInertiaKgm2;
        private readonly float _engineFrictionTorqueNm;
        private readonly float _engineFrictionLinearNmPerKrpm;
        private readonly float _engineFrictionQuadraticNmPerKrpm2;
        private readonly float _idleControlWindowRpm;
        private readonly float _idleControlGainNmPerRpm;
        private readonly float _minCoupledRiseIdleRpmPerSecond;
        private readonly float _minCoupledRiseFullRpmPerSecond;
        private readonly float _engineOverrunIdleLossFraction;
        private readonly float _overrunCurveExponent;
        private readonly float _drivelineCouplingRate;
        private readonly float _topSpeedKmh;
        private readonly float _finalDriveRatio;
        private readonly float _tireCircumferenceM;
        private readonly int _gearCount;
        private readonly float[] _gearRatios;
        private readonly float[] _gearMaxSpeedMps;
        private readonly float[] _gearAutoShiftSpeedMps;
        private readonly float[] _gearMinSpeedMps;
        private readonly CurveProfile _torqueCurve;

        private float _rpm;
        private float _grossHorsepower;
        private float _netHorsepower;
        private float _distanceMeters;
        private float _speedMps;

        public EngineModel(
            float idleRpm,
            float maxRpm,
            float revLimiter,
            float autoShiftRpm,
            float engineBraking,
            float topSpeedKmh,
            float finalDriveRatio,
            float tireCircumferenceM,
            int gearCount,
            float[]? gearRatios = null,
            float peakTorqueNm = 0f,
            float peakTorqueRpm = 0f,
            float idleTorqueNm = 0f,
            float redlineTorqueNm = 0f,
            float engineBrakingTorqueNm = 0f,
            float powerFactor = 1f,
            float engineInertiaKgm2 = 0.24f,
            float engineFrictionTorqueNm = 20f,
            float drivelineCouplingRate = 12f,
            CurveProfile? torqueCurve = null,
            float engineOverrunIdleLossFraction = 0.35f,
            float engineFrictionLinearNmPerKrpm = 0f,
            float engineFrictionQuadraticNmPerKrpm2 = 0f,
            float idleControlWindowRpm = 150f,
            float idleControlGainNmPerRpm = 0.08f,
            float minCoupledRiseIdleRpmPerSecond = 2200f,
            float minCoupledRiseFullRpmPerSecond = 6200f,
            float overrunCurveExponent = 1f)
        {
            _idleRpm = Math.Max(500f, idleRpm);
            _stallRpm = Math.Max(200f, _idleRpm * 0.55f);
            _maxRpm = Math.Max(_idleRpm + 1000f, maxRpm);
            _revLimiter = Math.Min(_maxRpm, Math.Max(_idleRpm, revLimiter));
            _autoShiftRpm = autoShiftRpm <= 0f
                ? _revLimiter * 0.92f
                : Math.Max(_idleRpm, Math.Min(_revLimiter, autoShiftRpm));
            _engineBraking = Math.Max(0.05f, Math.Min(1.0f, engineBraking));
            _engineBrakingTorqueNm = Math.Max(0f, engineBrakingTorqueNm);
            _powerFactor = Math.Max(0.05f, powerFactor);
            _engineInertiaKgm2 = Math.Max(0.01f, engineInertiaKgm2);
            _engineFrictionTorqueNm = Math.Max(0f, engineFrictionTorqueNm);
            _engineFrictionLinearNmPerKrpm = Math.Max(0f, engineFrictionLinearNmPerKrpm);
            _engineFrictionQuadraticNmPerKrpm2 = Math.Max(0f, engineFrictionQuadraticNmPerKrpm2);
            _idleControlWindowRpm = Math.Max(0f, idleControlWindowRpm);
            _idleControlGainNmPerRpm = Math.Max(0f, idleControlGainNmPerRpm);
            _minCoupledRiseIdleRpmPerSecond = Math.Max(0f, minCoupledRiseIdleRpmPerSecond);
            _minCoupledRiseFullRpmPerSecond = Math.Max(_minCoupledRiseIdleRpmPerSecond, minCoupledRiseFullRpmPerSecond);
            _engineOverrunIdleLossFraction = Math.Max(0f, Math.Min(1f, engineOverrunIdleLossFraction));
            _overrunCurveExponent = Math.Max(0.2f, Math.Min(5f, overrunCurveExponent));
            _drivelineCouplingRate = Math.Max(0.1f, drivelineCouplingRate);
            _topSpeedKmh = Math.Max(50f, topSpeedKmh);
            _finalDriveRatio = Math.Max(0.1f, finalDriveRatio);
            _tireCircumferenceM = Math.Max(0.5f, tireCircumferenceM);
            _gearCount = Math.Max(1, gearCount);
            _rpm = 0f;
            _grossHorsepower = 0f;
            _netHorsepower = 0f;
            _distanceMeters = 0f;
            _speedMps = 0f;

            _gearRatios = gearRatios != null && gearRatios.Length == _gearCount
                ? gearRatios
                : CalculateGearRatios(_gearCount);

            _gearMaxSpeedMps = new float[_gearCount];
            _gearAutoShiftSpeedMps = new float[_gearCount];
            _gearMinSpeedMps = new float[_gearCount];
            var shiftRpm = _idleRpm + ((_revLimiter - _idleRpm) * 0.35f);
            for (var i = 0; i < _gearCount; i++)
            {
                var gearIndex = i + 1;
                _gearMaxSpeedMps[i] = SpeedMpsFromRpm(_revLimiter, gearIndex);
                _gearAutoShiftSpeedMps[i] = SpeedMpsFromRpm(_autoShiftRpm, gearIndex);
                _gearMinSpeedMps[i] = i == 0 ? 0f : SpeedMpsFromRpm(shiftRpm, gearIndex);
            }

            _torqueCurve = torqueCurve ?? CurveFactory.FromLegacy(
                _idleRpm,
                _revLimiter,
                peakTorqueRpm,
                idleTorqueNm,
                peakTorqueNm,
                redlineTorqueNm);
        }

        public float Rpm => _rpm;
        public float IdleRpm => _idleRpm;
        public float StallRpm => _stallRpm;
        public float MaxRpm => _maxRpm;
        public float RevLimiter => _revLimiter;
        public float AutoShiftRpm => _autoShiftRpm;
        public float Horsepower => _grossHorsepower;
        public float GrossHorsepower => _grossHorsepower;
        public float NetHorsepower => _netHorsepower;
        public float SpeedKmh => _speedMps * 3.6f;
        public float SpeedMps => _speedMps;
        public float DistanceMeters => _distanceMeters;

        public void OverrideRpm(float rpm)
        {
            var clamped = Math.Max(_stallRpm, Math.Min(_revLimiter, rpm));
            if (clamped > _rpm)
                _rpm = clamped;
        }

        public float GetGearMaxSpeedKmh(int gear)
        {
            var clampedGear = Math.Max(1, Math.Min(_gearCount, gear));
            return _gearMaxSpeedMps[clampedGear - 1] * 3.6f;
        }

        public float GetGearMinSpeedKmh(int gear)
        {
            var clampedGear = Math.Max(1, Math.Min(_gearCount, gear));
            return _gearMinSpeedMps[clampedGear - 1] * 3.6f;
        }

        public float GetGearRangeKmh(int gear)
        {
            var clampedGear = Math.Max(1, Math.Min(_gearCount, gear));
            var range = _gearMaxSpeedMps[clampedGear - 1] - _gearMinSpeedMps[clampedGear - 1];
            return Math.Max(0.1f, range * 3.6f);
        }

        public float GetGearRatio(int gear)
        {
            var clampedGear = Math.Max(1, Math.Min(_gearCount, gear));
            return _gearRatios[clampedGear - 1];
        }

        public int GetGearForSpeedKmh(float speedKmh)
        {
            var speedMps = Math.Max(0f, speedKmh / 3.6f);
            var topSpeedMps = _topSpeedKmh / 3.6f;
            for (var i = 0; i < _gearCount; i++)
            {
                var gearMax = i == _gearCount - 1 ? _gearMaxSpeedMps[i] : _gearAutoShiftSpeedMps[i];
                gearMax = Math.Min(gearMax, topSpeedMps);
                if (speedMps <= gearMax + 0.01f)
                    return i + 1;
            }

            return _gearCount;
        }
    }
}



