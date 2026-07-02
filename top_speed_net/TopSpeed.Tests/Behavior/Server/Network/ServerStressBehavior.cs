using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using TopSpeed.Network;
using TopSpeed.Protocol;
using TopSpeed.Server.Logging;
using TopSpeed.Server.Network;
using Xunit;

namespace TopSpeed.Tests;

[Trait("Category", "Stress")]
public sealed class ServerStressBehaviorTests
{
    private const int PlayersPerRoom = ProtocolConstants.MaxPlayers;

    [Fact]
    public void ServerStress_ShouldCompleteTwoHundredPlayersAcrossRooms()
    {
        using var fixture = new ServerStressFixture(totalPlayers: 200);
        var clients = fixture.ConnectPlayers();
        fixture.CreateRoomsAndJoin(clients, PlayersPerRoom);

        var watch = Stopwatch.StartNew();
        fixture.StartAllRooms(clients, PlayersPerRoom);
        fixture.SendStartedAndRacingFrames(clients, PlayersPerRoom, frameCount: 4);
        fixture.FinishAllPlayers(clients, PlayersPerRoom);
        fixture.Server.Update(0.25f);
        watch.Stop();

        var snapshot = fixture.Server.GetStressSnapshotForTest();
        snapshot.PlayerCount.Should().Be(200);
        snapshot.RoomCount.Should().Be(20);
        snapshot.CompletedRoomCount.Should().Be(20);
        snapshot.RacingRoomCount.Should().Be(0);
        snapshot.AbortedRoomCount.Should().Be(0);
        snapshot.ActiveRaceParticipantCount.Should().Be(0);
        snapshot.FinishedResultCount.Should().Be(200);
        snapshot.DnfResultCount.Should().Be(0);
        snapshot.UnresolvedResultCount.Should().Be(0);
        snapshot.CompletionInvariantFailureCount.Should().Be(0);
        snapshot.AuthorityDropCount.Should().Be(0);
        snapshot.TotalJournalEventCount.Should().BeGreaterThanOrEqualTo(20 * 3);
        snapshot.RaceSnapshotSends.Should().BeGreaterThan(0);
        snapshot.StateSyncFramesSent.Should().BeGreaterThan(0);
        watch.Elapsed.Should().BeLessThan(TimeSpan.FromSeconds(30));
    }

    [Fact]
    public void ServerStress_ShouldResumeDisconnectedPlayersAndStillComplete()
    {
        using var fixture = new ServerStressFixture(totalPlayers: 40);
        var clients = fixture.ConnectPlayers();
        fixture.CreateRoomsAndJoin(clients, PlayersPerRoom);
        fixture.StartAllRooms(clients, PlayersPerRoom);
        fixture.SendStartedAndRacingFrames(clients, PlayersPerRoom, frameCount: 2);

        for (var i = 0; i < clients.Count; i += 5)
            fixture.DisconnectAndResume(clients[i]);

        fixture.SendStartedAndRacingFrames(clients, PlayersPerRoom, frameCount: 2);
        fixture.FinishAllPlayers(clients, PlayersPerRoom);
        fixture.Server.Update(0.25f);

        var snapshot = fixture.Server.GetStressSnapshotForTest();
        snapshot.PlayerCount.Should().Be(40);
        snapshot.CompletedRoomCount.Should().Be(4);
        snapshot.FinishedResultCount.Should().Be(40);
        snapshot.DnfResultCount.Should().Be(0);
        snapshot.UnresolvedResultCount.Should().Be(0);
        snapshot.CompletionInvariantFailureCount.Should().Be(0);
        snapshot.AuthorityDropCount.Should().Be(0);
    }

