using System;
using System.Threading;
using SharpDX;
using SharpDX.DirectInput;

namespace TopSpeed.Input
{
    internal sealed class InputManager : IDisposable
    {
        private const int JoystickRescanIntervalMs = 1000;
        private const int JoystickScanTimeoutMs = 5000;
        private const int MenuBackThreshold = 50;
        private readonly DirectInput _directInput;
        private readonly Keyboard _keyboard;
        private readonly GamepadDevice _gamepad;
        private JoystickDevice? _joystick;
        private readonly InputState _current;
        private readonly InputState _previous;
        private readonly bool[] _keyLatch;
        private readonly IntPtr _windowHandle;
        private int _lastJoystickScan;
        private bool _suspended;
        private bool _menuBackLatched;
        private readonly object _hidLock = new object();
        private readonly object _hidScanLock = new object();
        private Thread? _hidScanThread;
        private CancellationTokenSource? _hidScanCts;
        private bool _joystickEnabled;
        private bool _disposed;

        public InputState Current => _current;

        public event Action? JoystickScanTimedOut;

        public InputManager(IntPtr windowHandle)
        {
            _windowHandle = windowHandle;
            _directInput = new DirectInput();
            _keyboard = new Keyboard(_directInput);
            _keyboard.Properties.BufferSize = 128;
            _keyboard.SetCooperativeLevel(windowHandle, CooperativeLevel.Foreground | CooperativeLevel.NonExclusive);
            _gamepad = new GamepadDevice();
            _current = new InputState();
            _previous = new InputState();
            _keyLatch = new bool[256];
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

            if (!_joystickEnabled)
                return;

            _gamepad.Update();
            if (!_gamepad.IsAvailable)
            {
                var joystick = GetJoystickDevice();
                if (joystick == null || !joystick.IsAvailable)
                    return;
                joystick.Update();
            }
        }

        public bool IsDown(Key key) => _current.IsDown(key);

        public bool WasPressed(Key key)
        {
            if (_suspended)
                return false;

            var index = (int)key;
            if (index < 0 || index >= _keyLatch.Length)
                return false;

            if (!TryGetKeyboardState(out var state))
            {
                _keyLatch[index] = false;
                return false;
            }

            if (state.IsPressed(key))
            {
                if (_keyLatch[index])
                    return false;
                _keyLatch[index] = true;
                return true;
            }

            _keyLatch[index] = false;
            return false;
        }

        public bool IsAnyInputHeld()
        {
            if (_suspended)
                return false;

            UpdateMenuBackLatchImmediate();

            if (IsAnyKeyboardKeyHeld())
                return true;

            return IsAnyJoystickButtonHeld();
        }

        public bool IsAnyMenuInputHeld()
        {
            if (_suspended)
                return false;

            if (IsAnyKeyboardKeyHeld(ignoreModifiers: true))
                return true;

            return IsAnyJoystickButtonHeld();
        }

        public bool IsMenuBackHeld()
        {
            if (_suspended)
                return false;

            if (IsDown(Key.Escape))
                return true;

            if (!_joystickEnabled)
                return false;

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
            if (!_joystickEnabled)
            {
                state = default;
                return false;
            }

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
            for (var i = 0; i < _keyLatch.Length; i++)
                _keyLatch[i] = false;
        }

        public IVibrationDevice? VibrationDevice => _gamepad.IsAvailable        
            ? (_joystickEnabled ? _gamepad : null)
            : (_joystickEnabled ? GetJoystickDevice() : null);

        public void SetDeviceMode(InputDeviceMode mode)
        {
            var enableJoystick = mode != InputDeviceMode.Keyboard;
            if (enableJoystick == _joystickEnabled)
                return;

            _joystickEnabled = enableJoystick;
            if (!_joystickEnabled)
            {
                StopHidScan();
                ClearJoystickDevice();
                return;
            }

            if (!_gamepad.IsAvailable && GetJoystickDevice() == null)
                StartHidScan();
        }

        private JoystickDevice? GetJoystickDevice()
        {
            lock (_hidLock)
            {
                return _joystick != null && _joystick.IsAvailable ? _joystick : null;
            }
        }

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

            JoystickDevice? joystick;
            lock (_hidLock)
            {
                joystick = _joystick;
            }
            if (joystick?.Device != null)
            {
                try
                {
                    joystick.Device.Unacquire();
                }
                catch (SharpDXException)
                {
                    // Ignore unacquire failures.
                }
            }
        }

        public void Resume()
        {
            _suspended = false;
            TryAcquire();

            JoystickDevice? joystick;
            lock (_hidLock)
            {
                joystick = _joystick;
            }
            if (joystick?.Device != null)
            {
                try
                {
                    joystick.Device.Acquire();
                }
                catch (SharpDXException)
                {
                    // Ignore acquire failures.
                }
            }
        }

