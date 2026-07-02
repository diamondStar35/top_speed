using TopSpeed.Localization;

namespace TopSpeed.Vehicles.Parsing
{
    public static partial class VehicleTsvParser
    {
        private static string Localized(string text)
        {
            return LocalizationService.Mark(text);
        }

        private static string Localized(string format, params object[] args)
        {
            return LocalizationService.Format(LocalizationService.Mark(format), args);
        }
    }
}

