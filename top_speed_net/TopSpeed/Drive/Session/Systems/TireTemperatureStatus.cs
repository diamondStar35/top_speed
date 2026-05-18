using TopSpeed.Localization;

namespace TopSpeed.Drive.Session.Systems
{
    internal static class TireTemperatureStatus
    {
        public static string ResolvePhrase(
            float temperatureC,
            float coldEndTemperatureC,
            float optimalStartTemperatureC,
            float optimalEndTemperatureC,
            float overheatEndTemperatureC)
        {
            if (temperatureC >= overheatEndTemperatureC)
                return LocalizationService.Mark("overheated tires");
            if (temperatureC >= optimalEndTemperatureC)
                return LocalizationService.Mark("hot tires");
            if (temperatureC >= optimalStartTemperatureC)
                return LocalizationService.Mark("optimal temperature");
            if (temperatureC >= coldEndTemperatureC)
                return LocalizationService.Mark("warming up tires");

            return LocalizationService.Mark("cold tires");
        }
    }
}
