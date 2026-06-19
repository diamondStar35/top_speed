using System;
using System.Collections.Generic;
using TopSpeed.Data;
using TopSpeed.Localization;

namespace TopSpeed.Protocol
{
    public static partial class PackageBuild
    {
        public static bool TryBuildPayload(
            TrackData trackData,
            string trackFile,
            string displayName,
            int fallbackLaps,
            out TrackPackagePayload payload,
            out string error)
        {
            payload = new TrackPackagePayload();
            error = string.Empty;

            if (trackData == null)
            {
                error = LocalizationService.Mark("Track data is missing.");
                return false;
            }

            if (!TryBuildTrackAssetBlobs(trackData, out var assets, out error))
                return false;

            var metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (trackData.Metadata != null)
            {
                foreach (var pair in trackData.Metadata)
                    metadata[pair.Key] = pair.Value ?? string.Empty;
            }

            var resolvedDisplayName = ResolveTrackDisplayName(displayName, trackData, trackFile);
            if (!metadata.ContainsKey("name"))
                metadata["name"] = resolvedDisplayName;

            var laps = trackData.Laps > 0 ? trackData.Laps : fallbackLaps;
            if (laps == 0)
                laps = 3;

            payload.Manifest = new TrackPackageManifest
            {
                TrackId = ResolveTrackId(trackData, trackFile),
                Version = ResolveTrackVersion(trackData),
                Hash = string.Empty,
                DefaultWeatherProfileId = trackData.DefaultWeatherProfileId,
                Ambience = trackData.Ambience,
                Laps = laps
            };
            payload.Definitions = trackData.Definitions ?? Array.Empty<TrackDefinition>();
            payload.Metadata = metadata;
            payload.RoomProfiles = trackData.RoomProfiles ?? new Dictionary<string, TrackRoomDefinition>(StringComparer.OrdinalIgnoreCase);
            payload.WeatherProfiles = trackData.WeatherProfiles ?? new Dictionary<string, TrackWeatherProfile>(StringComparer.OrdinalIgnoreCase);
            payload.SoundDefinitions = trackData.SoundSources ?? new Dictionary<string, TrackSoundSourceDefinition>(StringComparer.OrdinalIgnoreCase);
            payload.AssetBlobs = assets;
            return true;
        }
    }
}
