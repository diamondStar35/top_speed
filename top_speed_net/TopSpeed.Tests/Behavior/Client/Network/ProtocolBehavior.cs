using TopSpeed.Network;
using TopSpeed.Protocol;
using Xunit;

namespace TopSpeed.Tests;

[Trait("Category", "Behavior")]
public sealed class ProtocolBehaviorTests
{
    [Fact]
    public void WriteRacePlayerState_Encodes_Race_Instance_First()
    {
        var payload = ClientPacketSerializer.WriteRacePlayerState(
            Command.PlayerState,
            raceInstanceId: 42u,
            playerId: 7u,
            playerNumber: 3,
            state: PlayerState.Racing);

        payload.Length.Should().Be(2 + 4 + 4 + 1 + 1);

        var reader = new PacketReader(payload);
        reader.ReadByte().Should().Be(ProtocolConstants.Version);
        reader.ReadByte().Should().Be((byte)Command.PlayerState);
        reader.ReadUInt32().Should().Be(42u);
        reader.ReadUInt32().Should().Be(7u);
        reader.ReadByte().Should().Be((byte)3);
        reader.ReadByte().Should().Be((byte)PlayerState.Racing);
    }

    [Fact]
    public void WriteRacePlayerDataToServer_Encodes_Race_Instance()
    {
        var payload = ClientPacketSerializer.WriteRacePlayerDataToServer(
            raceInstanceId: 9001u,
            playerId: 11u,
            playerNumber: 2,
            car: CarType.Vehicle4,
            raceData: new PlayerRaceData
            {
                PositionX = 1.5f,
                PositionY = 300.25f,
                Speed = 188,
                Frequency = 9200
            },
            state: PlayerState.Finished,
            engineRunning: true,
            braking: false,
            horning: true,
            backfiring: false,
            mediaLoaded: true,
            mediaPlaying: true,
            mediaId: 99u,
            radioVolumePercent: 75);

        payload.Length.Should().Be(2 + 36);

        var reader = new PacketReader(payload);
        reader.ReadByte().Should().Be(ProtocolConstants.Version);
        reader.ReadByte().Should().Be((byte)Command.PlayerDataToServer);
        reader.ReadUInt32().Should().Be(9001u);
        reader.ReadUInt32().Should().Be(11u);
        reader.ReadByte().Should().Be((byte)2);
        reader.ReadByte().Should().Be((byte)CarType.Vehicle4);
        reader.ReadSingle().Should().Be(1.5f);
        reader.ReadSingle().Should().Be(300.25f);
        reader.ReadUInt16().Should().Be((ushort)188);
        reader.ReadInt32().Should().Be(9200);
        reader.ReadByte().Should().Be((byte)PlayerState.Finished);
        reader.ReadBool().Should().BeTrue();
        reader.ReadBool().Should().BeFalse();
        reader.ReadBool().Should().BeTrue();
        reader.ReadBool().Should().BeFalse();
        reader.ReadBool().Should().BeTrue();
        reader.ReadBool().Should().BeTrue();
        reader.ReadUInt32().Should().Be(99u);
        reader.ReadByte().Should().Be((byte)75);
    }

    [Fact]
    public void RoomEvent_RoundTrip_Uses_RaceState_Without_Legacy_Bools()
    {
        var roomEvent = new PacketRoomEvent
        {
            RoomId = 12,
            RoomVersion = 5,
            EventSequence = 9,
            RaceInstanceId = 6,
            Kind = RoomEventKind.RoomSummaryUpdated,
            HostPlayerId = 44,
            RoomType = GameRoomType.PlayersRace,
            PlayerCount = 2,
            PlayersToStart = 2,
            RaceState = RoomRaceState.Preparing,
            RacePaused = true,
            TrackName = "desert",
            Laps = 3,
            GameRulesFlags = 17,
            RoomName = "room-a",
            SubjectPlayerId = 7,
            SubjectPlayerNumber = 1,
            SubjectPlayerState = PlayerState.AwaitingStart,
            SubjectPlayerName = "alice"
        };

        var payload = ClientPacketSerializer.WriteRoomEvent(roomEvent);
        var expectedTrackPayload = 1
            + 2 + PacketWriter.MeasureString16(roomEvent.TrackName)
            + 2
            + 2
            + 2;
        var expectedPayload =
            4 + 4 + 4 + 4 + 1 + 4 + 1 + 1 + 1 + 1 + 1 + expectedTrackPayload + 4 + 4 +
            ProtocolConstants.MaxRoomNameLength + 4 + 1 + 1 + ProtocolConstants.MaxPlayerNameLength;
        payload.Length.Should().Be(2 + expectedPayload);

        ClientPacketSerializer.TryReadRoomEvent(payload, out var parsed).Should().BeTrue();
        parsed.RoomId.Should().Be(roomEvent.RoomId);
        parsed.EventSequence.Should().Be(roomEvent.EventSequence);
        parsed.RaceInstanceId.Should().Be(roomEvent.RaceInstanceId);
        parsed.RaceState.Should().Be(roomEvent.RaceState);
        parsed.RacePaused.Should().Be(roomEvent.RacePaused);
        parsed.TrackName.Should().Be(roomEvent.TrackName);
        parsed.SubjectPlayerId.Should().Be(roomEvent.SubjectPlayerId);
        parsed.SubjectPlayerNumber.Should().Be(roomEvent.SubjectPlayerNumber);
    }

    [Fact]
    public void WriteRoomRaceControl_Encodes_Action()
    {
        var payload = ClientPacketSerializer.WriteRoomRaceControl(RoomRaceControlAction.Stop);

        payload.Length.Should().Be(3);

        var reader = new PacketReader(payload);
        reader.ReadByte().Should().Be(ProtocolConstants.Version);
        reader.ReadByte().Should().Be((byte)Command.RoomRaceControl);
        reader.ReadByte().Should().Be((byte)RoomRaceControlAction.Stop);
    }
}
