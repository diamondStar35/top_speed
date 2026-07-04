using System;
using System.Collections.Generic;
using System.IO;
using TopSpeed.Core;
using TopSpeed.Protocol;
using TopSpeed.Vehicles.Parsing;

namespace TopSpeed.Game
{
    internal sealed partial class Game
    {
        // A downloaded (or locally reused) custom vehicle, parsed and ready to build a car from.
        private sealed class DownloadedVehiclePackage
        {
            public string Hash = string.Empty;
            public VehiclePackagePayload Payload = new VehiclePackagePayload();
            public CustomVehicleTsvData Vehicle = new CustomVehicleTsvData();
            public string SourceDirectory = string.Empty;
            public string TsvPath = string.Empty;
            public string DisplayName = string.Empty;
        }

        // Session-lived, in-memory cache. Always populated on download so peers' real sounds
        // play; persists across races within a run; gone on restart unless the user kept it.
        private readonly Dictionary<string, DownloadedVehiclePackage> _multiplayerVehiclePackageCache =
            new Dictionary<string, DownloadedVehiclePackage>(StringComparer.OrdinalIgnoreCase);

        // hash -> .tsv path for vehicles already in the client's own Vehicles folder (the same
        // folder used for offline custom vehicles). Lets us reuse a local/kept vehicle instead of
        // downloading it. Built lazily; invalidated when a vehicle is kept.
        private Dictionary<string, string>? _localVehicleIndex;

        private bool TryGetCachedVehiclePackage(string hash, out DownloadedVehiclePackage package)
        {
            package = null!;
            var normalizedHash = VehiclePackageRef.NormalizeHash(hash);
            if (string.IsNullOrWhiteSpace(normalizedHash))
                return false;

            if (_multiplayerVehiclePackageCache.TryGetValue(normalizedHash, out var cached) && cached != null)
            {
                package = cached;
                return true;
            }

            // Reuse a vehicle we already have locally (previously kept, or authored by the user)
            // when its content hashes to the one the server wants.
            EnsureLocalVehicleIndex();
            if (_localVehicleIndex!.TryGetValue(normalizedHash, out var localTsvPath)
                && File.Exists(localTsvPath)
                && VehicleTsvParser.TryLoadFromFile(localTsvPath, out var parsed, out _))
            {
                package = new DownloadedVehiclePackage
                {
                    Hash = normalizedHash,
                    Vehicle = parsed,
                    SourceDirectory = parsed.SourceDirectory,
                    TsvPath = Path.GetFullPath(localTsvPath),
                    DisplayName = string.IsNullOrWhiteSpace(parsed.Meta?.Name) ? normalizedHash : parsed.Meta!.Name
                };
                _multiplayerVehiclePackageCache[normalizedHash] = package;
                return true;
            }

            return false;
        }

        // Writes the .tsv + sound blobs to a per-hash session dir, parses via the shared parser
        // (reproducing the single-player load path), and caches the parsed vehicle in memory.
        private bool TryMaterializeAndCacheVehiclePackage(string hash, VehiclePackagePayload payload, out DownloadedVehiclePackage package)
        {
            package = null!;
            var normalizedHash = VehiclePackageRef.NormalizeHash(hash);
            if (string.IsNullOrWhiteSpace(normalizedHash) || payload == null)
                return false;

            var root = GetVehiclePackageMaterializedDirectory(normalizedHash);
            if (!TryWriteVehiclePackageFiles(root, payload, out var tsvPath))
                return false;

            if (!VehicleTsvParser.TryLoadFromFile(tsvPath, out var parsed, out _))
                return false;

            package = new DownloadedVehiclePackage
            {
                Hash = normalizedHash,
                Payload = payload,
                Vehicle = parsed,
                SourceDirectory = parsed.SourceDirectory,
                TsvPath = Path.GetFullPath(tsvPath),
                DisplayName = payload.Manifest.DisplayName
            };
            _multiplayerVehiclePackageCache[normalizedHash] = package;
            return true;
        }

