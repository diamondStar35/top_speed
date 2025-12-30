using System;

namespace TopSpeed.Vehicles
{
    /// <summary>
    /// Simulates realistic engine behavior with RPM, throttle response, and engine braking.
    /// </summary>
    internal sealed class EngineModel
    {
        private readonly float _idleRpm;
        private readonly float _maxRpm;
        private readonly float _revLimiter;
        private readonly float _engineBraking;
        private readonly float _topSpeedKmh;
        private readonly int _gearCount;
        private readonly float[] _gearRatios;

        private float _rpm;
        private float _distanceMeters;
        private float _speedMps; // meters per second

        /// <summary>
        /// Creates a new engine model with the specified parameters.
        /// </summary>
        /// <param name="idleRpm">Idle RPM (e.g., 800)</param>
        /// <param name="maxRpm">Maximum RPM (e.g., 7000)</param>
        /// <param name="revLimiter">Rev limiter RPM (e.g., 6500)</param>
        /// <param name="engineBraking">Engine braking coefficient (0.1 - 1.0)</param>
        /// <param name="topSpeedKmh">Vehicle top speed in km/h</param>
        /// <param name="gearCount">Number of gears</param>
        public EngineModel(
            float idleRpm,
            float maxRpm,
            float revLimiter,
            float engineBraking,
            float topSpeedKmh,
            int gearCount)
        {
            _idleRpm = Math.Max(500f, idleRpm);
            _maxRpm = Math.Max(_idleRpm + 1000f, maxRpm);
            _revLimiter = Math.Min(_maxRpm, Math.Max(_idleRpm, revLimiter));
            _engineBraking = Math.Max(0.05f, Math.Min(1.0f, engineBraking));
            _topSpeedKmh = Math.Max(50f, topSpeedKmh);
            _gearCount = Math.Max(1, gearCount);
            _rpm = _idleRpm;
            _distanceMeters = 0f;
            _speedMps = 0f;

            // Calculate gear ratios for smooth progression
            _gearRatios = CalculateGearRatios(_gearCount);
        }

        /// <summary>Current engine RPM.</summary>
        public float Rpm => _rpm;

        /// <summary>Current speed in km/h.</summary>
        public float SpeedKmh => _speedMps * 3.6f;

        /// <summary>Current speed in m/s.</summary>
        public float SpeedMps => _speedMps;

        /// <summary>Total distance traveled in meters.</summary>
        public float DistanceMeters => _distanceMeters;

        /// <summary>
        /// Updates the engine state for one frame.
        /// </summary>
        /// <param name="elapsed">Time elapsed in seconds</param>
        /// <param name="throttleInput">Throttle input (0-100)</param>
        /// <param name="brakeInput">Brake input (0 to -100, negative values)</param>
        /// <param name="gear">Current gear (1 to gearCount)</param>
        /// <param name="surfaceAccelMod">Surface acceleration modifier (0.0-1.0)</param>
        /// <param name="surfaceDecelMod">Surface deceleration modifier (0.0-1.0)</param>
        /// <returns>The acceleration/deceleration to apply to the vehicle speed.</returns>
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

            // Calculate target RPM based on current speed and gear
            var speedRatio = _speedMps / (_topSpeedKmh / 3.6f);
            var targetRpmFromSpeed = _idleRpm + (_revLimiter - _idleRpm) * speedRatio / gearRatio;

            // RPM response to throttle
            float targetRpm;
            float rpmChangeRate;

            if (throttle > 0.1f)
            {
                // Throttle applied: RPM rises towards rev limiter based on throttle position
                var throttleTarget = _idleRpm + (_revLimiter - _idleRpm) * throttle;
                targetRpm = Math.Max(targetRpmFromSpeed, throttleTarget);
                rpmChangeRate = 3000f * throttle * surfaceAccelMod; // RPM per second
            }
            else
            {
                // No throttle: RPM decays due to engine braking
                targetRpm = Math.Max(_idleRpm, targetRpmFromSpeed * 0.9f);
                rpmChangeRate = 2000f * _engineBraking * surfaceDecelMod;
            }

            // Smoothly adjust RPM towards target
            if (_rpm < targetRpm)
            {
                _rpm = Math.Min(targetRpm, _rpm + rpmChangeRate * elapsed);
            }
            else
            {
                _rpm = Math.Max(targetRpm, _rpm - rpmChangeRate * elapsed);
            }

            // Clamp RPM to valid range
            _rpm = Math.Max(_idleRpm, Math.Min(_maxRpm, _rpm));

            // Apply rev limiter (cut power at limiter)
            var effectiveRpm = _rpm;
            if (_rpm > _revLimiter)
            {
                effectiveRpm = _revLimiter;
            }

            // Calculate acceleration/deceleration
            float acceleration = 0f;

