using System;
using System.Collections.Generic;
using System.Numerics;
using System.IO;
using TopSpeed.Audio;
using TopSpeed.Core;
using TopSpeed.Data;
using TopSpeed.Tracks.Geometry;
using TS.Audio;

namespace TopSpeed.Tracks
{
    internal sealed class Track : IDisposable
    {
        private const float LaneWidthMeters = 5.0f;
        private const float LegacyLaneWidthMeters = 50.0f;
        private const float CallLengthMeters = 30.0f;
        private const int Types = 9;
        private const int Surfaces = 5;
        private const int Noises = 12;
        private const float MinPartLengthMeters = 50.0f;

        public struct Road
        {
            public float Left;
            public float Right;
            public TrackSurface Surface;
            public TrackType Type;
            public float Length;
        }

        private readonly AudioManager _audio;
        private readonly string _trackName;
        private readonly bool _userDefined;
        private readonly TrackDefinition[] _definition;
        private readonly int _segmentCount;
        private readonly TrackWeather _weather;
        private readonly TrackAmbience _ambience;
        private readonly TrackLayout? _layout;
        private readonly TrackGeometry? _geometry;
        private readonly TrackGeometrySpan[]? _layoutSpans;
        private readonly float[]? _layoutSpanStart;
        private TrackNoise _currentNoise;

        private float _laneWidth;
        private float _curveScale;
        private float _callLength;
        private float _lapDistance;
        private float _lapCenter;
        private int _currentRoad;
        private float _relPos;
        private float _prevRelPos;
        private int _lastCalled;
        private float _factor;
        private float _noiseLength;
        private float _noiseStartPos;
        private float _noiseEndPos;
        private bool _noisePlaying;

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

        private bool IsLayout => _layout != null && _geometry != null && _layoutSpans != null && _layoutSpanStart != null;

        private Track(string trackName, TrackData data, AudioManager audio, bool userDefined)
        {
            _trackName = trackName.Length < 64 ? trackName : string.Empty;      
            _userDefined = userDefined;
            _audio = audio;
            _laneWidth = LaneWidthMeters;
            UpdateCurveScale();
            _callLength = CallLengthMeters;
            _weather = data.Weather;
            _ambience = data.Ambience;
            _definition = data.Definitions;
            _segmentCount = _definition.Length;
            _currentNoise = TrackNoise.NoNoise;

            InitializeSounds();
        }

        private Track(string trackName, TrackLayout layout, TrackGeometry geometry, AudioManager audio, bool userDefined)
        {
            _trackName = trackName.Length < 64 ? trackName : string.Empty;
            _userDefined = userDefined;
            _audio = audio;
            _layout = layout ?? throw new ArgumentNullException(nameof(layout));
            _geometry = geometry ?? throw new ArgumentNullException(nameof(geometry));
            _laneWidth = Math.Max(1f, _layout.DefaultWidthMeters * 0.5f);
            UpdateCurveScale();
            _callLength = CallLengthMeters;
            _weather = _layout.Weather;
            _ambience = _layout.Ambience;
            _definition = Array.Empty<TrackDefinition>();
            _segmentCount = _layout.Geometry.Spans.Count;
            _layoutSpans = new TrackGeometrySpan[_segmentCount];
            _layoutSpanStart = new float[_segmentCount];
            _currentNoise = _layout.DefaultNoise;

            var distance = 0f;
            for (var i = 0; i < _segmentCount; i++)
            {
                _layoutSpans[i] = _layout.Geometry.Spans[i];
                _layoutSpanStart[i] = distance;
                distance += _layoutSpans[i].LengthMeters;
            }

            InitializeSounds();
        }

        public static Track Load(string nameOrPath, AudioManager audio)
        {
            var layoutTrack = TryLoadLayout(nameOrPath, audio);
            if (layoutTrack != null)
                return layoutTrack;

            if (!LooksLikePath(nameOrPath))
                throw new FileNotFoundException("Track layout not found.", nameOrPath);

            var data = ReadCustomTrackData(nameOrPath);
            var displayName = ResolveCustomTrackName(nameOrPath, data.Name);
            return new Track(displayName, data, audio, userDefined: true);
        }

