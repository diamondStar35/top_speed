using System;

namespace TopSpeed.Vehicles
{
    internal struct VehicleDynamicsParameters
    {
        public float MassKg;
        public float WheelbaseM;
        public float TrackWidthM;
        public float CgHeightM;
        public float CgToFrontM;
        public float CgToRearM;
        public float FrontWeightBias;
        public float FrontBrakeBias;
        public float DriveBiasFront;
        public float YawInertiaKgM2;
        public float CorneringStiffnessFront;
        public float CorneringStiffnessRear;
        public float DragCoefficient;
        public float FrontalAreaM2;
        public float RollingResistanceCoefficient;
        public float RollStiffnessFrontFraction;
        public float TireLoadSensitivity;
        public float DownforceCoefficient;
        public float DownforceFrontBias;
        public float LongitudinalStiffnessFront;
        public float LongitudinalStiffnessRear;
        public float SteerTurnRate;
        public float SteerReturnRate;
        public float SteerGamma;
        public float SteerLowDeg;
        public float SteerHighDeg;
        public float SteerSpeedKph;
        public float SteerSpeedExponent;
        public float MaxSpeedKph;
    }

    internal struct VehicleDynamicsState
    {
        public float VelLong;
        public float VelLat;
        public float Yaw;
        public float YawRate;
        public float SteerInput;
        public float SteerWheelAngleRad;
        public float SteerWheelAngleDeg;
    }

    internal struct VehicleDynamicsInputs
    {
        public float Elapsed;
        public int SteeringCommand;
        public float DriveForce;
        public float BrakeForce;
        public float EngineBrakeForce;
        public float SurfaceTractionMod;
        public float TireGripCoefficient;
        public float LateralGripCoefficient;
    }

    internal struct VehicleDynamicsResult
    {
        public float SpeedKph;
        public float SpeedDiffKph;
        public float LongitudinalGripFactor;
        public float LateralUsage;
    }

    internal static class VehicleDynamics
    {
        private const float AirDensity = 1.225f;

