using System;

namespace TopSpeed.Menu
{
    internal static class MenuSoundPresets
    {
        // Preset value that loads no menu cues at all. Stored in settings like any other preset name,
        // so it round-trips through ui.menuSoundPreset without a separate flag.
        public const string Off = "off";

        public static bool IsOff(string? preset)
        {
            return string.Equals(preset?.Trim(), Off, StringComparison.OrdinalIgnoreCase);
        }
    }
}
