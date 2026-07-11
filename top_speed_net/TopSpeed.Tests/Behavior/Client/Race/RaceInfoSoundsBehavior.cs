using TopSpeed.Drive.Session;
using Xunit;

namespace TopSpeed.Tests;

[Trait("Category", "Behavior")]
public sealed class RaceInfoSoundsBehaviorTests
{
    [Theory]
    [InlineData(0, 3, false)]   // first of three
    [InlineData(1, 3, false)]   // second of three
    [InlineData(2, 3, true)]    // third of three is last
    [InlineData(0, 2, false)]   // winner of a two-car race
    [InlineData(1, 2, true)]    // runner-up of a two-car race is last
    [InlineData(8, 10, false)]  // ninth of ten is a numbered position
    [InlineData(9, 10, true)]   // tenth of ten is last
    public void IsLastPlace_ShouldOnlyFlagTheFinalFinisher(int finishIndex, int totalRacers, bool expected)
    {
        RaceInfoSounds.IsLastPlace(finishIndex, totalRacers).Should().Be(expected);
    }

    [Fact]
    public void IsLastPlace_SoloRun_ShouldNotAnnounceLastPlace()
    {
        // Winning alone is a win, not a last place.
        RaceInfoSounds.IsLastPlace(finishIndex: 0, totalRacers: 1).Should().BeFalse();
    }

    [Theory]
    [InlineData(0, "race\\info\\finished1")]
    [InlineData(2, "race\\info\\finished3")]
    [InlineData(7, "race\\info\\finished8")]
    [InlineData(8, "race\\info\\finished9")]
    public void NumberedFinishedKey_ShouldNameTheOneBasedPosition(int finishIndex, string expected)
    {
        RaceInfoSounds.NumberedFinishedKey(finishIndex).Should().Be(expected);
    }

    [Fact]
    public void FinishedSounds_ShouldNeverNeedATenthPositionClip()
    {
        // With a ten-player maximum, tenth place is always last, so finished10 need not exist.
        const int maxPlayers = TopSpeed.Protocol.ProtocolConstants.MaxPlayers;

        for (var racers = 1; racers <= maxPlayers; racers++)
        {
            for (var index = 0; index < racers; index++)
            {
                if (RaceInfoSounds.IsLastPlace(index, racers))
                    continue;

                RaceInfoSounds.NumberedFinishedKey(index).Should().NotBe(
                    $"race\\info\\finished{maxPlayers}",
                    $"position {index + 1} of {racers} should never ask for a {maxPlayers}th-place clip");
            }
        }
    }
}
