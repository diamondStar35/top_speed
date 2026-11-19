using System;
using System.Collections.Generic;
using System.IO;
using TopSpeed.Protocol;

namespace TopSpeed.Game
{
    internal sealed partial class Game
    {
        private static void MaterializeTrackPackageAssets(string hash, TrackPackagePayload payload)
        {
            var assets = payload.AssetBlobs ?? new Dictionary<string, byte[]>(StringComparer.OrdinalIgnoreCase);
            if (assets.Count == 0)
                return;

            try
            {
                var root = Path.GetFullPath(GetTrackPackageAssetsDirectory(hash));
                Directory.CreateDirectory(root);
                foreach (var pair in assets)
                {
                    var key = TrackPackageCodec.NormalizeAssetKey(pair.Key ?? string.Empty);
                    if (string.IsNullOrWhiteSpace(key))
                        continue;

                    var relative = key.Replace('/', Path.DirectorySeparatorChar);
                    var candidate = Path.GetFullPath(Path.Combine(root, relative));
                    if (!IsInsideRoot(candidate, root))
                        continue;

                    var parent = Path.GetDirectoryName(candidate);
                    if (!string.IsNullOrWhiteSpace(parent))
                        Directory.CreateDirectory(parent);

                    var bytes = pair.Value ?? Array.Empty<byte>();
                    var tempPath = candidate + ".tmp";
                    File.WriteAllBytes(tempPath, bytes);
                    File.Copy(tempPath, candidate, overwrite: true);
                    File.Delete(tempPath);
                }
            }
            catch (IOException)
            {
            }
            catch (UnauthorizedAccessException)
            {
            }
        }

        private static bool IsInsideRoot(string candidate, string root)
        {
            if (string.Equals(candidate, root, StringComparison.OrdinalIgnoreCase))
                return true;

            var rootPrefix = root.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
            return candidate.StartsWith(rootPrefix, StringComparison.OrdinalIgnoreCase);
        }
    }
}
