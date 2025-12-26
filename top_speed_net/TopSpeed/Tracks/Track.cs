using System;
using System.Collections.Generic;
using System.IO;
using TopSpeed.Audio;
using TopSpeed.Core;
using TopSpeed.Data;
using TS.Audio;

namespace TopSpeed.Tracks
{
    internal sealed class Track : IDisposable
    {
        private const int LaneWidthUnits = 15000;
        private const int CallLength = 3000;
        private const int Types = 9;
        private const int Surfaces = 5;
        private const int Noises = 12;
        private const int MinPartLength = 5000;

        public struct Road
        {
            public int Left;
            public int Right;
            public TrackSurface Surface;
            public TrackType Type;
            public int Length;
        }

        private readonly AudioManager _audio;
        private readonly string _trackName;
        private readonly bool _userDefined;
        private readonly TrackDefinition[] _definition;
        private readonly int _length;
        private readonly TrackWeather _weather;
        private readonly TrackAmbience _ambience;

        private int _laneWidth;
        private int _callLength;
        private int _lapDistance;
        private int _lapCenter;
        private int _currentRoad;
        private int _relPos;
        private int _prevRelPos;
        private int _lastCalled;
        private float _factor;
        private int _noiseLength;
        private int _noiseStartPos;
        private int _noiseEndPos;
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

        private Track(string trackName, TrackData data, AudioManager audio, bool userDefined)
        {
            _trackName = trackName.Length < 64 ? trackName : string.Empty;
            _userDefined = userDefined;
            _audio = audio;
            _laneWidth = LaneWidthUnits;
            _callLength = CallLength;
            _weather = data.Weather;
            _ambience = data.Ambience;
            _definition = data.Definitions;
            _length = _definition.Length;

            InitializeSounds();
        }

        public static Track Load(string nameOrPath, AudioManager audio)
        {
            if (TrackCatalog.BuiltIn.TryGetValue(nameOrPath, out var builtIn))
            {
                return new Track(nameOrPath, builtIn, audio, userDefined: false);
            }

            var data = ReadCustomTrackData(nameOrPath);
            return new Track(nameOrPath, data, audio, userDefined: true);
        }

