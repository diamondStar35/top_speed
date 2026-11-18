using System;
using System.IO;
using System.IO.Compression;
using System.Text;
using TopSpeed.Updater;
using TopSpeed.Updater.Archive;
using TopSpeed.Updater.Windows;
using Xunit;

namespace TopSpeed.Tests;

public sealed class UpdaterInstallationBehaviorTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "TopSpeed.Updater.Tests", Guid.NewGuid().ToString("N"));
    private readonly Options _options;
    private static readonly byte[] OldExecutable = Encoding.UTF8.GetBytes("MZ old executable");
    private static readonly byte[] NewExecutable = Encoding.UTF8.GetBytes("MZ updated executable");

    public UpdaterInstallationBehaviorTests()
    {
        var target = Path.Combine(_root, "install");
        Directory.CreateDirectory(target);
        _options = new Options
        {
            TargetDir = target,
            ZipPath = Path.Combine(_root, "update.zip"),
            GameName = "TestGame",
            ProcessId = int.MaxValue
        };
        File.WriteAllBytes(Path.Combine(target, Platform.Executable(_options.GameName)), OldExecutable);
        File.WriteAllBytes(Path.Combine(target, Platform.Executable(Platform.UpdaterName)), OldExecutable);
    }

    [Fact]
    public void Install_ReplacesPackagedFilesWithoutDeletingCustomDrafts()
    {
        var custom = Path.Combine(_options.TargetDir, "Vehicles", "custom.car");
        Directory.CreateDirectory(Path.GetDirectoryName(custom)!);
        File.WriteAllText(custom, "custom vehicle");
        File.WriteAllText(custom + ".new", "unfinished draft");
        File.WriteAllText(custom + ".superseded", "user backup");
        using (var archive = ZipFile.Open(_options.ZipPath, ZipArchiveMode.Create))
            AddExecutables(archive);

        Install();

        File.ReadAllBytes(Path.Combine(_options.TargetDir, Platform.Executable(_options.GameName))).Should().Equal(NewExecutable);
        File.ReadAllText(custom + ".new").Should().Be("unfinished draft");
        File.ReadAllText(custom + ".superseded").Should().Be("user backup");
        File.Exists(_options.ZipPath).Should().BeFalse();
        Directory.GetDirectories(_options.TargetDir, ".topspeed-update-*").Should().BeEmpty();
    }

    [Fact]
    public void Install_WhenArchiveCannotBeDeleted_StillCompletesTheUpdate()
    {
        using (var archive = ZipFile.Open(_options.ZipPath, ZipArchiveMode.Create))
            AddExecutables(archive);
        using var heldArchive = new FileStream(_options.ZipPath, FileMode.Open, FileAccess.Read, FileShare.Read);

        Install();

        File.ReadAllBytes(Path.Combine(_options.TargetDir, Platform.Executable(_options.GameName))).Should().Equal(NewExecutable);
        File.Exists(_options.ZipPath).Should().BeTrue();
    }

    [Fact]
    public void Install_WithCorruptArchive_PreservesInstalledFilesAndDrafts()
    {
        var draft = Path.Combine(_options.TargetDir, "notes.new");
        File.WriteAllText(Path.Combine(_options.TargetDir, "notes"), "original");
        File.WriteAllText(draft, "draft");
        File.WriteAllText(_options.ZipPath, "invalid archive");

        Assert.Throws<InvalidDataException>(Install);

        AssertOriginalGame();
        File.ReadAllText(draft).Should().Be("draft");
        File.Exists(_options.ZipPath).Should().BeTrue();
    }

    [Theory]
    [InlineData("../install-sibling/escaped.bin")]
    [InlineData("..\\install-sibling\\escaped.bin")]
    [InlineData("../install-sibling/")]
    public void Install_RejectsUnsafeEntriesBeforeReplacingAnyInstalledFile(string entry)
    {
        using (var archive = ZipFile.Open(_options.ZipPath, ZipArchiveMode.Create))
        {
            AddExecutables(archive);
            archive.CreateEntry(entry);
        }

        Assert.Throws<InvalidDataException>(Install);

        AssertOriginalGame();
        Directory.Exists(Path.Combine(_root, "install-sibling")).Should().BeFalse();
        Directory.GetDirectories(_options.TargetDir, ".topspeed-update-*").Should().BeEmpty();
    }

    [Fact]
    public void Install_RejectsMissingUpdaterBeforeReplacingTheGame()
    {
        using (var archive = ZipFile.Open(_options.ZipPath, ZipArchiveMode.Create))
            Add(archive, Platform.Executable(_options.GameName), NewExecutable);

        Assert.Throws<InvalidDataException>(Install);

        AssertOriginalGame();
    }

    [Fact]
    public void Install_MapsMacBundlePayloadIntoContentsMacOS()
    {
        _options.TargetDir = Path.Combine(_root, "TestGame.app", "Contents", "MacOS");
        Directory.CreateDirectory(_options.TargetDir);
        using (var archive = ZipFile.Open(_options.ZipPath, ZipArchiveMode.Create))
        {
            AddExecutables(archive, "TestGame.app/Contents/MacOS/");
            Add(archive, "TestGame.app/Contents/Info.plist", Encoding.UTF8.GetBytes("bundle metadata"));
        }

        Install();

        File.ReadAllBytes(Path.Combine(_options.TargetDir, Platform.Executable(_options.GameName))).Should().Equal(NewExecutable);
        Directory.Exists(Path.Combine(_options.TargetDir, "TestGame.app")).Should().BeFalse();
    }

    [Fact]
    public void WorkerCleanup_RejectsAnUnrelatedDirectory()
    {
        var unrelated = Path.Combine(_root, "unrelated");
        Directory.CreateDirectory(unrelated);
        var file = Path.Combine(unrelated, Platform.UpdaterName + ".exe");
        File.WriteAllBytes(file, OldExecutable);

        Assert.Throws<InvalidOperationException>(() => WorkerFiles.Delete(_options.TargetDir, unrelated));

        File.Exists(file).Should().BeTrue();
    }

    [Fact]
    public void WorkerCleanup_LeavesUnknownFilesAlone()
    {
        var directory = WorkerFiles.Create(_options.TargetDir);
        var draft = Path.Combine(directory, "user-draft");
        File.WriteAllText(draft, "preserve me");

        Assert.Throws<IOException>(() => WorkerFiles.Delete(_options.TargetDir, directory));

        File.ReadAllText(draft).Should().Be("preserve me");
    }

    private void Install() => Installer.Install(_options, new Log(_options));

    private void AssertOriginalGame()
    {
        File.ReadAllBytes(Path.Combine(_options.TargetDir, Platform.Executable(_options.GameName))).Should().Equal(OldExecutable);
    }

    private void AddExecutables(ZipArchive archive, string prefix = "")
    {
        Add(archive, prefix + Platform.Executable(_options.GameName), NewExecutable);
        Add(archive, prefix + Platform.Executable(Platform.UpdaterName), NewExecutable);
    }

    private static void Add(ZipArchive archive, string name, byte[] content)
    {
        using var entry = archive.CreateEntry(name).Open();
        entry.Write(content, 0, content.Length);
    }

    public void Dispose()
    {
        var expectedParent = Path.GetFullPath(Path.Combine(Path.GetTempPath(), "TopSpeed.Updater.Tests"));
        if (Path.GetDirectoryName(Path.GetFullPath(_root)) != expectedParent)
            throw new InvalidOperationException("Unexpected test directory.");
        Directory.Delete(_root, recursive: true);
    }
}
