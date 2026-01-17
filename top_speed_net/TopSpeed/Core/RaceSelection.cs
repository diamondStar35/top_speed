using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TopSpeed.Common;
using TopSpeed.Data;
using TopSpeed.Input;

namespace TopSpeed.Core
{
    internal sealed class RaceSelection
    {
        private readonly RaceSetup _setup;
        private readonly RaceSettings _settings;
        private readonly Dictionary<string, (DateTime LastWriteUtc, string Display)> _customTrackCache =
            new Dictionary<string, (DateTime LastWriteUtc, string Display)>(StringComparer.OrdinalIgnoreCase);

        public RaceSelection(RaceSetup setup, RaceSettings settings)
        {
            _setup = setup ?? throw new ArgumentNullException(nameof(setup));
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        }

        public void SelectTrack(TrackCategory category, string trackKey)
        {
            _setup.TrackCategory = category;
            _setup.TrackNameOrFile = trackKey;
        }

        public void SelectRandomTrack(TrackCategory category)
        {
            SelectRandomTrack(category, _settings.RandomCustomTracks);
        }

        public void SelectRandomTrack(TrackCategory category, bool includeCustom)
        {
            if (category == TrackCategory.CustomTrack)
            {
                SelectRandomCustomTrack();
                return;
            }
            var customTracks = includeCustom ? GetCustomTrackFiles() : Array.Empty<string>();
            _setup.TrackCategory = category;
            _setup.TrackNameOrFile = TrackList.GetRandomTrackKey(category, customTracks);
        }

        public void SelectRandomTrackAny(bool includeCustom)
        {
            var customTracks = includeCustom ? GetCustomTrackFiles() : Array.Empty<string>();
            var pick = TrackList.GetRandomTrackAny(customTracks);
            _setup.TrackCategory = pick.Category;
            _setup.TrackNameOrFile = pick.Key;
        }

        public void SelectRandomCustomTrack()
        {
            var customTracks = GetCustomTrackFiles().ToList();
            if (customTracks.Count == 0)
            {
                SelectTrack(TrackCategory.RaceTrack, TrackList.RaceTracks[0].Key);
                return;
            }

            var index = Algorithm.RandomInt(customTracks.Count);
            SelectTrack(TrackCategory.CustomTrack, customTracks[index]);
        }

        public void SelectRandomCustomExploreTrack()
        {
            var customTracks = GetCustomMapTrackFiles().ToList();
            if (customTracks.Count == 0)
            {
                SelectTrack(TrackCategory.CustomTrack, TrackList.RaceTracks[0].Key);
                return;
            }

            var index = Algorithm.RandomInt(customTracks.Count);
            SelectTrack(TrackCategory.CustomTrack, customTracks[index]);
        }

        public void SelectVehicle(int index)
        {
            _setup.VehicleIndex = index;
            _setup.VehicleFile = null;
        }

        public void SelectCustomVehicle(string file)
        {
            _setup.VehicleIndex = null;
            _setup.VehicleFile = file;
        }

        public void SelectRandomVehicle()
        {
            var customFiles = _settings.RandomCustomVehicles ? GetCustomVehicleFiles().ToList() : new List<string>();
            var total = VehicleCatalog.VehicleCount + customFiles.Count;
            if (total <= 0)
            {
                SelectVehicle(0);
                return;
            }

            var roll = Algorithm.RandomInt(total);
            if (roll < VehicleCatalog.VehicleCount)
            {
                SelectVehicle(roll);
                return;
            }

            var customIndex = roll - VehicleCatalog.VehicleCount;
            if (customIndex >= 0 && customIndex < customFiles.Count)
                SelectCustomVehicle(customFiles[customIndex]);
            else
                SelectVehicle(0);
        }

        public IEnumerable<string> GetCustomTrackFiles()
        {
            var root = Path.Combine(AssetPaths.Root, "Tracks");
            if (!Directory.Exists(root))
                return Array.Empty<string>();
            return Directory.EnumerateFiles(root, "*.tsm", SearchOption.TopDirectoryOnly);
        }

        public IEnumerable<string> GetCustomMapTrackFiles()
        {
            return GetCustomTrackFiles();
        }

