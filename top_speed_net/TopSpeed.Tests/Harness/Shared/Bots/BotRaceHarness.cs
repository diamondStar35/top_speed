using System;
using System.Collections.Generic;
using TopSpeed.Bots;
using TopSpeed.Collision;
using TopSpeed.Data;
using TopSpeed.Protocol;

namespace TopSpeed.Tests;

/// <summary>
/// Closed-loop, headless race simulation: road model -> planner -> bot physics -> collisions ->
/// road model. The existing <see cref="BotPhysicsHarness"/> only feeds constant pedal inputs, so
/// it can prove the car integrates correctly but never that the driver drives well. This one runs
/// real bots over real tracks and reports what actually happened, which is the only way to tell a
/// better driver from a differently-broken one.
/// </summary>
internal sealed class BotRaceHarness
{
    /// <summary>Matches the dedicated server's fixed simulation step.</summary>
    public const float StepSeconds = 0.008f;

    private readonly RoadModel _road;
    private readonly List<Runner> _runners = new();
    /// <summary>
    /// Per-pair re-arm times. A single graze separates and re-overlaps over several ticks, so a
    /// plain "are they overlapping" debounce reports one incident as four. Counting incidents, not
    /// ticks, is what makes the collision metric mean anything.
    /// </summary>
    private readonly Dictionary<long, float> _contactRearmAt = new();
    private const float ContactRearmSeconds = 1.5f;
    private readonly bool _collisionsEnabled;

    public BotRaceHarness(TrackDefinition[] definitions, float laneHalfWidth = RoadModel.DefaultLaneHalfWidth, bool collisionsEnabled = true)
    {
        _road = new RoadModel(definitions, laneHalfWidth);
        _collisionsEnabled = collisionsEnabled;
    }

    private float _clock;

    public RoadModel Road => _road;
    public IReadOnlyList<Runner> Runners => _runners;

    public static BotRaceHarness ForBuiltInTrack(string trackName, bool collisionsEnabled = true)
    {
        var track = TrackCatalog.BuiltIn[trackName];
        return new BotRaceHarness(track.Definitions, collisionsEnabled: collisionsEnabled);
    }

    /// <summary>A single stretch of road repeated, for isolating one corner type.</summary>
    public static BotRaceHarness ForUniformTrack(
        TrackType type,
        int segments = 8,
        float segmentLength = 400f,
        TrackSurface surface = TrackSurface.Asphalt,
        float laneHalfWidth = RoadModel.DefaultLaneHalfWidth)
    {
        var definitions = new TrackDefinition[segments];
        for (var i = 0; i < segments; i++)
            definitions[i] = new TrackDefinition(type, surface, TrackNoise.NoNoise, segmentLength);
        return new BotRaceHarness(definitions, laneHalfWidth);
    }

    /// <summary>
    /// A straight too narrow for two cars to sit side by side, so a following bot has no choice
    /// but to actually follow. Without this, a faster bot simply drives around the car in front
    /// and the car-following behaviour never gets exercised.
    /// </summary>
    public static BotRaceHarness ForSingleFileStraight() => ForUniformTrack(TrackType.Straight, laneHalfWidth: 2.0f);

    public Runner AddBot(
        BotDrivingDifficulty difficulty,
        CarType car = CarType.Vehicle1,
        float positionY = 0f,
        float positionX = float.NaN,
        float speedKph = 0f,
        uint id = 0u)
    {
        var config = BotPhysicsCatalog.Get(car);
        var seg = _road.At(positionY);
        var runner = new Runner(
            id == 0u ? (uint)(_runners.Count + 1) : id,
            difficulty,
            config,
            _road);

        runner.PositionY = positionY;
        runner.PositionX = float.IsNaN(positionX) ? (seg.Left + seg.Right) * 0.5f : positionX;
        runner.SpeedKph = speedKph;
        runner.SyncPhysicsFromPose();
        _runners.Add(runner);
        return runner;
    }

