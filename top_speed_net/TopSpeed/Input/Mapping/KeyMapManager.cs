using System.Collections.Generic;
using Key = TopSpeed.Input.InputKey;
using TopSpeed.Input.Devices.Controller;
using TopSpeed.Localization;

namespace TopSpeed.Input
{
    internal sealed class KeyMapManager
    {
        private readonly DriveInput _input;

        public KeyMapManager(DriveInput input)
        {
            _input = input;
        }

        public IReadOnlyList<DriveIntentDefinition> Actions => _input.GetIntentDefinitions();

        public string GetLabel(DriveIntent action)
        {
            return _input.GetIntentLabel(action);
        }

        public Key GetKey(DriveIntent action)
        {
            return _input.GetKeyMapping(action);
        }

        public AxisOrButton GetAxis(DriveIntent action)
        {
            return _input.GetAxisMapping(action);
        }

        public void ApplyKeyMapping(DriveIntent action, Key key)
        {
            _input.ApplyKeyMapping(action, key);
        }

        public void ApplyAxisMapping(DriveIntent action, AxisOrButton axis)
        {
            _input.ApplyAxisMapping(action, axis);
        }

        public bool IsKeyInUse(Key key, DriveIntent ignore)
        {
            foreach (var action in Actions)
            {
                if (action.Action == ignore)
                    continue;
                if (ModifierKeys.Conflicts(GetKey(action.Action), key))
                    return true;
            }
            return false;
        }

        public bool IsAxisInUse(AxisOrButton axis, DriveIntent ignore)
        {
            foreach (var action in Actions)
            {
                if (action.Action == ignore)
                    continue;
                if (GetAxis(action.Action) == axis)
                    return true;
            }
            return false;
        }

        public static bool IsReservedKey(Key key)
        {
            if (key == Key.F11)
                return true;
            if (key >= Key.D1 && key <= Key.D0)
                return true;
            return key == Key.LeftAlt || key == Key.BothAlt;
        }

        public static string FormatKey(Key key)
        {
            return InputDisplayText.Key(key);
        }

        public static string FormatAxis(AxisOrButton axis)
        {
            return InputDisplayText.Axis(axis);
        }

        public static string FormatAxis(AxisOrButton axis, ControllerDisplayProfile profile)
        {
            return InputDisplayText.Axis(axis, profile);
        }

        public string GetMappingInstruction(bool keyboard, DriveIntent action)
        {
            var label = LocalizationService.Translate(GetLabel(action)).ToLowerInvariant();
            return keyboard
                ? LocalizationService.Format(LocalizationService.Mark("Press the new key for {0}."), label)
                : LocalizationService.Format(LocalizationService.Mark("Move or press the controller control for {0}."), label);
        }
    }
}



