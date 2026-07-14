using TopSpeed.Input;
using TS.Sdl.Input;

namespace TopSpeed.Game
{
    internal sealed partial class Game
    {
        private void UpdateDriveTouchControls(float deltaSeconds)
        {
            if (!_isAndroidPlatform || !IsRaceState(_state))
            {
                DisableDriveTouchControls();
                return;
            }

            EnsureDriveTouchZones();

            var useMotionSteering = _settings.AndroidUseMotionSteering;
            SyncMotionSteeringMode(useMotionSteering);

            var steering = useMotionSteering ? ReadMotionSteering(deltaSeconds) : 0;
            var throttle = 0;
            var brake = 0;
            var clutch = 0;
            var horn = false;

            if (TryReadVehicleTouch(out var vehicleTouch))
            {
                var deltaX = vehicleTouch.X - vehicleTouch.StartX;
                var deltaY = vehicleTouch.Y - vehicleTouch.StartY;
                ApplyVehicleAxes(useMotionSteering, deltaX, deltaY, ref steering, ref throttle, ref brake);
            }

            var gearUp = _input.WasZoneGesturePressed(GestureIntent.TwoFingerSwipeUp, DriveTouchVehicleZoneId);
            var gearDown = _input.WasZoneGesturePressed(GestureIntent.TwoFingerSwipeDown, DriveTouchVehicleZoneId);
            var startEngine = _input.WasZoneGesturePressed(GestureIntent.DoubleTap, DriveTouchVehicleZoneId);
            var reportSpeed = _input.WasZoneGesturePressed(GestureIntent.DoubleTap, DriveTouchInfoLeftZoneId);
            var reportDistance = _input.WasZoneGesturePressed(GestureIntent.TwoFingerDoubleTap, DriveTouchInfoRightZoneId);
            var currentGear = _input.WasZoneGesturePressed(GestureIntent.ThreeFingerTap, DriveTouchInfoRightZoneId);
            var requestInfo = _input.WasZoneGesturePressed(GestureIntent.ThreeFingerDoubleTap, DriveTouchInfoRightZoneId);
            var previousPlayer = _input.WasZoneGesturePressed(GestureIntent.TwoFingerSwipeDown, DriveTouchInfoRightZoneId);
            var nextPlayer = _input.WasZoneGesturePressed(GestureIntent.TwoFingerSwipeUp, DriveTouchInfoRightZoneId);
            var repeatFocusedPlayer = _input.WasZoneGesturePressed(GestureIntent.TwoFingerTripleTap, DriveTouchInfoRightZoneId);

            var currentLapNr = _input.WasZoneGesturePressed(GestureIntent.DoubleTap, DriveTouchInfoRightZoneId);
            var currentRacePerc = _input.WasZoneGesturePressed(GestureIntent.TwoFingerDoubleTap, DriveTouchInfoLeftZoneId);
            var currentLapPerc = _input.WasZoneGesturePressed(GestureIntent.ThreeFingerDoubleTap, DriveTouchInfoLeftZoneId);
            var currentRaceTime = _input.WasZoneGesturePressed(GestureIntent.TwoFingerTripleTap, DriveTouchInfoLeftZoneId);
            var pause = _input.WasZoneGesturePressed(GestureIntent.ThreeFingerTripleTap, DriveTouchInfoLeftZoneId);
            var pitThisTime = _input.WasZoneGesturePressed(GestureIntent.ThreeFingerTap, DriveTouchVehicleZoneId);
            if (_input.WasZoneGesturePressed(GestureIntent.SwipeDown, DriveTouchInfoLeftZoneId))
                _driveTouchExitRequested = true;

            ApplyTopZoneInputs(ref clutch, ref horn);

            _driveInput.SetTouchInputState(
                steering,
                throttle,
                brake,
                clutch,
                horn,
                gearUp,
                gearDown,
                startEngine,
                reportDistance,
                reportSpeed,
                currentGear,
                currentLapNr,
                currentRacePerc,
                currentLapPerc,
                currentRaceTime,
                pause,
                requestInfo,
                previousPlayer,
                nextPlayer,
                repeatFocusedPlayer,
                pitThisTime);
        }

