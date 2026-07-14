using Key = TopSpeed.Input.InputKey;
using System;
using System.Collections.Generic;
using TopSpeed.Input.Devices.Controller;

namespace TopSpeed.Input
{
    internal sealed class DriveSettings
    {
        public const int MinVoiceInputGainPercent = 0;
        public const int MaxVoiceInputGainPercent = 400;
        public const int DefaultVoiceInputGainPercent = 200;

        public DriveSettings()
        {
            RestoreDefaults();
        }

        public string Language { get; set; } = "en";
        public Dictionary<DriveIntent, AxisOrButton> ControllerBindings { get; set; } = new Dictionary<DriveIntent, AxisOrButton>();
        public Dictionary<DriveIntent, Key> KeyboardBindings { get; set; } = new Dictionary<DriveIntent, Key>();
        public PedalInvertMode ControllerThrottleInvertMode { get; set; }
        public PedalInvertMode ControllerBrakeInvertMode { get; set; }
        public PedalInvertMode ControllerClutchInvertMode { get; set; }
        public int ControllerSteeringDeadZone { get; set; }
        public State ControllerCenter { get; set; }

        public bool ForceFeedback { get; set; }
        public KeyboardProgressiveRate KeyboardProgressiveRate { get; set; }
        public bool AndroidUseMotionSteering { get; set; }
        public InputDeviceMode DeviceMode { get; set; }

        public AutomaticInfoMode AutomaticInfo { get; set; }
        public CopilotMode Copilot { get; set; }
        public CurveAnnouncementMode CurveAnnouncement { get; set; }
        public float CurveAnnouncementLeadTimeSeconds { get; set; }
        public int NrOfLaps { get; set; }
        public int NrOfComputers { get; set; }
        public RaceDifficulty Difficulty { get; set; }
        public UnitSystem Units { get; set; }
        public bool FuelConsumptionEnabled { get; set; }
        public bool TireWearEnabled { get; set; }
        public float MusicVolume { get; set; }
        public AudioVolumeSettings AudioVolumes { get; set; } = new AudioVolumeSettings();
        public bool HrtfAudio { get; set; }
        public bool StereoWidening { get; set; }
        public bool AutoDetectAudioDeviceFormat { get; set; }
        public string VoiceInputDeviceName { get; set; } = string.Empty;
        public int VoiceInputGainPercent { get; set; }
        public bool RandomCustomTracks { get; set; }
        public bool RandomCustomVehicles { get; set; }
        public bool SingleRaceCustomVehicles { get; set; }
        public bool KeepDownloadedVehiclesPrompt { get; set; }
        public string LastServerAddress { get; set; } = string.Empty;
        public int DefaultServerPort { get; set; }
        public string DefaultCallSign { get; set; } = string.Empty;
        public bool UseUpdateProxy { get; set; }
        public string UpdateProxyUrlPrefix { get; set; } = string.Empty;
        public float ScreenReaderRateMs { get; set; }
        public ulong? SpeechBackendId { get; set; }
        public SpeechOutputMode SpeechMode { get; set; }
        public int? SpeechVoiceIndex { get; set; }
        public float SpeechRate { get; set; }
        public bool ScreenReaderInterrupt { get; set; }
        public bool UsageHints { get; set; }
        public bool MenuAutoFocus { get; set; }
        public bool MenuWrapNavigation { get; set; }
        public string MenuSoundPreset { get; set; } = "1";
        public bool MenuNavigatePanning { get; set; }
        public bool PlayLogoAtStartup { get; set; }
        public bool AutoCheckUpdates { get; set; }
        public string RadioLastFolder { get; set; } = string.Empty;
        public bool RadioShuffle { get; set; }
        public Dictionary<string, Key> ShortcutKeyBindings { get; set; } = new Dictionary<string, Key>(StringComparer.Ordinal);
        public Dictionary<string, TopSpeed.Shortcuts.ShortcutModifiers> ShortcutModifierBindings { get; set; } = new Dictionary<string, TopSpeed.Shortcuts.ShortcutModifiers>(StringComparer.Ordinal);
        public List<SavedServerEntry> SavedServers { get; set; } = new List<SavedServerEntry>();

