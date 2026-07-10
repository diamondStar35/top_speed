using System;
using TopSpeed.Physics.Powertrain;
using TopSpeed.Vehicles;
using TopSpeed.Vehicles.Control;
using Xunit;

namespace TopSpeed.Tests;

[Trait("Category", "Behavior")]
public sealed class PitLaneControllerBehaviorTests
{
    private const float TargetSpeedKmh = 56.33f;

    public static TheoryData<int> AllVehicles()
    {
        var data = new TheoryData<int>();
        for (var index = 0; index < OfficialVehicleCatalog.VehicleCount; index++)
            data.Add(index);
        return data;
    }

    [Theory]
    [MemberData(nameof(AllVehicles))]
    public void PitLaneCruise_ShouldHoldTargetSpeedWithoutClosingTheThrottle(int vehicleIndex)
    {
        var run = DrivePitLane(vehicleIndex, seconds: 20f);

        // A closed throttle is what arms the overrun backfire. Arriving from racing speed lifts
        // once; after that the proportional throttle law must settle on a steady partial opening
        // rather than chattering shut and open, which made backfire-equipped cars pop continuously.
        run.ThrottleLifts.Should().BeLessThanOrEqualTo(
            1,
            "the only closed-throttle event on pit road should be the single lift at pit entry");
        run.ClosedThrottleFramesWhileCruising.Should().Be(
            0,
            "settled pit-lane cruise should never close the throttle");
    }

    [Theory]
    [MemberData(nameof(AllVehicles))]
    public void PitLaneCruise_ShouldSettleWithinAboutOneKmhOfTarget(int vehicleIndex)
    {
        var run = DrivePitLane(vehicleIndex, seconds: 20f);

        run.MinCruiseSpeedKmh.Should().BeInRange(TargetSpeedKmh - 1.5f, TargetSpeedKmh + 1.5f);
        run.MaxCruiseSpeedKmh.Should().BeInRange(TargetSpeedKmh - 1.5f, TargetSpeedKmh + 1.5f);
        (run.MaxCruiseSpeedKmh - run.MinCruiseSpeedKmh).Should().BeLessThan(
            0.5f,
            "a settled cruise should not oscillate");
    }

    [Theory]
    [MemberData(nameof(AllVehicles))]
    public void PitLaneEntry_ShouldReachTargetSpeedInsideTheEntryWindow(int vehicleIndex)
    {
        // PitStop.EnteringLaneDurationSeconds is 15 s, after which BrakeMode forces the car to a
        // stop at the box. Heavy, slippery cars cannot shed racing speed on coast alone in that
        // window, which is why the brake stays in the controller for genuine overspeed.
        var run = DrivePitLane(vehicleIndex, seconds: 15f);

        run.SecondsToTargetSpeed.Should().BeLessThan(15f);
    }

    [Fact]
    public void ReadIntent_ShouldNeverCommandNegativeThrottle()
    {
        var spec = OfficialVehicleCatalog.Get(0);
        var controller = new PitLaneController(s => GearForSpeed(spec, s));

        for (var speedKmh = 0f; speedKmh <= 300f; speedKmh += 0.25f)
        {
            var intent = controller.ReadIntent(Context(speedKmh, GearForSpeed(spec, speedKmh)));

            intent.Throttle.Should().BeGreaterThanOrEqualTo(0, $"speed {speedKmh} km/h");
        }
    }

    [Fact]
    public void ReadIntent_ShouldOnlyBrakeForGenuineOverspeed()
    {
        var spec = OfficialVehicleCatalog.Get(0);
        var controller = new PitLaneController(s => GearForSpeed(spec, s));

        // Car.ApplyCoastDecel ignores any brake request weaker than 10%, so a brake command that
        // does not clear that gate is dead weight. Everything inside the coast band must be a pure
        // throttle command.
        controller.ReadIntent(Context(TargetSpeedKmh + 4f, 1)).Brake.Should().Be(0);
        controller.ReadIntent(Context(TargetSpeedKmh + 20f, 1)).Brake.Should().BeLessThan(-10);
    }

