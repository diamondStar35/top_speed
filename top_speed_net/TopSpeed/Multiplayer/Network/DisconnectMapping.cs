using LiteNetLib;
using TopSpeed.Localization;
using TopSpeed.Protocol;

namespace TopSpeed.Network
{
    internal readonly struct ClientDisconnectClassification
    {
        public ClientDisconnectClassification(
            MultiplayerDisconnectReason reason,
            MultiplayerConnectionState state,
            bool shouldAttemptReconnect,
            string message)
        {
            Reason = reason;
            State = state;
            ShouldAttemptReconnect = shouldAttemptReconnect;
            Message = message ?? string.Empty;
        }

        public MultiplayerDisconnectReason Reason { get; }
        public MultiplayerConnectionState State { get; }
        public bool ShouldAttemptReconnect { get; }
        public string Message { get; }
    }

    internal static class DisconnectMapping
    {
        public static ClientDisconnectClassification ForClient(DisconnectReason reason)
        {
            return reason switch
            {
                DisconnectReason.Timeout => new ClientDisconnectClassification(
                    MultiplayerDisconnectReason.TimedOut,
                    MultiplayerConnectionState.TimedOut,
                    shouldAttemptReconnect: true,
                    LocalizationService.Mark("Connection timed out.")),
                DisconnectReason.RemoteConnectionClose => new ClientDisconnectClassification(
                    MultiplayerDisconnectReason.DisconnectedCleanly,
                    MultiplayerConnectionState.DisconnectedCleanly,
                    shouldAttemptReconnect: true,
                    LocalizationService.Mark("Connection closed by server.")),
                DisconnectReason.DisconnectPeerCalled => new ClientDisconnectClassification(
                    MultiplayerDisconnectReason.LocalDisconnect,
                    MultiplayerConnectionState.DisconnectedCleanly,
                    shouldAttemptReconnect: false,
                    LocalizationService.Mark("Disconnected.")),
                DisconnectReason.InvalidProtocol => new ClientDisconnectClassification(
                    MultiplayerDisconnectReason.ProtocolError,
                    MultiplayerConnectionState.ProtocolError,
                    shouldAttemptReconnect: false,
                    LocalizationService.Mark("Connection closed due to protocol error.")),
                DisconnectReason.ConnectionRejected => new ClientDisconnectClassification(
                    MultiplayerDisconnectReason.ConnectionRejected,
                    MultiplayerConnectionState.ProtocolError,
                    shouldAttemptReconnect: false,
                    LocalizationService.Mark("Connection rejected by server.")),
                DisconnectReason.ConnectionFailed => new ClientDisconnectClassification(
                    MultiplayerDisconnectReason.ConnectionFailed,
                    MultiplayerConnectionState.ConnectionLostSuspected,
                    shouldAttemptReconnect: true,
                    LocalizationService.Mark("Unable to reach server.")),
                DisconnectReason.NetworkUnreachable => new ClientDisconnectClassification(
                    MultiplayerDisconnectReason.NetworkError,
                    MultiplayerConnectionState.ConnectionLostSuspected,
                    shouldAttemptReconnect: true,
                    LocalizationService.Mark("Network is unreachable.")),
                DisconnectReason.HostUnreachable => new ClientDisconnectClassification(
                    MultiplayerDisconnectReason.HostUnreachable,
                    MultiplayerConnectionState.ConnectionLostSuspected,
                    shouldAttemptReconnect: true,
                    LocalizationService.Mark("Host is unreachable.")),
                DisconnectReason.UnknownHost => new ClientDisconnectClassification(
                    MultiplayerDisconnectReason.UnknownHost,
                    MultiplayerConnectionState.ConnectionLostSuspected,
                    shouldAttemptReconnect: false,
                    LocalizationService.Mark("Unable to resolve server host.")),
                DisconnectReason.PeerNotFound => new ClientDisconnectClassification(
                    MultiplayerDisconnectReason.PeerNotFound,
                    MultiplayerConnectionState.ConnectionLostSuspected,
                    shouldAttemptReconnect: true,
                    LocalizationService.Mark("Peer session was not found on server.")),
                DisconnectReason.Reconnect => new ClientDisconnectClassification(
                    MultiplayerDisconnectReason.ReconnectRequested,
                    MultiplayerConnectionState.ConnectionLostSuspected,
                    shouldAttemptReconnect: true,
                    LocalizationService.Mark("Connection changed. Reconnecting.")),
                DisconnectReason.PeerToPeerConnection => new ClientDisconnectClassification(
                    MultiplayerDisconnectReason.ProtocolError,
                    MultiplayerConnectionState.ProtocolError,
                    shouldAttemptReconnect: false,
                    LocalizationService.Mark("Unsupported peer-to-peer mode.")),
                _ => new ClientDisconnectClassification(
                    MultiplayerDisconnectReason.Unknown,
                    MultiplayerConnectionState.ConnectionLostSuspected,
                    shouldAttemptReconnect: true,
                    LocalizationService.Mark("Connection lost."))
            };
        }
    }
}
