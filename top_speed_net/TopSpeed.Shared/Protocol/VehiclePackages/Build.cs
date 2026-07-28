using System;
using System.Collections.Generic;
using System.IO;
using TopSpeed.Localization;
using TopSpeed.Vehicles.Parsing;

namespace TopSpeed.Protocol
{
    public static class VehiclePackageBuild
    {
        // vehiclesRoot, when supplied, is the Vehicles folder the file was discovered under. It lets
        // the manifest carry the vehicle's folder path relative to that root rather than just the
        // leaf folder, so a client can reproduce the layout instead of flattening every vehicle into
        // one level (where "NASCAR/cup car dodge" and "IndyCar/cup car dodge" would collide).
        public static bool TryBuildPackageFromVehicleFile(
            string vehicleFile,
            out VehiclePackagePayload payload,
            out byte[] bytes,
            out CustomVehicleTsvData parsed,
            out string error,
            string? vehiclesRoot = null)
        {
            payload = new VehiclePackagePayload();
            bytes = Array.Empty<byte>();
            parsed = null!;
            error = string.Empty;

            if (!VehicleTsvParser.TryLoadFromFile(vehicleFile, out parsed, out var issues))
            {
                error = BuildLoadError(issues);
                return false;
            }

            string tsvText;
            try
            {
                tsvText = File.ReadAllText(Path.GetFullPath(vehicleFile));
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
            {
                error = ex.Message;
                return false;
            }

            var sourceRoot = parsed.SourceDirectory;
            if (string.IsNullOrWhiteSpace(sourceRoot))
                sourceRoot = Path.GetDirectoryName(Path.GetFullPath(vehicleFile)) ?? string.Empty;

            if (!TryBuildAssetBlobs(sourceRoot, parsed, out var assets, out error))
                return false;

            var displayName = string.IsNullOrWhiteSpace(parsed.Meta?.Name)
                ? Path.GetFileNameWithoutExtension(vehicleFile)
                : parsed.Meta!.Name;

            payload = new VehiclePackagePayload
            {
                Manifest = new VehiclePackageManifest
                {
                    VehicleId = ResolveVehicleId(parsed, vehicleFile),
                    Version = string.IsNullOrWhiteSpace(parsed.Meta?.Version) ? "1" : parsed.Meta!.Version,
                    Hash = string.Empty,
                    DisplayName = ClampDisplayName(displayName),
                    TsvFileName = Path.GetFileName(vehicleFile) ?? string.Empty,
                    FolderName = ResolveFolderName(vehicleFile, vehiclesRoot)
                },
                TsvText = tsvText,
                AssetBlobs = assets
            };

            var hash = VehiclePackageCodec.ComputeHash(payload);
            payload.Manifest.Hash = hash;
            if (!VehiclePackageCodec.TryValidate(payload, out error))
                return false;

            bytes = VehiclePackageCodec.Serialize(payload);
            return true;
        }

        // "NASCAR/cup car dodge" for a vehicle nested under the Vehicles root, or just the leaf
        // folder name when the root is unknown or the file sits outside it. A file directly in the
        // root keeps returning that root's own name, which the client already treats as "no usable
        // folder" and replaces with the vehicle's display name.
        private static string ResolveFolderName(string vehicleFile, string? vehiclesRoot)
        {
            var directory = Path.GetDirectoryName(Path.GetFullPath(vehicleFile)) ?? string.Empty;
            var leaf = Path.GetFileName(directory) ?? string.Empty;
            if (string.IsNullOrWhiteSpace(directory) || string.IsNullOrWhiteSpace(vehiclesRoot))
                return leaf;

            var root = Path.GetFullPath(vehiclesRoot!);
            if (!IsPathInsideRoot(directory, root) || string.Equals(directory, root, StringComparison.OrdinalIgnoreCase))
                return leaf;

            var relative = directory
                .Substring(root.TrimEnd(Path.DirectorySeparatorChar).Length)
                .TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                .Replace(Path.DirectorySeparatorChar, '/')
                .Replace('\\', '/');
            return string.IsNullOrWhiteSpace(relative) ? leaf : relative;
        }

        private static string ResolveVehicleId(CustomVehicleTsvData parsed, string vehicleFile)
        {
            var id = parsed.Meta?.Name;
            if (string.IsNullOrWhiteSpace(id))
                id = Path.GetFileNameWithoutExtension(vehicleFile);
            id = (id ?? "custom").Trim();
            if (id.Length == 0)
                id = "custom";
            if (id.Length > ProtocolConstants.MaxVehicleIdLength)
                id = id.Substring(0, ProtocolConstants.MaxVehicleIdLength);
            return id;
        }

        private static string ClampDisplayName(string value)
        {
            var trimmed = (value ?? string.Empty).Trim();
            if (trimmed.Length == 0)
                trimmed = "Custom vehicle";
            if (trimmed.Length > ProtocolConstants.MaxVehiclePackageDisplayNameLength)
                trimmed = trimmed.Substring(0, ProtocolConstants.MaxVehiclePackageDisplayNameLength);
            return trimmed;
        }

        private static bool TryBuildAssetBlobs(
            string sourceRoot,
            CustomVehicleTsvData parsed,
            out IReadOnlyDictionary<string, byte[]> assets,
            out string error)
        {
            error = string.Empty;
            var map = new Dictionary<string, byte[]>(StringComparer.OrdinalIgnoreCase);
            assets = map;

            var root = string.IsNullOrWhiteSpace(sourceRoot) ? string.Empty : Path.GetFullPath(sourceRoot);
            if (string.IsNullOrWhiteSpace(root))
            {
                error = LocalizationService.Mark("Unable to resolve custom vehicle folder path.");
                return false;
            }

            var sounds = parsed.Sounds ?? new CustomVehicleSounds();
            foreach (var relative in EnumerateSoundPaths(sounds))
            {
                if (!TryAddAsset(root, relative, map, out error))
                    return false;
            }

            return true;
        }

        private static IEnumerable<string> EnumerateSoundPaths(CustomVehicleSounds sounds)
        {
            yield return sounds.Engine;
            yield return sounds.Start;
            yield return sounds.Stop ?? string.Empty;
            yield return sounds.Horn;
            yield return sounds.Throttle ?? string.Empty;
            yield return sounds.Brake;
            var crash = sounds.CrashVariants ?? Array.Empty<string>();
            for (var i = 0; i < crash.Count; i++)
                yield return crash[i];
            var backfire = sounds.BackfireVariants ?? Array.Empty<string>();
            for (var i = 0; i < backfire.Count; i++)
                yield return backfire[i];
        }

        private static bool TryAddAsset(string root, string? relativeAssetPath, Dictionary<string, byte[]> map, out string error)
        {
            error = string.Empty;
            if (string.IsNullOrWhiteSpace(relativeAssetPath))
                return true;

            var key = VehiclePackageCodec.NormalizeAssetKey(relativeAssetPath ?? string.Empty);
            if (string.IsNullOrWhiteSpace(key))
            {
                error = LocalizationService.Format(LocalizationService.Mark("Invalid sound asset path: {0}"), relativeAssetPath ?? string.Empty);
                return false;
            }

            if (map.ContainsKey(key))
                return true;

            var relativePath = key.Replace('/', Path.DirectorySeparatorChar);
            var absolutePath = Path.GetFullPath(Path.Combine(root, relativePath));
            if (!IsPathInsideRoot(absolutePath, root))
            {
                error = LocalizationService.Format(LocalizationService.Mark("Sound asset path escapes the vehicle folder: {0}"), relativeAssetPath ?? string.Empty);
                return false;
            }

            // Only bundle actual sound files that ship with the vehicle. References that are not
            // files (e.g. "builtin1" built-in sound tokens, or defaults) are left in the .tsv text
            // and resolved by the client the same way as an offline custom vehicle.
            if (!File.Exists(absolutePath))
                return true;

            try
            {
                map[key] = File.ReadAllBytes(absolutePath);
                return true;
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
            {
                error = ex.Message;
                return false;
            }
        }

        private static bool IsPathInsideRoot(string candidatePath, string rootPath)
        {
            if (string.Equals(candidatePath, rootPath, StringComparison.OrdinalIgnoreCase))
                return true;

            var rootPrefix = rootPath.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
            return candidatePath.StartsWith(rootPrefix, StringComparison.OrdinalIgnoreCase);
        }

        private static string BuildLoadError(IReadOnlyList<VehicleTsvIssue> issues)
        {
            if (issues == null || issues.Count == 0)
                return LocalizationService.Mark("Failed to load this vehicle file.");

            for (var i = 0; i < issues.Count; i++)
            {
                if (issues[i].Severity == VehicleTsvIssueSeverity.Error)
                    return issues[i].ToString();
            }

            return issues[0].ToString();
        }
    }
}
