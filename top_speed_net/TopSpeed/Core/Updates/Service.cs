using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Runtime.Serialization.Json;
using System.Threading;
using System.Threading.Tasks;
using TopSpeed.Localization;

namespace TopSpeed.Core.Updates
{
    internal sealed class UpdateService
    {
        private readonly UpdateConfig _config;
        private readonly HttpClient _http;
        private readonly object _proxyLock = new object();
        private bool _useProxy;
        private string _proxyPrefix = string.Empty;

        public UpdateService(UpdateConfig config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _http = new HttpClient();
            _http.DefaultRequestHeaders.UserAgent.ParseAdd("TopSpeedUpdater/1.0");
            _http.Timeout = TimeSpan.FromSeconds(25);
        }

        public void ConfigureProxy(bool enabled, string? urlPrefix)
        {
            lock (_proxyLock)
            {
                _useProxy = enabled;
                _proxyPrefix = (urlPrefix ?? string.Empty).Trim();
            }
        }

        public async Task<UpdateCheckResult> CheckAsync(GameVersion current, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(_config.RuntimeAssetTag))
                return Fail(LocalizationService.Mark("Updates are not available on this platform."));

            try
            {
                var info = await ReadInfoAsync(cancellationToken).ConfigureAwait(false);
                if (info == null)
                    return Fail(LocalizationService.Mark("The update info file could not be read."));

                if (!GameVersion.TryParse(info.Version, out var remoteVersion))
                    return Fail(LocalizationService.Mark("The update info file has an invalid version format."));

                if (remoteVersion.CompareTo(current) <= 0)
                    return new UpdateCheckResult { IsSuccess = true };

                var release = await ReadLatestReleaseAsync(cancellationToken).ConfigureAwait(false);
                var expectedAsset = _config.BuildExpectedAssetName(info.Version ?? string.Empty);
                var asset = FindAsset(release, expectedAsset);
                if (asset == null || string.IsNullOrWhiteSpace(asset.DownloadUrl))
                    return Fail(LocalizationService.Format(
                        LocalizationService.Mark("Update package '{0}' was not found in the latest release."),
                        expectedAsset));

                return new UpdateCheckResult
                {
                    IsSuccess = true,
                    Update = new UpdateInfo
                    {
                        VersionText = info.Version ?? string.Empty,
                        Version = remoteVersion,
                        Changes = info.Changes != null ? (IReadOnlyList<string>)info.Changes : Array.Empty<string>(),
                        DownloadUrl = asset.DownloadUrl ?? string.Empty,
                        AssetSizeBytes = asset.Size ?? 0
                    }
                };
            }
            catch (TaskCanceledException)
            {
                return Fail(LocalizationService.Mark("Update check timed out."));
            }
            catch (Exception ex)
            {
                return Fail(LocalizationService.Format(
                    LocalizationService.Mark("Update check failed: {0}"),
                    ex.Message));
            }
        }

        public async Task<LatestChangesResult> GetLatestChangesAsync(CancellationToken cancellationToken)
        {
            try
            {
                var info = await ReadInfoAsync(cancellationToken).ConfigureAwait(false);
                if (info == null)
                    return FailLatestChanges(LocalizationService.Mark("The update info file could not be read."));

                var changes = new List<string>();
                if (info.Changes != null)
                {
                    for (var i = 0; i < info.Changes.Count; i++)
                    {
                        var line = info.Changes[i];
                        if (string.IsNullOrWhiteSpace(line))
                            continue;
                        changes.Add(line.Trim());
                    }
                }

                return new LatestChangesResult
                {
                    IsSuccess = true,
                    VersionText = info.Version ?? string.Empty,
                    Changes = changes
                };
            }
            catch (TaskCanceledException)
            {
                return FailLatestChanges(LocalizationService.Mark("Latest changes request timed out."));
            }
            catch (Exception ex)
            {
                return FailLatestChanges(LocalizationService.Format(
                    LocalizationService.Mark("Latest changes request failed: {0}"),
                    ex.Message));
            }
        }