        // Writes payload.TsvText as vehicle.tsv plus each sound blob at its relative path under
        // rootDir. Returns the .tsv path. Shared by session materialization and "keep".
        private bool TryWriteVehiclePackageFiles(string rootDir, VehiclePackagePayload payload, out string tsvPath)
        {
            tsvPath = string.Empty;
            try
            {
                var root = Path.GetFullPath(rootDir);
                Directory.CreateDirectory(root);

                var assets = payload.AssetBlobs ?? new Dictionary<string, byte[]>(StringComparer.OrdinalIgnoreCase);
                foreach (var pair in assets)
                {
                    var key = VehiclePackageCodec.NormalizeAssetKey(pair.Key ?? string.Empty);
                    if (string.IsNullOrWhiteSpace(key))
                        continue;

                    var relative = key.Replace('/', Path.DirectorySeparatorChar);
                    var candidate = Path.GetFullPath(Path.Combine(root, relative));
                    if (!IsInsideRoot(candidate, root))
                        continue;

                    var parent = Path.GetDirectoryName(candidate);
                    if (!string.IsNullOrWhiteSpace(parent))
                        Directory.CreateDirectory(parent);
                    File.WriteAllBytes(candidate, pair.Value ?? Array.Empty<byte>());
                }

                tsvPath = Path.Combine(root, ResolvePackageTsvFileName(payload));
                File.WriteAllText(tsvPath, payload.TsvText ?? string.Empty);
                return true;
            }
            catch (IOException)
            {
                return false;
            }
            catch (UnauthorizedAccessException)
            {
                return false;
            }
        }

        // The original .tsv filename from the package (e.g. "chevy laguna.tsv"), sanitized, so a
        // materialized/kept vehicle keeps its real name instead of a generic "vehicle.tsv".
        private static string ResolvePackageTsvFileName(VehiclePackagePayload payload)
        {
            var name = Path.GetFileName((payload?.Manifest?.TsvFileName ?? string.Empty).Trim());
            if (string.IsNullOrWhiteSpace(name))
                return "vehicle.tsv";
            foreach (var invalid in Path.GetInvalidFileNameChars())
                name = name.Replace(invalid, '_');
            if (!name.EndsWith(".tsv", StringComparison.OrdinalIgnoreCase))
                name += ".tsv";
            return name;
        }

        // Builds hash -> .tsv path for every vehicle already in the client's Vehicles folder.
        private void EnsureLocalVehicleIndex()
        {
            if (_localVehicleIndex != null)
                return;

            var index = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var folder = GetClientVehiclesFolder();
            try
            {
                if (Directory.Exists(folder))
                {
                    foreach (var file in Directory.EnumerateFiles(folder, "*.tsv", SearchOption.AllDirectories))
                    {
                        if (VehiclePackageBuild.TryBuildPackageFromVehicleFile(file, out var payload, out _, out _, out _))
                        {
                            var hash = VehiclePackageRef.NormalizeHash(payload.Manifest.Hash);
                            if (!string.IsNullOrWhiteSpace(hash) && !index.ContainsKey(hash))
                                index[hash] = Path.GetFullPath(file);
                        }
                    }
                }
            }
            catch (IOException)
            {
            }
            catch (UnauthorizedAccessException)
            {
            }

            _localVehicleIndex = index;
        }

        private void InvalidateLocalVehicleIndex()
        {
            _localVehicleIndex = null;
        }

        private static string GetClientVehiclesFolder()
        {
            return Path.Combine(AssetPaths.Root, "Vehicles");
        }

        private static string GetVehiclePackageSessionRoot()
        {
            return Path.Combine(AppData.Root(), "vehicle_packages_session");
        }

        private static string GetVehiclePackageMaterializedDirectory(string hash)
        {
            var normalizedHash = VehiclePackageRef.NormalizeHash(hash);
            return Path.Combine(GetVehiclePackageSessionRoot(), normalizedHash);
        }
    }
}
