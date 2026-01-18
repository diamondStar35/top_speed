using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using TopSpeed.Audio;
using TopSpeed.Core;
using TopSpeed.Data;
using TopSpeed.Tracks.Areas;
using TopSpeed.Tracks.Guidance;
using TopSpeed.Tracks.Geometry;
using TopSpeed.Tracks.Sectors;
using TopSpeed.Tracks.Topology;
using TS.Audio;

namespace TopSpeed.Tracks.Map
{
    internal sealed class MapTrack : IDisposable
    {
        private const float CallLengthMeters = 30.0f;

        private readonly AudioManager _audio;
        private readonly TrackMap _map;
        private readonly TrackAreaManager _areaManager;
        private readonly TrackSectorManager _sectorManager;
        private readonly TrackSectorRuleManager _sectorRuleManager;
        private readonly TrackPortalManager _portalManager;
        private readonly TrackApproachBeacon _approachBeacon;
        private readonly TrackPathManager _pathManager;
        private readonly TrackBranchManager _branchManager;
        private readonly string _trackName;
        private readonly bool _userDefined;
        private TrackNoise _currentNoise;

        private AudioSourceHandle? _soundCrowd;
        private AudioSourceHandle? _soundOcean;
        private AudioSourceHandle? _soundRain;
        private AudioSourceHandle? _soundWind;
        private AudioSourceHandle? _soundStorm;
        private AudioSourceHandle? _soundDesert;
        private AudioSourceHandle? _soundAirport;
        private AudioSourceHandle? _soundAirplane;
        private AudioSourceHandle? _soundClock;
        private AudioSourceHandle? _soundJet;
        private AudioSourceHandle? _soundThunder;
        private AudioSourceHandle? _soundPile;
        private AudioSourceHandle? _soundConstruction;
        private AudioSourceHandle? _soundRiver;
        private AudioSourceHandle? _soundHelicopter;
        private AudioSourceHandle? _soundOwl;
        private AudioSourceHandle? _soundBeacon;
        private float _beaconCooldown;

        private MapTrack(string trackName, TrackMap map, AudioManager audio, bool userDefined)
        {
            _trackName = string.IsNullOrWhiteSpace(trackName) ? "Track" : trackName.Trim();
            _map = map ?? throw new ArgumentNullException(nameof(map));
            _audio = audio ?? throw new ArgumentNullException(nameof(audio));
            _userDefined = userDefined;
            _currentNoise = TrackNoise.NoNoise;
            _areaManager = map.BuildAreaManager();
            _portalManager = map.BuildPortalManager();
            _sectorManager = new TrackSectorManager(map.Sectors, _areaManager, _portalManager);
            _sectorRuleManager = map.BuildSectorRuleManager();
            _approachBeacon = new TrackApproachBeacon(map);
            _pathManager = new TrackPathManager(map.Paths, map.Shapes, _portalManager, map.DefaultWidthMeters);
            _branchManager = map.BuildBranchManager();
            InitializeSounds();
        }

        public static MapTrack Load(string nameOrPath, AudioManager audio)
        {
            if (!TrackMapLoader.TryResolvePath(nameOrPath, out var path))
                throw new FileNotFoundException("Track map not found.", nameOrPath);

            var map = TrackMapLoader.Load(path);
            var name = map.Name;
            var userDefined = path.IndexOfAny(new[] { '\\', '/' }) >= 0;
            return new MapTrack(name, map, audio, userDefined);
        }

        public string TrackName => _trackName;
        public TrackWeather Weather => _map.Weather;
        public TrackAmbience Ambience => _map.Ambience;
        public TrackSurface InitialSurface => _map.DefaultSurface;
        public bool UserDefined => _userDefined;
        public float LaneWidth => Math.Max(0.5f, _map.DefaultWidthMeters * 0.5f);
        public TrackMap Map => _map;
        public float Length => Math.Max(0f, _map.CellCount * _map.CellSizeMeters);
        public TrackSectorRuleManager SectorRules => _sectorRuleManager;
        public TrackBranchManager Branches => _branchManager;

        public int Lap(float distanceMeters)
        {
            var length = Length;
            if (length <= 0f)
                return 1;
            return (int)(distanceMeters / length) + 1;
        }

