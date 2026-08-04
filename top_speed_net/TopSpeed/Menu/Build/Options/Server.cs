using System.Collections.Generic;
using TopSpeed.Localization;

namespace TopSpeed.Menu
{
    internal sealed partial class MenuRegistry
    {
        private MenuScreen BuildOptionsServerSettingsMenu()
        {
            var items = new List<MenuItem>
            {
                new MenuItem(
                    () => LocalizationService.Format(
                        LocalizationService.Mark("Default server port: {0}"),
                        FormatServerPort(_settings.DefaultServerPort)),
                    MenuAction.None,
                    onActivate: _server.BeginServerPortEntry),
                new MenuItem(
                    () => string.IsNullOrWhiteSpace(_settings.DefaultCallSign)
                        ? LocalizationService.Mark("Default call sign: not set")
                        : LocalizationService.Format(
                            LocalizationService.Mark("Default call sign: {0}"),
                            _settings.DefaultCallSign),
                    MenuAction.None,
                    onActivate: _server.BeginDefaultCallSignEntry),
                new CheckBox(LocalizationService.Mark("Prompt to keep downloaded custom vehicles"),
                    () => _settings.KeepDownloadedVehiclesPrompt,
                    value => _settingsActions.UpdateSetting(() => _settings.KeepDownloadedVehiclesPrompt = value),
                    hintProvider: HintToggleProvider(LocalizationService.Mark("When checked, after a multiplayer race you are asked whether to keep custom vehicles you downloaded (they are saved to your Vehicles folder and become available offline). When unchecked, downloaded vehicles are never kept after you close the game.")))
            };
            return BackMenu("options_server", items);
        }
    }
}

