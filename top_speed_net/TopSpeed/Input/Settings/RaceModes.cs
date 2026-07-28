namespace TopSpeed.Input
{
    internal enum InputDeviceMode
    {
        Keyboard,
        Controller,
        Both
    }

    internal enum KeyboardProgressiveRate : byte
    {
        Off = 0,
        Fastest_0_25s = 1,
        Fast_0_50s = 2,
        Moderate_0_75s = 3,
        Slowest_1_00s = 4
    }

    internal enum PedalInvertMode
    {
        Auto = 0,
        Normal = 1,
        Inverted = 2
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

    internal enum CurveAnnouncementStyle
    {
        Spoken = 0,
        Tones = 1
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

    internal enum SpeechOutputMode
    {
        Speech = 0,
        Braille = 1,
        SpeechAndBraille = 2
    }
}

