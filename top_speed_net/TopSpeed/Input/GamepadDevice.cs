using System;
using System.Collections.Generic;
using SharpDX.XInput;

namespace TopSpeed.Input
{
    internal sealed class GamepadDevice : IVibrationDevice
    {
        private Controller? _controller;
        private JoystickStateSnapshot _state;
        private readonly Dictionary<VibrationEffectType, VibrationState> _activeEffects = new Dictionary<VibrationEffectType, VibrationState>();
        private bool _connected;

        private struct VibrationState
        {
            public float LeftMotor;
            public float RightMotor;
            public int Gain;
        }

        public GamepadDevice()
        {
            TryConnect();
        }

        public bool IsAvailable => _connected;

        public JoystickStateSnapshot State => _state;

        public bool ForceFeedbackCapable => true; // XInput always supports vibration

        public bool Update()
        {
            if (_controller == null || !_controller.IsConnected)
            {
                if (!TryConnect())
                {
                    _activeEffects.Clear();
                    return false;
                }
            }

            var controller = _controller;
            if (controller == null)
            {
                _connected = false;
                return false;
            }

            _connected = true;
            var state = controller.GetState();
            var gamepad = state.Gamepad;

            // Map XInput to JoystickStateSnapshot
            // Axis range in JoystickDevice is -100 to 100.
            // XInput is -32768 to 32767.
            
            _state = new JoystickStateSnapshot
            {
                X = ScaleAxis(gamepad.LeftThumbX),
                Y = -ScaleAxis(gamepad.LeftThumbY), // Invert Y to match DirectInput usually
                Rx = ScaleAxis(gamepad.RightThumbX),
                Ry = -ScaleAxis(gamepad.RightThumbY),
                // Map Triggers to Z and Rz axes
                Z = ScaleTrigger(gamepad.LeftTrigger),
                Rz = ScaleTrigger(gamepad.RightTrigger),
                
                // Buttons
                B1 = (gamepad.Buttons & GamepadButtonFlags.A) != 0,
                B2 = (gamepad.Buttons & GamepadButtonFlags.B) != 0,
                B3 = (gamepad.Buttons & GamepadButtonFlags.X) != 0,
                B4 = (gamepad.Buttons & GamepadButtonFlags.Y) != 0,
                B5 = (gamepad.Buttons & GamepadButtonFlags.LeftShoulder) != 0,
                B6 = (gamepad.Buttons & GamepadButtonFlags.RightShoulder) != 0,
                B7 = (gamepad.Buttons & GamepadButtonFlags.Back) != 0,
                B8 = (gamepad.Buttons & GamepadButtonFlags.Start) != 0,
                B9 = (gamepad.Buttons & GamepadButtonFlags.LeftThumb) != 0,
                B10 = (gamepad.Buttons & GamepadButtonFlags.RightThumb) != 0,
            };

            // POV
            int pov = -1;
            if ((gamepad.Buttons & GamepadButtonFlags.DPadUp) != 0)
            {
                if ((gamepad.Buttons & GamepadButtonFlags.DPadRight) != 0) pov = 4500;
                else if ((gamepad.Buttons & GamepadButtonFlags.DPadLeft) != 0) pov = 31500;
                else pov = 0;
            }
            else if ((gamepad.Buttons & GamepadButtonFlags.DPadDown) != 0)
            {
                if ((gamepad.Buttons & GamepadButtonFlags.DPadRight) != 0) pov = 13500;
                else if ((gamepad.Buttons & GamepadButtonFlags.DPadLeft) != 0) pov = 22500;
                else pov = 18000;
            }
            else if ((gamepad.Buttons & GamepadButtonFlags.DPadRight) != 0) pov = 9000;
            else if ((gamepad.Buttons & GamepadButtonFlags.DPadLeft) != 0) pov = 27000;

            if (pov != -1)
            {
                // Set POV boolean flags based on angle (simplified)
                // Existing code checks Pov1-Pov4 etc manually via SetPov helper, 
                // but JoystickStateSnapshot.From() does that.
                // We should replicate the Snapshot logic or reuse it.
                // Since Snapshot.From is internal and tied to DirectInput State, we manually fill booleans?
                // Actually JoystickStateSnapshot struct has Pov1 bools.
                // Let's implement the logic briefly:
                _state.Pov1 = (pov == 0 || pov == 4500 || pov == 31500); // Up
                _state.Pov2 = (pov == 4500 || pov == 9000 || pov == 13500); // Right
                _state.Pov3 = (pov == 13500 || pov == 18000 || pov == 22500); // Down
                _state.Pov4 = (pov == 22500 || pov == 27000 || pov == 31500); // Left
            }

            UpdateVibration();

            return true;
        }

