using System.Collections.Generic;
using System.Runtime.Serialization;
using TopSpeed.Input;

namespace TopSpeed.Core.Settings
{
    [DataContract]
    internal sealed class SettingsFileDocument
    {
        [DataMember(Name = "schemaVersion")]
        public int? SchemaVersion { get; set; }

        [DataMember(Name = "language")]
        public string? Language { get; set; }

        [DataMember(Name = "audio")]
        public SettingsAudioDocument? Audio { get; set; }

        [DataMember(Name = "input")]
        public SettingsInputDocument? Input { get; set; }

        [DataMember(Name = "race")]
        public SettingsRaceDocument? Race { get; set; }

        [DataMember(Name = "ui")]
        public SettingsUiDocument? Ui { get; set; }

        [DataMember(Name = "speech")]
        public SettingsSpeechDocument? Speech { get; set; }

        [DataMember(Name = "network")]
        public SettingsNetworkDocument? Network { get; set; }

        [DataMember(Name = "radio")]
        public SettingsRadioDocument? Radio { get; set; }
    }

    [DataContract]
    internal sealed class SettingsAudioDocument
    {
        [DataMember(Name = "musicVolume")]
        public decimal? MusicVolume { get; set; }

        [DataMember(Name = "masterVolumePercent")]
        public int? MasterVolumePercent { get; set; }

        [DataMember(Name = "playerVehicleEnginePercent")]
        public int? PlayerVehicleEnginePercent { get; set; }

        [DataMember(Name = "playerVehicleEventsPercent")]
        public int? PlayerVehicleEventsPercent { get; set; }

        [DataMember(Name = "otherVehicleEnginePercent")]
        public int? OtherVehicleEnginePercent { get; set; }

        [DataMember(Name = "otherVehicleEventsPercent")]
        public int? OtherVehicleEventsPercent { get; set; }

        [DataMember(Name = "surfaceLoopsPercent")]
        public int? SurfaceLoopsPercent { get; set; }

        [DataMember(Name = "radioPercent")]
        public int? RadioPercent { get; set; }

        [DataMember(Name = "ambientsAndSourcesPercent")]
        public int? AmbientsAndSourcesPercent { get; set; }

        [DataMember(Name = "musicPercent")]
        public int? MusicPercent { get; set; }

        [DataMember(Name = "onlineServerEventsPercent")]
        public int? OnlineServerEventsPercent { get; set; }

        [DataMember(Name = "communicatorPercent")]
        public int? CommunicatorPercent { get; set; }

        [DataMember(Name = "hrtfAudio")]
        public bool? HrtfAudio { get; set; }

        [DataMember(Name = "stereoWidening")]
        public bool? StereoWidening { get; set; }

        [DataMember(Name = "autoDetectAudioDeviceFormat")]
        public bool? AutoDetectAudioDeviceFormat { get; set; }

        [DataMember(Name = "voiceInputDevice")]
        public string? VoiceInputDevice { get; set; }

        [DataMember(Name = "voiceInputGainPercent")]
        public int? VoiceInputGainPercent { get; set; }

    }

    [DataContract]
    internal sealed class SettingsInputDocument
    {
        [DataMember(Name = "forceFeedback")]
        public bool? ForceFeedback { get; set; }

        [DataMember(Name = "keyboardProgressiveRate")]
        public int? KeyboardProgressiveRate { get; set; }

        [DataMember(Name = "deviceMode")]
        public int? DeviceMode { get; set; }

        [DataMember(Name = "androidUseMotionSteering")]
        public bool? AndroidUseMotionSteering { get; set; }

        [DataMember(Name = "keyboard")]
        public SettingsKeyboardDocument? Keyboard { get; set; }

        [DataMember(Name = "menuShortcuts")]
        public SettingsMenuShortcutsDocument? MenuShortcuts { get; set; }

        [DataMember(Name = "controller")]
        public SettingsControllerDocument? Controller { get; set; }
    }

    [DataContract]
    internal sealed class SettingsMenuShortcutsDocument
    {
        [DataMember(Name = "bindings")]
        public List<SettingsMenuShortcutBindingDocument>? Bindings { get; set; }
    }

    [DataContract]
    internal sealed class SettingsMenuShortcutBindingDocument
    {
        [DataMember(Name = "id")]
        public string? Id { get; set; }

        [DataMember(Name = "key")]
        public int? Key { get; set; }

        [DataMember(Name = "shift")]
        public bool? Shift { get; set; }

        [DataMember(Name = "control")]
        public bool? Control { get; set; }

        [DataMember(Name = "alt")]
        public bool? Alt { get; set; }
    }

    [DataContract]
    internal sealed class SettingsKeyboardDocument
    {
        [DataMember(Name = "bindings")]
        public List<SettingsKeyboardBindingDocument>? Bindings { get; set; }
    }

    [DataContract]
    internal sealed class SettingsKeyboardBindingDocument
    {
        [DataMember(Name = "intent")]
        public string? Intent { get; set; }

        [DataMember(Name = "key")]
        public int? Key { get; set; }
    }

    [DataContract]
    internal sealed class SettingsControllerDocument
    {
        [DataMember(Name = "bindings")]
        public List<SettingsControllerBindingDocument>? Bindings { get; set; }

