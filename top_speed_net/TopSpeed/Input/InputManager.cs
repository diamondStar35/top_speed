using System;
using SharpDX;
using SharpDX.DirectInput;

namespace TopSpeed.Input
{
    internal sealed class InputManager : IDisposable
    {
        private const int JoystickRescanIntervalMs = 1000;
        private const int MenuBackThreshold = 50;
        private readonly DirectInput _directInput;
        private readonly Keyboard _keyboard;
        private readonly GamepadDevice _gamepad;
        private JoystickDevice? _joystick;
        private readonly InputState _current;
        private readonly InputState _previous;
        private readonly IntPtr _windowHandle;
        private int _lastJoystickScan;
        private bool _suspended;
        private bool _menuBackLatched;

        public InputState Current => _current;

        public InputManager(IntPtr windowHandle)
        {
            _windowHandle = windowHandle;
            _directInput = new DirectInput();
            _keyboard = new Keyboard(_directInput);
            _keyboard.Properties.BufferSize = 128;
            _keyboard.SetCooperativeLevel(windowHandle, CooperativeLevel.Foreground | CooperativeLevel.NonExclusive);
            _gamepad = new GamepadDevice();
            if (!_gamepad.IsAvailable)
                TryRescanJoystick(force: true);
            _current = new InputState();
            _previous = new InputState();
            TryAcquire();
        }

        public void Update()
        {
            _previous.CopyFrom(_current);
            _current.Clear();

            if (_suspended)
                return;

            if (!TryAcquire())
                return;

            var state = _keyboard.GetCurrentState();
            foreach (var key in state.PressedKeys)
            {
                _current.Set(key, true);
            }

            _gamepad.Update();
            if (!_gamepad.IsAvailable)
            {
                if (_joystick == null || !_joystick.IsAvailable)
                    TryRescanJoystick();
                _joystick?.Update();
            }
        }

        public bool IsDown(Key key) => _current.IsDown(key);

        public bool WasPressed(Key key) => _current.IsDown(key) && !_previous.IsDown(key);

        public bool IsAnyInputHeld()
        {
            if (_suspended)
                return false;

            UpdateMenuBackLatchImmediate();

            if (IsAnyKeyboardKeyHeld())
                return true;

            return IsAnyJoystickButtonHeld();
        }

        public bool IsMenuBackHeld()
        {
            if (_suspended)
                return false;

            if (IsDown(Key.Escape))
                return true;

            if (TryGetJoystickState(out var state))
                return state.X < -MenuBackThreshold || state.Pov4;

            return false;
        }

        public void LatchMenuBack()
        {
            _menuBackLatched = true;
        }

        public bool ShouldIgnoreMenuBack()
        {
            if (!_menuBackLatched)
                return false;
            if (IsMenuBackHeld())
                return true;
            _menuBackLatched = false;
            return false;
        }

        public bool TryGetJoystickState(out JoystickStateSnapshot state)        
        {
            var device = VibrationDevice;
            if (device != null && device.IsAvailable)
            {
                state = device.State;
                return true;
            }
            state = default;
            return false;
        }

        public void ResetState()
        {
            _current.Clear();
            _previous.Clear();
        }

        public IVibrationDevice? VibrationDevice => _gamepad.IsAvailable        
            ? _gamepad
            : (_joystick != null && _joystick.IsAvailable ? _joystick : null);  

        public void Suspend()
        {
            _suspended = true;
            try
            {
                _keyboard.Unacquire();
            }
            catch (SharpDXException)
            {
                // Ignore unacquire failures.
            }
        }

        public void Resume()
        {
            _suspended = false;
            TryAcquire();
        }

        private bool IsAnyKeyboardKeyHeld()
        {
            try
            {
                _keyboard.Acquire();
                var state = _keyboard.GetCurrentState();
                return state.PressedKeys.Count > 0;
            }
            catch (SharpDXException)
            {
                return false;
            }
        }

        private bool IsAnyJoystickButtonHeld()
        {
            if (_gamepad.IsAvailable)
            {
                _gamepad.Update();
                return _gamepad.State.HasAnyButtonDown();
            }

            if (_joystick == null || !_joystick.IsAvailable)
                TryRescanJoystick();

            if (_joystick == null || !_joystick.IsAvailable)
                return false;

            return _joystick.Update() && _joystick.State.HasAnyButtonDown();
        }

        private void UpdateMenuBackLatchImmediate()
        {
            if (!_menuBackLatched)
                return;
            if (!IsMenuBackHeldImmediate())
                _menuBackLatched = false;
        }

        private bool IsMenuBackHeldImmediate()
        {
            try
            {
                _keyboard.Acquire();
                var state = _keyboard.GetCurrentState();
                foreach (var key in state.PressedKeys)
                {
                    if (key == Key.Escape)
                        return true;
                }
            }
            catch (SharpDXException)
            {
            }

            if (_gamepad.IsAvailable)
            {
                _gamepad.Update();
                var state = _gamepad.State;
                return state.X < -MenuBackThreshold || state.Pov4;
            }

            if (_joystick == null || !_joystick.IsAvailable)
                TryRescanJoystick();

            if (_joystick == null || !_joystick.IsAvailable)
                return false;

            return _joystick.Update() && (_joystick.State.X < -MenuBackThreshold || _joystick.State.Pov4);
        }

        private bool TryAcquire()
        {
            try
            {
                _keyboard.Acquire();
                return true;
            }
            catch (SharpDXException)
            {
                return false;
            }
        }

        private void TryRescanJoystick(bool force = false)
        {
            var now = Environment.TickCount;
            if (!force && unchecked((uint)(now - _lastJoystickScan)) < (uint)JoystickRescanIntervalMs)
                return;
            _lastJoystickScan = now;

            _joystick?.Dispose();
            _joystick = new JoystickDevice(_directInput, _windowHandle);
            if (_joystick.IsAvailable)
                return;

            _joystick.Dispose();
            _joystick = null;
        }

        public void Dispose()
        {
            _keyboard.Unacquire();
            _keyboard.Dispose();
            _gamepad.Dispose();
            _joystick?.Dispose();
            _directInput.Dispose();
        }
    }
}