        private int ScaleAxis(short value)
        {
            // -32768 to 32767 -> -100 to 100
            return (int)(value / 327.67f);
        }

        private int ScaleTrigger(byte value)
        {
            // 0-255 -> 0-100 (Unsigned axis usually, but DirectInput ranges are -100 to 100.
            // If we use Z axis, typically expected 0 to 100 or -100 to 100.
            // Let's map 0..255 to 0..100.
            return (int)(value / 2.55f);
        }

        private bool TryConnect()
        {
            var indices = new[] { UserIndex.One, UserIndex.Two, UserIndex.Three, UserIndex.Four };
            foreach (var index in indices)
            {
                var controller = new Controller(index);
                if (controller.IsConnected)
                {
                    _controller = controller;
                    _connected = true;
                    return true;
                }
            }

            _controller = null;
            _connected = false;
            return false;
        }

        public void LoadEffect(VibrationEffectType type, string effectPath)
        {
            // No-op for XInput, we ignore files
        }

        public void PlayEffect(VibrationEffectType type, int intensity = 10000)
        {
            // Map effects to rumble strength
            var left = 0f;
            var right = 0f;

            switch (type)
            {
                case VibrationEffectType.Start:
                    left = 0.5f; right = 0.5f; break; // Jolt
                case VibrationEffectType.Crash:
                    left = 1.0f; right = 1.0f; break; // Strong rumble
                case VibrationEffectType.Engine:
                    left = 0.2f; right = 0.0f; break; // Low rumble
                case VibrationEffectType.Gravel:
                    left = 0.3f; right = 0.4f; break;
                case VibrationEffectType.Spring:
                    left = 0.0f; right = 0.0f; break; // Spring is centering force, not vibration
                case VibrationEffectType.CurbLeft:
                case VibrationEffectType.CurbRight:
                case VibrationEffectType.BumpLeft:
                case VibrationEffectType.BumpRight:
                    left = 0.0f; right = 0.6f; break; // Sharp bump
            }

            _activeEffects[type] = new VibrationState { LeftMotor = left, RightMotor = right, Gain = intensity };
            UpdateVibration();
        }

        public void StopEffect(VibrationEffectType type)
        {
            if (_activeEffects.ContainsKey(type))
            {
                _activeEffects.Remove(type);
                UpdateVibration();
            }
        }

        public void Gain(VibrationEffectType type, int value)
        {
            if (_activeEffects.TryGetValue(type, out var state))
            {
                state.Gain = value;
                _activeEffects[type] = state;
                UpdateVibration();
            }
        }

        private void UpdateVibration()
        {
            if (_controller == null || !_connected) return;

            float leftSum = 0;
            float rightSum = 0;

            foreach (var state in _activeEffects.Values)
            {
                float gain = state.Gain / 10000.0f;
                leftSum += state.LeftMotor * gain;
                rightSum += state.RightMotor * gain;
            }

            // Clamp
            if (leftSum > 1.0f) leftSum = 1.0f;
            if (rightSum > 1.0f) rightSum = 1.0f;

            var vib = new Vibration
            {
                LeftMotorSpeed = (ushort)(leftSum * 65535),
                RightMotorSpeed = (ushort)(rightSum * 65535)
            };
            _controller.SetVibration(vib);
        }

        public void Dispose()
        {
            if (_controller != null && _connected)
            {
                _controller.SetVibration(new Vibration());
            }
            _activeEffects.Clear();
        }
    }
}
