using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using TopSpeed.Server.Logging;
using TopSpeed.Server.Protocol;
using TopSpeed.Server.Tracks;
using TopSpeed.Data;
using TopSpeed.Protocol;

namespace TopSpeed.Server.Network
{
    internal sealed class PlayerConnection
    {
        public IPEndPoint EndPoint { get; }
        public uint Id { get; }
        public byte PlayerNumber { get; set; }
        public CarType Car { get; set; }
        public int PositionX { get; set; }
        public int PositionY { get; set; }
        public ushort Speed { get; set; }
        public int Frequency { get; set; }
        public PlayerState State { get; set; }
        public string Name { get; set; }
        public bool EngineRunning { get; set; }
        public bool Braking { get; set; }
        public bool Horning { get; set; }
        public bool Backfiring { get; set; }
        public DateTime LastSeenUtc { get; set; }
        public bool JoinBroadcasted { get; set; }

        public PlayerConnection(IPEndPoint endPoint, uint id)
        {
            EndPoint = endPoint;
            Id = id;
            PlayerNumber = 0;
            Car = CarType.Vehicle1;
            PositionX = 0;
            PositionY = 0;
            Speed = 0;
            Frequency = ProtocolConstants.DefaultFrequency;
            State = PlayerState.NotReady;
            Name = string.Empty;
            EngineRunning = false;
            Braking = false;
            Horning = false;
            Backfiring = false;
            LastSeenUtc = DateTime.UtcNow;
            JoinBroadcasted = false;
        }

        public PacketPlayerData ToPacket(PlayerState overrideState)
        {
            return new PacketPlayerData
            {
                PlayerId = Id,
                PlayerNumber = PlayerNumber,
                Car = Car,
                RaceData = new PlayerRaceData
                {
                    PositionX = PositionX,
                    PositionY = PositionY,
                    Speed = Speed,
                    Frequency = Frequency
                },
                State = overrideState,
                EngineRunning = EngineRunning,
                Braking = Braking,
                Horning = Horning,
                Backfiring = Backfiring
            };
        }
    }

    internal sealed class RaceServer : IDisposable
    {
        private const float ServerUpdateTime = 0.1f;
        private static readonly TimeSpan ConnectionTimeout = TimeSpan.FromSeconds(10);

        private readonly RaceServerConfig _config;
        private readonly Logger _logger;
        private readonly Dictionary<uint, PlayerConnection> _players = new Dictionary<uint, PlayerConnection>();
        private readonly Dictionary<string, uint> _endpointIndex = new Dictionary<string, uint>(StringComparer.OrdinalIgnoreCase);
        private readonly object _lock = new object();
        private readonly UdpServerTransport _transport;

        private uint _nextId = 1;
        private float _lastUpdateTime;
        private bool _raceStarted;
        private bool _trackSelected;
        private TrackData? _trackData;
        private string _trackName = string.Empty;
        private readonly List<byte> _raceResults = new List<byte>();

        public RaceServer(RaceServerConfig config, Logger logger)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _transport = new UdpServerTransport(_logger);
            _transport.PacketReceived += OnPacketReceived;
        }

        public void Start()
        {
            _transport.Start(_config.Port);
            _logger.Info("Race server started.");
        }

        public void Stop()
        {
            lock (_lock)
            {
                _players.Clear();
                _endpointIndex.Clear();
                _raceStarted = false;
                _trackSelected = false;
                _trackData = null;
            }
            _transport.Stop();
            _logger.Info("Race server stopped.");
        }

        public void Update(float deltaSeconds)
        {
            lock (_lock)
            {
                _lastUpdateTime += deltaSeconds;
                if (_lastUpdateTime < ServerUpdateTime)
                    return;
                _lastUpdateTime = 0.0f;

                CleanupConnections();
                BroadcastPlayerData();
                CheckForBumps();
            }
        }

        public void LoadTrack(string trackName, byte defaultLaps)
        {
            if (string.IsNullOrWhiteSpace(trackName))
                throw new ArgumentException("Track name required.", nameof(trackName));

            var data = TrackLoader.LoadTrack(trackName, defaultLaps);
            LoadCustomTrack(trackName, data);
        }