    [Fact]
    public void ServerStress_ShouldRejectInvalidRacePacketsWithoutCorruptingCompletion()
    {
        using var fixture = new ServerStressFixture(totalPlayers: 40);
        var clients = fixture.ConnectPlayers();
        fixture.CreateRoomsAndJoin(clients, PlayersPerRoom);
        fixture.StartAllRooms(clients, PlayersPerRoom);
        fixture.SendStartedAndRacingFrames(clients, PlayersPerRoom, frameCount: 1);

        for (var i = 0; i < clients.Count; i++)
        {
            var client = clients[i];
            var raceInstance = fixture.Server.GetRoomRaceInstanceForTest(client.RoomId);
            fixture.Send(client, ClientPacketSerializer.WriteRacePlayerDataToServer(
                raceInstance,
                client.PlayerId,
                (byte)((client.PlayerNumber + 1) % PlayersPerRoom),
                CarType.Vehicle1,
                BuildRaceData(0f, 10f, 80),
                PlayerState.Racing,
                engineRunning: true,
                braking: false,
                horning: false,
                backfiring: false,
                mediaLoaded: false,
                mediaPlaying: false,
                mediaId: 0,
                radioVolumePercent: 100));

            fixture.Send(client, ClientPacketSerializer.WriteRacePlayerDataToServer(
                raceInstance,
                client.PlayerId,
                client.PlayerNumber,
                CarType.Vehicle1,
                BuildRaceData(float.NaN, 10f, 80),
                PlayerState.Racing,
                engineRunning: true,
                braking: false,
                horning: false,
                backfiring: false,
                mediaLoaded: false,
                mediaPlaying: false,
                mediaId: 0,
                radioVolumePercent: 100));
        }

        fixture.FinishAllPlayers(clients, PlayersPerRoom);
        fixture.Server.Update(0.25f);

        var snapshot = fixture.Server.GetStressSnapshotForTest();
        snapshot.CompletedRoomCount.Should().Be(4);
        snapshot.FinishedResultCount.Should().Be(40);
        snapshot.UnresolvedResultCount.Should().Be(0);
        snapshot.CompletionInvariantFailureCount.Should().Be(0);
        snapshot.AuthorityDropCount.Should().BeGreaterThanOrEqualTo(80);
    }

    [Fact]
    public void ServerStress_PrepareShouldNotBlockOnSuspendedMember()
    {
        using var fixture = new ServerStressFixture(totalPlayers: 2);
        var clients = fixture.ConnectPlayers();
        fixture.CreateRoomsAndJoin(clients, playersPerRoom: 2);

        var host = clients[0];
        var other = clients[1];
        fixture.Send(host, ClientPacketSerializer.WriteRoomStartRace());
        fixture.DisconnectWithoutResume(other);
        fixture.Send(host, ClientPacketSerializer.WriteRoomPlayerReady(CarType.Vehicle1, automaticTransmission: true, TopSpeed.Protocol.VehiclePackageRef.None()));
        fixture.Server.Update(0.25f);

        var snapshot = fixture.Server.GetStressSnapshotForTest();
        snapshot.RoomCount.Should().Be(1);
        snapshot.PreparingRoomCount.Should().Be(0);
    }

    [Fact]
    public void ServerStress_ClosedRoomIdShouldBeReused()
    {
        using var fixture = new ServerStressFixture(totalPlayers: 1);
        var clients = fixture.ConnectPlayers();
        var host = clients[0];

        fixture.Send(host, ClientPacketSerializer.WriteRoomCreate("reuse-id-1", GameRoomType.BotsRace, 2));
        var firstRoomId = fixture.Server.GetPlayerSnapshotForTest(host.PlayerId).RoomId.GetValueOrDefault();
        firstRoomId.Should().BeGreaterThan(0u);

        fixture.Send(host, ClientPacketSerializer.WriteRoomLeave());
        fixture.Server.GetStressSnapshotForTest().RoomCount.Should().Be(0);

        fixture.Send(host, ClientPacketSerializer.WriteRoomCreate("reuse-id-2", GameRoomType.BotsRace, 2));
        var secondRoomId = fixture.Server.GetPlayerSnapshotForTest(host.PlayerId).RoomId.GetValueOrDefault();

        secondRoomId.Should().Be(firstRoomId);
    }

    private static PlayerRaceData BuildRaceData(float x, float y, ushort speed)
    {
        return new PlayerRaceData
        {
            PositionX = x,
            PositionY = y,
            Speed = speed,
            Frequency = 22050 + speed
        };
    }