        public void SetLaneWidth(float laneWidth)
        {
            _map.DefaultWidthMeters = Math.Max(0.5f, laneWidth * 2f);
        }

        public MapMovementState CreateStartState()
        {
            return MapMovement.CreateStart(_map);
        }

        public MapMovementState CreateStateFromWorld(Vector3 worldPosition, MapDirection heading)
        {
            var (x, z) = _map.WorldToCell(worldPosition);
            return new MapMovementState
            {
                CellX = x,
                CellZ = z,
                Heading = heading,
                HeadingDegrees = HeadingDegrees(heading),
                WorldPosition = _map.CellToWorld(x, z),
                DistanceMeters = 0f,
                PendingMeters = 0f
            };
        }

        public TrackPose GetPose(MapMovementState state)
        {
            var forward = MapMovement.DirectionVector(state.Heading);
            var right = new Vector3(forward.Z, 0f, -forward.X);
            var up = Vector3.UnitY;
            var heading = state.Heading switch
            {
                MapDirection.North => 0f,
                MapDirection.East => (float)(Math.PI * 0.5),
                MapDirection.South => (float)Math.PI,
                MapDirection.West => (float)(-Math.PI * 0.5),
                _ => 0f
            };
            return new TrackPose(state.WorldPosition, forward, right, up, heading, 0f);
        }

        public TrackRoad RoadAt(MapMovementState state)
        {
            if (!_map.TryGetCell(state.CellX, state.CellZ, out var cell))
            {
                var defaultRoad = BuildDefaultRoad();
                var defaultSafeZone = false;
                var defaultLength = _map.CellSizeMeters;
                var defaultWidth = Math.Max(0.5f, _map.DefaultWidthMeters);
                var defaultSurface = _map.DefaultSurface;
                var defaultNoise = _map.DefaultNoise;
                ApplyPathWidth(state.WorldPosition, ref defaultWidth);
                ApplyAreaOverrides(state.WorldPosition, state.Heading, ref defaultWidth, ref defaultLength, ref defaultSurface, ref defaultNoise, ref defaultSafeZone);
                defaultRoad.Left = -defaultWidth * 0.5f;
                defaultRoad.Right = defaultWidth * 0.5f;
                defaultRoad.Length = defaultLength;
                defaultRoad.Surface = defaultSurface;
                defaultRoad.IsSafeZone = defaultSafeZone;
                defaultRoad.IsOutOfBounds = !IsWithinTrackInternal(state.WorldPosition, defaultSafeZone);
                ApplySectorRuleFields(state.WorldPosition, state.Heading, ref defaultRoad);
                return defaultRoad;
            }

            var width = Math.Max(0.5f, cell.WidthMeters);
            var length = _map.CellSizeMeters;
            var surface = cell.Surface;
            var noise = cell.Noise;
            var safeZone = cell.IsSafeZone;

            ApplyPathWidth(state.WorldPosition, ref width);
            ApplyAreaOverrides(state.WorldPosition, state.Heading, ref width, ref length, ref surface, ref noise, ref safeZone);

            var road = new TrackRoad
            {
                Left = -width * 0.5f,
                Right = width * 0.5f,
                Surface = surface,
                Type = ResolveCurveType(cell.Exits, state.Heading),
                Length = length,
                IsSafeZone = safeZone,
                IsOutOfBounds = !IsWithinTrackInternal(state.WorldPosition, safeZone)
            };
            ApplySectorRuleFields(state.WorldPosition, state.Heading, ref road);
            return road;
        }

        public bool TryGetSectorRules(
            Vector3 worldPosition,
            MapDirection heading,
            out TrackSectorDefinition sector,
            out TrackSectorRules rules,
            out PortalDefinition? portal,
            out float? portalDeltaDegrees)
        {
            sector = null!;
            rules = null!;
            portal = null;
            portalDeltaDegrees = null;

            if (_sectorManager == null || _sectorRuleManager == null)
                return false;

            var position2D = new Vector2(worldPosition.X, worldPosition.Z);
            if (!_sectorManager.TryLocate(position2D, HeadingDegrees(heading), out var foundSector, out var foundPortal, out var delta))
                return false;

            if (!_sectorRuleManager.TryGetRules(foundSector.Id, out var foundRules))
                return false;

            sector = foundSector;
            rules = foundRules;
            portal = foundPortal;
            portalDeltaDegrees = delta;
            return true;
        }