        public void LoadCustomTrack(string trackName, TrackData data)
        {
            if (string.IsNullOrWhiteSpace(trackName))
                throw new ArgumentException("Track name required.", nameof(trackName));
            if (data == null)
                throw new ArgumentNullException(nameof(data));

            lock (_lock)
            {
                _trackName = trackName;
                _trackData = data;
                _trackSelected = true;
                SendTrackToNotReady();
                _logger.Info($"Track loaded: {trackName}.");
            }
        }

        public void StartRace()
        {
            lock (_lock)
            {
                if (_raceStarted)
                    return;
                _raceStarted = true;
                _raceResults.Clear();
                SendGeneral(Command.StartRace);
                _logger.Info("Race started.");
            }
        }

        public void StopRace(byte[]? results = null)
        {
            lock (_lock)
            {
                _raceStarted = false;
                _trackSelected = false;
                _trackData = null;
                _trackName = string.Empty;

                var finalResults = results ?? _raceResults.ToArray();
                var packet = new PacketRaceResults
                {
                    Results = finalResults,
                    NPlayers = (byte)Math.Min(finalResults.Length, ProtocolConstants.MaxPlayers)
                };
                Send(PacketSerializer.WriteRaceResults(packet));
                _logger.Info("Race stopped.");
            }
        }

        public void AbortRace()
        {
            lock (_lock)
            {
                _raceStarted = false;
                SendGeneral(Command.RaceAborted);
                _logger.Warning("Race aborted.");
            }
        }

        private void OnPacketReceived(IPEndPoint endPoint, byte[] payload)
        {
            if (!PacketSerializer.TryReadHeader(payload, out var header))
                return;
            if (header.Version != ProtocolConstants.Version)
                return;

            lock (_lock)
            {
            var connection = GetOrAddConnection(endPoint);
            if (connection == null)
                return;
            connection.LastSeenUtc = DateTime.UtcNow;

                switch (header.Command)
                {
                    case Command.PlayerDataToServer:
                        if (PacketSerializer.TryReadPlayerData(payload, out var playerData))
                            HandlePlayerData(connection, playerData);
                        break;
                    case Command.PlayerState:
                        if (PacketSerializer.TryReadPlayerState(payload, out var playerState))
                            HandlePlayerState(connection, playerState);
                        break;
                    case Command.PlayerHello:
                        if (PacketSerializer.TryReadPlayerHello(payload, out var hello))
                            HandlePlayerHello(connection, hello);
                        break;
                    case Command.KeepAlive:
                        break;
                    case Command.PlayerFinished:
                        if (PacketSerializer.TryReadPlayer(payload, out var finished))
                            HandlePlayerFinished(connection, finished);
                        break;
                    case Command.PlayerFinalize:
                        if (PacketSerializer.TryReadPlayerState(payload, out var finalize))
                            HandlePlayerFinalize(connection, finalize);
                        break;
                    case Command.PlayerStarted:
                        if (PacketSerializer.TryReadPlayer(payload, out var started))
                            HandlePlayerStarted(connection, started);
                        break;
                    case Command.PlayerCrashed:
                        if (PacketSerializer.TryReadPlayer(payload, out var crashed))
                            HandlePlayerCrashed(connection, crashed);
                        break;
                }
            }
        }

        private PlayerConnection? GetOrAddConnection(IPEndPoint endPoint)
        {
            var key = endPoint.ToString();
            if (_endpointIndex.TryGetValue(key, out var id) && _players.TryGetValue(id, out var existing))
                return existing;

            var playerNumber = FindFreePlayerNumber();
            if (playerNumber < 0)
            {
                SendDisconnect(endPoint);
                _logger.Warning("Server full. Connection refused.");
                return null;
            }

            var connectionId = _nextId++;
            var connection = new PlayerConnection(endPoint, connectionId)
            {
                PlayerNumber = (byte)playerNumber
            };
            _players[connectionId] = connection;
            _endpointIndex[key] = connectionId;

            var packet = PacketSerializer.WritePlayerNumber(connectionId, connection.PlayerNumber);
            _transport.Send(endPoint, packet);
            _logger.Info($"Player connected: id={connectionId}, number={connection.PlayerNumber}.");

            if (!string.IsNullOrWhiteSpace(_config.Motd))
            {
                var info = new PacketServerInfo { Motd = _config.Motd };
                _transport.Send(endPoint, PacketSerializer.WriteServerInfo(info));
            }

            if (_trackSelected)
            {
                if (_raceStarted)
                    _transport.Send(endPoint, PacketSerializer.WriteGeneral(Command.StartRace));
                else
                    SendTrackTo(connection);
            }

            return connection;
        }

