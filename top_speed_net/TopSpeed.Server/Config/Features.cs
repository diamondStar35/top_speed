using System.Text.Json.Serialization;

namespace TopSpeed.Server.Config
{
    internal sealed class ServerFeaturesSettings
    {
        [JsonPropertyName("text_chat")]
        public bool TextChat { get; set; } = true;

        [JsonPropertyName("custom_tracks")]
        public bool CustomTracks { get; set; } = true;

        [JsonPropertyName("custom_vehicles")]
        public bool CustomVehicles { get; set; } = true;

        [JsonPropertyName("voice_chat")]
        public bool VoiceChat { get; set; } = true;

        public ServerFeaturesSettings Clone()
        {
            return new ServerFeaturesSettings
            {
                TextChat = TextChat,
                CustomTracks = CustomTracks,
                CustomVehicles = CustomVehicles,
                VoiceChat = VoiceChat
            };
        }
    }
}
