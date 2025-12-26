using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using System.Text;
using TopSpeed.Protocol;

namespace TopSpeed.Network
{
    internal sealed class MultiplayerConnector
    {
        public async Task<ConnectResult> ConnectAsync(string host, int port, string callSign, TimeSpan timeout, CancellationToken token)
        {
            if (string.IsNullOrWhiteSpace(host))
                return ConnectResult.CreateFail("No server address was provided.");

            IPAddress? address = null;
            try
            {
                var addresses = await Dns.GetHostAddressesAsync(host);
                foreach (var candidate in addresses)
                {
                    if (candidate.AddressFamily == AddressFamily.InterNetwork)
                    {
                        address = candidate;
                        break;
                    }
                }
                address ??= addresses.Length > 0 ? addresses[0] : null;
            }
            catch (Exception ex)
            {
                return ConnectResult.CreateFail($"Unable to resolve server address: {ex.Message}");
            }

            if (address == null)
                return ConnectResult.CreateFail("Unable to resolve server address.");

            if (port <= 0 || port > 65535)
                port = ClientProtocol.DefaultServerPort;

            var client = new UdpClient(AddressFamily.InterNetwork);
            client.Client.ReceiveBufferSize = 1024 * 1024;
            client.Client.SendBufferSize = 1024 * 1024;
            var endpoint = new IPEndPoint(address, port);
            var sanitizedCallSign = SanitizeCallSign(callSign);

            var hello = BuildPlayerHelloPacket(sanitizedCallSign);
            var handshake = BuildPlayerStatePacket();
            try
            {
                await client.SendAsync(hello, hello.Length, endpoint);
                await client.SendAsync(handshake, handshake.Length, endpoint);
            }
            catch (Exception ex)
            {
                client.Dispose();
                return ConnectResult.CreateFail($"Failed to send handshake: {ex.Message}");
            }

            var deadline = DateTime.UtcNow + timeout;
            var keepAlivePayload = BuildKeepAlivePacket();
            var nextKeepAlive = DateTime.UtcNow + TimeSpan.FromSeconds(1);
            byte? playerNumber = null;
            uint? playerId = null;
            string? motd = null;
            while (DateTime.UtcNow < deadline && !token.IsCancellationRequested)
            {
                var remaining = deadline - DateTime.UtcNow;
                if (remaining <= TimeSpan.Zero)
                    break;
                if (DateTime.UtcNow >= nextKeepAlive)
                {
                    try
                    {
                        await client.SendAsync(keepAlivePayload, keepAlivePayload.Length, endpoint);
                    }
                    catch
                    {
                        // Ignore keep alive failures during connect.
                    }
                    nextKeepAlive = DateTime.UtcNow + TimeSpan.FromSeconds(1);
                }

                var wait = remaining < TimeSpan.FromMilliseconds(200) ? remaining : TimeSpan.FromMilliseconds(200);
                var delayTask = Task.Delay(wait, token);
                var receiveTask = client.ReceiveAsync();
                var completed = await Task.WhenAny(receiveTask, delayTask);
                if (completed != receiveTask)
                    continue;

                var result = receiveTask.Result;
                if (!TryReadHeader(result.Buffer, out var command))
                    continue;

                if (command == Command.Disconnect)
                {
                    client.Dispose();
                    return ConnectResult.CreateFail("The server refused the connection (server may be full).");
                }
                if (command == Command.PlayerNumber && TryReadPlayerNumber(result.Buffer, out var assignedId, out var assignedNumber))
                {
                    playerId = assignedId;
                    playerNumber = assignedNumber;
                    if (!string.IsNullOrWhiteSpace(motd))
                        return ConnectResult.CreateSuccess(client, endpoint, assignedId, assignedNumber, motd, sanitizedCallSign);
                }
                else if (command == Command.ServerInfo && TryReadServerInfo(result.Buffer, out var message))
                {
                    motd = message;
                    if (playerNumber.HasValue && playerId.HasValue)
                        return ConnectResult.CreateSuccess(client, endpoint, playerId.Value, playerNumber.Value, motd, sanitizedCallSign);
                }
            }

            if (playerNumber.HasValue && playerId.HasValue)
                return ConnectResult.CreateSuccess(client, endpoint, playerId.Value, playerNumber.Value, motd, sanitizedCallSign);

            client.Dispose();
            return ConnectResult.CreateFail("No response from server. The server may be offline or unreachable.");
        }

