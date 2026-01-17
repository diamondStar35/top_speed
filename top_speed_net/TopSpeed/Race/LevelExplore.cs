using System;
using System.IO;
using System.Numerics;
using SharpDX.DirectInput;
using TopSpeed.Audio;
using TopSpeed.Core;
using TopSpeed.Data;
using TopSpeed.Input;
using TopSpeed.Speech;
using TopSpeed.Tracks.Map;

namespace TopSpeed.Race
{
    internal sealed class LevelExplore : IDisposable
    {
        private static readonly float[] StepSizes = { 1f, 5f, 10f, 20f, 30f, 50f, 100f };
        private const float WidthAnnounceThreshold = 0.5f;

        private readonly AudioManager _audio;
        private readonly SpeechService _speech;
        private readonly RaceSettings _settings;
        private readonly InputManager _input;
        private readonly TrackMap _map;

        private Vector3 _worldPosition;
        private int _stepIndex;
        private bool _initialized;
        private bool _exitRequested;
        private Vector3 _listenerForward = Vector3.UnitZ;
        private MapDirection _mapHeading = MapDirection.North;
        private MapMovementState _mapState;
        private MapSnapshot _mapSnapshot;

        private Vector3 _lastListenerPosition;
        private bool _listenerInitialized;

        public LevelExplore(
            AudioManager audio,
            SpeechService speech,
            RaceSettings settings,
            InputManager input,
            string track)
        {
            _audio = audio ?? throw new ArgumentNullException(nameof(audio));
            _speech = speech ?? throw new ArgumentNullException(nameof(speech));
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _input = input ?? throw new ArgumentNullException(nameof(input));

            if (!TrackMapLoader.TryResolvePath(track, out var mapPath))
                throw new FileNotFoundException("Track map not found.", track);

            _map = TrackMapLoader.Load(mapPath);
            _stepIndex = 1; // default 5 meters
        }

        public bool WantsExit => _exitRequested;

        public void Initialize()
        {
            _mapState = MapMovement.CreateStart(_map);
            _mapHeading = _mapState.Heading;
            _worldPosition = _mapState.WorldPosition;
            _mapSnapshot = BuildMapSnapshot(_mapState.CellX, _mapState.CellZ);
            _speech.Speak($"Track {FormatTrackName(_map.Name)}.");
            _speech.Speak($"Step {StepSizes[_stepIndex]:0.#} meters.");
            _initialized = true;
        }

        public void Run(float elapsed)
        {
            if (!_initialized)
                return;

            if (_input.WasPressed(Key.Escape))
                _exitRequested = true;

            HandleStepAdjust();
            HandleCoordinateKeys();
            HandleMovement();
            UpdateAudioListener(elapsed);
        }

        public void Dispose()
        {
        }

        private void HandleStepAdjust()
        {
            if (!_input.WasPressed(Key.Back))
                return;

            var shift = _input.IsDown(Key.LeftShift) || _input.IsDown(Key.RightShift);
            if (shift)
            {
                if (_stepIndex > 0)
                    _stepIndex--;
            }
            else
            {
                if (_stepIndex < StepSizes.Length - 1)
                    _stepIndex++;
            }

            _speech.Speak($"Step {StepSizes[_stepIndex]:0.#} meters.");
        }

        private void HandleCoordinateKeys()
        {
            if (_input.WasPressed(Key.K))
                _speech.Speak($"X {Math.Round(_worldPosition.X, 2):0.##} meters.");
            if (_input.WasPressed(Key.L))
                _speech.Speak($"Z {Math.Round(_worldPosition.Z, 2):0.##} meters.");
        }

        private void HandleMovement()
        {
            if (_input.WasPressed(Key.Up))
            {
                AttemptMoveMap(StepSizes[_stepIndex], MapDirection.North);
                return;
            }

            if (_input.WasPressed(Key.Down))
            {
                AttemptMoveMap(StepSizes[_stepIndex], MapDirection.South);
                return;
            }

            if (_input.WasPressed(Key.Left))
            {
                AttemptMoveMap(StepSizes[_stepIndex], MapDirection.West);
                return;
            }

            if (_input.WasPressed(Key.Right))
            {
                AttemptMoveMap(StepSizes[_stepIndex], MapDirection.East);
            }
        }

        private void AttemptMoveMap(float distanceMeters, MapDirection direction)
        {
            var moved = MapMovement.TryMove(_map, ref _mapState, distanceMeters, direction, out _, out var boundaryHit);
            if (boundaryHit)
                _speech.Speak("Track boundary.");
            if (!moved)
                return;

            _worldPosition = _mapState.WorldPosition;
            _mapHeading = direction;
            _mapState.Heading = direction;
            _listenerForward = MapMovement.DirectionVector(direction);

            var previous = _mapSnapshot;
            var current = BuildMapSnapshot(_mapState.CellX, _mapState.CellZ);
            AnnounceMapChanges(previous, current);
            _mapSnapshot = current;
        }

