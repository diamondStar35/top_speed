using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using TopSpeed.Server.Logging;
using TopSpeed.Protocol;

namespace TopSpeed.Server.Network
{
    internal sealed class ServerDiscoveryService : IDisposable
    {
        private readonly RaceServer _server;
        private readonly RaceServerConfig _config;
        private readonly Logger _logger;
        private UdpClient? _client;
        private CancellationTokenSource? _cts;
        private Task? _receiveTask;

        public ServerDiscoveryService(RaceServer server, RaceServerConfig config, Logger logger)
        {
            _server = server ?? throw new ArgumentNullException(nameof(server));
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public void Start()
        {
            if (_client != null)
                return;

            _client = new UdpClient(AddressFamily.InterNetwork);
            _client.EnableBroadcast = true;
            _client.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            _client.Client.ReceiveBufferSize = 1024 * 1024;
            _client.Client.SendBufferSize = 1024 * 1024;
            _client.Client.Bind(new IPEndPoint(IPAddress.Any, _config.DiscoveryPort));
            _cts = new CancellationTokenSource();
            _receiveTask = Task.Run(() => ReceiveLoop(_cts.Token));
            _logger.Info($"Discovery service listening on {_config.DiscoveryPort}.");
        }

        public void Stop()
        {
            if (_client == null)
                return;
            _cts?.Cancel();
            _client.Close();
            _client.Dispose();
            _client = null;
            _logger.Info("Discovery service stopped.");
        }

        private async Task ReceiveLoop(CancellationToken token)
        {
            if (_client == null)
                return;

            while (!token.IsCancellationRequested)
            {
                try
                {
                    var result = await _client.ReceiveAsync(token);
                    if (!DiscoveryProtocol.TryParseRequest(result.Buffer, out var version))
                        continue;
                    if (version != ProtocolConstants.Version)
                        continue;

                    var snapshot = _server.GetSnapshot();
                    var response = DiscoveryProtocol.BuildResponse(snapshot);
                    await _client.SendAsync(response, response.Length, result.RemoteEndPoint);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.Warning($"Discovery receive failed: {ex.Message}");
                }
            }
        }

        public void Dispose()
        {
            Stop();
            _cts?.Dispose();
        }
    }
}
