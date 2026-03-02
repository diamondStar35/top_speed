using System.Collections.Generic;
using System.Runtime.Serialization;

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

        [DataMember(Name = "network")]
        public SettingsNetworkDocument? Network { get; set; }

        [DataMember(Name = "accessibility")]
        public SettingsAccessibilityDocument? Accessibility { get; set; }
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

        [DataMember(Name = "musicPercent")]
        public int? MusicPercent { get; set; }

        [DataMember(Name = "onlineServerEventsPercent")]
        public int? OnlineServerEventsPercent { get; set; }

        [DataMember(Name = "hrtfAudio")]
        public bool? HrtfAudio { get; set; }

        [DataMember(Name = "stereoWidening")]
        public bool? StereoWidening { get; set; }

        [DataMember(Name = "autoDetectAudioDeviceFormat")]
        public bool? AutoDetectAudioDeviceFormat { get; set; }
    }

    [DataContract]
    internal sealed class SettingsInputDocument
    {
        [DataMember(Name = "forceFeedback")]
        public bool? ForceFeedback { get; set; }

        [DataMember(Name = "deviceMode")]
        public int? DeviceMode { get; set; }

        [DataMember(Name = "keyboard")]
        public SettingsKeyboardDocument? Keyboard { get; set; }

        [DataMember(Name = "joystick")]
        public SettingsJoystickDocument? Joystick { get; set; }
    }

    [DataContract]
    internal sealed class SettingsKeyboardDocument
    {
        [DataMember(Name = "left")] public int? Left { get; set; }
        [DataMember(Name = "right")] public int? Right { get; set; }
        [DataMember(Name = "throttle")] public int? Throttle { get; set; }
        [DataMember(Name = "brake")] public int? Brake { get; set; }
        [DataMember(Name = "gearUp")] public int? GearUp { get; set; }
        [DataMember(Name = "gearDown")] public int? GearDown { get; set; }
        [DataMember(Name = "horn")] public int? Horn { get; set; }
        [DataMember(Name = "requestInfo")] public int? RequestInfo { get; set; }
        [DataMember(Name = "currentGear")] public int? CurrentGear { get; set; }
        [DataMember(Name = "currentLapNr")] public int? CurrentLapNr { get; set; }
        [DataMember(Name = "currentRacePerc")] public int? CurrentRacePerc { get; set; }
        [DataMember(Name = "currentLapPerc")] public int? CurrentLapPerc { get; set; }
        [DataMember(Name = "currentRaceTime")] public int? CurrentRaceTime { get; set; }
        [DataMember(Name = "startEngine")] public int? StartEngine { get; set; }
        [DataMember(Name = "reportDistance")] public int? ReportDistance { get; set; }
        [DataMember(Name = "reportSpeed")] public int? ReportSpeed { get; set; }
        [DataMember(Name = "trackName")] public int? TrackName { get; set; }
        [DataMember(Name = "pause")] public int? Pause { get; set; }
    }

    [DataContract]
    internal sealed class SettingsJoystickDocument
    {
        [DataMember(Name = "left")] public int? Left { get; set; }
        [DataMember(Name = "right")] public int? Right { get; set; }
        [DataMember(Name = "throttle")] public int? Throttle { get; set; }
        [DataMember(Name = "brake")] public int? Brake { get; set; }
        [DataMember(Name = "gearUp")] public int? GearUp { get; set; }
        [DataMember(Name = "gearDown")] public int? GearDown { get; set; }
        [DataMember(Name = "horn")] public int? Horn { get; set; }
        [DataMember(Name = "requestInfo")] public int? RequestInfo { get; set; }
        [DataMember(Name = "currentGear")] public int? CurrentGear { get; set; }
        [DataMember(Name = "currentLapNr")] public int? CurrentLapNr { get; set; }
        [DataMember(Name = "currentRacePerc")] public int? CurrentRacePerc { get; set; }
        [DataMember(Name = "currentLapPerc")] public int? CurrentLapPerc { get; set; }
        [DataMember(Name = "currentRaceTime")] public int? CurrentRaceTime { get; set; }
        [DataMember(Name = "startEngine")] public int? StartEngine { get; set; }
        [DataMember(Name = "reportDistance")] public int? ReportDistance { get; set; }
        [DataMember(Name = "reportSpeed")] public int? ReportSpeed { get; set; }
        [DataMember(Name = "trackName")] public int? TrackName { get; set; }
        [DataMember(Name = "pause")] public int? Pause { get; set; }
        [DataMember(Name = "center")] public SettingsJoystickCenterDocument? Center { get; set; }
    }

    [DataContract]
    internal sealed class SettingsJoystickCenterDocument
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
        [DataMember(Name = "numberOfLaps")] public int? NumberOfLaps { get; set; }
        [DataMember(Name = "numberOfComputers")] public int? NumberOfComputers { get; set; }
        [DataMember(Name = "difficulty")] public int? Difficulty { get; set; }
        [DataMember(Name = "units")] public int? Units { get; set; }
        [DataMember(Name = "randomCustomTracks")] public bool? RandomCustomTracks { get; set; }
        [DataMember(Name = "randomCustomVehicles")] public bool? RandomCustomVehicles { get; set; }
        [DataMember(Name = "singleRaceCustomVehicles")] public bool? SingleRaceCustomVehicles { get; set; }
    }

    [DataContract]
    internal sealed class SettingsUiDocument
    {
        [DataMember(Name = "usageHints")] public bool? UsageHints { get; set; }
        [DataMember(Name = "menuWrapNavigation")] public bool? MenuWrapNavigation { get; set; }
        [DataMember(Name = "menuSoundPreset")] public string? MenuSoundPreset { get; set; }
        [DataMember(Name = "menuNavigatePanning")] public bool? MenuNavigatePanning { get; set; }
    }

    [DataContract]
    internal sealed class SettingsNetworkDocument
    {
        [DataMember(Name = "lastServerAddress")] public string? LastServerAddress { get; set; }
        [DataMember(Name = "defaultServerPort")] public int? DefaultServerPort { get; set; }
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
    }

    [DataContract]
    internal sealed class SettingsAccessibilityDocument
    {
        [DataMember(Name = "screenReaderRateMs")]
        public decimal? ScreenReaderRateMs { get; set; }
    }
}
