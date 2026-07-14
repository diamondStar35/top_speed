using TopSpeed.Vehicles;
using Xunit;

namespace TopSpeed.Tests;

[Trait("Category", "Behavior")]
public sealed class TireTractionAssistBehaviorTests
{
    [Fact]
    public void ResolveStraightLineTractionScale_StraightAndHealthy_ShouldStronglyRecoverTraction()
    {
        var assisted = TireTractionAssist.ResolveStraightLineTractionScale(
            baseTractionScale: 0.62f,
            speedMps: 60f,
            steeringInput: 0,
            slipAngleNormalized: 0.01f,
            lateralSlipNormalized: 0.01f,
            wearFraction: 0.10f,
            overheatNormalized: 0.08f);

        assisted.Should().BeGreaterThan(0.86f);
    }

    [Fact]
    public void ResolveStraightLineTractionScale_Turning_ShouldKeepBaseTractionBehavior()
    {
        var assisted = TireTractionAssist.ResolveStraightLineTractionScale(
            baseTractionScale: 0.62f,
            speedMps: 60f,
            steeringInput: 45,
            slipAngleNormalized: 0.40f,
            lateralSlipNormalized: 0.36f,
            wearFraction: 0.10f,
            overheatNormalized: 0.08f);

        assisted.Should().BeApproximately(0.62f, 0.02f);
    }

    [Fact]
    public void ResolveStraightLineTractionScale_SevereDamage_ShouldStillAssistButLess()
    {
        var healthy = TireTractionAssist.ResolveStraightLineTractionScale(
            baseTractionScale: 0.55f,
            speedMps: 72f,
            steeringInput: 0,
            slipAngleNormalized: 0.01f,
            lateralSlipNormalized: 0.01f,
            wearFraction: 0.18f,
            overheatNormalized: 0.05f);
        var damaged = TireTractionAssist.ResolveStraightLineTractionScale(
            baseTractionScale: 0.55f,
            speedMps: 72f,
            steeringInput: 0,
            slipAngleNormalized: 0.01f,
            lateralSlipNormalized: 0.01f,
            wearFraction: 0.94f,
            overheatNormalized: 0.94f);

        damaged.Should().BeGreaterThan(0.72f);
        damaged.Should().BeLessThan(healthy);
    }
}