        public async Task<DownloadResult> DownloadAsync(
            UpdateInfo update,
            string targetDirectory,
            Action<DownloadProgress> onProgress,
            CancellationToken cancellationToken)
        {
            if (update == null)
                throw new ArgumentNullException(nameof(update));
            if (string.IsNullOrWhiteSpace(targetDirectory))
                throw new ArgumentException("Target directory is required.", nameof(targetDirectory));

            var zipPath = Path.Combine(targetDirectory, _config.BuildExpectedAssetName(update.VersionText));
            var downloadUrl = ResolveProxyUrl(update.DownloadUrl);

            try
            {
                using (var response = await _http.GetAsync(downloadUrl, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false))
                {
                    if (!response.IsSuccessStatusCode)
                        return new DownloadResult
                        {
                            IsSuccess = false,
                            ErrorMessage = LocalizationService.Format(
                                LocalizationService.Mark("Download failed with status code {0}."),
                                (int)response.StatusCode),
                            ZipPath = zipPath
                        };

                    var totalBytes = response.Content.Headers.ContentLength ?? update.AssetSizeBytes;
                    var downloaded = 0L;
                    var lastPercent = -1;
                    var buffer = new byte[81920];

                    using (var stream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false))
                    using (var file = new FileStream(zipPath, FileMode.Create, FileAccess.Write, FileShare.None))
                    {
                        while (true)
                        {
                            var read = await stream.ReadAsync(buffer, 0, buffer.Length, cancellationToken).ConfigureAwait(false);
                            if (read <= 0)
                                break;
                            await file.WriteAsync(buffer, 0, read, cancellationToken).ConfigureAwait(false);
                            downloaded += read;

                            var percent = 0;
                            if (totalBytes > 0)
                                percent = (int)Math.Floor((downloaded * 100d) / totalBytes);
                            if (percent > 100)
                                percent = 100;

                            if (percent != lastPercent || downloaded == totalBytes)
                            {
                                lastPercent = percent;
                                onProgress?.Invoke(new DownloadProgress
                                {
                                    DownloadedBytes = downloaded,
                                    TotalBytes = totalBytes,
                                    Percent = percent
                                });
                            }
                        }
                    }

                    onProgress?.Invoke(new DownloadProgress
                    {
                        DownloadedBytes = downloaded,
                        TotalBytes = totalBytes,
                        Percent = 100
                    });

                    return new DownloadResult
                    {
                        IsSuccess = true,
                        ZipPath = zipPath,
                        TotalBytes = totalBytes
                    };
                }
            }
            catch (TaskCanceledException)
            {
                return new DownloadResult
                {
                    IsSuccess = false,
                    ErrorMessage = LocalizationService.Mark("Download timed out or was canceled."),
                    ZipPath = zipPath
                };
            }
            catch (Exception ex)
            {
                return new DownloadResult
                {
                    IsSuccess = false,
                    ErrorMessage = LocalizationService.Format(
                        LocalizationService.Mark("Download failed: {0}"),
                        ex.Message),
                    ZipPath = zipPath
                };
            }
        }

        private async Task<InfoDoc?> ReadInfoAsync(CancellationToken cancellationToken)
        {
            using (var response = await _http.GetAsync(_config.InfoUrl, cancellationToken).ConfigureAwait(false))
            {
                if (!response.IsSuccessStatusCode)
                    return null;
                using (var stream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false))
                {
                    return ReadJson<InfoDoc>(stream);
                }
            }
        }

        private async Task<ReleaseDoc?> ReadLatestReleaseAsync(CancellationToken cancellationToken)
        {
            using (var response = await _http.GetAsync(_config.LatestReleaseApiUrl, cancellationToken).ConfigureAwait(false))
            {
                if (!response.IsSuccessStatusCode)
                    return null;
                using (var stream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false))
                {
                    return ReadJson<ReleaseDoc>(stream);
                }
            }
        }

        private static T? ReadJson<T>(Stream stream) where T : class
        {
            var serializer = new DataContractJsonSerializer(typeof(T));
            return serializer.ReadObject(stream) as T;
        }

        private string ResolveProxyUrl(string? url)
        {
            var value = (url ?? string.Empty).Trim();
            if (value.Length == 0)
                return value;

            bool useProxy;
            string prefix;
            lock (_proxyLock)
            {
                useProxy = _useProxy;
                prefix = _proxyPrefix;
            }

            if (!useProxy || string.IsNullOrWhiteSpace(prefix))
                return value;

            var trimmedPrefix = prefix.Trim();
            if (value.StartsWith(trimmedPrefix, StringComparison.OrdinalIgnoreCase))
                return value;

            if (!trimmedPrefix.EndsWith("/", StringComparison.Ordinal))
                trimmedPrefix += "/";

            return trimmedPrefix + value;
        }

        private static ReleaseAssetDoc? FindAsset(ReleaseDoc? release, string expectedName)
        {
            if (release?.Assets == null || release.Assets.Count == 0)
                return null;

            for (var i = 0; i < release.Assets.Count; i++)
            {
                var asset = release.Assets[i];
                if (asset == null || string.IsNullOrWhiteSpace(asset.Name))
                    continue;
                var assetName = asset.Name ?? string.Empty;
                if (!string.Equals(assetName.Trim(), expectedName, StringComparison.OrdinalIgnoreCase))
                    continue;
                return asset;
            }

            return null;
        }

        private static UpdateCheckResult Fail(string message)
        {
            return new UpdateCheckResult
            {
                IsSuccess = false,
                ErrorMessage = message ?? LocalizationService.Mark("Unknown update error.")
            };
        }

        private static LatestChangesResult FailLatestChanges(string message)
        {
            return new LatestChangesResult
            {
                IsSuccess = false,
                ErrorMessage = message ?? LocalizationService.Mark("Unknown update error.")
            };
        }
    }
}

