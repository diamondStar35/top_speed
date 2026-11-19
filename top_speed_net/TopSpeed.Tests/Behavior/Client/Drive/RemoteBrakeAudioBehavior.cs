using TopSpeed.Data;
using TopSpeed.Drive.Session.Audio;
using TopSpeed.Vehicles;
using Xunit;

namespace TopSpeed.Tests;

[Trait("Category", "Behavior")]
[Collection("Audio rendering")]
public sealed class RemoteBrakeAudioBehaviorTests
{
    [Theory]
    [InlineData(TrackSurface.Snow)]
    [InlineData(TrackSurface.Gravel)]
    [InlineData(TrackSurface.Sand)]
    [InlineData(TrackSurface.Water)]
    public void BrakingOnLooseSurfaces_ShouldNotPlayAsphaltSqueal(TrackSurface surface)
    {
        using var renderer = new AudioRenderingHarness(false);
        using var audio = new RaceAudioFactory(renderer).CreateRemote(VehicleLoader.LoadOfficial(0, default));
        audio.UpdateBraking(true, surface);
        audio.Brake.IsPlaying.Should().BeFalse();
    }

    [Fact]
    public void EnteringSnowWhileBraking_ShouldStopExistingAsphaltSqueal()
    {
        using var renderer = new AudioRenderingHarness(false);
        using var audio = new RaceAudioFactory(renderer).CreateRemote(VehicleLoader.LoadOfficial(0, default));
        audio.UpdateBraking(true, TrackSurface.Asphalt);
        audio.Brake.IsPlaying.Should().BeTrue();
        audio.UpdateBraking(true, TrackSurface.Snow);
        audio.Brake.IsPlaying.Should().BeFalse();
        audio.UpdateBraking(true, TrackSurface.Asphalt);
        audio.Brake.IsPlaying.Should().BeTrue();
        audio.UpdateBraking(false, TrackSurface.Asphalt);
        audio.Brake.IsPlaying.Should().BeFalse();
    }
}
