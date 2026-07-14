using System;
using Key = TopSpeed.Input.InputKey;
using TopSpeed.Input;
using TopSpeed.Input.Devices.Controller;

namespace TopSpeed.Menu
{
    internal static class MenuInputUtil
    {
        private const int ControllerThreshold = 50;

        public static bool TryGetPressedDigit(IInputService input, out int digit)
        {
            for (var d = 1; d <= 9; d++)
            {
                if (input.WasPressed(ToDigitKey(d)) || input.WasPressed(ToNumpadKey(d)))
                {
                    digit = d;
                    return true;
                }
            }
            digit = 0;
            return false;
        }

        public static bool TryGetPressedLetter(IInputService input, out char letter)
        {
            letter = '\0';
            if (input == null || HasModifierHeld(input))
                return false;

            for (var c = 'A'; c <= 'Z'; c++)
            {
                if (!input.WasPressed(ToLetterKey(c)))
                    continue;

                letter = c;
                return true;
            }

            return false;
        }

        public static bool TryGetLetterKey(char letter, out Key key)
        {
            key = ToLetterKey(char.ToUpperInvariant(letter));
            return key != Key.Unknown;
        }

        public static bool ItemStartsWithLetter(MenuItem item, char letter)
        {
            var text = item.GetDisplayText();
            if (string.IsNullOrWhiteSpace(text))
                return false;

            for (var i = 0; i < text.Length; i++)
            {
                var ch = text[i];
                if (!char.IsLetterOrDigit(ch))
                    continue;

                return char.ToUpperInvariant(ch) == letter;
            }

            return false;
        }

        public static bool IsNearCenter(State state, bool useAxes)
        {
            if (!useAxes)
                return true;
            return Math.Abs(state.X) <= ControllerThreshold && Math.Abs(state.Y) <= ControllerThreshold;
        }

        public static bool WasControllerUpPressed(State current, State previous, bool useAxes)
        {
            var currentUp = (useAxes && current.Y < -ControllerThreshold) || current.Pov1;
            var previousUp = (useAxes && previous.Y < -ControllerThreshold) || previous.Pov1;
            return currentUp && !previousUp;
        }

        public static bool WasControllerDownPressed(State current, State previous, bool useAxes)
        {
            var currentDown = (useAxes && current.Y > ControllerThreshold) || current.Pov3;
            var previousDown = (useAxes && previous.Y > ControllerThreshold) || previous.Pov3;
            return currentDown && !previousDown;
        }

        public static bool WasControllerActivatePressed(State current, State previous, bool useAxes)
        {
            var currentRight = (useAxes && current.X > ControllerThreshold) || current.Pov2;
            var previousRight = (useAxes && previous.X > ControllerThreshold) || previous.Pov2;
            if (currentRight && !previousRight)
                return true;
            return current.B1 && !previous.B1;
        }

        public static bool WasControllerBackPressed(State current, State previous, bool useAxes)
        {
            var currentLeft = (useAxes && current.X < -ControllerThreshold) || current.Pov4;
            var previousLeft = (useAxes && previous.X < -ControllerThreshold) || previous.Pov4;
            return currentLeft && !previousLeft;
        }

        private static Key ToDigitKey(int digit)
        {
            return digit switch
            {
                1 => Key.D1,
                2 => Key.D2,
                3 => Key.D3,
                4 => Key.D4,
                5 => Key.D5,
                6 => Key.D6,
                7 => Key.D7,
                8 => Key.D8,
                9 => Key.D9,
                _ => Key.Unknown
            };
        }

        private static Key ToNumpadKey(int digit)
        {
            return digit switch
            {
                1 => Key.NumberPad1,
                2 => Key.NumberPad2,
                3 => Key.NumberPad3,
                4 => Key.NumberPad4,
                5 => Key.NumberPad5,
                6 => Key.NumberPad6,
                7 => Key.NumberPad7,
                8 => Key.NumberPad8,
                9 => Key.NumberPad9,
                _ => Key.Unknown
            };
        }

        private static Key ToLetterKey(char letter)
        {
            return letter switch
            {
                'A' => Key.A,
                'B' => Key.B,
                'C' => Key.C,
                'D' => Key.D,
                'E' => Key.E,
                'F' => Key.F,
                'G' => Key.G,
                'H' => Key.H,
                'I' => Key.I,
                'J' => Key.J,
                'K' => Key.K,
                'L' => Key.L,
                'M' => Key.M,
                'N' => Key.N,
                'O' => Key.O,
                'P' => Key.P,
                'Q' => Key.Q,
                'R' => Key.R,
                'S' => Key.S,
                'T' => Key.T,
                'U' => Key.U,
                'V' => Key.V,
                'W' => Key.W,
                'X' => Key.X,
                'Y' => Key.Y,
                'Z' => Key.Z,
                _ => Key.Unknown
            };
        }

        internal static bool HasModifierHeld(IInputService input)
        {
            return input.IsDown(Key.LeftControl)
                || input.IsDown(Key.RightControl)
                || input.IsDown(Key.LeftShift)
                || input.IsDown(Key.RightShift)
                || input.IsDown(Key.LeftAlt)
                || input.IsDown(Key.RightAlt);
        }
    }
}