        public IReadOnlyList<TrackInfo> GetCustomTrackInfo()
        {
            var files = GetCustomTrackFiles().ToList();
            return BuildCustomTrackInfo(files);
        }

        public IReadOnlyList<TrackInfo> GetCustomMapTrackInfo()
        {
            return GetCustomTrackInfo();
        }

        public IEnumerable<string> GetCustomVehicleFiles()
        {
            var root = Path.Combine(AssetPaths.Root, "Vehicles");
            if (!Directory.Exists(root))
                return Array.Empty<string>();
            return Directory.EnumerateFiles(root, "*.vhc", SearchOption.TopDirectoryOnly);
        }

        private string ResolveCustomTrackDisplayName(string file)
        {
            var display = TryReadCustomTrackName(file);
            if (string.IsNullOrWhiteSpace(display))
                display = Path.GetFileNameWithoutExtension(file);
            return string.IsNullOrWhiteSpace(display) ? "Custom track" : display!;
        }

        private string? TryReadCustomTrackName(string file)
        {
            try
            {
                var lastWrite = File.GetLastWriteTimeUtc(file);
                if (_customTrackCache.TryGetValue(file, out var cached) && cached.LastWriteUtc == lastWrite)
                    return cached.Display;

                string? parsed = null;
                foreach (var line in File.ReadLines(file))
                {
                    var trimmed = line.Trim();
                    if (TryParseNameLine(trimmed, out var name))
                    {
                        parsed = name;
                        break;
                    }

                    if (LooksLikeTrackDataLine(trimmed))
                        break;
                }

                var display = string.IsNullOrWhiteSpace(parsed)
                    ? Path.GetFileNameWithoutExtension(file)
                    : parsed;

                display = string.IsNullOrWhiteSpace(display) ? "Custom track" : display!;
                _customTrackCache[file] = (lastWrite, display);
                return display;
            }
            catch
            {
                return null;
            }
        }

        private IReadOnlyList<TrackInfo> BuildCustomTrackInfo(List<string> files)
        {
            if (files.Count == 0)
            {
                _customTrackCache.Clear();
                return Array.Empty<TrackInfo>();
            }

            var items = new List<TrackInfo>(files.Count);
            var known = new HashSet<string>(files, StringComparer.OrdinalIgnoreCase);
            foreach (var file in files)
            {
                var display = ResolveCustomTrackDisplayName(file);
                items.Add(new TrackInfo(file, display));
            }

            var staleKeys = _customTrackCache.Keys.Where(key => !known.Contains(key)).ToList();
            foreach (var key in staleKeys)
                _customTrackCache.Remove(key);

            return items
                .OrderBy(item => item.Display, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private static bool TryParseNameLine(string line, out string name)      
        {
            name = string.Empty;
            if (string.IsNullOrWhiteSpace(line))
                return false;

            var trimmed = line.Trim();
            if (trimmed.StartsWith("#", StringComparison.Ordinal) ||
                trimmed.StartsWith(";", StringComparison.Ordinal))
            {
                trimmed = trimmed.Substring(1).TrimStart();
            }

            var separatorIndex = trimmed.IndexOf('=');
            if (separatorIndex < 0)
                separatorIndex = trimmed.IndexOf(':');
            if (separatorIndex <= 0)
                return false;

            var key = trimmed.Substring(0, separatorIndex).Trim();
            if (!key.Equals("name", StringComparison.OrdinalIgnoreCase) &&
                !key.Equals("trackname", StringComparison.OrdinalIgnoreCase) &&
                !key.Equals("title", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            var value = trimmed.Substring(separatorIndex + 1).Trim().Trim('"');
            if (string.IsNullOrWhiteSpace(value))
                return false;

            name = value;
            return true;
        }

        private static bool LooksLikeTrackDataLine(string line)
        {
            if (string.IsNullOrWhiteSpace(line))
                return false;

            var trimmed = line.Trim();
            if (trimmed.StartsWith("#", StringComparison.Ordinal) ||
                trimmed.StartsWith(";", StringComparison.Ordinal))
            {
                return false;
            }

            var parts = trimmed.Split((char[])null!, StringSplitOptions.RemoveEmptyEntries);
            foreach (var part in parts)
            {
                if (int.TryParse(part, out _))
                    return true;
            }

            return false;
        }
    }
}