        private int FindFreePlayerNumber()
        {
            var maxPlayers = Math.Min(_config.MaxPlayers, ProtocolConstants.MaxPlayers);
            for (var i = 0; i < maxPlayers; i++)
            {
                if (_players.Values.All(p => p.PlayerNumber != i))
                    return i;
            }
            return -1;
        }

        private void HandlePlayerData(PlayerConnection connection, PacketPlayerData packet)
        {
            connection.Car = packet.Car;
            connection.PlayerNumber = packet.PlayerNumber;
            connection.PositionX = packet.RaceData.PositionX;
            connection.PositionY = packet.RaceData.PositionY;
            connection.Speed = packet.RaceData.Speed;
            connection.Frequency = packet.RaceData.Frequency;
            connection.EngineRunning = packet.EngineRunning;
            connection.Braking = packet.Braking;
            connection.Horning = packet.Horning;
            connection.Backfiring = packet.Backfiring;
        }

        private void HandlePlayerState(PlayerConnection connection, PacketPlayerState packet)
        {
            if (packet.State == PlayerState.NotReady && connection.State != PlayerState.NotReady && _trackSelected)
            {
                SendTrackTo(connection);
            }
            connection.State = packet.State;
        }

        private void HandlePlayerHello(PlayerConnection connection, PacketPlayerHello packet)
        {
            var name = (packet.Name ?? string.Empty).Trim();
            if (name.Length > ProtocolConstants.MaxPlayerNameLength)
                name = name.Substring(0, ProtocolConstants.MaxPlayerNameLength);
            connection.Name = name;
            if (!connection.JoinBroadcasted)
            {
                connection.JoinBroadcasted = true;
                var displayName = string.IsNullOrWhiteSpace(name)
                    ? $"Player {connection.PlayerNumber + 1}"
                    : name;
                var joined = new PacketPlayerJoined
                {
                    PlayerId = connection.Id,
                    PlayerNumber = connection.PlayerNumber,
                    Name = displayName
                };
                SendExcept(connection.Id, PacketSerializer.WritePlayerJoined(joined));
            }
        }

        private void HandlePlayerFinished(PlayerConnection connection, PacketPlayer packet)
        {
            _raceResults.Add(packet.PlayerNumber);
            SendExcept(connection.Id, PacketSerializer.WritePlayer(Command.PlayerFinished, packet.PlayerId, packet.PlayerNumber));
            if (CountRacers() == 0)
                StopRace();
        }

        private void HandlePlayerFinalize(PlayerConnection connection, PacketPlayerState packet)
        {
            SendExcept(connection.Id, PacketSerializer.WritePlayerState(Command.PlayerFinalize, packet.PlayerId, packet.PlayerNumber, packet.State));
            if (_raceStarted)
                _transport.Send(connection.EndPoint, PacketSerializer.WriteGeneral(Command.StartRace));
        }

        private void HandlePlayerStarted(PlayerConnection connection, PacketPlayer packet)
        {
            SendExcept(connection.Id, PacketSerializer.WritePlayer(Command.PlayerStarted, packet.PlayerId, packet.PlayerNumber));
        }

        private void HandlePlayerCrashed(PlayerConnection connection, PacketPlayer packet)
        {
            SendToRacersExcept(connection.Id, PacketSerializer.WritePlayer(Command.PlayerCrashed, packet.PlayerId, packet.PlayerNumber));
        }

        private void BroadcastPlayerData()
        {
            foreach (var player in _players.Values)
            {
                if (player.State == PlayerState.Undefined || player.State == PlayerState.NotReady)
                    continue;
                var dataPacket = player.ToPacket(player.State);
                var payload = PacketSerializer.WritePlayerData(dataPacket);
                SendToRacersExcept(player.Id, payload);
            }
        }

