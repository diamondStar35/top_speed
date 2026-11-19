using System;
using System.IO;
using TopSpeed.Core;
using TopSpeed.Protocol;

namespace TopSpeed.Game
{
    internal sealed partial class Game
    {
        private bool TryGetCachedTrackPackage(string hash, out TrackPackagePayload payload)
        {
            payload = new TrackPackagePayload();
            var normalizedHash = TrackPackageRef.NormalizeHash(hash);
            if (string.IsNullOrWhiteSpace(normalizedHash))
                return false;

            if (_multiplayerTrackPackageCache.TryGetValue(normalizedHash, out var cached))
            {
                payload = cached;
                return true;
            }

            var filePath = GetTrackPackageCacheFilePath(normalizedHash);
            if (!File.Exists(filePath))
                return false;

            try
            {
                var bytes = File.ReadAllBytes(filePath);
                if (bytes.Length == 0 || bytes.Length > ProtocolConstants.MaxTrackPackageBytes)
                    return false;
                if (!TrackPackageCodec.TryDeserialize(bytes, out var loaded, out _))
                    return false;

                var computedHash = TrackPackageCodec.ComputeHash(loaded);
                if (!string.Equals(computedHash, normalizedHash, StringComparison.OrdinalIgnoreCase))
                    return false;

                loaded.Manifest.Hash = computedHash;
                MaterializeTrackPackageAssets(computedHash, loaded);
                _multiplayerTrackPackageCache[computedHash] = loaded;
                payload = loaded;
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

        private void SaveTrackPackageToCache(string hash, TrackPackagePayload payload, byte[] bytes)
        {
            var normalizedHash = TrackPackageRef.NormalizeHash(hash);
            if (string.IsNullOrWhiteSpace(normalizedHash) || payload == null || bytes == null || bytes.Length == 0)
                return;

            payload.Manifest.Hash = normalizedHash;
            _multiplayerTrackPackageCache[normalizedHash] = payload;

            try
            {
                var cacheDir = GetTrackPackageCacheDirectory();
                Directory.CreateDirectory(cacheDir);
                var filePath = GetTrackPackageCacheFilePath(normalizedHash);
                var tempPath = filePath + ".tmp";
                File.WriteAllBytes(tempPath, bytes);
                File.Copy(tempPath, filePath, overwrite: true);
                File.Delete(tempPath);
            }
            catch (IOException)
            {
            }
            catch (UnauthorizedAccessException)
            {
            }

            MaterializeTrackPackageAssets(normalizedHash, payload);
        }

        private static string GetTrackPackageCacheDirectory()
        {
            return Path.Combine(AppData.Root(), "track_packages");
        }

        private static string GetTrackPackageCacheFilePath(string hash)
        {
            var normalizedHash = TrackPackageRef.NormalizeHash(hash);
            return Path.Combine(GetTrackPackageCacheDirectory(), normalizedHash + ".tspkg");
        }

        private static string GetTrackPackageAssetsDirectory(string hash)
        {
            var normalizedHash = TrackPackageRef.NormalizeHash(hash);
            return Path.Combine(GetTrackPackageCacheDirectory(), normalizedHash, "assets");
        }

        private static string GetTrackPackageAssetRootMarkerPath(string hash)
        {
            return Path.Combine(GetTrackPackageAssetsDirectory(hash), "_package_assets_root");
        }
    }
}
