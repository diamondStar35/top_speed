using System;
using System.Collections.Generic;
using TopSpeed.Audio;
using TopSpeed.Data;
using TopSpeed.Drive.Session;
using TopSpeed.Drive.Session.Audio;
using TopSpeed.Drive.Panels;
using TopSpeed.Drive.TimeTrial.Stats;
using TopSpeed.Input;
using TopSpeed.Input.Devices.Vibration;
using TopSpeed.Runtime;
using TopSpeed.Speech;
using TopSpeed.Tracks;
using TopSpeed.Vehicles;
using TopSpeed.Vehicles.Control;
using TopSpeed.Vehicles.Core;
using TS.Audio;
using CoreRequestsSubsystem = TopSpeed.Drive.Session.Systems.CoreRequests;
using ExitSubsystem = TopSpeed.Drive.Session.Systems.Exit;
using GeneralRequestsSubsystem = TopSpeed.Drive.Session.Systems.GeneralRequests;
using ListenerSubsystem = TopSpeed.Drive.Session.Systems.Listener;
using PanelsSubsystem = TopSpeed.Drive.Session.Systems.Panels;
using PlayerInfoSubsystem = TopSpeed.Drive.Session.Systems.PlayerInfo;
using PlayerVehicleSubsystem = TopSpeed.Drive.Session.Systems.PlayerVehicle;
using TrackAudioService = TopSpeed.Drive.Session.Systems.TrackAudio;
using ProgressSubsystem = TopSpeed.Drive.TimeTrial.Session.Systems.Progress;
using SessionRuntime = TopSpeed.Drive.Session.Session;

namespace TopSpeed.Drive.TimeTrial
{
    internal sealed partial class TimeTrialSession : IDisposable
    {
        private const int MaxLaps = 16;
        private const int MaxUnkeys = 12;
        private const int RandomSoundGroups = 16;
        private const int RandomSoundMax = 32;
        private const float DefaultStartCueDelaySeconds = 1.0f;
        private const float DefaultProgressStartDelaySeconds = 4.0f;
        private const float FinishAnnouncementDelaySeconds = 2.0f;
        private const float PostFinishStopSpeedKph = 0.5f;
        private readonly AudioManager _audio;
        private readonly RaceAudioFactory _raceAudio;
        private readonly SpeechService _speech;
        private readonly DriveSettings _settings;
        private readonly DriveInput _input;
        private readonly IVibrationDevice? _vibrationDevice;
        private readonly IFileDialogs _fileDialogs;
        private readonly Track _track;
        private readonly ICar _car;
        private readonly Store _scores;
        private readonly SessionRuntime _session;
        private readonly Queue _soundQueue;
        private readonly Queue _raceInfoQueue;
        private readonly Queue _trackInfoQueue;
        private readonly List<int> _lapTimes;
        private readonly string _trackId;
        private readonly int _nrOfLaps;
        private readonly ICarController _finishLockController;
        private readonly VehicleRadioController _localRadio;
        private readonly RadioVehiclePanel _radioPanel;
        private readonly VehiclePanelManager _panelManager;
        private readonly Source?[][] _randomSounds;
        private readonly string?[] _randomSoundBaseNames;
        private readonly int[] _totalRandomSounds;
        private readonly Source[] _soundUnkey;
        private readonly Source[] _soundLaps;

        private readonly PanelsSubsystem _panels;
        private readonly PlayerVehicleSubsystem _playerVehicle;
        private readonly ProgressSubsystem _progress;
        private readonly ListenerSubsystem _listener;
        private readonly CoreRequestsSubsystem _coreRequests;
        private readonly GeneralRequestsSubsystem _generalRequests;
        private readonly PlayerInfoSubsystem _playerInfo;
        private readonly ExitSubsystem _exit;
        private readonly TrackAudioService _trackAudio;

