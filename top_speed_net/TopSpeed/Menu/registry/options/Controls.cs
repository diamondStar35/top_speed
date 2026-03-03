using System.Collections.Generic;
using TopSpeed.Input;
using TopSpeed.Protocol;

namespace TopSpeed.Menu
{
    internal sealed partial class MenuRegistry
    {
        private MenuScreen BuildOptionsControlsMenu()
        {
            var items = new List<MenuItem>
            {
                new MenuItem(() => $"Select device: {DeviceLabel(_settings.DeviceMode)}", MenuAction.None, nextMenuId: "options_controls_device"),
                new CheckBox(
                    "Force feedback",
                    () => _settings.ForceFeedback,
                    value => _settingsActions.UpdateSetting(() => _settings.ForceFeedback = value),
                    hint: "Enables force feedback or vibration if your controller supports it. Press ENTER to toggle."),
                new RadioButton(
                    "Progressive (Pseudo-Analog) keyboard input",
                    new[] { "off", "0.25s", "0.50s", "0.75s", "1.00s" },
                    () => (int)_settings.KeyboardProgressiveRate,
                    value => _settingsActions.UpdateSetting(() => _settings.KeyboardProgressiveRate = (KeyboardProgressiveRate)value),
                    hint: "When off, car controls immediately jump to 100 percent as soon as keys are pressed. The further right this is, the slower the car's control chases the state of your keyboard, allowing to feather throttle, brakes, etc. The opposite key can be used for immediate reset, so steer left will disable a progressively releasing steer right for example."),
                new MenuItem("Map keyboard keys", MenuAction.None, nextMenuId: "options_controls_keyboard"),
                new MenuItem("Map joystick keys", MenuAction.None, nextMenuId: "options_controls_joystick"),
                BackItem()
            };
            return _menu.CreateMenu("options_controls", items);
        }

        private MenuScreen BuildOptionsControlsDeviceMenu()
        {
            var items = new List<MenuItem>
            {
                new MenuItem("Keyboard", MenuAction.Back, onActivate: () => _settingsActions.SetDevice(InputDeviceMode.Keyboard)),
                new MenuItem("Joystick", MenuAction.Back, onActivate: () => _settingsActions.SetDevice(InputDeviceMode.Joystick)),
                new MenuItem("Both", MenuAction.Back, onActivate: () => _settingsActions.SetDevice(InputDeviceMode.Both)),
                BackItem()
            };
            return _menu.CreateMenu("options_controls_device", items, "Select input device");
        }

        private MenuScreen BuildOptionsControlsKeyboardMenu()
        {
            return _menu.CreateMenu("options_controls_keyboard", BuildMappingItems(InputMappingMode.Keyboard));
        }

        private MenuScreen BuildOptionsControlsJoystickMenu()
        {
            return _menu.CreateMenu("options_controls_joystick", BuildMappingItems(InputMappingMode.Joystick));
        }

        private List<MenuItem> BuildMappingItems(InputMappingMode mode)
        {
            var items = new List<MenuItem>();
            foreach (var action in _raceInput.KeyMap.Actions)
            {
                var definition = action;
                items.Add(new MenuItem(
                    () => $"{definition.Label}: {_mapping.FormatMappingValue(definition.Action, mode)}",
                    MenuAction.None,
                    onActivate: () => _mapping.BeginMapping(mode, definition.Action)));
            }

            items.Add(BackItem());
            return items;
        }
    }
}
