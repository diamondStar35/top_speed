namespace TopSpeed.Protocol
{
    public sealed class VehiclePackageManifest
    {
        public string VehicleId = string.Empty;
        public string Version = string.Empty;
        public string Hash = string.Empty;
        public string DisplayName = string.Empty;
        // Original on-disk names so a kept copy reproduces the source layout (not part of the content
        // hash). TsvFileName is the vehicle's own file name; FolderName is its folder path relative
        // to the Vehicles root, so any grouping folders above it are preserved too.
        public string TsvFileName = string.Empty;
        public string FolderName = string.Empty;
    }
}