    /// <summary>
    /// A car that holds a fixed speed and line. Useful as a rolling roadblock when testing whether
    /// a bot overtakes rather than piling into it.
    /// </summary>
    public Runner AddPaceCar(float positionY, float positionX, float speedKph, bool isHuman = false, uint id = 0u)
    {
        var runner = AddBot(BotDrivingDifficulty.Normal, CarType.Vehicle1, positionY, positionX, speedKph, id);
        runner.IsScripted = true;
        runner.IsHuman = isHuman;
        return runner;
    }

    public BotRaceReport Run(float seconds)
    {
        var steps = (int)Math.Round(seconds / StepSeconds);
        for (var i = 0; i < steps; i++)
            Step();
        return BuildReport(seconds);
    }

    public void Step()
    {
        _clock += StepSeconds;
        var traffic = BuildTraffic();

        for (var i = 0; i < _runners.Count; i++)
        {
            var runner = _runners[i];
            if (runner.IsScripted)
            {
                runner.AdvanceScripted(StepSeconds);
                continue;
            }
            runner.Advance(StepSeconds, traffic);
        }

        if (_collisionsEnabled)
            ResolveCollisions();
    }

    private BotVehicleObservation[] BuildTraffic()
    {
        var traffic = new BotVehicleObservation[_runners.Count];
        for (var i = 0; i < _runners.Count; i++)
        {
            var runner = _runners[i];
            traffic[i] = new BotVehicleObservation(
                runner.Id,
                runner.IsHuman,
                runner.PositionX,
                runner.PositionY,
                runner.SpeedKph,
                runner.WidthM,
                runner.LengthM,
                runner.PhysicsState.LateralVelocityMps);
        }
        return traffic;
    }

    /// <summary>
    /// Mirrors the hosts: all-pairs overlap test, debounced so one sustained overlap counts once.
    /// </summary>
    private void ResolveCollisions()
    {
        for (var i = 0; i < _runners.Count; i++)
        {
            for (var j = i + 1; j < _runners.Count; j++)
            {
                var a = _runners[i];
                var b = _runners[j];
                var key = ((long)a.Id << 32) | b.Id;
                var bodyA = a.ToCollisionBody();
                var bodyB = b.ToCollisionBody();

                if (!VehicleCollisionResolver.TryResolve(in bodyA, in bodyB, out var response))
                    continue;

                if (_contactRearmAt.TryGetValue(key, out var rearmAt) && _clock < rearmAt)
                    continue;
                _contactRearmAt[key] = _clock + ContactRearmSeconds;

                a.ApplyBump(response.First);
                b.ApplyBump(response.Second);
                a.Collisions++;
                b.Collisions++;
            }
        }
    }

    private BotRaceReport BuildReport(float seconds)
    {
        var collisions = 0;
        var offRoad = 0;
        var fullCrashes = 0;
        var brakeTicks = 0;
        var totalTicks = 0;
        var minSpeed = float.MaxValue;
        var minDistance = float.MaxValue;

        for (var i = 0; i < _runners.Count; i++)
        {
            var runner = _runners[i];
            if (runner.IsScripted)
                continue;
            collisions += runner.Collisions;
            offRoad += runner.OffRoadEvents;
            fullCrashes += runner.FullCrashes;
            brakeTicks += runner.BrakeTicks;
            totalTicks += runner.Ticks;
            if (runner.DistanceTravelled < minDistance)
                minDistance = runner.DistanceTravelled;
            if (runner.MinSpeedAfterLaunchKph < minSpeed)
                minSpeed = runner.MinSpeedAfterLaunchKph;
        }

        return new BotRaceReport(
            Seconds: seconds,
            Collisions: collisions / 2,
            OffRoadEvents: offRoad,
            FullCrashes: fullCrashes,
            BrakeDutyCycle: totalTicks == 0 ? 0f : (float)brakeTicks / totalTicks,
            MinDistanceTravelledM: minDistance == float.MaxValue ? 0f : minDistance,
            MinSpeedAfterLaunchKph: minSpeed == float.MaxValue ? 0f : minSpeed);
    }

