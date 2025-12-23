using System;

namespace TS.Audio
{
    public enum DistanceModel
    {
        Inverse,
        Linear,
        Exponential
    }

    public enum HrtfMode
    {
        Mono,
        Stereo
    }

    public enum HrtfDownmixMode
    {
        Average,
        Left,
        Right,
        Sum
    }

    public sealed class AudioSystemConfig
    {
        public uint SampleRate = 44100;
        public uint Channels = 2;
        public uint PeriodSizeInFrames = 256;
        public bool UseHrtf = false;
        public HrtfMode HrtfMode = HrtfMode.Mono;
        public HrtfDownmixMode HrtfDownmixMode = HrtfDownmixMode.Average;
        public string? HrtfSofaPath = null;
        public float SpeedOfSound = 343.0f;
        public float DopplerFactor = 1.0f;
        public bool UseCurveDistanceScaler = false;
        public float CurveDistanceScaler = 1.0f;
        public float MinDistance = 1.0f;
        public float MaxDistance = 10000.0f;
        public float RollOff = 1.0f;
        public DistanceModel DistanceModel = DistanceModel.Inverse;
        public bool UseVerticalVelocity = false;
    }

    public sealed class AudioOutputConfig
    {
        public string Name = "main";
        public uint SampleRate = 44100;
        public uint Channels = 2;
        public uint PeriodSizeInFrames = 256;
        public int? DeviceIndex = null;
    }
}
