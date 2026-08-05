using System;
using MonoMac.AppKit;
using MonoMac.Foundation;
using TopSpeed.Input;

namespace TopSpeed.Windowing.Eto
{
    // Cocoa dispatches Control-modified key presses through the key-equivalent chain
    // (window tabbing, key-view-loop focus navigation) before ordinary key delivery,
    // which consumes Control+Tab so the game window's keyDown never sees it — plain Tab
    // arrives fine, Control+Tab does not. A local NSEvent monitor observes every key
    // event before the app dispatches it, so Control+Tab (and Control+Shift+Tab) can be
    // translated into game key events here and the original swallowed before Cocoa gets
    // a chance to eat it.
    //
    // This file is compiled only for osx runtime identifiers (see TopSpeed.csproj): it
    // needs MonoMac, which only Eto.Platform.Mac64 provides. WindowHost locates it by
    // name via reflection so shared code never references it directly.
    internal static class MacControlTabInterceptor
    {
        private const ushort TabKeyCode = 48;

        private static NSObject? _monitor;
        private static LocalEventHandler? _handler;

        public static void Install(
            Func<IntPtr> gameWindowHandle,
            Func<bool> allowIntercept,
            Action<InputKey> onKeyDown,
            Action<InputKey> onKeyUp)
        {
            if (_monitor != null)
                return;

            // Kept in a static so the delegate outlives this call; the monitor holds it
            // for the lifetime of the process.
            _handler = theEvent =>
            {
                if (theEvent.KeyCode != TabKeyCode)
                    return theEvent;
                if ((theEvent.ModifierFlags & NSEventModifierMask.ControlKeyMask) == 0)
                    return theEvent;

                // Only intercept events destined for the game window; Control+Tab inside
                // another window (e.g. a file dialog) keeps its native behavior.
                var handle = gameWindowHandle();
                var targetWindow = theEvent.Window;
                if (handle == IntPtr.Zero || targetWindow == null || targetWindow.Handle != handle)
                    return theEvent;
                if (!allowIntercept())
                    return theEvent;

                // What goes into the game is a bare InputKey.Tab: the Control modifier is not carried
                // along, and the input layer reads Control's state separately from the keyboard. So one
                // press feeds two consumers at once — the panel switch, which checks IsCtrlDown, and
                // whatever is bound to plain Tab, which ignores modifiers entirely. That is why the
                // doubled announcement is a macOS-only fault: on every other platform Control+Tab
                // reaches the window as one modified key event, while here Cocoa eats the chord and it
                // only arrives at all by being re-injected through this monitor, stripped of its
                // modifier. See IsPanelSwitchChord in Input/Drive/Query.cs for the guard.
                if (theEvent.Type == NSEventType.KeyDown)
                    onKeyDown(InputKey.Tab);
                else if (theEvent.Type == NSEventType.KeyUp)
                    onKeyUp(InputKey.Tab);
                else
                    return theEvent;

                return null!;
            };

            _monitor = NSEvent.AddLocalMonitorForEventsMatchingMask(
                NSEventMask.KeyDown | NSEventMask.KeyUp,
                _handler);
        }
    }
}
