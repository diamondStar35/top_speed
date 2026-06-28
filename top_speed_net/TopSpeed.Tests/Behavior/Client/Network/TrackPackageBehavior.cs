using System.Collections.Generic;
using System.Numerics;
using TopSpeed.Network;
using TopSpeed.Protocol;
using TopSpeed.Server.Protocol;
using TopSpeed.Data;
using Xunit;

namespace TopSpeed.Tests;

[Trait("Category", "Behavior")]
public sealed class TrackPackageBehaviorTests
{
    [Fact]
    public void TrackPackageCodec_ShouldRoundTrip_SoundDefinitions()
    {
        var payload = new TrackPackagePayload
        {
            Manifest = new TrackPackageManifest
            {
                TrackId = "track-custom-1",
                Version = "1.0.0",
                DefaultWeatherProfileId = "storm",
                Ambience = TrackAmbience.Desert,
                Laps = 4
            },
            Definitions = new[]
            {
                new TrackDefinition(
                    TrackType.Right,
                    TrackSurface.Asphalt,
                    TrackNoise.Crowd,
                    120f,
                    segmentId: "segment-a",
                    width: 14f,
                    height: 1.5f,
                    weatherProfileId: "storm",
                    weatherTransitionSeconds: 1.25f,
                    roomId: "hangar",
                    roomOverrides: new TrackRoomOverrides
                    {
                        ReverbGain = 0.2f,
                        Diffusion = 0.65f
                    },
                    soundSourceIds: new[] { "engine" },
                    metadata: new Dictionary<string, string>
                    {
                        ["hint"] = "apex-late"
                    },
                    isPitEntry: true,
                    isPitExit: true)
            },
            Metadata = new Dictionary<string, string>(System.StringComparer.OrdinalIgnoreCase)
            {
                ["author"] = "qa"
            },
            RoomProfiles = new Dictionary<string, TrackRoomDefinition>(System.StringComparer.OrdinalIgnoreCase)
            {
                ["hangar"] = new TrackRoomDefinition(
                    "hangar",
                    "Hangar",
                    reverbTimeSeconds: 1.1f,
                    reverbGain: 0.3f,
                    hfDecayRatio: 0.4f,
                    lateReverbGain: 0.5f,
                    diffusion: 0.6f,
                    airAbsorption: 0.7f,
                    occlusionScale: 0.8f,
                    transmissionScale: 0.9f)
            },
            WeatherProfiles = new Dictionary<string, TrackWeatherProfile>(System.StringComparer.OrdinalIgnoreCase)
            {
                ["storm"] = new TrackWeatherProfile(
                    "storm",
                    TrackWeather.Storm,
                    5f,
                    3f,
                    1.24f,
                    0.93f,
                    16f,
                    0.92f,
                    100.1f,
                    1500f,
                    0.2f,
                    0.4f,
                    0.8f)
            },
            SoundDefinitions = new Dictionary<string, TrackSoundSourceDefinition>(System.StringComparer.OrdinalIgnoreCase)
            {
                ["engine"] = new TrackSoundSourceDefinition(
                    "engine",
                    TrackSoundSourceType.Static,
                    "audio/engine.ogg",
                    new[] { "audio/engine_alt.ogg" },
                    new[] { "engine_b" },
                    TrackSoundRandomMode.PerArea,
                    loop: true,
                    volume: 0.6f,
                    spatial: true,
                    allowHrtf: true,
                    fadeInSeconds: 0.2f,
                    fadeOutSeconds: 0.5f,
                    crossfadeSeconds: 1.2f,
                    pitch: 1.05f,
                    pan: 0.1f,
                    minDistance: 2f,
                    maxDistance: 40f,
                    rolloff: 1.3f,
                    global: false,
                    startAreaId: "start",
                    endAreaId: "finish",
                    startPosition: new Vector3(1f, 2f, 3f),
                    startRadiusMeters: 5f,
                    endPosition: new Vector3(4f, 5f, 6f),
                    endRadiusMeters: 7f,
                    position: new Vector3(8f, 9f, 10f),
                    speedMetersPerSecond: 11f)
            },
            AssetBlobs = new Dictionary<string, byte[]>(System.StringComparer.OrdinalIgnoreCase)
            {
                ["audio/engine.ogg"] = new byte[] { 1, 2, 3, 4 },
                ["audio/engine_alt.ogg"] = new byte[] { 5, 6, 7 }
            }
        };
        payload.Manifest.Hash = TrackPackageCodec.ComputeHash(payload);

        var bytes = TrackPackageCodec.Serialize(payload);
        TrackPackageCodec.TryDeserialize(bytes, out var parsed, out var error).Should().BeTrue(error);
        parsed.Definitions.Should().HaveCount(1);
        parsed.Metadata.Should().ContainKey("author");
        parsed.RoomProfiles.Should().ContainKey("hangar");
        parsed.Definitions[0].SegmentId.Should().Be("segment-a");
        parsed.Definitions[0].Width.Should().Be(14f);
        parsed.Definitions[0].Height.Should().Be(1.5f);
        parsed.Definitions[0].RoomId.Should().Be("hangar");
        parsed.Definitions[0].RoomOverrides.Should().NotBeNull();
        parsed.Definitions[0].RoomOverrides!.ReverbGain.Should().Be(0.2f);
        parsed.Definitions[0].Metadata.Should().ContainKey("hint");
        parsed.Definitions[0].WeatherProfileId.Should().Be("storm");
        parsed.Definitions[0].WeatherTransitionSeconds.Should().Be(1.25f);
        parsed.Definitions[0].IsPitEntry.Should().BeTrue();
        parsed.Definitions[0].IsPitExit.Should().BeTrue();
        parsed.SoundDefinitions.Should().ContainKey("engine");
        parsed.SoundDefinitions["engine"].Type.Should().Be(TrackSoundSourceType.Static);
        parsed.SoundDefinitions["engine"].RandomMode.Should().Be(TrackSoundRandomMode.PerArea);
        parsed.SoundDefinitions["engine"].CrossfadeSeconds.Should().Be(1.2f);
        parsed.SoundDefinitions["engine"].StartPosition.Should().Be(new Vector3(1f, 2f, 3f));
        parsed.SoundDefinitions["engine"].Position.Should().Be(new Vector3(8f, 9f, 10f));
        TrackPackageCodec.ComputeHash(parsed).Should().Be(payload.Manifest.Hash);
    }

