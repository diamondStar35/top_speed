using TopSpeed.Physics.Tires.Wear;
using Xunit;

namespace TopSpeed.Tests;

[Trait("Category", "Behavior")]
public sealed class TireWearInputSignalBehaviorTests
{
    [Fact]
    public void ResolveLongitudinalSlipNormalized_GentleAcceleration_ShouldStayLow()
    {
        var signal = TireWearInputSignals.ResolveLongitudinalSlipNormalized(
            driveAccelerationMps2: 1.2f,
            brakeDecelMps2: 0f);

        signal.Should().BeLessThan(0.08f);
    }

    [Fact]
    public void ResolveLongitudinalSlipNormalized_HardAcceleration_ShouldRise()
    {
        var signal = TireWearInputSignals.ResolveLongitudinalSlipNormalized(
            driveAccelerationMps2: 5.8f,
            brakeDecelMps2: 0f);

        signal.Should().BeGreaterThan(0.52f);
    }

    [Fact]
    public void ResolveLongitudinalSlipNormalized_HardBraking_ShouldRise()
    {
        var signal = TireWearInputSignals.ResolveLongitudinalSlipNormalized(
            driveAccelerationMps2: 0f,
            brakeDecelMps2: 8.0f);

        signal.Should().BeGreaterThan(0.45f);
    }

    [Fact]
    public void ResolveLoadNormalized_HigherMassAndCornering_ShouldIncreaseLoadSignal()
    {
        var lightGentle = TireWearInputSignals.ResolveLoadNormalized(
            massKg: 900f,
            lateralLoadRatio: 0.20f,
            longitudinalSlipNormalized: 0.06f);
        var heavyAggressive = TireWearInputSignals.ResolveLoadNormalized(
            massKg: 1800f,
            lateralLoadRatio: 0.75f,
            longitudinalSlipNormalized: 0.55f);

        heavyAggressive.Should().BeGreaterThan(lightGentle + 0.30f);
    }

    [Fact]
    public void ResolveRollingResistanceNormalized_HigherSpeed_ShouldIncreaseSignal()
    {
        var lowSpeed = TireWearInputSignals.ResolveRollingResistanceNormalized(
            rollingResistanceCoefficient: 0.013f,
            surfaceRollingResistanceFactor: 1f,
            speedMps: 12f);
        var highSpeed = TireWearInputSignals.ResolveRollingResistanceNormalized(
            rollingResistanceCoefficient: 0.013f,
            surfaceRollingResistanceFactor: 1f,
            speedMps: 45f);

        highSpeed.Should().BeGreaterThan(lowSpeed);
    }
}
