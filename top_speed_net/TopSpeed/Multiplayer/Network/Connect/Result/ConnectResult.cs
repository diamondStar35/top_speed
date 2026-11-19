using System.Collections.Concurrent;
using System.Net;
using LiteNetLib;
using TopSpeed.Localization;
using TopSpeed.Protocol;

namespace TopSpeed.Network
{
    internal readonly struct CompatibilityNotice
    {
        public CompatibilityNotice(ProtocolCompatStatus status, ProtocolVer clientVersion, ProtocolRange serverSupported, string message)
        {
            Status = status;
            ClientVersion = clientVersion;
            ServerSupported = serverSupported;
            Message = message ?? string.Empty;
        }

        public ProtocolCompatStatus Status { get; }
        public ProtocolVer ClientVersion { get; }
        public ProtocolRange ServerSupported { get; }
        public string Message { get; }
    }

    internal readonly struct ConnectResult
    {
        private ConnectResult(
            bool success,
            string message,
            MultiplayerSession? session,
            string? motd,
            CompatibilityNotice? compatibilityNotice,
            MultiplayerDisconnectReason disconnectReason,
            MultiplayerConnectionState connectionState)
        {
            Success = success;
            Message = message;
            Session = session;
            Address = session?.Address;
            Port = session?.Port ?? 0;
            PlayerNumber = session?.PlayerNumber ?? 0;
            PlayerId = session?.PlayerId ?? 0;
            Motd = motd ?? string.Empty;
            CompatibilityNotice = compatibilityNotice;
            DisconnectReason = disconnectReason;
            ConnectionState = connectionState;
        }

        public bool Success { get; }
        public string Message { get; }
        public MultiplayerSession? Session { get; }
        public IPAddress? Address { get; }
        public int Port { get; }
        public byte PlayerNumber { get; }
        public uint PlayerId { get; }
        public string Motd { get; }
        public CompatibilityNotice? CompatibilityNotice { get; }
        public bool RequiresCompatibilityConfirmation => CompatibilityNotice.HasValue && CompatibilityNotice.Value.Status == ProtocolCompatStatus.CompatibleDowngrade;
        public MultiplayerDisconnectReason DisconnectReason { get; }
        public MultiplayerConnectionState ConnectionState { get; }

        public static ConnectResult CreateSuccess(
            NetManager manager,
            NetPeer? peer,
            IPEndPoint endPoint,
            uint playerId,
            byte playerNumber,
            string? motd,
            string? playerName,
            ConcurrentQueue<IncomingPacket> incoming,
            PacketProtocolWelcome? protocolWelcome)
        {
            if (peer == null)
            {
                manager.Stop();
                return CreateFail(LocalizationService.Mark("Connection lost before session initialization."));
            }

            var session = new MultiplayerSession(manager, peer, endPoint, playerId, playerNumber, protocolWelcome?.ResumeToken ?? 0, motd, playerName, incoming);
            session.ApplyDisconnectClassification(MultiplayerDisconnectReason.Unknown, MultiplayerConnectionState.Connected);
            return new ConnectResult(
                true,
                LocalizationService.Mark("Connected."),
                session,
                motd,
                BuildCompatibilityNotice(protocolWelcome),
                MultiplayerDisconnectReason.Unknown,
                MultiplayerConnectionState.Connected);
        }

        public static ConnectResult CreateFail(
            string message,
            MultiplayerDisconnectReason disconnectReason = MultiplayerDisconnectReason.Unknown,
            MultiplayerConnectionState connectionState = MultiplayerConnectionState.ConnectionLostSuspected)
        {
            return new ConnectResult(
                false,
                message ?? LocalizationService.Mark("Connection failed."),
                null,
                null,
                null,
                disconnectReason,
                connectionState);
        }

        private static CompatibilityNotice? BuildCompatibilityNotice(PacketProtocolWelcome? welcome)
        {
            if (welcome == null)
                return null;
            if (welcome.Status != ProtocolCompatStatus.CompatibleDowngrade)
                return null;

            var range = new ProtocolRange(welcome.ServerMinSupported, welcome.ServerMaxSupported);
            var message = string.IsNullOrWhiteSpace(welcome.Message)
                ? LocalizationService.Mark("The server supports an older protocol range. You can continue, but some features may behave differently.")
                : welcome.Message;
            return new CompatibilityNotice(
                welcome.Status,
                ProtocolProfile.Current,
                range,
                message);
        }
    }
}