        public static Track LoadFromData(string trackName, TrackData data, AudioManager audio, bool userDefined)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));
            return new Track(trackName, data, audio, userDefined);
        }

        public string TrackName => _trackName;
        public int Length => _lapDistance;
        public int SegmentCount => _length;
        public int LapDistance => _lapDistance;
        public TrackWeather Weather => _weather;
        public TrackAmbience Ambience => _ambience;
        public bool UserDefined => _userDefined;
        public int LaneWidth => _laneWidth;
        public TrackSurface InitialSurface => _definition.Length > 0 ? _definition[0].Surface : TrackSurface.Asphalt;

        public void SetLaneWidth(int laneWidth)
        {
            _laneWidth = laneWidth;
        }

        public int Lap(int position)
        {
            if (_lapDistance <= 0)
                return 1;
            return (position / _lapDistance) + 1;
        }


        public void Initialize()
        {
            _lapDistance = 0;
            _lapCenter = 0;
            for (var i = 0; i < _length; i++)
            {
                _lapDistance += _definition[i].Length;
                switch (_definition[i].Type)
                {
                    case TrackType.EasyLeft:
                        _lapCenter -= _definition[i].Length / 2;
                        break;
                    case TrackType.Left:
                        _lapCenter -= _definition[i].Length * 2 / 3;
                        break;
                    case TrackType.HardLeft:
                        _lapCenter -= _definition[i].Length;
                        break;
                    case TrackType.HairpinLeft:
                        _lapCenter -= _definition[i].Length * 3 / 2;
                        break;
                    case TrackType.EasyRight:
                        _lapCenter += _definition[i].Length / 2;
                        break;
                    case TrackType.Right:
                        _lapCenter += _definition[i].Length * 2 / 3;
                        break;
                    case TrackType.HardRight:
                        _lapCenter += _definition[i].Length;
                        break;
                    case TrackType.HairpinRight:
                        _lapCenter += _definition[i].Length * 3 / 2;
                        break;
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

        public void Run(int position)
        {
            if (_noisePlaying && position > _noiseEndPos)
                _noisePlaying = false;

            if (_length == 0)
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

        public Road RoadAtPosition(int position)
        {
            if (_lapDistance == 0)
                Initialize();

            var lap = position / _lapDistance;
            var pos = position % _lapDistance;
            var dist = 0;
            var center = lap * _lapCenter;

            for (var i = 0; i < _length; i++)
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

            return new Road { Left = 0, Right = 0, Surface = TrackSurface.Asphalt, Type = TrackType.Straight, Length = MinPartLength };
        }

        public Road RoadComputer(int position)
        {
            if (_lapDistance == 0)
                Initialize();

            var lap = position / _lapDistance;
            var pos = position % _lapDistance;
            var dist = 0;
            var center = lap * _lapCenter;
            var relPos = 0;

            for (var i = 0; i < _length; i++)
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

            return new Road { Left = 0, Right = 0, Surface = TrackSurface.Asphalt, Type = TrackType.Straight, Length = MinPartLength };
        }

        public bool NextRoad(int position, int speed, int curveAnnouncementMode, out Road road)
        {
            road = new Road();
            if (_length == 0)
                return false;

            if (curveAnnouncementMode == 0)
            {
                var currentLength = _definition[_currentRoad].Length;
                if ((_relPos + _callLength > currentLength) && (_prevRelPos + _callLength <= currentLength))
                {
                    var next = _definition[(_currentRoad + 1) % _length];
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

            var delta = (roadAhead - _lastCalled + _length) % _length;
            if (delta > 0 && delta <= _length / 2)
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

        private int RoadIndexAt(int position)
        {
            if (_lapDistance == 0)
                Initialize();

            var pos = position % _lapDistance;
            var dist = 0;
            for (var i = 0; i < _length; i++)
            {
                if (dist <= pos && dist + _definition[i].Length > pos)
                    return i;
                dist += _definition[i].Length;
            }
            return -1;
        }

        private void CalculateNoiseLength()
        {
            _noiseLength = 0;
            var i = _currentRoad;
            while (i < _length && _definition[i].Noise == _definition[_currentRoad].Noise)
            {
                _noiseLength += _definition[i].Length;
                i++;
            }
            _noisePlaying = true;
        }

        private void UpdateLoopingNoise(AudioSourceHandle? sound, int position, int? pan = null)
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

        private int UpdateCenter(int center, TrackDefinition definition)
        {
            switch (definition.Type)
            {
                case TrackType.EasyLeft:
                    return center - definition.Length / 2;
                case TrackType.Left:
                    return center - definition.Length * 2 / 3;
                case TrackType.HardLeft:
                    return center - definition.Length;
                case TrackType.HairpinLeft:
                    return center - definition.Length * 3 / 2;
                case TrackType.EasyRight:
                    return center + definition.Length / 2;
                case TrackType.Right:
                    return center + definition.Length * 2 / 3;
                case TrackType.HardRight:
                    return center + definition.Length;
                case TrackType.HairpinRight:
                    return center + definition.Length * 3 / 2;
                default:
                    return center;
            }
        }

        private void ApplyRoadOffset(ref Road road, int center, int relPos, TrackType type)
        {
            switch (type)
            {
                case TrackType.Straight:
                    road.Left = center - _laneWidth;
                    road.Right = center + _laneWidth;
                    break;
                case TrackType.EasyLeft:
                    road.Left = center - _laneWidth - relPos / 2;
                    road.Right = center + _laneWidth - relPos / 2;
                    break;
                case TrackType.Left:
                    road.Left = center - _laneWidth - relPos * 2 / 3;
                    road.Right = center + _laneWidth - relPos * 2 / 3;
                    break;
                case TrackType.HardLeft:
                    road.Left = center - _laneWidth - relPos;
                    road.Right = center + _laneWidth - relPos;
                    break;
                case TrackType.HairpinLeft:
                    road.Left = center - _laneWidth - relPos * 3 / 2;
                    road.Right = center + _laneWidth - relPos * 3 / 2;
                    break;
                case TrackType.EasyRight:
                    road.Left = center - _laneWidth + relPos / 2;
                    road.Right = center + _laneWidth + relPos / 2;
                    break;
                case TrackType.Right:
                    road.Left = center - _laneWidth + relPos * 2 / 3;
                    road.Right = center + _laneWidth + relPos * 2 / 3;
                    break;
                case TrackType.HardRight:
                    road.Left = center - _laneWidth + relPos;
                    road.Right = center + _laneWidth + relPos;
                    break;
                case TrackType.HairpinRight:
                    road.Left = center - _laneWidth + relPos * 3 / 2;
                    road.Right = center + _laneWidth + relPos * 3 / 2;
                    break;
                default:
                    road.Left = center - _laneWidth;
                    road.Right = center + _laneWidth;
                    break;
            }
        }

        private static TrackData ReadCustomTrackData(string filename)
        {
            if (!File.Exists(filename))
            {
                return new TrackData(true, TrackWeather.Sunny, TrackAmbience.NoAmbience,
                    new[] { new TrackDefinition(TrackType.Straight, TrackSurface.Asphalt, TrackNoise.NoNoise, MinPartLength) });
            }

            var ints = new List<int>();
            foreach (var line in File.ReadLines(filename))
            {
                if (int.TryParse(line.Trim(), out var value))
                    ints.Add(value);
            }

            var length = 0;
            var index = 0;
            while (index < ints.Count)
            {
                var first = ints[index++];
                if (first < 0)
                    break;
                if (index < ints.Count) index++;
                if (index >= ints.Count) break;
                var third = ints[index++];
                if (third < MinPartLength && index < ints.Count)
                    index++;
                length++;
            }

            if (length == 0)
            {
                return new TrackData(true, TrackWeather.Sunny, TrackAmbience.NoAmbience,
                    new[] { new TrackDefinition(TrackType.Straight, TrackSurface.Asphalt, TrackNoise.NoNoise, MinPartLength) });
            }

            var definitions = new TrackDefinition[length];
            index = 0;
            for (var i = 0; i < length; i++)
            {
                var typeValue = index < ints.Count ? ints[index++] : 0;
                var surfaceValue = index < ints.Count ? ints[index++] : 0;
                var temp = index < ints.Count ? ints[index++] : 0;

                var noiseValue = 0;
                var lengthValue = 0;
                if (temp < Noises)
                {
                    noiseValue = temp;
                    lengthValue = index < ints.Count ? ints[index++] : MinPartLength;
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
                    lengthValue = temp;
                }

                if (typeValue >= Types)
                    typeValue = 0;
                if (surfaceValue >= Surfaces)
                    surfaceValue = 0;
                if (noiseValue >= Noises)
                    noiseValue = 0;
                if (lengthValue < MinPartLength)
                    lengthValue = MinPartLength;

                definitions[i] = new TrackDefinition((TrackType)typeValue, (TrackSurface)surfaceValue, (TrackNoise)noiseValue, lengthValue);
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
            return new TrackData(true, weather, ambience, definitions);
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