        private uint _nextMediaId;
        private Track.Road _currentRoad;
        private CarState _lastRecordedCarState;
        private int _lap;
        private int _raceTime;
        private int _localCrashCount;
        private int _unkeyQueue;
        private int _lastLapRaceTimeMs;
        private float _speakTime;
        private bool _manualTransmission;
        private bool _started;
        private bool _finished;
        private bool _exitWhenQueueIdle;
        private bool _requirePostFinishStopBeforeExit;
        private DriveResultSummary? _pendingResultSummary;

        private Source _soundStart;
        private Source? _soundTheme;
        private Source? _soundPause;
        private Source? _soundResume;
        private Source? _soundTurnEndDing;

        public TimeTrialSession(
            AudioManager audio,
            SpeechService speech,
            DriveSettings settings,
            DriveInput input,
            string track,
            string trackId,
            bool automaticTransmission,
            int laps,
            int vehicleIndex,
            string? vehicleFile,
            IVibrationDevice? vibrationDevice,
            IFileDialogs fileDialogs)
        {
            _audio = audio ?? throw new ArgumentNullException(nameof(audio));
            _speech = speech ?? throw new ArgumentNullException(nameof(speech));
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _input = input ?? throw new ArgumentNullException(nameof(input));
            _vibrationDevice = vibrationDevice;
            _fileDialogs = fileDialogs ?? throw new ArgumentNullException(nameof(fileDialogs));
            _trackId = trackId ?? throw new ArgumentNullException(nameof(trackId));
            _raceAudio = new RaceAudioFactory(_audio);
            _scores = Store.CreateDefault();
            _soundQueue = new Queue();
            _raceInfoQueue = new Queue();
            _trackInfoQueue = new Queue();
            _lapTimes = new List<int>();
            _finishLockController = new FinishLockInputController(input);
            _manualTransmission = !automaticTransmission;
            _lap = 0;
            _unkeyQueue = 0;

            _nrOfLaps = ApplyAdventureLapOverride(track, laps);
            var runtimeObjects = CreateRuntimeObjects(track, vehicleIndex, vehicleFile);
            _track = runtimeObjects.Track;
            _car = runtimeObjects.Car;
            _localRadio = runtimeObjects.LocalRadio;
            _radioPanel = runtimeObjects.RadioPanel;
            _panelManager = runtimeObjects.PanelManager;
            (_randomSounds, _totalRandomSounds) = CreateRandomSoundContainers();
            _randomSoundBaseNames = new string?[RandomSoundGroups];
            ConfigureDefaultRandomSounds();
            _soundUnkey = CreateUnkeySounds();
            _soundLaps = CreateLapSounds(_nrOfLaps);
            _soundStart = LoadLanguageSound("race\\start321");
            _soundTheme = LoadLanguageMusicSound("music\\theme4", streamFromDisk: false);
            _soundPause = LoadLanguageSound("race\\pause");
            _soundResume = LoadLanguageSound("race\\unpause");
            _soundTurnEndDing = LoadLegacySound("ding.ogg");
            PreloadRaceSpeechSources();
            _trackAudio = new TrackAudioService(_settings, GetRandomSoundBySlot, _soundTurnEndDing, QueueTrackInfoSound, (sessionEvent, delay) => _session!.QueueEvent(sessionEvent, delay));
            _panels = new PanelsSubsystem("panels", 100, _input, _panelManager, _radioPanel, SpeakText);
            _playerVehicle = new PlayerVehicleSubsystem(
                "vehicle",
                110,
                _car,
                _input,
                _track,
                _settings,
                _trackAudio,
                () => _currentRoad,
                road => _currentRoad = road,
                () => _started,
                () => _finished,
                TrackLocalCrashState,
                SpeakText);
            _listener = new ListenerSubsystem("listener", 130, _audio, _car, _localRadio);
            _coreRequests = new CoreRequestsSubsystem(
                "coreRequests",
                200,
                _input,
                _settings,
                _car,
                _track,
                () => _started,
                () => _lap,
                () => _nrOfLaps,
                () => _lap <= _nrOfLaps ? _session!.Context.ProgressMilliseconds : _raceTime,
                SpeakText);
            _generalRequests = new GeneralRequestsSubsystem(
                "generalRequests",
                210,
                _input,
                _car,
                () => _started,
                () => _lap,
                () => _nrOfLaps,
                () => _session!.ApplyCommand(new Command(Commands.RequestPause)));
            _playerInfo = new PlayerInfoSubsystem(
                "playerInfo",
                220,
                _input,
                () => 0,
                player => player == 0,
                GetPlayerNameForPlayer,
                _ => GetVehicleName(),
                () => _started,
                SpeakText,
                CalculatePlayerPerc);
            _exit = new ExitSubsystem(
                "exit",
                300,
                UpdateExitWhenQueueIdle,
                () => _session!.ApplyCommand(new Command(Commands.RequestExit)));
            _progress = new ProgressSubsystem(
                "progress",
                120,
                _track,
                _car,
                _settings,
                _nrOfLaps,
                _soundLaps,
                _lapTimes,
                () => _lap,
                lap => _lap = lap,
                () => _lastLapRaceTimeMs,
                value => _lastLapRaceTimeMs = value,
                ApplyPlayerFinishState,
                () => _session!.QueueEvent(new Event(Events.ProgressFinish), FinishAnnouncementDelaySeconds),
                SpeakRaceInfo);
            _session = CreateSession();
        }

