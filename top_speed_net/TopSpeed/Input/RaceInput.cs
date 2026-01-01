using System;
using SharpDX.DirectInput;

namespace TopSpeed.Input
{
    internal sealed class RaceInput
    {
        private readonly RaceSettings _settings;
        private readonly InputState _lastState;
        private readonly InputState _prevState;
        private JoystickAxisOrButton _left;
        private JoystickAxisOrButton _right;
        private JoystickAxisOrButton _throttle;
        private JoystickAxisOrButton _brake;
        private JoystickAxisOrButton _gearUp;
        private JoystickAxisOrButton _gearDown;
        private JoystickAxisOrButton _horn;
        private JoystickAxisOrButton _requestInfo;
        private JoystickAxisOrButton _currentGear;
        private JoystickAxisOrButton _currentLapNr;
        private JoystickAxisOrButton _currentRacePerc;
        private JoystickAxisOrButton _currentLapPerc;
        private JoystickAxisOrButton _currentRaceTime;
        private InputDeviceMode _deviceMode;
        private Key _kbLeft;
        private Key _kbRight;
        private Key _kbThrottle;
        private Key _kbBrake;
        private Key _kbGearUp;
        private Key _kbGearDown;
        private Key _kbHorn;
        private Key _kbRequestInfo;
        private Key _kbCurrentGear;
        private Key _kbCurrentLapNr;
        private Key _kbCurrentRacePerc;
        private Key _kbCurrentLapPerc;
        private Key _kbCurrentRaceTime;
        private Key _kbPlayer1;
        private Key _kbPlayer2;
        private Key _kbPlayer3;
        private Key _kbPlayer4;
        private Key _kbPlayer5;
        private Key _kbPlayer6;
        private Key _kbPlayer7;
        private Key _kbPlayer8;
        private Key _kbTrackName;
        private Key _kbPlayerNumber;
        private Key _kbPause;
        private Key _kbPlayerPos1;
        private Key _kbPlayerPos2;
        private Key _kbPlayerPos3;
        private Key _kbPlayerPos4;
        private Key _kbPlayerPos5;
        private Key _kbPlayerPos6;
        private Key _kbPlayerPos7;
        private Key _kbPlayerPos8;
        private Key _kbFlush;
        private JoystickStateSnapshot _center;
        private JoystickStateSnapshot _lastJoystick;
        private JoystickStateSnapshot _prevJoystick;
        private bool _hasCenter;
        private bool _hasPrevJoystick;
        private bool _joystickAvailable;
        private bool UseJoystick => _deviceMode != InputDeviceMode.Keyboard && _joystickAvailable;
        private bool UseKeyboard => _deviceMode != InputDeviceMode.Joystick || !_joystickAvailable;

        public RaceInput(RaceSettings settings)
        {
            _settings = settings;
            _lastState = new InputState();
            _prevState = new InputState();
            Initialize();
        }

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
            ReadFromSettings();

            _kbPlayer1 = Key.F1;
            _kbPlayer2 = Key.F2;
            _kbPlayer3 = Key.F3;
            _kbPlayer4 = Key.F4;
            _kbPlayer5 = Key.F5;
            _kbPlayer6 = Key.F6;
            _kbPlayer7 = Key.F7;
            _kbPlayer8 = Key.F8;
            _kbTrackName = Key.F9;
            _kbPlayerNumber = Key.F11;
            _kbPause = Key.F12;
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

        public void Run(InputState input)
        {
            Run(input, null);
        }

        public void Run(InputState input, JoystickStateSnapshot? joystick)      
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
        }

        public void SetLeft(JoystickAxisOrButton a)
        {
            _left = a;
            _settings.JoystickLeft = a;
        }

        public void SetLeft(Key key)
        {
            _kbLeft = key;
            _settings.KeyLeft = key;
        }

        public void SetRight(JoystickAxisOrButton a)
        {
            _right = a;
            _settings.JoystickRight = a;
        }

        public void SetRight(Key key)
        {
            _kbRight = key;
            _settings.KeyRight = key;
        }

        public void SetThrottle(JoystickAxisOrButton a)
        {
            _throttle = a;
            _settings.JoystickThrottle = a;
        }

        public void SetThrottle(Key key)
        {
            _kbThrottle = key;
            _settings.KeyThrottle = key;
        }

        public void SetBrake(JoystickAxisOrButton a)
        {
            _brake = a;
            _settings.JoystickBrake = a;
        }

        public void SetBrake(Key key)
        {
            _kbBrake = key;
            _settings.KeyBrake = key;
        }

