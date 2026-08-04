using System;
using System.Net;
using TopSpeed.Bots;
using TopSpeed.Data;
using TopSpeed.Protocol;

namespace TopSpeed.Server.Network
{
    internal sealed class PlayerConnection
    {
        public PlayerConnection(IPEndPoint endPoint, uint id, ulong resumeToken)
        {
            EndPoint = endPoint ?? throw new ArgumentNullException(nameof(endPoint));
            Id = id;
            ResumeToken = resumeToken;
            RemoteAddress = endPoint.Address;
            Frequency = ProtocolConstants.DefaultFrequency;
            State = PlayerState.NotReady;
            Name = string.Empty;
            LastHeartbeatUtc = DateTime.UtcNow;
            WidthM = 1.8f;
            LengthM = 4.5f;
            MassKg = 1500f;
            Handshake = HandshakeState.Pending;
            NegotiatedProtocol = ProtocolProfile.ServerSupported.MaxSupported;
            RadioVolumePercent = 100;
            ConnectionEpoch = 1;
            LifecycleState = ConnectionLifecycleState.TransportConnected;
            GameConnectionState = MultiplayerConnectionState.Connecting;
            LastDisconnectReason = MultiplayerDisconnectReason.Unknown;
        }

        public IPEndPoint EndPoint { get; private set; }
        public uint Id { get; }
        public IPAddress RemoteAddress { get; private set; }
        public uint ConnectionEpoch { get; private set; }
        public uint? RoomId { get; set; }
        public byte PlayerNumber { get; set; }
        public CarType Car { get; set; }

        // Custom vehicle package selected by this player (empty when using a built-in vehicle).
        public string SelectedVehicleHash { get; set; } = string.Empty;
        public float PositionX { get; set; }
        public float PositionY { get; set; }
        public ushort Speed { get; set; }
        public int Frequency { get; set; }
        public PlayerState State { get; set; }
        public string Name { get; set; }
        public bool ServerPresenceAnnounced { get; set; }
        public bool EngineRunning { get; set; }
        public bool Braking { get; set; }
        public bool Horning { get; set; }
        public bool Backfiring { get; set; }
        public bool MediaLoaded { get; set; }
        public bool MediaPlaying { get; set; }
        public uint MediaId { get; set; }
        public byte RadioVolumePercent { get; set; }
        public InMedia? IncomingMedia { get; set; }
        public InMedia? IncomingCommunicatorMedia { get; set; }
        public MediaBlob? CommunicatorMediaBlob { get; set; }
        public CommunicatorMediaState? CommunicatorMedia { get; set; }
        public LiveState? Live { get; set; }
        public VoiceState? Voice { get; set; }
        public DateTime LastHeartbeatUtc { get; set; }
        public DateTime? SuspendedUtc { get; private set; }
        public ConnectionLifecycleState LifecycleState { get; private set; }
        public MultiplayerConnectionState GameConnectionState { get; private set; }
        public MultiplayerDisconnectReason LastDisconnectReason { get; private set; }
        public uint LastClientHeartbeatTick { get; set; }
        public uint LastClientObservedServerTick { get; set; }
        public bool Connected =>
            LifecycleState == ConnectionLifecycleState.TransportConnected
            || LifecycleState == ConnectionLifecycleState.ProtocolNegotiated
            || LifecycleState == ConnectionLifecycleState.PlayerIdentified
            || LifecycleState == ConnectionLifecycleState.SessionReady
            || LifecycleState == ConnectionLifecycleState.InRoom
            || LifecycleState == ConnectionLifecycleState.Active
            || LifecycleState == ConnectionLifecycleState.Resumed;
        public ulong ResumeToken { get; private set; }
        public float WidthM { get; set; }
        public float LengthM { get; set; }
        public float MassKg { get; set; }
        public HandshakeState Handshake { get; set; }
        public ProtocolVer NegotiatedProtocol { get; set; }
        public ProtocolRange? ClientSupportedRange { get; set; }
        public ProtocolVer ClientVersion { get; set; }

