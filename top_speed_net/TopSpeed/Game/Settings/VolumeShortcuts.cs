using TopSpeed.Input;
using TopSpeed.Localization;
using TopSpeed.Shortcuts;
using TopSpeed.Core.Multiplayer;

namespace TopSpeed.Game
{
    internal sealed partial class Game
    {
        private const string GlobalVolumeNextCategoryShortcutActionId = "global_volume_next_category";
        private const string GlobalVolumePreviousCategoryShortcutActionId = "global_volume_previous_category";
        private const string GlobalVolumeDecreaseStepShortcutActionId = "global_volume_decrease_step";
        private const string GlobalVolumeIncreaseStepShortcutActionId = "global_volume_increase_step";
        private const string GlobalVolumeDecreaseLargeStepShortcutActionId = "global_volume_decrease_large_step";
        private const string GlobalVolumeIncreaseLargeStepShortcutActionId = "global_volume_increase_large_step";

        private static readonly string[] GlobalShortcutActionIds =
        {
            GlobalVolumeNextCategoryShortcutActionId,
            GlobalVolumePreviousCategoryShortcutActionId,
            GlobalVolumeDecreaseStepShortcutActionId,
            GlobalVolumeIncreaseStepShortcutActionId,
            GlobalVolumeDecreaseLargeStepShortcutActionId,
            GlobalVolumeIncreaseLargeStepShortcutActionId
        };

        private static readonly AudioVolumeCategory[] GlobalVolumeShortcutCategories =
        {
            AudioVolumeCategory.Master,
            AudioVolumeCategory.PlayerVehicleEngine,
            AudioVolumeCategory.PlayerVehicleEvents,
            AudioVolumeCategory.OtherVehicleEngine,
            AudioVolumeCategory.OtherVehicleEvents,
            AudioVolumeCategory.SurfaceLoops,
            AudioVolumeCategory.Radio,
            AudioVolumeCategory.AmbientsAndSources,
            AudioVolumeCategory.Music,
            AudioVolumeCategory.OnlineServerEvents,
            AudioVolumeCategory.Communicator
        };

        private int _globalVolumeShortcutCategoryIndex;

        private void RegisterGlobalShortcutActions()
        {
            _menu.RegisterShortcutAction(
                GlobalVolumeNextCategoryShortcutActionId,
                LocalizationService.Mark("Next volume category"),
                LocalizationService.Mark("Cycles to the next global volume category."),
                InputKey.F6,
                ShortcutModifiers.None,
                () => CycleGlobalVolumeCategory(1),
                CanHandleGlobalShortcutInput);

            _menu.RegisterShortcutAction(
                GlobalVolumePreviousCategoryShortcutActionId,
                LocalizationService.Mark("Previous volume category"),
                LocalizationService.Mark("Cycles to the previous global volume category."),
                InputKey.F6,
                new ShortcutModifiers(shift: true, control: false, alt: false),
                () => CycleGlobalVolumeCategory(-1),
                CanHandleGlobalShortcutInput);

            _menu.RegisterShortcutAction(
                GlobalVolumeDecreaseStepShortcutActionId,
                LocalizationService.Mark("Decrease volume by 1"),
                LocalizationService.Mark("Decreases the selected global volume category by one step."),
                InputKey.F7,
                ShortcutModifiers.None,
                () => AdjustSelectedGlobalVolume(-1),
                CanHandleGlobalShortcutInput);

            _menu.RegisterShortcutAction(
                GlobalVolumeIncreaseStepShortcutActionId,
                LocalizationService.Mark("Increase volume by 1"),
                LocalizationService.Mark("Increases the selected global volume category by one step."),
                InputKey.F8,
                ShortcutModifiers.None,
                () => AdjustSelectedGlobalVolume(1),
                CanHandleGlobalShortcutInput);

            _menu.RegisterShortcutAction(
                GlobalVolumeDecreaseLargeStepShortcutActionId,
                LocalizationService.Mark("Decrease volume by 10"),
                LocalizationService.Mark("Decreases the selected global volume category by ten steps."),
                InputKey.F7,
                new ShortcutModifiers(shift: true, control: false, alt: false),
                () => AdjustSelectedGlobalVolume(-10),
                CanHandleGlobalShortcutInput);

            _menu.RegisterShortcutAction(
                GlobalVolumeIncreaseLargeStepShortcutActionId,
                LocalizationService.Mark("Increase volume by 10"),
                LocalizationService.Mark("Increases the selected global volume category by ten steps."),
                InputKey.F8,
                new ShortcutModifiers(shift: true, control: false, alt: false),
                () => AdjustSelectedGlobalVolume(10),
                CanHandleGlobalShortcutInput);

            _menu.SetGlobalShortcutActions(GlobalShortcutActionIds);
        }

        private void HandleGlobalVolumeShortcuts()
        {
            if (!CanHandleGlobalShortcutInput())
                return;

            if (_menu.TryTriggerShortcutAction(GlobalVolumeNextCategoryShortcutActionId, _input))
                return;
            if (_menu.TryTriggerShortcutAction(GlobalVolumePreviousCategoryShortcutActionId, _input))
                return;
            if (_menu.TryTriggerShortcutAction(GlobalVolumeDecreaseStepShortcutActionId, _input))
                return;
            if (_menu.TryTriggerShortcutAction(GlobalVolumeIncreaseStepShortcutActionId, _input))
                return;
            if (_menu.TryTriggerShortcutAction(GlobalVolumeDecreaseLargeStepShortcutActionId, _input))
                return;
            _menu.TryTriggerShortcutAction(GlobalVolumeIncreaseLargeStepShortcutActionId, _input);
        }

