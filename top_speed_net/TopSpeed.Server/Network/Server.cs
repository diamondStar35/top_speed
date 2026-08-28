using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using Bogus;
using TopSpeed.Protocol;
using TopSpeed.Server.Logging;
using TopSpeed.Server.Config;

namespace TopSpeed.Server.Network
{
    internal sealed partial class RaceServer : IDisposable
    {
        private const float ServerSimulationStepSeconds = 0.008f;
        private const float ServerSnapshotIntervalSeconds = 1f / 60f;
        private const float CleanupIntervalSeconds = 1.0f;
        private const float BotRaceStartDelaySeconds = 6.5f;
        private const float BotHornMinDistanceMeters = 100.0f;
        private const float BotBackfirePulseSeconds = 0.1f;
        private static readonly TimeSpan ConnectTimeout = ConnectionRecoveryRules.DefaultConnectTimeout;
        private static readonly TimeSpan HeartbeatMissWindow = ConnectionRecoveryRules.DefaultHeartbeatMissWindow;
        private static readonly TimeSpan ReconnectGrace = ConnectionRecoveryRules.DefaultReconnectGrace;

        private readonly RaceServerConfig _config;
        private readonly Logger _logger;
        private readonly object _lock = new object();
        private readonly ServerCommandBus _commandBus = new ServerCommandBus();
        private readonly UdpServerTransport _transport;
        private readonly ServerPktReg _pktReg;
        private readonly ISessionService _session;
        private readonly IRuntimeService _runtime;
        private readonly IRoomService _room;
        private readonly IRaceService _race;
        private readonly IMediaService _media;
        private readonly ILiveService _live;
        private readonly IVoiceService _voice;
        private readonly IChatService _chat;
        private readonly INotifyService _notify;
        private readonly Dictionary<uint, PlayerConnection> _players = new Dictionary<uint, PlayerConnection>();
        private readonly Dictionary<string, uint> _endpointIndex = new Dictionary<string, uint>(StringComparer.OrdinalIgnoreCase);
        private readonly ConcurrentDictionary<string, uint> _endpointEpochIndex = new ConcurrentDictionary<string, uint>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<uint, GameRoom> _rooms = new Dictionary<uint, GameRoom>();
        private readonly Dictionary<string, PackageRecord> _trackPackageCache = new Dictionary<string, PackageRecord>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<uint, PackageUploadSession> _trackPackageUploads = new Dictionary<uint, PackageUploadSession>();
        private readonly Faker _faker = new Faker();
        private readonly Random _random = new Random();

        private uint _nextPlayerId = 1;
        private uint _nextBotId = 1_000_000;
        private float _simulationAccumulator;
        private float _snapshotAccumulator;
        private float _cleanupAccumulator;
        private uint _simulationTick;
        private int _serverLoopThreadId;
        private int _authorityDropsPlayerState;
        private int _authorityDropsPlayerData;
        private int _authorityDropsPlayerStarted;
        private int _authorityDropsPlayerCrashed;
        private int _joinDeniedGameInProgress;
        private int _roomMutationDenied;
        private int _raceSnapshotSends;
        private int _stateSyncFramesSent;
        private int _bumpEventsHumanHuman;
        private int _bumpEventsHumanBot;
        private int _botCrashEvents;
        private int _botRestartEvents;
        private int _botResumeEvents;
        private int _botStartEvents;
        private int _botFinishEvents;
        private int _botHornOvertakeEvents;
        private int _botHornBumpEvents;
        private int _droppedPacketsStaleEpoch;
        private int _droppedPacketsInvalidHeader;
        private int _droppedPacketsVersionMismatch;
        private int _droppedPacketsUnknownCommand;
        private int _replayGapCount;
        private int _epochRejectCount;
        private int _heartbeatSuspicionCount;
        private int _startBarrierBlockedInsufficientActive;
        private int _startBarrierBlockedMissingReady;
        private int _startBarrierBlockedTrackNotReady;
        private int _transportNetworkErrorCount;
        private int _transportLastLatencyMs;
        private int _transportMaxLatencyMs;
        private long _transportLatencyTotalMs;
        private int _transportLatencySampleCount;
        private int _transportPeerAddressChangedCount;

        public RaceServer(RaceServerConfig config, Logger logger)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _config.Moderation ??= new ServerModerationSettings();
            _config.Features ??= new ServerFeaturesSettings();
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _transport = new UdpServerTransport(_logger);
            _pktReg = new ServerPktReg();
            _session = new Session(this);
            _runtime = new Runtime(this);
            _room = new Room(this);
            _race = new Race(this);
            _media = new Media(this);
            _live = new Live(this);
            _voice = new Voice(this);
            _chat = new Chat(this);
            _notify = new Notify(this);
            RegisterPackets();
            _transport.PacketReceived += OnPacket;
            _transport.PeerDisconnected += OnPeerDisconnected;
            _transport.NetworkError += OnNetworkError;
            _transport.PeerLatencyUpdated += OnPeerLatencyUpdated;
            _transport.PeerAddressChanged += OnPeerAddressChanged;
        }
    }
}


