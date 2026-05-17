using TopSpeed.Protocol;
using TopSpeed.Vehicles;
using Xunit;

namespace TopSpeed.Tests;

[Trait("Category", "Behavior")]
public sealed class CatalogFuelBehaviorTests
{
    [Theory]
    [InlineData(CarType.Vehicle1, 74f, 3.8f)]
    [InlineData(CarType.Vehicle2, 64f, 4.0f)]
    [InlineData(CarType.Vehicle3, 35f, 1.4f)]
    [InlineData(CarType.Vehicle4, 44f, 2.0f)]
    [InlineData(CarType.Vehicle5, 61f, 7.0f)]
    [InlineData(CarType.Vehicle6, 60f, 2.5f)]
    [InlineData(CarType.Vehicle7, 90f, 6.5f)]
    [InlineData(CarType.Vehicle8, 59f, 3.0f)]
    [InlineData(CarType.Vehicle9, 75f, 2.1f)]
    [InlineData(CarType.Vehicle10, 17f, 1.0f)]
    [InlineData(CarType.Vehicle11, 16f, 1.1f)]
    [InlineData(CarType.Vehicle12, 17f, 1.0f)]
    public void OfficialVehicles_ShouldUseExplicitFuelAndDisplacement(
        CarType carType,
        float expectedTankLiters,
        float expectedDisplacementLiters)
    {
        var spec = OfficialVehicleCatalog.Get((int)carType);

        spec.FuelTankCapacityLiters.Should().BeApproximately(expectedTankLiters, 0.001f);
        spec.EngineDisplacementLiters.Should().BeApproximately(expectedDisplacementLiters, 0.001f);
    }
}
