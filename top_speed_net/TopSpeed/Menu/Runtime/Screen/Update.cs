using TopSpeed.Input;

namespace TopSpeed.Menu
{
    internal sealed partial class MenuScreen
    {
        public MenuUpdateResult Update(IInputService input, in MenuLetterPress letterPress)
        {
            if (_items.Count == 0)
                return MenuUpdateResult.None;

            if (!TryHandlePendingTitle(input))
                return MenuUpdateResult.None;

            var state = CaptureInputState(input);

            if (input.ShouldIgnoreMenuBack())
                return MenuUpdateResult.None;

            if (TryHandleLetterNavigation(in letterPress))
                return MenuUpdateResult.None;

            if (TryHandleNumberActivation(input, out var numberResult))
                return numberResult;

            if (state.NextScreen && SwitchToNextScreen())
                return MenuUpdateResult.None;

            if (state.PreviousScreen && SwitchToPreviousScreen())
                return MenuUpdateResult.None;

            if (TryHandleHeldInputGate(input, state, out var heldResult))
                return heldResult;

            if (TryHandleItemAdjustment(state))
                return MenuUpdateResult.None;

            if (TryHandleActionBrowse(state))
                return MenuUpdateResult.None;

            HandleNavigation(state);
            HandleMusicAdjustment(state);

            if (state.Activate)
                return HandleActivation();

            if (state.Back)
            {
                input.LatchMenuBack();
                return MenuUpdateResult.Back;
            }

            if (_index == NoSelection && _autoFocusPending)
            {
                if (_waitForTitleSpeechBeforeAutoFocus)
                {
                    if (input.IsAnyInputHeld())
                    {
                        _speech.Purge();
                        _waitForTitleSpeechBeforeAutoFocus = false;
                    }
                    else if (_speech.IsSpeaking())
                    {
                        return MenuUpdateResult.None;
                    }
                }

                _waitForTitleSpeechBeforeAutoFocus = false;
                FocusFirstItem();
                ClearAutoFocusPending();
            }

            return MenuUpdateResult.None;
        }
    }
}