    private sealed class ServerStressFixture : IDisposable
    {
        private readonly int _totalPlayers;
        private int _nextReconnectPort = 50000;

        public ServerStressFixture(int totalPlayers)
        {
            _totalPlayers = totalPlayers;
            Logger = new Logger(LogLevel.None, logFilePath: null, writeToConsole: false);
            Server = new RaceServer(new RaceServerConfig { MaxPlayers = totalPlayers + 16 }, Logger);
        }

        public RaceServer Server { get; }
        private Logger Logger { get; }

        public List<StressClient> ConnectPlayers()
        {
            var clients = new List<StressClient>(_totalPlayers);
            for (var i = 0; i < _totalPlayers; i++)
            {
                var client = new StressClient(
                    playerId: (uint)(i + 1),
                    endPoint: new IPEndPoint(IPAddress.Loopback, 30000 + i));
                SendProtocolHello(client, resumePlayerId: 0, resumeToken: 0);
                Send(client, WritePlayerHello("stress-" + client.PlayerId));
                clients.Add(client);
            }

            return clients;
        }

        public void CreateRoomsAndJoin(IReadOnlyList<StressClient> clients, int playersPerRoom)
        {
            var roomCount = clients.Count / playersPerRoom;
            for (var roomIndex = 0; roomIndex < roomCount; roomIndex++)
            {
                var host = clients[roomIndex * playersPerRoom];
                host.RoomId = (uint)(roomIndex + 1);
                host.PlayerNumber = 0;
                Send(host, ClientPacketSerializer.WriteRoomCreate("stress-" + roomIndex, GameRoomType.PlayersRace, (byte)playersPerRoom));

                for (var offset = 1; offset < playersPerRoom; offset++)
                {
                    var client = clients[(roomIndex * playersPerRoom) + offset];
                    client.RoomId = host.RoomId;
                    client.PlayerNumber = (byte)offset;
                    Send(client, ClientPacketSerializer.WriteRoomJoin(host.RoomId));
                }
            }
        }

        public void StartAllRooms(IReadOnlyList<StressClient> clients, int playersPerRoom)
        {
            var roomCount = clients.Count / playersPerRoom;
            for (var roomIndex = 0; roomIndex < roomCount; roomIndex++)
            {
                var host = clients[roomIndex * playersPerRoom];
                Send(host, ClientPacketSerializer.WriteRoomStartRace());

                for (var offset = 0; offset < playersPerRoom; offset++)
                {
                    var client = clients[(roomIndex * playersPerRoom) + offset];
                    Send(client, ClientPacketSerializer.WriteRoomPlayerReady((CarType)((int)CarType.Vehicle1 + (offset % 4)), automaticTransmission: true, TopSpeed.Protocol.VehiclePackageRef.None()));
                }
            }
        }

        public void SendStartedAndRacingFrames(IReadOnlyList<StressClient> clients, int playersPerRoom, int frameCount)
        {
            RefreshPlayerNumbers(clients);
            for (var i = 0; i < clients.Count; i++)
            {
                var client = clients[i];
                var raceInstance = Server.GetRoomRaceInstanceForTest(client.RoomId);
                Send(client, ClientPacketSerializer.WriteRacePlayer(Command.PlayerStarted, raceInstance, client.PlayerId, client.PlayerNumber));
            }

            for (var frame = 0; frame < frameCount; frame++)
            {
                for (var i = 0; i < clients.Count; i++)
                {
                    var client = clients[i];
                    var raceInstance = Server.GetRoomRaceInstanceForTest(client.RoomId);
                    var lane = client.PlayerNumber - ((playersPerRoom - 1) / 2f);
                    Send(client, ClientPacketSerializer.WriteRacePlayerDataToServer(
                        raceInstance,
                        client.PlayerId,
                        client.PlayerNumber,
                        CarType.Vehicle1,
                        BuildRaceData(lane * 4f, 100f + (frame * 60f), 120),
                        PlayerState.Racing,
                        engineRunning: true,
                        braking: false,
                        horning: frame % 2 == 0 && client.PlayerNumber == 0,
                        backfiring: false,
                        mediaLoaded: false,
                        mediaPlaying: false,
                        mediaId: 0,
                        radioVolumePercent: 100));
                }

                Server.Update(1f / 15f);
            }
        }

