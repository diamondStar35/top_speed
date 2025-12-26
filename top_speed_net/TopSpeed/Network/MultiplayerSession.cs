using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using TopSpeed.Protocol;

namespace TopSpeed.Network
{
    internal sealed class MultiplayerSession : IDisposable
    {
        private readonly UdpClient _client;
        private readonly IPEndPoint _serverEndPoint;
        private readonly CancellationTokenSource _cts;
        private readonly Task _keepAliveTask;
        private readonly Task _receiveTask;
        private readonly ConcurrentQueue<IncomingPacket> _incoming;

        public MultiplayerSession(UdpClient client, IPEndPoint serverEndPoint, uint playerId, byte playerNumber, string? motd, string? playerName)
        {
            _client = client ?? throw new ArgumentNullException(nameof(client));
            _serverEndPoint = serverEndPoint ?? throw new ArgumentNullException(nameof(serverEndPoint));
            PlayerId = playerId;
            PlayerNumber = playerNumber;
            Motd = motd ?? string.Empty;
            PlayerName = playerName ?? string.Empty;
            _cts = new CancellationTokenSource();
            _keepAliveTask = Task.Run(KeepAliveLoop);
            _incoming = new ConcurrentQueue<IncomingPacket>();
            _receiveTask = Task.Run(ReceiveLoop);
        }

        public IPAddress Address => _serverEndPoint.Address;
        public int Port => _serverEndPoint.Port;
        public uint PlayerId { get; }
        public byte PlayerNumber { get; }
        public string Motd { get; }
        public string PlayerName { get; }

        private async Task KeepAliveLoop()
        {
            var payload = new[] { ProtocolConstants.Version, (byte)Command.KeepAlive };
            while (!_cts.IsCancellationRequested)
            {
                try
                {
                    await _client.SendAsync(payload, payload.Length, _serverEndPoint);
                }
                catch
                {
                    // Keepalive failures shouldn't crash the session.
                }

                try
                {
                    await Task.Delay(1000, _cts.Token);
                }
                catch (TaskCanceledException)
                {
                    break;
                }
            }
        }

        private async Task ReceiveLoop()
        {
            while (!_cts.IsCancellationRequested)
            {
                UdpReceiveResult result;
                try
                {
                    result = await _client.ReceiveAsync();
                }
                catch (ObjectDisposedException)
                {
                    break;
                }
                catch (SocketException)
                {
                    if (_cts.IsCancellationRequested)
                        break;
                    continue;
                }
                catch
                {
                    if (_cts.IsCancellationRequested)
                        break;
                    continue;
                }

                if (!Equals(result.RemoteEndPoint.Address, _serverEndPoint.Address))
                    continue;
                if (result.RemoteEndPoint.Port != _serverEndPoint.Port)
                    continue;
                if (!ClientPacketSerializer.TryReadHeader(result.Buffer, out var command))
                    continue;
                _incoming.Enqueue(new IncomingPacket(command, result.Buffer));
            }
        }

        public bool TryDequeuePacket(out IncomingPacket packet)
        {
            return _incoming.TryDequeue(out packet);
        }

        public void SendPlayerState(PlayerState state)
        {
            var payload = ClientPacketSerializer.WritePlayerState(Command.PlayerState, PlayerId, PlayerNumber, state);
            SafeSend(payload);
        }

        public void SendPlayerData(PlayerRaceData raceData, CarType car, PlayerState state, bool engine, bool braking, bool horning, bool backfiring)
        {
            var payload = ClientPacketSerializer.WritePlayerDataToServer(PlayerId, PlayerNumber, car, raceData, state, engine, braking, horning, backfiring);
            SafeSend(payload);
        }

        public void SendPlayerStarted()
        {
            var payload = ClientPacketSerializer.WritePlayer(Command.PlayerStarted, PlayerId, PlayerNumber);
            SafeSend(payload);
        }

        public void SendPlayerFinished()
        {
            var payload = ClientPacketSerializer.WritePlayer(Command.PlayerFinished, PlayerId, PlayerNumber);
            SafeSend(payload);
        }

        public void SendPlayerFinalize(PlayerState state)
        {
            var payload = ClientPacketSerializer.WritePlayerState(Command.PlayerFinalize, PlayerId, PlayerNumber, state);
            SafeSend(payload);
        }

        public void SendPlayerCrashed()
        {
            var payload = ClientPacketSerializer.WritePlayer(Command.PlayerCrashed, PlayerId, PlayerNumber);
            SafeSend(payload);
        }

        private void SafeSend(byte[] payload)
        {
            try
            {
                _client.Send(payload, payload.Length, _serverEndPoint);
            }
            catch
            {
                // Ignore send failures to keep the client running.
            }
        }

        public void Dispose()
        {
            _cts.Cancel();
            _client.Close();
            _client.Dispose();
            _cts.Dispose();
        }
    }

    internal readonly struct IncomingPacket
    {
        public IncomingPacket(Command command, byte[] payload)
        {
            Command = command;
            Payload = payload;
        }

        public Command Command { get; }
        public byte[] Payload { get; }
    }
}
