using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using LiteNetLib;
using TopSpeed.Localization;
using TopSpeed.Protocol;

namespace TopSpeed.Network
{
    internal sealed partial class MultiplayerConnector
    {
        public Task<ConnectResult> ConnectAsync(string host, int port, string callSign, TimeSpan timeout, CancellationToken token)
        {
            return ConnectAsync(host, port, callSign, timeout, token, resumePlayerId: 0, resumeToken: 0);
        }

        public async Task<ConnectResult> ConnectAsync(string host, int port, string callSign, TimeSpan timeout, CancellationToken token, uint resumePlayerId, ulong resumeToken)
        {
            if (string.IsNullOrWhiteSpace(host))
                return ConnectResult.CreateFail(LocalizationService.Mark("No server address was provided."));

            var resolve = await Task.Run(() => TryResolveHost(host), token).ConfigureAwait(false);
            if (!resolve.Success)
                return ConnectResult.CreateFail(resolve.Error);

            var address = resolve.Address!;
            if (port <= 0 || port > 65535)
                port = ClientProtocol.DefaultServerPort;

            var sanitizedCallSign = SanitizeCallSign(callSign);
            var endpoint = new IPEndPoint(address, port);

            var incoming = new ConcurrentQueue<IncomingPacket>();
            var listener = new EventBasedNetListener();
            NetPeer? connectedPeer = null;
            var disconnected = false;
            var disconnectReason = string.Empty;
            var disconnectClassification = new ClientDisconnectClassification(
                MultiplayerDisconnectReason.Unknown,
                MultiplayerConnectionState.ConnectionLostSuspected,
                shouldAttemptReconnect: true,
                LocalizationService.Mark("Connection lost."));
            var hasDisconnectClassification = false;
            SocketError? lastNetworkSocketError = null;
            var latestLatencyMs = 0;
            MultiplayerSession? activeSession = null;
            var sawProtocolPacketVersionMismatch = false;
            var remotePacketVersion = (byte)0;

            listener.PeerConnectedEvent += peer => connectedPeer = peer;
            listener.PeerDisconnectedEvent += (_, info) =>
            {
                disconnected = true;
                disconnectReason = info.Reason.ToString();
                disconnectClassification = DisconnectMapping.ForClient(info.Reason);
                hasDisconnectClassification = true;
                incoming.Enqueue(new IncomingPacket(
                    Command.Disconnect,
                    new[] { ProtocolConstants.Version, (byte)Command.Disconnect },
                    DateTime.UtcNow.Ticks,
                    disconnectClassification.Reason,
                    disconnectClassification.State,
                    hasDisconnectClassification: true));
            };
            listener.NetworkErrorEvent += (_, socketError) =>
            {
                lastNetworkSocketError = socketError;
            };
            listener.NetworkLatencyUpdateEvent += (_, latency) =>
            {
                latestLatencyMs = latency;
                activeSession?.ApplyTransportPing(latency);
            };
            listener.NetworkReceiveEvent += (_, reader, _, _) =>
            {
                var data = reader.GetRemainingBytes();
                reader.Recycle();
                if (data.Length > 0 && data[0] != ProtocolConstants.Version)
                {
                    sawProtocolPacketVersionMismatch = true;
                    remotePacketVersion = data[0];
                    return;
                }

                if (ClientPacketSerializer.TryReadHeader(data, out var command))
                    incoming.Enqueue(new IncomingPacket(command, data, DateTime.UtcNow.Ticks));
            };

            var manager = new NetManager(listener)
            {
                UpdateTime = 1,
                PingInterval = 1000,
                DisconnectTimeout = 10000,
                DisconnectOnUnreachable = false,
                ReconnectDelay = 500,
                MaxConnectAttempts = 20,
                UnsyncedEvents = false,
                UnsyncedReceiveEvent = false,
                UnsyncedDeliveryEvent = false,
                AutoRecycle = false,
                EnableStatistics = true,
                MtuOverride = 0,
                MtuDiscovery = false,
                UseNativeSockets = false,
                ChannelsCount = PacketStreams.Count
            };

            if (!manager.Start())
                return ConnectResult.CreateFail(LocalizationService.Mark("Failed to initialize network client."));

            manager.Connect(endpoint.Address.ToString(), endpoint.Port, ProtocolConstants.ConnectionKey);

            var protocolHello = BuildProtocolHelloPacket(resumePlayerId, resumeToken);
            var hello = BuildPlayerHelloPacket(sanitizedCallSign);
            var keepAlive = new[] { ProtocolConstants.Version, (byte)Command.KeepAlive };
            var protocolHelloSent = false;
            var protocolNegotiated = false;
            var nextKeepAliveUtc = DateTime.UtcNow;
            byte? playerNumber = null;
            uint? playerId = null;
            string? motd = null;
            PacketProtocolWelcome? protocolWelcome = null;
            string? protocolFailureMessage = null;
            var deadline = DateTime.UtcNow + timeout;

            while (DateTime.UtcNow < deadline && !token.IsCancellationRequested)
            {
                manager.PollEvents();

                if (disconnected && connectedPeer == null)
                {
                    manager.Stop();
                    var baseMessage = hasDisconnectClassification
                        ? disconnectClassification.Message
                        : LocalizationService.Format(LocalizationService.Mark("Connection failed: {0}"), disconnectReason);
                    if (lastNetworkSocketError.HasValue)
                    {
                        baseMessage = LocalizationService.Format(
                            LocalizationService.Mark("{0} Socket error: {1}."),
                            baseMessage,
                            lastNetworkSocketError.Value);
                    }

                    if (hasDisconnectClassification)
                    {
                        return ConnectResult.CreateFail(
                            baseMessage,
                            disconnectClassification.Reason,
                            disconnectClassification.State);
                    }

                    return ConnectResult.CreateFail(baseMessage);
                }

                if (!protocolHelloSent && connectedPeer != null && connectedPeer.ConnectionState == ConnectionState.Connected)
                {
                    var sendHelloResult = TrySendHandshakePacket(connectedPeer, protocolHello);
                    if (!sendHelloResult.Success)
                    {
                        manager.Stop();
                        return ConnectResult.CreateFail(sendHelloResult.Error);
                    }

                    protocolHelloSent = true;
                    nextKeepAliveUtc = DateTime.UtcNow + TimeSpan.FromSeconds(1);
                }

                if (protocolHelloSent && connectedPeer != null && DateTime.UtcNow >= nextKeepAliveUtc)
                {
                    TrySendKeepAlive(connectedPeer, keepAlive);
                    nextKeepAliveUtc = DateTime.UtcNow + TimeSpan.FromSeconds(1);
                }

                var poll = ProcessConnectPoll(
                    incoming,
                    manager,
                    connectedPeer,
                    hello,
                    protocolNegotiated,
                    playerId,
                    playerNumber,
                    motd,
                    protocolWelcome,
                    protocolFailureMessage,
                    disconnectReason,
                    hasDisconnectClassification ? disconnectClassification : (ClientDisconnectClassification?)null,
                    lastNetworkSocketError,
                    sanitizedCallSign,
                    endpoint);

                protocolNegotiated = poll.ProtocolNegotiated;
                playerId = poll.PlayerId;
                playerNumber = poll.PlayerNumber;
                motd = poll.Motd;
                protocolWelcome = poll.ProtocolWelcome;
                protocolFailureMessage = poll.ProtocolFailureMessage;

                if (poll.Result.HasValue)
                {
                    if (poll.Result.Value.Success && poll.Result.Value.Session != null)
                    {
                        poll.Result.Value.Session.ApplyTransportPing(latestLatencyMs);
                        activeSession = poll.Result.Value.Session;
                    }

                    return poll.Result.Value;
                }

                await Task.Delay(10, token).ConfigureAwait(false);
            }

            manager.Stop();
            if (token.IsCancellationRequested)
                return ConnectResult.CreateFail(LocalizationService.Mark("Connection attempt canceled."));

            if (protocolHelloSent && !protocolNegotiated)
            {
                if (sawProtocolPacketVersionMismatch)
                {
                    return ConnectResult.CreateFail(LocalizationService.Format(
                        LocalizationService.Mark("Protocol packet version mismatch. Server uses packet version {0}, client expects {1}. Update your client or server."),
                        remotePacketVersion,
                        ProtocolConstants.Version),
                        MultiplayerDisconnectReason.ProtocolError,
                        MultiplayerConnectionState.ProtocolError);
                }

                return ConnectResult.CreateFail(
                    LocalizationService.Mark("No protocol negotiation response from server. The server may be outdated or incompatible."),
                    MultiplayerDisconnectReason.ProtocolError,
                    MultiplayerConnectionState.ProtocolError);
            }

            return ConnectResult.CreateFail(
                LocalizationService.Mark("No response from server. The server may be offline or unreachable."),
                MultiplayerDisconnectReason.ConnectionFailed,
                MultiplayerConnectionState.ConnectionLostSuspected);
        }
    }
}

