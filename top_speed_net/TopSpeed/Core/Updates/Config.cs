using System;
using TopSpeed.Protocol;
using TopSpeed.Runtime;

namespace TopSpeed.Core.Updates
{
    internal sealed class UpdateConfig
    {
        private const string RepoOwner = "diamondStar35";
        private const string RepoName = "top_speed";

        public UpdateConfig(
            string infoUrl,
            string latestReleaseApiUrl,
            string assetTemplate,
            string runtimeAssetTag,
            string updaterEntryName,
            string gameEntryName)
        {
            InfoUrl = infoUrl ?? throw new ArgumentNullException(nameof(infoUrl));
            LatestReleaseApiUrl = latestReleaseApiUrl ?? throw new ArgumentNullException(nameof(latestReleaseApiUrl));
            AssetTemplate = assetTemplate ?? throw new ArgumentNullException(nameof(assetTemplate));
            RuntimeAssetTag = runtimeAssetTag ?? throw new ArgumentNullException(nameof(runtimeAssetTag));
            UpdaterEntryName = updaterEntryName ?? throw new ArgumentNullException(nameof(updaterEntryName));
            GameEntryName = gameEntryName ?? throw new ArgumentNullException(nameof(gameEntryName));
        }

        public string InfoUrl { get; }
        public string LatestReleaseApiUrl { get; }
        public string AssetTemplate { get; }
        public string RuntimeAssetTag { get; }
        public string UpdaterEntryName { get; }
        public string GameEntryName { get; }

        public static UpdateConfig Default { get; } = CreateDefault();

        public static GameVersion CurrentVersion =>
            new GameVersion(
                ReleaseVersionInfo.ClientYear,
                ReleaseVersionInfo.ClientMonth,
                ReleaseVersionInfo.ClientDay,
                ReleaseVersionInfo.ClientRevision);

        public string BuildExpectedAssetName(string version)
        {
            return AssetTemplate
                .Replace("{runtime}", RuntimeAssetTag)
                .Replace("{version}", version ?? string.Empty)
                .Replace("{ext}", ResolvePackageExtension(RuntimeAssetTag));
        }

        private static UpdateConfig CreateDefault()
        {
            var runtimeAssetTag = ResolveRuntimeAssetTag();
            return new UpdateConfig(
                $"https://raw.githubusercontent.com/{RepoOwner}/{RepoName}/main/info.json",
                $"https://api.github.com/repos/{RepoOwner}/{RepoName}/releases/latest",
                "TopSpeed-{runtime}-Release-v-{version}{ext}",
                runtimeAssetTag,
                "TopSpeedUpdater",
                "TopSpeed");
        }

        /// <summary>
        /// The name the updater shipped under before it could replace itself. Every install from
        /// before then still has one, and it is what runs the first update after, since the new
        /// one only arrives by being unpacked by the old.
        /// </summary>
        public const string LegacyUpdaterEntryName = "Updater";

        private static string ResolveRuntimeAssetTag()
        {
            try
            {
                return RuntimeAssetResolver.DetectClientRuntimeAssetTag();
            }
            catch (PlatformNotSupportedException)
            {
                return string.Empty;
            }
        }

        private static string ResolvePackageExtension(string runtimeAssetTag)
        {
            return runtimeAssetTag.StartsWith("android", StringComparison.OrdinalIgnoreCase)
                ? ".apk"
                : ".zip";
        }
    }
}

