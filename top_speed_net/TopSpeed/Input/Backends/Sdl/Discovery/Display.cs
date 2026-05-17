using System.Globalization;
using TopSpeed.Localization;
using TS.Sdl.Input;

namespace TopSpeed.Input.Backends.Sdl
{
    internal static class Display
    {
        public static ControllerDisplayProfile CreateProfile(DeviceMetadata metadata, bool isRacingWheel)
        {
            if (isRacingWheel || metadata.JoystickType == JoystickType.Wheel)
                return ControllerDisplayProfile.RacingWheel;

            if (!IsGamepad(metadata))
                return ControllerDisplayProfile.Joystick;

            return new ControllerDisplayProfile(ControllerDeviceType.Gamepad, ResolveGamepadFamily(metadata));
        }

        public static string BuildChoiceLabel(DeviceMetadata metadata, bool isRacingWheel)
        {
            var name = BuildName(metadata);
            var type = BuildTypeLabel(metadata, isRacingWheel);
            var disambiguator = BuildDisambiguator(metadata);
            return $"{name} ({type}, {disambiguator})";
        }

        public static string BuildName(DeviceMetadata metadata)
        {
            if (!string.IsNullOrWhiteSpace(metadata.Name))
                return metadata.Name.Trim();

            return LocalizationService.Format(
                LocalizationService.Mark("Controller {0}"),
                metadata.InstanceId.ToString(CultureInfo.InvariantCulture));
        }

        private static string BuildTypeLabel(DeviceMetadata metadata, bool isRacingWheel)
        {
            if (isRacingWheel || metadata.JoystickType == JoystickType.Wheel)
                return LocalizationService.Translate(LocalizationService.Mark("Racing wheel"));

            if (IsGamepad(metadata))
                return LocalizationService.Translate(LocalizationService.Mark("Gamepad"));

            return LocalizationService.Translate(LocalizationService.Mark("Joystick"));
        }

        private static string BuildDisambiguator(DeviceMetadata metadata)
        {
            if (metadata.PlayerIndex >= 0)
                return string.Format(CultureInfo.InvariantCulture, "P{0}", metadata.PlayerIndex + 1);

            if (metadata.VendorId != 0 || metadata.ProductId != 0)
            {
                return string.Format(
                    CultureInfo.InvariantCulture,
                    "{0:X4}:{1:X4}",
                    metadata.VendorId,
                    metadata.ProductId);
            }

            return metadata.InstanceId.ToString(CultureInfo.InvariantCulture);
        }

        private static bool IsGamepad(DeviceMetadata metadata)
        {
            return metadata.IsGamepad
                || metadata.JoystickType == JoystickType.Gamepad
                || metadata.GamepadType != GamepadType.Unknown;
        }

        private static ControllerGamepadFamily ResolveGamepadFamily(DeviceMetadata metadata)
        {
            switch (metadata.GamepadType)
            {
                case GamepadType.Xbox360:
                case GamepadType.XboxOne:
                    return ControllerGamepadFamily.Xbox;

                case GamepadType.PS3:
                case GamepadType.PS4:
                case GamepadType.PS5:
                    return ControllerGamepadFamily.PlayStation;

                case GamepadType.NintendoSwitchPro:
                case GamepadType.NintendoSwitchJoyconLeft:
                case GamepadType.NintendoSwitchJoyconRight:
                case GamepadType.NintendoSwitchJoyconPair:
                case GamepadType.GameCube:
                    return ControllerGamepadFamily.Nintendo;
            }

            var name = metadata.Name?.ToLowerInvariant() ?? string.Empty;
            if (name.Contains("xbox"))
                return ControllerGamepadFamily.Xbox;
            if (name.Contains("playstation")
                || name.Contains("dualshock")
                || name.Contains("dualsense")
                || name.Contains("ps3")
                || name.Contains("ps4")
                || name.Contains("ps5"))
            {
                return ControllerGamepadFamily.PlayStation;
            }

            if (name.Contains("nintendo")
                || name.Contains("switch")
                || name.Contains("joy-con")
                || name.Contains("joycon")
                || name.Contains("gamecube"))
            {
                return ControllerGamepadFamily.Nintendo;
            }

            return ControllerGamepadFamily.Semantic;
        }
    }
}
