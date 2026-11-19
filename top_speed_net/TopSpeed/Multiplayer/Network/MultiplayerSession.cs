using System;
using System.Collections.Concurrent;
using System.Net;
using System.Threading;
using LiteNetLib;
using TopSpeed.Network.Session;
using TopSpeed.Protocol;

namespace TopSpeed.Network
{
    internal sealed partial class MultiplayerSession : IDisposable
    {
        private static readonly TimeSpan HeartbeatTimeoutWindow = TimeSpan.FromSeconds(10);

        private readonly NetManager _manager;
        private readonly IPEndPoint _serverEndPoint;
        private readonly ConcurrentQueue<IncomingPacket> _incoming;
        private readonly Sender _sender;
        private readonly Media _media;
        private readonly LiveSend _live;
        private readonly VoiceSend _voice;
        private readonly Loop _loop;
        private Action<IncomingPacket>? _packetSink;
        private Action<bool>? _voiceTransmissionSink;
        private byte _playerNumber;
        private int _disconnectPacketQueued;
        private uint _heartbeatTick;
        private uint _lastServerHeartbeatTick;
        private uint _lastServerHeartbeatEchoClientTick;
        private int _lastKnownPingMs;
        private bool _voiceTransmissionOpen;
        private DateTime _lastServerHeartbeatUtc;
        private DateTime _lastHeartbeatProbeUtc;

        public MultiplayerSession(
            NetManager manager,
            NetPeer peer,
            IPEndPoint serverEndPoint,
            uint playerId,
            byte playerNumber,
            ulong resumeToken,
            string? motd,
            string? playerName,
            ConcurrentQueue<IncomingPacket> incoming)
        {
            _manager = manager ?? throw new ArgumentNullException(nameof(manager));
            _serverEndPoint = serverEndPoint ?? throw new ArgumentNullException(nameof(serverEndPoint));
            _incoming = incoming ?? throw new ArgumentNullException(nameof(incoming));
            _sender = new Sender(peer ?? throw new ArgumentNullException(nameof(peer)));
            _media = new Media(_sender);
            _live = new LiveSend(_sender);
            _voice = new VoiceSend(_sender);
            PlayerId = playerId;
            ResumeToken = resumeToken;
            _playerNumber = playerNumber;
            Motd = motd ?? string.Empty;
            PlayerName = playerName ?? string.Empty;
            _lastServerHeartbeatUtc = DateTime.UtcNow;
            _lastHeartbeatProbeUtc = DateTime.UtcNow;
            _loop = new Loop(PollEventsSafe, DrainIncomingToSink, SendKeepAlive);
        }

        public IPAddress Address => _serverEndPoint.Address;
        public int Port => _serverEndPoint.Port;
        public uint PlayerId { get; }
        public ulong ResumeToken { get; }
        public byte PlayerNumber => _playerNumber;
        public string Motd { get; }
        public string PlayerName { get; }
        public bool IsConnected => _sender.IsConnected;
        public MultiplayerConnectionState ConnectionState { get; private set; } = MultiplayerConnectionState.Connected;
        public MultiplayerDisconnectReason LastDisconnectReason { get; private set; } = MultiplayerDisconnectReason.Unknown;
        public int LastKnownPingMs => _lastKnownPingMs;
        public uint LastServerHeartbeatTick => _lastServerHeartbeatTick;

        public void UpdatePlayerNumber(byte playerNumber)
        {
            _playerNumber = playerNumber;
        }

        public bool TryDequeuePacket(out IncomingPacket packet)
        {
            return _incoming.TryDequeue(out packet);
        }

        private void QueueSyntheticDisconnectIfNeeded()
        {
            if (_sender.IsConnected)
                return;

            QueueSyntheticDisconnect(
                MultiplayerDisconnectReason.ConnectionFailed,
                MultiplayerConnectionState.ConnectionLostSuspected);
        }

