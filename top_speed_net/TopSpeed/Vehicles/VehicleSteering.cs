using System;

namespace TopSpeed.Vehicles
{
    internal static class VehicleSteering
    {
        private const float ReturnScaleAtZeroSpeed = 0.05f;
        private const float ReturnScaleAtTargetSpeed = 1.8f;

        public static void UpdateSteeringInput(
            ref VehicleDynamicsState state,
            float steerTurnRate,
            float steerReturnRate,
            float steerGamma,
            float steerLowDeg,
            float steerHighDeg,
            float steerSpeedKph,
            float steerSpeedExponent,
            int steeringCommand,
            float speedKph,
            float dt)
        {
            var desired = VehicleMath.Clamp(steeringCommand / 100.0f, -1f, 1f);
            if (speedKph < 5f)
                speedKph = 0f;
            var speedT = steerSpeedKph > 0f ? speedKph / steerSpeedKph : 1f;
            speedT = VehicleMath.Clamp(speedT, 0f, 1f);
            speedT = VehicleMath.SmoothStep(speedT);
            if (steerSpeedExponent > 0f)
                speedT = (float)Math.Pow(speedT, steerSpeedExponent);
            var returnScale = VehicleMath.Lerp(ReturnScaleAtZeroSpeed, ReturnScaleAtTargetSpeed, speedT);
            var rate = Math.Abs(desired) > Math.Abs(state.SteerInput)
                ? steerTurnRate
                : steerReturnRate * returnScale;
            state.SteerInput = VehicleMath.Approach(state.SteerInput, desired, rate * dt);

            var shaped = Math.Abs(state.SteerInput) <= 0f
                ? 0f
                : Math.Sign(state.SteerInput) * (float)Math.Pow(Math.Abs(state.SteerInput), steerGamma);
            var steerLimit = CalculateSteerLimit(steerLowDeg, steerHighDeg, steerSpeedKph, steerSpeedExponent, speedKph);
            state.SteerWheelAngleDeg = shaped * steerLimit;
            state.SteerWheelAngleRad = state.SteerWheelAngleDeg * ((float)Math.PI / 180.0f);
        }

        private static float CalculateSteerLimit(
            float steerLowDeg,
            float steerHighDeg,
            float steerSpeedKph,
            float steerSpeedExponent,
            float speedKph)
        {
            if (steerSpeedKph <= 0f)
                return steerLowDeg;
            var t = speedKph / steerSpeedKph;
            t = VehicleMath.Clamp(t, 0f, 1f);
            t = VehicleMath.SmoothStep(t);
            if (steerSpeedExponent > 0f)
                t = (float)Math.Pow(t, steerSpeedExponent);
            return VehicleMath.Lerp(steerLowDeg, steerHighDeg, t);
        }
    }
}
