using System;
using System.Collections.Generic;
using SharpDX;
using SharpDX.DirectInput;

namespace TopSpeed.Input
{
    internal sealed class JoystickDevice : IVibrationDevice
    {
        private readonly Joystick? _joystick;
        private JoystickStateSnapshot _state;
        private readonly Dictionary<VibrationEffectType, ForceFeedbackEffect> _effects = new Dictionary<VibrationEffectType, ForceFeedbackEffect>();
        private bool _connected;

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

            _connected = true;
        }

        public bool IsAvailable => _joystick != null && _connected;

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
                _connected = true;
                return true;
            }
            catch (SharpDXException)
            {
                _connected = false;
                return false;
            }
        }

        public void LoadEffect(VibrationEffectType type, string effectPath)
        {
            if (!ForceFeedbackCapable || _joystick == null)
                return;

            if (_effects.TryGetValue(type, out var existing))
            {
                existing.Dispose();
                _effects.Remove(type);
            }

            _effects[type] = new ForceFeedbackEffect(this, effectPath);
        }

        public void PlayEffect(VibrationEffectType type, int intensity = 10000)
        {
            if (_effects.TryGetValue(type, out var effect))
                effect.Play();
        }

        public void StopEffect(VibrationEffectType type)
        {
            if (_effects.TryGetValue(type, out var effect))
                effect.Stop();
        }

        public void Gain(VibrationEffectType type, int value)
        {
            if (_effects.TryGetValue(type, out var effect))
                effect.Gain(value);
        }

        public void Dispose()
        {
            if (_joystick == null)
                return;
            foreach (var effect in _effects.Values)
                effect.Dispose();
            _effects.Clear();
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
