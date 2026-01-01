using System;
using SharpDX;
using SharpDX.DirectInput;

namespace TopSpeed.Input
{
    internal sealed class InputManager : IDisposable
    {
        private const int JoystickRescanIntervalMs = 1000;
        private readonly DirectInput _directInput;
        private readonly Keyboard _keyboard;
        private readonly GamepadDevice _gamepad;
        private JoystickDevice? _joystick;
        private readonly InputState _current;
        private readonly InputState _previous;
        private readonly IntPtr _windowHandle;
        private int _lastJoystickScan;
        private bool _suspended;

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
