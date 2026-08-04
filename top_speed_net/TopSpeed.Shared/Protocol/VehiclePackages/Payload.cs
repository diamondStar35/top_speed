using System;
using System.Collections.Generic;

namespace TopSpeed.Protocol
{
    public sealed class VehiclePackagePayload
    {
        public VehiclePackageManifest Manifest = new VehiclePackageManifest();

        // Raw .tsv text as authored by the server owner. Both sides parse this with
        // the shared VehicleTsvParser, so remote clients reproduce the exact
        // single-player custom-vehicle load path (no per-field codec drift).
        public string TsvText = string.Empty;

        // Referenced sound files, keyed by their normalized relative path exactly as
        // written in the .tsv, so materializing them next to the .tsv resolves cleanly.
        public IReadOnlyDictionary<string, byte[]> AssetBlobs =
            new Dictionary<string, byte[]>(StringComparer.OrdinalIgnoreCase);
    }
}