    [Fact]
    public void TrackPackageTransferChunk_ShouldRoundTrip()
    {
        var packet = new PacketTrackPackageTransferChunk
        {
            Hash = "ABCDEF0123",
            ChunkIndex = 2,
            Data = new byte[] { 10, 11, 12 }
        };

        var payload = ClientPacketSerializer.WriteTrackPackageTransferChunk(packet);
        ClientPacketSerializer.TryReadTrackPackageTransferChunk(payload, out var parsed).Should().BeTrue();
        parsed.Hash.Should().Be("abcdef0123");
        parsed.ChunkIndex.Should().Be((ushort)2);
        parsed.Data.Should().Equal(new byte[] { 10, 11, 12 });
    }

    [Fact]
    public void TrackPackageUploadBegin_ShouldRoundTripBetweenClientAndServer()
    {
        var packet = new PacketTrackPackageUploadBegin
        {
            UploadId = 77,
            TrackId = "city-loop",
            Version = "1.3",
            Hash = "AABBCCDD",
            TotalBytes = 2048
        };

        var payload = ClientPacketSerializer.WriteTrackPackageUploadBegin(packet);
        PacketSerializer.TryReadTrackPackageUploadBegin(payload, out var parsed).Should().BeTrue();
        parsed.UploadId.Should().Be((uint)77);
        parsed.TrackId.Should().Be("city-loop");
        parsed.Version.Should().Be("1.3");
        parsed.Hash.Should().Be("aabbccdd");
        parsed.TotalBytes.Should().Be((uint)2048);
    }

    [Fact]
    public void TrackPackageUploadChunk_ShouldRoundTripBetweenClientAndServer()
    {
        var packet = new PacketTrackPackageUploadChunk
        {
            UploadId = 44,
            ChunkIndex = 5,
            Data = new byte[] { 1, 2, 3, 4 }
        };

        var payload = ClientPacketSerializer.WriteTrackPackageUploadChunk(packet);
        PacketSerializer.TryReadTrackPackageUploadChunk(payload, out var parsed).Should().BeTrue();
        parsed.UploadId.Should().Be((uint)44);
        parsed.ChunkIndex.Should().Be((ushort)5);
        parsed.Data.Should().Equal(new byte[] { 1, 2, 3, 4 });
    }

    [Fact]
    public void TrackPackageUploadEnd_ShouldRoundTripBetweenClientAndServer()
    {
        var packet = new PacketTrackPackageUploadEnd
        {
            UploadId = 12
        };

        var payload = ClientPacketSerializer.WriteTrackPackageUploadEnd(packet);
        PacketSerializer.TryReadTrackPackageUploadEnd(payload, out var parsed).Should().BeTrue();
        parsed.UploadId.Should().Be((uint)12);
    }

