using System;
using System.Collections.Generic;
using SoundFlow.Abstracts.Devices;
using SoundFlow.Backends.MiniAudio;
using TopSpeed.Audio;
using TopSpeed.Core.Multiplayer;
using TopSpeed.Drive.Multiplayer;
using TopSpeed.Input;
using TopSpeed.Protocol;
using TopSpeed.Runtime;
using TopSpeed.Vehicles;
using TS.Audio;

namespace TopSpeed.Game.Multiplayer.Communicator
{
    internal sealed partial class MultiplayerCommunicatorRuntime : IDisposable
    {
        private const int MaxCapturedSamples = ProtocolConstants.VoiceSampleRate * 2;
        private const int MaxQueuedRemoteFrames = 16;
        private const int MediaVolumeStepPercent = 10;

        private readonly AudioManager _audio;
        private readonly DriveSettings _settings;
        private readonly IMultiplayerRuntime _multiplayer;
        private readonly IInputService _input;
        private readonly Func<Network.MultiplayerSession?> _getSession;
        private readonly IFileDialogs _fileDialogs;
        private readonly Action<string> _announce;
        private readonly Action _saveSettings;
        private readonly Func<string, bool> _isShortcutHeld;
        private readonly Func<bool> _isInputBlocked;
        private readonly object _captureLock = new object();
        private readonly object _mediaSelectionLock = new object();
        private readonly List<short> _capturedSamples = new List<short>(ProtocolConstants.VoiceSampleRate);
        private readonly Dictionary<uint, RemoteVoiceStream> _remoteStreams = new Dictionary<uint, RemoteVoiceStream>();
        private readonly Dictionary<uint, MediaTransfer> _remoteMediaTransfers = new Dictionary<uint, MediaTransfer>();
        private readonly Dictionary<uint, PacketPlayerCommunicatorMediaState> _remoteMediaStates = new Dictionary<uint, PacketPlayerCommunicatorMediaState>();
        private readonly Dictionary<uint, VehicleRadioController> _remoteMediaControllers = new Dictionary<uint, VehicleRadioController>();
        private readonly List<string> _mediaPlaylist = new List<string>();
        private readonly HashSet<string> _pressedShortcutActions = new HashSet<string>(StringComparer.Ordinal);
        private readonly Random _random = new Random();
        private readonly VoiceEncoder _encoder = new VoiceEncoder();
        private readonly short[] _captureFrame = new short[ProtocolConstants.VoiceSampleRate * ProtocolConstants.VoiceFrameMs / 1000];
        private readonly VehicleRadioController _localMedia;

        private MiniAudioEngine? _captureEngine;
        private AudioCaptureDevice? _captureDevice;
        private string _captureDeviceName = string.Empty;
        private int _captureChannels = 1;
        private int _captureInputSampleRate = ProtocolConstants.VoiceSampleRate;
        private long _captureSourceFrameIndex;
        private double _captureNextOutputSourceFrame;
        private bool _captureHasPreviousSample;
        private float _capturePreviousSample;

        private Network.MultiplayerSession? _boundSession;
        private bool _transmitting;
        private bool _micCueOpen;
        private uint _streamId;
        private uint _nextStreamId = 1;
        private uint _nextMediaId = 1;
        private ushort _activeFrequencyTenths;
        private bool _activePushToTalk;
        private uint _timestampMs;
        private bool _mediaPickerInProgress;
        private bool _mediaFolderPickerInProgress;
        private string? _pendingSelectedMediaPath;
        private string? _pendingSelectedMediaFolder;
        private string _mediaPlaylistFolder = string.Empty;
        private int _mediaPlaylistIndex = -1;
        private bool _mediaShuffleMode;
        private bool _mediaLoopMode;
        private bool _lastObservedMediaPlaying;
        private bool _lastSentMediaStateValid;
        private bool _lastSentMediaLoaded;
        private bool _lastSentMediaPlaying;
        private uint _lastSentMediaId;
        private ushort _lastSentMediaFrequencyTenths;
        private byte _lastSentMediaVolumePercent = 100;

        private SoundAsset? _micOpenSound;
        private SoundAsset? _micCloseSound;
        private SoundAsset? _volumeUpSound;
        private SoundAsset? _volumeDownSound;
        private readonly SoundAsset?[] _pttCues = new SoundAsset?[12];

        public MultiplayerCommunicatorRuntime(
            AudioManager audio,
            DriveSettings settings,
            IMultiplayerRuntime multiplayer,
            IInputService input,
            Func<Network.MultiplayerSession?> getSession,
            IFileDialogs fileDialogs,
            Action<string> announce,
            Action saveSettings,
            Func<string, bool>? isShortcutHeld = null,
            Func<bool>? isInputBlocked = null)
        {
            _audio = audio ?? throw new ArgumentNullException(nameof(audio));
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _multiplayer = multiplayer ?? throw new ArgumentNullException(nameof(multiplayer));
            _input = input ?? throw new ArgumentNullException(nameof(input));
            _getSession = getSession ?? throw new ArgumentNullException(nameof(getSession));
            _fileDialogs = fileDialogs ?? throw new ArgumentNullException(nameof(fileDialogs));
            _announce = announce ?? throw new ArgumentNullException(nameof(announce));
            _saveSettings = saveSettings ?? throw new ArgumentNullException(nameof(saveSettings));
            _isShortcutHeld = isShortcutHeld ?? (_ => false);
            _isInputBlocked = isInputBlocked ?? (() => false);
            _mediaShuffleMode = _settings.RadioShuffle;
            _mediaLoopMode = false;
            _localMedia = new VehicleRadioController(
                _audio,
                new VehicleRadioController.PlaybackOptions(
                    AudioEngineOptions.RadioBusName,
                    spatialize: false,
                    allowHrtf: false,
                    _settings,
                    AudioVolumeCategory.Communicator,
                    "Communicator"));
            TryRestoreMediaFolderPlaylist();
            ApplyMediaLoopMode();
        }

        public void BindSession(Network.MultiplayerSession? session)
        {
            if (ReferenceEquals(_boundSession, session))
                return;

            Disarm();
            ResetLocalMediaTransmissionState();
            if (session == null)
                PauseLocalMedia();
            _boundSession = session;
            ClearRemoteStreams();
            ClearRemoteMedia();
        }

        public void Dispose()
        {
            Disarm();
            _boundSession = null;
            ClearRemoteStreams();
            ClearRemoteMedia();
            _localMedia.Dispose();
            DisposeCachedSounds();
        }
    }
}