        private void UpdateAudioListener(float elapsed)
        {
            var forward = _listenerForward.LengthSquared() > 0.0001f ? Vector3.Normalize(_listenerForward) : Vector3.UnitZ;
            var up = Vector3.UnitY;
            var velocity = Vector3.Zero;
            if (_listenerInitialized && elapsed > 0f)
                velocity = (_worldPosition - _lastListenerPosition) / elapsed;

            _lastListenerPosition = _worldPosition;
            _listenerInitialized = true;

            var position = AudioWorld.ToMeters(_worldPosition);
            var velocityMeters = AudioWorld.ToMeters(velocity);
            _audio.UpdateListener(position, forward, up, velocityMeters);
        }

        private MapSnapshot BuildMapSnapshot(int x, int z)
        {
            if (!_map.TryGetCell(x, z, out var cell))
            {
                return new MapSnapshot
                {
                    Surface = TrackSurface.Asphalt,
                    Noise = TrackNoise.NoNoise,
                    WidthMeters = 0f,
                    IsSafeZone = false,
                    Zone = string.Empty,
                    Exits = MapExits.None
                };
            }

            return new MapSnapshot
            {
                Surface = cell.Surface,
                Noise = cell.Noise,
                WidthMeters = cell.WidthMeters,
                IsSafeZone = cell.IsSafeZone,
                Zone = cell.Zone ?? string.Empty,
                Exits = cell.Exits
            };
        }

        private void AnnounceMapChanges(MapSnapshot previous, MapSnapshot current)
        {
            if (previous.Surface != current.Surface)
                _speech.Speak($"{FormatSurface(current.Surface)} surface.");

            if (previous.Noise != current.Noise)
                _speech.Speak($"{FormatNoise(current.Noise)} zone.");

            if (Math.Abs(previous.WidthMeters - current.WidthMeters) >= WidthAnnounceThreshold)
                _speech.Speak($"Width {Math.Round(current.WidthMeters, 1):0.#} meters.");

            if (previous.IsSafeZone != current.IsSafeZone)
            {
                if (current.IsSafeZone)
                    _speech.Speak("Safe zone.");
                else
                    _speech.Speak("Leaving safe zone.");
            }

            if (!string.Equals(previous.Zone, current.Zone, StringComparison.OrdinalIgnoreCase))
            {
                if (!string.IsNullOrWhiteSpace(current.Zone))
                    _speech.Speak($"{current.Zone}.");
                else if (!string.IsNullOrWhiteSpace(previous.Zone))
                    _speech.Speak("Leaving zone.");
            }

            var previousCurve = DescribeCurve(previous.Exits, _mapHeading);
            var currentCurve = DescribeCurve(current.Exits, _mapHeading);
            if (!string.Equals(previousCurve, currentCurve, StringComparison.OrdinalIgnoreCase))
                _speech.Speak(currentCurve);

            var wasIntersection = IsIntersection(previous.Exits);
            var isIntersection = IsIntersection(current.Exits);
            if (wasIntersection != isIntersection)
            {
                if (isIntersection)
                    _speech.Speak("Intersection.");
                else
                    _speech.Speak("Leaving intersection.");
            }
        }

        private static bool IsIntersection(MapExits exits)
        {
            return CountExits(exits) >= 3;
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

        private static string DescribeCurve(MapExits exits, MapDirection heading)
        {
            if (exits == MapExits.None)
                return "Off track.";

            var count = CountExits(exits);
            if (count >= 3)
                return "Straight.";

            if (count == 2)
            {
                var straight = exits == (MapExits.North | MapExits.South) || exits == (MapExits.East | MapExits.West);
                if (straight)
                    return "Straight.";

                var right = IsRightTurn(exits, heading);
                return right ? "Right curve." : "Left curve.";
            }

            return "Dead end.";
        }

        private static bool IsRightTurn(MapExits exits, MapDirection heading)
        {
            return heading switch
            {
                MapDirection.North => (exits & MapExits.East) != 0,
                MapDirection.East => (exits & MapExits.South) != 0,
                MapDirection.South => (exits & MapExits.West) != 0,
                MapDirection.West => (exits & MapExits.North) != 0,
                _ => false
            };
        }

        private static string FormatTrackName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return "Track";
            return name.Replace('_', ' ').Replace('-', ' ').Trim();
        }

        private static string FormatSurface(TrackSurface surface)
        {
            return surface switch
            {
                TrackSurface.Asphalt => "Asphalt",
                TrackSurface.Gravel => "Gravel",
                TrackSurface.Water => "Water",
                TrackSurface.Sand => "Sand",
                TrackSurface.Snow => "Snow",
                _ => "Surface"
            };
        }

        private static string FormatNoise(TrackNoise noise)
        {
            return noise switch
            {
                TrackNoise.Crowd => "Crowd",
                TrackNoise.Ocean => "Ocean",
                TrackNoise.Trackside => "Trackside",
                TrackNoise.Clock => "Clock",
                TrackNoise.Jet => "Jet",
                TrackNoise.Thunder => "Thunder",
                TrackNoise.Pile => "Construction",
                TrackNoise.Construction => "Construction",
                TrackNoise.River => "River",
                TrackNoise.Helicopter => "Helicopter",
                TrackNoise.Owl => "Owl",
                _ => "Quiet"
            };
        }


        private struct MapSnapshot
        {
            public TrackSurface Surface;
            public TrackNoise Noise;
            public float WidthMeters;
            public bool IsSafeZone;
            public string Zone;
            public MapExits Exits;
        }
    }
}