        public void Rebind(IPEndPoint endPoint)
        {
            EndPoint = endPoint ?? throw new ArgumentNullException(nameof(endPoint));
            RemoteAddress = endPoint.Address;
            ConnectionEpoch++;
            if (ConnectionEpoch == 0)
                ConnectionEpoch = 1;
            LifecycleState = ConnectionLifecycleState.Resumed;
            GameConnectionState = MultiplayerConnectionState.Connected;
            LastDisconnectReason = MultiplayerDisconnectReason.Unknown;
            SuspendedUtc = null;
            LastHeartbeatUtc = DateTime.UtcNow;
        }

        public void UpdateTransportEndPoint(IPEndPoint endPoint)
        {
            EndPoint = endPoint ?? throw new ArgumentNullException(nameof(endPoint));
            RemoteAddress = endPoint.Address;
            LastHeartbeatUtc = DateTime.UtcNow;
        }

        public void MarkProtocolNegotiated()
        {
            LifecycleState = ConnectionLifecycleState.ProtocolNegotiated;
            LastHeartbeatUtc = DateTime.UtcNow;
        }

        public void MarkPlayerIdentified()
        {
            LifecycleState = RoomId.HasValue
                ? ConnectionLifecycleState.InRoom
                : ConnectionLifecycleState.PlayerIdentified;
            LastHeartbeatUtc = DateTime.UtcNow;
        }

        public void MarkSessionReady()
        {
            LifecycleState = RoomId.HasValue
                ? ConnectionLifecycleState.InRoom
                : ConnectionLifecycleState.SessionReady;
            SuspendedUtc = null;
            LastHeartbeatUtc = DateTime.UtcNow;
        }

        public void MarkInRoom()
        {
            LifecycleState = ConnectionLifecycleState.InRoom;
            SuspendedUtc = null;
            LastHeartbeatUtc = DateTime.UtcNow;
        }

        public void MarkActive()
        {
            LifecycleState = ConnectionLifecycleState.Active;
            GameConnectionState = MultiplayerConnectionState.Connected;
            LastDisconnectReason = MultiplayerDisconnectReason.Unknown;
            SuspendedUtc = null;
            LastHeartbeatUtc = DateTime.UtcNow;
        }

        public void MarkSuspended()
        {
            LifecycleState = ConnectionLifecycleState.SuspectedLost;
            GameConnectionState = MultiplayerConnectionState.ConnectionLostSuspected;
            SuspendedUtc = DateTime.UtcNow;
            IncomingMedia = null;
            IncomingCommunicatorMedia = null;
        }

        public void MarkReconnecting()
        {
            LifecycleState = ConnectionLifecycleState.Reconnecting;
        }

        public void MarkConnected()
        {
            LifecycleState = ConnectionLifecycleState.Active;
            GameConnectionState = MultiplayerConnectionState.Connected;
            LastDisconnectReason = MultiplayerDisconnectReason.Unknown;
            SuspendedUtc = null;
            LastHeartbeatUtc = DateTime.UtcNow;
        }

        public void MarkExpired()
        {
            LifecycleState = ConnectionLifecycleState.Expired;
            GameConnectionState = MultiplayerConnectionState.TimedOut;
        }

        public void MarkClosed()
        {
            LifecycleState = ConnectionLifecycleState.Closed;
            ResumeToken = 0;
            SuspendedUtc = null;
            IncomingMedia = null;
            IncomingCommunicatorMedia = null;
            CommunicatorMediaBlob = null;
            CommunicatorMedia = null;
        }

        public void SetDisconnectOutcome(MultiplayerDisconnectReason reason, MultiplayerConnectionState state)
        {
            LastDisconnectReason = reason;
            GameConnectionState = state;
        }

        public PacketPlayerData ToPacket()
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
                State = State,
                EngineRunning = EngineRunning,
                Braking = Braking,
                Horning = Horning,
                Backfiring = Backfiring,
                MediaLoaded = MediaLoaded,
                MediaPlaying = MediaPlaying,
                MediaId = MediaId,
                RadioVolumePercent = RadioVolumePercent
            };
        }
    }

}
