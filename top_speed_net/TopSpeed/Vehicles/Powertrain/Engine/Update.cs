using System;

namespace TopSpeed.Vehicles
{
    internal sealed partial class EngineModel
    {
        public float Update(
            float elapsed,
            int throttleInput,
            int brakeInput,
            int gear,
            float surfaceAccelMod = 1.0f,
            float surfaceDecelMod = 1.0f)
        {
            var clampedGear = Math.Max(1, Math.Min(_gearCount, gear));
            var gearRatio = _gearRatios[clampedGear - 1];
            var throttle = Math.Max(0f, Math.Min(100f, throttleInput)) / 100f;
            var brake = Math.Max(0f, Math.Min(100f, -brakeInput)) / 100f;
            var speedRatio = _speedMps / (_topSpeedKmh / 3.6f);

            float targetRpmFromSpeed;
            if (clampedGear == 1)
            {
                var gearMax = _gearMaxSpeedMps[clampedGear - 1];
                var positionInGear = gearMax <= 0f ? 0f : Math.Min(1f, Math.Max(0f, _speedMps / gearMax));
                targetRpmFromSpeed = _idleRpm + ((_revLimiter - _idleRpm) * positionInGear);
            }
            else
            {
                var gearMin = _gearMinSpeedMps[clampedGear - 1];
                var gearRange = Math.Max(0.1f, _gearMaxSpeedMps[clampedGear - 1] - gearMin);
                var positionInGear = Math.Min(1f, Math.Max(0f, (_speedMps - gearMin) / gearRange));
                var shiftRpm = _idleRpm + ((_revLimiter - _idleRpm) * 0.35f);
                targetRpmFromSpeed = shiftRpm + ((_revLimiter - shiftRpm) * positionInGear);
            }

            float targetRpm;
            float rpmChangeRate;
            if (throttle > 0.1f)
            {
                var throttleTarget = _idleRpm + ((_revLimiter - _idleRpm) * throttle);
                targetRpm = Math.Max(targetRpmFromSpeed, throttleTarget);
                rpmChangeRate = 3000f * throttle * surfaceAccelMod;
            }
            else
            {
                targetRpm = Math.Max(_stallRpm, targetRpmFromSpeed * 0.9f);
                rpmChangeRate = 2000f * _engineBraking * surfaceDecelMod;
            }

            if (_rpm < targetRpm)
                _rpm = Math.Min(targetRpm, _rpm + (rpmChangeRate * elapsed));
            else
                _rpm = Math.Max(targetRpm, _rpm - (rpmChangeRate * elapsed));

            _rpm = Math.Max(_stallRpm, Math.Min(_maxRpm, _rpm));
            var effectiveRpm = _rpm > _revLimiter ? _revLimiter : _rpm;

            float acceleration;
            if (throttle > 0.1f)
            {
                var rpmNormalized = (effectiveRpm - _idleRpm) / (_maxRpm - _idleRpm);
                var torqueCurve = EvaluateTorqueCurve(rpmNormalized);
                var baseAccel = _topSpeedKmh / 3.6f * 0.15f;
                acceleration = baseAccel * torqueCurve * throttle * gearRatio * surfaceAccelMod;
                var speedFactor = 1f - (speedRatio * 0.5f);
                acceleration *= Math.Max(0.1f, speedFactor);
            }
            else if (brake > 0.1f)
            {
                var brakePower = _topSpeedKmh / 3.6f * 0.5f;
                acceleration = -brakePower * brake * surfaceDecelMod;
            }
            else
            {
                var engineBrakeForce = _topSpeedKmh / 3.6f * 0.03f * _engineBraking;
                acceleration = -engineBrakeForce * surfaceDecelMod;
            }

            _speedMps += acceleration * elapsed;
            var maxSpeedInGear = Math.Min(_topSpeedKmh / 3.6f, _gearMaxSpeedMps[clampedGear - 1]);
            _speedMps = Math.Max(0f, Math.Min(maxSpeedInGear, _speedMps));
            _distanceMeters += _speedMps * elapsed;

            return acceleration;
        }
    }
}

