using System;
using Key = TopSpeed.Input.InputKey;
using TopSpeed.Input.Devices.Controller;

namespace TopSpeed.Input
{
    internal sealed partial class DriveInput
    {
        public void Initialize()
        {
            _left = AxisOrButton.AxisNone;
            _right = AxisOrButton.AxisNone;
            _throttle = AxisOrButton.AxisNone;
            _brake = AxisOrButton.AxisNone;
            _clutch = AxisOrButton.AxisNone;
            _gearUp = AxisOrButton.AxisNone;
            _gearDown = AxisOrButton.AxisNone;
            _horn = AxisOrButton.AxisNone;
            _requestInfo = AxisOrButton.AxisNone;
            _currentGear = AxisOrButton.AxisNone;
            _currentLapNr = AxisOrButton.AxisNone;
            _currentRacePerc = AxisOrButton.AxisNone;
            _currentLapPerc = AxisOrButton.AxisNone;
            _currentRaceTime = AxisOrButton.AxisNone;
            _startEngine = AxisOrButton.AxisNone;
            _reportDistance = AxisOrButton.AxisNone;
            _reportSpeed = AxisOrButton.AxisNone;
            _reportFuel = AxisOrButton.AxisNone;
            _reportTireState = AxisOrButton.AxisNone;
            _trackName = AxisOrButton.AxisNone;
            _pause = AxisOrButton.AxisNone;
            ReadFromSettings();
            _allowDrivingInput = true;
            _allowAuxiliaryInput = true;
            _overlayInputBlocked = false;
            _pausedHornInputAllowed = false;
            _controllerIsRacingWheel = false;
            ClearTouchInputState();
            ResetPedalCalibration();

            _kbPlayerNumber = Key.F11;
            _kbPlayerPos1 = Key.D1;
            _kbPlayerPos2 = Key.D2;
            _kbPlayerPos3 = Key.D3;
            _kbPlayerPos4 = Key.D4;
            _kbPlayerPos5 = Key.D5;
            _kbPlayerPos6 = Key.D6;
            _kbPlayerPos7 = Key.D7;
            _kbPlayerPos8 = Key.D8;
            _kbPlayerPos9 = Key.D9;
            _kbPlayerPos0 = Key.D0;
            _kbFlush = Key.LeftAlt;
        }

        private void ReadFromSettings()
        {
            _left = _settings.GetControllerBinding(DriveIntent.SteerLeft);
            _right = _settings.GetControllerBinding(DriveIntent.SteerRight);
            _throttle = _settings.GetControllerBinding(DriveIntent.Throttle);
            _brake = _settings.GetControllerBinding(DriveIntent.Brake);
            _clutch = _settings.GetControllerBinding(DriveIntent.Clutch);
            _gearUp = _settings.GetControllerBinding(DriveIntent.GearUp);
            _gearDown = _settings.GetControllerBinding(DriveIntent.GearDown);
            _horn = _settings.GetControllerBinding(DriveIntent.Horn);
            _requestInfo = _settings.GetControllerBinding(DriveIntent.RequestInfo);
            _currentGear = _settings.GetControllerBinding(DriveIntent.CurrentGear);
            _currentLapNr = _settings.GetControllerBinding(DriveIntent.CurrentLapNr);
            _currentRacePerc = _settings.GetControllerBinding(DriveIntent.CurrentRacePerc);
            _currentLapPerc = _settings.GetControllerBinding(DriveIntent.CurrentLapPerc);
            _currentRaceTime = _settings.GetControllerBinding(DriveIntent.CurrentRaceTime);
            _startEngine = _settings.GetControllerBinding(DriveIntent.StartEngine);
            _reportDistance = _settings.GetControllerBinding(DriveIntent.ReportDistance);
            _reportSpeed = _settings.GetControllerBinding(DriveIntent.ReportSpeed);
            _reportFuel = _settings.GetControllerBinding(DriveIntent.ReportFuel);
            _reportTireState = _settings.GetControllerBinding(DriveIntent.ReportTireState);
            _trackName = _settings.GetControllerBinding(DriveIntent.TrackName);
            _pause = _settings.GetControllerBinding(DriveIntent.Pause);
            _center = _settings.ControllerCenter;
            _hasCenter = true;
            _kbLeft = _settings.GetKeyboardBinding(DriveIntent.SteerLeft);
            _kbRight = _settings.GetKeyboardBinding(DriveIntent.SteerRight);
            _kbThrottle = _settings.GetKeyboardBinding(DriveIntent.Throttle);
            _kbBrake = _settings.GetKeyboardBinding(DriveIntent.Brake);
            _kbClutch = _settings.GetKeyboardBinding(DriveIntent.Clutch);
            _kbGearUp = _settings.GetKeyboardBinding(DriveIntent.GearUp);
            _kbGearDown = _settings.GetKeyboardBinding(DriveIntent.GearDown);
            _kbHorn = _settings.GetKeyboardBinding(DriveIntent.Horn);
            _kbRequestInfo = _settings.GetKeyboardBinding(DriveIntent.RequestInfo);
            _kbCurrentGear = _settings.GetKeyboardBinding(DriveIntent.CurrentGear);
            _kbCurrentLapNr = _settings.GetKeyboardBinding(DriveIntent.CurrentLapNr);
            _kbCurrentRacePerc = _settings.GetKeyboardBinding(DriveIntent.CurrentRacePerc);
            _kbCurrentLapPerc = _settings.GetKeyboardBinding(DriveIntent.CurrentLapPerc);
            _kbCurrentRaceTime = _settings.GetKeyboardBinding(DriveIntent.CurrentRaceTime);
            _kbStartEngine = _settings.GetKeyboardBinding(DriveIntent.StartEngine);
            _kbReportDistance = _settings.GetKeyboardBinding(DriveIntent.ReportDistance);
            _kbReportSpeed = _settings.GetKeyboardBinding(DriveIntent.ReportSpeed);
            _kbReportFuel = _settings.GetKeyboardBinding(DriveIntent.ReportFuel);
            _kbReportTireState = _settings.GetKeyboardBinding(DriveIntent.ReportTireState);
            _kbTrackName = _settings.GetKeyboardBinding(DriveIntent.TrackName);
            _kbPause = _settings.GetKeyboardBinding(DriveIntent.Pause);
            _deviceMode = _settings.DeviceMode;
        }
    }
}