        private void EnsureDriveTouchZones()
        {
            if (_driveTouchZonesApplied)
                return;

            _input.SetTouchZones(new[]
            {
                new TouchZone(
                    DriveTouchInfoLeftZoneId,
                    new TouchZoneRect(0f, 0f, DriveTouchInfoSplitX, DriveTouchSplitY),
                    priority: 20,
                    behavior: TouchZoneBehavior.Lock),
                new TouchZone(
                    DriveTouchInfoRightZoneId,
                    new TouchZoneRect(DriveTouchInfoSplitX, 0f, 1f - DriveTouchInfoSplitX, DriveTouchSplitY),
                    priority: 30,
                    behavior: TouchZoneBehavior.Lock),
                new TouchZone(
                    DriveTouchVehicleZoneId,
                    new TouchZoneRect(0f, DriveTouchSplitY, 1f, 1f - DriveTouchSplitY),
                    priority: 20,
                    behavior: TouchZoneBehavior.Lock)
            });
            _driveTouchZonesApplied = true;
        }

        private void DisableDriveTouchControls()
        {
            if (_driveTouchZonesApplied)
            {
                _input.ClearTouchZones();
                _driveTouchZonesApplied = false;
            }

            _driveGyroSteering = 0f;
            _driveGyroBias = 0f;
            _driveGyroRate = 0f;
            _driveGyroGravityX = 0f;
            _driveGyroGravityY = 0f;
            _driveGyroGravityZ = 0f;
            _driveGyroNeutralAngle = 0f;
            _driveGyroOrientationReady = false;
            _driveDisplayOrientation = TS.Sdl.Video.DisplayOrientation.Unknown;
            _driveMotionSteering = 0f;
            _driveMotionNeedsRecenter = true;
            _driveMotionEnabled = false;
            _driveTouchExitRequested = false;
            _driveInput.ClearTouchInputState();
        }

        private void SyncMotionSteeringMode(bool useMotionSteering)
        {
            if (useMotionSteering && !_driveMotionEnabled)
            {
                _driveMotionEnabled = true;
                _driveMotionNeedsRecenter = true;
                _driveMotionSteering = 0f;
            }
            else if (!useMotionSteering && _driveMotionEnabled)
            {
                _driveMotionEnabled = false;
                _driveMotionNeedsRecenter = true;
                _driveMotionSteering = 0f;
            }
        }

        private bool TryReadVehicleTouch(out TouchZoneState touch)
        {
            return _input.TryGetTouchZoneState(DriveTouchVehicleZoneId, out touch) &&
                touch.IsActive &&
                touch.FingerCount == 1;
        }

        private void ApplyVehicleAxes(
            bool useMotionSteering,
            float deltaX,
            float deltaY,
            ref int steering,
            ref int throttle,
            ref int brake)
        {
            if (deltaX > 0f)
            {
                throttle = ScalePercent(deltaX, DriveTouchAxisTravel);
                brake = 0;
            }
            else if (deltaX < 0f)
            {
                throttle = 0;
                brake = -ScalePercent(-deltaX, DriveTouchAxisTravel);
            }

            if (useMotionSteering)
                return;

            if (deltaY < 0f)
                steering = -ScalePercent(-deltaY, DriveTouchAxisTravel);
            else if (deltaY > 0f)
                steering = ScalePercent(deltaY, DriveTouchAxisTravel);
        }

        private void ApplyTopZoneInputs(ref int clutch, ref bool horn)
        {
            if (IsSinglePointerActive(DriveTouchInfoLeftZoneId))
                clutch = 100;
            if (IsSinglePointerActive(DriveTouchInfoRightZoneId))
                horn = true;
        }

        private bool IsSinglePointerActive(string zoneId)
        {
            return _input.TryGetTouchZoneState(zoneId, out var state) &&
                state.IsActive &&
                state.FingerCount == 1;
        }

        private bool ConsumeDriveTouchExitRequest()
        {
            if (!_driveTouchExitRequested)
                return false;

            _driveTouchExitRequested = false;
            return true;
        }
    }
}