        private void QueueSyntheticDisconnect(
            MultiplayerDisconnectReason disconnectReason,
            MultiplayerConnectionState connectionState)
        {
            if (Interlocked.Exchange(ref _disconnectPacketQueued, 1) != 0)
                return;

            ApplyDisconnectClassification(disconnectReason, connectionState);
            _incoming.Enqueue(new IncomingPacket(
                Command.Disconnect,
                new[] { ProtocolConstants.Version, (byte)Command.Disconnect },
                DateTime.UtcNow.Ticks,
                disconnectReason,
                connectionState,
                hasDisconnectClassification: true));
        }

        public void ApplyDisconnectClassification(MultiplayerDisconnectReason reason, MultiplayerConnectionState state)
        {
            LastDisconnectReason = reason;
            ConnectionState = state;
        }

        public void ApplyTransportPing(int latencyMs)
        {
            _lastKnownPingMs = latencyMs;
        }

        public void ApplyServerHeartbeat(PacketServerHeartbeat heartbeat)
        {
            if (heartbeat == null)
                return;

            _lastServerHeartbeatTick = heartbeat.ServerTick;
            _lastServerHeartbeatEchoClientTick = heartbeat.LastReceivedClientTick;
            _lastServerHeartbeatUtc = DateTime.UtcNow;
        }

        private void EvaluateHeartbeatTimeout()
        {
            if (!_sender.IsConnected)
                return;

            if (_heartbeatTick == 0)
                return;

            var now = DateTime.UtcNow;
            if (now - _lastServerHeartbeatUtc <= HeartbeatTimeoutWindow)
                return;

            if (now - _lastHeartbeatProbeUtc < TimeSpan.FromSeconds(2))
                return;

            QueueSyntheticDisconnect(MultiplayerDisconnectReason.TimedOut, MultiplayerConnectionState.TimedOut);
        }

        public void SetPacketSink(Action<IncomingPacket>? packetSink)
        {
            _packetSink = packetSink;
            if (packetSink != null)
                DrainIncomingToSink();
        }

        public void SetVoiceTransmissionSink(Action<bool>? sink)
        {
            _voiceTransmissionSink = sink;
        }

        private void NotifyVoiceTransmissionState(bool opened)
        {
            if (_voiceTransmissionOpen == opened)
                return;

            _voiceTransmissionOpen = opened;
            _voiceTransmissionSink?.Invoke(opened);
        }

        public void Dispose()
        {
            NotifyVoiceTransmissionState(false);
            _voiceTransmissionSink = null;
            _live.Reset();
            _voice.Reset();
            _loop.Dispose();
            _manager.Stop();
        }
    }

    internal readonly struct IncomingPacket
    {
        public IncomingPacket(Command command, byte[] payload)
            : this(
                command,
                payload,
                0,
                MultiplayerDisconnectReason.Unknown,
                MultiplayerConnectionState.ConnectionLostSuspected,
                hasDisconnectClassification: false)
        {
        }

        public IncomingPacket(Command command, byte[] payload, long receivedUtcTicks)
            : this(
                command,
                payload,
                receivedUtcTicks,
                MultiplayerDisconnectReason.Unknown,
                MultiplayerConnectionState.ConnectionLostSuspected,
                hasDisconnectClassification: false)
        {
        }

        public IncomingPacket(
            Command command,
            byte[] payload,
            long receivedUtcTicks,
            MultiplayerDisconnectReason disconnectReason,
            MultiplayerConnectionState connectionState,
            bool hasDisconnectClassification)
        {
            Command = command;
            Payload = payload;
            ReceivedUtcTicks = receivedUtcTicks;
            DisconnectReason = disconnectReason;
            ConnectionState = connectionState;
            HasDisconnectClassification = hasDisconnectClassification;
        }

        public Command Command { get; }
        public byte[] Payload { get; }
        public long ReceivedUtcTicks { get; }
        public MultiplayerDisconnectReason DisconnectReason { get; }
        public MultiplayerConnectionState ConnectionState { get; }
        public bool HasDisconnectClassification { get; }
    }
}

