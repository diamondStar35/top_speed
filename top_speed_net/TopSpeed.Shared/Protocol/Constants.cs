namespace TopSpeed.Protocol
{
    public static class ProtocolConstants
    {
        public const int MaxPlayers = 10;
        public const int MaxMultiTrackLength = 8192;
        public const int MaxMediaFileExtensionLength = 16;
        public const int MaxMediaBytes = 8 * 1024 * 1024;
        public const int MaxMediaChunkBytes = 900;
        public const int MaxTrackPackageBytes = 32 * 1024 * 1024;
        public const int MaxTrackPackageAssetBytes = 8 * 1024 * 1024;
        public const int MaxTrackPackageChunkBytes = 900;
        public const int MaxTrackPackageCacheEntries = 50;
        public const int MaxTrackPackageCatalogEntries = 256;
        public const int MaxTrackPackageDisplayNameLength = 160;
        public const int MaxTrackIdLength = 128;
        public const int MaxTrackVersionLength = 64;
        public const int MaxTrackHashLength = 128;
        public const int MaxVehiclePackageBytes = 32 * 1024 * 1024;
        public const int MaxVehiclePackageChunkBytes = 900;
        public const int MaxVehiclePackageCacheEntries = 64;
        public const int MaxVehiclePackageCatalogEntries = 256;
        public const int MaxVehiclePackageDisplayNameLength = 160;
        public const int MaxVehicleIdLength = 128;
        public const int MaxVehicleVersionLength = 64;
        public const int MaxVehicleHashLength = 128;
        public const int MaxLiveFrameBytes = 1200;
        public const int LiveSampleRate = 48000;
        public const int LiveFrameMs = 60;
        public const int LiveChannelsMin = 1;
        public const int LiveChannelsMax = 2;
        public const int LiveTimeoutMs = 3000;
        public const int MaxVoiceFrameBytes = 900;
        public const int VoiceSampleRate = 48000;
        public const int VoiceFrameMs = 20;
        public const int VoiceChannelsMin = 1;
        public const int VoiceChannelsMax = 1;
        public const int VoiceTimeoutMs = 3000;
        public const int VoiceFrequencyTenthsMin = 0;
        public const int VoiceFrequencyTenthsMax = 10000;
        public const byte PacketVersion = ProtocolVersionInfo.PacketVersion;
        public const byte Version = PacketVersion;
        public const int DefaultFrequency = 22050;
        public const int MaxPlayerNameLength = 64;
        public const int MaxMotdLength = 4096;
        public const int MaxRoomNameLength = 128;
        public const int MaxRoomListEntries = 64;
        public const int MaxProtocolMessageLength = 512;
        public const int MaxRoomPlayersToStart = 10;
        public const int MaxVersionLabelLength = 32;
        public const int MaxProtocolDetailsLength = 1024;
        public const string ConnectionKey = "TopSpeedMultiplayer";
    }
}
