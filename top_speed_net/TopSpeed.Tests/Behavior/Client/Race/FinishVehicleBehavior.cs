using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using TopSpeed.Input;
using TopSpeed.Tracks;
using TopSpeed.Vehicles;
using TopSpeed.Vehicles.Control;
using TopSpeed.Vehicles.Core;
using TopSpeed.Vehicles.Physics;
using Xunit;

namespace TopSpeed.Tests;

[Trait("Category", "Behavior")]
public sealed class FinishVehicleBehaviorTests
{
    [Fact]
    public void Apply_DisablesDriveControls_AndStopsMotionImmediately()
    {
        var car = new RecordingCar { ManualTransmission = true };
        var controller = new RecordingController();

        TopSpeed.Drive.Session.FinishVehicle.Apply(car, controller);

        car.ManualTransmission.Should().BeFalse();
        car.OverrideController.Should().BeSameAs(controller);
        car.Calls.Should().Equal(
            "SetOverrideController",
            "SetNeutralGear",
            "Quiet",
            "ShutdownEngine",
            "StopMotionImmediately");
    }

    [Fact]
    public void FinishLockInputController_DoesNotApplyBrake()
    {
        var input = new DriveInput(new DriveSettings());
        input.SetTouchInputState(
            steering: 25,
            throttle: 100,
            brake: -100,
            clutch: 80,
            horn: true,
            gearUp: true,
            gearDown: true,
            startEngine: false);
        input.Run(new InputState(), 0f);
        var controller = new FinishLockInputController(input);

        var intent = controller.ReadIntent(new CarControlContext(
            CarState.Running,
            started: true,
            manualTransmission: false,
            gear: 1,
            speed: 120f,
            positionX: 0f,
            positionY: 1000f,
            elapsed: 0.016f));

        intent.Brake.Should().Be(0);
        intent.Clutch.Should().Be(80);
        intent.Horn.Should().BeTrue();
        intent.GearUp.Should().BeFalse();
        intent.GearDown.Should().BeFalse();
    }

    // The finish lock holds the car in neutral and never issues a gear change, so drive acceleration
    // is scaled by a coupling factor of 0 and the throttle cannot move the car. Passing it through is
    // what lets a finished player rev the engine while waiting for the rest of the field.
    [Fact]
    public void FinishLockInputController_PassesThrottleThroughForFreeRevving()
    {
        var input = new DriveInput(new DriveSettings());
        input.SetTouchInputState(
            steering: 25,
            throttle: 100,
            brake: -100,
            clutch: 80,
            horn: true,
            gearUp: true,
            gearDown: true,
            startEngine: false);
        input.Run(new InputState(), 0f);
        var controller = new FinishLockInputController(input);

        var intent = controller.ReadIntent(new CarControlContext(
            CarState.Running,
            started: true,
            manualTransmission: false,
            gear: 1,
            speed: 120f,
            positionX: 0f,
            positionY: 1000f,
            elapsed: 0.016f));

        intent.Throttle.Should().Be(100);
    }

    [Fact]
    public void RemoteMarkFinished_TransitionsToSettlingWithoutZeroingSpeed()
    {
        var player = (ComputerPlayer)RuntimeHelpers.GetUninitializedObject(typeof(ComputerPlayer));
        SetField(player, "_positionY", 900f);
        SetField(player, "_remoteTargetY", 900f);
        SetField(player, "_speed", 120f);
        SetField(player, "_remoteTargetSpeed", 120f);

        player.MarkFinished(1000f);

        player.Finished.Should().BeTrue();
        player.PositionY.Should().Be(1000f);
        player.Speed.Should().Be(120f);
        player.State.Should().Be(ComputerPlayer.ComputerState.Stopping);
        GetField<float>(player, "_remoteTargetY").Should().Be(1000f);
        GetField<float>(player, "_remoteTargetSpeed").Should().Be(0f);
    }

    [Fact]
    public void LocalBotStopAtFinish_TransitionsToSettlingAndClearsTransientSignals()
    {
        var player = (ComputerPlayer)RuntimeHelpers.GetUninitializedObject(typeof(ComputerPlayer));
        SetField(player, "_positionX", 1f);
        SetField(player, "_positionY", 900f);
        SetField(player, "_speed", 120f);
        SetField(player, "_speedDiff", 5f);
        SetField(player, "_remoteTargetX", 2f);
        SetField(player, "_remoteTargetY", 950f);
        SetField(player, "_remoteTargetSpeed", 120f);
        SetField(player, "_horning", true);

        player.StopAtFinish();

        player.Finished.Should().BeTrue();
        player.PositionY.Should().Be(900f);
        player.Speed.Should().Be(120f);
        player.State.Should().Be(ComputerPlayer.ComputerState.Stopping);
        GetField<float>(player, "_speedDiff").Should().Be(5f);
        GetField<float>(player, "_remoteTargetX").Should().Be(1f);
        GetField<float>(player, "_remoteTargetY").Should().Be(900f);
        GetField<float>(player, "_remoteTargetSpeed").Should().Be(0f);
        GetField<bool>(player, "_horning").Should().BeFalse();
    }

