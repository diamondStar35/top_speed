using System;
using System.IO;
using SoundFlow.Metadata;
using SoundFlow.Metadata.Models;
using Xunit;

namespace TopSpeed.Tests;

[Trait("Category", "Behavior")]
public sealed class Mp3DurationBehaviorTests
{
    [Theory]
    [InlineData(9, 128)]
    [InlineData(14, 320)]
    public void Mpeg1WithoutXingHeader_ShouldNotDelayEngineStart(int bitrateIndex, int bitrateKbps)
    {
        // Forty complete MPEG-1 Layer III frames, with no optional Xing duration header.
        var frameBytes = 144 * bitrateKbps * 1000 / 44100;
        var bytes = new byte[40 * frameBytes];
        for (var offset = 0; offset < bytes.Length; offset += frameBytes)
        {
            bytes[offset] = 0xff;
            bytes[offset + 1] = 0xfb;
            bytes[offset + 2] = (byte)(bitrateIndex << 4);
        }
        using var stream = new MemoryStream(bytes);
        var result = SoundMetadataReader.Read(stream, new ReadOptions());
        result.IsSuccess.Should().BeTrue();
        result.Value!.Bitrate.Should().Be(bitrateKbps * 1000);
        result.Value.Duration.TotalSeconds.Should().BeApproximately(40 * 1152d / 44100, 0.01,
            "engine-start timing uses the MP3 duration and must not wait after its audio ends");
    }
}