        public bool TryMove(ref MapMovementState state, float distanceMeters, MapDirection heading, out TrackRoad road, out bool boundaryHit)
        {
            boundaryHit = false;
            road = BuildDefaultRoad();
            var previousState = state;
            var previousPosition = state.WorldPosition;
            if (!MapMovement.TryMove(_map, ref state, distanceMeters, heading, out var cell, out boundaryHit))
            {
                road = RoadAt(state);
                return false;
            }

            if (!IsSectorTransitionAllowed(previousPosition, state.WorldPosition, heading))
            {
                state = previousState;
                boundaryHit = true;
                road = RoadAt(state);
                return false;
            }

            road = RoadAt(state);
            return true;
        }

        public bool NextRoad(MapMovementState state, float speed, int curveAnnouncementMode, out TrackRoad road)
        {
            road = RoadAt(state);
            if (!_map.TryGetCell(state.CellX, state.CellZ, out _))
                return false;

            var steps = (int)Math.Max(1f, Math.Round(CallLengthMeters / _map.CellSizeMeters));
            var x = state.CellX;
            var z = state.CellZ;
            var heading = state.Heading;
            var currentType = road.Type;

            for (var i = 0; i < steps; i++)
            {
                if (!_map.TryStep(x, z, heading, out var nextX, out var nextZ, out var nextCell))
                    return false;

                var nextState = state;
                nextState.CellX = nextX;
                nextState.CellZ = nextZ;
                var nextRoad = new TrackRoad
                {
                    Left = -Math.Max(0.5f, nextCell.WidthMeters) * 0.5f,
                    Right = Math.Max(0.5f, nextCell.WidthMeters) * 0.5f,
                    Surface = nextCell.Surface,
                    Type = ResolveCurveType(nextCell.Exits, heading),
                    Length = _map.CellSizeMeters,
                    IsSafeZone = nextCell.IsSafeZone,
                    IsOutOfBounds = false
                };

                if (nextRoad.Type != currentType)
                {
                    road = nextRoad;
                    return true;
                }

                x = nextX;
                z = nextZ;
            }

            return false;
        }

        public void Initialize()
        {
            _currentNoise = TrackNoise.NoNoise;
            _beaconCooldown = 0f;
        }

        public void Run(MapMovementState state, float elapsed)
        {
            if (!_map.TryGetCell(state.CellX, state.CellZ, out var cell))
                return;

            var noise = cell.Noise;
            var surface = cell.Surface;
            var safeZone = cell.IsSafeZone;
            var length = _map.CellSizeMeters;
            var width = Math.Max(0.5f, cell.WidthMeters);
            ApplyAreaOverrides(state.WorldPosition, state.Heading, ref width, ref length, ref surface, ref noise, ref safeZone);
            if (noise != _currentNoise)
            {
                StopNoise(_currentNoise);
                _currentNoise = noise;
            }

            UpdateNoiseLoop(noise);
            UpdateApproachBeacon(state, elapsed);

            if (_map.Weather == TrackWeather.Rain)
                PlayIfNotPlaying(_soundRain);
            else
                StopSound(_soundRain);

            if (_map.Weather == TrackWeather.Wind)
                PlayIfNotPlaying(_soundWind);
            else
                StopSound(_soundWind);

            if (_map.Weather == TrackWeather.Storm)
                PlayIfNotPlaying(_soundStorm);
            else
                StopSound(_soundStorm);

            if (_map.Ambience == TrackAmbience.Desert)
                PlayIfNotPlaying(_soundDesert);
            else if (_map.Ambience == TrackAmbience.Airport)
                PlayIfNotPlaying(_soundAirport);
            else
            {
                StopSound(_soundDesert);
                StopSound(_soundAirport);
            }
        }

        public void FinalizeTrack()
        {
            StopAllSounds();
        }

        public void Dispose()
        {
            FinalizeTrack();
            DisposeSound(_soundCrowd);
            DisposeSound(_soundOcean);
            DisposeSound(_soundRain);
            DisposeSound(_soundWind);
            DisposeSound(_soundStorm);
            DisposeSound(_soundDesert);
            DisposeSound(_soundAirport);
            DisposeSound(_soundAirplane);
            DisposeSound(_soundClock);
            DisposeSound(_soundJet);
            DisposeSound(_soundThunder);
            DisposeSound(_soundPile);
            DisposeSound(_soundConstruction);
            DisposeSound(_soundRiver);
            DisposeSound(_soundHelicopter);
            DisposeSound(_soundOwl);
            DisposeSound(_soundBeacon);
        }

