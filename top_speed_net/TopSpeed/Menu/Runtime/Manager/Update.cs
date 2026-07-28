using System;
using Key = TopSpeed.Input.InputKey;
using TopSpeed.Input;
using TopSpeed.Shortcuts;

namespace TopSpeed.Menu
{
    internal sealed partial class MenuManager
    {
        // Optional caller-supplied test for keys that should not trigger first-letter navigation
        // because they are already doing a live job elsewhere (e.g. drive status/horn keys while the
        // pit-stop menu overlay is open). Set while such a dialog is active and cleared afterwards.
        private Func<Key, bool>? _externalLetterReservation;

        public void SetLetterNavigationReservation(Func<Key, bool>? isReservedKey)
        {
            _externalLetterReservation = isReservedKey;
        }

        public MenuAction Update(IInputService input)
        {
            if (_stack.Count == 0)
                return MenuAction.None;

            var current = _stack.Peek();
            var ctrlHeld = input.IsDown(Key.LeftControl) || input.IsDown(Key.RightControl);
            var shiftHeld = input.IsDown(Key.LeftShift) || input.IsDown(Key.RightShift);
            var altHeld = input.IsDown(Key.LeftAlt) || input.IsDown(Key.RightAlt);
            var plainSpacePressed = !ctrlHeld && !shiftHeld && !altHeld && input.WasPressed(Key.Space);
            var helpRequested = plainSpacePressed || input.WasGesturePressed(GestureIntent.LongPress);
            if (helpRequested && current.TrySpeakCurrentHintOnDemand())
                return MenuAction.None;

            var context = new ShortcutContext(current.Id, current.ActiveViewId);
            if (TryHandleShortcut(input, current, in context))
                return MenuAction.None;

            var letterPress = ResolveLetterPress(input, in context);
            var result = current.Update(input, in letterPress);

            if (result.BackRequested)
                return HandleClose(current, MenuCloseSource.Shortcut, CloseKind.Back);

            if (result.ActivatedItem == null)
                return MenuAction.None;

            var item = result.ActivatedItem;
            var stackCount = _stack.Count;
            var announcement = item.ActivateAndGetAnnouncement();
            if (announcement != null)
                current.CancelPendingHint();
            var stackChanged = _stack.Count != stackCount || _stack.Peek() != current;
            if (item.Action == MenuAction.Back)
                return HandleClose(current, MenuCloseSource.Item, CloseKind.Back);
            if (item.IsCloseItem)
                return HandleClose(current, MenuCloseSource.Item, CloseKind.Close);
            if (!string.IsNullOrWhiteSpace(item.NextMenuId))
            {
                Push(item.NextMenuId!);
                return MenuAction.None;
            }

            if (!stackChanged && !item.SuppressPostActivateAnnouncement && !string.IsNullOrWhiteSpace(announcement))
                _speech.Speak(announcement!);
            return item.Action;
        }

        private bool TryHandleShortcut(IInputService input, MenuScreen current, in ShortcutContext context)
        {
            if (!_shortcutCatalog.TryResolveTriggeredAction(input, in context, out var action))
                return false;

            current.CancelPendingHint();
            action.Trigger();
            return true;
        }

        private MenuLetterPress ResolveLetterPress(IInputService input, in ShortcutContext context)
        {
            if (!MenuInputUtil.TryGetPressedLetter(input, out var letter))
                return MenuLetterPress.None;

            var reserved = MenuInputUtil.TryGetLetterKey(letter, out var key)
                && (_shortcutCatalog.HasUnmodifiedBindingForKeyInContext(key, in context)
                    || (_externalLetterReservation?.Invoke(key) ?? false));
            return new MenuLetterPress(letter, reserved);
        }

        private MenuAction HandleClose(MenuScreen current, MenuCloseSource source, CloseKind kind)
        {
            var e = new CloseEvent(current.Id, current.ActiveViewId, source, kind);
            if (current.TryHandleClose(e))
                return MenuAction.None;

            if (kind == CloseKind.Close)
                return MenuAction.None;

            if (_stack.Count > 1)
            {
                _stack.Peek().CancelPendingHint();
                _stack.Pop();
                _stack.Peek().QueueTitleAnnouncement();
                return MenuAction.None;
            }

            return MenuAction.Exit;
        }

        private MenuScreen GetScreen(string id)
        {
            if (!_screens.TryGetValue(id, out var screen))
                throw new InvalidOperationException($"Menu not registered: {id}");
            return screen;
        }
    }
}




