using System;
using TopSpeed.Drive.Session.Audio;
using TopSpeed.Input;
using TopSpeed.Vehicles;
using TopSpeed.Vehicles.Audio;
using Xunit;

namespace TopSpeed.Tests;

[Trait("Category", "Behavior")]
[Collection("Audio rendering")]
public sealed class SurfaceVolumeBehaviorTests
{
    [Theory]
    [InlineData(-50)]
    [InlineData(-10)]
    [InlineData(-1)]
    [InlineData(1)]
    [InlineData(10)]
    [InlineData(50)]
    public void PartialSteering_NeverReducesSurfaceAudioBelowStraightDriving(int steering)
    {
        using var fixture = new Fixture();
        fixture.Step(steering);
        foreach (var source in fixture.Sources)
            source.GetVolume().Should().BeInRange(0.9f, 0.95f,
                "partial steering must preserve the continuous surface cue on every surface");
    }

    [Theory]
    [InlineData(-100, 0.95f)]
    [InlineData(-50, 0.925f)]
    [InlineData(0, 0.9f)]
    [InlineData(50, 0.925f)]
    [InlineData(100, 0.95f)]
    public void SteeringVolume_SettlesAtSmallProportionalBoost(int steering, float expected)
    {
        using var fixture = new Fixture();
        fixture.Step(steering, elapsed: 1f);
        foreach (var source in fixture.Sources)
            source.GetVolume().Should().BeApproximately(expected, 0.00001f);
    }

    [Fact]
    public void BinaryInput_SmoothsBothTurningAndReturnToCenter()
    {
        using var fixture = new Fixture();
        fixture.Step(100);
        var firstStep = fixture.Audio.Asphalt.GetVolume();
        firstStep.Should().BeGreaterThan(0.9f).And.BeLessThan(0.915f);
        fixture.Step(100, elapsed: 1f);
        fixture.Step(0);
        fixture.Audio.Asphalt.GetVolume().Should().BeGreaterThan(0.935f).And.BeLessThan(0.95f);
        fixture.Step(0, elapsed: 1f);
        fixture.Audio.Asphalt.GetVolume().Should().BeApproximately(0.9f, 0.00001f);
    }

    [Fact]
    public void Smoothing_IsIndependentOfFrameRate()
    {
        static float Simulate(int frameRate)
        {
            using var fixture = new Fixture();
            for (var frame = 0; frame < frameRate / 10; frame++)
                fixture.Step(100, elapsed: 1f / frameRate);
            return fixture.Audio.Asphalt.GetVolume();
        }
        Simulate(30).Should().BeApproximately(Simulate(120), 0.000001f);
    }

    [Theory]
    [InlineData(0, 0f)]
    [InlineData(35, 0.3325f)]
    [InlineData(100, 0.95f)]
    public void VolumeSetting_StillScalesAndMutesSteeringCue(int percent, float expected)
    {
        using var fixture = new Fixture();
        fixture.Step(100, elapsed: 1f);
        fixture.Settings.AudioVolumes.SurfaceLoopsPercent = percent;
        fixture.Step(100);
        foreach (var source in fixture.Sources)
            source.GetVolume().Should().BeApproximately(expected, 0.00001f);
    }

    [Fact]
    public void SurfaceChangesAndDirectionReversal_KeepTheSameSmoothedVolume()
    {
        using var fixture = new Fixture();
        fixture.Step(100, elapsed: 1f);
        fixture.Step(-100);
        foreach (var source in fixture.Sources)
            source.GetVolume().Should().BeApproximately(0.95f, 0.00001f);
        fixture.Step(0);
        var fadingVolume = fixture.Audio.Asphalt.GetVolume();
        foreach (var source in fixture.Sources)
            source.GetVolume().Should().Be(fadingVolume);
    }

    [Theory]
    [InlineData(0f, 0.9f)]
    [InlineData(10f, 0.92f)]
    [InlineData(50f, 1f)]
    [InlineData(200f, 1f)]
    public void Braking_PreservesLooseSurfaceVolume(float speed, float expected)
    {
        using var fixture = new Fixture();
        fixture.Step(0, speed: speed, braking: true);
        fixture.Audio.Asphalt.GetVolume().Should().BeApproximately(0.9f, 0.00001f);
        foreach (var source in fixture.Sources[1..])
            source.GetVolume().Should().BeApproximately(expected, 0.00001f);
    }

    [Fact]
    public void BrakingAndSteering_DoNotLowerTheCueOrExceedTheVolumeSetting()
    {
        using var fixture = new Fixture();
        fixture.Settings.AudioVolumes.SurfaceLoopsPercent = 50;
        fixture.Step(100, elapsed: 1f, speed: 10f, braking: true);
        foreach (var source in fixture.Sources)
            source.GetVolume().Should().BeApproximately(0.475f, 0.00001f);
        fixture.Step(100, speed: 200f, braking: true);
        foreach (var source in fixture.Sources)
            source.GetVolume().Should().BeLessThanOrEqualTo(0.5f);
    }

    [Fact]
    public void Reset_RestoresStraightDrivingVolume()
    {
        using var fixture = new Fixture();
        fixture.Step(100, elapsed: 1f);
        fixture.Volume.Reset();
        fixture.Step(0);
        foreach (var source in fixture.Sources)
            source.GetVolume().Should().Be(0.9f);
    }

    private sealed class Fixture : IDisposable
    {
        private readonly AudioRenderingHarness _renderer = new(useHrtf: false);
        public PlayerVehicleAudio Audio { get; }
        public DriveSettings Settings { get; } = new();
        public SurfaceVolume Volume { get; } = new();
        public TS.Audio.Source[] Sources { get; }

        public Fixture()
        {
            Settings.AudioVolumes.SurfaceLoopsPercent = 100;
            Audio = new RaceAudioFactory(_renderer).CreatePlayer(VehicleLoader.LoadOfficial(0, default));
            Sources = new[] { Audio.Asphalt, Audio.Gravel, Audio.Water, Audio.Sand, Audio.Snow };
        }

        public void Step(int steering, float elapsed = 1f / 60f, float speed = 200f, bool braking = false)
        {
            Volume.Update(steering, elapsed);
            Volume.Apply(Settings, Audio, speed, braking);
        }

        public void Dispose()
        {
            Audio.Dispose();
            _renderer.Dispose();
        }
    }
}
