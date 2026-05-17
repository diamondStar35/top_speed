using System.Collections.Generic;
using Key = TopSpeed.Input.InputKey;
using TopSpeed.Localization;

namespace TopSpeed.Input
{
    internal sealed partial class InputMappingHandler
    {
        private void TryCaptureKeyboardMapping()
        {
            for (var i = 1; i < 256; i++)
            {
                var key = (Key)i;
                if (!_input.WasPressed(key))
                    continue;
                if (ModifierKeys.TryGetGroup(key, out var group))
                {
                    ShowModifierKeyChoice(group);
                    return;
                }

                ApplyCapturedKeyboardKey(key);
                return;
            }
        }

        private void ShowModifierKeyChoice(ModifierKeyGroup group)
        {
            _mappingActive = false;

            var modifierName = ModifierKeys.GetDisplayName(group);
            var options = new Dictionary<int, string>
            {
                [(int)ModifierKeySelection.Left] = LocalizationService.Format(LocalizationService.Mark("Left {0}"), modifierName),
                [(int)ModifierKeySelection.Right] = LocalizationService.Format(LocalizationService.Mark("Right {0}"), modifierName),
                [(int)ModifierKeySelection.Both] = LocalizationService.Mark("Use both modifier keys")
            };

            _showChoiceDialog(
                LocalizationService.Mark("Choose modifier key"),
                LocalizationService.Format(LocalizationService.Mark("You pressed {0}. Choose a modifier option."), modifierName),
                options,
                true,
                LocalizationService.Mark("Cancel"),
                (isCanceled, choiceId) => HandleModifierKeyChoiceResult(group, isCanceled, choiceId));
        }

        private void HandleModifierKeyChoiceResult(ModifierKeyGroup group, bool isCanceled, int choiceId)
        {
            if (isCanceled)
            {
                _speech.Speak(LocalizationService.Mark("Mapping cancelled."));
                return;
            }

            if (!System.Enum.IsDefined(typeof(ModifierKeySelection), choiceId))
            {
                _speech.Speak(LocalizationService.Mark("Mapping cancelled."));
                return;
            }

            var selection = (ModifierKeySelection)choiceId;
            var key = ModifierKeys.GetKey(group, selection);
            if (ApplyCapturedKeyboardKey(key))
                return;

            _mappingActive = true;
        }

        private bool ApplyCapturedKeyboardKey(Key key)
        {
            if (KeyMapManager.IsReservedKey(key))
            {
                _speech.Speak(LocalizationService.Mark("That key is reserved."));
                return false;
            }
            if (_driveInput.KeyMap.IsKeyInUse(key, _mappingAction))
            {
                _speech.Speak(LocalizationService.Mark("That key is already in use."));
                return false;
            }

            _driveInput.KeyMap.ApplyKeyMapping(_mappingAction, key);
            _saveSettings();
            _mappingActive = false;
            var label = _driveInput.KeyMap.GetLabel(_mappingAction);
            _speech.Speak(LocalizationService.Format(
                LocalizationService.Mark("{0} set to {1}."),
                LocalizationService.Translate(label),
                KeyMapManager.FormatKey(key)));
            return true;
        }
    }
}



