using System;
using System.Collections.Generic;
using System.IO;
using TopSpeed.Core;
using TopSpeed.Input;
using TopSpeed.Localization;
using TopSpeed.Network;

namespace TopSpeed.Menu
{
    internal sealed partial class MenuRegistry
    {
        private static readonly MenuItem[] EmptyItems = Array.Empty<MenuItem>();

        private static IReadOnlyList<string> LoadMenuSoundPresets()
        {
            var root = Path.Combine(AssetPaths.SoundsRoot, "menu");
            if (!Directory.Exists(root))
                return Array.Empty<string>();

            var presets = new List<string>();
            foreach (var directory in Directory.GetDirectories(root))
            {
                var name = Path.GetFileName(directory);
                if (string.IsNullOrWhiteSpace(name))
                    continue;
                presets.Add(name.Trim());
            }

            presets.Sort(StringComparer.OrdinalIgnoreCase);

            // Silence is a valid choice alongside the sound packs on disk: the navigate, wrap and edge
            // cues talk over the screen reader for players who navigate by speech alone.
            presets.Insert(0, MenuSoundPresets.Off);
            return presets;
        }

        private string MainMenuTitle()
        {
            if (InteractionHints.IsTouchPlatform())
            {
                return LocalizationService.Mark("Main menu. Swipe left or right to navigate the options. Swipe up to activate. Swipe down to go back. Swipe up with two fingers to move to the first item and swipe down with two fingers to move to the last item. Long press repeats the current usage hint.");
            }

            var keyboard = LocalizationService.Mark("Main Menu. Use your arrow keys to navigate the options. Press ENTER to select. Press ESCAPE to back out of any menu. Pressing HOME or END will move you to the top or bottom of a menu.");
            var controller = LocalizationService.Mark("Main Menu. Use the view finder to move through the options. Press up or down to navigate. Press right or button 1 to select. Press left to back out of any menu.");
            var both = LocalizationService.Mark("Main Menu. Use your arrow keys or the view finder to move through the options. Press ENTER or right or button 1 to select. Press ESCAPE or left to back out of any menu. Pressing HOME or END will move you to the top or bottom of a menu.");

            return _settings.DeviceMode switch
            {
                InputDeviceMode.Keyboard => keyboard,
                InputDeviceMode.Controller => controller,
                _ => both
            };
        }

        private static string HintForPlatform(string desktopHint, string touchHint)
        {
            return InteractionHints.ForPlatform(desktopHint, touchHint);
        }

        private static Func<string> HintForPlatformProvider(string desktopHint, string touchHint)
        {
            return () => HintForPlatform(desktopHint, touchHint);
        }

        private static string HintForPlatform(string hint, string desktopControl, string touchControl)
        {
            var text = LocalizationService.Translate(hint).Trim();
            var control = LocalizationService.Translate(InteractionHints.ForPlatform(desktopControl, touchControl)).Trim();
            if (string.IsNullOrWhiteSpace(text))
                return control;
            if (string.IsNullOrWhiteSpace(control))
                return text;

            return text + " " + control;
        }

        private static Func<string> HintForPlatformProvider(string hint, string desktopControl, string touchControl)
        {
            return () => HintForPlatform(hint, desktopControl, touchControl);
        }

        private static string HintToggle(string hint)
        {
            return HintForPlatform(
                hint,
                LocalizationService.Mark("Press ENTER to toggle."),
                LocalizationService.Mark("Swipe up to toggle."));
        }

        private static Func<string> HintToggleProvider(string hint)
        {
            return () => HintToggle(hint);
        }

        private static string HintChange(string hint)
        {
            return HintForPlatform(
                hint,
                LocalizationService.Mark("Press ENTER to change."),
                LocalizationService.Mark("Swipe up to change."));
        }

        private static Func<string> HintChangeProvider(string hint)
        {
            return () => HintChange(hint);
        }

        private static string HintSelect(string hint)
        {
            return HintForPlatform(
                hint,
                LocalizationService.Mark("Press ENTER to select."),
                LocalizationService.Mark("Swipe up to select."));
        }

        private static Func<string> HintSelectProvider(string hint)
        {
            return () => HintSelect(hint);
        }

        private static string HintStart(string hint)
        {
            return HintForPlatform(
                hint,
                LocalizationService.Mark("Press ENTER to start."),
                LocalizationService.Mark("Swipe up to start."));
        }

        private static Func<string> HintStartProvider(string hint)
        {
            return () => HintStart(hint);
        }

        private static string HintAdjust(string hint)
        {
            return HintForPlatform(
                hint,
                LocalizationService.Mark("Use LEFT or RIGHT to change."),
                LocalizationService.Mark("Swipe left or right with two fingers to change."));
        }

        private static Func<string> HintAdjustProvider(string hint)
        {
            return () => HintAdjust(hint);
        }

        private static string HintSlider(string hint)
        {
            return HintForPlatform(
                hint,
                LocalizationService.Mark("Use LEFT or RIGHT to change by 1, PAGE UP or PAGE DOWN to change by 10, HOME for maximum, END for minimum."),
                LocalizationService.Mark("Swipe up or down with two fingers to change by 10, swipe left or right with two fingers to change by 1, and swipe up or down with three fingers for maximum or minimum."));
        }

        private static Func<string> HintSliderProvider(string hint)
        {
            return () => HintSlider(hint);
        }

        private static string FormatServerPort(int port)
        {
            return port > 0
                ? port.ToString()
                : LocalizationService.Format(LocalizationService.Mark("default ({0})"), ClientProtocol.DefaultServerPort);
        }

        private static string DeviceLabel(InputDeviceMode mode)
        {
            return mode switch
            {
                InputDeviceMode.Keyboard => LocalizationService.Translate(LocalizationService.Mark("keyboard")),
                InputDeviceMode.Controller => LocalizationService.Translate(LocalizationService.Mark("controller")),
                InputDeviceMode.Both => LocalizationService.Translate(LocalizationService.Mark("both")),
                _ => LocalizationService.Translate(LocalizationService.Mark("keyboard"))
            };
        }

        private MenuScreen BackMenu(string id, IEnumerable<MenuItem> items, string? title = null)
        {
            return _menu.CreateMenu(id, items, title, spec: ScreenSpec.Back);
        }

        private MenuScreen EmptyMenu(string id, string? title = null, ScreenSpec? spec = null)
        {
            return _menu.CreateMenu(id, EmptyItems, title, spec: spec);
        }

        private MenuScreen EmptyBackMenu(string id, string? title = null)
        {
            return EmptyMenu(id, title, ScreenSpec.Back);
        }

        private MenuScreen ChatMenu(string id, string viewId, IEnumerable<MenuItem> items, string title, ScreenSpec? viewSpec = null, ScreenSpec? spec = null)
        {
            var menu = _menu.CreateMenu(id, items, title, spec: spec);
            menu.SetScreens(new[]
            {
                new MenuView(viewId, items, title, spec: viewSpec),
                _sharedLobbyChatScreen
            }, viewId);
            return menu;
        }
    }
}