        public static VehicleDynamicsResult Step(ref VehicleDynamicsState state, in VehicleDynamicsParameters p, in VehicleDynamicsInputs input)
        {
            var result = new VehicleDynamicsResult();
            var dt = Math.Max(0f, input.Elapsed);
            if (dt <= 0f)
                return result;

            if (!IsFinite(state.VelLong)) state.VelLong = 0f;
            if (!IsFinite(state.VelLat)) state.VelLat = 0f;
            if (!IsFinite(state.Yaw)) state.Yaw = 0f;
            if (!IsFinite(state.YawRate)) state.YawRate = 0f;
            if (!IsFinite(state.SteerInput)) state.SteerInput = 0f;

            var prevSpeed = (float)Math.Sqrt(state.VelLong * state.VelLong + state.VelLat * state.VelLat) * 3.6f;

            VehicleSteering.UpdateSteeringInput(
                ref state,
                p.SteerTurnRate,
                p.SteerReturnRate,
                p.SteerGamma,
                p.SteerLowDeg,
                p.SteerHighDeg,
                p.SteerSpeedKph,
                p.SteerSpeedExponent,
                input.SteeringCommand,
                prevSpeed,
                dt);

            var driveForce = Math.Max(0f, input.DriveForce);
            var brakeForce = Math.Max(0f, input.BrakeForce);
            var engineBrakeForce = Math.Max(0f, input.EngineBrakeForce);
            var speedMpsInitial = prevSpeed / 3.6f;
            if (speedMpsInitial < 0.05f && driveForce <= 1f && (brakeForce + engineBrakeForce) > 0f)
            {
                state.VelLong = 0f;
                state.VelLat = 0f;
                state.YawRate = 0f;
                result.SpeedKph = 0f;
                result.SpeedDiffKph = -prevSpeed;
                result.LateralUsage = 0f;
                result.LongitudinalGripFactor = 1f;
                return result;
            }

            var speedForward = Math.Abs(state.VelLong);
            var dragForce = 0.5f * AirDensity * p.DragCoefficient * p.FrontalAreaM2 * speedForward * speedForward;
            var rollingForce = p.RollingResistanceCoefficient * p.MassKg * 9.80665f;
            var resistSign = speedForward > 0.25f ? Math.Sign(state.VelLong) : 0;
            var resistForce = (dragForce + rollingForce) * resistSign;

            var speedSign = Math.Abs(state.VelLong) > 0.1f ? Math.Sign(state.VelLong) : 0f;
            var fxDrive = driveForce;
            var fxBrake = -(brakeForce + engineBrakeForce) * speedSign;
            var fxTotal = fxDrive + fxBrake - resistForce;

            var weight = p.MassKg * 9.80665f;
            var speedMps = (float)Math.Sqrt(state.VelLong * state.VelLong + state.VelLat * state.VelLat);
            var downforce = 0.5f * AirDensity * p.DownforceCoefficient * p.FrontalAreaM2 * speedMps * speedMps;
            var downforceFrontBias = p.DownforceFrontBias > 0f
                ? VehicleMath.Clamp(p.DownforceFrontBias, 0f, 1f)
                : p.FrontWeightBias;
            var downforceFront = downforce * downforceFrontBias;
            var downforceRear = downforce - downforceFront;
            var axApprox = fxTotal / Math.Max(1f, p.MassKg);
            var loadTransferLong = p.MassKg * axApprox * p.CgHeightM / Math.Max(0.01f, p.WheelbaseM);
            var frontLoadTotal = (weight * p.FrontWeightBias) - loadTransferLong + downforceFront;
            var rearLoadTotal = (weight - (weight * p.FrontWeightBias)) + loadTransferLong + downforceRear;
            if (frontLoadTotal < weight * 0.1f) frontLoadTotal = weight * 0.1f;
            if (rearLoadTotal < weight * 0.1f) rearLoadTotal = weight * 0.1f;

            var ayApprox = state.VelLong * state.YawRate;
            var latTransfer = p.MassKg * ayApprox * p.CgHeightM / Math.Max(0.01f, p.TrackWidthM);
            var rollFront = p.RollStiffnessFrontFraction > 0f
                ? VehicleMath.Clamp(p.RollStiffnessFrontFraction, 0.2f, 0.8f)
                : p.FrontWeightBias;
            var frontLat = latTransfer * rollFront * 0.5f;
            var rearLat = latTransfer * (1f - rollFront) * 0.5f;

            var frontLeftLoad = frontLoadTotal * 0.5f + frontLat;
            var frontRightLoad = frontLoadTotal * 0.5f - frontLat;
            var rearLeftLoad = rearLoadTotal * 0.5f + rearLat;
            var rearRightLoad = rearLoadTotal * 0.5f - rearLat;
            var minWheelLoad = Math.Max(1f, weight * 0.02f);
            if (frontLeftLoad < minWheelLoad) frontLeftLoad = minWheelLoad;
            if (frontRightLoad < minWheelLoad) frontRightLoad = minWheelLoad;
            if (rearLeftLoad < minWheelLoad) rearLeftLoad = minWheelLoad;
            if (rearRightLoad < minWheelLoad) rearRightLoad = minWheelLoad;

            var muBase = Math.Max(0.05f, input.TireGripCoefficient * input.SurfaceTractionMod * input.LateralGripCoefficient);
            var nominalLoad = Math.Max(1f, (weight + downforce) * 0.25f);
            var muFrontLeft = AdjustMuForLoad(muBase, frontLeftLoad, nominalLoad, p.TireLoadSensitivity);
            var muFrontRight = AdjustMuForLoad(muBase, frontRightLoad, nominalLoad, p.TireLoadSensitivity);
            var muRearLeft = AdjustMuForLoad(muBase, rearLeftLoad, nominalLoad, p.TireLoadSensitivity);
            var muRearRight = AdjustMuForLoad(muBase, rearRightLoad, nominalLoad, p.TireLoadSensitivity);

            var driveFront = fxDrive * p.DriveBiasFront;
            var driveRear = fxDrive - driveFront;
            var brakeFront = brakeForce * p.FrontBrakeBias;
            var brakeRear = brakeForce - brakeFront;

            var fxFrontLeft = (driveFront * 0.5f) - (brakeFront * 0.5f);
            var fxFrontRight = (driveFront * 0.5f) - (brakeFront * 0.5f);
            var fxRearLeft = (driveRear * 0.5f) - (brakeRear * 0.5f);
            var fxRearRight = (driveRear * 0.5f) - (brakeRear * 0.5f);

            fxFrontLeft = ApplyLongitudinalStiffness(fxFrontLeft, frontLeftLoad, muFrontLeft, p.LongitudinalStiffnessFront);
            fxFrontRight = ApplyLongitudinalStiffness(fxFrontRight, frontRightLoad, muFrontRight, p.LongitudinalStiffnessFront);
            fxRearLeft = ApplyLongitudinalStiffness(fxRearLeft, rearLeftLoad, muRearLeft, p.LongitudinalStiffnessRear);
            fxRearRight = ApplyLongitudinalStiffness(fxRearRight, rearRightLoad, muRearRight, p.LongitudinalStiffnessRear);

            var steerLeft = state.SteerWheelAngleRad;
            var steerRight = state.SteerWheelAngleRad;
            if (Math.Abs(state.SteerWheelAngleRad) > 0.0001f)
            {
                var turnRadius = p.WheelbaseM / (float)Math.Tan(state.SteerWheelAngleRad);
                var radiusAbs = Math.Abs(turnRadius);
                var inner = (float)Math.Atan(p.WheelbaseM / Math.Max(0.01f, radiusAbs - p.TrackWidthM * 0.5f));
                var outer = (float)Math.Atan(p.WheelbaseM / (radiusAbs + p.TrackWidthM * 0.5f));
                if (state.SteerWheelAngleRad > 0f)
                {
                    steerRight = inner;
                    steerLeft = outer;
                }
                else
                {
                    steerRight = -outer;
                    steerLeft = -inner;
                }
            }

            var halfTrack = p.TrackWidthM * 0.5f;
            var frontX = p.CgToFrontM;
            var rearX = -p.CgToRearM;

            var fyFrontLeft = TireLateralForce(state, frontX, -halfTrack, steerLeft, p.CorneringStiffnessFront * 0.5f);
            var fyFrontRight = TireLateralForce(state, frontX, halfTrack, steerRight, p.CorneringStiffnessFront * 0.5f);
            var fyRearLeft = TireLateralForce(state, rearX, -halfTrack, 0f, p.CorneringStiffnessRear * 0.5f);
            var fyRearRight = TireLateralForce(state, rearX, halfTrack, 0f, p.CorneringStiffnessRear * 0.5f);

            fyFrontLeft = ClampFrictionEllipse(fyFrontLeft, fxFrontLeft, muFrontLeft * frontLeftLoad);
            fyFrontRight = ClampFrictionEllipse(fyFrontRight, fxFrontRight, muFrontRight * frontRightLoad);
            fyRearLeft = ClampFrictionEllipse(fyRearLeft, fxRearLeft, muRearLeft * rearLeftLoad);
            fyRearRight = ClampFrictionEllipse(fyRearRight, fxRearRight, muRearRight * rearRightLoad);

            var fxSum = fxFrontLeft + fxFrontRight + fxRearLeft + fxRearRight - resistForce;
            var fySum = fyFrontLeft + fyFrontRight + fyRearLeft + fyRearRight;

            var mz =
                (frontX * fyFrontLeft - (-halfTrack) * fxFrontLeft) +
                (frontX * fyFrontRight - (halfTrack) * fxFrontRight) +
                (rearX * fyRearLeft - (-halfTrack) * fxRearLeft) +
                (rearX * fyRearRight - (halfTrack) * fxRearRight);

            var dvx = (fxSum / Math.Max(1f, p.MassKg)) + (state.YawRate * state.VelLat);
            var dvy = (fySum / Math.Max(1f, p.MassKg)) - (state.YawRate * state.VelLong);
            var dr = mz / Math.Max(1f, p.YawInertiaKgM2);

            state.VelLong += dvx * dt;
            if (Math.Abs(state.VelLong) < 0.01f && Math.Abs(fxDrive) < 1f)
                state.VelLong = 0f;

            if (Math.Abs(state.VelLong) < 0.5f)
            {
                var kinematicYaw = state.VelLong * (float)Math.Tan(state.SteerWheelAngleRad) / Math.Max(0.01f, p.WheelbaseM);
                state.YawRate = VehicleMath.Approach(state.YawRate, kinematicYaw, 4.0f * dt);
                state.VelLat = VehicleMath.Approach(state.VelLat, 0f, 2.5f * dt);
            }
            else
            {
                state.VelLat += dvy * dt;
                state.YawRate += dr * dt;
            }

            state.Yaw += state.YawRate * dt;

            speedMps = (float)Math.Sqrt(state.VelLong * state.VelLong + state.VelLat * state.VelLat);
            if (p.MaxSpeedKph > 0f)
            {
                var maxSpeed = p.MaxSpeedKph / 3.6f;
                if (speedMps > maxSpeed && speedMps > 0.01f)
                {
                    var scale = maxSpeed / speedMps;
                    state.VelLong *= scale;
                    state.VelLat *= scale;
                    speedMps = maxSpeed;
                }
            }

            result.SpeedKph = speedMps * 3.6f;
            result.SpeedDiffKph = result.SpeedKph - prevSpeed;
            result.LateralUsage = Math.Abs(fySum) / Math.Max(1f, muBase * weight);
            if (result.LateralUsage > 1f)
                result.LateralUsage = 1f;
            result.LongitudinalGripFactor = (float)Math.Sqrt(Math.Max(0f, 1f - (result.LateralUsage * result.LateralUsage)));

            return result;
        }

