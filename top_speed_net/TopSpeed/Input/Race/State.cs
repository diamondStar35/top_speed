using SharpDX.DirectInput;
using TopSpeed.Protocol;
using System;

namespace TopSpeed.Input
{
    internal sealed partial class RaceInput
    {
        public void Initialize()
        {
            _left = JoystickAxisOrButton.AxisNone;
            _right = JoystickAxisOrButton.AxisNone;
            _throttle = JoystickAxisOrButton.AxisNone;
            _brake = JoystickAxisOrButton.AxisNone;
            _gearUp = JoystickAxisOrButton.AxisNone;
            _gearDown = JoystickAxisOrButton.AxisNone;
            _horn = JoystickAxisOrButton.AxisNone;
            _requestInfo = JoystickAxisOrButton.AxisNone;
            _currentGear = JoystickAxisOrButton.AxisNone;
            _currentLapNr = JoystickAxisOrButton.AxisNone;
            _currentRacePerc = JoystickAxisOrButton.AxisNone;
            _currentLapPerc = JoystickAxisOrButton.AxisNone;
            _currentRaceTime = JoystickAxisOrButton.AxisNone;
            _startEngine = JoystickAxisOrButton.AxisNone;
            _reportDistance = JoystickAxisOrButton.AxisNone;
            _reportSpeed = JoystickAxisOrButton.AxisNone;
            _trackName = JoystickAxisOrButton.AxisNone;
            _pause = JoystickAxisOrButton.AxisNone;
            ReadFromSettings();
            _allowDrivingInput = true;
            _allowAuxiliaryInput = true;
            _overlayInputBlocked = false;

            _kbPlayer1 = Key.F1;
            _kbPlayer2 = Key.F2;
            _kbPlayer3 = Key.F3;
            _kbPlayer4 = Key.F4;
            _kbPlayer5 = Key.F5;
            _kbPlayer6 = Key.F6;
            _kbPlayer7 = Key.F7;
            _kbPlayer8 = Key.F8;
            _kbPlayerNumber = Key.F11;
            _kbPlayerPos1 = Key.D1;
            _kbPlayerPos2 = Key.D2;
            _kbPlayerPos3 = Key.D3;
            _kbPlayerPos4 = Key.D4;
            _kbPlayerPos5 = Key.D5;
            _kbPlayerPos6 = Key.D6;
            _kbPlayerPos7 = Key.D7;
            _kbPlayerPos8 = Key.D8;
            _kbFlush = Key.LeftAlt;
        }

        public void Run(InputState input, float deltaSeconds)
        {
            Run(input, null, deltaSeconds);
        }

        public void Run(InputState input, JoystickStateSnapshot? joystick, float deltaSeconds)
        {
            _prevState.CopyFrom(_lastState);
            _lastState.CopyFrom(input);
            if (joystick.HasValue)
            {
                if (_hasPrevJoystick)
                    _prevJoystick = _lastJoystick;
                _lastJoystick = joystick.Value;
                if (!_hasCenter)
                {
                    _center = joystick.Value;
                    _hasCenter = true;
                }
                if (!_hasPrevJoystick)
                    _prevJoystick = joystick.Value;
                _hasPrevJoystick = true;
            }
            _joystickAvailable = joystick.HasValue;
            if (!joystick.HasValue)
                _hasPrevJoystick = false;

            UpdateSimulatedInputs(deltaSeconds);
        }

        private void UpdateSimulatedInputs(float deltaSeconds)
        {
            if (_settings.KeyboardProgressiveRate == KeyboardProgressiveRate.Off)
            {
                _simThrottle = _lastState.IsDown(_kbThrottle) ? 1.0f : 0.0f;
                _simBrake = _lastState.IsDown(_kbBrake) ? 1.0f : 0.0f;
                if (_lastState.IsDown(_kbLeft)) _simSteer = -1.0f;
                else if (_lastState.IsDown(_kbRight)) _simSteer = 1.0f;
                else _simSteer = 0.0f;
                return;
            }

            float rate = _settings.KeyboardProgressiveRate switch
            {
                KeyboardProgressiveRate.Instant_0_25s => 0.25f,
                KeyboardProgressiveRate.Instant_0_50s => 0.50f,
                KeyboardProgressiveRate.Instant_0_75s => 0.75f,
                KeyboardProgressiveRate.Instant_1_00s => 1.00f,
                _ => 0.50f
            };

            // Throttle
            if (_lastState.IsDown(_kbThrottle))
            {
                _simBrake = 0f; // Opposite kill
                _simThrottle = Math.Min(1.0f, _simThrottle + (deltaSeconds / rate));
            }
            else
            {
                _simThrottle = Math.Max(0f, _simThrottle - (deltaSeconds / rate));
            }

            // Brake
            if (_lastState.IsDown(_kbBrake))
            {
                _simThrottle = 0f; // Opposite kill
                _simBrake = Math.Min(1.0f, _simBrake + (deltaSeconds / rate));
            }
            else
            {
                _simBrake = Math.Max(0f, _simBrake - (deltaSeconds / rate));
            }

            // Steering
            if (_lastState.IsDown(_kbLeft))
            {
                if (_simSteer > 0f) _simSteer = 0f; // Opposite kill
                _simSteer = Math.Max(-1.0f, _simSteer - (deltaSeconds / rate));
            }
            else if (_lastState.IsDown(_kbRight))
            {
                if (_simSteer < 0f) _simSteer = 0f; // Opposite kill
                _simSteer = Math.Min(1.0f, _simSteer + (deltaSeconds / rate));
            }
            else
            {
                if (_simSteer > 0) _simSteer = Math.Max(0f, _simSteer - (deltaSeconds / rate));
                else if (_simSteer < 0) _simSteer = Math.Min(0f, _simSteer + (deltaSeconds / rate));
            }
        }

