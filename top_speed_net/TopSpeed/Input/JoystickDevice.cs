using System;
using SharpDX;
using SharpDX.DirectInput;

namespace TopSpeed.Input
{
    internal sealed class JoystickDevice : IDisposable
    {
        private readonly Joystick? _joystick;
        private JoystickStateSnapshot _state;

        public JoystickDevice(DirectInput directInput, IntPtr windowHandle)
        {
            var devices = directInput.GetDevices(DeviceType.Gamepad, DeviceEnumerationFlags.AttachedOnly);
            if (devices.Count == 0)
                devices = directInput.GetDevices(DeviceType.Joystick, DeviceEnumerationFlags.AttachedOnly);
            if (devices.Count == 0)
                return;

            _joystick = new Joystick(directInput, devices[0].InstanceGuid);
            _joystick.SetCooperativeLevel(windowHandle, CooperativeLevel.Exclusive | CooperativeLevel.Background);
            _joystick.Properties.BufferSize = 128;

            foreach (var deviceObject in _joystick.GetObjects())
            {
                if ((deviceObject.ObjectId.Flags & DeviceObjectTypeFlags.Axis) != 0)
                {
                    _joystick.GetObjectPropertiesById(deviceObject.ObjectId).Range = new InputRange(-100, 100);
                }
            }

            try
            {
                _joystick.Properties.AutoCenter = false;
            }
            catch (SharpDXException)
            {
                // Some devices do not support auto-centering configuration.
            }
        }

        public bool IsAvailable => _joystick != null;

        internal Joystick? Device => _joystick;

        public bool ForceFeedbackCapable
        {
            get
            {
                if (_joystick == null)
                    return false;
                return (_joystick.Capabilities.Flags & DeviceFlags.ForceFeedback) != 0;
            }
        }

        public JoystickStateSnapshot State => _state;

        public bool Update()
        {
            if (_joystick == null)
                return false;
            try
            {
                _joystick.Acquire();
                _joystick.Poll();
                var state = _joystick.GetCurrentState();
                _state = JoystickStateSnapshot.From(state);
                return true;
            }
            catch (SharpDXException)
            {
                return false;
            }
        }

        public void Dispose()
        {
            if (_joystick == null)
                return;
            try
            {
                _joystick.Unacquire();
            }
            catch (SharpDXException)
            {
            }
            _joystick.Dispose();
        }
    }
}
