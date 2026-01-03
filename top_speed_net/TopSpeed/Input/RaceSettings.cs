using SharpDX.DirectInput;
using TopSpeed.Common;

namespace TopSpeed.Input
{
    internal enum InputDeviceMode
    {
        Keyboard,
        Joystick,
        Both
    }

    internal enum CopilotMode
    {
        Off = 0,
        CurvesOnly = 1,
        All = 2
    }

    internal enum CurveAnnouncementMode
    {
        FixedDistance = 0,
        SpeedDependent = 1
    }

    internal enum AutomaticInfoMode
    {
        Off = 0,
        LapsOnly = 1,
        On = 2
    }

    internal enum RaceDifficulty
    {
        Easy = 0,
        Normal = 1,
        Hard = 2
    }

    internal enum UnitSystem
    {
        Metric = 0,
        Imperial = 1
    }

    internal sealed class RaceSettings
    {
        public RaceSettings()
        {
            RestoreDefaults();
        }

        public string Language { get; set; } = "en";
        public JoystickAxisOrButton JoystickLeft { get; set; }
        public JoystickAxisOrButton JoystickRight { get; set; }
        public JoystickAxisOrButton JoystickThrottle { get; set; }
        public JoystickAxisOrButton JoystickBrake { get; set; }
        public JoystickAxisOrButton JoystickGearUp { get; set; }
        public JoystickAxisOrButton JoystickGearDown { get; set; }
        public JoystickAxisOrButton JoystickHorn { get; set; }
        public JoystickAxisOrButton JoystickRequestInfo { get; set; }
        public JoystickAxisOrButton JoystickCurrentGear { get; set; }
        public JoystickAxisOrButton JoystickCurrentLapNr { get; set; }
        public JoystickAxisOrButton JoystickCurrentRacePerc { get; set; }
        public JoystickAxisOrButton JoystickCurrentLapPerc { get; set; }
        public JoystickAxisOrButton JoystickCurrentRaceTime { get; set; }
        public JoystickAxisOrButton JoystickStartEngine { get; set; }
        public JoystickAxisOrButton JoystickReportDistance { get; set; }
        public JoystickAxisOrButton JoystickReportSpeed { get; set; }
        public JoystickAxisOrButton JoystickTrackName { get; set; }
        public JoystickAxisOrButton JoystickPause { get; set; }
        public JoystickStateSnapshot JoystickCenter { get; set; }

        public Key KeyLeft { get; set; }
        public Key KeyRight { get; set; }
        public Key KeyThrottle { get; set; }
        public Key KeyBrake { get; set; }
        public Key KeyGearUp { get; set; }
        public Key KeyGearDown { get; set; }
        public Key KeyHorn { get; set; }
        public Key KeyRequestInfo { get; set; }
        public Key KeyCurrentGear { get; set; }
        public Key KeyCurrentLapNr { get; set; }
        public Key KeyCurrentRacePerc { get; set; }
        public Key KeyCurrentLapPerc { get; set; }
        public Key KeyCurrentRaceTime { get; set; }
        public Key KeyStartEngine { get; set; }
        public Key KeyReportDistance { get; set; }
        public Key KeyReportSpeed { get; set; }
        public Key KeyTrackName { get; set; }
        public Key KeyPause { get; set; }

        public bool ForceFeedback { get; set; }
        public InputDeviceMode DeviceMode { get; set; }

        public AutomaticInfoMode AutomaticInfo { get; set; }
        public CopilotMode Copilot { get; set; }
        public CurveAnnouncementMode CurveAnnouncement { get; set; }
        public int NrOfLaps { get; set; }
        public int NrOfComputers { get; set; }
        public RaceDifficulty Difficulty { get; set; }
        public UnitSystem Units { get; set; }
        public int ServerNumber { get; set; }
        public float MusicVolume { get; set; }
        public bool ThreeDSound { get; set; }
        public bool ReverseStereo { get; set; }
        public bool RandomCustomTracks { get; set; }
        public bool RandomCustomVehicles { get; set; }
        public bool SingleRaceCustomVehicles { get; set; }
        public string LastServerAddress { get; set; } = string.Empty;
        public int ServerPort { get; set; }
        public float ScreenReaderRateMs { get; set; }

        public bool UseJoystick
        {
            get => DeviceMode != InputDeviceMode.Keyboard;
            set => DeviceMode = value ? InputDeviceMode.Joystick : InputDeviceMode.Keyboard;
        }

        public void RestoreDefaults()
        {
            Language = "en";
            JoystickLeft = JoystickAxisOrButton.AxisXNeg;
            JoystickRight = JoystickAxisOrButton.AxisXPos;
            JoystickThrottle = JoystickAxisOrButton.AxisRzPos;
            JoystickBrake = JoystickAxisOrButton.AxisZPos;
            JoystickGearUp = JoystickAxisOrButton.Button2;
            JoystickGearDown = JoystickAxisOrButton.Button1;
            JoystickHorn = JoystickAxisOrButton.Button3;
            JoystickRequestInfo = JoystickAxisOrButton.Button4;
            JoystickCurrentGear = JoystickAxisOrButton.Button5;
            JoystickCurrentLapNr = JoystickAxisOrButton.Button6;
            JoystickCurrentRacePerc = JoystickAxisOrButton.Button7;
            JoystickCurrentLapPerc = JoystickAxisOrButton.Button8;
            JoystickCurrentRaceTime = JoystickAxisOrButton.Button9;
            JoystickStartEngine = JoystickAxisOrButton.Button10;
            JoystickReportDistance = JoystickAxisOrButton.Button11;
            JoystickReportSpeed = JoystickAxisOrButton.Button12;
            JoystickTrackName = JoystickAxisOrButton.Button13;
            JoystickPause = JoystickAxisOrButton.Button14;
            JoystickCenter = default;

            KeyLeft = Key.Left;
            KeyRight = Key.Right;
            KeyThrottle = Key.Up;
            KeyBrake = Key.Down;
            KeyGearUp = Key.A;
            KeyGearDown = Key.Z;
            KeyHorn = Key.Space;
            KeyRequestInfo = Key.Tab;
            KeyCurrentGear = Key.Q;
            KeyCurrentLapNr = Key.W;
            KeyCurrentRacePerc = Key.E;
            KeyCurrentLapPerc = Key.R;
            KeyCurrentRaceTime = Key.T;
            KeyStartEngine = Key.Return;
            KeyReportDistance = Key.C;
            KeyReportSpeed = Key.S;
            KeyTrackName = Key.F9;
            KeyPause = Key.P;

            ForceFeedback = false;
            DeviceMode = InputDeviceMode.Keyboard;
            AutomaticInfo = AutomaticInfoMode.On;
            Copilot = CopilotMode.All;
            CurveAnnouncement = CurveAnnouncementMode.SpeedDependent;
            NrOfLaps = 3;
            NrOfComputers = 3;
            Difficulty = RaceDifficulty.Easy;
            Units = UnitSystem.Metric;
            ServerNumber = Algorithm.RandomInt(4999) + 1000;
            MusicVolume = 0.6f;
            ThreeDSound = true;
            ReverseStereo = false;
            RandomCustomTracks = false;
            RandomCustomVehicles = false;
            SingleRaceCustomVehicles = false;
            MusicVolume = 0.6f;
            LastServerAddress = string.Empty;
            ServerPort = 0;
            ScreenReaderRateMs = 0f;
        }
    }
}
