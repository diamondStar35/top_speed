using TopSpeed.Physics.Fuel;
using Xunit;

namespace TopSpeed.Tests;

[Trait("Category", "Behavior")]
public sealed class FuelRuntimeBehaviorTests
{
    [Fact]
    public void Step_WithCombustionAndLoad_ShouldConsumeFuel()
    {
        var config = new FuelConfig(tankCapacityLiters: 72f, engineDisplacementLiters: 2.5f);
        var initial = new FuelRuntimeState(remainingLiters: 72f, smoothedBurnLitersPerSecond: 0f);

        var result = FuelRuntime.Step(
            config,
            initial,
            new FuelRuntimeInput(
                elapsedSeconds: 10f,
                combustionActive: true,
                throttleNormalized: 0.75f,
                netPowerKw: 120f,
                speedMps: 35f));

        result.State.RemainingLiters.Should().BeLessThan(initial.RemainingLiters);
        result.BurnLitersPerHour.Should().BeGreaterThan(0f);
        result.PowerScale.Should().Be(1f);
    }

    [Fact]
    public void Step_SameLoad_LargerDisplacement_ShouldBurnMoreFuel()
    {
        var small = new FuelConfig(tankCapacityLiters: 72f, engineDisplacementLiters: 1.6f);
        var large = new FuelConfig(tankCapacityLiters: 72f, engineDisplacementLiters: 6.2f);
        var state = new FuelRuntimeState(remainingLiters: 60f, smoothedBurnLitersPerSecond: 0f);
        var input = new FuelRuntimeInput(
            elapsedSeconds: 5f,
            combustionActive: true,
            throttleNormalized: 0.6f,
            netPowerKw: 90f,
            speedMps: 30f);

        var smallResult = FuelRuntime.Step(small, state, input);
        var largeResult = FuelRuntime.Step(large, state, input);

        largeResult.BurnLitersPerHour.Should().BeGreaterThan(smallResult.BurnLitersPerHour);
    }

    [Fact]
    public void Step_WhenTankNearEmpty_ShouldReducePower_ThenReachZeroWhenEmpty()
    {
        var config = new FuelConfig(tankCapacityLiters: 10f, engineDisplacementLiters: 2.0f);
        var nearEmptyState = new FuelRuntimeState(remainingLiters: 0.3f, smoothedBurnLitersPerSecond: 0f);

        var nearEmpty = FuelRuntime.Step(
            config,
            nearEmptyState,
            new FuelRuntimeInput(
                elapsedSeconds: 0.1f,
                combustionActive: true,
                throttleNormalized: 1f,
                netPowerKw: 150f,
                speedMps: 20f));

        nearEmpty.LowFuel.Should().BeTrue();
        nearEmpty.PowerScale.Should().BeLessThan(1f);
        nearEmpty.PowerScale.Should().BeGreaterThan(0f);

        var empty = FuelRuntime.Step(
            config,
            new FuelRuntimeState(remainingLiters: 0f, smoothedBurnLitersPerSecond: 0f),
            new FuelRuntimeInput(
                elapsedSeconds: 1f,
                combustionActive: true,
                throttleNormalized: 1f,
                netPowerKw: 150f,
                speedMps: 20f));

        empty.EmptyFuel.Should().BeTrue();
        empty.PowerScale.Should().Be(0f);
    }
}
