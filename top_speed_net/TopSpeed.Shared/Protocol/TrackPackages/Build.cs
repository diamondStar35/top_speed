using System;
using TopSpeed.Data;

namespace TopSpeed.Protocol
{
    public static partial class PackageBuild
    {
        public static bool TryBuildPackageFromTrackFile(
            string trackFile,
            string displayName,
            int fallbackLaps,
            out TrackPackagePayload payload,
            out byte[] bytes,
            out string error)
        {
            payload = new TrackPackagePayload();
            bytes = Array.Empty<byte>();
            error = string.Empty;

            if (!TrackTsmParser.TryLoadFromFile(trackFile, out var trackData, out var issues))
            {
                error = BuildTrackLoadError(issues);
                return false;
            }

            if (!TryBuildPayload(trackData, trackFile, displayName, fallbackLaps, out payload, out error))
                return false;

            var hash = TrackPackageCodec.ComputeHash(payload);
            payload.Manifest.Hash = hash;
            if (!TrackPackageCodec.TryValidate(payload, out error))
                return false;

            bytes = TrackPackageCodec.Serialize(payload);
            return true;
        }
    }
}

