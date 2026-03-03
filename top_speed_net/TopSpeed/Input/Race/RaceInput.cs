using System;
using System.Collections.Generic;
using SharpDX.DirectInput;

namespace TopSpeed.Input
{
    internal sealed partial class RaceInput
    {
        private enum InputScope
        {
            Driving,
            Auxiliary
        }

        private enum TriggerMode
        {
            Hold,
            Press
        }

        private readonly struct InputActionMeta
        {
            public InputActionMeta(InputScope scope, TriggerMode keyboardMode, TriggerMode joystickMode, bool allowNumpadEnterAlias = false)
            {
                Scope = scope;
                KeyboardMode = keyboardMode;
                JoystickMode = joystickMode;
                AllowNumpadEnterAlias = allowNumpadEnterAlias;
            }

            public InputScope Scope { get; }
            public TriggerMode KeyboardMode { get; }
            public TriggerMode JoystickMode { get; }
            public bool AllowNumpadEnterAlias { get; }
        }

        private readonly struct InputActionBinding
        {
            public InputActionBinding(
                string label,
                InputActionMeta meta,
                Func<Key> getKey,
                Action<Key> setKey,
                Func<JoystickAxisOrButton> getAxis,
                Action<JoystickAxisOrButton> setAxis)
            {
                Label = label;
                Meta = meta;
                GetKey = getKey;
                SetKey = setKey;
                GetAxis = getAxis;
                SetAxis = setAxis;
            }

            public string Label { get; }
            public InputActionMeta Meta { get; }
            public Func<Key> GetKey { get; }
            public Action<Key> SetKey { get; }
            public Func<JoystickAxisOrButton> GetAxis { get; }
            public Action<JoystickAxisOrButton> SetAxis { get; }
        }

        private readonly RaceSettings _settings;
        private readonly InputState _lastState;
        private readonly InputState _prevState;
        private readonly List<InputActionDefinition> _actionDefinitions;
        private readonly Dictionary<InputAction, InputActionBinding> _actionBindings;
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
        private JoystickAxisOrButton _startEngine;
        private JoystickAxisOrButton _reportDistance;
        private JoystickAxisOrButton _reportSpeed;
        private JoystickAxisOrButton _trackName;
        private JoystickAxisOrButton _pause;
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
        private Key _kbStartEngine;
        private Key _kbReportDistance;
        private Key _kbReportSpeed;
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
        private bool _allowDrivingInput;
        private bool _allowAuxiliaryInput;
        private bool _overlayInputBlocked;

        private float _simThrottle;
        private float _simBrake;
        private float _simSteer;

        private bool UseJoystick => _deviceMode != InputDeviceMode.Keyboard && _joystickAvailable;
        private bool UseKeyboard => _deviceMode != InputDeviceMode.Joystick || !_joystickAvailable;

        public KeyMapManager KeyMap { get; }

        public RaceInput(RaceSettings settings)
        {
            _settings = settings;
            _lastState = new InputState();
            _prevState = new InputState();
            _actionDefinitions = new List<InputActionDefinition>();
            _actionBindings = CreateActionBindings();
            Initialize();
            KeyMap = new KeyMapManager(this);
        }
    }
}
