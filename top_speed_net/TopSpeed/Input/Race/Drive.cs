using SharpDX.DirectInput;
using TopSpeed.Protocol;
using System;

namespace TopSpeed.Input
{
    internal sealed partial class RaceInput
    {
        public int GetSteering()
        {
            if (!_allowDrivingInput || _overlayInputBlocked)
                return 0;

            var joystickSteer = 0;
            if (UseJoystick)
            {
                var left = GetAxis(_left);
                var right = GetAxis(_right);
                joystickSteer = left != 0 ? -left : right;
            }

            if (!UseKeyboard)
                return joystickSteer;

            var keyboardSteer = _settings.KeyboardProgressiveRate != KeyboardProgressiveRate.Off
                ? (int)(_simSteer * 100.0f)
                : (_lastState.IsDown(_kbLeft) ? -100 : (_lastState.IsDown(_kbRight) ? 100 : 0));

            // Return the value with the greater magnitude (furthest from center)
            return Math.Abs(keyboardSteer) > Math.Abs(joystickSteer) ? keyboardSteer : joystickSteer;
        }

        public int GetThrottle()
        {
            if (!_allowDrivingInput || _overlayInputBlocked)
                return 0;

            var joystickThrottle = UseJoystick ? GetAxis(_throttle) : 0;
            if (!UseKeyboard)
                return joystickThrottle;

            var keyboardThrottle = _settings.KeyboardProgressiveRate != KeyboardProgressiveRate.Off
                ? (int)(_simThrottle * 100.0f)
                : (_lastState.IsDown(_kbThrottle) ? 100 : 0);

            return Math.Max(joystickThrottle, keyboardThrottle);
        }

        public int GetBrake()
        {
            if (!_allowDrivingInput || _overlayInputBlocked)
                return 0;

            var joystickBrake = UseJoystick ? -GetAxis(_brake) : 0;
            if (!UseKeyboard)
                return joystickBrake;

            var keyboardBrake = _settings.KeyboardProgressiveRate != KeyboardProgressiveRate.Off
                ? (int)(_simBrake * -100.0f)
                : (_lastState.IsDown(_kbBrake) ? -100 : 0);

            // Return the more negative value (stronger braking)
            return Math.Min(joystickBrake, keyboardBrake);
        }

        public bool GetReverseRequested() => _allowDrivingInput && UseKeyboard && WasPressed(Key.Z);

        public bool GetForwardRequested() => _allowDrivingInput && UseKeyboard && WasPressed(Key.A);
    }
}