    internal sealed class Runner
    {
        private readonly RoadModel _road;
        private readonly BotPhysicsConfig _config;
        private readonly BotCapabilities _capabilities;
        private readonly BotRoadPreview[] _preview = BotRoadSampling.CreatePreview();
        private readonly float[] _distances = BotRoadSampling.CreateDistances();
        private readonly BotDrivingDifficulty _difficulty;
        private BotDriverState _driverState;
        private float _previewRefresh;
        private float _startY;
        private bool _startCaptured;

        public Runner(uint id, BotDrivingDifficulty difficulty, BotPhysicsConfig config, RoadModel road)
        {
            Id = id;
            _difficulty = difficulty;
            _config = config;
            _road = road;
            _capabilities = BotCapabilities.From(config);
            PhysicsState = new BotPhysicsState { Gear = 1 };
        }

        public uint Id { get; }
        public bool IsHuman { get; set; }
        public bool IsScripted { get; set; }
        public float PositionX { get; set; }
        public float PositionY { get; set; }
        public float SpeedKph { get; set; }
        public BotPhysicsState PhysicsState;

        public float WidthM => _config.WidthM;
        public float LengthM => _config.LengthM;

        public int Collisions { get; set; }
        public int OffRoadEvents { get; private set; }
        public int FullCrashes { get; private set; }
        public int BrakeTicks { get; private set; }
        public int Ticks { get; private set; }
        public float MaxLaneDeviation { get; private set; }
        public float MinSpeedAfterLaunchKph { get; private set; } = float.MaxValue;
        public float LastTargetSpeedKph { get; private set; }
        public float LastSteering { get; private set; }
        public float LastThrottle { get; private set; }
        public float LastBrake { get; private set; }
        public BotManeuver LastManeuver { get; private set; }
        public float DistanceTravelled => _startCaptured ? PositionY - _startY : 0f;

        public void SyncPhysicsFromPose()
        {
            var state = PhysicsState;
            state.PositionX = PositionX;
            state.PositionY = PositionY;
            state.SpeedKph = SpeedKph;
            if (state.Gear < 1)
                state.Gear = 1;
            PhysicsState = state;
        }

        public void AdvanceScripted(float elapsed)
        {
            PositionY += (SpeedKph / 3.6f) * elapsed;
            SyncPhysicsFromPose();
        }

        public void Advance(float elapsed, BotVehicleObservation[] traffic)
        {
            if (!_startCaptured)
            {
                _startY = PositionY;
                _startCaptured = true;
            }

            RefreshPreview(elapsed);

            var ego = new BotEgoState(
                PositionX,
                PositionY,
                SpeedKph,
                PhysicsState.LateralVelocityMps,
                PhysicsState.YawRateRad,
                PhysicsState.Gear,
                PhysicsState.EffectiveDriveRatio);
            var capabilities = _capabilities;
            var input = new BotDrivingInput(
                _difficulty,
                Id * 2654435761u,
                Id,
                elapsed,
                in ego,
                in capabilities,
                _preview,
                traffic);

            var control = BotDrivingPlanner.Step(ref _driverState, in input);
            LastTargetSpeedKph = control.TargetSpeedKph;
            LastSteering = control.Steering;
            LastThrottle = control.Throttle;
            LastBrake = control.Brake;
            LastManeuver = control.Maneuver;

            Ticks++;
            if (control.Braking)
                BrakeTicks++;

            var road = _road.At(PositionY);
            var state = PhysicsState;
            state.PositionX = PositionX;
            state.PositionY = PositionY;
            state.SpeedKph = SpeedKph;
            var physicsInput = new BotPhysicsInput(
                elapsed,
                road.Surface,
                (int)Math.Round(control.Throttle),
                (int)Math.Round(control.Brake),
                (int)Math.Round(control.Steering),
                ambientTemperatureC: float.NaN,
                rainGain: 0f,
                stormGain: 0f,
                windGain: 0f);
            BotPhysics.Step(_config, ref state, in physicsInput);
            PhysicsState = state;
            PositionX = state.PositionX;
            PositionY = state.PositionY;
            SpeedKph = state.SpeedKph;

            EvaluateRoad();
        }

