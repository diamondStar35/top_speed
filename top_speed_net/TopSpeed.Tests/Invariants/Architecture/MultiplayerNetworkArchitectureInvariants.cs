using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Xunit;

namespace TopSpeed.Tests;

[Trait("Category", "Invariants")]
public sealed class MultiplayerNetworkArchitectureInvariants
{
    [Fact]
    public void RefactorScope_FileNames_ShouldNotContainDottedSegments()
    {
        var root = FindTopSpeedNetRoot();
        var scopes = new[]
        {
            Path.Combine(root, "TopSpeed", "Multiplayer"),
            Path.Combine(root, "TopSpeed.Server", "Network")
        };

        var offenders = new List<string>();
        foreach (var scope in scopes)
        {
            foreach (var file in Directory.GetFiles(scope, "*.cs", SearchOption.AllDirectories))
            {
                var fileName = Path.GetFileNameWithoutExtension(file);
                if (fileName.Contains('.', StringComparison.Ordinal))
                    offenders.Add(ToRelative(root, file));
            }
        }

        offenders.Should().BeEmpty("refactor scope files should use folder splits instead of dotted filenames");
    }

    [Fact]
    public void ServerNetwork_FileNames_ShouldNotUseLegacyLowerSnakeCase()
    {
        var root = FindTopSpeedNetRoot();
        var networkRoot = Path.Combine(root, "TopSpeed.Server", "Network");
        var legacyNameRegex = new Regex("^[a-z0-9_]+$", RegexOptions.Compiled);

        var offenders = Directory.GetFiles(networkRoot, "*.cs", SearchOption.AllDirectories)
            .Where(file => legacyNameRegex.IsMatch(Path.GetFileNameWithoutExtension(file)))
            .Select(file => ToRelative(root, file))
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        offenders.Should().BeEmpty("server network source files should use normalized PascalCase filenames");
    }

    [Fact]
    public void ClientRoomSyncHandlers_ShouldAvoidDirectMenuAndSpeechCalls()
    {
        var root = FindTopSpeedNetRoot();
        var handlersRoot = Path.Combine(root, "TopSpeed", "Multiplayer", "Coordinator", "RoomSync", "Handlers");
        var forbiddenTokens = new[]
        {
            "_menu.",
            "_speech.",
            "_audio.",
            "RebuildRoomOptionsMenu(",
            "RebuildRoomGameRulesMenu("
        };

        var offenders = FindTokenOffenders(root, handlersRoot, forbiddenTokens);
        offenders.Should().BeEmpty("room-sync handlers should mutate state first and emit UI effects through packet effects");
    }

    [Fact]
    public void ServerNotifyService_ShouldNotCallRoomRaceOrSessionServices()
    {
        var root = FindTopSpeedNetRoot();
        var notifyRoot = Path.Combine(root, "TopSpeed.Server", "Network", "Services", "Notify");
        var forbiddenTokens = new[]
        {
            "_owner._room.",
            "_owner._race.",
            "_owner._session."
        };

        var offenders = FindTokenOffenders(root, notifyRoot, forbiddenTokens);
        offenders.Should().BeEmpty("notify service should only handle packet emission");
    }

    [Fact]
    public void ServerRoomAndRaceServices_ShouldNotCallSessionService()
    {
        var root = FindTopSpeedNetRoot();
        var roomRoot = Path.Combine(root, "TopSpeed.Server", "Network", "Services", "Room");
        var raceRoot = Path.Combine(root, "TopSpeed.Server", "Network", "Services", "Race");

        var roomOffenders = FindTokenOffenders(root, roomRoot, "_owner._session.");
        var raceOffenders = FindTokenOffenders(root, raceRoot, "_owner._session.");

        roomOffenders.Concat(raceOffenders)
            .Should()
            .BeEmpty("room/race ownership should not depend on session service internals");
    }

    private static string[] FindTokenOffenders(string topSpeedRoot, string folder, params string[] forbiddenTokens)
    {
        var offenders = new List<string>();
        foreach (var file in Directory.GetFiles(folder, "*.cs", SearchOption.AllDirectories))
        {
            var text = File.ReadAllText(file);
            var matched = forbiddenTokens.Where(token => text.Contains(token, StringComparison.Ordinal)).ToArray();
            if (matched.Length == 0)
                continue;

            offenders.Add($"{ToRelative(topSpeedRoot, file)} ({string.Join(", ", matched)})");
        }

        return offenders.ToArray();
    }

    private static string ToRelative(string root, string fullPath)
    {
        return Path.GetRelativePath(root, fullPath).Replace('\\', '/');
    }

    private static string FindTopSpeedNetRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory != null)
        {
            var candidate = Path.Combine(directory.FullName, "top_speed_net");
            if (Directory.Exists(candidate))
                return candidate;

            if (string.Equals(directory.Name, "top_speed_net", StringComparison.OrdinalIgnoreCase))
                return directory.FullName;

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate top_speed_net from test output directory.");
    }
}
