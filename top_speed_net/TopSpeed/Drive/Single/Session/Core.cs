using System;
using System.Collections.Generic;
using TopSpeed.Audio;
using TopSpeed.Data;
using TopSpeed.Drive.Session;
using TopSpeed.Drive.Session.Audio;
using TopSpeed.Drive.Panels;
using TopSpeed.Input;
using TopSpeed.Input.Devices.Vibration;
using TopSpeed.Runtime;
using TopSpeed.Speech;
using TopSpeed.Tracks;
using TopSpeed.Vehicles;
using TopSpeed.Vehicles.Control;
using TopSpeed.Vehicles.Core;
using TS.Audio;
using BotsSubsystem = TopSpeed.Drive.Single.Session.Systems.Bots;
using CollisionsSubsystem = TopSpeed.Drive.Single.Session.Systems.Collisions;
using CommentarySubsystem = TopSpeed.Drive.Single.Session.Systems.Commentary;
using ProgressSubsystem = TopSpeed.Drive.Single.Session.Systems.Progress;
using CoreRequestsSubsystem = TopSpeed.Drive.Session.Systems.CoreRequests;
using ExitSubsystem = TopSpeed.Drive.Session.Systems.Exit;
using GeneralRequestsSubsystem = TopSpeed.Drive.Session.Systems.GeneralRequests;
using ListenerSubsystem = TopSpeed.Drive.Session.Systems.Listener;
using PanelsSubsystem = TopSpeed.Drive.Session.Systems.Panels;
using PlayerInfoSubsystem = TopSpeed.Drive.Session.Systems.PlayerInfo;
using PlayerVehicleSubsystem = TopSpeed.Drive.Session.Systems.PlayerVehicle;
using TrackAudioService = TopSpeed.Drive.Session.Systems.TrackAudio;
using PitStopSubsystem = TopSpeed.Drive.Session.Systems.PitStop;
using SessionRuntime = TopSpeed.Drive.Session.Session;

namespace TopSpeed.Drive.Single
{
    internal sealed partial class SingleSession : IDisposable
    {
        private const int MaxComputerPlayers = 7;
        private const int MaxPlayers = 8;
        private const int MaxLaps = 16;
        private const int MaxUnkeys = 12;
        private const int RandomSoundGroups = 16;
        private const int RandomSoundMax = 32;
        private const float StartLineY = 140.0f;
        private const float DefaultStartCueDelaySeconds = 1.0f;
        private const float DefaultProgressStartDelaySeconds = 4.0f;
        private const float PostFinishStopSpeedKph = 0.5f;
        private const float BotSettledSpeedKph = 0.5f;
        private readonly AudioManager _audio;
        private readonly RaceAudioFactory _raceAudio;
        private readonly SpeechService _speech;
        private readonly DriveSettings _settings;
        private readonly DriveInput _input;
        private readonly IVibrationDevice? _vibrationDevice;
        private readonly IFileDialogs _fileDialogs;
        private readonly RacePhysicsToggles _physicsToggles;
        private readonly Track _track;
        private readonly ICar _car;
        private readonly SessionRuntime _session;
        private readonly Queue _soundQueue;
        private readonly Queue _raceInfoQueue;
        private readonly Queue _trackInfoQueue;
        private readonly ICarController _finishLockController;
        private readonly VehicleRadioController _localRadio;
        private readonly RadioVehiclePanel _radioPanel;
        private readonly VehiclePanelManager _panelManager;
        private readonly int _nrOfLaps;
        private readonly ComputerPlayer?[] _computerPlayers;
        private readonly Source?[] _soundNumbers;
        private readonly Source?[][] _randomSounds;
        private readonly string?[] _randomSoundBaseNames;
        private readonly int[] _totalRandomSounds;
        private readonly Source[] _soundUnkey;
        private readonly Source[] _soundLaps;
        private readonly Source?[] _soundPosition;
        private readonly Source?[] _soundPlayerNr;
        private readonly Source?[] _soundPlayerNrInfo;
        private readonly Source?[] _soundFinished;
        private Source? _soundFinishedLast;
        private readonly Dictionary<int, int> _finishTimesMs;
        private readonly List<int> _finishOrder;

        private readonly PanelsSubsystem _panels;
        private readonly PlayerVehicleSubsystem _playerVehicle;
        private readonly ListenerSubsystem _listener;
        private readonly CoreRequestsSubsystem _coreRequests;
        private readonly GeneralRequestsSubsystem _generalRequests;
        private readonly PlayerInfoSubsystem _playerInfo;
        private readonly ExitSubsystem _exit;
        private readonly TrackAudioService _trackAudio;
        private readonly BotsSubsystem _bots;
        private readonly ProgressSubsystem _progress;
        private readonly CommentarySubsystem _commentary;
        private readonly CollisionsSubsystem _collisions;
        private readonly PitStopSubsystem _pitStop;

