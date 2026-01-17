using System;
using System.Collections.Generic;
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
    internal sealed class TrackMap
    {
        private readonly Dictionary<CellKey, TrackMapCell> _cells;
        private readonly List<TrackSectorDefinition> _sectors;
        private readonly List<ShapeDefinition> _shapes;
        private readonly List<TrackAreaDefinition> _areas;
        private readonly List<PortalDefinition> _portals;
        private readonly List<LinkDefinition> _links;
        private readonly List<PathDefinition> _paths;
        private readonly List<TrackBeaconDefinition> _beacons;
        private readonly List<TrackMarkerDefinition> _markers;
        private readonly List<TrackApproachDefinition> _approaches;

        public TrackMap(string name, float cellSizeMeters)
        {
            Name = string.IsNullOrWhiteSpace(name) ? "Track" : name.Trim();
            CellSizeMeters = Math.Max(0.1f, cellSizeMeters);
            _cells = new Dictionary<CellKey, TrackMapCell>();
            _sectors = new List<TrackSectorDefinition>();
            _shapes = new List<ShapeDefinition>();
            _areas = new List<TrackAreaDefinition>();
            _portals = new List<PortalDefinition>();
            _links = new List<LinkDefinition>();
            _paths = new List<PathDefinition>();
            _beacons = new List<TrackBeaconDefinition>();
            _markers = new List<TrackMarkerDefinition>();
            _approaches = new List<TrackApproachDefinition>();
        }

        public string Name { get; }
        public float CellSizeMeters { get; }
        public int CellCount => _cells.Count;
        public IReadOnlyList<TrackSectorDefinition> Sectors => _sectors;
        public IReadOnlyList<TrackAreaDefinition> Areas => _areas;
        public IReadOnlyList<ShapeDefinition> Shapes => _shapes;
        public IReadOnlyList<PortalDefinition> Portals => _portals;
        public IReadOnlyList<LinkDefinition> Links => _links;
        public IReadOnlyList<PathDefinition> Paths => _paths;
        public IReadOnlyList<TrackBeaconDefinition> Beacons => _beacons;
        public IReadOnlyList<TrackMarkerDefinition> Markers => _markers;
        public IReadOnlyList<TrackApproachDefinition> Approaches => _approaches;
        public TrackWeather Weather { get; set; } = TrackWeather.Sunny;
        public TrackAmbience Ambience { get; set; } = TrackAmbience.NoAmbience;
        public TrackSurface DefaultSurface { get; set; } = TrackSurface.Asphalt;
        public TrackNoise DefaultNoise { get; set; } = TrackNoise.NoNoise;
        public float DefaultWidthMeters { get; set; } = 12f;
        public int StartX { get; set; }
        public int StartZ { get; set; }
        public MapDirection StartHeading { get; set; } = MapDirection.North;

        public bool TryGetCell(int x, int z, out TrackMapCell cell)
        {
            return _cells.TryGetValue(new CellKey(x, z), out cell!);
        }

        public TrackMapCell GetOrCreateCell(int x, int z)
        {
            var key = new CellKey(x, z);
            if (_cells.TryGetValue(key, out var cell))
                return cell;

            cell = new TrackMapCell
            {
                Exits = MapExits.None,
                Surface = DefaultSurface,
                Noise = DefaultNoise,
                WidthMeters = DefaultWidthMeters,
                IsSafeZone = false
            };
            _cells[key] = cell;
            return cell;
        }

        public void AddSector(TrackSectorDefinition sector)
        {
            if (sector == null)
                throw new ArgumentNullException(nameof(sector));
            _sectors.Add(sector);
        }

        public void AddShape(ShapeDefinition shape)
        {
            if (shape == null)
                throw new ArgumentNullException(nameof(shape));
            _shapes.Add(shape);
        }

        public void AddArea(TrackAreaDefinition area)
        {
            if (area == null)
                throw new ArgumentNullException(nameof(area));
            _areas.Add(area);
        }

        public void AddPortal(PortalDefinition portal)
        {
            if (portal == null)
                throw new ArgumentNullException(nameof(portal));
            _portals.Add(portal);
        }

        public void AddLink(LinkDefinition link)
        {
            if (link == null)
                throw new ArgumentNullException(nameof(link));
            _links.Add(link);
        }

        public void AddPath(PathDefinition path)
        {
            if (path == null)
                throw new ArgumentNullException(nameof(path));
            _paths.Add(path);
        }

        public void AddBeacon(TrackBeaconDefinition beacon)
        {
            if (beacon == null)
                throw new ArgumentNullException(nameof(beacon));
            _beacons.Add(beacon);
        }

        public void AddMarker(TrackMarkerDefinition marker)
        {
            if (marker == null)
                throw new ArgumentNullException(nameof(marker));
            _markers.Add(marker);
        }

        public void AddApproach(TrackApproachDefinition approach)
        {
            if (approach == null)
                throw new ArgumentNullException(nameof(approach));
            _approaches.Add(approach);
        }

        public TrackAreaManager BuildAreaManager()
        {
            return new TrackAreaManager(_shapes, _areas);
        }

        public TrackPortalManager BuildPortalManager()
        {
            return new TrackPortalManager(_portals, _links);
        }

        public TrackSectorManager BuildSectorManager()
        {
            return new TrackSectorManager(_sectors, BuildAreaManager(), BuildPortalManager());
        }

        public TrackApproachManager BuildApproachManager()
        {
            return new TrackApproachManager(_sectors, _approaches, BuildPortalManager());
        }

        public void MergeCell(int x, int z, MapExits exits, TrackSurface? surface, TrackNoise? noise, float? widthMeters, bool? safeZone, string? zone)
        {
            var cell = GetOrCreateCell(x, z);
            cell.Exits |= exits;
            if (surface.HasValue)
                cell.Surface = surface.Value;
            if (noise.HasValue)
                cell.Noise = noise.Value;
            if (widthMeters.HasValue)
                cell.WidthMeters = Math.Max(0.5f, widthMeters.Value);
            if (safeZone.HasValue)
                cell.IsSafeZone = safeZone.Value;
            if (!string.IsNullOrWhiteSpace(zone))
                cell.Zone = zone!.Trim();
        }

        public bool TryStep(int x, int z, MapDirection direction, out int nextX, out int nextZ, out TrackMapCell nextCell)
        {
            nextCell = null!;
            nextX = x;
            nextZ = z;
            if (!TryGetCell(x, z, out var cell))
                return false;

            if (!AllowsExit(cell, direction))
                return false;

            (nextX, nextZ) = Offset(x, z, direction);
            if (!TryGetCell(nextX, nextZ, out nextCell))
                return false;

            if (!AllowsEntry(nextCell, direction))
                return false;

            return true;
        }

        public Vector3 CellToWorld(int x, int z)
        {
            return new Vector3(x * CellSizeMeters, 0f, z * CellSizeMeters);
        }

        public (int X, int Z) WorldToCell(Vector3 worldPosition)
        {
            var x = (int)Math.Round(worldPosition.X / CellSizeMeters, MidpointRounding.AwayFromZero);
            var z = (int)Math.Round(worldPosition.Z / CellSizeMeters, MidpointRounding.AwayFromZero);
            return (x, z);
        }

        public static MapDirection? DirectionFromDelta(Vector3 delta)
        {
            if (Math.Abs(delta.X) > Math.Abs(delta.Z))
                return delta.X >= 0f ? MapDirection.East : MapDirection.West;
            if (Math.Abs(delta.Z) > 0f)
                return delta.Z >= 0f ? MapDirection.North : MapDirection.South;
            return null;
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

        private static (int X, int Z) Offset(int x, int z, MapDirection direction)
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

        private static bool AllowsExit(TrackMapCell cell, MapDirection direction)
        {
            return (cell.Exits & ExitsFromDirection(direction)) != 0;
        }

        private static bool AllowsEntry(TrackMapCell cell, MapDirection direction)
        {
            var opposite = Opposite(direction);
            return (cell.Exits & ExitsFromDirection(opposite)) != 0;
        }

        private readonly struct CellKey : IEquatable<CellKey>
        {
            public CellKey(int x, int z)
            {
                X = x;
                Z = z;
            }

            public int X { get; }
            public int Z { get; }

            public bool Equals(CellKey other)
            {
                return X == other.X && Z == other.Z;
            }

            public override bool Equals(object? obj)
            {
                return obj is CellKey other && Equals(other);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    return (X * 397) ^ Z;
                }
            }
        }
    }
}
