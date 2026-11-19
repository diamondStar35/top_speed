using TopSpeed.Protocol;

namespace TopSpeed.Network
{
    internal static partial class ClientPacketSerializer
    {
        private static TrackPackageRef ReadCatalogTrackRef(ref PacketReader reader)
        {
            var kind = (RoomTrackSelectionKind)reader.ReadByte();
            var builtInTrack = reader.ReadString16();
            var trackId = reader.ReadString16();
            var version = reader.ReadString16();
            var hash = reader.ReadString16();

            return new TrackPackageRef
            {
                Kind = kind,
                BuiltInTrackKey = builtInTrack ?? string.Empty,
                TrackId = trackId ?? string.Empty,
                Version = version ?? string.Empty,
                Hash = TrackPackageRef.NormalizeHash(hash)
            };
        }

        private static int MeasureCatalogTrackRef(TrackPackageRef track)
        {
            var normalized = NormalizeCatalogTrackRef(track);
            return 1
                + 2 + PacketWriter.MeasureString16(normalized.BuiltInTrackKey)
                + 2 + PacketWriter.MeasureString16(normalized.TrackId)
                + 2 + PacketWriter.MeasureString16(normalized.Version)
                + 2 + PacketWriter.MeasureString16(normalized.Hash);
        }

        private static void WriteCatalogTrackRef(ref PacketWriter writer, TrackPackageRef track)
        {
            var normalized = NormalizeCatalogTrackRef(track);
            writer.WriteByte((byte)normalized.Kind);
            writer.WriteString16(normalized.BuiltInTrackKey ?? string.Empty);
            writer.WriteString16(normalized.TrackId ?? string.Empty);
            writer.WriteString16(normalized.Version ?? string.Empty);
            writer.WriteString16(normalized.Hash ?? string.Empty);
        }

        private static TrackPackageRef NormalizeCatalogTrackRef(TrackPackageRef track)
        {
            return TrackPackageRef.Clone(track);
        }
    }
}
