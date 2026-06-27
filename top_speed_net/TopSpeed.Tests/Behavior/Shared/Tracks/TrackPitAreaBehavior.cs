using System;
using System.IO;
using System.Linq;
using TopSpeed.Data;
using Xunit;

namespace TopSpeed.Tests;

[Trait("Category", "Behavior")]
public sealed class TrackPitAreaBehaviorTests
{
    private const string WeatherAndSegment =
        """

        [weather:clear]
        kind = sunny

        [segment:one]
        type = straight
        surface = asphalt
        noise = none
        length = 100
        """;

    [Fact]
    public void Parser_WithoutDirective_ShouldHavePitArea()
    {
        using var temp = new TemporaryTrackFile(
            "[meta]\nname = No Directive\nweather = clear\nambience = noambience\n" + WeatherAndSegment);

        var loaded = TrackTsmParser.TryLoadFromFile(temp.Path, out var track, out var issues);

        loaded.Should().BeTrue(string.Join(Environment.NewLine, issues.Select(i => i.ToString())));
        track.HasPitArea.Should().BeTrue();
    }

    [Fact]
    public void Parser_PitAreaFalse_ShouldDisablePitArea()
    {
        using var temp = new TemporaryTrackFile(
            "[meta]\nname = No Pit\nweather = clear\nambience = noambience\npit_area = false\n" + WeatherAndSegment);

        var loaded = TrackTsmParser.TryLoadFromFile(temp.Path, out var track, out var issues);

        loaded.Should().BeTrue(string.Join(Environment.NewLine, issues.Select(i => i.ToString())));
        track.HasPitArea.Should().BeFalse();
    }

    [Fact]
    public void Parser_PitAreaTrue_ShouldHavePitArea()
    {
        using var temp = new TemporaryTrackFile(
            "[meta]\nname = Has Pit\nweather = clear\nambience = noambience\npit_area = true\n" + WeatherAndSegment);

        var loaded = TrackTsmParser.TryLoadFromFile(temp.Path, out var track, out _);

        loaded.Should().BeTrue();
        track.HasPitArea.Should().BeTrue();
    }

    [Fact]
    public void Parser_PitAreaFalse_WithPitSegment_ShouldWarnAndDisablePitArea()
    {
        using var temp = new TemporaryTrackFile(
            """
            [meta]
            name = Contradiction
            weather = clear
            ambience = noambience
            pit_area = false

            [weather:clear]
            kind = sunny

            [segment:one]
            type = straight
            surface = asphalt
            noise = none
            length = 100
            pit = pitentry

            [segment:two]
            type = straight
            surface = asphalt
            noise = none
            length = 100
            pit = pitexit
            """);

        var loaded = TrackTsmParser.TryLoadFromFile(temp.Path, out var track, out var issues);

        // Directive wins: the track still loads (warning, not error), pitting is disabled.
        loaded.Should().BeTrue(string.Join(Environment.NewLine, issues.Select(i => i.ToString())));
        track.HasPitArea.Should().BeFalse();
        issues.Should().Contain(i =>
            i.Severity == TrackTsmIssueSeverity.Warning
            && i.Message.Contains("pit segments are ignored", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Parser_SegmentWithBothPitMarkers_ShouldBeEntryAndExit()
    {
        using var temp = new TemporaryTrackFile(
            """
            [meta]
            name = Combined Pit
            weather = clear
            ambience = noambience

            [weather:clear]
            kind = sunny

            [segment:one]
            type = straight
            surface = asphalt
            noise = none
            length = 100
            pit = pit_entry
            pit = pit_exit
            """);

        var loaded = TrackTsmParser.TryLoadFromFile(temp.Path, out var track, out var issues);

        loaded.Should().BeTrue(string.Join(Environment.NewLine, issues.Select(i => i.ToString())));
        var segment = track.Definitions.Single();
        segment.IsPitEntry.Should().BeTrue();
        segment.IsPitExit.Should().BeTrue();
    }

    [Fact]
    public void Parser_SegmentWithBothPitMarkers_OrderIndependent()
    {
        using var temp = new TemporaryTrackFile(
            """
            [meta]
            name = Combined Pit Reversed
            weather = clear
            ambience = noambience

            [weather:clear]
            kind = sunny

            [segment:one]
            type = straight
            surface = asphalt
            noise = none
            length = 100
            pit = pit_exit
            pit = pit_entry
            """);

        var loaded = TrackTsmParser.TryLoadFromFile(temp.Path, out var track, out var issues);

        loaded.Should().BeTrue(string.Join(Environment.NewLine, issues.Select(i => i.ToString())));
        var segment = track.Definitions.Single();
        segment.IsPitEntry.Should().BeTrue();
        segment.IsPitExit.Should().BeTrue();
    }

    [Fact]
    public void Parser_PitAreaInvalidValue_ShouldReject()
    {
        using var temp = new TemporaryTrackFile(
            "[meta]\nname = Bad Bool\nweather = clear\nambience = noambience\npit_area = banana\n" + WeatherAndSegment);

        var loaded = TrackTsmParser.TryLoadFromFile(temp.Path, out _, out var issues);

        loaded.Should().BeFalse();
        issues.Should().Contain(i => i.Message.Contains("Invalid boolean", StringComparison.OrdinalIgnoreCase));
    }

    private sealed class TemporaryTrackFile : IDisposable
    {
        private readonly string _directory;
        public TemporaryTrackFile(string content)
        {
            _directory = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "topspeed-track-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_directory);
            Path = System.IO.Path.Combine(_directory, "track.tsm");
            File.WriteAllText(Path, content.Replace("\r\n", "\n").Replace("\n", Environment.NewLine));
        }

        public string Path { get; }

        public void Dispose()
        {
            if (Directory.Exists(_directory))
                Directory.Delete(_directory, recursive: true);
        }
    }
}