        public static Track LoadFromData(string trackName, TrackData data, AudioManager audio, bool userDefined)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));
            return new Track(trackName, data, audio, userDefined);
        }

        private static Track? TryLoadLayout(string nameOrPath, AudioManager audio)
        {
            var root = Path.Combine(AssetPaths.Root, "Tracks");
            var sources = new ITrackLayoutSource[]
            {
                new FileTrackLayoutSource(new[] { root })
            };
            var loader = new TrackLayoutLoader(sources);
            var request = new TrackLayoutLoadRequest(nameOrPath, validate: true, buildGeometry: true, allowWarnings: true);
            var result = loader.Load(request);
            if (!result.IsSuccess || result.Layout == null || result.Geometry == null)
                return null;

            var trackName = ResolveLayoutTrackName(nameOrPath, result.Layout);
            var userDefined = LooksLikePath(nameOrPath);
            return new Track(trackName, result.Layout, result.Geometry, audio, userDefined);
        }

        private static string ResolveLayoutTrackName(string identifier, TrackLayout layout)
        {
            var name = layout.Metadata?.Name;
            if (!string.IsNullOrWhiteSpace(name))
                return name!;
            if (identifier.IndexOfAny(new[] { '\\', '/' }) >= 0)
            {
                var fileName = Path.GetFileNameWithoutExtension(identifier);
                if (!string.IsNullOrWhiteSpace(fileName))
                    return fileName;
            }
            return string.IsNullOrWhiteSpace(identifier) ? "Track" : identifier;
        }

        private static bool LooksLikePath(string identifier)
        {
            if (string.IsNullOrWhiteSpace(identifier))
                return false;
            if (identifier.IndexOfAny(new[] { '\\', '/' }) >= 0)
                return true;
            return Path.HasExtension(identifier);
        }

        public string TrackName => _trackName;
        public float Length => _lapDistance;
        public int SegmentCount => _segmentCount;
        public float LapDistance => _lapDistance;
        public TrackWeather Weather => _weather;
        public TrackAmbience Ambience => _ambience;
        public bool UserDefined => _userDefined;
        public float LaneWidth => _laneWidth;
        public bool HasGeometry => _geometry != null;
        public TrackSurface InitialSurface => _layout != null
            ? _layout.DefaultSurface
            : (_definition.Length > 0 ? _definition[0].Surface : TrackSurface.Asphalt);

        public TrackPose GetPose(float positionMeters)
        {
            if (_geometry != null)
                return _geometry.GetPose(positionMeters);
            var pos = new Vector3(0f, 0f, positionMeters);
            return new TrackPose(pos, Vector3.UnitZ, Vector3.UnitX, Vector3.UnitY, 0f, 0f);
        }

        public Vector3 GetWorldPosition(float positionMeters, float lateralOffset)
        {
            var pose = GetPose(positionMeters);
            return pose.Position + pose.Right * lateralOffset;
        }

        public void SetLaneWidth(float laneWidth)
        {
            _laneWidth = laneWidth;
            UpdateCurveScale();
        }

        public int Lap(float position)
        {
            if (_lapDistance <= 0)
                return 1;
            return (int)(position / _lapDistance) + 1;
        }


        public void Initialize()
        {
            _lapDistance = 0;
            _lapCenter = 0;
            if (IsLayout)
            {
                _lapDistance = _geometry!.LengthMeters;
                _lapCenter = 0f;
                _currentRoad = 0;
                _relPos = 0f;
                _prevRelPos = 0f;
                _lastCalled = 0;
            }
            else
            {
                for (var i = 0; i < _segmentCount; i++)
                {
                    _lapDistance += _definition[i].Length;
                    switch (_definition[i].Type)
                    {
                        case TrackType.EasyLeft:
                            _lapCenter -= (_definition[i].Length * _curveScale) / 2;
                            break;
                        case TrackType.Left:
                            _lapCenter -= (_definition[i].Length * _curveScale) * 2 / 3;
                            break;
                        case TrackType.HardLeft:
                            _lapCenter -= _definition[i].Length * _curveScale;
                            break;
                        case TrackType.HairpinLeft:
                            _lapCenter -= (_definition[i].Length * _curveScale) * 3 / 2;
                            break;
                        case TrackType.EasyRight:
                            _lapCenter += (_definition[i].Length * _curveScale) / 2;
                            break;
                        case TrackType.Right:
                            _lapCenter += (_definition[i].Length * _curveScale) * 2 / 3;
                            break;
                        case TrackType.HardRight:
                            _lapCenter += _definition[i].Length * _curveScale;
                            break;
                        case TrackType.HairpinRight:
                            _lapCenter += (_definition[i].Length * _curveScale) * 3 / 2;
                            break;
                    }
                }
            }

            if (_weather == TrackWeather.Rain)
                _soundRain?.Play(loop: true);
            else if (_weather == TrackWeather.Wind)
                _soundWind?.Play(loop: true);
            else if (_weather == TrackWeather.Storm)
                _soundStorm?.Play(loop: true);

            if (_ambience == TrackAmbience.Desert)
                _soundDesert?.Play(loop: true);
            else if (_ambience == TrackAmbience.Airport)
                _soundAirport?.Play(loop: true);
        }

        public void FinalizeTrack()
        {
            if (_weather == TrackWeather.Rain)
                _soundRain?.Stop();
            else if (_weather == TrackWeather.Wind)
                _soundWind?.Stop();
            else if (_weather == TrackWeather.Storm)
                _soundStorm?.Stop();

            if (_ambience == TrackAmbience.Desert)
                _soundDesert?.Stop();
            else if (_ambience == TrackAmbience.Airport)
                _soundAirport?.Stop();
        }

        public void Run(float position)
        {
            if (_noisePlaying && position > _noiseEndPos)
                _noisePlaying = false;

            if (IsLayout)
            {
                if (_lapDistance == 0)
                    Initialize();
                if (_lapDistance <= 0f)
                    return;

                var pos = WrapDistance(position);
                var noise = _layout!.NoiseAt(pos);
                if (noise != _currentNoise)
                {
                    _noisePlaying = false;
                    _currentNoise = noise;
                }

                switch (noise)
                {
                    case TrackNoise.Crowd:
                        UpdateLoopingNoiseLayout(_soundCrowd, position, noise);
                        break;
                    case TrackNoise.Ocean:
                        UpdateLoopingNoiseLayout(_soundOcean, position, noise, pan: -10);
                        break;
                    case TrackNoise.Runway:
                        PlayIfNotPlaying(_soundAirplane);
                        break;
                    case TrackNoise.Clock:
                        UpdateLoopingNoiseLayout(_soundClock, position, noise, pan: 25);
                        break;
                    case TrackNoise.Jet:
                        PlayIfNotPlaying(_soundJet);
                        break;
                    case TrackNoise.Thunder:
                        PlayIfNotPlaying(_soundThunder);
                        break;
                    case TrackNoise.Pile:
                        UpdateLoopingNoiseLayout(_soundPile, position, noise);
                        break;
                    case TrackNoise.Construction:
                        UpdateLoopingNoiseLayout(_soundConstruction, position, noise);
                        break;
                    case TrackNoise.River:
                        UpdateLoopingNoiseLayout(_soundRiver, position, noise);
                        break;
                    case TrackNoise.Helicopter:
                        PlayIfNotPlaying(_soundHelicopter);
                        break;
                    case TrackNoise.Owl:
                        PlayIfNotPlaying(_soundOwl);
                        break;
                    default:
                        _soundCrowd?.Stop();
                        _soundOcean?.Stop();
                        _soundClock?.Stop();
                        _soundPile?.Stop();
                        _soundConstruction?.Stop();
                        _soundRiver?.Stop();
                        break;
                }
                return;
            }

            if (_segmentCount == 0)
                return;

            switch (_definition[_currentRoad].Noise)
            {
                case TrackNoise.Crowd:
                    UpdateLoopingNoise(_soundCrowd, position);
                    break;
                case TrackNoise.Ocean:
                    UpdateLoopingNoise(_soundOcean, position, pan: -10);
                    break;
                case TrackNoise.Runway:
                    PlayIfNotPlaying(_soundAirplane);
                    break;
                case TrackNoise.Clock:
                    UpdateLoopingNoise(_soundClock, position, pan: 25);
                    break;
                case TrackNoise.Jet:
                    PlayIfNotPlaying(_soundJet);
                    break;
                case TrackNoise.Thunder:
                    PlayIfNotPlaying(_soundThunder);
                    break;
                case TrackNoise.Pile:
                    UpdateLoopingNoise(_soundPile, position);
                    break;
                case TrackNoise.Construction:
                    UpdateLoopingNoise(_soundConstruction, position);
                    break;
                case TrackNoise.River:
                    UpdateLoopingNoise(_soundRiver, position);
                    break;
                case TrackNoise.Helicopter:
                    PlayIfNotPlaying(_soundHelicopter);
                    break;
                case TrackNoise.Owl:
                    PlayIfNotPlaying(_soundOwl);
                    break;
                default:
                    _soundCrowd?.Stop();
                    _soundOcean?.Stop();
                    _soundClock?.Stop();
                    _soundPile?.Stop();
                    _soundConstruction?.Stop();
                    _soundRiver?.Stop();
                    break;
            }
        }

        public Road RoadAtPosition(float position)
        {
            if (_lapDistance == 0)
                Initialize();

            if (IsLayout)
            {
                return BuildLayoutRoad(position, updateState: true);
            }

            var lap = (int)(position / _lapDistance);
            var pos = WrapDistance(position);
            var dist = 0.0f;
            var center = lap * _lapCenter;

            for (var i = 0; i < _segmentCount; i++)
            {
                if (dist <= pos && dist + _definition[i].Length > pos)
                {
                    _prevRelPos = _relPos;
                    _relPos = pos - dist;
                    _currentRoad = i;
                    var road = new Road
                    {
                        Type = _definition[i].Type,
                        Surface = _definition[i].Surface,
                        Length = _definition[i].Length
                    };

                    ApplyRoadOffset(ref road, center, _relPos, _definition[i].Type);
                    return road;
                }

                center = UpdateCenter(center, _definition[i]);
                dist += _definition[i].Length;
            }

            return new Road { Left = 0, Right = 0, Surface = TrackSurface.Asphalt, Type = TrackType.Straight, Length = MinPartLengthMeters };
        }

        public Road RoadComputer(float position)
        {
            if (_lapDistance == 0)
                Initialize();

            if (IsLayout)
            {
                return BuildLayoutRoad(position, updateState: false);
            }

            var lap = (int)(position / _lapDistance);
            var pos = WrapDistance(position);
            var dist = 0.0f;
            var center = lap * _lapCenter;
            var relPos = 0.0f;

            for (var i = 0; i < _segmentCount; i++)
            {
                if (dist <= pos && dist + _definition[i].Length > pos)
                {
                    relPos = pos - dist;
                    var road = new Road
                    {
                        Type = _definition[i].Type,
                        Surface = _definition[i].Surface,
                        Length = _definition[i].Length
                    };

                    ApplyRoadOffset(ref road, center, relPos, _definition[i].Type);
                    return road;
                }

                center = UpdateCenter(center, _definition[i]);
                dist += _definition[i].Length;
            }

            return new Road { Left = 0, Right = 0, Surface = TrackSurface.Asphalt, Type = TrackType.Straight, Length = MinPartLengthMeters };
        }

        private Road BuildLayoutRoad(float position, bool updateState)
        {
            if (!IsLayout || _layoutSpans == null || _layoutSpanStart == null)
                return new Road { Left = 0, Right = 0, Surface = TrackSurface.Asphalt, Type = TrackType.Straight, Length = MinPartLengthMeters };

            if (_lapDistance <= 0f)
                return new Road { Left = 0, Right = 0, Surface = TrackSurface.Asphalt, Type = TrackType.Straight, Length = MinPartLengthMeters };

            var pos = WrapDistance(position);
            var index = LayoutSpanIndexAt(pos);
            var span = _layoutSpans[index];

            if (updateState)
            {
                _prevRelPos = _relPos;
                _relPos = pos - _layoutSpanStart[index];
                _currentRoad = index;
            }

            var width = Math.Max(0.5f, _layout!.WidthAt(pos));
            var half = width * 0.5f;
            return new Road
            {
                Left = -half,
                Right = half,
                Surface = _layout.SurfaceAt(pos),
                Type = ResolveCurveType(span),
                Length = span.LengthMeters
            };
        }

        private Road BuildLayoutRoadForIndex(int index)
        {
            if (!IsLayout || _layoutSpans == null || _layoutSpanStart == null)
                return new Road { Left = 0, Right = 0, Surface = TrackSurface.Asphalt, Type = TrackType.Straight, Length = MinPartLengthMeters };

            if (index < 0 || index >= _layoutSpans.Length)
                return new Road { Left = 0, Right = 0, Surface = TrackSurface.Asphalt, Type = TrackType.Straight, Length = MinPartLengthMeters };

            var span = _layoutSpans[index];
            var sampleOffset = Math.Min(0.25f, span.LengthMeters * 0.5f);
            var pos = WrapDistance(_layoutSpanStart[index] + sampleOffset);
            var width = Math.Max(0.5f, _layout!.WidthAt(pos));
            var half = width * 0.5f;
            return new Road
            {
                Left = -half,
                Right = half,
                Surface = _layout.SurfaceAt(pos),
                Type = ResolveCurveType(span),
                Length = span.LengthMeters
            };
        }

        public bool NextRoad(float position, float speed, int curveAnnouncementMode, out Road road)
        {
            road = new Road();
            if (_segmentCount == 0)
                return false;

            if (IsLayout)
            {
                return NextLayoutRoad(position, speed, curveAnnouncementMode, out road);
            }

            if (curveAnnouncementMode == 0)
            {
                var currentLength = _definition[_currentRoad].Length;
                if ((_relPos + _callLength > currentLength) && (_prevRelPos + _callLength <= currentLength))
                {
                    var next = _definition[(_currentRoad + 1) % _segmentCount];
                    road.Type = next.Type;
                    road.Surface = next.Surface;
                    road.Length = next.Length;
                    return true;
                }
                return false;
            }

            var lookAhead = _callLength + speed / 2;
            var roadAhead = RoadIndexAt(position + lookAhead);
            if (roadAhead < 0)
                return false;

            var delta = (roadAhead - _lastCalled + _segmentCount) % _segmentCount;
            if (delta > 0 && delta <= _segmentCount / 2)
            {
                var next = _definition[roadAhead];
                road.Type = next.Type;
                road.Surface = next.Surface;
                road.Length = next.Length;
                _lastCalled = roadAhead;
                return true;
            }

            return false;
        }

        private bool NextLayoutRoad(float position, float speed, int curveAnnouncementMode, out Road road)
        {
            road = new Road();
            if (!IsLayout || _layoutSpans == null || _layoutSpanStart == null)
                return false;

            if (_lapDistance == 0)
                Initialize();

            if (_segmentCount == 0)
                return false;

            if (curveAnnouncementMode == 0)
            {
                var currentLength = _layoutSpans[_currentRoad].LengthMeters;
                if ((_relPos + _callLength > currentLength) && (_prevRelPos + _callLength <= currentLength))
                {
                    road = BuildLayoutRoadForIndex((_currentRoad + 1) % _segmentCount);
                    return road.Length > 0f;
                }
                return false;
            }

            var lookAhead = _callLength + speed / 2;
            var roadAhead = LayoutSpanIndexAt(position + lookAhead);
            if (roadAhead < 0)
                return false;

            var delta = (roadAhead - _lastCalled + _segmentCount) % _segmentCount;
            if (delta > 0 && delta <= _segmentCount / 2)
            {
                road = BuildLayoutRoadForIndex(roadAhead);
                _lastCalled = roadAhead;
                return road.Length > 0f;
            }

            return false;
        }

        private int RoadIndexAt(float position)
        {
            if (_lapDistance == 0)
                Initialize();

            var pos = WrapDistance(position);
            var dist = 0.0f;
            for (var i = 0; i < _segmentCount; i++)
            {
                if (dist <= pos && dist + _definition[i].Length > pos)
                    return i;
                dist += _definition[i].Length;
            }
            return -1;
        }

        private int LayoutSpanIndexAt(float position)
        {
            if (_layoutSpanStart == null || _layoutSpanStart.Length == 0)
                return -1;

            var pos = WrapDistance(position);
            var index = Array.BinarySearch(_layoutSpanStart, pos);
            if (index >= 0)
                return index;
            index = ~index - 1;
            if (index < 0)
                index = 0;
            if (index >= _layoutSpanStart.Length)
                index = _layoutSpanStart.Length - 1;
            return index;
        }

        private TrackType ResolveCurveType(TrackGeometrySpan span)
        {
            if (span.Kind != TrackGeometrySpanKind.Arc || span.Direction == TrackCurveDirection.Straight)
                return TrackType.Straight;
            if (!span.CurveSeverity.HasValue)
                return TrackType.Straight;

            return span.Direction switch
            {
                TrackCurveDirection.Left => span.CurveSeverity.Value switch
                {
                    TrackCurveSeverity.Easy => TrackType.EasyLeft,
                    TrackCurveSeverity.Normal => TrackType.Left,
                    TrackCurveSeverity.Hard => TrackType.HardLeft,
                    TrackCurveSeverity.Hairpin => TrackType.HairpinLeft,
                    _ => TrackType.Left
                },
                TrackCurveDirection.Right => span.CurveSeverity.Value switch
                {
                    TrackCurveSeverity.Easy => TrackType.EasyRight,
                    TrackCurveSeverity.Normal => TrackType.Right,
                    TrackCurveSeverity.Hard => TrackType.HardRight,
                    TrackCurveSeverity.Hairpin => TrackType.HairpinRight,
                    _ => TrackType.Right
                },
                _ => TrackType.Straight
            };
        }

        private float WrapDistance(float position)
        {
            if (_lapDistance <= 0f)
                return 0f;
            var wrapped = position % _lapDistance;
            if (wrapped < 0f)
                wrapped += _lapDistance;
            if (wrapped == _lapDistance)
                return 0f;
            return wrapped;
        }

        private void CalculateNoiseLength()
        {
            _noiseLength = 0;
            var i = _currentRoad;
            while (i < _segmentCount && _definition[i].Noise == _definition[_currentRoad].Noise)
            {
                _noiseLength += _definition[i].Length;
                i++;
            }
            _noisePlaying = true;
        }

        private void CalculateNoiseLengthLayout(float position, TrackNoise noise)
        {
            _noiseLength = 0f;
            if (_layout == null || _lapDistance <= 0f)
            {
                _noisePlaying = false;
                return;
            }

            var pos = WrapDistance(position);
            var start = 0f;
            var end = _lapDistance;
            var found = false;
            for (var i = 0; i < _layout.NoiseZones.Count; i++)
            {
                var zone = _layout.NoiseZones[i];
                if (zone.Value == noise && zone.Contains(pos))
                {
                    start = zone.StartMeters;
                    end = zone.EndMeters;
                    found = true;
                    break;
                }
            }

            if (!found)
            {
                if (noise == _layout.DefaultNoise)
                {
                    start = 0f;
                    end = _lapDistance;
                    found = true;
                }
                else
                {
                    _noisePlaying = false;
                    return;
                }
            }

            _noiseLength = Math.Max(0f, end - start);
            if (_noiseLength <= 0f)
            {
                _noisePlaying = false;
                return;
            }

            var offset = pos - start;
            _noiseStartPos = position - offset;
            _noiseEndPos = _noiseStartPos + _noiseLength;
            _noisePlaying = true;
        }

        private void UpdateLoopingNoise(AudioSourceHandle? sound, float position, int? pan = null)
        {
            if (sound == null)
                return;

            if (!_noisePlaying)
            {
                CalculateNoiseLength();
                _noiseStartPos = position;
                _noiseEndPos = position + _noiseLength;
            }

            _factor = (position - _noiseStartPos) * 1.0f / _noiseLength;
            if (_factor < 0.5f)
                _factor *= 2.0f;
            else
                _factor = 2.0f * (1.0f - _factor);

            SetVolumePercent(sound, (int)(80.0f + _factor * 20.0f));
            if (!sound.IsPlaying)
            {
                if (pan.HasValue)
                    sound.SetPan(pan.Value / 100f);
                sound.Play(loop: true);
            }
        }

        private void UpdateLoopingNoiseLayout(AudioSourceHandle? sound, float position, TrackNoise noise, int? pan = null)
        {
            if (sound == null)
                return;

            if (!_noisePlaying)
            {
                CalculateNoiseLengthLayout(position, noise);
                if (_noiseLength <= 0f)
                    return;
            }

            _factor = (position - _noiseStartPos) * 1.0f / _noiseLength;
            if (_factor < 0.5f)
                _factor *= 2.0f;
            else
                _factor = 2.0f * (1.0f - _factor);

            SetVolumePercent(sound, (int)(80.0f + _factor * 20.0f));
            if (!sound.IsPlaying)
            {
                if (pan.HasValue)
                    sound.SetPan(pan.Value / 100f);
                sound.Play(loop: true);
            }
        }

        private static void PlayIfNotPlaying(AudioSourceHandle? sound)
        {
            if (sound == null)
                return;
            if (!sound.IsPlaying)
                sound.Play(loop: false);
        }

        private static void SetVolumePercent(AudioSourceHandle sound, int volume)
        {
            var clamped = Math.Max(0, Math.Min(100, volume));
            sound.SetVolume(clamped / 100f);
        }

        private void InitializeSounds()
        {
            var root = Path.Combine(AssetPaths.SoundsRoot, "Legacy");
            _soundCrowd = CreateLegacySound(root, "crowd.wav");
            _soundOcean = CreateLegacySound(root, "ocean.wav");
            _soundRain = CreateLegacySound(root, "rain.wav");
            _soundWind = CreateLegacySound(root, "wind.wav");
            _soundStorm = CreateLegacySound(root, "storm.wav");
            _soundDesert = CreateLegacySound(root, "desert.wav");
            _soundAirport = CreateLegacySound(root, "airport.wav");
            _soundAirplane = CreateLegacySound(root, "airplane.wav");
            _soundClock = CreateLegacySound(root, "clock.wav");
            _soundJet = CreateLegacySound(root, "jet.wav");
            _soundThunder = CreateLegacySound(root, "thunder.wav");
            _soundPile = CreateLegacySound(root, "pile.wav");
            _soundConstruction = CreateLegacySound(root, "const.wav");
            _soundRiver = CreateLegacySound(root, "river.wav");
            _soundHelicopter = CreateLegacySound(root, "helicopter.wav");
            _soundOwl = CreateLegacySound(root, "owl.wav");
        }

        private AudioSourceHandle? CreateLegacySound(string root, string file)
        {
            var path = Path.Combine(root, file);
            if (!File.Exists(path))
                return null;
            return _audio.CreateLoopingSource(path);
        }

        private float UpdateCenter(float center, TrackDefinition definition)    
        {
            switch (definition.Type)
            {
                case TrackType.EasyLeft:
                    return center - (definition.Length * _curveScale) / 2;
                case TrackType.Left:
                    return center - (definition.Length * _curveScale) * 2 / 3;
                case TrackType.HardLeft:
                    return center - definition.Length * _curveScale;
                case TrackType.HairpinLeft:
                    return center - (definition.Length * _curveScale) * 3 / 2;
                case TrackType.EasyRight:
                    return center + (definition.Length * _curveScale) / 2;
                case TrackType.Right:
                    return center + (definition.Length * _curveScale) * 2 / 3;
                case TrackType.HardRight:
                    return center + definition.Length * _curveScale;
                case TrackType.HairpinRight:
                    return center + (definition.Length * _curveScale) * 3 / 2;
                default:
                    return center;
            }
        }

        private void ApplyRoadOffset(ref Road road, float center, float relPos, TrackType type)
        {
            var offset = relPos * _curveScale;
            switch (type)
            {
                case TrackType.Straight:
                    road.Left = center - _laneWidth;
                    road.Right = center + _laneWidth;
                    break;
                case TrackType.EasyLeft:
                    road.Left = center - _laneWidth - offset / 2;
                    road.Right = center + _laneWidth - offset / 2;
                    break;
                case TrackType.Left:
                    road.Left = center - _laneWidth - offset * 2 / 3;
                    road.Right = center + _laneWidth - offset * 2 / 3;
                    break;
                case TrackType.HardLeft:
                    road.Left = center - _laneWidth - offset;
                    road.Right = center + _laneWidth - offset;
                    break;
                case TrackType.HairpinLeft:
                    road.Left = center - _laneWidth - offset * 3 / 2;
                    road.Right = center + _laneWidth - offset * 3 / 2;
                    break;
                case TrackType.EasyRight:
                    road.Left = center - _laneWidth + offset / 2;
                    road.Right = center + _laneWidth + offset / 2;
                    break;
                case TrackType.Right:
                    road.Left = center - _laneWidth + offset * 2 / 3;
                    road.Right = center + _laneWidth + offset * 2 / 3;
                    break;
                case TrackType.HardRight:
                    road.Left = center - _laneWidth + offset;
                    road.Right = center + _laneWidth + offset;
                    break;
                case TrackType.HairpinRight:
                    road.Left = center - _laneWidth + offset * 3 / 2;
                    road.Right = center + _laneWidth + offset * 3 / 2;
                    break;
                default:
                    road.Left = center - _laneWidth;
                    road.Right = center + _laneWidth;
                    break;
            }
        }

        private void UpdateCurveScale()
        {
            _curveScale = LegacyLaneWidthMeters > 0f ? _laneWidth / LegacyLaneWidthMeters : 1.0f;
            if (_curveScale <= 0f)
                _curveScale = 0.01f;
        }

        private static string ResolveCustomTrackName(string path, string? name)
        {
            var trimmedName = name?.Trim();
            if (!string.IsNullOrWhiteSpace(trimmedName))
                return trimmedName!;
            var fileName = Path.GetFileNameWithoutExtension(path);
            return string.IsNullOrWhiteSpace(fileName) ? path : fileName;
        }

        private static bool TryParseCustomTrackName(string line, out string name)
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

        private static TrackData ReadCustomTrackData(string filename)
        {
            if (!File.Exists(filename))
            {
                return new TrackData(true, TrackWeather.Sunny, TrackAmbience.NoAmbience,
                    new[] { new TrackDefinition(TrackType.Straight, TrackSurface.Asphalt, TrackNoise.NoNoise, MinPartLengthMeters) });
            }

            var ints = new List<int>();
            string? name = null;
            foreach (var line in File.ReadLines(filename))
            {
                var trimmed = line.Trim();
                if (TryParseCustomTrackName(trimmed, out var parsedName))
                {
                    if (string.IsNullOrWhiteSpace(name))
                        name = parsedName;
                    continue;
                }

                AppendIntsFromLine(trimmed, ints);
            }

            var length = 0;
            var index = 0;
            var minPartLengthLegacy = 5000;

            while (index < ints.Count)
            {
                var first = ints[index++];
                if (first < 0)
                    break;
                if (index < ints.Count) index++;
                if (index >= ints.Count) break;
                var third = ints[index++];
                if (third < minPartLengthLegacy && index < ints.Count)
                    index++;
                length++;
            }

            if (length == 0)
            {
                return new TrackData(true, TrackWeather.Sunny, TrackAmbience.NoAmbience,
                    new[] { new TrackDefinition(TrackType.Straight, TrackSurface.Asphalt, TrackNoise.NoNoise, MinPartLengthMeters) },
                    name: name);
            }

            var definitions = new TrackDefinition[length];
            index = 0;
            for (var i = 0; i < length; i++)
            {
                var typeValue = index < ints.Count ? ints[index++] : 0;
                var surfaceValue = index < ints.Count ? ints[index++] : 0;
                var temp = index < ints.Count ? ints[index++] : 0;

                var noiseValue = 0;
                var lengthValueLegacy = 0;
                if (temp < Noises)
                {
                    noiseValue = temp;
                    lengthValueLegacy = index < ints.Count ? ints[index++] : minPartLengthLegacy;
                }
                else
                {
                    if (typeValue >= Types)
                    {
                        noiseValue = (typeValue - Types) + 1;
                        typeValue = 0;
                    }
                    else
                    {
                        noiseValue = 0;
                    }
                    lengthValueLegacy = temp;
                }

                if (typeValue >= Types)
                    typeValue = 0;
                if (surfaceValue >= Surfaces)
                    surfaceValue = 0;
                if (noiseValue >= Noises)
                    noiseValue = 0;
                if (lengthValueLegacy < minPartLengthLegacy)
                    lengthValueLegacy = minPartLengthLegacy;

                definitions[i] = new TrackDefinition((TrackType)typeValue, (TrackSurface)surfaceValue, (TrackNoise)noiseValue, lengthValueLegacy / 100.0f);
            }

            if (index < ints.Count)
                index++; // skip -1

            var weatherValue = index < ints.Count ? ints[index++] : 0;
            if (weatherValue < 0)
                weatherValue = 0;
            var ambienceValue = index < ints.Count ? ints[index++] : 0;
            if (ambienceValue < 0)
                ambienceValue = 0;

            var weather = (TrackWeather)weatherValue;
            var ambience = (TrackAmbience)ambienceValue;
            return new TrackData(true, weather, ambience, definitions, name: name);
        }

        private static int AppendIntsFromLine(string line, List<int> values)
        {
            if (string.IsNullOrWhiteSpace(line))
                return 0;

            var trimmed = line.Trim();
            if (trimmed.StartsWith("#", StringComparison.Ordinal) ||
                trimmed.StartsWith(";", StringComparison.Ordinal))
            {
                return 0;
            }

            var added = 0;
            var parts = trimmed.Split((char[])null!, StringSplitOptions.RemoveEmptyEntries);
            foreach (var part in parts)
            {
                if (int.TryParse(part, out var value))
                {
                    values.Add(value);
                    added++;
                }
            }

            return added;
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

        private static void DisposeSound(AudioSourceHandle? sound)
        {
            if (sound == null)
                return;
            sound.Stop();
            sound.Dispose();
        }
    }
}