        private static float TireLateralForce(VehicleDynamicsState state, float x, float y, float steerAngle, float stiffness)
        {
            var vx = state.VelLong - state.YawRate * y;
            var vy = state.VelLat + state.YawRate * x;
            var vxSafe = Math.Abs(vx) < 0.5f ? (0.5f * Math.Sign(vx == 0f ? 1f : vx)) : vx;
            var slip = (float)Math.Atan2(vy, vxSafe) - steerAngle;
            return -stiffness * slip;
        }

        private static float ClampFrictionEllipse(float fy, float fx, float maxForce)
        {
            var maxFy = (float)Math.Sqrt(Math.Max(0f, (maxForce * maxForce) - (fx * fx)));
            return VehicleMath.Clamp(fy, -maxFy, maxFy);
        }

        private static float ApplyLongitudinalStiffness(float fxCmd, float load, float mu, float stiffness)
        {
            var maxForce = mu * load;
            if (stiffness <= 0f)
                return VehicleMath.Clamp(fxCmd, -maxForce, maxForce);

            var denom = Math.Max(1f, stiffness * load);
            var kappa = fxCmd / denom;
            var fx = (float)(stiffness * load * Math.Tanh(kappa));
            return VehicleMath.Clamp(fx, -maxForce, maxForce);
        }

        private static float AdjustMuForLoad(float mu, float load, float nominal, float sensitivity)
        {
            if (sensitivity <= 0f || nominal <= 0f)
                return mu;
            var loadFactor = 1f - (sensitivity * ((load - nominal) / nominal));
            loadFactor = VehicleMath.Clamp(loadFactor, 0.4f, 1.4f);
            return mu * loadFactor;
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }
    }
}
