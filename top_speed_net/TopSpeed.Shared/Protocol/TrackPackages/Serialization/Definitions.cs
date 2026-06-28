using System;
using System.Collections.Generic;
using System.IO;

namespace TopSpeed.Protocol
{
    public static partial class TrackPackageCodec
    {
        private static void WriteDefinition(BinaryWriter writer, TopSpeed.Data.TrackDefinition definition)
        {
            writer.Write((byte)definition.Type);
            writer.Write((byte)definition.Surface);
            writer.Write((byte)definition.Noise);
            writer.Write(definition.Length);
            writer.Write(definition.SegmentId ?? string.Empty);
            writer.Write(definition.Width);
            writer.Write(definition.Height);
            writer.Write(definition.WeatherProfileId ?? string.Empty);
            writer.Write(definition.WeatherTransitionSeconds);
            writer.Write(definition.RoomId ?? string.Empty);
            WriteRoomOverrides(writer, definition.RoomOverrides);
            WriteStringList(writer, definition.SoundSourceIds ?? Array.Empty<string>());
            WriteMetadata(writer, definition.Metadata ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase));
            writer.Write(definition.IsPitEntry);
            writer.Write(definition.IsPitExit);
        }

        private static TopSpeed.Data.TrackDefinition ReadDefinition(BinaryReader reader)
        {
            var type = (TopSpeed.Data.TrackType)reader.ReadByte();
            var surface = (TopSpeed.Data.TrackSurface)reader.ReadByte();
            var noise = (TopSpeed.Data.TrackNoise)reader.ReadByte();
            var length = reader.ReadSingle();
            var segmentId = NormalizeNull(reader.ReadString());
            var width = reader.ReadSingle();
            var height = reader.ReadSingle();
            var weatherProfileId = NormalizeNull(reader.ReadString());
            var weatherTransitionSeconds = reader.ReadSingle();
            var roomId = NormalizeNull(reader.ReadString());
            var roomOverrides = ReadRoomOverrides(reader);
            var soundSourceIds = ReadStringList(reader);
            var metadata = ReadMetadata(reader);
            var isPitEntry = reader.ReadBoolean();
            var isPitExit = reader.ReadBoolean();
            return new TopSpeed.Data.TrackDefinition(
                type,
                surface,
                noise,
                length,
                segmentId,
                width,
                height,
                weatherProfileId,
                weatherTransitionSeconds,
                roomId,
                roomOverrides,
                soundSourceIds,
                metadata,
                isPitEntry,
                isPitExit);
        }
    }
}