        public bool UseController
        {
            get => DeviceMode != InputDeviceMode.Keyboard;
            set => DeviceMode = value ? InputDeviceMode.Controller : InputDeviceMode.Keyboard;
        }

        public Key GetKeyboardBinding(DriveIntent intent)
        {
            return KeyboardBindings != null && KeyboardBindings.TryGetValue(intent, out var key)
                ? key
                : Key.Unknown;
        }

        public AxisOrButton GetControllerBinding(DriveIntent intent)
        {
            return ControllerBindings != null && ControllerBindings.TryGetValue(intent, out var axis)
                ? axis
                : AxisOrButton.AxisNone;
        }

        public void SetKeyboardBinding(DriveIntent intent, Key key)
        {
            KeyboardBindings ??= new Dictionary<DriveIntent, Key>();
            KeyboardBindings[intent] = key;
        }

        public void SetControllerBinding(DriveIntent intent, AxisOrButton axis)
        {
            ControllerBindings ??= new Dictionary<DriveIntent, AxisOrButton>();
            ControllerBindings[intent] = axis;
        }

        public void RestoreDefaults()
        {
            Language = "en";
            ControllerBindings = CreateDefaultControllerBindings();
            KeyboardBindings = CreateDefaultKeyboardBindings();
            ControllerThrottleInvertMode = PedalInvertMode.Auto;
            ControllerBrakeInvertMode = PedalInvertMode.Auto;
            ControllerClutchInvertMode = PedalInvertMode.Auto;
            ControllerSteeringDeadZone = 1;
            ControllerCenter = default;

            ForceFeedback = false;
            KeyboardProgressiveRate = KeyboardProgressiveRate.Off;
            AndroidUseMotionSteering = true;
            DeviceMode = InputDeviceMode.Keyboard;
            AutomaticInfo = AutomaticInfoMode.On;
            Copilot = CopilotMode.All;
            CurveAnnouncement = CurveAnnouncementMode.SpeedDependent;
            CurveAnnouncementLeadTimeSeconds = 1.8f;
            NrOfLaps = 3;
            NrOfComputers = 3;
            Difficulty = RaceDifficulty.Easy;
            Units = UnitSystem.Metric;
            FuelConsumptionEnabled = false;
            TireWearEnabled = false;
            MusicVolume = 0.6f;
            AudioVolumes = new AudioVolumeSettings();
            AudioVolumes.RestoreDefaults((int)Math.Round(MusicVolume * 100f));
            HrtfAudio = true;
            StereoWidening = false;
            AutoDetectAudioDeviceFormat = true;
            VoiceInputDeviceName = string.Empty;
            VoiceInputGainPercent = DefaultVoiceInputGainPercent;
            RandomCustomTracks = false;
            RandomCustomVehicles = false;
            SingleRaceCustomVehicles = false;
            KeepDownloadedVehiclesPrompt = true;
            LastServerAddress = string.Empty;
            DefaultServerPort = 28630;
            DefaultCallSign = string.Empty;
            UseUpdateProxy = false;
            UpdateProxyUrlPrefix = string.Empty;
            ScreenReaderRateMs = 0f;
            SpeechBackendId = null;
            SpeechMode = SpeechOutputMode.Speech;
            SpeechVoiceIndex = null;
            SpeechRate = 0.5f;
            ScreenReaderInterrupt = false;
            UsageHints = true;
            MenuAutoFocus = true;
            MenuWrapNavigation = true;
            MenuSoundPreset = "1";
            MenuNavigatePanning = false;
            PlayLogoAtStartup = true;
            AutoCheckUpdates = true;
            RadioLastFolder = string.Empty;
            RadioShuffle = false;
            ShortcutKeyBindings = new Dictionary<string, Key>(StringComparer.Ordinal);
            ShortcutModifierBindings = new Dictionary<string, TopSpeed.Shortcuts.ShortcutModifiers>(StringComparer.Ordinal);
            SavedServers = new List<SavedServerEntry>();
        }