        [DataMember(Name = "throttleInvertMode")] public int? ThrottleInvertMode { get; set; }
        [DataMember(Name = "brakeInvertMode")] public int? BrakeInvertMode { get; set; }
        [DataMember(Name = "clutchInvertMode")] public int? ClutchInvertMode { get; set; }
        [DataMember(Name = "steeringDeadZone")] public int? SteeringDeadZone { get; set; }
        [DataMember(Name = "center")] public SettingsControllerCenterDocument? Center { get; set; }
    }

    [DataContract]
    internal sealed class SettingsControllerBindingDocument
    {
        [DataMember(Name = "intent")]
        public string? Intent { get; set; }

        [DataMember(Name = "axis")]
        public int? Axis { get; set; }
    }

    [DataContract]
    internal sealed class SettingsControllerCenterDocument
    {
        [DataMember(Name = "x")] public int? X { get; set; }
        [DataMember(Name = "y")] public int? Y { get; set; }
        [DataMember(Name = "z")] public int? Z { get; set; }
        [DataMember(Name = "rx")] public int? Rx { get; set; }
        [DataMember(Name = "ry")] public int? Ry { get; set; }
        [DataMember(Name = "rz")] public int? Rz { get; set; }
        [DataMember(Name = "slider1")] public int? Slider1 { get; set; }
        [DataMember(Name = "slider2")] public int? Slider2 { get; set; }
    }

    [DataContract]
    internal sealed class SettingsRaceDocument
    {
        [DataMember(Name = "automaticInfo")] public int? AutomaticInfo { get; set; }
        [DataMember(Name = "copilot")] public int? Copilot { get; set; }
        [DataMember(Name = "curveAnnouncement")] public int? CurveAnnouncement { get; set; }
        [DataMember(Name = "curveAnnouncementLeadTimeSeconds")] public decimal? CurveAnnouncementLeadTimeSeconds { get; set; }
        [DataMember(Name = "numberOfLaps")] public int? NumberOfLaps { get; set; }
        [DataMember(Name = "numberOfComputers")] public int? NumberOfComputers { get; set; }
        [DataMember(Name = "difficulty")] public int? Difficulty { get; set; }
        [DataMember(Name = "units")] public int? Units { get; set; }
        [DataMember(Name = "randomCustomTracks")] public bool? RandomCustomTracks { get; set; }
        [DataMember(Name = "randomCustomVehicles")] public bool? RandomCustomVehicles { get; set; }
        [DataMember(Name = "singleRaceCustomVehicles")] public bool? SingleRaceCustomVehicles { get; set; }
        [DataMember(Name = "keepDownloadedVehiclesPrompt")] public bool? KeepDownloadedVehiclesPrompt { get; set; }
    }

    [DataContract]
    internal sealed class SettingsUiDocument
    {
        [DataMember(Name = "usageHints")] public bool? UsageHints { get; set; }
        [DataMember(Name = "menuAutoFocus")] public bool? MenuAutoFocus { get; set; }
        [DataMember(Name = "menuWrapNavigation")] public bool? MenuWrapNavigation { get; set; }
        [DataMember(Name = "menuSoundPreset")] public string? MenuSoundPreset { get; set; }
        [DataMember(Name = "menuNavigatePanning")] public bool? MenuNavigatePanning { get; set; }
        [DataMember(Name = "playLogoAtStartup")] public bool? PlayLogoAtStartup { get; set; }
        [DataMember(Name = "autoCheckUpdates")] public bool? AutoCheckUpdates { get; set; }
    }

    [DataContract]
    internal sealed class SettingsNetworkDocument
    {
        [DataMember(Name = "lastServerAddress")] public string? LastServerAddress { get; set; }
        [DataMember(Name = "defaultServerPort")] public int? DefaultServerPort { get; set; }
        [DataMember(Name = "defaultCallSign")] public string? DefaultCallSign { get; set; }
        [DataMember(Name = "useUpdateProxy")] public bool? UseUpdateProxy { get; set; }
        [DataMember(Name = "updateProxyUrlPrefix")] public string? UpdateProxyUrlPrefix { get; set; }
        [DataMember(Name = "savedServers")] public SettingsSavedServersDocument? SavedServers { get; set; }
    }

    [DataContract]
    internal sealed class SettingsSavedServersDocument
    {
        [DataMember(Name = "servers")] public List<SettingsSavedServerDocument>? Servers { get; set; }
    }

    [DataContract]
    internal sealed class SettingsSavedServerDocument
    {
        [DataMember(Name = "name")] public string? Name { get; set; }
        [DataMember(Name = "host")] public string? Host { get; set; }
        [DataMember(Name = "port")] public int? Port { get; set; }
        [DataMember(Name = "defaultCallSign")] public string? DefaultCallSign { get; set; }
    }

    [DataContract]
    internal sealed class SettingsSpeechDocument
    {
        [DataMember(Name = "mode")]
        public int? Mode { get; set; }

        [DataMember(Name = "screenReaderRateMs")]
        public decimal? ScreenReaderRateMs { get; set; }

        [DataMember(Name = "backend")]
        public ulong? Backend { get; set; }

        [DataMember(Name = "voice")]
        public int? Voice { get; set; }

        [DataMember(Name = "rate")]
        public decimal? Rate { get; set; }

        [DataMember(Name = "interrupt")]
        public bool? Interrupt { get; set; }
    }

    [DataContract]
    internal sealed class SettingsRadioDocument
    {
        [DataMember(Name = "lastFolder")]
        public string? LastFolder { get; set; }

        [DataMember(Name = "shuffleEnabled")]
        public bool? ShuffleEnabled { get; set; }
    }
}

