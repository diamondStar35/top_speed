using System;
using System.Diagnostics;
using SharpDX.DirectInput;
using TopSpeed.Audio;
using TopSpeed.Common;
using TopSpeed.Data;
using TopSpeed.Input;
using TopSpeed.Menu;
using TopSpeed.Network;
using TopSpeed.Protocol;
using TopSpeed.Race;
using TopSpeed.Speech;
using TopSpeed.Windowing;

namespace TopSpeed.Core
{
    internal sealed class Game : IDisposable, IMenuActions
    {
        private enum AppState
        {
            Logo,
            Menu,
            TimeTrial,
            SingleRace,
            MultiplayerRace,
            Exploration,
            Paused,
            Calibration
        }

        private readonly GameWindow _window;
        private readonly AudioManager _audio;
        private readonly SpeechService _speech;
        private readonly InputManager _input;
        private readonly MenuManager _menu;
        private readonly RaceSettings _settings;
        private readonly RaceInput _raceInput;
        private readonly RaceSetup _setup;
        private readonly SettingsManager _settingsManager;
        private readonly RaceSelection _selection;
        private readonly MenuRegistry _menuRegistry;
        private readonly MultiplayerCoordinator _multiplayerCoordinator;
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
        private LevelExplore? _explore;
        private TrackData? _pendingMultiplayerTrack;
        private string _pendingMultiplayerTrackName = string.Empty;
        private int _pendingMultiplayerLaps;
        private bool _pendingMultiplayerStart;
        private bool _audioLoopActive;
        public bool IsModalInputActive { get; private set; }
        internal int LoopIntervalMs => IsMenuState(_state) ? 30 : 8;

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
            _settings = _settingsManager.Load();
            _audio = new AudioManager(_settings.ThreeDSound, _settings.AutoDetectAudioDeviceFormat);
            _input = new InputManager(_window.Handle);
            _speech = new SpeechService(_input.IsAnyInputHeld);
            _speech.ScreenReaderRateMs = _settings.ScreenReaderRateMs;
            _input.JoystickScanTimedOut += () => _speech.Speak("No joystick detected.");
            _input.SetDeviceMode(_settings.DeviceMode);
            _raceInput = new RaceInput(_settings);
            _setup = new RaceSetup();
            _menu = new MenuManager(_audio, _speech, () => _settings.UsageHints);
            _menu.SetWrapNavigation(_settings.MenuWrapNavigation);
            _menu.SetMenuSoundPreset(_settings.MenuSoundPreset);
            _menu.SetMenuNavigatePanning(_settings.MenuNavigatePanning);
            _selection = new RaceSelection(_setup, _settings);
            _menuRegistry = new MenuRegistry(_menu, _settings, _setup, _raceInput, _selection, this);
            _inputMapping = new InputMappingHandler(_input, _raceInput, _settings, _speech, SaveSettings);
            _multiplayerCoordinator = new MultiplayerCoordinator(
                _menu,
                _speech,
                _settings,
                new MultiplayerConnector(),
                PromptTextInput,
                SaveSettings,
                EnterMenuState,
                SetSession,
                ClearSession,
                ResetPendingMultiplayerState);
            _menuRegistry.RegisterAll();
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
                _raceInput.Run(_input.Current, joystick);
            else
                _raceInput.Run(_input.Current);

