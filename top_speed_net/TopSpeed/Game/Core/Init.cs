using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using TopSpeed.Audio;
using TopSpeed.Core;
using TopSpeed.Core.Multiplayer;
using TopSpeed.Core.Settings;
using TopSpeed.Core.Updates;
using TopSpeed.Data;
using TopSpeed.Drive.TimeTrial;
using TopSpeed.Input;
using TopSpeed.Localization;
using TopSpeed.Menu;
using TopSpeed.Network;
using TopSpeed.Runtime;
using TopSpeed.Shortcuts;
using TopSpeed.Speech;

namespace TopSpeed.Game
{
    internal sealed partial class Game
    {
        public Game(IWindowHost window, ITextInputService textInput, IFileDialogs fileDialogs, IClipboardService clipboard)
        {
            _window = window ?? throw new ArgumentNullException(nameof(window));
            _textInput = textInput ?? throw new ArgumentNullException(nameof(textInput));
            _fileDialogs = fileDialogs ?? throw new ArgumentNullException(nameof(fileDialogs));
            _clipboard = clipboard ?? throw new ArgumentNullException(nameof(clipboard));
            _settingsManager = new SettingsManager();
            var settingsLoad = _settingsManager.Load();
            _settings = settingsLoad.Settings;
            _settingsIssues = settingsLoad.Issues;
            _settingsFileMissing = settingsLoad.SettingsFileMissing;
            _clientLanguages = ClientLanguages.Load();
            _settings.Language = ClientLanguages.ResolveCode(_settings.Language, _clientLanguages);
            LocalizationBootstrap.Configure(_settings.Language, LocalizationBootstrap.ClientCatalogGroup);
            var audio = new AudioManager(_settings.HrtfAudio, _settings.AutoDetectAudioDeviceFormat);
            _isAndroidPlatform = RuntimeInformation.IsOSPlatform(OSPlatform.Create("ANDROID"));
            var keyboardFactories = new List<IKeyboardBackendFactory>
            {
#if WINDOWS
                new TopSpeed.Input.Devices.Keyboard.Backends.DirectInput.Factory(),
                new TopSpeed.Input.Devices.Keyboard.Backends.Sdl.Factory()
#else
                new TopSpeed.Input.Devices.Keyboard.Backends.Eto.Factory(),
                new TopSpeed.Input.Devices.Keyboard.Backends.Sdl.Factory()
#endif
            };
            var controllerFactories = new List<IControllerBackendFactory>();
            if (_isAndroidPlatform)
                controllerFactories.Add(new TopSpeed.Input.Backends.Sdl.Factory(_window as TS.Sdl.Input.IControllerEventSource));
            else
                controllerFactories.Add(new TopSpeed.Input.Backends.Sdl.Factory());

            var backendRegistry = new BackendRegistry(
                keyboardFactories,
                controllerFactories);
            var input = new InputService(
                _window.NativeHandle,
                backendRegistry,
                _window as IKeyboardEventSource,
                _window as IGestureEventSource);
            var speech = new SpeechService(audio, input.IsAnyInputHeld, input.PrepareForInterruptableSpeech);
            _audio = audio;
            _input = input;
            _speech = speech;
            speech.ScreenReaderRateMs = _settings.ScreenReaderRateMs;
            speech.OutputMode = _settings.SpeechMode;
            speech.SpeechRate = _settings.SpeechRate;
            speech.ScreenReaderInterrupt = _settings.ScreenReaderInterrupt;
            speech.PreferredBackendId = _settings.SpeechBackendId;
            speech.PreferredVoiceIndex = _settings.SpeechVoiceIndex;
            _driveInput = new DriveInput(_settings);
            _setup = new DriveSetup();
            _driveSessionFactory = new DriveSessionFactory(audio, speech, _settings, _driveInput, _fileDialogs);
            _stateMachine = new StateMachine(this);
            _menu = new MenuManager(audio, speech, () => _settings.UsageHints);
            _dialogs = new DialogManager(_menu, message => speech.Speak(message));
            _choices = new ChoiceDialogManager(_menu, message => speech.Speak(message));
            var pick = new Pick();
            var fmt = new ResultFmt(pick);
            var resultDialogs = new ResultDialogs(pick, fmt);
            _resultShow = new ResultShow(dialog => _dialogs.Show(dialog), PlayRaceWinSound, resultDialogs);
            _menu.SetWrapNavigation(_settings.MenuWrapNavigation);
            _menu.SetMenuSoundPreset(_settings.MenuSoundPreset);
            _menu.SetMenuNavigatePanning(_settings.MenuNavigatePanning);
            _menu.SetMenuAutoFocus(_settings.MenuAutoFocus);
            _selection = new DriveSelection(_setup, _settings);
            _menuRegistry = new MenuRegistry(_menu, _settings, _setup, _driveInput, _selection, this, this, this, this, this, this);
            _inputMapping = new InputMappingHandler(
                input,
                _driveInput,
                _settings,
                speech,
                SaveSettings,
                (title, caption, items, cancelable, cancelLabel, onResult) => ShowChoiceDialog(
                    title,
                    caption,
                    items,
                    cancelable,
                    cancelLabel,
                    result => onResult(result.IsCanceled, result.ChoiceId)));
            _shortcutMapping = new ShortcutMappingHandler(input, _menu, _settings, speech, SaveSettings);
            _updateConfig = UpdateConfig.Default;
            _updateService = new UpdateService(_updateConfig);
            CleanupAndroidUpdatePackages();
            ApplyUpdateProxySettings();
            _multiplayerConnector = new MultiplayerConnector();
            _sessionReconnector = new SessionReconnector(_multiplayerConnector);
            var multiplayerCoordinator = new MultiplayerCoordinator(
                _menu,
                _dialogs,
                audio,
                speech,
                _settings,
                _multiplayerConnector,
                BeginPromptTextInput,
                text => _clipboard.TrySetText(text),
                SaveSettings,
                EnterMenuState,
                SetSession,
                GetSession,
                ClearSession,
                ResetPendingMultiplayerState,
                SetMultiplayerLoadout);
            _multiplayerCoordinator = multiplayerCoordinator;
            _multiplayerMenuTouch = multiplayerCoordinator;
            _multiplayerCommunicatorRuntime = new Multiplayer.Communicator.MultiplayerCommunicatorRuntime(
                audio,
                _settings,
                multiplayerCoordinator,
                input,
                GetSession,
                _fileDialogs,
                text => _speech.Speak(text),
                SaveSettings,
                IsShortcutActionHeld,
                () => _textInputPromptActive || _inputMapping.IsActive || _shortcutMapping.IsActive);
            _multiplayerRaceRuntime = new MultiplayerRaceRuntime(this);
            _multiplayerDispatch = new MultiplayerDispatch(this);
            _menuRegistry.RegisterAll();
            _multiplayerCoordinator.ConfigureMenuCloseHandlers();
            RegisterGlobalShortcutActions();
            ApplySavedShortcutBindings();
            _settings.AudioVolumes ??= new AudioVolumeSettings();
            _settings.SyncAudioCategoriesFromMusicVolume();
            ApplyAudioSettings();
            _audio.StartUpdateThread(8);
            _needsCalibration = _settings.UsageHints && _settings.ScreenReaderRateMs <= 0f;
            input.NoControllerDetected += HandleNoControllerDetected;
            input.ControllerBackendUnavailable += HandleControllerBackendUnavailable;
            input.SetDeviceMode(_settings.DeviceMode);
        }

        public void Initialize()
        {
            if (_settings.PlayLogoAtStartup)
            {
                _logo = new LogoScreen((AudioManager)_audio);
                _logo.Start();
            }
            _state = AppState.Logo;
        }
    }
}




