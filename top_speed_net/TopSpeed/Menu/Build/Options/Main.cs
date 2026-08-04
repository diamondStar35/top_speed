using System.Collections.Generic;

using TopSpeed.Localization;
namespace TopSpeed.Menu
{
    internal sealed partial class MenuRegistry
    {
        private MenuScreen BuildOptionsMenu()
        {
            var items = new List<MenuItem>
            {
                new MenuItem(LocalizationService.Mark("General"),
                    MenuAction.None,
                    nextMenuId: "options_game",
                    hint: LocalizationService.Mark("Configure general gameplay and interface behavior, including units, usage hints, menu behavior, and update checking.")),
                new MenuItem(LocalizationService.Mark("Speech"),
                    MenuAction.None,
                    nextMenuId: "options_speech",
                    hint: LocalizationService.Mark("Configure speech behavior, including screen-reader backend selection and screen-reader calibration.")),
                new MenuItem(LocalizationService.Mark("Audio"),
                    MenuAction.None,
                    nextMenuId: "options_audio",
                    hint: LocalizationService.Mark("Configure spatial audio behavior, including HRTF processing, stereo widening, automatic device format detection, and voice input device selection.")),
                new MenuItem(LocalizationService.Mark("Volume settings"), MenuAction.None, nextMenuId: "options_volume",
                    onActivate: () =>
                    {
                        _settings.SyncAudioCategoriesFromMusicVolume();
                        _audio.ApplyAudioSettings();
                    },
                    hint: LocalizationService.Mark("Adjust category-based volume balance for engine sounds, effects, ambience, radio, music, and online events.")),
                new MenuItem(LocalizationService.Mark("Controls"),
                    MenuAction.None,
                    nextMenuId: "options_controls",
                    hint: LocalizationService.Mark("Configure input devices, force feedback, progressive keyboard behavior, key mappings, and menu shortcut mappings.")),
                new MenuItem(LocalizationService.Mark("Race settings"),
                    MenuAction.None,
                    nextMenuId: "options_drive",
                    hint: LocalizationService.Mark("Set race defaults such as copilot callouts, curve announcements, automatic information, laps, computer opponents, and difficulty.")),
                new MenuItem(LocalizationService.Mark("Server settings"),
                    MenuAction.None,
                    nextMenuId: "options_server",
                    hint: LocalizationService.Mark("Set the defaults used when you connect to a multiplayer server, including the port and your call sign, and whether you are offered custom vehicles you download.")),
                new MenuItem(LocalizationService.Mark("Restore default settings"),
                    MenuAction.None,
                    onActivate: _settingsActions.ShowRestoreDefaultsDialog,
                    hint: LocalizationService.Mark("Reset all configurable settings back to their default values."))
            };
            return BackMenu("options_main", items);
        }
    }
}






