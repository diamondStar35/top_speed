using System.Collections.Generic;
using TopSpeed.Data;
using TopSpeed.Drive.Session;
using TopSpeed.Drive.Session.Systems;
using TopSpeed.Input;
using TopSpeed.Tracks;
using Xunit;

namespace TopSpeed.Tests;

[Trait("Category", "Behavior")]
public sealed class TrackAudioBehaviorTests
{
    [Fact]
    public void AnnounceNextRoad_Routes_Copilot_Callouts_To_Track_Info_Channel()
    {
        var settings = new DriveSettings
        {
            Copilot = CopilotMode.All
        };
        var soundIndexes = new List<int>();
        var queuedTrackInfoSounds = 0;
        var events = new List<Event>();
        var delays = new List<float>();
        var trackAudio = new TrackAudio(
            settings,
            index =>
            {
                soundIndexes.Add(index);
                return null;
            },
            loadRaceCueSound: null,
            turnEndDing: null,
            queueTrackInfoSound: _ => queuedTrackInfoSounds++,
            queueEvent: (sessionEvent, delay) =>
            {
                events.Add(sessionEvent);
                delays.Add(delay);
            });

        var currentRoad = new Track.Road
        {
            Type = TrackType.Straight,
            Surface = TrackSurface.Asphalt
        };
        var nextRoad = new Track.Road
        {
            Type = TrackType.Left,
            Surface = TrackSurface.Gravel
        };

        var announcedRoad = trackAudio.AnnounceNextRoad(currentRoad, nextRoad);

        announcedRoad.Should().Be(nextRoad);
        queuedTrackInfoSounds.Should().Be(1);
        soundIndexes.Should().Equal((int)TrackType.Left - 1, (int)TrackSurface.Gravel + 8);
        events.Should().ContainSingle();
        events[0].Id.Should().Be(Events.PlayTrackInfoSound);
        delays.Should().ContainSingle().Which.Should().Be(1.0f);
    }
}