        private TrackRoad BuildDefaultRoad()
        {
            var width = Math.Max(0.5f, _map.DefaultWidthMeters);
            return new TrackRoad
            {
                Left = -width * 0.5f,
                Right = width * 0.5f,
                Surface = _map.DefaultSurface,
                Type = TrackType.Straight,
                Length = _map.CellSizeMeters
            };
        }

        private void ApplyAreaOverrides(
            Vector3 worldPosition,
            MapDirection heading,
            ref float width,
            ref float length,
            ref TrackSurface surface,
            ref TrackNoise noise,
            ref bool safeZone)
        {
            var position = new Vector2(worldPosition.X, worldPosition.Z);
            ApplySectorOverrides(position, heading, ref width, ref length, ref surface, ref noise, ref safeZone);

            if (_areaManager == null)
                return;

            var areas = _areaManager.FindAreasContaining(position);
            if (areas.Count == 0)
                return;

            var area = areas[areas.Count - 1];
            if (area.Surface.HasValue)
                surface = area.Surface.Value;
            if (area.Noise.HasValue)
                noise = area.Noise.Value;
            if (area.WidthMeters.HasValue)
                width = Math.Max(0.5f, area.WidthMeters.Value);
            if (area.Type == TrackAreaType.SafeZone || (area.Flags & TrackAreaFlags.SafeZone) != 0)
                safeZone = true;

            if (!TryApplyMetadataDimensions(area.Metadata, ref width, ref length))
                TryApplyShapeDimensions(area, heading, ref width, ref length);
        }

        private void ApplySectorOverrides(
            Vector2 position,
            MapDirection heading,
            ref float width,
            ref float length,
            ref TrackSurface surface,
            ref TrackNoise noise,
            ref bool safeZone)
        {
            if (_sectorManager == null)
                return;

            var sectors = _sectorManager.FindSectorsContaining(position);
            if (sectors.Count == 0)
                return;

            var sector = sectors[sectors.Count - 1];
            if (sector.Surface.HasValue)
                surface = sector.Surface.Value;
            if (sector.Noise.HasValue)
                noise = sector.Noise.Value;
            if ((sector.Flags & TrackSectorFlags.SafeZone) != 0)
                safeZone = true;

            TryApplyMetadataDimensions(sector.Metadata, ref width, ref length);
        }

        private static bool TryApplyMetadataDimensions(
            IReadOnlyDictionary<string, string> metadata,
            ref float width,
            ref float length)
        {
            if (metadata == null || metadata.Count == 0)
                return false;

            var hadAny = false;
            if (TryGetMetadataFloat(metadata, out var widthValue, "intersection_width", "width", "lane_width"))
            {
                width = Math.Max(0.5f, widthValue);
                hadAny = true;
            }
            if (TryGetMetadataFloat(metadata, out var lengthValue, "intersection_length", "length"))
            {
                length = Math.Max(0.1f, lengthValue);
                hadAny = true;
            }
            return hadAny;
        }

        private void TryApplyShapeDimensions(
            TrackAreaDefinition area,
            MapDirection heading,
            ref float width,
            ref float length)
        {
            if (!_areaManager.TryGetShape(area.ShapeId, out var shape))
                return;

            switch (shape.Type)
            {
                case ShapeType.Rectangle:
                    ApplyRectangleDimensions(shape, heading, ref width, ref length);
                    break;
                case ShapeType.Circle:
                    width = Math.Max(width, shape.Radius * 2f);
                    length = Math.Max(length, shape.Radius * 2f);
                    break;
            }
        }

        private static void ApplyRectangleDimensions(
            ShapeDefinition shape,
            MapDirection heading,
            ref float width,
            ref float length)
        {
            var rectWidth = Math.Abs(shape.Width);
            var rectHeight = Math.Abs(shape.Height);
            if (rectWidth <= 0f || rectHeight <= 0f)
                return;

            switch (heading)
            {
                case MapDirection.North:
                case MapDirection.South:
                    width = Math.Max(width, rectWidth);
                    length = Math.Max(length, rectHeight);
                    break;
                case MapDirection.East:
                case MapDirection.West:
                    width = Math.Max(width, rectHeight);
                    length = Math.Max(length, rectWidth);
                    break;
            }
        }