        public void SyncMusicVolumeFromAudioCategories()
        {
            AudioVolumes ??= new AudioVolumeSettings();
            AudioVolumes.ClampAll();
            MusicVolume = AudioVolumeSettings.PercentToScalar(AudioVolumes.MusicPercent);
        }

        public void SyncAudioCategoriesFromMusicVolume()
        {
            AudioVolumes ??= new AudioVolumeSettings();
            AudioVolumes.MusicPercent = AudioVolumeSettings.ClampPercent((int)Math.Round(Math.Max(0f, Math.Min(1f, MusicVolume)) * 100f));
            AudioVolumes.ClampAll();
        }

        private static Dictionary<DriveIntent, AxisOrButton> CreateDefaultControllerBindings()
        {
            return new Dictionary<DriveIntent, AxisOrButton>
            {
                [DriveIntent.SteerLeft] = AxisOrButton.AxisXNeg,
                [DriveIntent.SteerRight] = AxisOrButton.AxisXPos,
                [DriveIntent.Throttle] = AxisOrButton.AxisRzPos,
                [DriveIntent.Brake] = AxisOrButton.AxisZPos,
                [DriveIntent.Clutch] = AxisOrButton.AxisSlider1Pos,
                [DriveIntent.GearUp] = AxisOrButton.Button2,
                [DriveIntent.GearDown] = AxisOrButton.Button1,
                [DriveIntent.Horn] = AxisOrButton.Button3,
                [DriveIntent.RequestInfo] = AxisOrButton.Button4,
                [DriveIntent.CurrentGear] = AxisOrButton.Button5,
                [DriveIntent.CurrentLapNr] = AxisOrButton.Button6,
                [DriveIntent.CurrentRacePerc] = AxisOrButton.Button7,
                [DriveIntent.CurrentLapPerc] = AxisOrButton.Button8,
                [DriveIntent.CurrentRaceTime] = AxisOrButton.Button9,
                [DriveIntent.StartEngine] = AxisOrButton.Button10,
                [DriveIntent.ReportDistance] = AxisOrButton.Button11,
                [DriveIntent.ReportSpeed] = AxisOrButton.Button12,
                [DriveIntent.ReportFuel] = AxisOrButton.Button15,
                [DriveIntent.ReportTireState] = AxisOrButton.Button16,
                [DriveIntent.TrackName] = AxisOrButton.Button13,
                [DriveIntent.Pause] = AxisOrButton.Button14,
                [DriveIntent.Pit] = AxisOrButton.Button15
            };
        }

        private static Dictionary<DriveIntent, Key> CreateDefaultKeyboardBindings()
        {
            return new Dictionary<DriveIntent, Key>
            {
                [DriveIntent.SteerLeft] = Key.Left,
                [DriveIntent.SteerRight] = Key.Right,
                [DriveIntent.Throttle] = Key.Up,
                [DriveIntent.Brake] = Key.Down,
                [DriveIntent.Clutch] = Key.BothShift,
                [DriveIntent.GearUp] = Key.A,
                [DriveIntent.GearDown] = Key.Z,
                [DriveIntent.Horn] = Key.Space,
                [DriveIntent.RequestInfo] = Key.Tab,
                [DriveIntent.CurrentGear] = Key.Q,
                [DriveIntent.CurrentLapNr] = Key.W,
                [DriveIntent.CurrentRacePerc] = Key.E,
                [DriveIntent.CurrentLapPerc] = Key.R,
                [DriveIntent.CurrentRaceTime] = Key.T,
                [DriveIntent.StartEngine] = Key.Return,
                [DriveIntent.ReportDistance] = Key.C,
                [DriveIntent.ReportSpeed] = Key.S,
                [DriveIntent.ReportFuel] = Key.X,
                [DriveIntent.ReportTireState] = Key.B,
                [DriveIntent.TrackName] = Key.F9,
                [DriveIntent.Pause] = Key.P,
                [DriveIntent.Pit] = Key.I
            };
        }
    }
}
