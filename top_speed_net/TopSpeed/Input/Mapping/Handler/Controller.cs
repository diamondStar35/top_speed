using TopSpeed.Input.Devices.Controller;
using TopSpeed.Localization;

namespace TopSpeed.Input
{
    internal sealed partial class InputMappingHandler
    {
        private void TryCaptureControllerMapping()
        {
            if (!_input.TryGetControllerState(out var state))
            {
                _mappingActive = false;
                _speech.Speak(LocalizationService.Mark("No controller detected."));
                return;
            }

            if (!_mappingHasPrevController)
            {
                _mappingPrevController = state;
                _mappingHasPrevController = true;
                return;
            }

            var axis = FindTriggeredAxis(state, _mappingPrevController);
            _mappingPrevController = state;
            if (axis == AxisOrButton.AxisNone)
                return;
            if (_driveInput.KeyMap.IsAxisInUse(axis, _mappingAction))
            {
                _speech.Speak(LocalizationService.Mark("That control is already in use."));
                return;
            }

            _driveInput.KeyMap.ApplyAxisMapping(_mappingAction, axis);
            _saveSettings();
            _mappingActive = false;
            var label = _driveInput.KeyMap.GetLabel(_mappingAction);
            var control = _input.TryGetControllerDisplayProfile(out var profile)
                ? KeyMapManager.FormatAxis(axis, profile)
                : KeyMapManager.FormatAxis(axis);
            _speech.Speak(LocalizationService.Format(
                LocalizationService.Mark("{0} set to {1}."),
                LocalizationService.Translate(label),
                control));
        }

        private AxisOrButton FindTriggeredAxis(State current, State previous)
        {
            for (var i = (int)AxisOrButton.AxisXNeg; i <= (int)AxisOrButton.Pov8; i++)
            {
                var axis = (AxisOrButton)i;
                if (IsAxisActive(axis, current) && !IsAxisActive(axis, previous))
                    return axis;
            }
            return AxisOrButton.AxisNone;
        }

        private bool IsAxisActive(AxisOrButton axis, State state)
        {
            var center = _settings.ControllerCenter;
            const int threshold = 50;
            switch (axis)
            {
                case AxisOrButton.AxisXNeg:
                    return state.X < center.X - threshold;
                case AxisOrButton.AxisXPos:
                    return state.X > center.X + threshold;
                case AxisOrButton.AxisYNeg:
                    return state.Y < center.Y - threshold;
                case AxisOrButton.AxisYPos:
                    return state.Y > center.Y + threshold;
                case AxisOrButton.AxisZNeg:
                    return state.Z < center.Z - threshold;
                case AxisOrButton.AxisZPos:
                    return state.Z > center.Z + threshold;
                case AxisOrButton.AxisRxNeg:
                    return state.Rx < center.Rx - threshold;
                case AxisOrButton.AxisRxPos:
                    return state.Rx > center.Rx + threshold;
                case AxisOrButton.AxisRyNeg:
                    return state.Ry < center.Ry - threshold;
                case AxisOrButton.AxisRyPos:
                    return state.Ry > center.Ry + threshold;
                case AxisOrButton.AxisRzNeg:
                    return state.Rz < center.Rz - threshold;
                case AxisOrButton.AxisRzPos:
                    return state.Rz > center.Rz + threshold;
                case AxisOrButton.AxisSlider1Neg:
                    return state.Slider1 < center.Slider1 - threshold;
                case AxisOrButton.AxisSlider1Pos:
                    return state.Slider1 > center.Slider1 + threshold;
                case AxisOrButton.AxisSlider2Neg:
                    return state.Slider2 < center.Slider2 - threshold;
                case AxisOrButton.AxisSlider2Pos:
                    return state.Slider2 > center.Slider2 + threshold;
                case AxisOrButton.Button1:
                    return state.B1;
                case AxisOrButton.Button2:
                    return state.B2;
                case AxisOrButton.Button3:
                    return state.B3;
                case AxisOrButton.Button4:
                    return state.B4;
                case AxisOrButton.Button5:
                    return state.B5;
                case AxisOrButton.Button6:
                    return state.B6;
                case AxisOrButton.Button7:
                    return state.B7;
                case AxisOrButton.Button8:
                    return state.B8;
                case AxisOrButton.Button9:
                    return state.B9;
                case AxisOrButton.Button10:
                    return state.B10;
                case AxisOrButton.Button11:
                    return state.B11;
                case AxisOrButton.Button12:
                    return state.B12;
                case AxisOrButton.Button13:
                    return state.B13;
                case AxisOrButton.Button14:
                    return state.B14;
                case AxisOrButton.Button15:
                    return state.B15;
                case AxisOrButton.Button16:
                    return state.B16;
                case AxisOrButton.Pov1:
                    return state.Pov1;
                case AxisOrButton.Pov2:
                    return state.Pov2;
                case AxisOrButton.Pov3:
                    return state.Pov3;
                case AxisOrButton.Pov4:
                    return state.Pov4;
                case AxisOrButton.Pov5:
                    return state.Pov5;
                case AxisOrButton.Pov6:
                    return state.Pov6;
                case AxisOrButton.Pov7:
                    return state.Pov7;
                case AxisOrButton.Pov8:
                    return state.Pov8;
                default:
                    return false;
            }
        }
    }
}


