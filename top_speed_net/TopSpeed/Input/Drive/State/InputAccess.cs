using System.Collections.Generic;
using Key = TopSpeed.Input.InputKey;
using TopSpeed.Input.Devices.Controller;

namespace TopSpeed.Input
{
    internal sealed partial class DriveInput
    {
        public void SetPanelInputAccess(bool allowDrivingInput, bool allowAuxiliaryInput)
        {
            _allowDrivingInput = allowDrivingInput;
            _allowAuxiliaryInput = allowAuxiliaryInput;
        }

        public void SetOverlayInputBlocked(bool blocked)
        {
            _overlayInputBlocked = blocked;
        }

        // True while a menu or dialog is capturing navigation during a race. When set, the keys and
        // controller inputs the menu navigates with are trapped for the menu (see IsInputTrappedByMenu),
        // so a drive intent mapped onto e.g. an arrow key does not also fire alongside the navigation.
        public void SetMenuNavigationActive(bool active)
        {
            _menuNavigationActive = active;
        }

        // The menu's navigation inputs, supplied once from the menu layer (via the game layer) so the
        // input code has a single source of truth for what the menu owns while it is open.
        public void SetMenuNavigationInputs(IReadOnlyCollection<Key> keys, IReadOnlyCollection<AxisOrButton> controllerInputs)
        {
            _menuNavigationKeys.Clear();
            if (keys != null)
            {
                foreach (var key in keys)
                    _menuNavigationKeys.Add(key);
            }

            _menuNavigationControllerInputs.Clear();
            if (controllerInputs != null)
            {
                foreach (var input in controllerInputs)
                    _menuNavigationControllerInputs.Add(input);
            }
        }

        public void SetPausedHornInputAllowed(bool allowed)
        {
            _pausedHornInputAllowed = allowed;
        }

        private bool IsCtrlDown()
        {
            return _lastState.IsDown(Key.LeftControl) || _lastState.IsDown(Key.RightControl);
        }

        private bool IsShiftDown()
        {
            return _lastState.IsDown(Key.LeftShift) || _lastState.IsDown(Key.RightShift);
        }
    }
}



