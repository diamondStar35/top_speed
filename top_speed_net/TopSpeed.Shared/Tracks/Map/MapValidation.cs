using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Numerics;
using TopSpeed.Data;
using TopSpeed.Tracks.Areas;
using TopSpeed.Tracks.Beacons;
using TopSpeed.Tracks.Guidance;
using TopSpeed.Tracks.Markers;
using TopSpeed.Tracks.Sectors;
using TopSpeed.Tracks.Topology;

namespace TopSpeed.Tracks.Map
{
    public enum TrackMapIssueSeverity
    {
        Warning = 0,
        Error = 1
    }

    public sealed class TrackMapIssue
    {
        public TrackMapIssue(TrackMapIssueSeverity severity, string message, int? lineNumber = null, int? cellX = null, int? cellZ = null)
        {
            Severity = severity;
            Message = message;
            LineNumber = lineNumber;
            CellX = cellX;
            CellZ = cellZ;
        }

        public TrackMapIssueSeverity Severity { get; }
        public string Message { get; }
        public int? LineNumber { get; }
        public int? CellX { get; }
        public int? CellZ { get; }

        public override string ToString()
        {
            var location = LineNumber.HasValue ? $"line {LineNumber}" : null;
            if (CellX.HasValue && CellZ.HasValue)
            {
                var cell = $"cell ({CellX},{CellZ})";
                location = string.IsNullOrWhiteSpace(location) ? cell : $"{location}, {cell}";
            }
            return string.IsNullOrWhiteSpace(location)
                ? $"{Severity}: {Message}"
                : $"{Severity}: {Message} ({location})";
        }
    }

    public sealed class TrackMapValidationOptions
    {
        public bool RequireSafeZones { get; set; }
        public bool RequireIntersections { get; set; }
        public bool TreatUnreachableCellsAsErrors { get; set; }
    }

    public sealed class TrackMapValidationResult
    {
        public TrackMapValidationResult(IReadOnlyList<TrackMapIssue> issues)
        {
            Issues = issues ?? Array.Empty<TrackMapIssue>();
        }

        public IReadOnlyList<TrackMapIssue> Issues { get; }

        public bool IsValid => Issues.All(issue => issue.Severity != TrackMapIssueSeverity.Error);
    }

    public sealed class TrackMapMetadata
    {
        public string Name { get; set; } = "Track";
        public float CellSizeMeters { get; set; } = 1f;
        public TrackWeather Weather { get; set; } = TrackWeather.Sunny;
        public TrackAmbience Ambience { get; set; } = TrackAmbience.NoAmbience;
        public TrackSurface DefaultSurface { get; set; } = TrackSurface.Asphalt;
        public TrackNoise DefaultNoise { get; set; } = TrackNoise.NoNoise;
        public float DefaultWidthMeters { get; set; } = 12f;
        public int StartX { get; set; }
        public int StartZ { get; set; }
        public MapDirection StartHeading { get; set; } = MapDirection.North;
    }

    public sealed class TrackMapCellDefinition
    {
        public MapExits Exits { get; set; }
        public TrackSurface Surface { get; set; }
        public TrackNoise Noise { get; set; }
        public float WidthMeters { get; set; }
        public bool IsSafeZone { get; set; }
        public string? Zone { get; set; }
    }

    public sealed class TrackMapDefinition
    {
        public TrackMapDefinition(TrackMapMetadata metadata, Dictionary<(int X, int Z), TrackMapCellDefinition> cells)
        {
            Metadata = metadata;
            Cells = cells;
            Sectors = Array.Empty<TrackSectorDefinition>();
            Areas = Array.Empty<TrackAreaDefinition>();
            Shapes = Array.Empty<ShapeDefinition>();
            Portals = Array.Empty<PortalDefinition>();
            Links = Array.Empty<LinkDefinition>();
            Paths = Array.Empty<PathDefinition>();
            Beacons = Array.Empty<TrackBeaconDefinition>();
            Markers = Array.Empty<TrackMarkerDefinition>();
            Approaches = Array.Empty<TrackApproachDefinition>();
        }

        public TrackMapDefinition(
            TrackMapMetadata metadata,
            Dictionary<(int X, int Z), TrackMapCellDefinition> cells,
            List<TrackSectorDefinition> sectors,
            List<TrackAreaDefinition> areas,
            List<ShapeDefinition> shapes,
            List<PortalDefinition> portals,
            List<LinkDefinition> links,
            List<PathDefinition> paths,
            List<TrackBeaconDefinition> beacons,
            List<TrackMarkerDefinition> markers,
            List<TrackApproachDefinition> approaches)
        {
            Metadata = metadata;
            Cells = cells;
            Sectors = sectors ?? new List<TrackSectorDefinition>();
            Areas = areas ?? new List<TrackAreaDefinition>();
            Shapes = shapes ?? new List<ShapeDefinition>();
            Portals = portals ?? new List<PortalDefinition>();
            Links = links ?? new List<LinkDefinition>();
            Paths = paths ?? new List<PathDefinition>();
            Beacons = beacons ?? new List<TrackBeaconDefinition>();
            Markers = markers ?? new List<TrackMarkerDefinition>();
            Approaches = approaches ?? new List<TrackApproachDefinition>();
        }

        public TrackMapMetadata Metadata { get; }
        public IReadOnlyDictionary<(int X, int Z), TrackMapCellDefinition> Cells { get; }
        public IReadOnlyList<TrackSectorDefinition> Sectors { get; }
        public IReadOnlyList<TrackAreaDefinition> Areas { get; }
        public IReadOnlyList<ShapeDefinition> Shapes { get; }
        public IReadOnlyList<PortalDefinition> Portals { get; }
        public IReadOnlyList<LinkDefinition> Links { get; }
        public IReadOnlyList<PathDefinition> Paths { get; }
        public IReadOnlyList<TrackBeaconDefinition> Beacons { get; }
        public IReadOnlyList<TrackMarkerDefinition> Markers { get; }
        public IReadOnlyList<TrackApproachDefinition> Approaches { get; }
    }

