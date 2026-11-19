using System;
using System.Linq;
using TopSpeed.Core.Multiplayer;
using TopSpeed.Protocol;
using Xunit;

namespace TopSpeed.Tests;

[Trait("Category", "Behavior")]
public sealed class CustomTrackMenuBehaviorTests
{
    [Fact]
    public void CustomTrackRandom_ShouldChooseFromTheServerCatalog()
    {
        var tracks = new[] { Entry("iowa", 'a'), Entry("snow", 'b') };
        TrackPackageRef? selected = null;
        var randomChosen = false;
        var items = MultiplayerCoordinator.BuildRoomTrackCustomItems(tracks,
            (track, _, random) => { selected = track; randomChosen = random; }, () => { });
        var randomItem = items.Single(item => item.Text == "Random");
        for (var i = 0; i < 30; i++)
        {
            randomItem.OnActivate!();
            selected.Should().NotBeNull();
            selected!.IsCustomPackage.Should().BeTrue();
            tracks.Select(track => track.Track.Hash).Should().Contain(selected.Hash);
            randomChosen.Should().BeFalse("the random choice is already resolved to a custom package");
        }
    }

    [Fact]
    public void EmptyServerCatalog_ShouldNotOfferRandomBuiltInTracks()
    {
        var items = MultiplayerCoordinator.BuildRoomTrackCustomItems(Array.Empty<PacketTrackPackageCatalogEntry>(),
            (_, _, _) => throw new InvalidOperationException("No track should be selected."), () => { });
        items.Should().NotContain(item => item.Text == "Random");
        items.Should().Contain(item => item.Text == "Upload a local track");
    }

    private static PacketTrackPackageCatalogEntry Entry(string id, char hash) => new()
    {
        Track = TrackPackageRef.Custom(id, "1", new string(hash, 64)),
        DisplayName = id
    };
}
