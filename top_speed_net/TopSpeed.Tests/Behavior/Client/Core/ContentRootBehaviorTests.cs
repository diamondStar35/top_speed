using System.IO;
using FluentAssertions;
using TopSpeed.Core;
using Xunit;

namespace TopSpeed.Tests;

[Trait("Category", "Behavior")]
public sealed class ContentRootBehaviorTests
{
    // Desktop points both roots at the same folder. If that did not collapse to a single root, every
    // vehicle and track would be found twice and listed twice.
    [Fact]
    public void Content_Roots_Collapse_To_One_When_Both_Roots_Are_The_Same_Folder()
    {
        var roots = AssetPaths.BuildContentRoots(@"C:\game", @"C:\game");

        roots.Should().ContainSingle().Which.Should().Be(@"C:\game");
    }

    [Fact]
    public void Content_Roots_Collapse_When_Only_A_Trailing_Separator_Differs()
    {
        var shipped = Path.Combine("C:", "game");
        var user = shipped + Path.DirectorySeparatorChar;

        AssetPaths.BuildContentRoots(shipped, user).Should().ContainSingle();
    }

    // Platforms that unpack their shipped assets keep the player's content somewhere the unpack step
    // never clears, so both folders have to be searched.
    [Fact]
    public void Content_Roots_Keep_Both_When_User_Content_Lives_Elsewhere()
    {
        var roots = AssetPaths.BuildContentRoots("/data/app/no_backup/topspeed_assets", "/data/app/files/user");

        roots.Should().HaveCount(2);
        roots[0].Should().Be("/data/app/no_backup/topspeed_assets");
        roots[1].Should().Be("/data/app/files/user");
    }

    [Fact]
    public void User_Content_Root_Defaults_To_The_Asset_Root()
    {
        AssetPaths.UserContentRoot.Should().Be(AssetPaths.Root);
    }
}
