using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TopSpeed.Protocol;
using TopSpeed.Vehicles.Loader;
using TopSpeed.Vehicles.Parsing;
using Xunit;

namespace TopSpeed.Tests
{
    // Guards Sound.ValidateCustomSounds, the check the custom-vehicles menu runs on entry so a car that
    // references a sound file it never shipped is flagged up front instead of only surfacing at the race
    // loader. The Required flag no longer gates selection (the car stays playable via the loader's fallback);
    // it drives the wording of the warning line ("required sound" vs "optional sound"). The value here is
    // behavioral: a regression would still compile but misclassify a slot and mislabel the warning.
    [Trait("Category", "Behavior")]
    public sealed class CustomSoundValidationBehaviorTests
    {
        [Fact]
        public void AllRequiredSoundsPresent_ShouldProduceNoIssues()
        {
            using var vehicle = TempVehicleFolder.Create("engine.wav", "start.wav", "horn.wav", "brake.wav", "crash.wav");
            var sounds = new CustomVehicleSounds
            {
                Engine = "engine.wav",
                Start = "start.wav",
                Horn = "horn.wav",
                Brake = "brake.wav",
                CrashVariants = new[] { "crash.wav" }
            };

            var issues = Sound.ValidateCustomSounds(sounds, vehicle.BuiltinRoot, vehicle.Path);

            issues.Should().BeEmpty();
        }

        [Fact]
        public void MissingRequiredEngine_ShouldReportRequiredIssue()
        {
            // The .tsv names an engine sound that is not on disk (a .tsv that shipped without its .wav).
            using var vehicle = TempVehicleFolder.Create("start.wav", "horn.wav", "brake.wav", "crash.wav");
            var sounds = new CustomVehicleSounds
            {
                Engine = "missing_engine.wav",
                Start = "start.wav",
                Horn = "horn.wav",
                Brake = "brake.wav",
                CrashVariants = new[] { "crash.wav" }
            };

            var issues = Sound.ValidateCustomSounds(sounds, vehicle.BuiltinRoot, vehicle.Path);

            issues.Should().ContainSingle();
            issues[0].Action.Should().Be(VehicleAction.Engine);
            issues[0].Required.Should().BeTrue();
            issues[0].Message.Should().Contain("missing_engine.wav");
            issues[0].MissingPath.Should().NotBeNullOrEmpty();
            issues[0].MissingPath.Should().Contain("missing_engine.wav");
        }

        [Fact]
        public void MissingOptionalStop_ShouldReportWarningNotRequired()
        {
            using var vehicle = TempVehicleFolder.Create("engine.wav", "start.wav", "horn.wav", "brake.wav", "crash.wav");
            var sounds = new CustomVehicleSounds
            {
                Engine = "engine.wav",
                Start = "start.wav",
                Horn = "horn.wav",
                Brake = "brake.wav",
                CrashVariants = new[] { "crash.wav" },
                Stop = "stop.wav" // declared but never shipped
            };

            var issues = Sound.ValidateCustomSounds(sounds, vehicle.BuiltinRoot, vehicle.Path);

            issues.Should().ContainSingle();
            issues[0].Action.Should().Be(VehicleAction.Stop);
            issues[0].Required.Should().BeFalse();
        }

        [Fact]
        public void CrashListWithOneSurvivingVariant_ShouldNotBeRequired()
        {
            // A single resolvable crash variant is enough to run, so the missing sibling must not escalate the
            // whole slot to a required error that would drop the car from the menu.
            using var vehicle = TempVehicleFolder.Create("engine.wav", "start.wav", "horn.wav", "brake.wav", "crash2.wav");
            var sounds = new CustomVehicleSounds
            {
                Engine = "engine.wav",
                Start = "start.wav",
                Horn = "horn.wav",
                Brake = "brake.wav",
                CrashVariants = new[] { "crash1.wav", "crash2.wav" }
            };

            var issues = Sound.ValidateCustomSounds(sounds, vehicle.BuiltinRoot, vehicle.Path);

            issues.Should().ContainSingle();
            issues[0].Action.Should().Be(VehicleAction.Crash);
            issues[0].Required.Should().BeFalse();
            issues.Any(x => x.Required).Should().BeFalse("one good crash variant keeps the car playable");
        }

        [Fact]
        public void CrashListWithNoVariants_ShouldBeRequired()
        {
            using var vehicle = TempVehicleFolder.Create("engine.wav", "start.wav", "horn.wav", "brake.wav");
            var sounds = new CustomVehicleSounds
            {
                Engine = "engine.wav",
                Start = "start.wav",
                Horn = "horn.wav",
                Brake = "brake.wav",
                CrashVariants = Array.Empty<string>()
            };

            var issues = Sound.ValidateCustomSounds(sounds, vehicle.BuiltinRoot, vehicle.Path);

            issues.Should().ContainSingle();
            issues[0].Action.Should().Be(VehicleAction.Crash);
            issues[0].Required.Should().BeTrue();
        }

        [Fact]
        public void BuiltinReferences_ShouldNotBeFlaggedAsMissing()
        {
            // Cars that point required slots at builtins must not be flagged just because they carry no local
            // .wav of their own. Builtin1 resolves via the official fallback files.
            using var vehicle = TempVehicleFolder.Create();
            vehicle.CreateBuiltinVehicle("Vehicle1", "engine.wav", "start.wav", "horn.wav", "brake.wav", "crash.wav");
            var sounds = new CustomVehicleSounds
            {
                Engine = "builtin1",
                Start = "builtin1",
                Horn = "builtin1",
                Brake = "builtin1",
                CrashVariants = new[] { "builtin1" }
            };

            var issues = Sound.ValidateCustomSounds(sounds, vehicle.BuiltinRoot, vehicle.Path);

            issues.Should().BeEmpty();
        }

        private sealed class TempVehicleFolder : IDisposable
        {
            public string Root { get; }
            public string Path { get; }
            public string BuiltinRoot { get; }

            private TempVehicleFolder(string root, string vehiclePath, string builtinRoot)
            {
                Root = root;
                Path = vehiclePath;
                BuiltinRoot = builtinRoot;
            }

            public static TempVehicleFolder Create(params string[] existingFiles)
            {
                var root = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"topspeed_sounds_{Guid.NewGuid():N}");
                var vehicleDir = System.IO.Path.Combine(root, "vehicle");
                var builtinDir = System.IO.Path.Combine(root, "builtin");
                Directory.CreateDirectory(vehicleDir);
                Directory.CreateDirectory(builtinDir);
                for (var i = 0; i < existingFiles.Length; i++)
                    File.WriteAllBytes(System.IO.Path.Combine(vehicleDir, existingFiles[i]), Array.Empty<byte>());
                return new TempVehicleFolder(root, vehicleDir, builtinDir);
            }

            public void CreateBuiltinVehicle(string folder, params string[] files)
            {
                var dir = System.IO.Path.Combine(BuiltinRoot, folder);
                Directory.CreateDirectory(dir);
                for (var i = 0; i < files.Length; i++)
                    File.WriteAllBytes(System.IO.Path.Combine(dir, files[i]), Array.Empty<byte>());
            }

            public void Dispose()
            {
                try
                {
                    if (Directory.Exists(Root))
                        Directory.Delete(Root, recursive: true);
                }
                catch (IOException)
                {
                    // Best-effort cleanup; a leftover temp folder must not fail the test.
                }
            }
        }
    }
}