        private bool IsAnyKeyboardKeyHeld(bool ignoreModifiers = false)
        {
            try
            {
                _keyboard.Acquire();
                var state = _keyboard.GetCurrentState();
                if (!ignoreModifiers)
                    return state.PressedKeys.Count > 0;

                foreach (var key in state.PressedKeys)
                {
                    if (key == Key.LeftControl || key == Key.RightControl ||
                        key == Key.LeftShift || key == Key.RightShift ||
                        key == Key.LeftAlt || key == Key.RightAlt)
                        continue;
                    return true;
                }

                return false;
            }
            catch (SharpDXException)
            {
                return false;
            }
        }

        private bool TryGetKeyboardState(out KeyboardState state)
        {
            state = null!;
            try
            {
                _keyboard.Acquire();
                state = _keyboard.GetCurrentState();
                return true;
            }
            catch (SharpDXException)
            {
                return false;
            }
        }

        private bool IsAnyJoystickButtonHeld()
        {
            if (!_joystickEnabled)
                return false;

            if (_gamepad.IsAvailable)
            {
                _gamepad.Update();
                return _gamepad.State.HasAnyButtonDown();
            }

            var joystick = GetJoystickDevice();
            if (joystick == null || !joystick.IsAvailable)
                return false;

            if (!joystick.Update())
                return false;

            return joystick.State.HasAnyButtonDown();
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

            if (!_joystickEnabled)
                return false;

            var joystick = GetJoystickDevice();
            if (joystick == null || !joystick.IsAvailable)
                return false;

            return joystick.Update() && (joystick.State.X < -MenuBackThreshold || joystick.State.Pov4);
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

        private bool TryRescanJoystick(bool force = false)
        {
            if (_disposed)
                return false;
            var now = Environment.TickCount;
            if (!force && unchecked((uint)(now - _lastJoystickScan)) < (uint)JoystickRescanIntervalMs)
                return false;
            _lastJoystickScan = now;

            JoystickDevice? newJoystick;
            try
            {
                newJoystick = new JoystickDevice(_directInput, _windowHandle);
            }
            catch (SharpDXException)
            {
                return false;
            }
            catch (NullReferenceException)
            {
                return false;
            }
            JoystickDevice? oldJoystick;
            var available = newJoystick.IsAvailable;
            lock (_hidLock)
            {
                oldJoystick = _joystick;
                _joystick = available ? newJoystick : null;
            }
            oldJoystick?.Dispose();
            if (!available)
                newJoystick.Dispose();
            return available;
        }

        private void StartHidScan()
        {
            if (_disposed || !_joystickEnabled || _gamepad.IsAvailable)
                return;
            lock (_hidScanLock)
            {
                if (_hidScanThread != null && _hidScanThread.IsAlive)
                    return;
                _hidScanCts?.Cancel();
                _hidScanCts?.Dispose();
                _hidScanCts = new CancellationTokenSource();
                var token = _hidScanCts.Token;
                _hidScanThread = new Thread(() => HidScanWorker(token))
                {
                    IsBackground = true,
                    Name = "JoystickScan"
                };
                _hidScanThread.Start();
            }
        }

        private void StopHidScan()
        {
            CancellationTokenSource? cts;
            Thread? thread;
            lock (_hidScanLock)
            {
                cts = _hidScanCts;
                thread = _hidScanThread;
                _hidScanCts = null;
                _hidScanThread = null;
            }

            if (cts != null)
            {
                cts.Cancel();
                cts.Dispose();
            }

            if (thread != null && thread.IsAlive)
                thread.Join(JoystickRescanIntervalMs + 500);
        }

        private void HidScanWorker(CancellationToken token)
        {
            var start = Environment.TickCount;
            while (true)
            {
                if (token.IsCancellationRequested || _disposed || !_joystickEnabled)
                    return;

                if (_gamepad.IsAvailable)
                    return;

                if (TryRescanJoystick(force: true))
                    return;

                var elapsed = unchecked((uint)(Environment.TickCount - start));
                if (elapsed >= (uint)JoystickScanTimeoutMs)
                {
                    JoystickScanTimedOut?.Invoke();
                    return;
                }

                if (token.WaitHandle.WaitOne(JoystickRescanIntervalMs))
                    return;
            }
        }

        private void ClearJoystickDevice()
        {
            JoystickDevice? oldJoystick;
            lock (_hidLock)
            {
                oldJoystick = _joystick;
                _joystick = null;
            }
            oldJoystick?.Dispose();
        }

        public void Dispose()
        {
            _disposed = true;
            StopHidScan();
            _keyboard.Unacquire();
            _keyboard.Dispose();
            _gamepad.Dispose();
            _joystick?.Dispose();
            _directInput.Dispose();
        }
    }
}
