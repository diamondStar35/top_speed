using TopSpeed.Physics.Tires.Wear;
using Xunit;

namespace TopSpeed.Tests;

[Trait("Category", "Behavior")]
public sealed class TireWearRuntimeBehaviorTests
{
    [Fact]
    public void ResolveInitialTemperature_ShouldStayNearAmbientAndSurface()
    {
        var config = TireWearDefaults.Balanced;
        var temperature = TireWearRuntime.ResolveInitialTemperature(config, ambientTemperatureC: 27f, surfaceTemperatureC: 35f);

        temperature.Should().BeInRange(27f, 35f);
    }

    [Fact]
    public void Step_WithDistanceOnly_ShouldIncreaseWear()
    {
        var config = TireWearDefaults.Balanced;
        var initial = TireWearDefaults.CreateInitialState(temperatureC: 30f);

        var result = TireWearRuntime.Step(
            config,
            initial,
            new TireWearInput(
                elapsedSeconds: 60f,
                speedMps: 45f,
                slipAngleNormalized: 0f,
                lateralSlipNormalized: 0f,
                longitudinalSlipNormalized: 0f,
                loadNormalized: 0.20f,
                rollingResistanceNormalized: 0.30f,
                ambientTemperatureC: 30f,
                surfaceTemperatureC: 36f,
                wetnessNormalized: 0f));

        result.State.WearFraction.Should().BeGreaterThan(initial.WearFraction);
        result.CombinedGripScale.Should().BeLessThanOrEqualTo(1f);
        result.CombinedGripScale.Should().BeGreaterThan(0.6f);
    }

    [Fact]
    public void Step_StraightLineSustainedThrottle_ShouldWarmTiresIntoWorkingRange()
    {
        var config = TireWearProfiles.CreateFromVehicle(
            tireGripCoefficient: 1.0f,
            massKg: 1774f,
            tireCircumferenceM: 2.22f,
            lateralGripCoefficient: 1.0f);
        var initial = TireWearDefaults.CreateInitialState(temperatureC: 26f);

        var result = TireWearRuntime.Step(
            config,
            initial,
            new TireWearInput(
                elapsedSeconds: 240f,
                speedMps: 38f,
                slipAngleNormalized: 0.03f,
                lateralSlipNormalized: 0.02f,
                longitudinalSlipNormalized: 0.24f,
                loadNormalized: 0.22f,
                rollingResistanceNormalized: 0.42f,
                ambientTemperatureC: 26f,
                surfaceTemperatureC: 33f,
                wetnessNormalized: 0f));

        result.State.TemperatureC.Should().BeGreaterThan(36f);
        result.State.TemperatureC.Should().BeLessThan(config.OverheatEndTemperatureC + 5f);
    }

    [Fact]
    public void Step_AfterCorneringHeat_StraightLineShouldNotInstantlyDropTemperature()
    {
        var config = TireWearProfiles.CreateFromVehicle(
            tireGripCoefficient: 1.0f,
            massKg: 1774f,
            tireCircumferenceM: 2.22f,
            lateralGripCoefficient: 1.0f);
        var initial = TireWearDefaults.CreateInitialState(temperatureC: 28f);

        var heated = TireWearRuntime.Step(
            config,
            initial,
            new TireWearInput(
                elapsedSeconds: 90f,
                speedMps: 44f,
                slipAngleNormalized: 0.70f,
                lateralSlipNormalized: 0.55f,
                longitudinalSlipNormalized: 0.45f,
                loadNormalized: 0.62f,
                rollingResistanceNormalized: 0.36f,
                ambientTemperatureC: 28f,
                surfaceTemperatureC: 36f,
                wetnessNormalized: 0f));

        var straight = TireWearRuntime.Step(
            config,
            heated.State,
            new TireWearInput(
                elapsedSeconds: 12f,
                speedMps: 50f,
                slipAngleNormalized: 0.02f,
                lateralSlipNormalized: 0.01f,
                longitudinalSlipNormalized: 0.04f,
                loadNormalized: 0.22f,
                rollingResistanceNormalized: 0.32f,
                ambientTemperatureC: 28f,
                surfaceTemperatureC: 36f,
                wetnessNormalized: 0f));

        straight.State.TemperatureC.Should().BeGreaterThan(40f);
        straight.State.TemperatureC.Should().BeGreaterThan(heated.State.TemperatureC - 15f);
    }

    [Fact]
    public void Step_SameDistance_HigherSlipShouldWearFaster()
    {
        var config = TireWearDefaults.Balanced;
        var initial = TireWearDefaults.CreateInitialState(temperatureC: 40f);
        var elapsedSeconds = 40f;
        var speedMps = 38f;

        var lowSlip = TireWearRuntime.Step(
            config,
            initial,
            new TireWearInput(
                elapsedSeconds,
                speedMps,
                slipAngleNormalized: 0.10f,
                lateralSlipNormalized: 0.08f,
                longitudinalSlipNormalized: 0.10f,
                loadNormalized: 0.25f,
                rollingResistanceNormalized: 0.28f,
                ambientTemperatureC: 28f,
                surfaceTemperatureC: 33f,
                wetnessNormalized: 0f));
        var highSlip = TireWearRuntime.Step(
            config,
            initial,
            new TireWearInput(
                elapsedSeconds,
                speedMps,
                slipAngleNormalized: 0.85f,
                lateralSlipNormalized: 0.65f,
                longitudinalSlipNormalized: 0.80f,
                loadNormalized: 0.70f,
                rollingResistanceNormalized: 0.35f,
                ambientTemperatureC: 28f,
                surfaceTemperatureC: 33f,
                wetnessNormalized: 0f));

        highSlip.State.WearFraction.Should().BeGreaterThan(lowSlip.State.WearFraction);
        highSlip.State.TemperatureC.Should().BeGreaterThan(lowSlip.State.TemperatureC);
    }