        public void SetGearUp(JoystickAxisOrButton a)
        {
            _gearUp = a;
            _settings.JoystickGearUp = a;
        }

        public void SetGearUp(Key key)
        {
            _kbGearUp = key;
            _settings.KeyGearUp = key;
        }

        public void SetGearDown(JoystickAxisOrButton a)
        {
            _gearDown = a;
            _settings.JoystickGearDown = a;
        }

        public void SetGearDown(Key key)
        {
            _kbGearDown = key;
            _settings.KeyGearDown = key;
        }

        public void SetHorn(JoystickAxisOrButton a)
        {
            _horn = a;
            _settings.JoystickHorn = a;
        }

        public void SetHorn(Key key)
        {
            _kbHorn = key;
            _settings.KeyHorn = key;
        }

        public void SetRequestInfo(JoystickAxisOrButton a)
        {
            _requestInfo = a;
            _settings.JoystickRequestInfo = a;
        }

        public void SetRequestInfo(Key key)
        {
            _kbRequestInfo = key;
            _settings.KeyRequestInfo = key;
        }

        public void SetCurrentGear(JoystickAxisOrButton a)
        {
            _currentGear = a;
            _settings.JoystickCurrentGear = a;
        }

        public void SetCurrentGear(Key key)
        {
            _kbCurrentGear = key;
            _settings.KeyCurrentGear = key;
        }

        public void SetCurrentLapNr(JoystickAxisOrButton a)
        {
            _currentLapNr = a;
            _settings.JoystickCurrentLapNr = a;
        }

        public void SetCurrentLapNr(Key key)
        {
            _kbCurrentLapNr = key;
            _settings.KeyCurrentLapNr = key;
        }

        public void SetCurrentRacePerc(JoystickAxisOrButton a)
        {
            _currentRacePerc = a;
            _settings.JoystickCurrentRacePerc = a;
        }

        public void SetCurrentRacePerc(Key key)
        {
            _kbCurrentRacePerc = key;
            _settings.KeyCurrentRacePerc = key;
        }

        public void SetCurrentLapPerc(JoystickAxisOrButton a)
        {
            _currentLapPerc = a;
            _settings.JoystickCurrentLapPerc = a;
        }

        public void SetCurrentLapPerc(Key key)
        {
            _kbCurrentLapPerc = key;
            _settings.KeyCurrentLapPerc = key;
        }

        public void SetCurrentRaceTime(JoystickAxisOrButton a)
        {
            _currentRaceTime = a;
            _settings.JoystickCurrentRaceTime = a;
        }

