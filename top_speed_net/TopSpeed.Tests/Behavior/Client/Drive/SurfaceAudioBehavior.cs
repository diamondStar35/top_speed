using System;
using System.Numerics;
using TopSpeed.Data;
using TopSpeed.Drive.Session.Audio;
using TopSpeed.Vehicles;
using TopSpeed.Vehicles.Audio;
using Xunit;

namespace TopSpeed.Tests;

[Trait("Category", "Behavior")]
public sealed class SurfaceAudioBehaviorTests
{
    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public void SurfaceLoops_FollowEnginePosition(bool useHrtf, bool stereoWidening)
    {
        using var renderer = new AudioRenderingHarness(useHrtf);
        renderer.Output.IsHrtfActive.Should().Be(useHrtf);
        var factory = new RaceAudioFactory(renderer);
        using var audio = factory.CreatePlayer(VehicleLoader.LoadOfficial(0, default));
        var surfaces = new[] { audio.Asphalt, audio.Gravel, audio.Water, audio.Sand, audio.Snow };
        foreach (var surface in surfaces)
        {
            surface.CaptureSnapshot().IsSpatialized.Should().BeTrue(
                "surface loops must use the engine's positional audio path");
            surface.SetStereoWidening(stereoWidening);
        }
        audio.Engine.SetStereoWidening(stereoWidening);

        foreach (var offset in new[] { -1f, -0.5f, -0.1f, 0f, 0.1f, 0.5f, 1f })
        {
            var angle = offset * MathF.PI / 2f;
            var position = new Vector3(MathF.Sin(angle), 0f, MathF.Cos(angle));
            audio.Engine.SetTransform(position, Vector3.Zero);
            audio.UpdateSurfaceTransform(position, Vector3.Zero);
            audio.Engine.Play(loop: true);
            var engineBalance = renderer.MeasureVehicleBalance();
            audio.Engine.Stop();

            foreach (var surface in surfaces)
            {
                surface.Play(loop: true);
                var surfaceBalance = renderer.MeasureSurfaceBalance();
                surface.Stop();
                surfaceBalance.Should().BeApproximately(engineBalance, 0.04f,
                    "surface changes and coasting with the engine off must preserve the position cue");
            }
        }
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public void ShippedSurfaceLoops_RemainAudibleAndDirectionalWhileCoasting(bool useHrtf, bool stereoWidening)
    {
        using var renderer = new AudioRenderingHarness(useHrtf, useRecordings: true);
        var factory = new RaceAudioFactory(renderer);
        using var audio = factory.CreatePlayer(VehicleLoader.LoadOfficial(0, default));
        var surfaces = new[]
        {
            (TrackSurface.Asphalt, audio.Asphalt),
            (TrackSurface.Gravel, audio.Gravel),
            (TrackSurface.Water, audio.Water),
            (TrackSurface.Sand, audio.Sand),
            (TrackSurface.Snow, audio.Snow)
        };
        var flow = new Flow();

        foreach (var (kind, source) in surfaces)
        {
            source.SetStereoWidening(stereoWidening);
            var frequency = 0;
            var previousFrequency = 0;
            flow.UpdateRoad(kind, 80f, ref frequency, ref previousFrequency,
                audio.Asphalt, audio.Gravel, audio.Water, audio.Sand, audio.Snow);
            foreach (var side in new[] { -1, 1 })
            {
                audio.UpdateSurfaceTransform(new Vector3(side, 0f, 0f), Vector3.Zero);
                source.Play(loop: true);
                // HRTF conveys direction through arrival time even when low-frequency energy
                // is similar in both ears. Widening at the road edge can mute the far ear,
                // so use channel balance there instead of correlating a silent channel.
                var direction = useHrtf && !stereoWidening
                    ? -Math.Sign(renderer.MeasureSurfaceDelayFrames())
                    : Math.Sign(renderer.MeasureSurfaceBalance());
                direction.Should().Be(side,
                    "{0} should be heard on side {1}", kind, side);
                source.Stop();
            }
        }
        audio.Engine.IsPlaying.Should().BeFalse();
    }
}