    [Fact]
    public void Step_OverheatedState_ShouldReduceGripAndIncreaseWear()
    {
        var config = TireWearDefaults.Balanced;
        var moderateState = new TireWearState(
            wearFraction: 0.18f,
            temperatureC: config.OptimalStartTemperatureC + 5f,
            smoothedSlipNormalized: 0.45f);
        var hotState = new TireWearState(
            wearFraction: 0.18f,
            temperatureC: config.OverheatEndTemperatureC + 12f,
            smoothedSlipNormalized: 0.45f);
        var input = new TireWearInput(
            elapsedSeconds: 16f,
            speedMps: 52f,
            slipAngleNormalized: 0.65f,
            lateralSlipNormalized: 0.50f,
            longitudinalSlipNormalized: 0.72f,
            loadNormalized: 0.65f,
            rollingResistanceNormalized: 0.32f,
            ambientTemperatureC: 30f,
            surfaceTemperatureC: 36f,
            wetnessNormalized: 0f);

        var moderate = TireWearRuntime.Step(config, moderateState, input);
        var hot = TireWearRuntime.Step(config, hotState, input);

        hot.CombinedGripScale.Should().BeLessThan(moderate.CombinedGripScale);
        hot.State.WearFraction.Should().BeGreaterThan(moderate.State.WearFraction);
    }

    [Fact]
    public void Step_WithExtremeValues_ShouldRemainBoundedAndFinite()
    {
        var config = TireWearDefaults.Balanced;
        var state = new TireWearState(
            wearFraction: float.NaN,
            temperatureC: float.PositiveInfinity,
            smoothedSlipNormalized: float.NegativeInfinity);
        var input = new TireWearInput(
            elapsedSeconds: 5f,
            speedMps: 120f,
            slipAngleNormalized: 4f,
            lateralSlipNormalized: 3f,
            longitudinalSlipNormalized: 2f,
            loadNormalized: 5f,
            rollingResistanceNormalized: 9f,
            ambientTemperatureC: float.NaN,
            surfaceTemperatureC: float.PositiveInfinity,
            wetnessNormalized: 4f);

        var result = TireWearRuntime.Step(config, state, input);

        result.State.WearFraction.Should().BeInRange(0f, 1f);
        result.State.SmoothedSlipNormalized.Should().BeInRange(0f, 1f);
        result.TractionGripScale.Should().BeInRange(0.45f, 1f);
        result.LateralGripScale.Should().BeInRange(0.45f, 1f);
        result.BrakeGripScale.Should().BeInRange(0.45f, 1f);
        result.CombinedGripScale.Should().BeInRange(0.45f, 1f);
        IsFinite(result.State.TemperatureC).Should().BeTrue();
    }

    [Fact]
    public void Step_WithWetWeather_ShouldRunCoolerThanDry()
    {
        var config = TireWearDefaults.Balanced;
        var state = new TireWearState(
            wearFraction: 0.12f,
            temperatureC: 96f,
            smoothedSlipNormalized: 0.45f);

        var dry = TireWearRuntime.Step(
            config,
            state,
            new TireWearInput(
                elapsedSeconds: 20f,
                speedMps: 52f,
                slipAngleNormalized: 0.60f,
                lateralSlipNormalized: 0.45f,
                longitudinalSlipNormalized: 0.50f,
                loadNormalized: 0.55f,
                rollingResistanceNormalized: 0.34f,
                ambientTemperatureC: 30f,
                surfaceTemperatureC: 38f,
                wetnessNormalized: 0f));
        var wet = TireWearRuntime.Step(
            config,
            state,
            new TireWearInput(
                elapsedSeconds: 20f,
                speedMps: 52f,
                slipAngleNormalized: 0.60f,
                lateralSlipNormalized: 0.45f,
                longitudinalSlipNormalized: 0.50f,
                loadNormalized: 0.55f,
                rollingResistanceNormalized: 0.34f,
                ambientTemperatureC: 30f,
                surfaceTemperatureC: 38f,
                wetnessNormalized: 1f));

        wet.State.TemperatureC.Should().BeLessThan(dry.State.TemperatureC);
    }

    [Fact]
    public void Profiles_CreateFromVehicle_ShouldProduceDifferentWearModels()
    {
        var heavySport = TireWearProfiles.CreateFromVehicle(
            tireGripCoefficient: 1.08f,
            massKg: 1774f,
            tireCircumferenceM: 2.22f,
            lateralGripCoefficient: 1.05f);
        var lightTouring = TireWearProfiles.CreateFromVehicle(
            tireGripCoefficient: 0.88f,
            massKg: 865f,
            tireCircumferenceM: 1.83f,
            lateralGripCoefficient: 1.0f);

        heavySport.BaseWearPerKilometer.Should().NotBeApproximately(lightTouring.BaseWearPerKilometer, 0.00001f);
        heavySport.OptimalEndTemperatureC.Should().NotBeApproximately(lightTouring.OptimalEndTemperatureC, 0.0001f);
        heavySport.GripAtFullWear.Should().NotBeApproximately(lightTouring.GripAtFullWear, 0.0001f);
    }

    private static bool IsFinite(float value)
    {
        return !float.IsNaN(value) && !float.IsInfinity(value);
    }
}