        private static bool TryGetMetadataFloat(
            IReadOnlyDictionary<string, string> metadata,
            out float value,
            params string[] keys)
        {
            value = 0f;
            foreach (var key in keys)
            {
                if (!metadata.TryGetValue(key, out var raw))
                    continue;
                if (float.TryParse(raw, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out value))
                    return true;
            }
            return false;
        }

        private static TrackType ResolveCurveType(MapExits exits, MapDirection heading)
        {
            var count = CountExits(exits);
            if (count >= 3)
                return TrackType.Straight;

            if (count == 2)
            {
                if (exits == (MapExits.North | MapExits.South) || exits == (MapExits.East | MapExits.West))
                    return TrackType.Straight;

                var right = TurnRight(heading);
                var left = TurnLeft(heading);
                var hasRight = (exits & TrackMap.ExitsFromDirection(right)) != 0;
                var hasLeft = (exits & TrackMap.ExitsFromDirection(left)) != 0;
                if (hasRight && !hasLeft)
                    return TrackType.Right;
                if (hasLeft && !hasRight)
                    return TrackType.Left;
            }

            return TrackType.Straight;
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

        private static MapDirection TurnRight(MapDirection heading)
        {
            return heading switch
            {
                MapDirection.North => MapDirection.East,
                MapDirection.East => MapDirection.South,
                MapDirection.South => MapDirection.West,
                MapDirection.West => MapDirection.North,
                _ => MapDirection.North
            };
        }

        private static MapDirection TurnLeft(MapDirection heading)
        {
            return heading switch
            {
                MapDirection.North => MapDirection.West,
                MapDirection.West => MapDirection.South,
                MapDirection.South => MapDirection.East,
                MapDirection.East => MapDirection.North,
                _ => MapDirection.North
            };
        }

        private void InitializeSounds()
        {
            var root = Path.Combine(AssetPaths.SoundsRoot, "Legacy");
            _soundCrowd = CreateLoop(root, "crowd.wav");
            _soundOcean = CreateLoop(root, "ocean.wav");
            _soundRain = CreateLoop(root, "rain.wav");
            _soundWind = CreateLoop(root, "wind.wav");
            _soundStorm = CreateLoop(root, "storm.wav");
            _soundDesert = CreateLoop(root, "desert.wav");
            _soundAirport = CreateLoop(root, "airport.wav");
            _soundAirplane = CreateLoop(root, "airplane.wav");
            _soundClock = CreateLoop(root, "clock.wav");
            _soundJet = CreateLoop(root, "jet.wav");
            _soundThunder = CreateLoop(root, "thunder.wav");
            _soundPile = CreateLoop(root, "pile.wav");
            _soundConstruction = CreateLoop(root, "const.wav");
            _soundRiver = CreateLoop(root, "river.wav");
            _soundHelicopter = CreateLoop(root, "helicopter.wav");
            _soundOwl = CreateLoop(root, "owl.wav");
            _soundBeacon = CreateSpatial(root, "beacon.wav");
        }

        private void ApplyPathWidth(Vector3 worldPosition, ref float width)
        {
            if (_pathManager == null || !_pathManager.HasPaths)
                return;

            var position = new Vector2(worldPosition.X, worldPosition.Z);
            var paths = _pathManager.FindPathsContaining(position);
            if (paths.Count == 0)
                return;

            var path = paths[paths.Count - 1];
            if (path.WidthMeters > 0f)
                width = Math.Max(0.5f, path.WidthMeters);
        }

        private AudioSourceHandle? CreateLoop(string root, string file)
        {
            var path = Path.Combine(root, file);
            if (!File.Exists(path))
                return null;
            return _audio.CreateLoopingSource(path);
        }

        private AudioSourceHandle? CreateSpatial(string root, string file)
        {
            var path = Path.Combine(root, file);
            if (!File.Exists(path))
                return null;
            return _audio.CreateSpatialSource(path, streamFromDisk: true, allowHrtf: true);
        }

        private void UpdateNoiseLoop(TrackNoise noise)
        {
            switch (noise)
            {
                case TrackNoise.Crowd:
                    PlayIfNotPlaying(_soundCrowd);
                    break;
                case TrackNoise.Ocean:
                    PlayIfNotPlaying(_soundOcean);
                    break;
                case TrackNoise.Trackside:
                    PlayIfNotPlaying(_soundAirplane);
                    break;
                case TrackNoise.Clock:
                    PlayIfNotPlaying(_soundClock);
                    break;
                case TrackNoise.Jet:
                    PlayIfNotPlaying(_soundJet);
                    break;
                case TrackNoise.Thunder:
                    PlayIfNotPlaying(_soundThunder);
                    break;
                case TrackNoise.Pile:
                case TrackNoise.Construction:
                    PlayIfNotPlaying(_soundPile);
                    PlayIfNotPlaying(_soundConstruction);
                    break;
                case TrackNoise.River:
                    PlayIfNotPlaying(_soundRiver);
                    break;
                case TrackNoise.Helicopter:
                    PlayIfNotPlaying(_soundHelicopter);
                    break;
                case TrackNoise.Owl:
                    PlayIfNotPlaying(_soundOwl);
                    break;
            }
        }

        private void StopNoise(TrackNoise noise)
        {
            switch (noise)
            {
                case TrackNoise.Crowd:
                    StopSound(_soundCrowd);
                    break;
                case TrackNoise.Ocean:
                    StopSound(_soundOcean);
                    break;
                case TrackNoise.Trackside:
                    StopSound(_soundAirplane);
                    break;
                case TrackNoise.Clock:
                    StopSound(_soundClock);
                    break;
                case TrackNoise.Jet:
                    StopSound(_soundJet);
                    break;
                case TrackNoise.Thunder:
                    StopSound(_soundThunder);
                    break;
                case TrackNoise.Pile:
                case TrackNoise.Construction:
                    StopSound(_soundPile);
                    StopSound(_soundConstruction);
                    break;
                case TrackNoise.River:
                    StopSound(_soundRiver);
                    break;
                case TrackNoise.Helicopter:
                    StopSound(_soundHelicopter);
                    break;
                case TrackNoise.Owl:
                    StopSound(_soundOwl);
                    break;
            }
        }

        private static void PlayIfNotPlaying(AudioSourceHandle? sound)
        {
            if (sound == null)
                return;
            if (!sound.IsPlaying)
                sound.Play(loop: true);
        }

        private void UpdateApproachBeacon(MapMovementState state, float elapsed)
        {
            if (_soundBeacon == null)
                return;

            var headingDegrees = state.HeadingDegrees;
            if (_approachBeacon.TryGetCue(state.WorldPosition, headingDegrees, out var cue) && !cue.Passed)
            {
                var position = AudioWorld.ToMeters(new Vector3(cue.PortalPosition.X, 0f, cue.PortalPosition.Y));
                _soundBeacon.SetPosition(position);
                _soundBeacon.SetVelocity(Vector3.Zero);
                _beaconCooldown -= elapsed;
                if (_beaconCooldown <= 0f)
                {
                    _soundBeacon.Stop();
                    _soundBeacon.SeekToStart();
                    _soundBeacon.Play(loop: false);
                    _beaconCooldown = 1.5f;
                }
                return;
            }

            _beaconCooldown = 0f;
            StopSound(_soundBeacon);
        }

        public bool IsWithinTrack(Vector3 worldPosition)
        {
            var position = new Vector2(worldPosition.X, worldPosition.Z);
            var safeZone = IsSafeZone(position);
            var (cellX, cellZ) = _map.WorldToCell(worldPosition);
            if (_map.TryGetCell(cellX, cellZ, out var cell) && cell.IsSafeZone)
                safeZone = true;
            return IsWithinTrackInternal(worldPosition, safeZone);
        }

        private bool IsWithinTrackInternal(Vector3 worldPosition, bool isSafeZone)
        {
            var position = new Vector2(worldPosition.X, worldPosition.Z);
            if (IsBlockedBySectorRules(position))
                return false;
            if (_pathManager.HasPaths)
            {
                if (_pathManager.ContainsAny(position))
                    return true;
                return isSafeZone;
            }

            var (cellX, cellZ) = _map.WorldToCell(worldPosition);
            return _map.TryGetCell(cellX, cellZ, out _);
        }

        private bool IsSafeZone(Vector2 position)
        {
            if (_areaManager == null)
                return false;

            var areas = _areaManager.FindAreasContaining(position);
            if (areas.Count == 0)
                return false;

            foreach (var area in areas)
            {
                if (area.Type == TrackAreaType.SafeZone || (area.Flags & TrackAreaFlags.SafeZone) != 0)
                    return true;
            }
            return false;
        }

        private static float HeadingDegrees(MapDirection heading)
        {
            return heading switch
            {
                MapDirection.North => 0f,
                MapDirection.East => 90f,
                MapDirection.South => 180f,
                MapDirection.West => 270f,
                _ => 0f
            };
        }

        internal bool IsSectorTransitionAllowed(Vector3 fromPosition, Vector3 toPosition, MapDirection heading)
        {
            if (_sectorRuleManager == null || _sectorManager == null)
                return true;

            var headingDegrees = HeadingDegrees(heading);
            var fromPos = new Vector2(fromPosition.X, fromPosition.Z);
            var toPos = new Vector2(toPosition.X, toPosition.Z);

            var hasFrom = _sectorManager.TryLocate(fromPos, headingDegrees, out var fromSector, out var fromPortal, out _);
            var hasTo = _sectorManager.TryLocate(toPos, headingDegrees, out var toSector, out var toPortal, out _);

            if (!hasTo)
                return true;

            if (!_sectorRuleManager.TryGetRules(toSector.Id, out var toRules))
                return true;

            if (toRules.IsClosed || toRules.IsRestricted)
                return false;

            if (hasFrom && !string.Equals(fromSector.Id, toSector.Id, StringComparison.OrdinalIgnoreCase))
            {
                var entryPortalId = toPortal?.Id;
                var exitPortalId = fromPortal?.Id;
                if (!_sectorRuleManager.AllowsExit(fromSector.Id, exitPortalId, heading))
                    return false;
                if (!_sectorRuleManager.AllowsEntry(toSector.Id, entryPortalId, heading))
                    return false;
            }

            return true;
        }

        private void ApplySectorRuleFields(Vector3 worldPosition, MapDirection heading, ref TrackRoad road)
        {
            if (_sectorRuleManager == null || _sectorManager == null)
                return;

            var position = new Vector2(worldPosition.X, worldPosition.Z);
            if (!_sectorManager.TryLocate(position, HeadingDegrees(heading), out var sector, out _, out _))
                return;
            if (!_sectorRuleManager.TryGetRules(sector.Id, out var rules))
                return;

            road.IsClosed = rules.IsClosed;
            road.IsRestricted = rules.IsRestricted;
            road.RequiresStop = rules.RequiresStop;
            road.RequiresYield = rules.RequiresYield;
            road.MinSpeedKph = rules.MinSpeedKph;
            road.MaxSpeedKph = rules.MaxSpeedKph;
        }

        private bool IsBlockedBySectorRules(Vector2 position)
        {
            if (_sectorRuleManager == null || _sectorManager == null)
                return false;

            var sectors = _sectorManager.FindSectorsContaining(position);
            if (sectors.Count == 0)
                return false;

            foreach (var sector in sectors)
            {
                if (_sectorRuleManager.TryGetRules(sector.Id, out var rules) &&
                    (rules.IsClosed || rules.IsRestricted))
                    return true;
            }

            return false;
        }

        private static void StopSound(AudioSourceHandle? sound)
        {
            if (sound == null)
                return;
            sound.Stop();
        }

        private void StopAllSounds()
        {
            StopSound(_soundCrowd);
            StopSound(_soundOcean);
            StopSound(_soundRain);
            StopSound(_soundWind);
            StopSound(_soundStorm);
            StopSound(_soundDesert);
            StopSound(_soundAirport);
            StopSound(_soundAirplane);
            StopSound(_soundClock);
            StopSound(_soundJet);
            StopSound(_soundThunder);
            StopSound(_soundPile);
            StopSound(_soundConstruction);
            StopSound(_soundRiver);
            StopSound(_soundHelicopter);
            StopSound(_soundOwl);
            StopSound(_soundBeacon);
        }

        private static void DisposeSound(AudioSourceHandle? sound)
        {
            if (sound == null)
                return;
            sound.Stop();
            sound.Dispose();
        }
    }
}
