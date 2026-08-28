using System;
using System.Collections.Generic;
using TopSpeed.Audio;
using TopSpeed.Core;
using TopSpeed.Data;
using TS.Audio;

namespace TopSpeed.Tracks
{
    internal sealed partial class Track : IDisposable
    {
        private const float LaneWidthMeters = 5.0f;
        private const float LegacyLaneWidthMeters = 50.0f;
        private const float CallLengthMeters = 30.0f;
        private const float MinPartLengthMeters = 50.0f;

        public struct Road
        {
            public float Left;
            public float Right;
            public TrackSurface Surface;
            public TrackType Type;
            public float Length;

            /// <summary>
            /// Signed lateral drift of the corridor per meter travelled forwards.
            /// See <see cref="RoadModel.CenterDriftPerMeter(TrackType, float)"/>.
            /// </summary>
            public float DriftPerMeter;

            /// <summary>Distance from this point to the end of its segment.</summary>
            public float SegmentRemainingM;
        }

        private readonly AudioManager _audio;
        private readonly string _trackName;
        private readonly bool _userDefined;
        private readonly TrackDefinition[] _definition;
        private readonly int _segmentCount;
        private readonly bool _hasPitArea;
        private readonly TrackWeather _defaultWeatherKind;
        private readonly TrackAmbience _ambience;
        private readonly string _defaultWeatherProfileId;
        private readonly IReadOnlyDictionary<string, TrackWeatherProfile> _weatherProfiles;
        private readonly IReadOnlyDictionary<string, TrackRoomDefinition> _roomProfiles;
        private readonly IReadOnlyDictionary<string, TrackSoundSourceDefinition> _soundDefinitions;
        private readonly Dictionary<string, int> _segmentIndexById;
        private readonly Dictionary<string, RuntimeTrackSound> _segmentTrackSounds;
        private readonly List<RuntimeTrackSound> _allTrackSounds;
        private readonly Random _random;
        private readonly string _sourceDirectory;
        private readonly float[] _segmentStartDistances;
        private RoadModel? _roadModel;

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
        private int _ambientVolumePercent;
        private float _ambientVolumeScale;
        private int _activeAudioSegmentIndex;
        private RoomAcoustics _activeRoomAcoustics;
        private TrackWeatherProfile _activeWeatherProfile;
        private TrackWeatherProfile _weatherTransitionFrom;
        private TrackWeatherProfile _weatherTransitionTo;
        private float _weatherTransitionSeconds;
        private float _weatherTransitionElapsedSeconds;
        private DateTime _lastWeatherUpdateUtc;

        private Source? _soundCrowd;
        private Source? _soundOcean;
        private Source? _soundRain;
        private Source? _soundWind;
        private Source? _soundStorm;
        private Source? _soundDesert;
        private Source? _soundAirport;
        private Source? _soundAirplane;
        private Source? _soundClock;
        private Source? _soundJet;
        private Source? _soundThunder;
        private Source? _soundPile;
        private Source? _soundConstruction;
        private Source? _soundRiver;
        private Source? _soundHelicopter;
        private Source? _soundOwl;

        private Track(string trackName, TrackData data, AudioManager audio, bool userDefined)
        {
            _trackName = trackName.Length < 64 ? trackName : string.Empty;
            _userDefined = userDefined;
            _audio = audio;
            _laneWidth = LaneWidthMeters;
            UpdateCurveScale();
            _callLength = CallLengthMeters;
            _defaultWeatherKind = data.Weather;
            _ambience = data.Ambience;
            _defaultWeatherProfileId = data.DefaultWeatherProfileId;
            _weatherProfiles = data.WeatherProfiles;
            _definition = data.Definitions;
            _segmentCount = _definition.Length;
            _hasPitArea = data.HasPitArea;
            _roomProfiles = data.RoomProfiles;
            _soundDefinitions = data.SoundSources;
            _segmentIndexById = BuildSegmentIndex(_definition);
            _segmentTrackSounds = new Dictionary<string, RuntimeTrackSound>(StringComparer.OrdinalIgnoreCase);
            _allTrackSounds = new List<RuntimeTrackSound>();
            _random = new Random();
            _sourceDirectory = ResolveSourceDirectory(data.SourcePath);
            _segmentStartDistances = new float[Math.Max(0, _segmentCount)];
            _ambientVolumePercent = 100;
            _ambientVolumeScale = 1f;
            _activeAudioSegmentIndex = -1;
            _activeRoomAcoustics = RoomAcoustics.Default;
            _activeWeatherProfile = ResolveWeatherProfile(0);
            _weatherTransitionFrom = _activeWeatherProfile;
            _weatherTransitionTo = _activeWeatherProfile;
            _lastWeatherUpdateUtc = DateTime.UtcNow;

            InitializeSounds();
            InitializeTrackSoundSources();
        }

        public string TrackName => _trackName;
        public float Length => _lapDistance;
        public int SegmentCount => _segmentCount;
        public float LapDistance => _lapDistance;
        public TrackWeather Weather => _defaultWeatherKind;
        public TrackAmbience Ambience => _ambience;
        public bool UserDefined => _userDefined;
        public bool HasPitArea => _hasPitArea;
        public float LaneWidth => _laneWidth;
        public TrackSurface InitialSurface => _definition.Length > 0 ? _definition[0].Surface : TrackSurface.Asphalt;
    }
}