    [Fact]
    public void TrackPackageUploadResult_ShouldRoundTripBetweenServerAndClient()
    {
        var packet = new PacketTrackPackageUploadResult
        {
            UploadId = 99,
            Status = TrackPackageUploadStatus.Reused,
            Hash = "ABC123",
            Message = "reused"
        };

        var payload = PacketSerializer.WriteTrackPackageUploadResult(packet);
        ClientPacketSerializer.TryReadTrackPackageUploadResult(payload, out var parsed).Should().BeTrue();
        parsed.UploadId.Should().Be((uint)99);
        parsed.Status.Should().Be(TrackPackageUploadStatus.Reused);
        parsed.Hash.Should().Be("abc123");
        parsed.Message.Should().Be("reused");
    }

    [Fact]
    public void TrackPackageTransferBegin_ShouldRoundTripBetweenServerAndClient()
    {
        var packet = new PacketTrackPackageTransferBegin
        {
            TrackId = "desert-run",
            Version = "2.0",
            Hash = "FF00AA",
            TotalBytes = 8192
        };

        var payload = PacketSerializer.WriteTrackPackageTransferBegin(packet);
        ClientPacketSerializer.TryReadTrackPackageTransferBegin(payload, out var parsed).Should().BeTrue();
        parsed.TrackId.Should().Be("desert-run");
        parsed.Version.Should().Be("2.0");
        parsed.Hash.Should().Be("ff00aa");
        parsed.TotalBytes.Should().Be((uint)8192);
    }

    [Fact]
    public void TrackPackageTransferEnd_ShouldRoundTripBetweenServerAndClient()
    {
        var packet = new PacketTrackPackageTransferEnd
        {
            Hash = "AA11BB22"
        };

        var payload = PacketSerializer.WriteTrackPackageTransferEnd(packet);
        ClientPacketSerializer.TryReadTrackPackageTransferEnd(payload, out var parsed).Should().BeTrue();
        parsed.Hash.Should().Be("aa11bb22");
    }

    [Fact]
    public void TrackPackageReady_ShouldRoundTripBetweenClientAndServer()
    {
        var payload = ClientPacketSerializer.WriteTrackPackageReady(new PacketTrackPackageReady
        {
            Hash = "CAFEBABE"
        });

        PacketSerializer.TryReadTrackPackageReady(payload, out var parsed).Should().BeTrue();
        parsed.Hash.Should().Be("cafebabe");
    }

    [Fact]
    public void TrackPackageCatalog_ShouldRoundTrip()
    {
        var packet = new PacketTrackPackageCatalog
        {
            Tracks = new[]
            {
                new PacketTrackPackageCatalogEntry
                {
                    Track = TrackPackageRef.Custom("city-loop", "2.4", "AABBCCDDEEFF"),
                    DisplayName = "City Loop by QA",
                    HasPitArea = true
                },
                new PacketTrackPackageCatalogEntry
                {
                    Track = TrackPackageRef.Custom("desert-run", "1.0", "001122334455"),
                    DisplayName = "Desert Run",
                    HasPitArea = false
                }
            }
        };

        var payload = ClientPacketSerializer.WriteTrackPackageCatalog(packet);
        ClientPacketSerializer.TryReadTrackPackageCatalog(payload, out var parsed).Should().BeTrue();
        parsed.Tracks.Should().HaveCount(2);
        parsed.Tracks[0].Track.TrackId.Should().Be("city-loop");
        parsed.Tracks[0].Track.Version.Should().Be("2.4");
        parsed.Tracks[0].Track.Hash.Should().Be("aabbccddeeff");
        parsed.Tracks[0].DisplayName.Should().Be("City Loop by QA");
        parsed.Tracks[0].HasPitArea.Should().BeTrue();
        parsed.Tracks[1].HasPitArea.Should().BeFalse();
    }

    [Fact]
    public void TrackPackageCatalogRequest_ShouldRoundTrip()
    {
        var payload = ClientPacketSerializer.WriteTrackPackageCatalogRequest();
        ClientPacketSerializer.TryReadTrackPackageCatalogRequest(payload, out _).Should().BeTrue();
    }

    [Fact]
    public void TrackPackageCodec_ShouldRejectMalformedPayload()
    {
        TrackPackageCodec.TryDeserialize(new byte[] { 1, 2, 3 }, out _, out var error).Should().BeFalse();
        error.Should().NotBeNullOrWhiteSpace();
    }
}