        public void SetCurrentRaceTime(Key key)
        {
            _kbCurrentRaceTime = key;
            _settings.KeyCurrentRaceTime = key;
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

        public int GetSteering()
        {
            var joystickSteer = 0;
            if (UseJoystick)
            {
                var left = GetAxis(_left);
                var right = GetAxis(_right);
                joystickSteer = left != 0 ? -left : right;
                if (joystickSteer != 0 || !UseKeyboard)
                    return joystickSteer;
            }

            if (UseKeyboard)
            {
                if (_lastState.IsDown(_kbLeft))
                    return -100;
                if (_lastState.IsDown(_kbRight))
                    return 100;
            }

            return joystickSteer;
        }

        public int GetThrottle()
        {
            var joystickThrottle = UseJoystick ? GetAxis(_throttle) : 0;
            if (joystickThrottle != 0 || !UseKeyboard)
                return joystickThrottle;

            return UseKeyboard && _lastState.IsDown(_kbThrottle) ? 100 : 0;
        }

        public int GetBrake()
        {
            var joystickBrake = UseJoystick ? -GetAxis(_brake) : 0;
            if (joystickBrake != 0 || !UseKeyboard)
                return joystickBrake;

            return UseKeyboard && _lastState.IsDown(_kbBrake) ? -100 : 0;
        }

        public bool GetGearUp() => (UseKeyboard && _lastState.IsDown(_kbGearUp)) || (UseJoystick && GetAxis(_gearUp) > 50);

        public bool GetGearDown() => (UseKeyboard && _lastState.IsDown(_kbGearDown)) || (UseJoystick && GetAxis(_gearDown) > 50);

        public bool GetHorn() => (UseKeyboard && _lastState.IsDown(_kbHorn)) || (UseJoystick && GetAxis(_horn) > 50);

        public bool GetRequestInfo() => (UseKeyboard && _lastState.IsDown(_kbRequestInfo)) || (UseJoystick && GetAxis(_requestInfo) > 50);

        public bool GetCurrentGear() => (UseKeyboard && WasPressed(_kbCurrentGear)) || AxisPressed(_currentGear);

        public bool GetCurrentLapNr() => (UseKeyboard && WasPressed(_kbCurrentLapNr)) || AxisPressed(_currentLapNr);

        public bool GetCurrentRacePerc() => (UseKeyboard && WasPressed(_kbCurrentRacePerc)) || AxisPressed(_currentRacePerc);

        public bool GetCurrentLapPerc() => (UseKeyboard && WasPressed(_kbCurrentLapPerc)) || AxisPressed(_currentLapPerc);

        public bool GetCurrentRaceTime() => (UseKeyboard && WasPressed(_kbCurrentRaceTime)) || AxisPressed(_currentRaceTime);

        public bool TryGetPlayerInfo(out int player)
        {
            if (WasPressed(_kbPlayer1)) { player = 0; return true; }
            if (WasPressed(_kbPlayer2)) { player = 1; return true; }
            if (WasPressed(_kbPlayer3)) { player = 2; return true; }
            if (WasPressed(_kbPlayer4)) { player = 3; return true; }
            if (WasPressed(_kbPlayer5)) { player = 4; return true; }
            if (WasPressed(_kbPlayer6)) { player = 5; return true; }
            if (WasPressed(_kbPlayer7)) { player = 6; return true; }
            if (WasPressed(_kbPlayer8)) { player = 7; return true; }
            player = 0;
            return false;
        }

        public bool TryGetPlayerPosition(out int player)
        {
            if (WasPressed(_kbPlayerPos1)) { player = 0; return true; }
            if (WasPressed(_kbPlayerPos2)) { player = 1; return true; }
            if (WasPressed(_kbPlayerPos3)) { player = 2; return true; }
            if (WasPressed(_kbPlayerPos4)) { player = 3; return true; }
            if (WasPressed(_kbPlayerPos5)) { player = 4; return true; }
            if (WasPressed(_kbPlayerPos6)) { player = 5; return true; }
            if (WasPressed(_kbPlayerPos7)) { player = 6; return true; }
            if (WasPressed(_kbPlayerPos8)) { player = 7; return true; }
            player = 0;
            return false;
        }

        public bool GetTrackName() => WasPressed(_kbTrackName);

        public bool GetPlayerNumber() => WasPressed(_kbPlayerNumber);

        public bool GetPause() => _lastState.IsDown(_kbPause);

        public bool GetStartEngine() => WasPressed(Key.Return) || WasPressed(Key.NumberPadEnter);

        public bool GetFlush() => _lastState.IsDown(_kbFlush);

        // Speed and distance reporting hotkeys
        public bool GetSpeedReport() => WasPressed(Key.S);
        public bool GetDistanceReport() => WasPressed(Key.C);

        private int GetAxis(JoystickAxisOrButton axis)
        {
            return GetAxis(axis, _lastJoystick);
        }

        private int GetAxis(JoystickAxisOrButton axis, JoystickStateSnapshot state)
        {
            switch (axis)
            {
                case JoystickAxisOrButton.AxisNone:
                    return 0;
                case JoystickAxisOrButton.AxisXNeg:
                    if (_center.X - state.X > 0)
                        return Math.Min(_center.X - state.X, 100);
                    break;
                case JoystickAxisOrButton.AxisXPos:
                    if (state.X - _center.X > 0)
                        return Math.Min(state.X - _center.X, 100);
                    break;
                case JoystickAxisOrButton.AxisYNeg:
                    if (_center.Y - state.Y > 0)
                        return Math.Min(_center.Y - state.Y, 100);
                    break;
                case JoystickAxisOrButton.AxisYPos:
                    if (state.Y - _center.Y > 0)
                        return Math.Min(state.Y - _center.Y, 100);
                    break;
                case JoystickAxisOrButton.AxisZNeg:
                    if (_center.Z - state.Z > 0)
                        return Math.Min(_center.Z - state.Z, 100);
                    break;
                case JoystickAxisOrButton.AxisZPos:
                    if (state.Z - _center.Z > 0)
                        return Math.Min(state.Z - _center.Z, 100);
                    break;
                case JoystickAxisOrButton.AxisRxNeg:
                    if (_center.Rx - state.Rx > 0)
                        return Math.Min(_center.Rx - state.Rx, 100);
                    break;
                case JoystickAxisOrButton.AxisRxPos:
                    if (state.Rx - _center.Rx > 0)
                        return Math.Min(state.Rx - _center.Rx, 100);
                    break;
                case JoystickAxisOrButton.AxisRyNeg:
                    if (_center.Ry - state.Ry > 0)
                        return Math.Min(_center.Ry - state.Ry, 100);
                    break;
                case JoystickAxisOrButton.AxisRyPos:
                    if (state.Ry - _center.Ry > 0)
                        return Math.Min(state.Ry - _center.Ry, 100);
                    break;
                case JoystickAxisOrButton.AxisRzNeg:
                    if (_center.Rz - state.Rz > 0)
                        return Math.Min(_center.Rz - state.Rz, 100);
                    break;
                case JoystickAxisOrButton.AxisRzPos:
                    if (state.Rz - _center.Rz > 0)
                        return Math.Min(state.Rz - _center.Rz, 100);
                    break;
                case JoystickAxisOrButton.AxisSlider1Neg:
                    if (_center.Slider1 - state.Slider1 > 0)
                        return Math.Min(_center.Slider1 - state.Slider1, 100);
                    break;
                case JoystickAxisOrButton.AxisSlider1Pos:
                    if (state.Slider1 - _center.Slider1 > 0)
                        return Math.Min(state.Slider1 - _center.Slider1, 100);
                    break;
                case JoystickAxisOrButton.AxisSlider2Neg:
                    if (_center.Slider2 - state.Slider2 > 0)
                        return Math.Min(_center.Slider2 - state.Slider2, 100);
                    break;
                case JoystickAxisOrButton.AxisSlider2Pos:
                    if (state.Slider2 - _center.Slider2 > 0)
                        return Math.Min(state.Slider2 - _center.Slider2, 100);
                    break;
                case JoystickAxisOrButton.Button1:
                    return state.B1 ? 100 : 0;
                case JoystickAxisOrButton.Button2:
                    return state.B2 ? 100 : 0;
                case JoystickAxisOrButton.Button3:
                    return state.B3 ? 100 : 0;
                case JoystickAxisOrButton.Button4:
                    return state.B4 ? 100 : 0;
                case JoystickAxisOrButton.Button5:
                    return state.B5 ? 100 : 0;
                case JoystickAxisOrButton.Button6:
                    return state.B6 ? 100 : 0;
                case JoystickAxisOrButton.Button7:
                    return state.B7 ? 100 : 0;
                case JoystickAxisOrButton.Button8:
                    return state.B8 ? 100 : 0;
                case JoystickAxisOrButton.Button9:
                    return state.B9 ? 100 : 0;
                case JoystickAxisOrButton.Button10:
                    return state.B10 ? 100 : 0;
                case JoystickAxisOrButton.Button11:
                    return state.B11 ? 100 : 0;
                case JoystickAxisOrButton.Button12:
                    return state.B12 ? 100 : 0;
                case JoystickAxisOrButton.Button13:
                    return state.B13 ? 100 : 0;
                case JoystickAxisOrButton.Button14:
                    return state.B14 ? 100 : 0;
                case JoystickAxisOrButton.Button15:
                    return state.B15 ? 100 : 0;
                case JoystickAxisOrButton.Button16:
                    return state.B16 ? 100 : 0;
                case JoystickAxisOrButton.Pov1:
                    return state.Pov1 ? 100 : 0;
                case JoystickAxisOrButton.Pov2:
                    return state.Pov2 ? 100 : 0;
                case JoystickAxisOrButton.Pov3:
                    return state.Pov3 ? 100 : 0;
                case JoystickAxisOrButton.Pov4:
                    return state.Pov4 ? 100 : 0;
                case JoystickAxisOrButton.Pov5:
                    return state.Pov5 ? 100 : 0;
                case JoystickAxisOrButton.Pov6:
                    return state.Pov6 ? 100 : 0;
                case JoystickAxisOrButton.Pov7:
                    return state.Pov7 ? 100 : 0;
                case JoystickAxisOrButton.Pov8:
                    return state.Pov8 ? 100 : 0;
                default:
                    return 0;
            }

            return 0;
        }

        private bool WasPressed(Key key)
        {
            return _lastState.IsDown(key) && !_prevState.IsDown(key);
        }

        private bool AxisPressed(JoystickAxisOrButton axis)
        {
            if (!UseJoystick)
                return false;
            var current = GetAxis(axis, _lastJoystick);
            var previous = _hasPrevJoystick ? GetAxis(axis, _prevJoystick) : 0;
            return current > 50 && previous <= 50;
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
            _deviceMode = _settings.DeviceMode;
        }
    }
}
