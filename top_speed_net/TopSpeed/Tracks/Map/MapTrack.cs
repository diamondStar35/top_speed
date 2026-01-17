using System;
using System.IO;
using System.Numerics;
using TopSpeed.Audio;
using TopSpeed.Core;
using TopSpeed.Data;
using TopSpeed.Tracks.Geometry;
using TS.Audio;

namespace TopSpeed.Tracks.Map
{
    internal sealed class MapTrack : IDisposable
    {
        private const float CallLengthMeters = 30.0f;

        private readonly AudioManager _audio;
        private readonly TrackMap _map;
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

        private MapTrack(string trackName, TrackMap map, AudioManager audio, bool userDefined)
        {
            _trackName = string.IsNullOrWhiteSpace(trackName) ? "Track" : trackName.Trim();
            _map = map ?? throw new ArgumentNullException(nameof(map));
            _audio = audio ?? throw new ArgumentNullException(nameof(audio));
            _userDefined = userDefined;
            _currentNoise = TrackNoise.NoNoise;
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
                return BuildDefaultRoad();

            var width = Math.Max(0.5f, cell.WidthMeters);
            return new TrackRoad
            {
                Left = -width * 0.5f,
                Right = width * 0.5f,
                Surface = cell.Surface,
                Type = ResolveCurveType(cell.Exits, state.Heading),
                Length = _map.CellSizeMeters,
                IsSafeZone = cell.IsSafeZone,
                IsOutOfBounds = false
            };
        }

        public bool TryMove(ref MapMovementState state, float distanceMeters, MapDirection heading, out TrackRoad road, out bool boundaryHit)
        {
            boundaryHit = false;
            road = BuildDefaultRoad();
            if (!MapMovement.TryMove(_map, ref state, distanceMeters, heading, out var cell, out boundaryHit))
            {
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
        }

        public void Run(MapMovementState state)
        {
            if (!_map.TryGetCell(state.CellX, state.CellZ, out var cell))
                return;

            var noise = cell.Noise;
            if (noise != _currentNoise)
            {
                StopNoise(_currentNoise);
                _currentNoise = noise;
            }

            UpdateNoiseLoop(noise);

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
        }

        private AudioSourceHandle? CreateLoop(string root, string file)
        {
            var path = Path.Combine(root, file);
            if (!File.Exists(path))
                return null;
            return _audio.CreateLoopingSource(path);
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