        private static string SanitizeCallSign(string callSign)
        {
            var trimmed = (callSign ?? string.Empty).Trim();
            if (trimmed.Length == 0)
                trimmed = "Player";
            if (trimmed.Length > ProtocolConstants.MaxPlayerNameLength)
                trimmed = trimmed.Substring(0, ProtocolConstants.MaxPlayerNameLength);
            return trimmed;
        }

        private static byte[] BuildPlayerHelloPacket(string callSign)
        {
            var buffer = new byte[2 + ProtocolConstants.MaxPlayerNameLength];
            buffer[0] = ProtocolConstants.Version;
            buffer[1] = (byte)Command.PlayerHello;
            var bytes = Encoding.ASCII.GetBytes(callSign ?? string.Empty);
            var count = Math.Min(bytes.Length, ProtocolConstants.MaxPlayerNameLength);
            Array.Copy(bytes, 0, buffer, 2, count);
            for (var i = 2 + count; i < buffer.Length; i++)
                buffer[i] = 0;
            return buffer;
        }

        private static byte[] BuildPlayerStatePacket()
        {
            var buffer = new byte[2 + 4 + 1 + 1];
            buffer[0] = ProtocolConstants.Version;
            buffer[1] = (byte)Command.PlayerState;
            var idBytes = BitConverter.GetBytes(0u);
            buffer[2] = idBytes[0];
            buffer[3] = idBytes[1];
            buffer[4] = idBytes[2];
            buffer[5] = idBytes[3];
            buffer[6] = 0;
            buffer[7] = (byte)PlayerState.NotReady;
            return buffer;
        }

        private static byte[] BuildKeepAlivePacket()
        {
            return new[] { ProtocolConstants.Version, (byte)Command.KeepAlive };
        }

        private static bool TryReadHeader(byte[] data, out Command command)
        {
            command = Command.Disconnect;
            if (data.Length < 2)
                return false;
            if (data[0] != ProtocolConstants.Version)
                return false;
            command = (Command)data[1];
            return true;
        }

        private static bool TryReadPlayerNumber(byte[] data, out uint playerId, out byte playerNumber)
        {
            playerId = 0;
            playerNumber = 0;
            if (data.Length < 2 + 4 + 1)
                return false;
            playerId = BitConverter.ToUInt32(data, 2);
            playerNumber = data[6];
            return true;
        }

        private static bool TryReadServerInfo(byte[] data, out string motd)
        {
            motd = string.Empty;
            if (data.Length < 2 + ProtocolConstants.MaxMotdLength)
                return false;
            if (data[0] != ProtocolConstants.Version || data[1] != (byte)Command.ServerInfo)
                return false;
            var text = Encoding.ASCII.GetString(data, 2, ProtocolConstants.MaxMotdLength);
            var nullIndex = text.IndexOf('\0');
            motd = nullIndex >= 0 ? text.Substring(0, nullIndex) : text.Trim();
            return true;
        }
    }

    internal readonly struct ConnectResult
    {
        private ConnectResult(bool success, string message, MultiplayerSession? session, string? motd)
        {
            Success = success;
            Message = message;
            Session = session;
            Address = session?.Address;
            Port = session?.Port ?? 0;
            PlayerNumber = session?.PlayerNumber ?? 0;
            PlayerId = session?.PlayerId ?? 0;
            Motd = motd ?? string.Empty;
        }

        public bool Success { get; }
        public string Message { get; }
        public MultiplayerSession? Session { get; }
        public IPAddress? Address { get; }
        public int Port { get; }
        public byte PlayerNumber { get; }
        public uint PlayerId { get; }
        public string Motd { get; }

        public static ConnectResult CreateSuccess(UdpClient client, IPEndPoint endPoint, uint playerId, byte playerNumber, string? motd, string? playerName)
        {
            var session = new MultiplayerSession(client, endPoint, playerId, playerNumber, motd, playerName);
            return new ConnectResult(true, "Connected.", session, motd);
        }

        public static ConnectResult CreateFail(string message)
        {
            return new ConnectResult(false, message ?? "Connection failed.", null, null);
        }
    }
}
