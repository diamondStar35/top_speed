using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using TopSpeed.Audio;
using TopSpeed.Data;
using TopSpeed.Input;
using TopSpeed.Menu;
using TopSpeed.Network;
using TopSpeed.Protocol;
using TopSpeed.Race;
using TopSpeed.Core;
using TopSpeed.Core.Multiplayer;
using TopSpeed.Core.Settings;
using TopSpeed.Speech;
using TopSpeed.Windowing;

namespace TopSpeed.Game
{
    internal sealed partial class Game : IDisposable,
        IMenuUiActions,
        IMenuRaceActions,
        IMenuServerActions,
        IMenuSettingsActions,
        IMenuAudioActions,
        IMenuMappingActions
    {
        private enum AppState
        {
            Logo,
            Menu,
            TimeTrial,
            SingleRace,
            MultiplayerRace,
            Paused,
            Calibration
        }

        private readonly GameWindow _window;
        private readonly AudioManager _audio;
        private readonly SpeechService _speech;
        private readonly InputManager _input;
        private readonly MenuManager _menu;
        private readonly DialogManager _dialogs;
        private readonly RaceSettings _settings;
        private readonly IReadOnlyList<SettingsIssue> _settingsIssues;
        private readonly RaceInput _raceInput;
        private readonly RaceSetup _setup;
        private readonly SettingsManager _settingsManager;
        private readonly RaceSelection _selection;
        private readonly MenuRegistry _menuRegistry;
        private readonly MultiplayerCoordinator _multiplayerCoordinator;
        private readonly ClientPktReg _mpPktReg;
        private readonly ConcurrentQueue<QueuedIncomingPacket> _queuedMultiplayerPackets;
        private MultiplayerSession? _session;
        private readonly InputMappingHandler _inputMapping;
        private LogoScreen? _logo;
        private AppState _state;
        private AppState _pausedState;
        private bool _needsCalibration;
        private bool _calibrationMenusRegistered;
        private string? _calibrationReturnMenuId;
        private bool _calibrationOverlay;
        private Stopwatch? _calibrationStopwatch;
        private bool _pendingRaceStart;
        private RaceMode _pendingMode;
        private bool _pauseKeyReleased = true;
        private LevelTimeTrial? _timeTrial;
        private LevelSingleRace? _singleRace;
        private LevelMultiplayer? _multiplayerRace;
        private bool _multiplayerRaceQuitConfirmActive;
        private TrackData? _pendingMultiplayerTrack;
        private string _pendingMultiplayerTrackName = string.Empty;
        private int _pendingMultiplayerLaps;
        private bool _pendingMultiplayerStart;
        private int _multiplayerVehicleIndex;
        private bool _multiplayerAutomaticTransmission = true;
        private bool _audioLoopActive;
        private bool _textInputPromptActive;
        private Action<TextInputResult>? _textInputPromptCallback;
        public bool IsModalInputActive { get; private set; }
        internal int LoopIntervalMs => IsMenuState(_state) ? 15 : 8;

        private const string CalibrationIntroMenuId = "calibration_intro";
        private const string CalibrationSampleMenuId = "calibration_sample";
        private const string CalibrationInstructions =
            "Screen-reader calibration. You'll be presented with a short piece of text on the next screen. Press ENTER when your screen-reader finishes speaking it.";
        private const string CalibrationSampleText =
            "I really have nothing interesting to put here not even the secret to life except this really long run on sentence that is probably the most boring thing you have ever read but that will help me get an idea of how fast your screen reader is speaking.";

        public event Action? ExitRequested;

        public Game(GameWindow window)
        {
            _window = window ?? throw new ArgumentNullException(nameof(window));
            _settingsManager = new SettingsManager();
            var settingsLoad = _settingsManager.Load();
            _settings = settingsLoad.Settings;
            _settingsIssues = settingsLoad.Issues;
            _audio = new AudioManager(_settings.HrtfAudio, _settings.AutoDetectAudioDeviceFormat);
            _input = new InputManager(_window.Handle);
            _speech = new SpeechService(_input.IsAnyInputHeld);
            _speech.ScreenReaderRateMs = _settings.ScreenReaderRateMs;
            _input.JoystickScanTimedOut += () => _speech.Speak("No joystick detected.");
            _input.SetDeviceMode(_settings.DeviceMode);
            _raceInput = new RaceInput(_settings);
            _setup = new RaceSetup();
            _menu = new MenuManager(_audio, _speech, () => _settings.UsageHints);
            _dialogs = new DialogManager(_menu);
            _menu.SetWrapNavigation(_settings.MenuWrapNavigation);
            _menu.SetMenuSoundPreset(_settings.MenuSoundPreset);
            _menu.SetMenuNavigatePanning(_settings.MenuNavigatePanning);
            _selection = new RaceSelection(_setup, _settings);
            _menuRegistry = new MenuRegistry(_menu, _settings, _setup, _raceInput, _selection, this, this, this, this, this, this);
            _inputMapping = new InputMappingHandler(_input, _raceInput, _settings, _speech, SaveSettings);
            _multiplayerCoordinator = new MultiplayerCoordinator(
                _menu,
                _dialogs,
                _audio,
                _speech,
                _settings,
                new MultiplayerConnector(),
                BeginPromptTextInput,
                SaveSettings,
                EnterMenuState,
                SetSession,
                GetSession,
                ClearSession,
                ResetPendingMultiplayerState,
                SetMultiplayerLoadout);
            _mpPktReg = new ClientPktReg();
            _queuedMultiplayerPackets = new ConcurrentQueue<QueuedIncomingPacket>();
            RegisterMultiplayerPacketHandlers();
            _menuRegistry.RegisterAll();
            _multiplayerCoordinator.ConfigureMenuCloseHandlers();
            _settings.AudioVolumes ??= new AudioVolumeSettings();
            _settings.SyncAudioCategoriesFromMusicVolume();
            ApplyAudioSettings();
            _needsCalibration = _settings.ScreenReaderRateMs <= 0f;
        }

        public void Initialize()
        {
            _logo = new LogoScreen(_audio);
            _logo.Start();
            _state = AppState.Logo;
        }

        public void Update(float deltaSeconds)
        {
            _input.Update();
            if (_input.TryGetJoystickState(out var joystick))
                _raceInput.Run(_input.Current, joystick, deltaSeconds);
            else
                _raceInput.Run(_input.Current, deltaSeconds);

            _raceInput.SetOverlayInputBlocked(
                _state == AppState.MultiplayerRace &&
                (_multiplayerCoordinator.Questions.HasActiveOverlayQuestion || _dialogs.HasActiveOverlayDialog));

            UpdateTextInputPrompt();

            switch (_state)
            {
                case AppState.Logo:
                    if (_logo == null || _logo.Update(_input, deltaSeconds))    
                    {
                        _logo?.Dispose();
                        _logo = null;
                        _menu.ShowRoot("main");
                        if (_needsCalibration)
                        {
                            if (!ShowSettingsIssuesDialog(() => StartCalibrationSequence()))
                                StartCalibrationSequence();
                            else
                                _state = AppState.Menu;
                        }
                        else
                        {
                            ShowSettingsIssuesDialog();
                            _menu.FadeInMenuMusic(force: true);
                            _state = AppState.Menu;
                        }
                    }
                    break;
                case AppState.Calibration:
                    _menu.Update(_input);
                    if (_calibrationOverlay && !IsCalibrationMenu(_menu.CurrentId))
                    {
                        _calibrationOverlay = false;
                        _state = AppState.Menu;
                    }
                    break;
                case AppState.Menu:
                    if (_session != null)
                    {
                        ProcessMultiplayerPackets();
                        if (_state != AppState.Menu)
                            break;
                    }

                    if (_textInputPromptActive)
                        break;

                    if (UpdateModalOperations())
                        break;

                    if (_inputMapping.IsActive)
                    {
                        _inputMapping.Update();
                        break;
                    }

                    var action = _menu.Update(_input);
                    HandleMenuAction(action);
                    break;
                case AppState.TimeTrial:
                    RunTimeTrial(deltaSeconds);
                    break;
                case AppState.SingleRace:
                    RunSingleRace(deltaSeconds);
                    break;
                case AppState.MultiplayerRace:
                    RunMultiplayerRace(deltaSeconds);
                    break;
                case AppState.Paused:
                    UpdatePaused();
                    break;
            }

            if (_pendingRaceStart)
            {
                _pendingRaceStart = false;
                StartRace(_pendingMode);
            }
            SyncAudioLoopState();
        }

        public void Dispose()
        {
            _logo?.Dispose();
            _menu.Dispose();
            _input.Dispose();
            _session?.SetPacketSink(null);
            _session?.Dispose();
            _speech.Dispose();
            _audio.Dispose();
        }

        private readonly struct QueuedIncomingPacket
        {
            public QueuedIncomingPacket(MultiplayerSession session, IncomingPacket packet)
            {
                Session = session;
                Packet = packet;
            }

            public MultiplayerSession Session { get; }
            public IncomingPacket Packet { get; }
        }

        public void FadeOutMenuMusic(int durationMs = 1000)
        {
            _menu.FadeOutMenuMusic(durationMs);
        }
    }
}