    public static class TrackMapFormat
    {
        private const string MapExtension = ".tsm";
        private static readonly HashSet<string> SectorKnownKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "id",
            "type",
            "name",
            "code",
            "area",
            "shape",
            "surface",
            "noise",
            "flags",
            "flag",
            "caps",
            "capabilities"
        };
        private static readonly HashSet<string> AreaKnownKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "id",
            "type",
            "name",
            "shape",
            "surface",
            "noise",
            "width",
            "flags",
            "flag",
            "caps",
            "capabilities"
        };
        private static readonly HashSet<string> BeaconKnownKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "id",
            "type",
            "role",
            "name",
            "name2",
            "secondary",
            "sector",
            "object",
            "shape",
            "x",
            "z",
            "heading",
            "orientation",
            "radius",
            "activation_radius"
        };
        private static readonly HashSet<string> MarkerKnownKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "id",
            "type",
            "name",
            "shape",
            "x",
            "z",
            "heading",
            "orientation"
        };
        private static readonly HashSet<string> ApproachKnownKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "id",
            "sector",
            "name",
            "entry",
            "exit",
            "entry_portal",
            "exit_portal",
            "runway_entry",
            "runway_exit",
            "taxi_entry",
            "taxi_exit",
            "gate_entry",
            "gate_exit",
            "entry_heading",
            "entry_dir",
            "entry_direction",
            "exit_heading",
            "exit_dir",
            "exit_direction",
            "approach_heading",
            "threshold_heading",
            "width",
            "lane_width",
            "approach_width",
            "length",
            "approach_length",
            "tolerance",
            "alignment_tolerance",
            "align_tol"
        };

        public static bool TryResolvePath(string nameOrPath, out string path)
        {
            path = string.Empty;
            if (string.IsNullOrWhiteSpace(nameOrPath))
                return false;

            if (nameOrPath.IndexOfAny(new[] { '\\', '/' }) >= 0)
            {
                path = nameOrPath;
                return File.Exists(path) && LooksLikeMap(path);
            }

            if (!Path.HasExtension(nameOrPath))
            {
                path = Path.Combine(AppContext.BaseDirectory, "Tracks", nameOrPath + MapExtension);
                return File.Exists(path);
            }

            path = Path.Combine(AppContext.BaseDirectory, "Tracks", nameOrPath);
            return File.Exists(path) && LooksLikeMap(path);
        }

        public static bool TryParse(string nameOrPath, out TrackMapDefinition? map, out List<TrackMapIssue> issues)
        {
            issues = new List<TrackMapIssue>();
            map = null;

            if (!TryResolvePath(nameOrPath, out var path))
            {
                issues.Add(new TrackMapIssue(TrackMapIssueSeverity.Error, "Track map not found."));
                return false;
            }

            var metadata = new TrackMapMetadata();
            var cells = new Dictionary<(int X, int Z), TrackMapCellDefinition>();
            var sectors = new List<TrackSectorDefinition>();
            var areas = new List<TrackAreaDefinition>();
            var shapes = new List<ShapeDefinition>();
            var portals = new List<PortalDefinition>();
            var links = new List<LinkDefinition>();
            var paths = new List<PathDefinition>();
            var beacons = new List<TrackBeaconDefinition>();
            var markers = new List<TrackMarkerDefinition>();
            var approaches = new List<TrackApproachDefinition>();

            var blocks = ReadBlocks(path, issues);
            foreach (var block in blocks)
            {
                switch (block.Name)
                {
                    case "meta":
                        ApplyMeta(metadata, block);
                        break;
                    case "cell":
                        ApplyCell(metadata, cells, block, issues);
                        break;
                    case "line":
                        ApplyLine(metadata, cells, block, issues);
                        break;
                    case "rect":
                        ApplyRect(metadata, cells, block, issues);
                        break;
                    case "sector":
                        ApplySector(sectors, block, issues);
                        break;
                    case "area":
                        ApplyArea(areas, block, issues);
                        break;
                    case "shape":
                        ApplyShape(shapes, block, issues);
                        break;
                    case "portal":
                        ApplyPortal(portals, block, issues);
                        break;
                    case "link":
                        ApplyLink(links, block, issues);
                        break;
                    case "path":
                        ApplyPath(paths, block, issues);
                        break;
                    case "beacon":
                        ApplyBeacon(beacons, block, issues);
                        break;
                    case "marker":
                        ApplyMarker(markers, block, issues);
                        break;
                    case "approach":
                        ApplyApproach(approaches, block, issues);
                        break;
                }
            }

            map = new TrackMapDefinition(metadata, cells, sectors, areas, shapes, portals, links, paths, beacons, markers, approaches);
            return issues.All(issue => issue.Severity != TrackMapIssueSeverity.Error);
        }

        public static TrackMapDefinition Parse(string nameOrPath)
        {
            if (!TryParse(nameOrPath, out var map, out var issues) || map == null)
            {
                var message = issues.Count > 0 ? issues[0].Message : "Failed to parse track map.";
                throw new InvalidDataException(message);
            }

            return map;
        }

        private static bool LooksLikeMap(string nameOrPath)
        {
            return string.Equals(Path.GetExtension(nameOrPath), MapExtension, StringComparison.OrdinalIgnoreCase);
        }

        private static string StripComments(string line)
        {
            if (string.IsNullOrWhiteSpace(line))
                return string.Empty;
            var trimmed = line.Trim();
            if (trimmed.StartsWith("#", StringComparison.Ordinal) ||
                trimmed.StartsWith(";", StringComparison.Ordinal))
                return string.Empty;
            var hash = trimmed.IndexOf('#');
            if (hash >= 0)
                trimmed = trimmed.Substring(0, hash);
            var semi = trimmed.IndexOf(';');
            if (semi >= 0)
                trimmed = trimmed.Substring(0, semi);
            return trimmed.Trim();
        }

        private sealed class SectionBlock
        {
            public SectionBlock(string name, string? argument, int startLine)
            {
                Name = name;
                Argument = string.IsNullOrWhiteSpace(argument) ? null : argument;
                StartLine = startLine;
                Values = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
            }

            public string Name { get; }
            public string? Argument { get; }
            public int StartLine { get; }
            public Dictionary<string, List<string>> Values { get; }

            public void AddValue(string key, string value)
            {
                if (!Values.TryGetValue(key, out var list))
                {
                    list = new List<string>();
                    Values[key] = list;
                }
                list.Add(value);
            }
        }

        private static List<SectionBlock> ReadBlocks(string path, List<TrackMapIssue> issues)
        {
            var blocks = new List<SectionBlock>();
            SectionBlock? current = null;
            var lineNumber = 0;

            foreach (var rawLine in File.ReadLines(path))
            {
                lineNumber++;
                var line = StripComments(rawLine);
                if (string.IsNullOrWhiteSpace(line))
                    continue;

                if (TryReadSectionHeader(line, out var name, out var argument))
                {
                    current = new SectionBlock(name, argument, lineNumber);
                    blocks.Add(current);
                    continue;
                }

                if (current == null)
                {
                    issues.Add(new TrackMapIssue(TrackMapIssueSeverity.Warning, "Value outside of a section.", lineNumber));
                    continue;
                }

                if (!TryParseKeyValue(line, out var key, out var value))
                {
                    issues.Add(new TrackMapIssue(TrackMapIssueSeverity.Warning, "Invalid key/value line.", lineNumber));
                    continue;
                }

                current.AddValue(key, value);
            }

            return blocks;
        }

        private static bool TryReadSectionHeader(string line, out string name, out string? argument)
        {
            name = string.Empty;
            argument = null;
            var trimmed = line.Trim();
            if (!trimmed.StartsWith("[", StringComparison.Ordinal) ||
                !trimmed.EndsWith("]", StringComparison.Ordinal))
                return false;

            var content = trimmed.Substring(1, trimmed.Length - 2).Trim();
            if (string.IsNullOrWhiteSpace(content))
                return false;

            var separatorIndex = content.IndexOf(':');
            if (separatorIndex < 0)
                separatorIndex = content.IndexOf(' ');

            if (separatorIndex >= 0)
            {
                name = content.Substring(0, separatorIndex).Trim().ToLowerInvariant();
                argument = content.Substring(separatorIndex + 1).Trim().Trim('"');
            }
            else
            {
                name = content.Trim().ToLowerInvariant();
            }

            return !string.IsNullOrWhiteSpace(name);
        }

        private static bool TryParseKeyValue(string line, out string key, out string value)
        {
            key = string.Empty;
            value = string.Empty;
            var idx = line.IndexOf('=');
            if (idx <= 0)
                return false;
            key = line.Substring(0, idx).Trim().ToLowerInvariant();
            value = line.Substring(idx + 1).Trim().Trim('"');
            return !string.IsNullOrWhiteSpace(key);
        }

        private static void ApplyMeta(TrackMapMetadata metadata, SectionBlock block)
        {
            if (TryGetValue(block, "name", out var name) && !string.IsNullOrWhiteSpace(name))
                metadata.Name = name.Trim().Trim('"');

            if (TryGetValue(block, "cell_size", out var cellSizeRaw) || TryGetValue(block, "cellsize", out cellSizeRaw))
            {
                if (TryFloat(cellSizeRaw, out var cellSize))
                    metadata.CellSizeMeters = Math.Max(0.1f, cellSize);
            }

            if (TryGetValue(block, "default_surface", out var defaultSurface) &&
                Enum.TryParse(defaultSurface, true, out TrackSurface surface))
                metadata.DefaultSurface = surface;

            if (TryGetValue(block, "default_noise", out var defaultNoise) &&
                Enum.TryParse(defaultNoise, true, out TrackNoise noise))
                metadata.DefaultNoise = noise;

            if (TryGetValue(block, "default_width", out var defaultWidth) && TryFloat(defaultWidth, out var width))
                metadata.DefaultWidthMeters = Math.Max(0.5f, width);

            if (TryGetValue(block, "weather", out var weatherRaw) &&
                Enum.TryParse(weatherRaw, true, out TrackWeather weather))
                metadata.Weather = weather;

            if (TryGetValue(block, "ambience", out var ambienceRaw) &&
                Enum.TryParse(ambienceRaw, true, out TrackAmbience ambience))
                metadata.Ambience = ambience;

            if (TryGetValue(block, "start_x", out var startXRaw) && TryInt(startXRaw, out var startX))
                metadata.StartX = startX;

            if (TryGetValue(block, "start_z", out var startZRaw) && TryInt(startZRaw, out var startZ))
                metadata.StartZ = startZ;

            if (TryGetValue(block, "start_heading", out var headingRaw) && TryDirection(headingRaw, out var heading))
                metadata.StartHeading = heading;
        }

        private static void ApplyCell(
            TrackMapMetadata metadata,
            Dictionary<(int X, int Z), TrackMapCellDefinition> cells,
            SectionBlock block,
            List<TrackMapIssue> issues)
        {
            if (!TryInt(block, "x", out var x) || !TryInt(block, "z", out var z))
            {
                issues.Add(new TrackMapIssue(TrackMapIssueSeverity.Error, "Cell requires x and z.", block.StartLine));
                return;
            }

            var exits = ParseExits(block);
            var surface = TrySurface(block, "surface", out var surfaceValue) ? surfaceValue : (TrackSurface?)null;
            var noise = TryNoise(block, "noise", out var noiseValue) ? noiseValue : (TrackNoise?)null;
            var width = TryFloat(block, "width", out var widthValue) ? widthValue : (float?)null;
            var safe = TryBool(block, "safe", out var safeValue) ? safeValue : (bool?)null;
            var zone = TryGetValue(block, "zone", out var zoneValue) ? zoneValue : null;

            MergeCell(metadata, cells, x, z, exits, surface, noise, width, safe, zone);
        }

        private static void ApplyLine(
            TrackMapMetadata metadata,
            Dictionary<(int X, int Z), TrackMapCellDefinition> cells,
            SectionBlock block,
            List<TrackMapIssue> issues)
        {
            if (!TryInt(block, "x", out var x) || !TryInt(block, "z", out var z))
            {
                issues.Add(new TrackMapIssue(TrackMapIssueSeverity.Error, "Line requires x and z.", block.StartLine));
                return;
            }

            if (!TryInt(block, "length", out var length) || length <= 0)
            {
                issues.Add(new TrackMapIssue(TrackMapIssueSeverity.Error, "Line requires a positive length.", block.StartLine));
                return;
            }

            if (!TryDirection(block, "dir", out var dir))
            {
                issues.Add(new TrackMapIssue(TrackMapIssueSeverity.Error, "Line requires a direction.", block.StartLine));
                return;
            }

            var exits = ParseExits(block);
            if (exits == MapExits.None)
                exits = ExitsFromDirection(dir) | ExitsFromDirection(Opposite(dir));

            var surface = TrySurface(block, "surface", out var surfaceValue) ? surfaceValue : (TrackSurface?)null;
            var noise = TryNoise(block, "noise", out var noiseValue) ? noiseValue : (TrackNoise?)null;
            var width = TryFloat(block, "width", out var widthValue) ? widthValue : (float?)null;
            var safe = TryBool(block, "safe", out var safeValue) ? safeValue : (bool?)null;
            var zone = TryGetValue(block, "zone", out var zoneValue) ? zoneValue : null;

            var currentX = x;
            var currentZ = z;
            for (var i = 0; i < length; i++)
            {
                MergeCell(metadata, cells, currentX, currentZ, exits, surface, noise, width, safe, zone);
                (currentX, currentZ) = Step(currentX, currentZ, dir);
            }
        }

        private static void ApplyRect(
            TrackMapMetadata metadata,
            Dictionary<(int X, int Z), TrackMapCellDefinition> cells,
            SectionBlock block,
            List<TrackMapIssue> issues)
        {
            if (!TryInt(block, "x", out var x) || !TryInt(block, "z", out var z))
            {
                issues.Add(new TrackMapIssue(TrackMapIssueSeverity.Error, "Rect requires x and z.", block.StartLine));
                return;
            }

            if (!TryInt(block, "width", out var rectWidth) || rectWidth <= 0)
            {
                issues.Add(new TrackMapIssue(TrackMapIssueSeverity.Error, "Rect requires a positive width.", block.StartLine));
                return;
            }

            if (!TryInt(block, "height", out var rectHeight) || rectHeight <= 0)
            {
                issues.Add(new TrackMapIssue(TrackMapIssueSeverity.Error, "Rect requires a positive height.", block.StartLine));
                return;
            }

            var exits = ParseExits(block);
            if (exits == MapExits.None && TryDirection(block, "dir", out var dir))
                exits = ExitsFromDirection(dir) | ExitsFromDirection(Opposite(dir));

            var surface = TrySurface(block, "surface", out var surfaceValue) ? surfaceValue : (TrackSurface?)null;
            var noise = TryNoise(block, "noise", out var noiseValue) ? noiseValue : (TrackNoise?)null;
            var width = TryFloat(block, "road_width", out var widthValue)
                ? widthValue
                : (TryFloat(block, "lane_width", out widthValue) ? widthValue : (float?)null);
            var safe = TryBool(block, "safe", out var safeValue) ? safeValue : (bool?)null;
            var zone = TryGetValue(block, "zone", out var zoneValue) ? zoneValue : null;

            for (var dz = 0; dz < rectHeight; dz++)
            {
                for (var dx = 0; dx < rectWidth; dx++)
                {
                    MergeCell(metadata, cells, x + dx, z + dz, exits, surface, noise, width, safe, zone);
                }
            }
        }

        private static void MergeCell(
            TrackMapMetadata metadata,
            Dictionary<(int X, int Z), TrackMapCellDefinition> cells,
            int x,
            int z,
            MapExits exits,
            TrackSurface? surface,
            TrackNoise? noise,
            float? widthMeters,
            bool? safeZone,
            string? zone)
        {
            if (!cells.TryGetValue((x, z), out var cell))
            {
                cell = new TrackMapCellDefinition
                {
                    Exits = MapExits.None,
                    Surface = metadata.DefaultSurface,
                    Noise = metadata.DefaultNoise,
                    WidthMeters = metadata.DefaultWidthMeters,
                    IsSafeZone = false
                };
                cells[(x, z)] = cell;
            }

            cell.Exits |= exits;

            if (surface.HasValue)
                cell.Surface = surface.Value;
            if (noise.HasValue)
                cell.Noise = noise.Value;
            if (widthMeters.HasValue)
                cell.WidthMeters = Math.Max(0.5f, widthMeters.Value);
            if (safeZone.HasValue)
                cell.IsSafeZone = safeZone.Value;
            var trimmedZone = zone?.Trim();
            if (!string.IsNullOrWhiteSpace(trimmedZone))
                cell.Zone = trimmedZone;
        }

        private static void ApplySector(
            List<TrackSectorDefinition> sectors,
            SectionBlock block,
            List<TrackMapIssue> issues)
        {
            if (!TryReadId(block, out var id))
            {
                issues.Add(new TrackMapIssue(TrackMapIssueSeverity.Error, "Sector requires an id.", block.StartLine));
                return;
            }

            if (!TryGetValue(block, "type", out var rawType) ||
                string.IsNullOrWhiteSpace(rawType) ||
                !Enum.TryParse(rawType, true, out TrackSectorType sectorType))
            {
                issues.Add(new TrackMapIssue(TrackMapIssueSeverity.Error, "Sector requires a valid type.", block.StartLine));
                return;
            }

            if (sectors.Any(s => string.Equals(s.Id, id, StringComparison.OrdinalIgnoreCase)))
            {
                issues.Add(new TrackMapIssue(TrackMapIssueSeverity.Error, $"Duplicate sector id '{id}'.", block.StartLine));
                return;
            }

            var name = TryGetValue(block, "name", out var nameValue) ? nameValue : null;
            var code = TryGetValue(block, "code", out var codeValue) ? codeValue : null;
            var areaId = TryGetValue(block, "area", out var areaValue) ? areaValue :
                (TryGetValue(block, "shape", out areaValue) ? areaValue : null);
            var surface = TrySurface(block, "surface", out var surfaceValue) ? surfaceValue : (TrackSurface?)null;
            var noise = TryNoise(block, "noise", out var noiseValue) ? noiseValue : (TrackNoise?)null;
            var flags = TrySectorFlags(block, out var sectorFlags) ? sectorFlags : TrackSectorFlags.None;
            var metadata = CollectSectorMetadata(block);

            sectors.Add(new TrackSectorDefinition(id, sectorType, name, areaId, code, surface, noise, flags, metadata));
        }

        private static void ApplyArea(
            List<TrackAreaDefinition> areas,
            SectionBlock block,
            List<TrackMapIssue> issues)
        {
            if (!TryReadId(block, out var id))
            {
                issues.Add(new TrackMapIssue(TrackMapIssueSeverity.Error, "Area requires an id.", block.StartLine));
                return;
            }

            if (!TryGetValue(block, "type", out var rawType) ||
                string.IsNullOrWhiteSpace(rawType) ||
                !Enum.TryParse(rawType, true, out TrackAreaType areaType))
            {
                issues.Add(new TrackMapIssue(TrackMapIssueSeverity.Error, "Area requires a valid type.", block.StartLine));
                return;
            }

            if (areas.Any(a => string.Equals(a.Id, id, StringComparison.OrdinalIgnoreCase)))
            {
                issues.Add(new TrackMapIssue(TrackMapIssueSeverity.Error, $"Duplicate area id '{id}'.", block.StartLine));
                return;
            }

            if (!TryGetValue(block, "shape", out var shapeId) || string.IsNullOrWhiteSpace(shapeId))
            {
                issues.Add(new TrackMapIssue(TrackMapIssueSeverity.Error, "Area requires a shape id.", block.StartLine));
                return;
            }

            var name = TryGetValue(block, "name", out var nameValue) ? nameValue : null;
            var surface = TrySurface(block, "surface", out var surfaceValue) ? surfaceValue : (TrackSurface?)null;
            var noise = TryNoise(block, "noise", out var noiseValue) ? noiseValue : (TrackNoise?)null;
            var width = TryFloat(block, "width", out var widthValue) ? Math.Max(0.1f, widthValue) : (float?)null;
            var flags = TryAreaFlags(block, out var areaFlags) ? areaFlags : TrackAreaFlags.None;
            var metadata = CollectAreaMetadata(block);

            areas.Add(new TrackAreaDefinition(id, areaType, shapeId, name, surface, noise, width, flags, metadata));
        }

        private static void ApplyShape(
            List<ShapeDefinition> shapes,
            SectionBlock block,
            List<TrackMapIssue> issues)
        {
            if (!TryReadId(block, out var id))
            {
                issues.Add(new TrackMapIssue(TrackMapIssueSeverity.Error, "Shape requires an id.", block.StartLine));
                return;
            }

            if (!TryGetValue(block, "type", out var rawType) ||
                string.IsNullOrWhiteSpace(rawType) ||
                !TryShapeType(rawType, out var shapeType))
            {
                issues.Add(new TrackMapIssue(TrackMapIssueSeverity.Error, "Shape requires a valid type.", block.StartLine));
                return;
            }

            if (shapes.Any(s => string.Equals(s.Id, id, StringComparison.OrdinalIgnoreCase)))
            {
                issues.Add(new TrackMapIssue(TrackMapIssueSeverity.Error, $"Duplicate shape id '{id}'.", block.StartLine));
                return;
            }

            switch (shapeType)
            {
                case ShapeType.Rectangle:
                    if (!TryFloat(block, "x", out var rectX) ||
                        !TryFloat(block, "z", out var rectZ) ||
                        !TryFloat(block, "width", out var width) ||
                        !TryFloat(block, "height", out var height) ||
                        width <= 0f || height <= 0f)
                    {
                        issues.Add(new TrackMapIssue(TrackMapIssueSeverity.Error, "Rectangle requires x, z, width, height.", block.StartLine));
                        return;
                    }
                    shapes.Add(new ShapeDefinition(id, shapeType, rectX, rectZ, width, height));
                    break;
                case ShapeType.Circle:
                    if (!TryFloat(block, "x", out var circleX) ||
                        !TryFloat(block, "z", out var circleZ) ||
                        !TryFloat(block, "radius", out var radius) ||
                        radius <= 0f)
                    {
                        issues.Add(new TrackMapIssue(TrackMapIssueSeverity.Error, "Circle requires x, z, radius.", block.StartLine));
                        return;
                    }
                    shapes.Add(new ShapeDefinition(id, shapeType, circleX, circleZ, radius: radius));
                    break;
                case ShapeType.Polygon:
                case ShapeType.Polyline:
                    if (!TryParsePoints(block, out var points))
                    {
                        issues.Add(new TrackMapIssue(TrackMapIssueSeverity.Error, "Shape requires points.", block.StartLine));
                        return;
                    }
                    if (shapeType == ShapeType.Polygon && points.Count < 3)
                    {
                        issues.Add(new TrackMapIssue(TrackMapIssueSeverity.Error, "Polygon requires at least 3 points.", block.StartLine));
                        return;
                    }
                    if (shapeType == ShapeType.Polyline && points.Count < 2)
                    {
                        issues.Add(new TrackMapIssue(TrackMapIssueSeverity.Error, "Polyline requires at least 2 points.", block.StartLine));
                        return;
                    }
                    shapes.Add(new ShapeDefinition(id, shapeType, points: points));
                    break;
            }
        }

        private static void ApplyPortal(
            List<PortalDefinition> portals,
            SectionBlock block,
            List<TrackMapIssue> issues)
        {
            if (!TryReadId(block, out var id))
            {
                issues.Add(new TrackMapIssue(TrackMapIssueSeverity.Error, "Portal requires an id.", block.StartLine));
                return;
            }

            if (!TryGetValue(block, "sector", out var sectorId) || string.IsNullOrWhiteSpace(sectorId))
            {
                issues.Add(new TrackMapIssue(TrackMapIssueSeverity.Error, "Portal requires a sector id.", block.StartLine));
                return;
            }

            if (!TryFloat(block, "x", out var x) || !TryFloat(block, "z", out var z))
            {
                issues.Add(new TrackMapIssue(TrackMapIssueSeverity.Error, "Portal requires x and z.", block.StartLine));
                return;
            }

            if (portals.Any(p => string.Equals(p.Id, id, StringComparison.OrdinalIgnoreCase)))
            {
                issues.Add(new TrackMapIssue(TrackMapIssueSeverity.Error, $"Duplicate portal id '{id}'.", block.StartLine));
                return;
            }

            var width = TryFloat(block, "width", out var widthMeters) ? Math.Max(0.1f, widthMeters) : 0f;
            var entryHeading = TryReadHeading(block, "entry", out var entryHeadingValue) ? entryHeadingValue : (float?)null;
            var exitHeading = TryReadHeading(block, "exit", out var exitHeadingValue) ? exitHeadingValue : (float?)null;

            if (!entryHeading.HasValue && !exitHeading.HasValue && TryReadHeadingFallback(block, out var bothHeading))
            {
                entryHeading = bothHeading;
                exitHeading = bothHeading;
            }

            var role = TryPortalRole(block, out var parsedRole) ? parsedRole : PortalRole.EntryExit;
            if (!TryPortalRole(block, out _))
            {
                if (entryHeading.HasValue && !exitHeading.HasValue)
                    role = PortalRole.Entry;
                else if (exitHeading.HasValue && !entryHeading.HasValue)
                    role = PortalRole.Exit;
            }

            portals.Add(new PortalDefinition(id, sectorId.Trim(), x, z, width, entryHeading, exitHeading, role));
        }

        private static void ApplyLink(
            List<LinkDefinition> links,
            SectionBlock block,
            List<TrackMapIssue> issues)
        {
            if (!TryGetValue(block, "from", out var from) || string.IsNullOrWhiteSpace(from) ||
                !TryGetValue(block, "to", out var to) || string.IsNullOrWhiteSpace(to))
            {
                issues.Add(new TrackMapIssue(TrackMapIssueSeverity.Error, "Link requires from and to portal ids.", block.StartLine));
                return;
            }

            var direction = TryLinkDirection(block, out var parsedDirection) ? parsedDirection : LinkDirection.TwoWay;
            var id = TryReadId(block, out var linkId) ? linkId : $"{from.Trim()}->{to.Trim()}";

            if (links.Any(l => string.Equals(l.Id, id, StringComparison.OrdinalIgnoreCase)))
            {
                issues.Add(new TrackMapIssue(TrackMapIssueSeverity.Error, $"Duplicate link id '{id}'.", block.StartLine));
                return;
            }

            links.Add(new LinkDefinition(id, from, to, direction));
        }

        private static void ApplyPath(
            List<PathDefinition> paths,
            SectionBlock block,
            List<TrackMapIssue> issues)
        {
            if (!TryReadId(block, out var id))
            {
                issues.Add(new TrackMapIssue(TrackMapIssueSeverity.Error, "Path requires an id.", block.StartLine));
                return;
            }

            if (!TryGetValue(block, "type", out var rawType) ||
                string.IsNullOrWhiteSpace(rawType) ||
                !TryPathType(rawType, out var pathType))
            {
                issues.Add(new TrackMapIssue(TrackMapIssueSeverity.Error, "Path requires a valid type.", block.StartLine));
                return;
            }

            if (paths.Any(p => string.Equals(p.Id, id, StringComparison.OrdinalIgnoreCase)))
            {
                issues.Add(new TrackMapIssue(TrackMapIssueSeverity.Error, $"Duplicate path id '{id}'.", block.StartLine));
                return;
            }

            var shapeId = TryGetValue(block, "shape", out var shapeValue) ? shapeValue : null;
            var fromPortal = TryGetValue(block, "from", out var fromValue) ? fromValue : null;
            var toPortal = TryGetValue(block, "to", out var toValue) ? toValue : null;
            var width = TryFloat(block, "width", out var widthMeters) ? Math.Max(0.1f, widthMeters) : 0f;
            var name = TryGetValue(block, "name", out var nameValue) ? nameValue : null;

            paths.Add(new PathDefinition(id, pathType, shapeId, fromPortal, toPortal, width, name));
        }

        private static void ApplyBeacon(
            List<TrackBeaconDefinition> beacons,
            SectionBlock block,
            List<TrackMapIssue> issues)
        {
            if (!TryReadId(block, out var id))
            {
                issues.Add(new TrackMapIssue(TrackMapIssueSeverity.Error, "Beacon requires an id.", block.StartLine));
                return;
            }

            if (!TryFloat(block, "x", out var x) || !TryFloat(block, "z", out var z))
            {
                issues.Add(new TrackMapIssue(TrackMapIssueSeverity.Error, "Beacon requires x and z.", block.StartLine));
                return;
            }

            if (beacons.Any(b => string.Equals(b.Id, id, StringComparison.OrdinalIgnoreCase)))
            {
                issues.Add(new TrackMapIssue(TrackMapIssueSeverity.Error, $"Duplicate beacon id '{id}'.", block.StartLine));
                return;
            }

            var type = TryBeaconType(block, out var parsedType) ? parsedType : TrackBeaconType.Undefined;
            var role = TryBeaconRole(block, out var parsedRole) ? parsedRole : TrackBeaconRole.Undefined;
            var name = TryGetValue(block, "name", out var nameValue) ? nameValue : null;
            var name2 = TryGetValue(block, "name2", out var name2Value) ? name2Value :
                (TryGetValue(block, "secondary", out name2Value) ? name2Value : null);
            var sectorId = TryGetValue(block, "sector", out var sectorValue) ? sectorValue :
                (TryGetValue(block, "object", out sectorValue) ? sectorValue : null);
            var shapeId = TryGetValue(block, "shape", out var shapeValue) ? shapeValue : null;
            var heading = TryReadHeadingValue(block, out var headingValue) ? headingValue : (float?)null;
            float? radius = null;
            if (TryFloat(block, "radius", out var radiusValue) && radiusValue > 0f)
                radius = radiusValue;
            else if (TryFloat(block, "activation_radius", out radiusValue) && radiusValue > 0f)
                radius = radiusValue;
            var metadata = CollectBeaconMetadata(block);

            beacons.Add(new TrackBeaconDefinition(id, type, x, z, name, name2, sectorId, shapeId, heading, radius, role, metadata));
        }

        private static void ApplyMarker(
            List<TrackMarkerDefinition> markers,
            SectionBlock block,
            List<TrackMapIssue> issues)
        {
            if (!TryReadId(block, out var id))
            {
                issues.Add(new TrackMapIssue(TrackMapIssueSeverity.Error, "Marker requires an id.", block.StartLine));
                return;
            }

            if (!TryFloat(block, "x", out var x) || !TryFloat(block, "z", out var z))
            {
                issues.Add(new TrackMapIssue(TrackMapIssueSeverity.Error, "Marker requires x and z.", block.StartLine));
                return;
            }

            if (markers.Any(m => string.Equals(m.Id, id, StringComparison.OrdinalIgnoreCase)))
            {
                issues.Add(new TrackMapIssue(TrackMapIssueSeverity.Error, $"Duplicate marker id '{id}'.", block.StartLine));
                return;
            }

            var type = TryMarkerType(block, out var parsedType) ? parsedType : TrackMarkerType.Undefined;
            var name = TryGetValue(block, "name", out var nameValue) ? nameValue : null;
            var shapeId = TryGetValue(block, "shape", out var shapeValue) ? shapeValue : null;
            var heading = TryReadHeadingValue(block, out var headingValue) ? headingValue : (float?)null;
            var metadata = CollectMarkerMetadata(block);

            markers.Add(new TrackMarkerDefinition(id, type, x, z, name, shapeId, heading, metadata));
        }

        private static void ApplyApproach(
            List<TrackApproachDefinition> approaches,
            SectionBlock block,
            List<TrackMapIssue> issues)
        {
            if (!TryReadId(block, out var id))
            {
                if (TryGetValue(block, "sector", out var sectorIdValue) && !string.IsNullOrWhiteSpace(sectorIdValue))
                {
                    id = sectorIdValue.Trim();
                }
                else
                {
                    issues.Add(new TrackMapIssue(TrackMapIssueSeverity.Error, "Approach requires an id or sector.", block.StartLine));
                    return;
                }
            }

            var sectorId = id;
            if (TryGetValue(block, "sector", out var sectorValue) && !string.IsNullOrWhiteSpace(sectorValue))
                sectorId = sectorValue.Trim();

            if (approaches.Any(a => string.Equals(a.SectorId, sectorId, StringComparison.OrdinalIgnoreCase)))
            {
                issues.Add(new TrackMapIssue(TrackMapIssueSeverity.Error, $"Duplicate approach sector id '{sectorId}'.", block.StartLine));
                return;
            }

            var name = TryGetValue(block, "name", out var nameValue) ? nameValue : null;
            var entryPortalId = TryGetValue(block, "entry_portal", out var entryValue) ? entryValue :
                (TryGetValue(block, "entry", out entryValue) ? entryValue :
                 (TryGetValue(block, "runway_entry", out entryValue) ? entryValue :
                  (TryGetValue(block, "taxi_entry", out entryValue) ? entryValue :
                   (TryGetValue(block, "gate_entry", out entryValue) ? entryValue : null))));
            var exitPortalId = TryGetValue(block, "exit_portal", out var exitValue) ? exitValue :
                (TryGetValue(block, "exit", out exitValue) ? exitValue :
                 (TryGetValue(block, "runway_exit", out exitValue) ? exitValue :
                  (TryGetValue(block, "taxi_exit", out exitValue) ? exitValue :
                   (TryGetValue(block, "gate_exit", out exitValue) ? exitValue : null))));

            var entryHeading = TryReadHeading(block, "entry", out var entryHeadingValue)
                ? entryHeadingValue
                : (TryReadHeading(block, "approach", out entryHeadingValue) ? entryHeadingValue : (float?)null);
            var exitHeading = TryReadHeading(block, "exit", out var exitHeadingValue)
                ? exitHeadingValue
                : (TryReadHeading(block, "threshold", out exitHeadingValue) ? exitHeadingValue : (float?)null);

            var width = TryFloat(block, "width", out var widthMeters) ? Math.Max(0.1f, widthMeters) :
                (TryFloat(block, "lane_width", out widthMeters) ? Math.Max(0.1f, widthMeters) :
                 (TryFloat(block, "approach_width", out widthMeters) ? Math.Max(0.1f, widthMeters) : (float?)null));
            var length = TryFloat(block, "length", out var lengthMeters) ? Math.Max(0.1f, lengthMeters) :
                (TryFloat(block, "approach_length", out lengthMeters) ? Math.Max(0.1f, lengthMeters) : (float?)null);
            var tolerance = TryFloat(block, "tolerance", out var toleranceDegrees) ? Math.Max(0f, toleranceDegrees) :
                (TryFloat(block, "alignment_tolerance", out toleranceDegrees) ? Math.Max(0f, toleranceDegrees) :
                 (TryFloat(block, "align_tol", out toleranceDegrees) ? Math.Max(0f, toleranceDegrees) : (float?)null));

            var metadata = CollectApproachMetadata(block);

            approaches.Add(new TrackApproachDefinition(
                sectorId,
                name,
                entryPortalId,
                exitPortalId,
                entryHeading,
                exitHeading,
                width,
                length,
                tolerance,
                metadata));
        }

        private static bool TryGetValue(SectionBlock block, string key, out string value)
        {
            value = string.Empty;
            if (!block.Values.TryGetValue(key, out var values) || values.Count == 0)
                return false;
            value = values[values.Count - 1];
            return true;
        }

        private static IEnumerable<string> GetValues(SectionBlock block, params string[] keys)
        {
            foreach (var key in keys)
            {
                if (block.Values.TryGetValue(key, out var values))
                {
                    foreach (var value in values)
                        yield return value;
                }
            }
        }

        private static bool TryReadId(SectionBlock block, out string id)
        {
            id = string.Empty;
            if (!string.IsNullOrWhiteSpace(block.Argument))
            {
                id = block.Argument!.Trim();
                return true;
            }
            if (TryGetValue(block, "id", out var rawId) && !string.IsNullOrWhiteSpace(rawId))
            {
                id = rawId.Trim();
                return true;
            }
            return false;
        }

        private static bool TryInt(SectionBlock block, string key, out int value)
        {
            value = 0;
            if (!TryGetValue(block, key, out var raw))
                return false;
            return int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out value);
        }

        private static bool TryFloat(SectionBlock block, string key, out float value)
        {
            value = 0f;
            if (!TryGetValue(block, key, out var raw))
                return false;
            return float.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out value);
        }

        private static bool TryBool(SectionBlock block, string key, out bool value)
        {
            value = false;
            if (!TryGetValue(block, key, out var raw))
                return false;
            return TryBool(raw, out value);
        }

        private static bool TrySurface(SectionBlock block, string key, out TrackSurface surface)
        {
            surface = TrackSurface.Asphalt;
            if (!TryGetValue(block, key, out var raw))
                return false;
            return Enum.TryParse(raw, true, out surface);
        }

        private static bool TryNoise(SectionBlock block, string key, out TrackNoise noise)
        {
            noise = TrackNoise.NoNoise;
            if (!TryGetValue(block, key, out var raw))
                return false;
            return Enum.TryParse(raw, true, out noise);
        }

        private static bool TrySectorFlags(SectionBlock block, out TrackSectorFlags flags)
        {
            flags = TrackSectorFlags.None;
            var found = false;
            foreach (var raw in GetValues(block, "flags", "flag", "caps", "capabilities"))
            {
                if (!TryParseSectorFlags(raw, out var parsed))
                    continue;
                flags |= parsed;
                found = true;
            }
            return found;
        }

        private static IReadOnlyDictionary<string, string>? CollectSectorMetadata(SectionBlock block)
        {
            Dictionary<string, string>? metadata = null;
            foreach (var pair in block.Values)
            {
                if (SectorKnownKeys.Contains(pair.Key))
                    continue;
                if (pair.Value.Count == 0)
                    continue;
                metadata ??= new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                metadata[pair.Key] = pair.Value[pair.Value.Count - 1];
            }
            return metadata;
        }

        private static bool TryAreaFlags(SectionBlock block, out TrackAreaFlags flags)
        {
            flags = TrackAreaFlags.None;
            var found = false;
            foreach (var raw in GetValues(block, "flags", "flag", "caps", "capabilities"))
            {
                if (!TryParseAreaFlags(raw, out var parsed))
                    continue;
                flags |= parsed;
                found = true;
            }
            return found;
        }

        private static IReadOnlyDictionary<string, string>? CollectAreaMetadata(SectionBlock block)
        {
            Dictionary<string, string>? metadata = null;
            foreach (var pair in block.Values)
            {
                if (AreaKnownKeys.Contains(pair.Key))
                    continue;
                if (pair.Value.Count == 0)
                    continue;
                metadata ??= new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                metadata[pair.Key] = pair.Value[pair.Value.Count - 1];
            }
            return metadata;
        }

        private static IReadOnlyDictionary<string, string>? CollectBeaconMetadata(SectionBlock block)
        {
            Dictionary<string, string>? metadata = null;
            foreach (var pair in block.Values)
            {
                if (BeaconKnownKeys.Contains(pair.Key))
                    continue;
                if (pair.Value.Count == 0)
                    continue;
                metadata ??= new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                metadata[pair.Key] = pair.Value[pair.Value.Count - 1];
            }
            return metadata;
        }

        private static IReadOnlyDictionary<string, string>? CollectMarkerMetadata(SectionBlock block)
        {
            Dictionary<string, string>? metadata = null;
            foreach (var pair in block.Values)
            {
                if (MarkerKnownKeys.Contains(pair.Key))
                    continue;
                if (pair.Value.Count == 0)
                    continue;
                metadata ??= new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                metadata[pair.Key] = pair.Value[pair.Value.Count - 1];
            }
            return metadata;
        }

        private static IReadOnlyDictionary<string, string>? CollectApproachMetadata(SectionBlock block)
        {
            Dictionary<string, string>? metadata = null;
            foreach (var pair in block.Values)
            {
                if (ApproachKnownKeys.Contains(pair.Key))
                    continue;
                if (pair.Value.Count == 0)
                    continue;
                metadata ??= new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                metadata[pair.Key] = pair.Value[pair.Value.Count - 1];
            }
            return metadata;
        }

        private static bool TryDirection(SectionBlock block, string key, out MapDirection direction)
        {
            direction = MapDirection.North;
            if (!TryGetValue(block, key, out var raw))
                return false;
            return TryDirection(raw, out direction);
        }

        private static MapExits ParseExits(SectionBlock block)
        {
            MapExits exits = MapExits.None;
            foreach (var raw in GetValues(block, "exits", "exit"))
                exits |= ParseExits(raw);
            return exits;
        }

        private static bool TryPortalRole(SectionBlock block, out PortalRole role)
        {
            role = PortalRole.EntryExit;
            if (!TryGetValue(block, "role", out var raw))
                return false;
            return TryPortalRole(raw, out role);
        }

        private static bool TryBeaconType(SectionBlock block, out TrackBeaconType type)
        {
            type = TrackBeaconType.Undefined;
            if (!TryGetValue(block, "type", out var raw))
                return false;
            return TryBeaconType(raw, out type);
        }

        private static bool TryBeaconRole(SectionBlock block, out TrackBeaconRole role)
        {
            role = TrackBeaconRole.Undefined;
            if (!TryGetValue(block, "role", out var raw))
                return false;
            return TryBeaconRole(raw, out role);
        }

        private static bool TryMarkerType(SectionBlock block, out TrackMarkerType type)
        {
            type = TrackMarkerType.Undefined;
            if (!TryGetValue(block, "type", out var raw))
                return false;
            return TryMarkerType(raw, out type);
        }

        private static bool TryLinkDirection(SectionBlock block, out LinkDirection direction)
        {
            direction = LinkDirection.TwoWay;
            if (TryGetValue(block, "dir", out var raw))
                return TryLinkDirection(raw, out direction);
            if (TryGetValue(block, "direction", out raw))
                return TryLinkDirection(raw, out direction);
            if (TryGetValue(block, "oneway", out var oneway) && TryBool(oneway, out var isOneWay))
            {
                direction = isOneWay ? LinkDirection.OneWay : LinkDirection.TwoWay;
                return true;
            }
            return false;
        }

        private static bool TryReadHeading(SectionBlock block, string key, out float headingDegrees)
        {
            headingDegrees = 0f;
            foreach (var raw in GetValues(block, $"{key}_heading", $"{key}_heading_deg", $"{key}_dir", $"{key}_direction", key))
            {
                if (TryDirection(raw, out var direction))
                {
                    headingDegrees = DirectionToDegrees(direction);
                    return true;
                }
                if (TryFloat(raw, out headingDegrees))
                    return true;
            }

            return false;
        }

        private static bool TryReadHeadingFallback(SectionBlock block, out float headingDegrees)
        {
            headingDegrees = 0f;
            foreach (var raw in GetValues(block, "heading", "dir", "direction"))
            {
                if (TryDirection(raw, out var direction))
                {
                    headingDegrees = DirectionToDegrees(direction);
                    return true;
                }
                if (TryFloat(raw, out headingDegrees))
                    return true;
            }
            return false;
        }

        private static bool TryReadHeadingValue(SectionBlock block, out float headingDegrees)
        {
            headingDegrees = 0f;
            foreach (var raw in GetValues(block, "heading", "orientation", "dir", "direction"))
            {
                if (TryDirection(raw, out var direction))
                {
                    headingDegrees = DirectionToDegrees(direction);
                    return true;
                }
                if (TryFloat(raw, out headingDegrees))
                    return true;
            }
            return false;
        }

        private static bool TryParsePoints(SectionBlock block, out List<Vector2> points)
        {
            points = new List<Vector2>();

            foreach (var raw in GetValues(block, "points"))
            {
                if (!TryParsePoints(raw, out var parsed))
                    return false;
                points.AddRange(parsed);
            }

            foreach (var raw in GetValues(block, "point"))
            {
                if (!TryParsePoints(raw, out var parsed))
                    return false;
                points.AddRange(parsed);
            }

            return points.Count > 0;
        }
        private static bool TryDirection(string value, out MapDirection direction)
        {
            direction = MapDirection.North;
            if (string.IsNullOrWhiteSpace(value))
                return false;
            var trimmed = value.Trim().ToLowerInvariant();
            switch (trimmed)
            {
                case "n":
                case "north":
                    direction = MapDirection.North;
                    return true;
                case "e":
                case "east":
                    direction = MapDirection.East;
                    return true;
                case "s":
                case "south":
                    direction = MapDirection.South;
                    return true;
                case "w":
                case "west":
                    direction = MapDirection.West;
                    return true;
            }
            return false;
        }

        private static bool TryShapeType(string value, out ShapeType type)
        {
            type = ShapeType.Undefined;
            if (string.IsNullOrWhiteSpace(value))
                return false;
            var trimmed = value.Trim().ToLowerInvariant();
            switch (trimmed)
            {
                case "rect":
                case "rectangle":
                    type = ShapeType.Rectangle;
                    return true;
                case "circle":
                    type = ShapeType.Circle;
                    return true;
                case "polygon":
                case "poly":
                    type = ShapeType.Polygon;
                    return true;
                case "polyline":
                case "line":
                case "path":
                    type = ShapeType.Polyline;
                    return true;
            }
            return Enum.TryParse(value, true, out type);
        }

        private static bool TryPortalRole(string value, out PortalRole role)
        {
            role = PortalRole.EntryExit;
            if (string.IsNullOrWhiteSpace(value))
                return false;
            var trimmed = value.Trim().ToLowerInvariant();
            switch (trimmed)
            {
                case "entry":
                    role = PortalRole.Entry;
                    return true;
                case "exit":
                    role = PortalRole.Exit;
                    return true;
                case "both":
                case "entryexit":
                case "entry_exit":
                    role = PortalRole.EntryExit;
                    return true;
            }
            return Enum.TryParse(value, true, out role);
        }

        private static bool TryBeaconType(string value, out TrackBeaconType type)
        {
            type = TrackBeaconType.Undefined;
            if (string.IsNullOrWhiteSpace(value))
                return false;
            var trimmed = value.Trim().ToLowerInvariant();
            switch (trimmed)
            {
                case "voice":
                case "speech":
                case "announce":
                    type = TrackBeaconType.Voice;
                    return true;
                case "beep":
                case "pip":
                case "tone":
                    type = TrackBeaconType.Beep;
                    return true;
                case "silent":
                case "none":
                    type = TrackBeaconType.Silent;
                    return true;
            }
            return Enum.TryParse(value, true, out type);
        }

        private static bool TryBeaconRole(string value, out TrackBeaconRole role)
        {
            role = TrackBeaconRole.Undefined;
            if (string.IsNullOrWhiteSpace(value))
                return false;
            var trimmed = value.Trim().ToLowerInvariant();
            switch (trimmed)
            {
                case "guide":
                case "guidance":
                    role = TrackBeaconRole.Guidance;
                    return true;
                case "align":
                case "alignment":
                    role = TrackBeaconRole.Alignment;
                    return true;
                case "entry":
                    role = TrackBeaconRole.Entry;
                    return true;
                case "exit":
                    role = TrackBeaconRole.Exit;
                    return true;
                case "center":
                case "centre":
                    role = TrackBeaconRole.Center;
                    return true;
                case "warn":
                case "warning":
                case "hazard":
                    role = TrackBeaconRole.Warning;
                    return true;
            }
            return Enum.TryParse(value, true, out role);
        }

        private static bool TryMarkerType(string value, out TrackMarkerType type)
        {
            type = TrackMarkerType.Undefined;
            if (string.IsNullOrWhiteSpace(value))
                return false;
            var trimmed = value.Trim().ToLowerInvariant();
            switch (trimmed)
            {
                case "start":
                    type = TrackMarkerType.Start;
                    return true;
                case "finish":
                case "end":
                    type = TrackMarkerType.Finish;
                    return true;
                case "checkpoint":
                case "check":
                    type = TrackMarkerType.Checkpoint;
                    return true;
                case "entry":
                    type = TrackMarkerType.Entry;
                    return true;
                case "exit":
                    type = TrackMarkerType.Exit;
                    return true;
                case "apex":
                    type = TrackMarkerType.Apex;
                    return true;
                case "curve":
                case "turn":
                    type = TrackMarkerType.Curve;
                    return true;
                case "intersection":
                case "cross":
                    type = TrackMarkerType.Intersection;
                    return true;
                case "merge":
                    type = TrackMarkerType.Merge;
                    return true;
                case "split":
                    type = TrackMarkerType.Split;
                    return true;
                case "branch":
                    type = TrackMarkerType.Branch;
                    return true;
                case "warning":
                case "hazard":
                    type = TrackMarkerType.Warning;
                    return true;
            }
            return Enum.TryParse(value, true, out type);
        }

        private static bool TryLinkDirection(string value, out LinkDirection direction)
        {
            direction = LinkDirection.TwoWay;
            if (string.IsNullOrWhiteSpace(value))
                return false;
            var trimmed = value.Trim().ToLowerInvariant();
            switch (trimmed)
            {
                case "two":
                case "both":
                case "twoway":
                    direction = LinkDirection.TwoWay;
                    return true;
                case "one":
                case "oneway":
                    direction = LinkDirection.OneWay;
                    return true;
            }
            return Enum.TryParse(value, true, out direction);
        }

        private static bool TryPathType(string value, out PathType type)
        {
            type = PathType.Undefined;
            if (string.IsNullOrWhiteSpace(value))
                return false;
            var trimmed = value.Trim().ToLowerInvariant();
            switch (trimmed)
            {
                case "road":
                    type = PathType.Road;
                    return true;
                case "curve":
                    type = PathType.Curve;
                    return true;
                case "intersection":
                case "cross":
                    type = PathType.Intersection;
                    return true;
                case "connector":
                    type = PathType.Connector;
                    return true;
                case "lane":
                    type = PathType.Lane;
                    return true;
                case "branch":
                    type = PathType.Branch;
                    return true;
                case "merge":
                    type = PathType.Merge;
                    return true;
                case "split":
                    type = PathType.Split;
                    return true;
                case "pit":
                case "pitlane":
                    type = PathType.PitLane;
                    return true;
            }
            return Enum.TryParse(value, true, out type);
        }

        private static float DirectionToDegrees(MapDirection direction)
        {
            return direction switch
            {
                MapDirection.North => 0f,
                MapDirection.East => 90f,
                MapDirection.South => 180f,
                MapDirection.West => 270f,
                _ => 0f
            };
        }

        private static bool TryParsePoints(string raw, out List<Vector2> points)
        {
            points = new List<Vector2>();
            if (string.IsNullOrWhiteSpace(raw))
                return false;
            var segments = raw.Split(new[] { ';', '|' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var segment in segments)
            {
                var trimmed = segment.Trim();
                if (trimmed.Length == 0)
                    continue;
                var coords = trimmed.Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries);
                if (coords.Length < 2)
                    return false;
                if (!float.TryParse(coords[0], NumberStyles.Float, CultureInfo.InvariantCulture, out var x))
                    return false;
                if (!float.TryParse(coords[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var z))
                    return false;
                points.Add(new Vector2(x, z));
            }
            return points.Count > 0;
        }

        private static bool TryParseSectorFlags(string raw, out TrackSectorFlags flags)
        {
            flags = TrackSectorFlags.None;
            if (string.IsNullOrWhiteSpace(raw))
                return false;

            var found = false;
            var tokens = raw.Split(new[] { ',', '|', ';', ' ' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var token in tokens)
            {
                if (TryParseSectorFlagToken(token, out var flag))
                {
                    flags |= flag;
                    found = true;
                }
            }

            return found;
        }

        private static bool TryParseSectorFlagToken(string token, out TrackSectorFlags flag)
        {
            flag = TrackSectorFlags.None;
            if (string.IsNullOrWhiteSpace(token))
                return false;

            var trimmed = token.Trim().ToLowerInvariant();
            switch (trimmed)
            {
                case "fuel":
                case "refuel":
                case "gas":
                case "pump":
                    flag = TrackSectorFlags.Fuel;
                    return true;
                case "parking":
                case "park":
                    flag = TrackSectorFlags.Parking;
                    return true;
                case "boarding":
                case "board":
                    flag = TrackSectorFlags.Boarding;
                    return true;
                case "service":
                case "servicing":
                    flag = TrackSectorFlags.Service;
                    return true;
                case "pit":
                case "pitlane":
                case "pit_box":
                case "pitbox":
                    flag = TrackSectorFlags.Pit;
                    return true;
                case "safe":
                case "safezone":
                    flag = TrackSectorFlags.SafeZone;
                    return true;
                case "hazard":
                case "danger":
                    flag = TrackSectorFlags.Hazard;
                    return true;
                case "closed":
                case "blocked":
                    flag = TrackSectorFlags.Closed;
                    return true;
                case "restricted":
                    flag = TrackSectorFlags.Restricted;
                    return true;
            }

            return Enum.TryParse(token, true, out flag);
        }

        private static bool TryParseAreaFlags(string raw, out TrackAreaFlags flags)
        {
            flags = TrackAreaFlags.None;
            if (string.IsNullOrWhiteSpace(raw))
                return false;

            var found = false;
            var tokens = raw.Split(new[] { ',', '|', ';', ' ' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var token in tokens)
            {
                if (TryParseAreaFlagToken(token, out var flag))
                {
                    flags |= flag;
                    found = true;
                }
            }

            return found;
        }

        private static bool TryParseAreaFlagToken(string token, out TrackAreaFlags flag)
        {
            flag = TrackAreaFlags.None;
            if (string.IsNullOrWhiteSpace(token))
                return false;

            var trimmed = token.Trim().ToLowerInvariant();
            switch (trimmed)
            {
                case "safe":
                case "safezone":
                    flag = TrackAreaFlags.SafeZone;
                    return true;
                case "hazard":
                case "danger":
                    flag = TrackAreaFlags.Hazard;
                    return true;
                case "slow":
                case "slowzone":
                    flag = TrackAreaFlags.SlowZone;
                    return true;
                case "closed":
                case "blocked":
                    flag = TrackAreaFlags.Closed;
                    return true;
                case "restricted":
                case "noentry":
                    flag = TrackAreaFlags.Restricted;
                    return true;
                case "pit":
                case "pitspeed":
                    flag = TrackAreaFlags.PitSpeed;
                    return true;
            }

            return Enum.TryParse(token, true, out flag);
        }

        private static bool TryFloat(string raw, out float value)
        {
            return float.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out value);
        }

        private static bool TryInt(string raw, out int value)
        {
            return int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out value);
        }

        private static bool TryBool(string raw, out bool value)
        {
            value = false;
            if (string.IsNullOrWhiteSpace(raw))
                return false;
            if (bool.TryParse(raw, out value))
                return true;
            if (raw == "1")
            {
                value = true;
                return true;
            }
            if (raw == "0")
            {
                value = false;
                return true;
            }
            return false;
        }

        private static MapExits ParseExits(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
                return MapExits.None;

            MapExits exits = MapExits.None;
            foreach (var ch in raw.Trim().ToUpperInvariant())
            {
                switch (ch)
                {
                    case 'N':
                        exits |= MapExits.North;
                        break;
                    case 'E':
                        exits |= MapExits.East;
                        break;
                    case 'S':
                        exits |= MapExits.South;
                        break;
                    case 'W':
                        exits |= MapExits.West;
                        break;
                }
            }
            return exits;
        }

        private static (int X, int Z) Step(int x, int z, MapDirection direction)
        {
            return direction switch
            {
                MapDirection.North => (x, z + 1),
                MapDirection.East => (x + 1, z),
                MapDirection.South => (x, z - 1),
                MapDirection.West => (x - 1, z),
                _ => (x, z)
            };
        }

        public static MapDirection Opposite(MapDirection direction)
        {
            return direction switch
            {
                MapDirection.North => MapDirection.South,
                MapDirection.East => MapDirection.West,
                MapDirection.South => MapDirection.North,
                MapDirection.West => MapDirection.East,
                _ => MapDirection.North
            };
        }

        public static MapExits ExitsFromDirection(MapDirection direction)
        {
            return direction switch
            {
                MapDirection.North => MapExits.North,
                MapDirection.East => MapExits.East,
                MapDirection.South => MapExits.South,
                MapDirection.West => MapExits.West,
                _ => MapExits.None
            };
        }
    }

    public static class TrackMapValidator
    {
        public static TrackMapValidationResult ValidateFile(string nameOrPath, TrackMapValidationOptions? options = null)
        {
            if (!TrackMapFormat.TryParse(nameOrPath, out var map, out var issues) || map == null)
                return new TrackMapValidationResult(issues);

            var opts = options ?? new TrackMapValidationOptions();
            Validate(map, opts, issues);
            return new TrackMapValidationResult(issues);
        }

        public static TrackMapValidationResult Validate(TrackMapDefinition map, TrackMapValidationOptions? options = null)
        {
            var issues = new List<TrackMapIssue>();
            var opts = options ?? new TrackMapValidationOptions();
            Validate(map, opts, issues);
            return new TrackMapValidationResult(issues);
        }

        private static void Validate(TrackMapDefinition map, TrackMapValidationOptions options, List<TrackMapIssue> issues)
        {
            if (map.Cells.Count == 0)
            {
                issues.Add(new TrackMapIssue(TrackMapIssueSeverity.Error, "Map contains no cells."));
                return;
            }

            if (map.Metadata.CellSizeMeters <= 0f)
                issues.Add(new TrackMapIssue(TrackMapIssueSeverity.Error, "Cell size must be positive."));

            if (!map.Cells.ContainsKey((map.Metadata.StartX, map.Metadata.StartZ)))
                issues.Add(new TrackMapIssue(TrackMapIssueSeverity.Error, "Start cell is missing.", cellX: map.Metadata.StartX, cellZ: map.Metadata.StartZ));

            var safeCount = 0;
            var intersectionCount = 0;

            foreach (var entry in map.Cells)
            {
                var x = entry.Key.X;
                var z = entry.Key.Z;
                var cell = entry.Value;

                if (cell.WidthMeters < 0.5f)
                    issues.Add(new TrackMapIssue(TrackMapIssueSeverity.Error, "Road width must be at least 0.5 meters.", cellX: x, cellZ: z));

                if (cell.Exits == MapExits.None)
                    issues.Add(new TrackMapIssue(TrackMapIssueSeverity.Warning, "Cell has no exits.", cellX: x, cellZ: z));

                if (cell.IsSafeZone)
                    safeCount++;

                if (CountExits(cell.Exits) >= 3)
                    intersectionCount++;

                ValidateExit(map, x, z, cell, MapDirection.North, MapExits.North, issues);
                ValidateExit(map, x, z, cell, MapDirection.East, MapExits.East, issues);
                ValidateExit(map, x, z, cell, MapDirection.South, MapExits.South, issues);
                ValidateExit(map, x, z, cell, MapDirection.West, MapExits.West, issues);
            }

            if (options.RequireSafeZones && safeCount == 0)
                issues.Add(new TrackMapIssue(TrackMapIssueSeverity.Warning, "Map has no safe zones."));

            if (options.RequireIntersections && intersectionCount == 0)
                issues.Add(new TrackMapIssue(TrackMapIssueSeverity.Warning, "Map has no intersections."));

            ValidateConnectivity(map, options, issues);

            ValidateTopology(map, issues);
        }

        private static void ValidateExit(
            TrackMapDefinition map,
            int x,
            int z,
            TrackMapCellDefinition cell,
            MapDirection direction,
            MapExits exit,
            List<TrackMapIssue> issues)
        {
            if ((cell.Exits & exit) == 0)
                return;

            var (nextX, nextZ) = Step(x, z, direction);
            if (!map.Cells.TryGetValue((nextX, nextZ), out var nextCell))
            {
                issues.Add(new TrackMapIssue(TrackMapIssueSeverity.Error, "Exit points to missing neighbor.", cellX: x, cellZ: z));
                return;
            }

            var opposite = TrackMapFormat.ExitsFromDirection(TrackMapFormat.Opposite(direction));
            if ((nextCell.Exits & opposite) == 0)
                issues.Add(new TrackMapIssue(TrackMapIssueSeverity.Error, "Exit does not have a matching entry on neighbor.", cellX: x, cellZ: z));
        }

        private static void ValidateConnectivity(TrackMapDefinition map, TrackMapValidationOptions options, List<TrackMapIssue> issues)
        {
            if (!map.Cells.ContainsKey((map.Metadata.StartX, map.Metadata.StartZ)))
                return;

            var visited = new HashSet<(int X, int Z)>();
            var queue = new Queue<(int X, int Z)>();
            var start = (map.Metadata.StartX, map.Metadata.StartZ);
            visited.Add(start);
            queue.Enqueue(start);

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                if (!map.Cells.TryGetValue(current, out var cell))
                    continue;

                foreach (var (direction, exit) in EnumerateExits())
                {
                    if ((cell.Exits & exit) == 0)
                        continue;
                    var next = Step(current.X, current.Z, direction);
                    if (!map.Cells.ContainsKey(next) || visited.Contains(next))
                        continue;
                    visited.Add(next);
                    queue.Enqueue(next);
                }
            }

            if (visited.Count == map.Cells.Count)
                return;

            var unreachable = map.Cells.Count - visited.Count;
            var severity = options.TreatUnreachableCellsAsErrors ? TrackMapIssueSeverity.Error : TrackMapIssueSeverity.Warning;
            issues.Add(new TrackMapIssue(severity, $"{unreachable} cell(s) are unreachable from the start."));
        }

        private static int CountExits(MapExits exits)
        {
            var count = 0;
            if ((exits & MapExits.North) != 0) count++;
            if ((exits & MapExits.East) != 0) count++;
            if ((exits & MapExits.South) != 0) count++;
            if ((exits & MapExits.West) != 0) count++;
            return count;
        }

        private static (int X, int Z) Step(int x, int z, MapDirection direction)
        {
            return direction switch
            {
                MapDirection.North => (x, z + 1),
                MapDirection.East => (x + 1, z),
                MapDirection.South => (x, z - 1),
                MapDirection.West => (x - 1, z),
                _ => (x, z)
            };
        }

        private static IEnumerable<(MapDirection Direction, MapExits Exit)> EnumerateExits()
        {
            yield return (MapDirection.North, MapExits.North);
            yield return (MapDirection.East, MapExits.East);
            yield return (MapDirection.South, MapExits.South);
            yield return (MapDirection.West, MapExits.West);
        }

        private static void ValidateTopology(TrackMapDefinition map, List<TrackMapIssue> issues)
        {
            if (map.Shapes.Count == 0 && map.Portals.Count == 0 && map.Paths.Count == 0 &&
                map.Links.Count == 0 && map.Areas.Count == 0 && map.Beacons.Count == 0 && map.Markers.Count == 0 &&
                map.Approaches.Count == 0)
                return;

            var sectorIds = new HashSet<string>(map.Sectors.Select(s => s.Id), StringComparer.OrdinalIgnoreCase);
            var shapeIds = new HashSet<string>(map.Shapes.Select(s => s.Id), StringComparer.OrdinalIgnoreCase);
            var portalIds = new HashSet<string>(map.Portals.Select(p => p.Id), StringComparer.OrdinalIgnoreCase);

            foreach (var area in map.Areas)
            {
                if (area.WidthMeters.HasValue && area.WidthMeters.Value < 0f)
                    issues.Add(new TrackMapIssue(TrackMapIssueSeverity.Error, $"Area '{area.Id}' width must be positive."));
                if (!shapeIds.Contains(area.ShapeId))
                    issues.Add(new TrackMapIssue(TrackMapIssueSeverity.Error, $"Area '{area.Id}' references missing shape '{area.ShapeId}'."));
            }

            foreach (var portal in map.Portals)
            {
                if (portal.WidthMeters < 0f)
                    issues.Add(new TrackMapIssue(TrackMapIssueSeverity.Error, $"Portal '{portal.Id}' width must be positive."));
                if (sectorIds.Count > 0 && !sectorIds.Contains(portal.SectorId))
                    issues.Add(new TrackMapIssue(TrackMapIssueSeverity.Warning, $"Portal '{portal.Id}' references missing sector '{portal.SectorId}'."));
            }

            foreach (var link in map.Links)
            {
                if (!portalIds.Contains(link.FromPortalId) || !portalIds.Contains(link.ToPortalId))
                    issues.Add(new TrackMapIssue(TrackMapIssueSeverity.Error, $"Link '{link.Id}' references missing portal(s)."));
            }

            foreach (var path in map.Paths)
            {
                if (!string.IsNullOrWhiteSpace(path.ShapeId) && !shapeIds.Contains(path.ShapeId!))
                    issues.Add(new TrackMapIssue(TrackMapIssueSeverity.Error, $"Path '{path.Id}' references missing shape '{path.ShapeId}'."));
                if (!string.IsNullOrWhiteSpace(path.FromPortalId) && !portalIds.Contains(path.FromPortalId!))
                    issues.Add(new TrackMapIssue(TrackMapIssueSeverity.Error, $"Path '{path.Id}' references missing portal '{path.FromPortalId}'."));
                if (!string.IsNullOrWhiteSpace(path.ToPortalId) && !portalIds.Contains(path.ToPortalId!))
                    issues.Add(new TrackMapIssue(TrackMapIssueSeverity.Error, $"Path '{path.Id}' references missing portal '{path.ToPortalId}'."));
                if (path.WidthMeters < 0f)
                    issues.Add(new TrackMapIssue(TrackMapIssueSeverity.Error, $"Path '{path.Id}' width must be positive."));
            }

            foreach (var beacon in map.Beacons)
            {
                if (!string.IsNullOrWhiteSpace(beacon.ShapeId) && !shapeIds.Contains(beacon.ShapeId!))
                    issues.Add(new TrackMapIssue(TrackMapIssueSeverity.Error, $"Beacon '{beacon.Id}' references missing shape '{beacon.ShapeId}'."));
                if (sectorIds.Count > 0 && !string.IsNullOrWhiteSpace(beacon.SectorId) && !sectorIds.Contains(beacon.SectorId!))
                    issues.Add(new TrackMapIssue(TrackMapIssueSeverity.Warning, $"Beacon '{beacon.Id}' references missing sector '{beacon.SectorId}'."));
                if (string.IsNullOrWhiteSpace(beacon.ShapeId) && !beacon.ActivationRadiusMeters.HasValue)
                    issues.Add(new TrackMapIssue(TrackMapIssueSeverity.Warning, $"Beacon '{beacon.Id}' has no activation area."));
            }

            foreach (var marker in map.Markers)
            {
                if (!string.IsNullOrWhiteSpace(marker.ShapeId) && !shapeIds.Contains(marker.ShapeId!))
                    issues.Add(new TrackMapIssue(TrackMapIssueSeverity.Error, $"Marker '{marker.Id}' references missing shape '{marker.ShapeId}'."));
            }

            foreach (var approach in map.Approaches)
            {
                if (sectorIds.Count > 0 && !sectorIds.Contains(approach.SectorId))
                    issues.Add(new TrackMapIssue(TrackMapIssueSeverity.Warning, $"Approach references missing sector '{approach.SectorId}'."));
                if (!string.IsNullOrWhiteSpace(approach.EntryPortalId) && !portalIds.Contains(approach.EntryPortalId!))
                    issues.Add(new TrackMapIssue(TrackMapIssueSeverity.Error, $"Approach '{approach.SectorId}' references missing entry portal '{approach.EntryPortalId}'."));
                if (!string.IsNullOrWhiteSpace(approach.ExitPortalId) && !portalIds.Contains(approach.ExitPortalId!))
                    issues.Add(new TrackMapIssue(TrackMapIssueSeverity.Error, $"Approach '{approach.SectorId}' references missing exit portal '{approach.ExitPortalId}'."));
                if (approach.WidthMeters.HasValue && approach.WidthMeters.Value <= 0f)
                    issues.Add(new TrackMapIssue(TrackMapIssueSeverity.Error, $"Approach '{approach.SectorId}' width must be positive."));
                if (approach.LengthMeters.HasValue && approach.LengthMeters.Value <= 0f)
                    issues.Add(new TrackMapIssue(TrackMapIssueSeverity.Error, $"Approach '{approach.SectorId}' length must be positive."));
                if (approach.AlignmentToleranceDegrees.HasValue && approach.AlignmentToleranceDegrees.Value < 0f)
                    issues.Add(new TrackMapIssue(TrackMapIssueSeverity.Error, $"Approach '{approach.SectorId}' tolerance must be non-negative."));
            }
        }
    }
}

