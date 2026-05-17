using System;
using System.Collections.Generic;
using TopSpeed.Data;
using TopSpeed.Input;

using TopSpeed.Localization;
namespace TopSpeed.Menu
{
    internal sealed partial class MenuRegistry
    {
        private MenuScreen BuildOptionsGameSettingsMenu()
        {
            return BackMenu("options_game", BuildGeneralSettingsItems());
        }

        internal void RefreshGeneralSettingsMenu()
        {
            _menu.UpdateItems("options_game", BuildGeneralSettingsItems(), preserveSelection: true);
        }

        private List<MenuItem> BuildGeneralSettingsItems()
        {
            var items = new List<MenuItem>
            {
                new MenuItem(
                    BuildLanguageOptionText,
                    MenuAction.None,
                    onActivate: _settingsActions.ChangeLanguage,
                    hintProvider: HintSelectProvider(LocalizationService.Mark("Choose the language used for menu and spoken interface text."))),
                new CheckBox(LocalizationService.Mark("Include custom tracks in randomization"),
                    () => _settings.RandomCustomTracks,
                    value => _settingsActions.UpdateSetting(() => _settings.RandomCustomTracks = value),
                    hintProvider: HintToggleProvider(LocalizationService.Mark("When checked, random track selection can include custom tracks."))),
                new CheckBox(LocalizationService.Mark("Include custom vehicles in randomization"),
                    () => _settings.RandomCustomVehicles,
                    value => _settingsActions.UpdateSetting(() => _settings.RandomCustomVehicles = value),
                    hintProvider: HintToggleProvider(LocalizationService.Mark("When checked, random vehicle selection can include custom vehicles."))),
                new Switch(LocalizationService.Mark("Units"),
                    LocalizationService.Mark("metric"),
                    LocalizationService.Mark("imperial"),
                    () => _settings.Units == UnitSystem.Metric,
                    value => _settingsActions.UpdateSetting(() => _settings.Units = value ? UnitSystem.Metric : UnitSystem.Imperial),
                    hintProvider: HintChangeProvider(LocalizationService.Mark("Switch between metric and imperial units."))),
                new CheckBox(LocalizationService.Mark("Enable usage hints"),
                    () => _settings.UsageHints,
                    value => _settingsActions.UpdateSetting(() => _settings.UsageHints = value),
                    hintProvider: HintToggleProvider(LocalizationService.Mark("When checked, menu items can speak usage hints after a short delay."))),
                new CheckBox(LocalizationService.Mark("Automatically focus first menu item"),
                    () => _settings.MenuAutoFocus,
                    value => _settingsActions.UpdateSetting(() => _settings.MenuAutoFocus = value),
                    onChanged: value => _menu.SetMenuAutoFocus(value),
                    hintProvider: HintToggleProvider(LocalizationService.Mark("When checked, each menu automatically focuses and announces the first item after the title."))),
                new CheckBox(LocalizationService.Mark("Enable menu wrapping"),
                    () => _settings.MenuWrapNavigation,
                    value => _settingsActions.UpdateSetting(() => _settings.MenuWrapNavigation = value),
                    onChanged: value => _menu.SetWrapNavigation(value),
                    hintProvider: HintToggleProvider(LocalizationService.Mark("When checked, menu navigation wraps from the last item to the first."))),
                BuildMenuSoundPresetItem(),
                new CheckBox(LocalizationService.Mark("Enable menu navigation panning"),
                    () => _settings.MenuNavigatePanning,
                    value => _settingsActions.UpdateSetting(() => _settings.MenuNavigatePanning = value),
                    onChanged: value => _menu.SetMenuNavigatePanning(value),
                    hintProvider: HintToggleProvider(LocalizationService.Mark("When checked, menu navigation sounds pan left or right based on the item position."))),
                new CheckBox(LocalizationService.Mark("Play logo at startup"),
                    () => _settings.PlayLogoAtStartup,
                    value => _settingsActions.UpdateSetting(() => _settings.PlayLogoAtStartup = value),
                    hintProvider: HintToggleProvider(LocalizationService.Mark("When checked, the startup logo audio plays when the game launches."))),
                new CheckBox(LocalizationService.Mark("Check for updates on startup"),
                    () => _settings.AutoCheckUpdates,
                    value => _settingsActions.UpdateSetting(() => _settings.AutoCheckUpdates = value),
                    hintProvider: HintToggleProvider(LocalizationService.Mark("When checked, the game checks for updates automatically after the logo."))),
                new CheckBox(LocalizationService.Mark("Use proxy for update downloads"),
                    () => _settings.UseUpdateProxy,
                    value => _settingsActions.SetUseUpdateProxy(value),
                    onChanged: _ => RefreshGeneralSettingsMenu(),
                    hintProvider: HintToggleProvider(LocalizationService.Mark("When checked, update package downloads are routed through your proxy URL.")))
            };

            if (_settings.UseUpdateProxy)
            {
                items.Add(new MenuItem(
                    BuildProxyUrlItemText,
                    MenuAction.None,
                    onActivate: _settingsActions.EditUpdateProxyUrl,
                    hintProvider: HintSelectProvider(LocalizationService.Mark("Edit the proxy URL used for update package downloads."))));
            }

            return items;
        }

        private string BuildLanguageOptionText()
        {
            return LocalizationService.Format(
                LocalizationService.Mark("Language: {0}"),
                _settingsActions.GetLanguageName());
        }

        private MenuItem BuildMenuSoundPresetItem()
        {
            if (_menuSoundPresets.Count < 2)
            {
                return new MenuItem(
                    () => LocalizationService.Format(
                        LocalizationService.Mark("Menu sounds: {0}"),
                        _menuSoundPresets.Count > 0
                            ? _menuSoundPresets[0]
                            : LocalizationService.Translate(LocalizationService.Mark("default"))),
                    MenuAction.None);
            }

            return new RadioButton(LocalizationService.Mark("Menu sounds"),
                _menuSoundPresets,
                () => GetMenuSoundPresetIndex(),
                value => _settingsActions.UpdateSetting(() => _settings.MenuSoundPreset = _menuSoundPresets[value]),
                onChanged: _ => _menu.SetMenuSoundPreset(_settings.MenuSoundPreset),
                hintProvider: HintAdjustProvider(LocalizationService.Mark("Select the menu sound preset.")));
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

        private string BuildProxyUrlItemText()
        {
            return string.IsNullOrWhiteSpace(_settings.UpdateProxyUrlPrefix)
                ? LocalizationService.Mark("Proxy URL, currently empty.")
                : LocalizationService.Format(
                    LocalizationService.Mark("Proxy URL, currently set to {0}"),
                    _settings.UpdateProxyUrlPrefix);
        }
    }
}

