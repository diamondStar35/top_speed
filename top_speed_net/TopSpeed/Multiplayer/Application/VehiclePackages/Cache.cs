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
                // A cached entry can go stale mid-session if the user edits the backing Vehicles-folder
                // copy (deletes the .tsv, or deletes/renames a referenced sound). If it no longer holds
                // the content the server wants, drop it and invalidate the index so the fresh by-hash
                // scan below re-finds a renamed copy or reports it missing so the caller re-downloads
                // the intact package. This makes a broken copy behave exactly like one we never had.
                if (IsCachedVehiclePackageUsable(cached, normalizedHash))
                {
                    package = cached;
                    return true;
                }

                _multiplayerVehiclePackageCache.Remove(normalizedHash);
                InvalidateLocalVehicleIndex();
            }

            // Reuse a vehicle we already have locally (previously kept, or authored by the user)
            // when its content still hashes to the one the server wants.
            EnsureLocalVehicleIndex();
            if (TryReuseLocalVehicle(normalizedHash, out package, out var indexLooksStale))
                return true;

            // The entry pointed at something that is no longer this vehicle, so the index is out of
            // date. Rebuild once and look again before giving up: the vehicle may simply have moved,
            // in which case it is still ours and still worth reusing.
            if (indexLooksStale)
            {
                InvalidateLocalVehicleIndex();
                EnsureLocalVehicleIndex();
                if (TryReuseLocalVehicle(normalizedHash, out package, out _))
                    return true;
            }

            return false;
        }

        // Confirms the indexed file still IS the package being asked for before reusing it. The index
        // records hash to path from when it was last built, so an entry can outlive the content it
        // describes: deleting a sound the .tsv references, or editing the .tsv, leaves the path valid
        // and the file still parseable while the vehicle is no longer what the server sent. Trusting
        // it then hands the race a different car, which is what made a peer show up in the default
        // vehicle for the one player whose local copy had been broken. Rebuilding the package also
        // yields its payload, so a locally reused vehicle can still be written out if kept.
        private bool TryReuseLocalVehicle(string normalizedHash, out DownloadedVehiclePackage package, out bool indexLooksStale)
        {
            package = null!;
            indexLooksStale = false;

            if (!_localVehicleIndex!.TryGetValue(normalizedHash, out var localTsvPath))
                return false;

            if (!File.Exists(localTsvPath))
            {
                indexLooksStale = true;
                return false;
            }

            if (!VehiclePackageBuild.TryBuildPackageFromVehicleFile(
                    localTsvPath,
                    out var payload,
                    out _,
                    out var parsed,
                    out _,
                    GetClientVehiclesFolder())
                || !string.Equals(
                    VehiclePackageRef.NormalizeHash(payload.Manifest.Hash),
                    normalizedHash,
                    StringComparison.OrdinalIgnoreCase))
            {
                indexLooksStale = true;
                return false;
            }

            package = new DownloadedVehiclePackage
            {
                Hash = normalizedHash,
                Payload = payload,
                Vehicle = parsed,
                SourceDirectory = parsed.SourceDirectory,
                TsvPath = Path.GetFullPath(localTsvPath),
                DisplayName = string.IsNullOrWhiteSpace(parsed.Meta?.Name) ? normalizedHash : parsed.Meta!.Name
            };
            _multiplayerVehiclePackageCache[normalizedHash] = package;
            return true;
        }

        // True when a cached entry still holds the content identified by expectedHash. A downloaded
        // package lives in the self-managed session dir, so an existence check on its .tsv is enough.
        // A Vehicles-folder copy can have a referenced sound deleted/renamed/edited out from under it
        // (which changes the content hash but not the .tsv path), so it is re-hashed and compared;
        // a mismatch means the on-disk copy is no longer the package the server wants.
        private bool IsCachedVehiclePackageUsable(DownloadedVehiclePackage cached, string expectedHash)
        {
            if (string.IsNullOrEmpty(cached.TsvPath))
                return true;
            if (!File.Exists(cached.TsvPath))
                return false;

            var sessionRoot = Path.GetFullPath(GetVehiclePackageSessionRoot());
            if (Path.GetFullPath(cached.TsvPath).StartsWith(sessionRoot, StringComparison.OrdinalIgnoreCase))
                return true;

            if (!VehiclePackageBuild.TryBuildPackageFromVehicleFile(cached.TsvPath, out var payload, out _, out _, out _))
                return false;
            return string.Equals(
                VehiclePackageRef.NormalizeHash(payload.Manifest.Hash),
                expectedHash,
                StringComparison.OrdinalIgnoreCase);
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

        // The vehicle's own .tsv filename from the package, sanitized, so a materialized or kept
        // vehicle keeps its real name instead of a generic "vehicle.tsv".
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

        // Kept vehicles are written here and nowhere else. This is the player's own content root,
        // which on platforms that unpack their shipped assets at startup is deliberately a different
        // folder from the one that unpack step clears when the app updates. On desktop it is the
        // same folder as the asset root, so this resolves exactly as it always has.
        private static string GetClientVehiclesFolder()
        {
            return Path.Combine(AssetPaths.UserContentRoot, "Vehicles");
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