        private bool IsShortcutActionHeld(string actionId)
        {
            if (string.IsNullOrWhiteSpace(actionId))
                return false;
            if (string.Equals(actionId, CommunicatorShortcutIds.PushToTalk, System.StringComparison.Ordinal) && IsTopZonePttGestureHeld())
                return true;
            if (!_menu.TryGetShortcutBinding(actionId, out var binding))
                return false;

            return IsShortcutHeld(binding);
        }

        private bool IsShortcutHeld(ShortcutBinding binding)
        {
            return binding.Key != InputKey.Unknown
                && _input.IsDown(binding.Key)
                && binding.Modifiers.MatchesInput(_input);
        }

        private bool CanHandleGlobalShortcutInput()
        {
            return !_textInputPromptActive
                && !_inputMapping.IsActive
                && !_shortcutMapping.IsActive;
        }

        private void CycleGlobalVolumeCategory(int delta)
        {
            if (GlobalVolumeShortcutCategories.Length == 0)
                return;

            _globalVolumeShortcutCategoryIndex += delta;
            while (_globalVolumeShortcutCategoryIndex < 0)
                _globalVolumeShortcutCategoryIndex += GlobalVolumeShortcutCategories.Length;
            while (_globalVolumeShortcutCategoryIndex >= GlobalVolumeShortcutCategories.Length)
                _globalVolumeShortcutCategoryIndex -= GlobalVolumeShortcutCategories.Length;

            var category = GlobalVolumeShortcutCategories[_globalVolumeShortcutCategoryIndex];
            var percent = GetVolumePercent(category);
            _speech.Speak(LocalizationService.Format(
                LocalizationService.Mark("{0}: {1} percent"),
                LocalizationService.Translate(GetVolumeCategoryName(category)),
                percent));
        }

        private void AdjustSelectedGlobalVolume(int delta)
        {
            if (GlobalVolumeShortcutCategories.Length == 0)
                return;

            var category = GlobalVolumeShortcutCategories[_globalVolumeShortcutCategoryIndex];
            var current = GetVolumePercent(category);
            var next = current + delta;
            next = AudioVolumeSettings.ClampPercent(next);
            if (next == current)
                return;
            SetVolumePercent(category, next);
            _settings.SyncMusicVolumeFromAudioCategories();
            ApplyAudioSettings();
            SaveSettings();
            _speech.Speak(LocalizationService.Format(LocalizationService.Mark("{0} percent"), next));
        }

        private int GetVolumePercent(AudioVolumeCategory category)
        {
            _settings.AudioVolumes ??= new AudioVolumeSettings();
            return _settings.AudioVolumes.GetPercent(category);
        }

        private void SetVolumePercent(AudioVolumeCategory category, int percent)
        {
            _settings.AudioVolumes ??= new AudioVolumeSettings();
            switch (category)
            {
                case AudioVolumeCategory.Master:
                    _settings.AudioVolumes.MasterPercent = percent;
                    return;
                case AudioVolumeCategory.PlayerVehicleEngine:
                    _settings.AudioVolumes.PlayerVehicleEnginePercent = percent;
                    return;
                case AudioVolumeCategory.PlayerVehicleEvents:
                    _settings.AudioVolumes.PlayerVehicleEventsPercent = percent;
                    return;
                case AudioVolumeCategory.OtherVehicleEngine:
                    _settings.AudioVolumes.OtherVehicleEnginePercent = percent;
                    return;
                case AudioVolumeCategory.OtherVehicleEvents:
                    _settings.AudioVolumes.OtherVehicleEventsPercent = percent;
                    return;
                case AudioVolumeCategory.SurfaceLoops:
                    _settings.AudioVolumes.SurfaceLoopsPercent = percent;
                    return;
                case AudioVolumeCategory.Radio:
                    _settings.AudioVolumes.RadioPercent = percent;
                    return;
                case AudioVolumeCategory.AmbientsAndSources:
                    _settings.AudioVolumes.AmbientsAndSourcesPercent = percent;
                    return;
                case AudioVolumeCategory.Music:
                    _settings.AudioVolumes.MusicPercent = percent;
                    return;
                case AudioVolumeCategory.OnlineServerEvents:
                    _settings.AudioVolumes.OnlineServerEventsPercent = percent;
                    return;
                case AudioVolumeCategory.Communicator:
                    _settings.AudioVolumes.CommunicatorPercent = percent;
                    return;
            }
        }

        private string GetVolumeCategoryName(AudioVolumeCategory category)
        {
            return category switch
            {
                AudioVolumeCategory.Master => LocalizationService.Mark("Master audio"),
                AudioVolumeCategory.PlayerVehicleEngine => LocalizationService.Mark("Vehicle engine"),
                AudioVolumeCategory.PlayerVehicleEvents => LocalizationService.Mark("Vehicle events"),
                AudioVolumeCategory.OtherVehicleEngine => LocalizationService.Mark("Other vehicles engine"),
                AudioVolumeCategory.OtherVehicleEvents => LocalizationService.Mark("Other vehicles events"),
                AudioVolumeCategory.SurfaceLoops => LocalizationService.Mark("Surface loops"),
                AudioVolumeCategory.Radio => LocalizationService.Mark("Radio"),
                AudioVolumeCategory.AmbientsAndSources => LocalizationService.Mark("Ambients and sound sources"),
                AudioVolumeCategory.Music => LocalizationService.Mark("Music"),
                AudioVolumeCategory.OnlineServerEvents => LocalizationService.Mark("Online server events"),
                AudioVolumeCategory.Communicator => LocalizationService.Mark("Communicator"),
                _ => LocalizationService.Mark("Volume")
            };
        }

    }
}
