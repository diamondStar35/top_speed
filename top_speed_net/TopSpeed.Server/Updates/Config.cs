using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using TopSpeed.Protocol;
using TopSpeed.Runtime;

namespace TopSpeed.Server.Updates
{
    internal sealed class ServerUpdateConfig
    {
        private const string RepoOwner = "diamondStar35";
        private const string RepoName = "top_speed";
        public const string AutoRuntimeAssetTag = "auto";

        private static readonly RuntimeAssetOption[] RuntimeOptions =
        {
            new RuntimeAssetOption("Automatic", AutoRuntimeAssetTag),
            new RuntimeAssetOption("Windows 64-bit", "win-x64"),
            new RuntimeAssetOption("Mac x64", "mac-x64"),
            new RuntimeAssetOption("Mac ARM64", "mac-arm64"),
            new RuntimeAssetOption("Linux 64-bit", "linux-x64"),
            new RuntimeAssetOption("Linux ARM64", "linux-arm64"),
            new RuntimeAssetOption("Linux ARM32", "linux-arm32"),
            new RuntimeAssetOption("Linux MUSL 64-bit", "linux-musl-x64"),
            new RuntimeAssetOption("Linux MUSL ARM64", "linux-musl-arm64"),
            new RuntimeAssetOption("Linux x86 Framework-Dependent", "linux-x86-fdd")
        };

        public ServerUpdateConfig(
            string infoUrl,
            string latestReleaseApiUrl,
            string assetTemplate,
            string runtimeAssetTag,
            string updaterEntryName,
            string serverEntryName)
        {
            InfoUrl = infoUrl ?? throw new ArgumentNullException(nameof(infoUrl));
            LatestReleaseApiUrl = latestReleaseApiUrl ?? throw new ArgumentNullException(nameof(latestReleaseApiUrl));
            AssetTemplate = assetTemplate ?? throw new ArgumentNullException(nameof(assetTemplate));
            RuntimeAssetTag = runtimeAssetTag ?? throw new ArgumentNullException(nameof(runtimeAssetTag));
            UpdaterEntryName = updaterEntryName ?? throw new ArgumentNullException(nameof(updaterEntryName));
            ServerEntryName = serverEntryName ?? throw new ArgumentNullException(nameof(serverEntryName));
        }

        public string InfoUrl { get; }
        public string LatestReleaseApiUrl { get; }
        public string AssetTemplate { get; }
        public string RuntimeAssetTag { get; }
        public string UpdaterEntryName { get; }
        public string ServerEntryName { get; }

        public static ServerUpdateConfig Default { get; } = Create(AutoRuntimeAssetTag);

        public static ServerUpdateConfig Create(string? configuredRuntimeAssetTag)
        {
            return new ServerUpdateConfig(
            $"https://raw.githubusercontent.com/{RepoOwner}/{RepoName}/main/info.json",
            $"https://api.github.com/repos/{RepoOwner}/{RepoName}/releases/latest",
            "TopSpeed.Server-{runtime}-Release-v-{version}.zip",
            ResolveRuntimeAssetTag(configuredRuntimeAssetTag),
            "TopSpeedUpdater",
            "TopSpeed.Server");
        }

        /// <summary>
        /// The name the updater shipped under before it could replace itself. Every folder that
        /// was installed before then still has one, and it is what runs the first update after,
        /// since the new one only arrives by being unpacked by the old.
        /// </summary>
        public const string LegacyUpdaterEntryName = "Updater";

        public static ServerVersion CurrentVersion =>
            new ServerVersion(
                ReleaseVersionInfo.ServerYear,
                ReleaseVersionInfo.ServerMonth,
                ReleaseVersionInfo.ServerDay,
                ReleaseVersionInfo.ServerRevision);

        public string BuildExpectedAssetName(string versionText)
        {
            return AssetTemplate
                .Replace("{runtime}", RuntimeAssetTag)
                .Replace("{version}", versionText ?? string.Empty);
        }

        public static string NormalizeConfiguredRuntimeAssetTag(string? value)
        {
            var raw = (value ?? string.Empty).Trim().ToLowerInvariant();
            if (raw.Length == 0 || raw == AutoRuntimeAssetTag)
                return AutoRuntimeAssetTag;

            for (var i = 0; i < RuntimeOptions.Length; i++)
            {
                if (string.Equals(RuntimeOptions[i].ShortName, raw, StringComparison.OrdinalIgnoreCase))
                    return RuntimeOptions[i].ShortName;
            }

            return AutoRuntimeAssetTag;
        }

        public static IReadOnlyList<RuntimeAssetOption> GetRuntimeOptions()
        {
            return RuntimeOptions;
        }

        public static string FormatRuntimeOptionLabel(RuntimeAssetOption option)
        {
            return option.LongName + " (" + option.ShortName + ")";
        }

        public static string ResolveCurrentRuntimeLabel(string? configuredRuntimeAssetTag)
        {
            var normalized = NormalizeConfiguredRuntimeAssetTag(configuredRuntimeAssetTag);
            if (normalized == AutoRuntimeAssetTag)
            {
                var detected = DetectRuntimeAssetTag();
                return "Automatic (" + detected + ")";
            }

            for (var i = 0; i < RuntimeOptions.Length; i++)
            {
                if (string.Equals(RuntimeOptions[i].ShortName, normalized, StringComparison.OrdinalIgnoreCase))
                    return FormatRuntimeOptionLabel(RuntimeOptions[i]);
            }

            return "Automatic (" + DetectRuntimeAssetTag() + ")";
        }

        private static string ResolveRuntimeAssetTag(string? configuredRuntimeAssetTag)
        {
            var normalized = NormalizeConfiguredRuntimeAssetTag(configuredRuntimeAssetTag);
            if (normalized == AutoRuntimeAssetTag)
                return DetectRuntimeAssetTag();
            return normalized;
        }

        private static string DetectRuntimeAssetTag()
        {
            return RuntimeAssetResolver.DetectServerRuntimeAssetTag();
        }
    }

    internal readonly struct RuntimeAssetOption
    {
        public RuntimeAssetOption(string longName, string shortName)
        {
            LongName = longName ?? string.Empty;
            ShortName = shortName ?? string.Empty;
        }

        public string LongName { get; }
        public string ShortName { get; }
    }
}
