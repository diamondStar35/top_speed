using System;
using System.Collections.Generic;
using TopSpeed.Data;
using TopSpeed.Input;

namespace TopSpeed.Menu
{
    internal sealed partial class MenuRegistry
    {
        private MenuScreen BuildOptionsGameSettingsMenu()
        {
            var items = new List<MenuItem>
            {
                new CheckBox(
                    "Include custom tracks in randomization",
                    () => _settings.RandomCustomTracks,
                    value => _settingsActions.UpdateSetting(() => _settings.RandomCustomTracks = value),
                    hint: "When checked, random track selection can include custom tracks. Press ENTER to toggle."),
                new CheckBox(
                    "Include custom vehicles in randomization",
                    () => _settings.RandomCustomVehicles,
                    value => _settingsActions.UpdateSetting(() => _settings.RandomCustomVehicles = value),
                    hint: "When checked, random vehicle selection can include custom vehicles. Press ENTER to toggle."),
                new CheckBox(
                    "Enable HRTF audio",
                    () => _settings.HrtfAudio,
                    value => _settingsActions.UpdateSetting(() => _settings.HrtfAudio = value),
                    hint: "When checked, Three-D audio uses HRTF spatialization for more realistic positioning. Press ENTER to toggle."),
                new CheckBox(
                    "Enable stereo widening for own car",
                    () => _settings.StereoWidening,
                    value => _settingsActions.UpdateSetting(() => _settings.StereoWidening = value),
                    hint: "When checked, the game will widen the stereo image of your own car's sounds and quiet the opposite ear as you move. Press ENTER to toggle."),
                new CheckBox(
                    "Automatic audio device format",
                    () => _settings.AutoDetectAudioDeviceFormat,
                    value => _settingsActions.UpdateSetting(() => _settings.AutoDetectAudioDeviceFormat = value),
                    hint: "When checked, the game uses the device channel count and sample rate. Restart required. Press ENTER to toggle."),
                new Switch(
                    "Units",
                    "metric",
                    "imperial",
                    () => _settings.Units == UnitSystem.Metric,
                    value => _settingsActions.UpdateSetting(() => _settings.Units = value ? UnitSystem.Metric : UnitSystem.Imperial),
                    hint: "Switch between metric and imperial units. Press ENTER to change."),
                new CheckBox(
                    "Enable usage hints",
                    () => _settings.UsageHints,
                    value => _settingsActions.UpdateSetting(() => _settings.UsageHints = value),
                    hint: "When checked, menu items can speak usage hints after a short delay. Press ENTER to toggle."),
                new CheckBox(
                    "Enable menu wrapping",
                    () => _settings.MenuWrapNavigation,
                    value => _settingsActions.UpdateSetting(() => _settings.MenuWrapNavigation = value),
                    onChanged: value => _menu.SetWrapNavigation(value),
                    hint: "When checked, menu navigation wraps from the last item to the first. Press ENTER to toggle."),
                BuildMenuSoundPresetItem(),
                new CheckBox(
                    "Enable menu navigation panning",
                    () => _settings.MenuNavigatePanning,
                    value => _settingsActions.UpdateSetting(() => _settings.MenuNavigatePanning = value),
                    onChanged: value => _menu.SetMenuNavigatePanning(value),
                    hint: "When checked, menu navigation sounds pan left or right based on the item position. Press ENTER to toggle."),
                new MenuItem("Recalibrate screen reader rate", MenuAction.None, onActivate: _settingsActions.RecalibrateScreenReaderRate),
                BackItem()
            };
            return _menu.CreateMenu("options_game", items);
        }

        private MenuItem BuildMenuSoundPresetItem()
        {
            if (_menuSoundPresets.Count < 2)
            {
                return new MenuItem(
                    () => $"Menu sounds: {(_menuSoundPresets.Count > 0 ? _menuSoundPresets[0] : "default")}",
                    MenuAction.None);
            }

            return new RadioButton(
                "Menu sounds",
                _menuSoundPresets,
                () => GetMenuSoundPresetIndex(),
                value => _settingsActions.UpdateSetting(() => _settings.MenuSoundPreset = _menuSoundPresets[value]),
                onChanged: _ => _menu.SetMenuSoundPreset(_settings.MenuSoundPreset),
                hint: "Select the menu sound preset. Use LEFT or RIGHT to change.");
        }

        private int GetMenuSoundPresetIndex()
        {
            if (_menuSoundPresets.Count == 0)
                return 0;
            for (var i = 0; i < _menuSoundPresets.Count; i++)
            {
                if (string.Equals(_menuSoundPresets[i], _settings.MenuSoundPreset, StringComparison.OrdinalIgnoreCase))
                    return i;
            }

            return 0;
        }
    }
}
