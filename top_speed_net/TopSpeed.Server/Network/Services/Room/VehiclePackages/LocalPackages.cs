using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TopSpeed.Localization;
using TopSpeed.Protocol;

namespace TopSpeed.Server.Network
{
    internal sealed partial class RaceServer
    {
        private void RefreshServerVehiclePackages()
        {
            var vehiclesRoot = GetServerVehiclesDirectory();
            var discovered = new Dictionary<string, DateTime>(StringComparer.OrdinalIgnoreCase);
            if (!Directory.Exists(vehiclesRoot))
            {
                RemoveStaleServerVehiclePackages(discovered);
                return;
            }

            var files = EnumerateServerVehicleFiles(vehiclesRoot);
            foreach (var file in files)
            {
                if (string.IsNullOrWhiteSpace(file))
                    continue;

                DateTime lastWriteUtc;
                try
                {
                    lastWriteUtc = File.GetLastWriteTimeUtc(file);
                }
                catch (IOException)
                {
                    continue;
                }
                catch (UnauthorizedAccessException)
                {
                    continue;
                }

                discovered[file] = lastWriteUtc;
                if (HasServerVehiclePackageForSource(file, lastWriteUtc))
                    continue;

                if (!VehiclePackageBuild.TryBuildPackageFromVehicleFile(file, out var payload, out var bytes, out var parsed, out var error))
                {
                    _logger.Warning(LocalizationService.Format(
                        LocalizationService.Mark("Skipping server vehicle package '{0}': {1}"),
                        file,
                        error));
                    continue;
                }

                StoreVehiclePackage(payload, bytes, parsed, file, lastWriteUtc);
            }

            RemoveStaleServerVehiclePackages(discovered);
        }

        private void RemoveStaleServerVehiclePackages(IReadOnlyDictionary<string, DateTime> discovered)
        {
            var keys = _vehiclePackageCache
                .Where(pair =>
                {
                    var sourcePath = pair.Value?.SourcePath ?? string.Empty;
                    if (string.IsNullOrWhiteSpace(sourcePath))
                        return true;
                    if (!discovered.TryGetValue(sourcePath, out var sourceLastWriteUtc))
                        return true;
                    return pair.Value!.SourceLastWriteUtc != sourceLastWriteUtc;
                })
                .Select(pair => pair.Key)
                .ToArray();

            for (var i = 0; i < keys.Length; i++)
            {
                if (IsVehiclePackageInUse(keys[i]))
                    continue;
                _vehiclePackageCache.Remove(keys[i]);
            }
        }

        private bool HasServerVehiclePackageForSource(string sourcePath, DateTime sourceLastWriteUtc)
        {
            foreach (var package in _vehiclePackageCache.Values)
            {
                if (package == null)
                    continue;
                if (!string.Equals(package.SourcePath, sourcePath, StringComparison.OrdinalIgnoreCase))
                    continue;
                if (package.SourceLastWriteUtc != sourceLastWriteUtc)
                    continue;
                return true;
            }

            return false;
        }

        private static string GetServerVehiclesDirectory()
        {
            return Path.Combine(AppContext.BaseDirectory, "Vehicles");
        }

        private static IReadOnlyList<string> EnumerateServerVehicleFiles(string vehiclesRoot)
        {
            if (!Directory.Exists(vehiclesRoot))
                return Array.Empty<string>();

            try
            {
                return Directory.EnumerateFiles(vehiclesRoot, "*.tsv", SearchOption.AllDirectories)
                    .Select(Path.GetFullPath)
                    .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
                    .ToArray();
            }
            catch (IOException)
            {
                return Array.Empty<string>();
            }
            catch (UnauthorizedAccessException)
            {
                return Array.Empty<string>();
            }
        }
    }
}
