using System;
using TopSpeed.Physics.Powertrain;
using TopSpeed.Vehicles.Control;

namespace TopSpeed.Vehicles
{
    internal partial class Car
    {
        private const float ParkingHoldBrakeInput = 1f;

        internal void RunDynamics(float elapsed, in CarControlIntent controlIntent)
        {
            if (_state == CarState.Running && _started())
                RunRunningDynamics(elapsed, controlIntent);
            else if (_state == CarState.Stopping)
                RunStoppingDynamics(elapsed);
        }

        private void RunRunningDynamics(float elapsed, in CarControlIntent controlIntent)
        {
            GuardDynamicInputs();
            _lastLongitudinalResult = default;
            _lastLateralLoadRatio = 0f;

            _currentSteering = controlIntent.Steering;
            _currentThrottle = _combustionState == EngineCombustionState.On ? controlIntent.Throttle : 0;
            _currentBrake = controlIntent.Brake;
            var clutchInput = controlIntent.Clutch;

            ApplySurfaceModifiers();
            _factor1 = 100;
            HandleTransmissionInput(controlIntent);
            UpdateThrottleLoopAudio(elapsed);

            _thrust = ResolveThrust();
            var speedMpsCurrent = _speed / 3.6f;
            var throttle = Math.Max(0f, Math.Min(100f, _currentThrottle)) / 100f;
            var inReverse = _gear == ReverseGear;
            var currentLapStart = GetLapStartPosition(_positionY);
            var reverseBlockedAtLapStart = inReverse && _positionY <= currentLapStart + 0.001f;
            var surfaceTractionMod = _surfaceTractionFactor > 0f
                ? _currentSurfaceTractionFactor / _surfaceTractionFactor
                : 1.0f;
            var longitudinalGripFactor = 1.0f;
            var drivelineCouplingFactor = UpdateDriveline(elapsed, speedMpsCurrent, throttle, inReverse, clutchInput);
            var canApplyThrottleDrive = CanApplyThrottleDrive(drivelineCouplingFactor);

            if (_engineStalled)
            {
                ApplyStalledDecel(elapsed);
            }
            else if (canApplyThrottleDrive)
            {
                ApplyThrottleDrive(
                    elapsed,
                    speedMpsCurrent,
                    throttle,
                    inReverse,
                    reverseBlockedAtLapStart,
                    surfaceTractionMod,
                    drivelineCouplingFactor,
                    ref longitudinalGripFactor);
            }
            else
            {
                ApplyCoastDecel(elapsed);
            }

            ClampSpeedAndTransmission(elapsed, throttle, inReverse, reverseBlockedAtLapStart, surfaceTractionMod, longitudinalGripFactor);
            SyncEngineFromSpeed(elapsed, out var couplingMode, out var rawCoupledDriveRpm);
            UpdateEngineRotationState(couplingMode, rawCoupledDriveRpm);
            if (_combustionState == EngineCombustionState.On)
            {
                UpdateStallState(elapsed, _speed / 3.6f, throttle, clutchInput);
                UpdateBackfireStateAfterDrive();
            }
            UpdateFuelModel(elapsed);
            UpdateBrakeAndSteeringOutput();
            IntegrateVehiclePosition(elapsed, currentLapStart);
            var postSpeedMps = Math.Max(0f, _speed / 3.6f);
            // Realized longitudinal acceleration after the gear-speed clamp: ~0
            // when pinned at a gear's rev limit even while gross drive torque is
            // high, so the tire model treats rev-limited cruise as cruise.
            var realizedDriveAccelMps2 = elapsed > 0f ? (postSpeedMps - speedMpsCurrent) / elapsed : 0f;
            UpdateTireWear(elapsed, postSpeedMps, realizedDriveAccelMps2);
            UpdateFrameAudioAndFeedback();
            EnsureSurfaceLoopPlaying();

            if (_combustionState == EngineCombustionState.Off
                && _engineRotationState == EngineRotationState.Stopped
                && _speed <= 0.05f)
            {
                CompleteStop();
            }
        }

        private void RunStoppingDynamics(float elapsed)
        {
            _currentThrottle = 0;
            _currentBrake = 0;
            var result = LongitudinalStep.Compute(
                new LongitudinalStepInput(
                    _powertrainConfiguration,
                    elapsed,
                    Math.Max(0f, _speed / 3.6f),
                    throttle: 0f,
                    brake: ParkingHoldBrakeInput,
                    surfaceTractionModifier: 1f,
                    surfaceBrakeModifier: ResolveSurfaceBrakeModifier() * ResolveTireWearBrakeScale(),
                    surfaceRollingResistanceModifier: ResolveSurfaceRollingResistanceModifier(),
                    longitudinalGripFactor: 1f,
                    GetDriveGear(),
                    _gear == ReverseGear,
                    IsNeutralGear(),
                    EffectiveTransmissionType(),
                    _drivelineCouplingFactor,
                    creepAccelerationMps2: 0f,
                    currentEngineRpm: _engine.Rpm,
                    requestDrive: false,
                    requestBrake: true,
                    applyEngineBraking: false,
                    resistanceEnvironment: _track.GetResistanceEnvironment(),
                    driveRatioOverride: _effectiveDriveRatioOverride > 0f ? _effectiveDriveRatioOverride : (float?)null,
                    gearPathEngaged: HasSelectedGearPath(),
                    effectiveMassKg: _massKg));
            _speed = Math.Max(0f, _speed + result.SpeedDeltaKph);
            _speedDiff = result.SpeedDeltaKph;
            _lastDriveRpm = 0f;

            SyncEngineFromSpeed(elapsed, out var couplingMode, out var rawCoupledDriveRpm);
            UpdateEngineRotationState(couplingMode, rawCoupledDriveRpm);

            UpdateEngineFreq();

            if (_combustionState == EngineCombustionState.Off
                && _engineRotationState == EngineRotationState.Stopped
                && _speed <= 0.05f)
            {
                CompleteStop();
                return;
            }

            if (_frame % 4 != 0)
                return;

            _frame = 0;
            UpdateSoundRoad();
            if (_speed <= 0f)
                StopSurfaceLoops();
        }

        private bool CanApplyThrottleDrive(float drivelineCouplingFactor)
        {
            if (_thrust <= 10f)
                return false;

            if (!_manualTransmission)
                return true;

            if (IsNeutralGear())
                return false;

            return drivelineCouplingFactor > 0.05f;
        }
    }
}

