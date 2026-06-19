using System.Collections.Generic;
using TopSpeed.Data;
using TopSpeed.Network;
using TopSpeed.Protocol;
using Xunit;

namespace TopSpeed.Tests;

[Trait("Category", "Behavior")]
public sealed class TrackWeatherPacketBehaviorTests
{
    [Fact]
    public void LoadCustomTrack_ShouldRoundTrip_WeatherProfiles_And_SegmentOverrides()
    {
        var payload = CreateLoadCustomTrackPayload();

        ClientPacketSerializer.TryReadLoadCustomTrack(payload, out var packet).Should().BeTrue();
        packet.NrOfLaps.Should().Be(3);
        packet.TrackName.Should().Be("custom");
        packet.TrackAmbience.Should().Be(TrackAmbience.Airport);
        packet.DefaultWeatherProfileId.Should().Be("clear");
        packet.WeatherProfiles.Should().ContainKey("clear");
        packet.WeatherProfiles.Should().ContainKey("stormfront");
        packet.WeatherProfiles["stormfront"].LateralWindMps.Should().Be(9f);
        packet.Definitions.Should().HaveCount(2);
        packet.Definitions[0].WeatherProfileId.Should().BeNull();
        packet.Definitions[1].WeatherProfileId.Should().Be("stormfront");
        packet.Definitions[1].WeatherTransitionSeconds.Should().Be(1.75f);
    }

    [Fact]
    public void LoadCustomTrack_ShouldReturnFalse_For_LegacyPayloadShape()
    {
        var payload = CreateLegacyLoadCustomTrackPayload();

        var act = () => ClientPacketSerializer.TryReadLoadCustomTrack(payload, out _);

        act.Should().NotThrow();
        ClientPacketSerializer.TryReadLoadCustomTrack(payload, out _).Should().BeFalse();
    }

    private static byte[] CreateLoadCustomTrackPayload()
    {
        var weatherProfiles = new[]
        {
            new TrackWeatherProfile(
                "clear",
                TrackWeather.Sunny,
                0f,
                0f,
                1.225f,
                1f,
                22f,
                0.45f,
                101.325f,
                20000f,
                0f,
                0f,
                0f),
            new TrackWeatherProfile(
                "stormfront",
                TrackWeather.Storm,
                6f,
                9f,
                1.24f,
                0.94f,
                15f,
                1f,
                99.5f,
                1800f,
                0.1f,
                0.25f,
                1f)
        };

        var definitions = new[]
        {
            new TrackDefinition(TrackType.Straight, TrackSurface.Asphalt, TrackNoise.NoNoise, 100f),
            new TrackDefinition(
                TrackType.Right,
                TrackSurface.Asphalt,
                TrackNoise.Crowd,
                140f,
                segmentId: null,
                width: 0f,
                height: 0f,
                weatherProfileId: "stormfront",
                weatherTransitionSeconds: 1.75f,
                roomId: null,
                roomOverrides: null,
                soundSourceIds: null,
                metadata: null)
        };

        var payloadSize = 4 + 12 + 1 + 2 + 2 + PacketWriter.MeasureString16("clear") + 1;
        foreach (var profile in weatherProfiles)
            payloadSize += 2 + PacketWriter.MeasureString16(profile.Id) + 1 + (11 * 4);
        foreach (var definition in definitions)
            payloadSize += 1 + 1 + 1 + 4 + 2 + PacketWriter.MeasureString16(definition.WeatherProfileId ?? string.Empty) + 4;

        var buffer = new byte[2 + payloadSize];
        var writer = new PacketWriter(buffer);
        writer.WriteByte(ProtocolConstants.Version);
        writer.WriteByte((byte)Command.LoadCustomTrack);
        writer.WriteInt32(3);
        writer.WriteFixedString("custom", 12);
        writer.WriteByte((byte)TrackAmbience.Airport);
        writer.WriteUInt16((ushort)definitions.Length);
        writer.WriteString16("clear");
        writer.WriteByte((byte)weatherProfiles.Length);
        foreach (var profile in weatherProfiles)
        {
            writer.WriteString16(profile.Id);
            writer.WriteByte((byte)profile.Kind);
            writer.WriteSingle(profile.LongitudinalWindMps);
            writer.WriteSingle(profile.LateralWindMps);
            writer.WriteSingle(profile.AirDensityKgPerM3);
            writer.WriteSingle(profile.DraftingFactor);
            writer.WriteSingle(profile.TemperatureC);
            writer.WriteSingle(profile.Humidity);
            writer.WriteSingle(profile.PressureKpa);
            writer.WriteSingle(profile.VisibilityM);
            writer.WriteSingle(profile.RainGain);
            writer.WriteSingle(profile.WindGain);
            writer.WriteSingle(profile.StormGain);
        }

        foreach (var definition in definitions)
        {
            writer.WriteByte((byte)definition.Type);
            writer.WriteByte((byte)definition.Surface);
            writer.WriteByte((byte)definition.Noise);
            writer.WriteSingle(definition.Length);
            writer.WriteString16(definition.WeatherProfileId ?? string.Empty);
            writer.WriteSingle(definition.WeatherTransitionSeconds);
        }

        return buffer;
    }

    private static byte[] CreateLegacyLoadCustomTrackPayload()
    {
        var definitions = new[]
        {
            new TrackDefinition(TrackType.Straight, TrackSurface.Asphalt, TrackNoise.NoNoise, 100f)
        };

        var payloadSize = 4 + 12 + 1 + 2 + 1;
        foreach (var definition in definitions)
            payloadSize += 1 + 1 + 1 + 4;

        var buffer = new byte[2 + payloadSize];
        var writer = new PacketWriter(buffer);
        writer.WriteByte(ProtocolConstants.Version);
        writer.WriteByte((byte)Command.LoadCustomTrack);
        writer.WriteInt32(3);
        writer.WriteFixedString("legacy", 12);
        writer.WriteByte((byte)TrackAmbience.Airport);
        writer.WriteUInt16((ushort)definitions.Length);
        writer.WriteByte((byte)TrackWeather.Sunny);
        foreach (var definition in definitions)
        {
            writer.WriteByte((byte)definition.Type);
            writer.WriteByte((byte)definition.Surface);
            writer.WriteByte((byte)definition.Noise);
            writer.WriteSingle(definition.Length);
        }

        return buffer;
    }
}