            switch (_state)
            {
                case AppState.Logo:
                    if (_logo == null || _logo.Update(_input, deltaSeconds))    
                    {
                        _logo?.Dispose();
                        _logo = null;
                        if (_needsCalibration)
                        {
                            StartCalibrationSequence();
                        }
                        else
                        {
                            _menu.ShowRoot("main");
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
                    if (UpdateModalOperations())
                        break;

                    if (_session != null)
                    {
                        ProcessMultiplayerPackets();
                        if (_state != AppState.Menu)
                            break;
                    }

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
                case AppState.Exploration:
                    RunExploration(deltaSeconds);
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

        private void HandleMenuAction(MenuAction action)
        {
            switch (action)
            {
                case MenuAction.Exit:
                    ExitRequested?.Invoke();
                    break;
                case MenuAction.QuickStart:
                    PrepareQuickStart();
                    QueueRaceStart(RaceMode.QuickStart);
                    break;
                default:
                    break;
            }
        }

        private bool UpdateModalOperations()
        {
            return _multiplayerCoordinator.UpdatePendingOperations();
        }

        private TextInputResult PromptTextInput(string prompt, string? initialValue,
            SpeechService.SpeakFlag speakFlag = SpeechService.SpeakFlag.None, bool speakBeforeInput = true)
        {
            if (speakBeforeInput)
                _speech.Speak(prompt, speakFlag);

            IsModalInputActive = true;
            _input.Suspend();
            var result = _window.ReceiveTextInput(initialValue);
            _input.Resume();
            IsModalInputActive = false;

            return result;
        }

        private void StartCalibrationSequence(string? returnMenuId = null)
        {
            _calibrationReturnMenuId = returnMenuId;
            _calibrationStopwatch = null;
            EnsureCalibrationMenus();
            _calibrationOverlay = !string.IsNullOrWhiteSpace(returnMenuId) && _menu.HasActiveMenu;
            if (_calibrationOverlay)
                _menu.Push(CalibrationIntroMenuId);
            else
                _menu.ShowRoot(CalibrationIntroMenuId);
            _state = AppState.Calibration;
        }

        private void EnsureCalibrationMenus()
        {
            if (_calibrationMenusRegistered)
                return;

            var introItems = new[]
            {
                new MenuItem("Ok", MenuAction.None, onActivate: BeginCalibrationSample)
            };
            var sampleItems = new[]
            {
                new MenuItem("Ok", MenuAction.None, onActivate: CompleteCalibration)
            };

            _menu.Register(_menu.CreateMenu(CalibrationIntroMenuId, introItems, CalibrationInstructions));
            _menu.Register(_menu.CreateMenu(CalibrationSampleMenuId, sampleItems, CalibrationSampleText));
            _calibrationMenusRegistered = true;
        }

        private void BeginCalibrationSample()
        {
            _calibrationStopwatch = Stopwatch.StartNew();
            if (_calibrationOverlay)
                _menu.ReplaceTop(CalibrationSampleMenuId);
            else
                _menu.ShowRoot(CalibrationSampleMenuId);
        }

        private void CompleteCalibration()
        {
            if (_calibrationStopwatch == null)
                return;

            var elapsedMs = _calibrationStopwatch.ElapsedMilliseconds;
            var words = CalibrationSampleText.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).Length;
            var rate = words > 0 ? (float)elapsedMs / words : 0f;
            _settings.ScreenReaderRateMs = rate;
            _speech.ScreenReaderRateMs = rate;
            SaveSettings();

            _needsCalibration = false;
            var returnMenu = _calibrationReturnMenuId ?? "main";
            _calibrationReturnMenuId = null;
            if (_calibrationOverlay && _menu.CanPop)
            {
                _menu.PopToPrevious();
            }
            else
            {
                _menu.ShowRoot(returnMenu);
            }
            _calibrationOverlay = false;
            _menu.FadeInMenuMusic(force: true);
            _state = AppState.Menu;
        }

        private static bool IsCalibrationMenu(string? id)
        {
            return id == CalibrationIntroMenuId || id == CalibrationSampleMenuId;
        }

        private void EnterMenuState()
        {
            _state = AppState.Menu;
        }

        private void SetSession(MultiplayerSession session)
        {
            _session = session;
        }

        private void ClearSession()
        {
            _session?.Dispose();
            _session = null;
        }

        private void ResetPendingMultiplayerState()
        {
            _pendingMultiplayerTrack = null;
            _pendingMultiplayerTrackName = string.Empty;
            _pendingMultiplayerLaps = 0;
            _pendingMultiplayerStart = false;
        }

        private void DisconnectFromServer()
        {
            _multiplayerRace?.FinalizeLevelMultiplayer();
            _multiplayerRace?.Dispose();
            _multiplayerRace = null;

            ResetPendingMultiplayerState();
            ClearSession();
            _state = AppState.Menu;
            _menu.ShowRoot("main");
            _menu.FadeInMenuMusic();
        }

        private void RestoreDefaults()
        {
            _settings.RestoreDefaults();
            _raceInput.SetDevice(_settings.DeviceMode);
            _input.SetDeviceMode(_settings.DeviceMode);
            _speech.ScreenReaderRateMs = _settings.ScreenReaderRateMs;
            _needsCalibration = _settings.ScreenReaderRateMs <= 0f;
            _menu.SetWrapNavigation(_settings.MenuWrapNavigation);
            _menu.SetMenuSoundPreset(_settings.MenuSoundPreset);
            _menu.SetMenuNavigatePanning(_settings.MenuNavigatePanning);
            SaveSettings();
            _speech.Speak("Defaults restored.");
        }

        private void SetDevice(InputDeviceMode mode)
        {
            _settings.DeviceMode = mode;
            _raceInput.SetDevice(mode);
            _input.SetDeviceMode(mode);
            SaveSettings();
        }

        private void ToggleCurveAnnouncements()
        {
            _settings.CurveAnnouncement = _settings.CurveAnnouncement == CurveAnnouncementMode.FixedDistance
                ? CurveAnnouncementMode.SpeedDependent
                : CurveAnnouncementMode.FixedDistance;
            SaveSettings();
        }

        private void ToggleSetting(Action update)
        {
            update();
            SaveSettings();
        }

        private void UpdateSetting(Action update)
        {
            update();
            SaveSettings();
        }

        private void PrepareQuickStart()
        {
            _setup.Mode = RaceMode.QuickStart;
            _setup.ClearSelection();
            _selection.SelectRandomTrackAny(_settings.RandomCustomTracks);
            _selection.SelectRandomVehicle();
            _setup.Transmission = TransmissionMode.Automatic;
        }

        private void QueueRaceStart(RaceMode mode)
        {
            _pendingRaceStart = true;
            _pendingMode = mode;
        }

        private void RunTimeTrial(float elapsed)
        {
            if (_timeTrial == null)
            {
                EndRace();
                return;
            }

            _timeTrial.Run(elapsed);
            if (_timeTrial.WantsPause)
                EnterPause(AppState.TimeTrial);
            if (_timeTrial.WantsExit || _input.WasPressed(SharpDX.DirectInput.Key.Escape))
                EndRace();
        }

        private void RunSingleRace(float elapsed)
        {
            if (_singleRace == null)
            {
                EndRace();
                return;
            }

            _singleRace.Run(elapsed);
            if (_singleRace.WantsPause)
                EnterPause(AppState.SingleRace);
            if (_singleRace.WantsExit || _input.WasPressed(SharpDX.DirectInput.Key.Escape))
                EndRace();
        }

        private void RunMultiplayerRace(float elapsed)
        {
            if (_multiplayerRace == null)
            {
                EndMultiplayerRace();
                return;
            }

            ProcessMultiplayerPackets();
            if (_multiplayerRace == null)
                return;
            _multiplayerRace.Run(elapsed);
            if (_multiplayerRace.WantsExit || _input.WasPressed(SharpDX.DirectInput.Key.Escape))
                EndMultiplayerRace();
        }

        private void RunExploration(float elapsed)
        {
            if (_explore == null)
            {
                EndExploration();
                return;
            }

            _explore.Run(elapsed);
            if (_explore.WantsExit || _input.WasPressed(SharpDX.DirectInput.Key.Escape))
                EndExploration();
        }

        private void ProcessMultiplayerPackets()
        {
            if (_session == null)
                return;

            while (_session.TryDequeuePacket(out var packet))
            {
                switch (packet.Command)
                {
                    case Command.PlayerJoined:
                        if (ClientPacketSerializer.TryReadPlayerJoined(packet.Payload, out var joined))
                        {
                            if (joined.PlayerNumber != _session.PlayerNumber)
                            {
                                var name = string.IsNullOrWhiteSpace(joined.Name)
                                    ? $"Player {joined.PlayerNumber + 1}"
                                    : joined.Name;
                                _speech.Speak($"{name} joined.");
                            }
                        }
                        break;
                    case Command.LoadCustomTrack:
                        if (ClientPacketSerializer.TryReadLoadCustomTrack(packet.Payload, out var track))
                        {
                            var name = string.IsNullOrWhiteSpace(track.TrackName) ? "custom" : track.TrackName;
                            var userDefined = string.Equals(name, "custom", StringComparison.OrdinalIgnoreCase);
                            _pendingMultiplayerTrack = new TrackData(userDefined, track.TrackWeather, track.TrackAmbience, track.Definitions);
                            _pendingMultiplayerTrackName = name;
                            _pendingMultiplayerLaps = track.NrOfLaps;
                            if (_pendingMultiplayerStart)
                                StartMultiplayerRace();
                        }
                        break;
                    case Command.StartRace:
                        StartMultiplayerRace();
                        break;
                    case Command.PlayerData:
                        if (_multiplayerRace != null && ClientPacketSerializer.TryReadPlayerData(packet.Payload, out var playerData))
                            _multiplayerRace.ApplyRemoteData(playerData);
                        break;
                    case Command.PlayerBumped:
                        if (_multiplayerRace != null && ClientPacketSerializer.TryReadPlayerBumped(packet.Payload, out var bump))
                            _multiplayerRace.ApplyBump(bump);
                        break;
                    case Command.PlayerDisconnected:
                        if (_multiplayerRace != null && ClientPacketSerializer.TryReadPlayer(packet.Payload, out var disconnected))
                            _multiplayerRace.RemoveRemotePlayer(disconnected.PlayerNumber);
                        break;
                    case Command.StopRace:
                    case Command.RaceAborted:
                        if (_state == AppState.MultiplayerRace)
                            EndMultiplayerRace();
                        break;
                }
            }
        }

        private void StartMultiplayerRace()
        {
            if (_session == null)
                return;
            if (_multiplayerRace != null)
                return;
            if (_pendingMultiplayerTrack == null)
            {
                _pendingMultiplayerStart = true;
                return;
            }

            _pendingMultiplayerStart = false;
            FadeOutMenuMusic();
            var trackName = string.IsNullOrWhiteSpace(_pendingMultiplayerTrackName) ? "custom" : _pendingMultiplayerTrackName;
            var laps = _pendingMultiplayerLaps > 0 ? _pendingMultiplayerLaps : _settings.NrOfLaps;
            var vehicleIndex = 0;
            var automatic = true;

            _multiplayerRace?.FinalizeLevelMultiplayer();
            _multiplayerRace?.Dispose();
            _multiplayerRace = new LevelMultiplayer(
                _audio,
                _speech,
                _settings,
                _raceInput,
                _pendingMultiplayerTrack!,
                trackName,
                automatic,
                laps,
                vehicleIndex,
                null,
                _input.VibrationDevice,
                _session,
                _session.PlayerId,
                _session.PlayerNumber);
            _multiplayerRace.Initialize();
            _state = AppState.MultiplayerRace;
        }

        private void EndMultiplayerRace()
        {
            _multiplayerRace?.FinalizeLevelMultiplayer();
            _multiplayerRace?.Dispose();
            _multiplayerRace = null;

            if (_session != null)
            {
                _session.SendPlayerState(PlayerState.NotReady);
                _state = AppState.Menu;
                _menu.ShowRoot("multiplayer_lobby");
            }
            else
            {
                _state = AppState.Menu;
                _menu.ShowRoot("main");
                _menu.FadeInMenuMusic();
            }
        }

        private void UpdatePaused()
        {
            if (!_raceInput.GetPause() && !_pauseKeyReleased)
            {
                _pauseKeyReleased = true;
                return;
            }

            if (_raceInput.GetPause() && _pauseKeyReleased)
            {
                _pauseKeyReleased = false;
                switch (_pausedState)
                {
                    case AppState.TimeTrial:
                        _timeTrial?.Unpause();
                        _timeTrial?.StopStopwatchDiff();
                        _state = AppState.TimeTrial;
                        break;
                    case AppState.SingleRace:
                        _singleRace?.Unpause();
                        _singleRace?.StopStopwatchDiff();
                        _state = AppState.SingleRace;
                        break;
                }
            }
        }

        private void EnterPause(AppState state)
        {
            _pausedState = state;
            _pauseKeyReleased = false;
            switch (_pausedState)
            {
                case AppState.TimeTrial:
                    _timeTrial?.StartStopwatchDiff();
                    _timeTrial?.Pause();
                    _timeTrial?.ClearPauseRequest();
                    _state = AppState.Paused;
                    break;
                case AppState.SingleRace:
                    _singleRace?.StartStopwatchDiff();
                    _singleRace?.Pause();
                    _singleRace?.ClearPauseRequest();
                    _state = AppState.Paused;
                    break;
            }
        }

        private void StartRace(RaceMode mode)
        {
            FadeOutMenuMusic();
            var track = string.IsNullOrWhiteSpace(_setup.TrackNameOrFile)
                ? TrackList.RaceTracks[0].Key
                : _setup.TrackNameOrFile!;
            var vehicleIndex = _setup.VehicleIndex ?? 0;
            var vehicleFile = _setup.VehicleFile;
            var automatic = _setup.Transmission == TransmissionMode.Automatic;

            switch (mode)
            {
                case RaceMode.TimeTrial:
                    _timeTrial?.FinalizeLevelTimeTrial();
                    _timeTrial?.Dispose();
                    _timeTrial = new LevelTimeTrial(_audio, _speech, _settings, _raceInput, track, automatic, _settings.NrOfLaps, vehicleIndex, vehicleFile, _input.VibrationDevice);
                    _timeTrial.Initialize();
                    _state = AppState.TimeTrial;
                    _speech.Speak("Time trial.");
                    break;
                case RaceMode.QuickStart:
                case RaceMode.SingleRace:
                    _singleRace?.FinalizeLevelSingleRace();
                    _singleRace?.Dispose();
                    _singleRace = new LevelSingleRace(_audio, _speech, _settings, _raceInput, track, automatic, _settings.NrOfLaps, vehicleIndex, vehicleFile, _input.VibrationDevice);
                    _singleRace.Initialize(Algorithm.RandomInt(_settings.NrOfComputers + 1));
                    _state = AppState.SingleRace;
                    _speech.Speak(mode == RaceMode.QuickStart ? "Quick start." : "Single race.");
                    break;
                case RaceMode.Exploration:
                    _explore?.Dispose();
                    _explore = new LevelExplore(_audio, _speech, _settings, _input, track);
                    _explore.Initialize();
                    _state = AppState.Exploration;
                    _speech.Speak("Exploration mode.");
                    break;
            }
        }

        private void EndRace()
        {
            _timeTrial?.FinalizeLevelTimeTrial();
            _timeTrial?.Dispose();
            _timeTrial = null;

            _singleRace?.FinalizeLevelSingleRace();
            _singleRace?.Dispose();
            _singleRace = null;

            _state = AppState.Menu;
            _menu.ShowRoot("main");
            _menu.FadeInMenuMusic();
        }

        private void EndExploration()
        {
            _explore?.Dispose();
            _explore = null;
            _state = AppState.Menu;
            _menu.ShowRoot("main");
            _menu.FadeInMenuMusic();
        }

        private void SyncAudioLoopState()
        {
            var shouldRun = IsRaceState(_state);
            if (shouldRun && !_audioLoopActive)
            {
                _audio.StartUpdateThread(8);
                _audioLoopActive = true;
            }
            else if (!shouldRun && _audioLoopActive)
            {
                _audio.StopUpdateThread();
                _audioLoopActive = false;
            }
        }

        private static bool IsRaceState(AppState state)
        {
            return state == AppState.TimeTrial
                || state == AppState.SingleRace
                || state == AppState.MultiplayerRace
                || state == AppState.Exploration
                || state == AppState.Paused;
        }

        private static bool IsMenuState(AppState state)
        {
            return state == AppState.Logo
                || state == AppState.Menu
                || state == AppState.Calibration;
        }

        public void Dispose()
        {
            _logo?.Dispose();
            _explore?.Dispose();
            _menu.Dispose();
            _input.Dispose();
            _session?.Dispose();
            _speech.Dispose();
            _audio.Dispose();
        }

        public void FadeOutMenuMusic(int durationMs = 1000)
        {
            _menu.FadeOutMenuMusic(durationMs);
        }

        private void SaveSettings()
        {
            _settingsManager.Save(_settings);
        }

        private void SaveMusicVolume(float volume)
        {
            _settings.MusicVolume = volume;
            SaveSettings();
        }

        void IMenuActions.SaveMusicVolume(float volume) => SaveMusicVolume(volume);
        void IMenuActions.QueueRaceStart(RaceMode mode) => QueueRaceStart(mode);
        void IMenuActions.StartServerDiscovery() => _multiplayerCoordinator.StartServerDiscovery();
        void IMenuActions.BeginManualServerEntry() => _multiplayerCoordinator.BeginManualServerEntry();
        void IMenuActions.DisconnectFromServer() => DisconnectFromServer();
        void IMenuActions.SpeakNotImplemented() => _speech.Speak("Not implemented yet.");
        void IMenuActions.BeginServerPortEntry() => _multiplayerCoordinator.BeginServerPortEntry();
        void IMenuActions.RestoreDefaults() => RestoreDefaults();
        void IMenuActions.RecalibrateScreenReaderRate() => StartCalibrationSequence("options_game");
        void IMenuActions.SetDevice(InputDeviceMode mode) => SetDevice(mode);
        void IMenuActions.ToggleCurveAnnouncements() => ToggleCurveAnnouncements();
        void IMenuActions.ToggleSetting(Action update) => ToggleSetting(update);
        void IMenuActions.UpdateSetting(Action update) => UpdateSetting(update);
        void IMenuActions.BeginMapping(InputMappingMode mode, InputAction action) => _inputMapping.BeginMapping(mode, action);
        string IMenuActions.FormatMappingValue(InputAction action, InputMappingMode mode) => _inputMapping.FormatMappingValue(action, mode);
    }
}