        public void FinishAllPlayers(IReadOnlyList<StressClient> clients, int playersPerRoom)
        {
            RefreshPlayerNumbers(clients);
            for (var roomOffset = 0; roomOffset < playersPerRoom; roomOffset++)
            {
                for (var i = roomOffset; i < clients.Count; i += playersPerRoom)
                {
                    var client = clients[i];
                    var raceInstance = Server.GetRoomRaceInstanceForTest(client.RoomId);
                    Send(client, ClientPacketSerializer.WriteRacePlayerDataToServer(
                        raceInstance,
                        client.PlayerId,
                        client.PlayerNumber,
                        CarType.Vehicle1,
                        BuildRaceData(client.PlayerNumber * 4f, 1_000_000f, 0),
                        PlayerState.Finished,
                        engineRunning: false,
                        braking: false,
                        horning: false,
                        backfiring: false,
                        mediaLoaded: false,
                        mediaPlaying: false,
                        mediaId: 0,
                        radioVolumePercent: 100));
                }
            }
        }

        public void DisconnectAndResume(StressClient client)
        {
            var serverPlayer = Server.GetPlayerSnapshotForTest(client.PlayerId);
            Server.DisconnectPeerForTest(client.EndPoint);
            client.EndPoint = new IPEndPoint(IPAddress.Loopback, _nextReconnectPort++);
            SendProtocolHello(client, serverPlayer.PlayerId, serverPlayer.ResumeToken);
            Send(client, WritePlayerHello("stress-" + client.PlayerId));
            var resumed = Server.GetPlayerSnapshotForTest(client.PlayerId);
            client.PlayerNumber = resumed.PlayerNumber;
            client.RoomId = resumed.RoomId.GetValueOrDefault(client.RoomId);
        }

        public void DisconnectWithoutResume(StressClient client)
        {
            Server.DisconnectPeerForTest(client.EndPoint);
        }

        public void Send(StressClient client, byte[] payload)
        {
            Server.InjectPacketForTest(client.EndPoint, payload);
        }

        private void SendProtocolHello(StressClient client, uint resumePlayerId, ulong resumeToken)
        {
            Send(client, ClientPacketSerializer.WriteProtocolHello(new PacketProtocolHello
            {
                ClientVersion = ProtocolProfile.Current,
                MinSupported = ProtocolProfile.ClientSupported.MinSupported,
                MaxSupported = ProtocolProfile.ClientSupported.MaxSupported,
                ResumePlayerId = resumePlayerId,
                ResumeToken = resumeToken
            }));
        }

        private void RefreshPlayerNumbers(IReadOnlyList<StressClient> clients)
        {
            for (var i = 0; i < clients.Count; i++)
            {
                var client = clients[i];
                var snapshot = Server.GetPlayerSnapshotForTest(client.PlayerId);
                client.PlayerNumber = snapshot.PlayerNumber;
                client.RoomId = snapshot.RoomId.GetValueOrDefault(client.RoomId);
            }
        }

        private static byte[] WritePlayerHello(string name)
        {
            var buffer = new byte[2 + ProtocolConstants.MaxPlayerNameLength];
            var writer = new PacketWriter(buffer);
            writer.WriteByte(ProtocolConstants.Version);
            writer.WriteByte((byte)Command.PlayerHello);
            writer.WriteFixedString(name, ProtocolConstants.MaxPlayerNameLength);
            return buffer;
        }

        public void Dispose()
        {
            Server.Dispose();
            Logger.Dispose();
        }
    }

    private sealed class StressClient
    {
        public StressClient(uint playerId, IPEndPoint endPoint)
        {
            PlayerId = playerId;
            EndPoint = endPoint;
        }

        public uint PlayerId { get; }
        public IPEndPoint EndPoint { get; set; }
        public uint RoomId { get; set; }
        public byte PlayerNumber { get; set; }
    }
}