    private static CarControlContext Context(float speedKmh, int gear)
    {
        return new CarControlContext(
            CarState.Running,
            started: true,
            manualTransmission: false,
            gear: gear,
            speed: speedKmh,
            positionX: 0f,
            positionY: 0f,
            elapsed: 1f / 60f);
    }

    private static PitLaneRun DrivePitLane(int vehicleIndex, float seconds)
    {
        var spec = OfficialVehicleCatalog.Get(vehicleIndex);
        var config = PowertrainHarness.BuildConfig(spec);
        var controller = new PitLaneController(s => GearForSpeed(spec, s));

        const float dt = 1f / 60f;
        var frames = (int)(seconds / dt);
        var cruiseFromFrame = (int)(frames * 0.75f);

        var speedKmh = Math.Max(120f, spec.TopSpeed * 0.8f);
        var lifts = 0;
        var closedWhileCruising = 0;
        var throttleWasOpen = true;
        var minCruise = float.MaxValue;
        var maxCruise = float.MinValue;
        var secondsToTarget = float.MaxValue;

        for (var frame = 0; frame < frames; frame++)
        {
            var gear = GearForSpeed(spec, speedKmh);
            var intent = controller.ReadIntent(Context(speedKmh, gear));
            var thrust = LongitudinalStep.ResolveThrust(intent.Throttle, intent.Brake);

            // Car.CanApplyThrottleDrive gates the drive path at thrust > 10 for an automatic.
            var driving = thrust > 10;
            var throttleClosed = thrust <= 0;
            if (throttleClosed && throttleWasOpen)
                lifts++;
            throttleWasOpen = !throttleClosed;

            if (frame >= cruiseFromFrame)
            {
                if (throttleClosed)
                    closedWhileCruising++;
                minCruise = Math.Min(minCruise, speedKmh);
                maxCruise = Math.Max(maxCruise, speedKmh);
            }

            if (secondsToTarget == float.MaxValue && speedKmh <= TargetSpeedKmh + 1.5f)
                secondsToTarget = frame * dt;

            var speedMps = speedKmh / 3.6f;
            var rpm = Clamp(Calculator.RpmAtSpeed(config, speedMps, gear), config.IdleRpm, config.RevLimiter);
            var result = LongitudinalStep.Compute(new LongitudinalStepInput(
                config,
                dt,
                speedMps,
                throttle: intent.Throttle / 100f,
                brake: Math.Max(0f, -intent.Brake) / 100f,
                surfaceTractionModifier: 1f,
                surfaceBrakeModifier: 1f,
                surfaceRollingResistanceModifier: 1f,
                longitudinalGripFactor: 1f,
                gear: gear,
                inReverse: false,
                isNeutral: false,
                transmissionType: spec.PrimaryTransmissionType,
                drivelineCouplingFactor: 1f,
                creepAccelerationMps2: 0f,
                currentEngineRpm: rpm,
                requestDrive: driving,
                requestBrake: thrust < -10,
                applyEngineBraking: !driving,
                resistanceEnvironment: ResistanceEnvironment.Calm));
            speedKmh = Math.Max(0f, speedKmh + result.SpeedDeltaKph);
        }

        return new PitLaneRun(lifts, closedWhileCruising, minCruise, maxCruise, secondsToTarget);
    }

    private static int GearForSpeed(OfficialVehicleSpec spec, float speedKmh)
    {
        for (var i = 0; i < spec.GearRatios.Length; i++)
        {
            var gearMaxKmh = (spec.RevLimiter / 60f)
                * spec.TireCircumferenceM
                / (spec.GearRatios[i] * spec.FinalDriveRatio)
                * 3.6f;
            if (speedKmh <= gearMaxKmh)
                return i + 1;
        }
        return spec.GearRatios.Length;
    }

    private static float Clamp(float value, float min, float max)
    {
        if (value < min) return min;
        if (value > max) return max;
        return value;
    }

    private sealed record PitLaneRun(
        int ThrottleLifts,
        int ClosedThrottleFramesWhileCruising,
        float MinCruiseSpeedKmh,
        float MaxCruiseSpeedKmh,
        float SecondsToTargetSpeed);
}
