using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TopSpeed.Protocol;

namespace TopSpeed.Network
{
    internal sealed class DiscoveryClient : IDisposable
    {
        private static readonly byte[] RequestMagic =
        {
            (byte)'T', (byte)'S', (byte)'D', (byte)'I', (byte)'S', (byte)'C', (byte)'O', (byte)'V', (byte)'E', (byte)'R', (byte)'Y'
        };

        private static readonly byte[] ResponseMagic =
        {
            (byte)'T', (byte)'S', (byte)'S', (byte)'E', (byte)'R', (byte)'V', (byte)'E', (byte)'R'
        };

        private const int MaxNameLength = 32;
        private const int MaxTrackLength = 32;

        private UdpClient? _client;

        public async Task<IReadOnlyList<ServerInfo>> ScanAsync(int discoveryPort, TimeSpan timeout, CancellationToken token)
        {
            var results = new Dictionary<string, ServerInfo>(StringComparer.OrdinalIgnoreCase);

            _client = new UdpClient(AddressFamily.InterNetwork);
            _client.EnableBroadcast = true;
            _client.Client.ReceiveBufferSize = 1024 * 1024;
            _client.Client.SendBufferSize = 1024 * 1024;
            _client.Client.Bind(new IPEndPoint(IPAddress.Any, 0));

            var request = BuildRequest();
            var broadcast = new IPEndPoint(IPAddress.Broadcast, discoveryPort);
            await _client.SendAsync(request, request.Length, broadcast);

            var deadline = DateTime.UtcNow + timeout;
            while (DateTime.UtcNow < deadline && !token.IsCancellationRequested)
            {
                var remaining = deadline - DateTime.UtcNow;
                if (remaining <= TimeSpan.Zero)
                    break;

                var delayTask = Task.Delay(remaining, token);
                var receiveTask = _client.ReceiveAsync();
                var completed = await Task.WhenAny(receiveTask, delayTask);
                if (completed != receiveTask)
                    break;

                var result = receiveTask.Result;
                if (TryParseResponse(result.Buffer, result.RemoteEndPoint, out var server))
                {
                    var key = $"{server.Address}:{server.Port}";
                    results[key] = server;
                }
            }

            return new List<ServerInfo>(results.Values);
        }

        private static byte[] BuildRequest()
        {
            var buffer = new byte[RequestMagic.Length + 1];
            Buffer.BlockCopy(RequestMagic, 0, buffer, 0, RequestMagic.Length);
            buffer[RequestMagic.Length] = ProtocolConstants.Version;
            return buffer;
        }

        private static bool TryParseResponse(byte[] data, IPEndPoint endPoint, out ServerInfo server)
        {
            server = default;
            if (data.Length < ResponseMagic.Length + 1 + 2 + 1 + 1 + 1 + 1 + MaxNameLength + MaxTrackLength)
                return false;
            for (var i = 0; i < ResponseMagic.Length; i++)
            {
                if (data[i] != ResponseMagic[i])
                    return false;
            }

            var offset = ResponseMagic.Length;
            var version = data[offset++];
            if (version != ProtocolConstants.Version)
                return false;

            var port = (ushort)(data[offset] | (data[offset + 1] << 8));
            offset += 2;
            var playerCount = data[offset++];
            var maxPlayers = data[offset++];
            var raceStarted = data[offset++] != 0;
            var trackSelected = data[offset++] != 0;

            var name = ReadFixedString(data, offset, MaxNameLength);
            offset += MaxNameLength;
            var track = ReadFixedString(data, offset, MaxTrackLength);

            server = new ServerInfo(endPoint.Address, port, name, playerCount, maxPlayers, raceStarted, trackSelected, track);
            return true;
        }

        private static string ReadFixedString(byte[] data, int offset, int length)
        {
            var value = Encoding.ASCII.GetString(data, offset, length);
            var nullIndex = value.IndexOf('\0');
            if (nullIndex >= 0)
                value = value.Substring(0, nullIndex);
            return value.Trim();
        }

        public void Dispose()
        {
            _client?.Dispose();
            _client = null;
        }
    }

    internal readonly struct ServerInfo
    {
        public ServerInfo(IPAddress address, int port, string name, int playerCount, int maxPlayers, bool raceStarted, bool trackSelected, string trackName)
        {
            Address = address;
            Port = port;
            Name = name ?? string.Empty;
            PlayerCount = playerCount;
            MaxPlayers = maxPlayers;
            RaceStarted = raceStarted;
            TrackSelected = trackSelected;
            TrackName = trackName ?? string.Empty;
        }

        public IPAddress Address { get; }
        public int Port { get; }
        public string Name { get; }
        public int PlayerCount { get; }
        public int MaxPlayers { get; }
        public bool RaceStarted { get; }
        public bool TrackSelected { get; }
        public string TrackName { get; }
    }
}
