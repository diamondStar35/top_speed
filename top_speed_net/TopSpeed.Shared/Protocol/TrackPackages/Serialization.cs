using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace TopSpeed.Protocol
{
    public static partial class TrackPackageCodec
    {
        private const byte FormatVersion = 2;

        private static void WritePayload(BinaryWriter writer, TrackPackagePayload payload, bool includeHash)
        {
            var manifest = payload.Manifest ?? new TrackPackageManifest();
            writer.Write(FormatVersion);
            writer.Write(manifest.TrackId ?? string.Empty);
            writer.Write(manifest.Version ?? string.Empty);
            writer.Write(includeHash ? TrackPackageRef.NormalizeHash(manifest.Hash) : string.Empty);
            writer.Write(manifest.DefaultWeatherProfileId ?? string.Empty);
            writer.Write((byte)manifest.Ambience);
            writer.Write(manifest.Laps);

            WriteMetadata(writer, payload.Metadata ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase));
            WriteRoomProfiles(writer, payload.RoomProfiles ?? new Dictionary<string, TopSpeed.Data.TrackRoomDefinition>(StringComparer.OrdinalIgnoreCase));

            var definitions = payload.Definitions ?? Array.Empty<TopSpeed.Data.TrackDefinition>();
            writer.Write(definitions.Length);
            for (var i = 0; i < definitions.Length; i++)
                WriteDefinition(writer, definitions[i]);

            WriteWeatherProfiles(writer, payload.WeatherProfiles ?? new Dictionary<string, TopSpeed.Data.TrackWeatherProfile>(StringComparer.OrdinalIgnoreCase));
            WriteSoundDefinitions(writer, payload.SoundDefinitions ?? new Dictionary<string, TopSpeed.Data.TrackSoundSourceDefinition>(StringComparer.OrdinalIgnoreCase));
            WriteAssets(writer, payload.AssetBlobs ?? new Dictionary<string, byte[]>(StringComparer.OrdinalIgnoreCase));
        }

        private static TrackPackagePayload ReadPayload(BinaryReader reader)
        {
            var format = reader.ReadByte();
            if (format != FormatVersion)
                throw new InvalidDataException(string.Format(CultureInfo.InvariantCulture, "Unsupported track package payload format '{0}'.", format));

            var payload = new TrackPackagePayload();
            payload.Manifest = new TrackPackageManifest
            {
                TrackId = reader.ReadString(),
                Version = reader.ReadString(),
                Hash = TrackPackageRef.NormalizeHash(reader.ReadString()),
                DefaultWeatherProfileId = reader.ReadString(),
                Ambience = (TopSpeed.Data.TrackAmbience)reader.ReadByte(),
                Laps = reader.ReadInt32()
            };

            payload.Metadata = ReadMetadata(reader);
            payload.RoomProfiles = ReadRoomProfiles(reader);

            var definitionCount = reader.ReadInt32();
            if (definitionCount < 0)
                throw new InvalidDataException("Invalid track definition count.");

            var definitions = new TopSpeed.Data.TrackDefinition[definitionCount];
            for (var i = 0; i < definitionCount; i++)
                definitions[i] = ReadDefinition(reader);

            payload.Definitions = definitions;
            payload.WeatherProfiles = ReadWeatherProfiles(reader);
            payload.SoundDefinitions = ReadSoundDefinitions(reader);
            payload.AssetBlobs = ReadAssets(reader);
            return payload;
        }
    }
}