        private Track.Road _currentRoad;
        private CarState _lastRecordedCarState;
        private int _lap;
        private int _raceTime;
        private int _localCrashCount;
        private int _unkeyQueue;
        private int _playerNumber;
        private int _nComputerPlayers;
        private int _position;
        private int _positionComment;
        private int _positionFinish;
        private float _speakTime;
        private bool _manualTransmission;
        private bool _started;
        private bool _finished;
        private bool _exitWhenQueueIdle;
        private bool _requirePostFinishStopBeforeExit;
        private bool _botsScheduled;
        private uint _nextMediaId;
        private DriveResultSummary? _pendingResultSummary;

        private Source _soundStart;
        private Source? _soundTheme;
        private Source? _soundPause;
        private Source? _soundResume;
        private Source? _soundTurnEndDing;
        private Source? _soundYouAre;
        private Source? _soundPlayer;
        private Source? _soundLetsPit;
        private Source? _soundRightTires;
        private Source? _soundLeftTires;
        private Source? _soundFuelingUp;
        private Source? _soundExitPitRoad;

        public SingleSession(
            AudioManager audio,
            SpeechService speech,
            DriveSettings settings,
            DriveInput input,
            string track,
            bool automaticTransmission,
            int laps,
            int vehicleIndex,
            string? vehicleFile,
            IVibrationDevice? vibrationDevice,
            IFileDialogs fileDialogs,
            RacePhysicsToggles physicsToggles)
        {
            _audio = audio ?? throw new ArgumentNullException(nameof(audio));
            _speech = speech ?? throw new ArgumentNullException(nameof(speech));
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _input = input ?? throw new ArgumentNullException(nameof(input));
            _vibrationDevice = vibrationDevice;
            _fileDialogs = fileDialogs ?? throw new ArgumentNullException(nameof(fileDialogs));
            _physicsToggles = physicsToggles;
            _raceAudio = new RaceAudioFactory(_audio);
            _soundQueue = new Queue();
            _raceInfoQueue = new Queue();
            _trackInfoQueue = new Queue();
            _finishLockController = new FinishLockInputController(input);
            _manualTransmission = !automaticTransmission;
            _nComputerPlayers = Math.Min(settings.NrOfComputers, MaxComputerPlayers);
            _playerNumber = 1;
            _positionFinish = 0;
            _computerPlayers = new ComputerPlayer?[MaxComputerPlayers];
            _soundNumbers = CreateNumberSounds();
            (_randomSounds, _totalRandomSounds) = CreateRandomSoundContainers();
            _randomSoundBaseNames = new string?[RandomSoundGroups];
            _soundUnkey = CreateUnkeySounds();
            _soundPosition = new Source?[MaxPlayers];
            _soundPlayerNr = new Source?[MaxPlayers];
            _soundPlayerNrInfo = new Source?[MaxPlayers];
            _soundFinished = new Source?[MaxPlayers];
            _finishTimesMs = new Dictionary<int, int>(MaxPlayers);
            _finishOrder = new List<int>(MaxPlayers);

            _nrOfLaps = ApplyAdventureLapOverride(track, laps);
            _soundLaps = CreateLapSounds(_nrOfLaps);
            var runtimeObjects = CreateRuntimeObjects(track, vehicleIndex, vehicleFile);
            _track = runtimeObjects.Track;
            _car = runtimeObjects.Car;
            _localRadio = runtimeObjects.LocalRadio;
            _radioPanel = runtimeObjects.RadioPanel;
            _panelManager = runtimeObjects.PanelManager;
            ConfigureDefaultRandomSounds();
            LoadRaceUiSounds();
            _soundStart = LoadLanguageSound("race\\start321");

            _trackAudio = new TrackAudioService(_settings, GetRandomSoundBySlot, _soundTurnEndDing, QueueTrackInfoSound, (sessionEvent, delay) => _session!.QueueEvent(sessionEvent, delay));
            _panels = new PanelsSubsystem("panels", 110, _input, _panelManager, _radioPanel, SpeakText);
            _playerVehicle = new PlayerVehicleSubsystem(
                "vehicle",
                120,
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
                SpeakText,
                skipCrashEval: () => _pitStop!.IsActive);
            _listener = new ListenerSubsystem("listener", 140, _audio, _car, _localRadio,
                () => _pitStop!.ListenerXOverride);
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
                SpeakText,
                isInPitStop: () => _pitStop!.IsActive);
            _generalRequests = new GeneralRequestsSubsystem(
                "generalRequests",
                230,
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
                () => _nComputerPlayers,
                player => player >= 0 && player <= _nComputerPlayers,
                GetPlayerNameForPlayer,
                GetVehicleNameForPlayer,
                () => _started,
                SpeakText,
                CalculatePlayerPerc,
                HandlePlayerNumberRequest);
            _exit = new ExitSubsystem(
                "exit",
                300,
                UpdateExitWhenQueueIdle,
                () => _session!.ApplyCommand(new Command(Commands.RequestExit)));
            _bots = new BotsSubsystem(
                "bots",
                100,
                _computerPlayers,
                _nComputerPlayers,
                _car,
                _track,
                _nrOfLaps,
                ReadCurrentRaceTimeMs,
                UpdatePositions,
                RecordFinish,
                AnnounceFinishOrder,
                CheckFinish,
                progressSeconds => _session!.QueueEvent(new Event(Events.ProgressFinish), 1.0f + _speakTime - progressSeconds));
            _progress = new ProgressSubsystem(
                "progress",
                130,
                _track,
                _car,
                _settings,
                _nrOfLaps,
                _soundLaps,
                () => _lap,
                lap => _lap = lap,
                ApplyPlayerFinishState,
                () =>
                {
                    RecordFinish(_playerNumber, _raceTime);
                    AnnounceFinishOrder(_playerNumber);
                    if (CheckFinish())
                        _session!.QueueEvent(new Event(Events.ProgressFinish), 1.0f + _speakTime - _session.Context.ProgressSeconds);
                },
                SpeakRaceInfo);
            _commentary = new CommentarySubsystem(
                "commentary",
                210,
                _settings,
                _input,
                _car,
                _computerPlayers,
                _nComputerPlayers,
                () => _playerNumber,
                GetPositionSoundByIndex,
                GetPlayerNumberSoundByIndex,
                GetRandomSoundBySlot,
                () => _started,
                () => _lap,
                () => _nrOfLaps,
                () => _positionComment,
                value => _positionComment = value,
                SpeakIfLoaded,
                Speak);
            _collisions = new CollisionsSubsystem("collisions", 150, _track, _car, _computerPlayers, () => _playerNumber, () => _nComputerPlayers, () => _pitStop!.IsGhosted);
            _pitStop = new PitStopSubsystem(
                "pitStop",
                135,
                _input,
                _car,
                _track,
                () => _started,
                () => _finished,
                _soundLetsPit,
                _soundRightTires,
                _soundLeftTires,
                _soundFuelingUp,
                _soundExitPitRoad,
                () => { },
                () => { },
                SpeakText,
                s => QueueSound(s),
                (s, d) => _session!.QueueEvent(new Event(Events.PlaySound, s), d),
                road => _trackAudio.AnnounceUpcomingCurve(road));