        private void RefreshPreview(float elapsed)
        {
            _previewRefresh -= elapsed;
            var rebuild = _previewRefresh <= 0f;
            if (rebuild)
            {
                _previewRefresh = BotRoadSampling.RefreshIntervalSeconds;
                BotRoadSampling.FillDistances(SpeedKph, _distances);
            }

            var count = rebuild ? _preview.Length : 1;
            for (var i = 0; i < count; i++)
            {
                var distance = _distances[i];
                var sample = _road.At(PositionY + distance);
                _preview[i] = new BotRoadPreview(
                    distance,
                    sample.Left,
                    sample.Right,
                    sample.Surface,
                    sample.Type,
                    _road.CenterDriftPerMeter(sample.Type),
                    Math.Max(1f, sample.Length - sample.RelPos));
            }
        }

        /// <summary>Mirrors the hosts' off-road rule, including the crash and mini-crash reset.</summary>
        private void EvaluateRoad()
        {
            var road = _road.At(PositionY);
            var halfWidth = Math.Max(0.1f, Math.Abs(road.Right - road.Left) * 0.5f);
            var center = BotRaceRules.RoadCenter(road.Left, road.Right);
            var deviation = Math.Abs(PositionX - center) / halfWidth;
            if (deviation > MaxLaneDeviation)
                MaxLaneDeviation = deviation;

            if (SpeedKph > 30f && SpeedKph < MinSpeedAfterLaunchKph)
                MinSpeedAfterLaunchKph = SpeedKph;

            var relPos = BotRaceRules.CalculateRelativeLanePosition(PositionX, road.Left, halfWidth);
            if (!BotRaceRules.IsOutsideRoad(relPos))
                return;

            OffRoadEvents++;
            var state = PhysicsState;
            if (BotRaceRules.IsFullCrash(state.Gear, SpeedKph))
            {
                FullCrashes++;
                state.SpeedKph = 0f;
                state.Gear = 1;
                state.AutoShiftCooldownSeconds = 0f;
                SpeedKph = 0f;
                _driverState = default;
            }
            else
            {
                state.SpeedKph /= 4f;
                SpeedKph = Math.Max(0f, state.SpeedKph);
            }

            state.PositionX = center;
            state.LateralVelocityMps = 0f;
            state.YawRateRad = 0f;
            PositionX = center;
            PhysicsState = state;
        }

        public VehicleCollisionBody ToCollisionBody()
            => new VehicleCollisionBody(PositionX, PositionY, SpeedKph, WidthM, LengthM, _config.MassKg);

        public void ApplyBump(in VehicleCollisionImpulse impulse)
        {
            PositionX += 2f * impulse.BumpX;
            PositionY = Math.Max(0f, PositionY + impulse.BumpY);
            SpeedKph = Math.Max(0f, SpeedKph + impulse.SpeedDeltaKph);

            var state = PhysicsState;
            state.PositionX = PositionX;
            state.PositionY = PositionY;
            state.SpeedKph = SpeedKph;
            state.LateralVelocityMps = 0f;
            state.YawRateRad = 0f;
            PhysicsState = state;

            BotDrivingPlanner.NotifyContact(ref _driverState);
        }
    }
}

internal readonly record struct BotRaceReport(
    float Seconds,
    int Collisions,
    int OffRoadEvents,
    int FullCrashes,
    float BrakeDutyCycle,
    float MinDistanceTravelledM,
    float MinSpeedAfterLaunchKph);
