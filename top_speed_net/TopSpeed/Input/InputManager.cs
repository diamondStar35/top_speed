using System;
using SharpDX;
using SharpDX.DirectInput;

namespace TopSpeed.Input
{
    internal sealed class InputManager : IDisposable
    {
        private readonly DirectInput _directInput;
        private readonly Keyboard _keyboard;
        private readonly JoystickDevice? _joystick;
        private readonly InputState _current;
        private readonly InputState _previous;

        public InputState Current => _current;

        public InputManager(IntPtr windowHandle)
        {
            _directInput = new DirectInput();
            _keyboard = new Keyboard(_directInput);
            _keyboard.Properties.BufferSize = 128;
            _keyboard.SetCooperativeLevel(windowHandle, CooperativeLevel.Foreground | CooperativeLevel.NonExclusive);
            _joystick = new JoystickDevice(_directInput, windowHandle);
            _current = new InputState();
            _previous = new InputState();
            TryAcquire();
        }

        public void Update()
        {
            _previous.CopyFrom(_current);
            _current.Clear();

            if (!TryAcquire())
                return;

            var state = _keyboard.GetCurrentState();
            foreach (var key in state.PressedKeys)
            {
                _current.Set(key, true);
            }

            _joystick?.Update();
        }

        public bool IsDown(Key key) => _current.IsDown(key);

        public bool WasPressed(Key key) => _current.IsDown(key) && !_previous.IsDown(key);

        public bool TryGetJoystickState(out JoystickStateSnapshot state)
        {
            if (_joystick != null && _joystick.IsAvailable)
            {
                state = _joystick.State;
                return true;
            }
            state = default;
            return false;
        }

        public JoystickDevice? Joystick => _joystick;

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

        public void Dispose()
        {
            _keyboard.Unacquire();
            _keyboard.Dispose();
            _joystick?.Dispose();
            _directInput.Dispose();
        }
    }
}
