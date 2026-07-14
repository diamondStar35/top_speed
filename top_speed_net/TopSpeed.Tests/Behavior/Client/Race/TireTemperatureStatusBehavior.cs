using TopSpeed.Drive.Session.Systems;
using TopSpeed.Localization;
using Xunit;

namespace TopSpeed.Tests;

[Trait("Category", "Behavior")]
public sealed class TireTemperatureStatusBehaviorTests
{
    [Fact]
    public void ResolvePhrase_WhenTemperatureIsBelowColdEnd_ShouldReportCold()
    {
        var phrase = TireTemperatureStatus.ResolvePhrase(
            temperatureC: 27f,
            coldEndTemperatureC: 32f,
            optimalStartTemperatureC: 54f,
            optimalEndTemperatureC: 78f,
            overheatEndTemperatureC: 112f);

        phrase.Should().Be(LocalizationService.Mark("cold tires"));
    }

    [Fact]
    public void ResolvePhrase_WhenTemperatureIsBetweenColdEndAndOptimalStart_ShouldReportWarmingUp()
    {
        var phrase = TireTemperatureStatus.ResolvePhrase(
            temperatureC: 57f,
            coldEndTemperatureC: 40f,
            optimalStartTemperatureC: 76f,
            optimalEndTemperatureC: 102f,
            overheatEndTemperatureC: 132f);

        phrase.Should().Be(LocalizationService.Mark("warming up tires"));
    }

    [Fact]
    public void ResolvePhrase_WhenTemperatureIsInsideOptimalRange_ShouldReportOptimal()
    {
        var phrase = TireTemperatureStatus.ResolvePhrase(
            temperatureC: 57f,
            coldEndTemperatureC: 32f,
            optimalStartTemperatureC: 54f,
            optimalEndTemperatureC: 78f,
            overheatEndTemperatureC: 112f);

        phrase.Should().Be(LocalizationService.Mark("optimal temperature"));
    }
}