        private void CheckForBumps()
        {
            var racers = _players.Values.Where(p => p.State == PlayerState.Racing).ToList();
            for (var i = 0; i < racers.Count; i++)
            {
                for (var j = 0; j < racers.Count; j++)
                {
                    if (i == j)
                        continue;
                    var player = racers[i];
                    var other = racers[j];
                    if (Math.Abs(player.PositionX - other.PositionX) < 1000 && Math.Abs(player.PositionY - other.PositionY) < 500)
                    {
                        var bump = new PacketPlayerBumped
                        {
                            PlayerId = player.Id,
                            PlayerNumber = player.PlayerNumber,
                            BumpX = player.PositionX - other.PositionX,
                            BumpY = player.PositionY - other.PositionY,
                            BumpSpeed = (ushort)Math.Max(0, player.Speed - other.Speed)
                        };
                        _transport.Send(player.EndPoint, PacketSerializer.WritePlayerBumped(bump));
                    }
                }
            }
        }

        private void SendTrackToNotReady()
        {
            foreach (var player in _players.Values)
            {
                if (player.State == PlayerState.NotReady)
                    SendTrackTo(player);
            }
        }

        private void SendTrackTo(PlayerConnection connection)
        {
            if (_trackData == null)
                return;

            var trackLength = (ushort)Math.Min(_trackData.Definitions.Length, ProtocolConstants.MaxMultiTrackLength);
            var packet = new PacketLoadCustomTrack
            {
                NrOfLaps = _trackData.Laps,
                TrackName = _trackData.UserDefined ? "custom" : _trackName,
                TrackWeather = _trackData.Weather,
                TrackAmbience = _trackData.Ambience,
                TrackLength = trackLength,
                Definitions = _trackData.Definitions
            };
            _transport.Send(connection.EndPoint, PacketSerializer.WriteLoadCustomTrack(packet));
        }

        private void SendGeneral(Command command)
        {
            Send(PacketSerializer.WriteGeneral(command));
        }

        private void Send(byte[] payload)
        {
            foreach (var player in _players.Values)
                _transport.Send(player.EndPoint, payload);
        }

        private void SendExcept(uint playerId, byte[] payload)
        {
            foreach (var player in _players.Values)
            {
                if (player.Id == playerId)
                    continue;
                _transport.Send(player.EndPoint, payload);
            }
        }

        private void SendToRacersExcept(uint playerId, byte[] payload)
        {
            foreach (var player in _players.Values)
            {
                if (player.Id == playerId)
                    continue;
                if (player.State != PlayerState.Racing)
                    continue;
                _transport.Send(player.EndPoint, payload);
            }
        }

        private int CountRacers()
        {
            var count = 0;
            foreach (var player in _players.Values)
            {
                if (player.State == PlayerState.Racing)
                    count++;
            }
            return count;
        }

        private void CleanupConnections()
        {
            var expired = new List<uint>();
            foreach (var pair in _players)
            {
                if (DateTime.UtcNow - pair.Value.LastSeenUtc > ConnectionTimeout)
                    expired.Add(pair.Key);
            }

            foreach (var id in expired)
            {
                if (_players.TryGetValue(id, out var player))
                {
                    _logger.Warning($"Player timeout: id={id}.");
                    SendDisconnect(player.EndPoint);
                    SendExcept(player.Id, PacketSerializer.WritePlayer(Command.PlayerDisconnected, player.Id, player.PlayerNumber));
                    _endpointIndex.Remove(player.EndPoint.ToString());
                    _players.Remove(id);
                    if (_raceStarted && CountRacers() == 0)
                        StopRace();
                }
            }
        }

        private void SendDisconnect(IPEndPoint endPoint)
        {
            _transport.Send(endPoint, PacketSerializer.WriteGeneral(Command.Disconnect));
        }

        public ServerSnapshot GetSnapshot()
        {
            lock (_lock)
            {
                var name = _config.Name ?? "TopSpeed Server";
                var trackName = _trackData != null && _trackData.UserDefined ? "custom" : _trackName;
                return new ServerSnapshot(
                    name,
                    _config.Port,
                    _config.MaxPlayers,
                    _players.Count,
                    _raceStarted,
                    _trackSelected,
                    trackName);
            }
        }

        public void Dispose()
        {
            _transport.Dispose();
        }
    }
}
