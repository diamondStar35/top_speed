using System.Collections.Generic;
using Key = TopSpeed.Input.InputKey;
using TopSpeed.Input;
using TopSpeed.Input.Devices.Controller;

namespace TopSpeed.Menu
{
    internal enum MenuInputAction
    {
        PreviousItem = 0,
        NextItem = 1,
        MoveHome = 2,
        MoveEnd = 3,
        MoveLeft = 4,
        MoveRight = 5,
        PageUp = 6,
        PageDown = 7,
        Activate = 8,
        Back = 9
    }

    internal readonly struct MenuInputBinding
    {
        public MenuInputBinding(MenuInputAction action, Key key)
        {
            Action = action;
            Key = key;
            Gesture = null;
        }

        public MenuInputBinding(MenuInputAction action, GestureIntent gesture)
        {
            Action = action;
            Key = null;
            Gesture = gesture;
        }

        public MenuInputAction Action { get; }
        public Key? Key { get; }
        public GestureIntent? Gesture { get; }
    }

    internal static class MenuInputBindings
    {
        private static readonly MenuInputBinding[] DefaultBindings =
        {
            new MenuInputBinding(MenuInputAction.PreviousItem, Key.Up),
            new MenuInputBinding(MenuInputAction.NextItem, Key.Down),
            new MenuInputBinding(MenuInputAction.MoveHome, Key.Home),
            new MenuInputBinding(MenuInputAction.MoveEnd, Key.End),
            new MenuInputBinding(MenuInputAction.MoveLeft, Key.Left),
            new MenuInputBinding(MenuInputAction.MoveRight, Key.Right),
            new MenuInputBinding(MenuInputAction.PageUp, Key.PageUp),
            new MenuInputBinding(MenuInputAction.PageDown, Key.PageDown),
            new MenuInputBinding(MenuInputAction.Activate, Key.Return),
            new MenuInputBinding(MenuInputAction.Activate, Key.NumberPadEnter),
            new MenuInputBinding(MenuInputAction.Back, Key.Escape)
        };

        private static readonly HashSet<GestureIntent> ReservedGestures = new HashSet<GestureIntent>
        {
            GestureIntent.SwipeLeft,
            GestureIntent.SwipeRight,
            GestureIntent.SwipeUp,
            GestureIntent.SwipeDown,
            GestureIntent.TwoFingerSwipeLeft,
            GestureIntent.TwoFingerSwipeRight,
            GestureIntent.TwoFingerSwipeUp,
            GestureIntent.TwoFingerSwipeDown,
            GestureIntent.ThreeFingerSwipeLeft,
            GestureIntent.ThreeFingerSwipeRight,
            GestureIntent.ThreeFingerSwipeUp,
            GestureIntent.ThreeFingerSwipeDown,
            GestureIntent.LongPress
        };

        // The keyboard keys that operate menus and dialogs, derived from the bindings above so the
        // two can never drift. DriveInput (wired in via the game layer) traps these while a menu is
        // open during a race, so a drive intent mapped onto e.g. an arrow key drives only the menu.
        public static IReadOnlyCollection<Key> NavigationKeys { get; } = BuildNavigationKeys();

        // The controller inputs that operate menus, mirroring the d-pad / left-stick / Button 1
        // logic in MenuInputUtil (WasControllerUp/Down/Activate/BackPressed). Kept here alongside the
        // keyboard set and expressed as AxisOrButton so DriveInput can trap them the same way; update
        // this in lockstep with MenuInputUtil if the controller navigation controls change.
        public static IReadOnlyCollection<AxisOrButton> NavigationControllerInputs { get; } = new HashSet<AxisOrButton>
        {
            AxisOrButton.Pov1,
            AxisOrButton.Pov2,
            AxisOrButton.Pov3,
            AxisOrButton.Pov4,
            AxisOrButton.AxisXNeg,
            AxisOrButton.AxisXPos,
            AxisOrButton.AxisYNeg,
            AxisOrButton.AxisYPos,
            AxisOrButton.Button1
        };

        private static HashSet<Key> BuildNavigationKeys()
        {
            var keys = new HashSet<Key>();
            for (var i = 0; i < DefaultBindings.Length; i++)
            {
                var binding = DefaultBindings[i];
                if (binding.Key.HasValue)
                    keys.Add(binding.Key.Value);
            }
            return keys;
        }

        public static bool IsPressed(IInputService input, MenuInputAction action)
        {
            var modifierHeld = MenuInputUtil.HasModifierHeld(input);
            for (var i = 0; i < DefaultBindings.Length; i++)
            {
                var binding = DefaultBindings[i];
                if (binding.Action != action)
                    continue;

                if (binding.Key.HasValue && !modifierHeld && input.WasPressed(binding.Key.Value))
                    return true;
                if (binding.Gesture.HasValue && input.WasGesturePressed(binding.Gesture.Value))
                    return true;
            }

            return false;
        }

        public static bool IsReservedGesture(GestureIntent intent)
        {
            return intent != GestureIntent.Unknown && ReservedGestures.Contains(intent);
        }
    }
}
