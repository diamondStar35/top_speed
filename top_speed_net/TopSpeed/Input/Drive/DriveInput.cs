using System;
using System.Collections.Generic;
using Key = TopSpeed.Input.InputKey;
using TopSpeed.Input.Devices.Controller;

namespace TopSpeed.Input
{
    internal sealed partial class DriveInput
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

        private readonly struct DriveIntentMeta
        {
            public DriveIntentMeta(InputScope scope, TriggerMode keyboardMode, TriggerMode controllerMode, bool allowNumpadEnterAlias = false)
            {
                Scope = scope;
                KeyboardMode = keyboardMode;
                ControllerMode = controllerMode;
                AllowNumpadEnterAlias = allowNumpadEnterAlias;
            }

            public InputScope Scope { get; }
            public TriggerMode KeyboardMode { get; }
            public TriggerMode ControllerMode { get; }
            public bool AllowNumpadEnterAlias { get; }
        }

        private readonly struct DriveIntentBinding
        {
            public DriveIntentBinding(
                string label,
                DriveIntentMeta meta,
                Func<Key> getKey,
                Action<Key> setKey,
                Func<AxisOrButton> getAxis,
                Action<AxisOrButton> setAxis)
            {
                Label = label;
                Meta = meta;
                GetKey = getKey;
                SetKey = setKey;
                GetAxis = getAxis;
                SetAxis = setAxis;
            }

            public string Label { get; }
            public DriveIntentMeta Meta { get; }
            public Func<Key> GetKey { get; }
            public Action<Key> SetKey { get; }
            public Func<AxisOrButton> GetAxis { get; }
            public Action<AxisOrButton> SetAxis { get; }
        }

        private readonly DriveSettings _settings;
        private readonly InputState _lastState;
        private readonly InputState _prevState;
        private readonly List<DriveIntentDefinition> _intentDefinitions;
        private readonly Dictionary<DriveIntent, DriveIntentBinding> _intentBindings;
        private AxisOrButton _left;
        private AxisOrButton _right;
        private AxisOrButton _throttle;
        private AxisOrButton _brake;
        private AxisOrButton _clutch;
        private AxisOrButton _gearUp;
        private AxisOrButton _gearDown;
        private AxisOrButton _horn;
        private AxisOrButton _requestInfo;
        private AxisOrButton _currentGear;
        private AxisOrButton _currentLapNr;
        private AxisOrButton _currentRacePerc;
        private AxisOrButton _currentLapPerc;
        private AxisOrButton _currentRaceTime;
        private AxisOrButton _startEngine;
        private AxisOrButton _reportDistance;
        private AxisOrButton _reportSpeed;
        private AxisOrButton _reportFuel;
        private AxisOrButton _reportTireState;
        private AxisOrButton _trackName;
        private AxisOrButton _pause;
        private AxisOrButton _pit;
        private InputDeviceMode _deviceMode;
        private Key _kbLeft;
        private Key _kbRight;
        private Key _kbThrottle;
        private Key _kbBrake;
        private Key _kbClutch;
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
        private Key _kbReportFuel;
        private Key _kbReportTireState;
        private Key _kbTrackName;
        private Key _kbPlayerNumber;
        private Key _kbPause;
        private Key _kbPit;
        private Key _kbPlayerPos1;
        private Key _kbPlayerPos2;
        private Key _kbPlayerPos3;
        private Key _kbPlayerPos4;
        private Key _kbPlayerPos5;
        private Key _kbPlayerPos6;
        private Key _kbPlayerPos7;
        private Key _kbPlayerPos8;
        private Key _kbPlayerPos9;
        private Key _kbPlayerPos0;
        private Key _kbFlush;
        private State _center;
        private State _lastController;
        private State _prevController;
        private bool _hasCenter;
        private bool _hasPrevController;
        private bool _controllerAvailable;
        private bool _allowDrivingInput;
        private bool _allowAuxiliaryInput;
        private bool _overlayInputBlocked;
        private bool _pausedHornInputAllowed;
        private bool _controllerIsRacingWheel;
        private readonly bool[] _hasPedalCalibration = new bool[8];
        private readonly int[] _pedalRestValues = new int[8];
        private readonly int[] _pedalMinValues = new int[8];
        private readonly int[] _pedalMaxValues = new int[8];
        private float _simThrottle;
        private float _simBrake;
        private float _simSteer;
        private float _simClutch;
        private int _touchSteering;
        private int _touchThrottle;
        private int _touchBrake;
        private int _touchClutch;
        private bool _touchHorn;
        private bool _touchGearUp;
        private bool _touchGearDown;
        private bool _touchStartEngine;
        private bool _touchReportDistance;
        private bool _touchReportSpeed;
        private bool _touchReportFuel;
        private bool _touchReportTireState;
        private bool _touchCurrentGear;
        private bool _touchCurrentLapNr;
        private bool _touchCurrentRacePerc;
        private bool _touchCurrentLapPerc;
        private bool _touchCurrentRaceTime;
        private bool _touchPause;
        private bool _touchRequestInfo;
        private bool _touchPreviousPlayerInfo;
        private bool _touchNextPlayerInfo;
        private bool _touchRepeatPlayerInfo;
        private bool _touchPit;
        private DriveIntentState _intentState;
        private bool UseController => _deviceMode != InputDeviceMode.Keyboard && _controllerAvailable;
        private bool UseKeyboard => _deviceMode != InputDeviceMode.Controller || !_controllerAvailable;

        public KeyMapManager KeyMap { get; }
        public DriveIntentState Intents => _intentState;

        public DriveInput(DriveSettings settings)
        {
            _settings = settings;
            _lastState = new InputState();
            _prevState = new InputState();
            _intentDefinitions = new List<DriveIntentDefinition>();
            _intentBindings = CreateIntentBindings();
            _intentState = DriveIntentState.Empty;
            Initialize();
            KeyMap = new KeyMapManager(this);
        }
    }
}