            if (throttle > 0.1f)
            {
                // Torque curve: peak torque at ~70% of max RPM
                var rpmNormalized = (effectiveRpm - _idleRpm) / (_maxRpm - _idleRpm);
                var torqueCurve = CalculateTorqueCurve(rpmNormalized);

                // Acceleration based on torque and gear ratio
                var baseAccel = _topSpeedKmh / 3.6f * 0.15f; // Base acceleration factor
                acceleration = baseAccel * torqueCurve * throttle * gearRatio * surfaceAccelMod;

                // Reduce acceleration as we approach top speed
                var speedFactor = 1f - (speedRatio * 0.5f);
                acceleration *= Math.Max(0.1f, speedFactor);
            }
            else if (brake > 0.1f)
            {
                // Braking
                var brakePower = _topSpeedKmh / 3.6f * 0.5f; // Strong braking
                acceleration = -brakePower * brake * surfaceDecelMod;
            }
            else
            {
                // Engine braking (gradual deceleration when coasting)
                var engineBrakeForce = _topSpeedKmh / 3.6f * 0.03f * _engineBraking;
                acceleration = -engineBrakeForce * surfaceDecelMod;
            }

            // Update speed
            _speedMps += acceleration * elapsed;
            _speedMps = Math.Max(0f, Math.Min(_topSpeedKmh / 3.6f, _speedMps));

            // Update distance
            _distanceMeters += _speedMps * elapsed;

            return acceleration;
        }

        /// <summary>
        /// Resets the engine to idle state.
        /// </summary>
        public void Reset()
        {
            _rpm = _idleRpm;
            _speedMps = 0f;
            _distanceMeters = 0f;
        }

        /// <summary>
        /// Sets the current speed directly (for external synchronization).
        /// </summary>
        public void SetSpeed(float speedMps)
        {
            _speedMps = Math.Max(0f, speedMps);
        }

        /// <summary>
        /// Syncs RPM and distance from the game's speed calculation.
        /// This is used when the game controls speed and EngineModel tracks for reporting.
        /// </summary>
        /// <param name="speedGameUnits">Speed in game units (same scale as topSpeed)</param>
        /// <param name="gear">Current gear (1-based)</param>
        /// <param name="elapsed">Time elapsed in seconds</param>
        /// <param name="throttleInput">Throttle input (0-100), used for RPM decay</param>
        public void SyncFromSpeed(float speedGameUnits, int gear, float elapsed, int throttleInput = 0)
        {
            // Convert game speed units to a ratio (0 to 1)
            var speedRatio = Math.Min(1f, Math.Max(0f, speedGameUnits / _topSpeedKmh));
            
            // Calculate gear range for RPM computation
            var clampedGear = Math.Max(1, Math.Min(_gearCount, gear));
            var gearRange = 1f / _gearCount;
            var gearSpeedRatio = speedRatio - (clampedGear - 1) * gearRange;
            var gearProgress = gearSpeedRatio / gearRange;
            gearProgress = Math.Max(0f, Math.Min(1f, gearProgress));

            // Calculate target RPM based on position within gear
            var targetRpm = _idleRpm + (_revLimiter - _idleRpm) * gearProgress;
            targetRpm = Math.Max(_idleRpm, Math.Min(_maxRpm, targetRpm));

            // Apply throttle-based RPM behavior
            var throttle = Math.Max(0, throttleInput) / 100f;
            if (throttle > 0.1f)
            {
                // Throttle applied: RPM rises quickly towards target
                var rpmRiseRate = 3000f * throttle;
                if (_rpm < targetRpm)
                    _rpm = Math.Min(targetRpm, _rpm + rpmRiseRate * elapsed);
                else
                    _rpm = Math.Max(targetRpm, _rpm - rpmRiseRate * 0.5f * elapsed);
            }
            else
            {
                // No throttle: RPM decays towards idle (engine braking)
                // Base decay rate is 1000 RPM/sec, scaled by engineBraking parameter
                var decayRate = 1000f * _engineBraking;
                if (_rpm > _idleRpm)
                    _rpm = Math.Max(_idleRpm, _rpm - decayRate * elapsed);
            }

            // Clamp RPM to valid range
            _rpm = Math.Max(_idleRpm, Math.Min(_maxRpm, _rpm));

            // Update distance - convert game speed to m/s (topSpeed is in km/h)
            var speedMps = speedGameUnits / 3.6f;
            _distanceMeters += speedMps * elapsed;
            _speedMps = speedMps;
        }

        private static float[] CalculateGearRatios(int gearCount)
        {
            var ratios = new float[gearCount];
            for (int i = 0; i < gearCount; i++)
            {
                // Higher gears have lower ratios (less torque multiplication)
                // First gear: ~2.5, Last gear: ~0.8
                float progress = (float)i / Math.Max(1, gearCount - 1);
                ratios[i] = 2.5f - (1.7f * progress);
            }
            return ratios;
        }

        private static float CalculateTorqueCurve(float rpmNormalized)
        {
            // Bell curve peaking at ~60% RPM
            // Simulates real engine torque characteristics
            var peak = 0.6f;
            var width = 0.4f;
            var x = (rpmNormalized - peak) / width;
            return (float)Math.Exp(-x * x) * 0.9f + 0.1f;
        }
    }
}