            _session = CreateSession();
        }

        public bool WantsExit => _session.Context.WantsExit;
        public bool WantsPause => _session.Context.WantsPause;
        public bool WantsPitStopMenu => _pitStop.NeedsChoice;
        public void AcceptPitStopChoice(int choiceId) => _pitStop.SetChoice(choiceId);

        private SessionRuntime CreateSession()
        {
            var allowedCommands = Defaults.StandardCommands;
            var allowedExternalEvents = Defaults.NoExternalEvents;
            var policy = new PolicyBuilder(Phase.Initializing, Phase.Countdown)
                .Add(Phase.Initializing, false, false, InputPolicy.Create(false, true, false), Defaults.NoSubsystems, allowedCommands, allowedExternalEvents, new[] { Phase.Countdown, Phase.Aborted })
                .Add(Phase.Countdown, true, true, InputPolicy.Create(true, true, true), PhaseDefinition.Subsystems(_bots, _panels, _playerVehicle, _progress, _listener, _coreRequests, _commentary, _playerInfo, _generalRequests, _exit), allowedCommands, allowedExternalEvents, new[] { Phase.Running, Phase.Paused, Phase.Aborted })
                .Add(Phase.Running, true, true, InputPolicy.Create(true, true, true), PhaseDefinition.Subsystems(_bots, _panels, _playerVehicle, _progress, _listener, _collisions, _coreRequests, _commentary, _playerInfo, _generalRequests, _exit, _pitStop), allowedCommands, allowedExternalEvents, new[] { Phase.Paused, Phase.Finished, Phase.Aborted })
                .Add(Phase.Paused, false, false, InputPolicy.Create(false, true, false), Defaults.NoSubsystems, allowedCommands, allowedExternalEvents, new[] { Phase.Countdown, Phase.Running, Phase.Finished, Phase.Aborted })
                .Add(Phase.Finished, true, true, InputPolicy.Create(false, true, false), PhaseDefinition.Subsystems(_bots, _playerVehicle, _listener, _exit), allowedCommands, allowedExternalEvents, new[] { Phase.Aborted })
                .Add(Phase.Aborted, false, false, InputPolicy.Create(false, true, false), Defaults.NoSubsystems, allowedCommands, allowedExternalEvents, Array.Empty<Phase>())
                .Build();

            var builder = new SessionBuilder(policy);
            builder.AddSubsystems(_bots, _panels, _playerVehicle, _progress, _listener, _collisions, _coreRequests, _commentary, _playerInfo, _generalRequests, _exit, _pitStop);
            builder.AddEventHandler(new HandlerId("single.events"), 100, HandleSessionEvent);
            builder.AddEventHandler(new HandlerId("single.phase"), 200, HandlePhaseEvent);
            return builder.Build();
        }
    }
}