        public bool WantsExit => _session.Context.WantsExit;
        public bool WantsPause => _session.Context.WantsPause;

        private SessionRuntime CreateSession()
        {
            var allowedCommands = Defaults.StandardCommands;
            var allowedExternalEvents = Defaults.NoExternalEvents;
            var policy = new PolicyBuilder(Phase.Initializing, Phase.Countdown)
                .Add(Phase.Initializing, false, false, InputPolicy.Create(false, true, false), Defaults.NoSubsystems, allowedCommands, allowedExternalEvents, new[] { Phase.Countdown, Phase.Aborted })
                .Add(Phase.Countdown, true, true, InputPolicy.Create(true, true, true), PhaseDefinition.Subsystems(_panels, _playerVehicle, _progress, _listener, _coreRequests, _generalRequests, _playerInfo, _exit), allowedCommands, allowedExternalEvents, new[] { Phase.Running, Phase.Paused, Phase.Aborted })
                .Add(Phase.Running, true, true, InputPolicy.Create(true, true, true), PhaseDefinition.Subsystems(_panels, _playerVehicle, _progress, _listener, _coreRequests, _generalRequests, _playerInfo, _exit), allowedCommands, allowedExternalEvents, new[] { Phase.Paused, Phase.Finishing, Phase.Finished, Phase.Aborted })
                .Add(Phase.Paused, false, false, InputPolicy.Create(false, true, false), Defaults.NoSubsystems, allowedCommands, allowedExternalEvents, new[] { Phase.Countdown, Phase.Running, Phase.Finishing, Phase.Aborted })
                .Add(Phase.Finishing, true, true, InputPolicy.Create(false, true, false), PhaseDefinition.Subsystems(_playerVehicle, _listener, _exit), allowedCommands, allowedExternalEvents, new[] { Phase.Finished, Phase.Aborted })
                .Add(Phase.Finished, true, true, InputPolicy.Create(false, true, false), PhaseDefinition.Subsystems(_playerVehicle, _listener, _exit), allowedCommands, allowedExternalEvents, new[] { Phase.Aborted })
                .Add(Phase.Aborted, false, false, InputPolicy.Create(false, true, false), Defaults.NoSubsystems, allowedCommands, allowedExternalEvents, Array.Empty<Phase>())
                .Build();

            var builder = new SessionBuilder(policy);
            builder.AddSubsystems(_panels, _playerVehicle, _progress, _listener, _coreRequests, _generalRequests, _playerInfo, _exit);
            builder.AddEventHandler(new HandlerId("timeTrial.events"), 100, HandleSessionEvent);
            builder.AddEventHandler(new HandlerId("timeTrial.phase"), 200, HandlePhaseEvent);
            return builder.Build();
        }
    }
}
