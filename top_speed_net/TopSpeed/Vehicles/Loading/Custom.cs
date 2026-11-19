using System;
using System.Collections.Generic;
using System.IO;
using TopSpeed.Core;
using TopSpeed.Data;
using TopSpeed.Localization;
using TopSpeed.Protocol;
using TopSpeed.Tracks;
using TopSpeed.Vehicles.Parsing;

namespace TopSpeed.Vehicles.Loader
{
    internal static class Custom
    {
        public static VehicleDefinition Load(string vehicleFile, TrackWeather weather)
        {
            var filePath = Path.IsPathRooted(vehicleFile)
                ? vehicleFile
                : ResolveRelativeVehicleFile(vehicleFile);
            var builtinRoot = Path.Combine(AssetPaths.SoundsRoot, "Vehicles");

            if (!VehicleTsvParser.TryLoadFromFile(filePath, out var parsed, out var issues))
            {
                var message = issues == null || issues.Count == 0
                    ? LocalizationService.Mark("Unknown parse error.")
                    : string.Join(" ", issues);
                throw new InvalidDataException(LocalizationService.Format(
                    LocalizationService.Mark("Failed to load custom vehicle '{0}'. {1}"),
                    filePath,
                    message));
            }

            var spec = Spec.FromCustom(parsed, weather);
            var def = new VehicleDefinition
            {
                CarType = CarType.Vehicle1,
                Name = parsed.Meta.Name,
                UserDefined = true,
                CustomFile = Path.GetFileNameWithoutExtension(filePath),
                CustomVersion = parsed.Meta.Version,
                CustomDescription = parsed.Meta.Description
            };
            Spec.Apply(def, spec);

            var vehicleRoot = parsed.SourceDirectory;

            // Required sounds fall back to the official builtin equivalent when a custom file is missing or
            // unresolvable, so a mispackaged vehicle (e.g. a .tsv that references a .wav that never shipped)
            // degrades to a working car instead of hard-crashing the race. The custom-vehicles menu already
            // flags these up front; this is the last line of defence for a car reached another way.
            def.SetSoundPath(VehicleAction.Engine, ResolveRequired(parsed.Sounds.Engine, builtinRoot, vehicleRoot, VehicleAction.Engine));
            def.SetSoundPath(VehicleAction.Start, ResolveRequired(parsed.Sounds.Start, builtinRoot, vehicleRoot, VehicleAction.Start));
            if (!string.IsNullOrWhiteSpace(parsed.Sounds.Stop))
                SetOptional(def, VehicleAction.Stop, parsed.Sounds.Stop!, builtinRoot, vehicleRoot);
            def.SetSoundPath(VehicleAction.Horn, ResolveRequired(parsed.Sounds.Horn, builtinRoot, vehicleRoot, VehicleAction.Horn));
            if (!string.IsNullOrWhiteSpace(parsed.Sounds.Throttle))
                SetOptional(def, VehicleAction.Throttle, parsed.Sounds.Throttle!, builtinRoot, vehicleRoot);
            def.SetSoundPath(VehicleAction.Brake, ResolveRequired(parsed.Sounds.Brake, builtinRoot, vehicleRoot, VehicleAction.Brake));
            def.SetSoundPaths(VehicleAction.Crash, ResolveRequiredList(parsed.Sounds.CrashVariants, builtinRoot, vehicleRoot, VehicleAction.Crash));
            if (parsed.Sounds.BackfireVariants != null && parsed.Sounds.BackfireVariants.Count > 0)
            {
                var backfire = ResolveOptionalList(parsed.Sounds.BackfireVariants, builtinRoot, vehicleRoot, VehicleAction.Backfire);
                if (backfire.Length > 0)
                    def.SetSoundPaths(VehicleAction.Backfire, backfire);
            }

            return def;
        }

        // A stored vehicle path can be relative to any content root, so try each and take the first
        // that exists. Falling back to the asset root when none match keeps the original behaviour
        // for a path that points at nothing, so the parse below reports it as before.
        private static string ResolveRelativeVehicleFile(string vehicleFile)
        {
            var roots = AssetPaths.ContentRoots;
            for (var i = 0; i < roots.Count; i++)
            {
                var candidate = Path.Combine(roots[i], vehicleFile);
                if (File.Exists(candidate))
                    return candidate;
            }

            return Path.Combine(AssetPaths.Root, vehicleFile);
        }

        private static string ResolveRequired(string value, string builtinRoot, string vehicleRoot, VehicleAction action)
        {
            if (Sound.TryResolveCustom(value, builtinRoot, vehicleRoot, action, out var resolved, out var error, out _))
                return resolved!;

            var fallback = Sound.ResolveOfficialFallback(builtinRoot, "Vehicle1", action);
            if (!string.IsNullOrWhiteSpace(fallback))
                return fallback!;

            // No custom file and no official fallback (a broken installation, not just a mispackaged car):
            // surface the original problem rather than silently continuing.
            throw new InvalidDataException(error);
        }

        private static void SetOptional(VehicleDefinition def, VehicleAction action, string value, string builtinRoot, string vehicleRoot)
        {
            // Optional slots simply drop out when unresolvable; the engine treats them as absent.
            if (Sound.TryResolveCustom(value, builtinRoot, vehicleRoot, action, out var resolved, out _, out _))
                def.SetSoundPath(action, resolved!);
        }

        private static string[] ResolveRequiredList(IReadOnlyList<string> values, string builtinRoot, string vehicleRoot, VehicleAction action)
        {
            var resolved = ResolveOptionalList(values, builtinRoot, vehicleRoot, action);
            if (resolved.Length > 0)
                return resolved;

            var fallback = Sound.ResolveOfficialFallback(builtinRoot, "Vehicle1", action);
            if (!string.IsNullOrWhiteSpace(fallback))
                return new[] { fallback! };

            throw new InvalidDataException($"No valid sound paths resolved for {action}, and no official fallback is available.");
        }

        private static string[] ResolveOptionalList(IReadOnlyList<string> values, string builtinRoot, string vehicleRoot, VehicleAction action)
        {
            if (values == null || values.Count == 0)
                return Array.Empty<string>();

            var result = new List<string>(values.Count);
            for (var i = 0; i < values.Count; i++)
                if (Sound.TryResolveCustom(values[i], builtinRoot, vehicleRoot, action, out var resolved, out _, out _))
                    result.Add(resolved!);

            return result.ToArray();
        }
    }
}

