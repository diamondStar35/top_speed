namespace TopSpeed.Protocol
{
    public sealed class VehiclePackageManifest
    {
        public string VehicleId = string.Empty;
        public string Version = string.Empty;
        public string Hash = string.Empty;
        public string DisplayName = string.Empty;
        // Original on-disk names so a kept copy reproduces the source layout (not part of the content
        // hash). TsvFileName e.g. "chevy laguna.tsv"; FolderName e.g. "Chevy Laguna".
        public string TsvFileName = string.Empty;
        public string FolderName = string.Empty;
    }
}
