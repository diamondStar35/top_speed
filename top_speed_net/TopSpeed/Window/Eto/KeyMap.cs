using System;
using Eto.Forms;
using TopSpeed.Input;

namespace TopSpeed.Windowing.Eto
{
    internal static class EtoKeyMap
    {
        public static bool TryMap(Keys keyData, out InputKey key)
        {
            var keyPart = keyData & Keys.KeyMask;

            switch (keyPart)
            {
                case Keys.A: key = InputKey.A; return true;
                case Keys.B: key = InputKey.B; return true;
                case Keys.C: key = InputKey.C; return true;
                case Keys.D: key = InputKey.D; return true;
                case Keys.E: key = InputKey.E; return true;
                case Keys.F: key = InputKey.F; return true;
                case Keys.G: key = InputKey.G; return true;
                case Keys.H: key = InputKey.H; return true;
                case Keys.I: key = InputKey.I; return true;
                case Keys.J: key = InputKey.J; return true;
                case Keys.K: key = InputKey.K; return true;
                case Keys.L: key = InputKey.L; return true;
                case Keys.M: key = InputKey.M; return true;
                case Keys.N: key = InputKey.N; return true;
                case Keys.O: key = InputKey.O; return true;
                case Keys.P: key = InputKey.P; return true;
                case Keys.Q: key = InputKey.Q; return true;
                case Keys.R: key = InputKey.R; return true;
                case Keys.S: key = InputKey.S; return true;
                case Keys.T: key = InputKey.T; return true;
                case Keys.U: key = InputKey.U; return true;
                case Keys.V: key = InputKey.V; return true;
                case Keys.W: key = InputKey.W; return true;
                case Keys.X: key = InputKey.X; return true;
                case Keys.Y: key = InputKey.Y; return true;
                case Keys.Z: key = InputKey.Z; return true;
                case Keys.D0: key = InputKey.D0; return true;
                case Keys.D1: key = InputKey.D1; return true;
                case Keys.D2: key = InputKey.D2; return true;
                case Keys.D3: key = InputKey.D3; return true;
                case Keys.D4: key = InputKey.D4; return true;
                case Keys.D5: key = InputKey.D5; return true;
                case Keys.D6: key = InputKey.D6; return true;
                case Keys.D7: key = InputKey.D7; return true;
                case Keys.D8: key = InputKey.D8; return true;
                case Keys.D9: key = InputKey.D9; return true;
                case Keys.F1: key = InputKey.F1; return true;
                case Keys.F2: key = InputKey.F2; return true;
                case Keys.F3: key = InputKey.F3; return true;
                case Keys.F4: key = InputKey.F4; return true;
                case Keys.F5: key = InputKey.F5; return true;
                case Keys.F6: key = InputKey.F6; return true;
                case Keys.F7: key = InputKey.F7; return true;
                case Keys.F8: key = InputKey.F8; return true;
                case Keys.F9: key = InputKey.F9; return true;
                case Keys.F10: key = InputKey.F10; return true;
                case Keys.F11: key = InputKey.F11; return true;
                case Keys.F12: key = InputKey.F12; return true;
                case Keys.F13: key = InputKey.F13; return true;
                case Keys.F14: key = InputKey.F14; return true;
                case Keys.F15: key = InputKey.F15; return true;
                case Keys.None:
                {
                    var modifiers = keyData & Keys.ModifierMask;

                    // Shift plus a key often produces a keysym of its own on X11 - ISO_Left_Tab for
                    // Tab, exclam for 1, asciitilde for the backtick - and Eto's GTK layer translates
                    // none of them, so the press arrives carrying modifier bits and no key at all.
                    // Every one of them looks identical here, so which key it was cannot be recovered.
                    //
                    // Guessing a modifier from the bits is worse than admitting that. Every real
                    // modifier press on GTK arrives with its own key part, so nothing here is a
                    // modifier going down; inferring one invented a press and then a release while the
                    // key was still held, which reported Left Shift as let go mid-hold and dropped the
                    // clutch. Report nothing instead: an unidentifiable key does nothing, rather than
                    // something wrong.
                    if (OperatingSystem.IsLinux())
                    {
                        key = InputKey.Unknown;
                        return false;
                    }

                    // macOS may genuinely deliver bare modifier events; no evidence either way, so its
                    // behaviour is left exactly as it was.
                    if (modifiers == Keys.Shift)
                    {
                        key = InputKey.LeftShift;
                        return true;
                    }

                    if (modifiers == Keys.Control)
                    {
                        key = InputKey.LeftControl;
                        return true;
                    }

                    if (modifiers == Keys.Alt)
                    {
                        key = InputKey.LeftAlt;
                        return true;
                    }

                    key = InputKey.Unknown;
                    return false;
                }
                case Keys.Escape:
                    key = InputKey.Escape;
                    return true;
                case Keys.Tab:
                    key = InputKey.Tab;
                    return true;
                case Keys.Backspace:
                    key = InputKey.Back;
                    return true;
                case Keys.Enter:
                    key = InputKey.Return;
                    return true;
                case Keys.Space:
                    key = InputKey.Space;
                    return true;
                case Keys.CapsLock:
                    key = InputKey.Capital;
                    return true;
                case Keys.ScrollLock:
                    key = InputKey.ScrollLock;
                    return true;
                case Keys.PrintScreen:
                    key = InputKey.PrintScreen;
                    return true;
                case Keys.NumberLock:
                    key = InputKey.NumberLock;
                    return true;
                case Keys.Pause:
                    key = InputKey.Pause;
                    return true;
                case Keys.Insert:
                    key = InputKey.Insert;
                    return true;
                case Keys.Delete:
                    key = InputKey.Delete;
                    return true;
                case Keys.Home:
                    key = InputKey.Home;
                    return true;
                case Keys.End:
                    key = InputKey.End;
                    return true;
                case Keys.PageUp:
                    key = InputKey.PageUp;
                    return true;
                case Keys.PageDown:
                    key = InputKey.PageDown;
                    return true;
                case Keys.Up:
                    key = InputKey.Up;
                    return true;
                case Keys.Down:
                    key = InputKey.Down;
                    return true;
                case Keys.Left:
                    key = InputKey.Left;
                    return true;
                case Keys.Right:
                    key = InputKey.Right;
                    return true;
                case Keys.Keypad0:
                    key = InputKey.NumberPad0;
                    return true;
                case Keys.Keypad1:
                    key = InputKey.NumberPad1;
                    return true;
                case Keys.Keypad2:
                    key = InputKey.NumberPad2;
                    return true;
                case Keys.Keypad3:
                    key = InputKey.NumberPad3;
                    return true;
                case Keys.Keypad4:
                    key = InputKey.NumberPad4;
                    return true;
                case Keys.Keypad5:
                    key = InputKey.NumberPad5;
                    return true;
                case Keys.Keypad6:
                    key = InputKey.NumberPad6;
                    return true;
                case Keys.Keypad7:
                    key = InputKey.NumberPad7;
                    return true;
                case Keys.Keypad8:
                    key = InputKey.NumberPad8;
                    return true;
                case Keys.Keypad9:
                    key = InputKey.NumberPad9;
                    return true;
                case Keys.Equal:
                    key = InputKey.Equals;
                    return true;
                case Keys.Minus:
                    key = InputKey.Minus;
                    return true;
                case Keys.Grave:
                    key = InputKey.Grave;
                    return true;
                case Keys.LeftBracket:
                    key = InputKey.LeftBracket;
                    return true;
                case Keys.RightBracket:
                    key = InputKey.RightBracket;
                    return true;
                case Keys.Semicolon:
                    key = InputKey.Semicolon;
                    return true;
                case Keys.Quote:
                    key = InputKey.Apostrophe;
                    return true;
                case Keys.Comma:
                    key = InputKey.Comma;
                    return true;
                case Keys.Period:
                    key = InputKey.Period;
                    return true;
                case Keys.Slash:
                    key = InputKey.Slash;
                    return true;
                case Keys.Backslash:
                    key = InputKey.Backslash;
                    return true;
                case Keys.Add:
                    key = InputKey.Add;
                    return true;
                case Keys.Subtract:
                    key = InputKey.Subtract;
                    return true;
                case Keys.Multiply:
                    key = InputKey.Multiply;
                    return true;
                case Keys.Divide:
                    key = InputKey.Divide;
                    return true;
                case Keys.Decimal:
                    key = InputKey.Decimal;
                    return true;
                case Keys.LeftShift:
                    key = InputKey.LeftShift;
                    return true;
                case Keys.RightShift:
                    key = InputKey.RightShift;
                    return true;
                case Keys.LeftControl:
                    key = InputKey.LeftControl;
                    return true;
                case Keys.RightControl:
                    key = InputKey.RightControl;
                    return true;
                case Keys.LeftAlt:
                    key = InputKey.LeftAlt;
                    return true;
                case Keys.RightAlt:
                    key = InputKey.RightAlt;
                    return true;
                case Keys.LeftApplication:
                    key = InputKey.LeftWindowsKey;
                    return true;
                case Keys.RightApplication:
                    key = InputKey.RightWindowsKey;
                    return true;
                case Keys.ContextMenu:
                    key = InputKey.Applications;
                    return true;
                default:
                    key = InputKey.Unknown;
                    return false;
            }
        }

        public static Keys ExtractModifierMask(Keys keyData)
        {
            return keyData & Keys.ModifierMask;
        }

        public static bool MatchesEnter(Keys keyData)
        {
            var keyPart = keyData & Keys.KeyMask;
            return keyPart == Keys.Enter;
        }

        public static bool MatchesEscape(Keys keyData)
        {
            var keyPart = keyData & Keys.KeyMask;
            return keyPart == Keys.Escape;
        }
    }
}