        public void SetCenter(JoystickStateSnapshot center)
        {
            _center = center;
            _hasCenter = true;
            _settings.JoystickCenter = center;
        }

        public void SetDevice(bool useJoystick)
        {
            SetDevice(useJoystick ? InputDeviceMode.Joystick : InputDeviceMode.Keyboard);
        }

        public void SetDevice(InputDeviceMode mode)
        {
            _deviceMode = mode;
            _settings.DeviceMode = mode;
        }

        public void SetPanelInputAccess(bool allowDrivingInput, bool allowAuxiliaryInput)
        {
            _allowDrivingInput = allowDrivingInput;
            _allowAuxiliaryInput = allowAuxiliaryInput;
        }

        public void SetOverlayInputBlocked(bool blocked)
        {
            _overlayInputBlocked = blocked;
        }

        private bool IsCtrlDown()
        {
            return _lastState.IsDown(Key.LeftControl) || _lastState.IsDown(Key.RightControl);
        }

        private bool IsShiftDown()
        {
            return _lastState.IsDown(Key.LeftShift) || _lastState.IsDown(Key.RightShift);
        }

        private void ReadFromSettings()
        {
            _left = _settings.JoystickLeft;
            _right = _settings.JoystickRight;
            _throttle = _settings.JoystickThrottle;
            _brake = _settings.JoystickBrake;
            _gearUp = _settings.JoystickGearUp;
            _gearDown = _settings.JoystickGearDown;
            _horn = _settings.JoystickHorn;
            _requestInfo = _settings.JoystickRequestInfo;
            _currentGear = _settings.JoystickCurrentGear;
            _currentLapNr = _settings.JoystickCurrentLapNr;
            _currentRacePerc = _settings.JoystickCurrentRacePerc;
            _currentLapPerc = _settings.JoystickCurrentLapPerc;
            _currentRaceTime = _settings.JoystickCurrentRaceTime;
            _startEngine = _settings.JoystickStartEngine;
            _reportDistance = _settings.JoystickReportDistance;
            _reportSpeed = _settings.JoystickReportSpeed;
            _trackName = _settings.JoystickTrackName;
            _pause = _settings.JoystickPause;
            _center = _settings.JoystickCenter;
            _hasCenter = true;
            _kbLeft = _settings.KeyLeft;
            _kbRight = _settings.KeyRight;
            _kbThrottle = _settings.KeyThrottle;
            _kbBrake = _settings.KeyBrake;
            _kbGearUp = _settings.KeyGearUp;
            _kbGearDown = _settings.KeyGearDown;
            _kbHorn = _settings.KeyHorn;
            _kbRequestInfo = _settings.KeyRequestInfo;
            _kbCurrentGear = _settings.KeyCurrentGear;
            _kbCurrentLapNr = _settings.KeyCurrentLapNr;
            _kbCurrentRacePerc = _settings.KeyCurrentRacePerc;
            _kbCurrentLapPerc = _settings.KeyCurrentLapPerc;
            _kbCurrentRaceTime = _settings.KeyCurrentRaceTime;
            _kbStartEngine = _settings.KeyStartEngine;
            _kbReportDistance = _settings.KeyReportDistance;
            _kbReportSpeed = _settings.KeyReportSpeed;
            _kbTrackName = _settings.KeyTrackName;
            _kbPause = _settings.KeyPause;
            _deviceMode = _settings.DeviceMode;
        }
    }
}