    private static void SetField<T>(ComputerPlayer player, string name, T value)
    {
        var field = typeof(ComputerPlayer).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
        field.Should().NotBeNull();
        field!.SetValue(player, value);
    }

    private static T GetField<T>(ComputerPlayer player, string name)
    {
        var field = typeof(ComputerPlayer).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
        field.Should().NotBeNull();
        return (T)field!.GetValue(player)!;
    }

    private sealed class RecordingController : ICarController
    {
        public CarControlIntent ReadIntent(in CarControlContext context) => CarControlIntent.Neutral;
    }

    private sealed class RecordingCar : ICar
    {
        public List<string> Calls { get; } = new();
        public ICarController? OverrideController { get; private set; }
        public CarState State => CarState.Running;
        public float PositionX => 0f;
        public float PositionY => 0f;
        public float Speed => 0f;
        public int Frequency => 0;
        public int Gear => 1;
        public bool InReverseGear => false;
        public bool ManualTransmission { get; set; }
        public bool ShiftOnDemandSupported => false;
        public bool ShiftOnDemandEnabled => false;
        public TopSpeed.Protocol.CarType CarType => TopSpeed.Protocol.CarType.Vehicle1;
        public ICarListener? Listener { get; set; }
        public bool CombustionActive => false;
        public bool EngineRunning => false;
        public bool Braking => false;
        public bool Horning => false;
        public bool UserDefined => false;
        public string? CustomFile => null;
        public string VehicleName => "test";
        public float WidthM => 2f;
        public float LengthM => 4f;
        public float MassKg => 1000f;
        public float LateralVelocityMps => 0f;
        public float SpeedKmh => 0f;
        public float EngineRpm => 0f;
        public float EngineHorsepower => 0f;
        public float EngineGrossHorsepower => 0f;
        public float EngineNetHorsepower => 0f;
        public float DistanceMeters => 0f;
        public float FuelLitersRemaining => 0f;
        public float FuelTankCapacityLiters => 0f;
        public float FuelBurnLitersPerHour => 0f;
        public float FuelEstimatedRangeMeters => 0f;
        public float FuelEfficiencyLitersPer100Km => 0f;
        public float FuelEfficiencyMpg => 0f;
        public bool FuelLow => false;
        public bool FuelEmpty => false;
        public bool FuelConsumptionEnabled => true;
        public bool TireWearEnabled => true;
        public void AddFuelLiters(float liters) { }
        public void ResetTireWear() { }
        public float TireWearPercent => 0f;
        public float TireTemperatureC => 0f;
        public float TireTemperaturePercent => 0f;
        public float TireColdEndTemperatureC => 32f;
        public float TireOptimalStartTemperatureC => 54f;
        public float TireOptimalEndTemperatureC => 78f;
        public float TireOverheatEndTemperatureC => 112f;

        public void Initialize(float positionX = 0, float positionY = 0) { }
        public void SetPosition(float positionX, float positionY) { }
        public void FinalizeCar() { }
        public void Start() { }
        public void RestartFromStall() { }
        public void RestartAfterCrash() { }
        public void ShutdownEngine() => Calls.Add(nameof(ShutdownEngine));
        public void StopMotionImmediately() => Calls.Add(nameof(StopMotionImmediately));
        public void Crash() { }
        public void MiniCrash(float newPosition) { }
        public void Bump(float bumpX, float bumpY, float speedDeltaKph) { }
        public void SetNeutralGear() => Calls.Add(nameof(SetNeutralGear));
        public void SetFirstGear() { }
        public int GetGearForSpeedKmh(float speedKmh) => 1;
        public void Stop() { }
        public void Quiet() => Calls.Add(nameof(Quiet));
        public void Run(float elapsed) { }
        public void BrakeSound() { }
        public void BrakeCurveSound() { }
        public void Evaluate(Track.Road road) { }
        public bool Backfiring() => false;
        public void Pause() { }
        public void Unpause() { }
        public void PrepareEngineForPitExit(float targetSpeedKmh) { }
        public void SetPrimaryController(ICarController controller) { }

        public void SetOverrideController(ICarController? controller)
        {
            OverrideController = controller;
            Calls.Add(nameof(SetOverrideController));
        }

        public void SetControlArbiter(IControlArbiter arbiter) { }
        public void SetModifiers(IReadOnlyList<ICarModifier>? modifiers) { }
        public void SetPhysicsModel(IModel model) { }
        public bool ToggleShiftOnDemand() => false;
        public void Dispose() { }
    }
}
