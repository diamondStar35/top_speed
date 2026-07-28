using System;
using System.Net;
using TopSpeed.Network;
using TopSpeed.Protocol;
using TopSpeed.Server.Config;
using TopSpeed.Server.Logging;
using TopSpeed.Server.Network;
using Xunit;

namespace TopSpeed.Tests;

[Trait("Category", "Behavior")]
public sealed class ServerHandshakeBehaviorTests
{
    [Fact]
    public void Handshake_ShouldWaitForPlayerIdentificationBeforeCompleting()
    {
        using var fixture = new HandshakeFixture();
        fixture.SendProtocolHello();

        var snapshot = fixture.Server.GetPlayerSnapshotForTest(1);
        snapshot.Handshake.Should().Be(HandshakeState.AwaitingPlayerHello);

        fixture.Send(ClientPacketSerializer.WriteGeneral(Command.Ping));
        fixture.Server.GetStressSnapshotForTest().PlayerCount.Should().Be(1);

        var afterPing = fixture.Server.GetPlayerSnapshotForTest(1);
        afterPing.Handshake.Should().Be(HandshakeState.AwaitingPlayerHello);
    }

    [Fact]
    public void Handshake_ShouldRejectInvalidNameBeforeConnectionEstablishes()
    {
        using var fixture = new HandshakeFixture(new ServerModerationSettings
        {
            MaxNameLength = 4,
            BlockRepeatedLettersInName = false,
            AllowDuplicateNames = true
        });
        fixture.SendProtocolHello();
        fixture.SendPlayerHello("abcdef");

        fixture.Server.GetStressSnapshotForTest().PlayerCount.Should().Be(0);
        Action readRemovedPlayer = () => fixture.Server.GetPlayerSnapshotForTest(1);
        readRemovedPlayer.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Handshake_ShouldCompleteAfterValidPlayerIdentification()
    {
        using var fixture = new HandshakeFixture();
        fixture.SendProtocolHello();
        fixture.SendPlayerHello("pilot");

        var snapshot = fixture.Server.GetPlayerSnapshotForTest(1);
        snapshot.Handshake.Should().Be(HandshakeState.Complete);
        snapshot.LifecycleState.Should().Be(ConnectionLifecycleState.Connected);
    }

    [Fact]
    public void Handshake_ShouldAllow_WhenNegotiatedProtocolDiffersFromClientVersion()
    {
        using var fixture = new HandshakeFixture();
        fixture.SendProtocolHello(new PacketProtocolHello
        {
            ClientVersion = ProtocolProfile.ClientSupported.MinSupported,
            MinSupported = ProtocolProfile.ClientSupported.MinSupported,
            MaxSupported = ProtocolProfile.ClientSupported.MaxSupported,
            ResumePlayerId = 0,
            ResumeToken = 0
        });

        var pending = fixture.Server.GetPlayerSnapshotForTest(1);
        pending.Handshake.Should().Be(HandshakeState.AwaitingPlayerHello);
        pending.LifecycleState.Should().Be(ConnectionLifecycleState.ProtocolNegotiated);

        fixture.SendPlayerHello("pilot");
        var completed = fixture.Server.GetPlayerSnapshotForTest(1);
        completed.Handshake.Should().Be(HandshakeState.Complete);
    }

    [Fact]
    public void Resume_ShouldFallbackToNewSession_WhenRemoteIpDoesNotMatch()
    {
        using var fixture = new HandshakeFixture();
        fixture.SendProtocolHello();
        fixture.SendPlayerHello("pilot");

        var original = fixture.Server.GetPlayerSnapshotForTest(1);
        fixture.Server.DisconnectPeerForTest(new IPEndPoint(IPAddress.Loopback, 30101));

        var mismatchedEndpoint = new IPEndPoint(IPAddress.Parse("127.0.0.2"), 40101);
        fixture.SendProtocolHello(new PacketProtocolHello
        {
            ClientVersion = ProtocolProfile.Current,
            MinSupported = ProtocolProfile.ClientSupported.MinSupported,
            MaxSupported = ProtocolProfile.ClientSupported.MaxSupported,
            ResumePlayerId = original.PlayerId,
            ResumeToken = original.ResumeToken
        }, mismatchedEndpoint);
        fixture.SendPlayerHello("pilot", mismatchedEndpoint);

        Action readOldPlayer = () => fixture.Server.GetPlayerSnapshotForTest(1);
        readOldPlayer.Should().Throw<InvalidOperationException>();

        var fallbackSnapshot = fixture.Server.GetPlayerSnapshotForTest(2);
        fallbackSnapshot.Handshake.Should().Be(HandshakeState.Complete);
        fallbackSnapshot.LifecycleState.Should().Be(ConnectionLifecycleState.Connected);
        fixture.Server.GetStressSnapshotForTest().PlayerCount.Should().Be(1);
    }

    [Fact]
    public void HandshakeCompatibilityStatus_ShouldStayExact_WhenClientCurrentMatchesServerCurrent()
    {
        var compat = ProtocolCompat.Resolve(ProtocolProfile.ClientSupported, ProtocolProfile.ServerSupported);
        compat.IsCompatible.Should().BeTrue();

        var status = RaceServer.ResolveEffectiveCompatibilityStatusForTest(compat, ProtocolProfile.Current);
        status.Should().Be(ProtocolCompatStatus.Exact);
    }

    [Fact]
    public void HandshakeCompatibilityStatus_ShouldDowngrade_WhenClientCurrentDiffersFromServerCurrent()
    {
        var compat = ProtocolCompat.Resolve(ProtocolProfile.ClientSupported, ProtocolProfile.ServerSupported);
        compat.IsCompatible.Should().BeTrue();

        var status = RaceServer.ResolveEffectiveCompatibilityStatusForTest(compat, new ProtocolVer(2026, 7, 12, 1));
        status.Should().Be(ProtocolCompatStatus.CompatibleDowngrade);
    }

    [Fact]
    public void HandshakeNegotiatedVersion_ShouldUseClientVersion_WhenStatusIsExact()
    {
        var negotiated = RaceServer.ResolveNegotiatedVersionForSessionForTest(
            ProtocolCompatStatus.Exact,
            ProtocolProfile.ServerSupported.MaxSupported,
            ProtocolProfile.Current);

        negotiated.Should().Be(ProtocolProfile.Current);
    }

    [Fact]
    public void HandshakeNegotiatedVersion_ShouldKeepNegotiated_WhenStatusIsCompatibleDowngrade()
    {
        var negotiated = RaceServer.ResolveNegotiatedVersionForSessionForTest(
            ProtocolCompatStatus.CompatibleDowngrade,
            ProtocolProfile.ServerSupported.MaxSupported,
            ProtocolProfile.ClientSupported.MinSupported);

        negotiated.Should().Be(ProtocolProfile.ServerSupported.MaxSupported);
    }

    [Fact]
    public void PlayersSnapshot_ShouldReportClientVersion_NotNegotiatedRangeMax()
    {
        using var fixture = new HandshakeFixture();
        fixture.SendProtocolHello();
        fixture.SendPlayerHello("pilot");

        var players = fixture.Server.GetPlayersSnapshot();
        players.Should().HaveCount(1);
        players[0].ProtocolVersion.Should().Be(ProtocolProfile.Current);
    }

    private sealed class HandshakeFixture : IDisposable
    {
        public HandshakeFixture(ServerModerationSettings? moderation = null)
        {
            Logger = new Logger(LogLevel.None, logFilePath: null, writeToConsole: false);
            Server = new RaceServer(new RaceServerConfig
            {
                MaxPlayers = 8,
                Moderation = moderation ?? new ServerModerationSettings()
            }, Logger);
        }

        public RaceServer Server { get; }
        private Logger Logger { get; }
        private IPEndPoint Endpoint { get; } = new(IPAddress.Loopback, 30101);

        public void Send(byte[] payload)
        {
            Server.InjectPacketForTest(Endpoint, payload);
        }

        public void Send(byte[] payload, IPEndPoint endPoint)
        {
            Server.InjectPacketForTest(endPoint, payload);
        }

        public void SendProtocolHello()
        {
            SendProtocolHello(new PacketProtocolHello
            {
                ClientVersion = ProtocolProfile.Current,
                MinSupported = ProtocolProfile.ClientSupported.MinSupported,
                MaxSupported = ProtocolProfile.ClientSupported.MaxSupported,
                ResumePlayerId = 0,
                ResumeToken = 0
            });
        }

        public void SendProtocolHello(PacketProtocolHello hello)
        {
            Send(ClientPacketSerializer.WriteProtocolHello(hello));
        }

        public void SendProtocolHello(PacketProtocolHello hello, IPEndPoint endPoint)
        {
            Send(ClientPacketSerializer.WriteProtocolHello(hello), endPoint);
        }

        public void SendPlayerHello(string name)
        {
            Send(WritePlayerHello(name));
        }

        public void SendPlayerHello(string name, IPEndPoint endPoint)
        {
            Send(WritePlayerHello(name), endPoint);
        }

        public void Dispose()
        {
            Server.Dispose();
            Logger.Dispose();
        }

        private static byte[] WritePlayerHello(string name)
        {
            var buffer = new byte[2 + ProtocolConstants.MaxPlayerNameLength];
            var writer = new PacketWriter(buffer);
            writer.WriteByte(ProtocolConstants.Version);
            writer.WriteByte((byte)Command.PlayerHello);
            writer.WriteFixedString(name ?? string.Empty, ProtocolConstants.MaxPlayerNameLength);
            return buffer;
        }
    }
}
