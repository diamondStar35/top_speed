using System;

namespace TS.Audio
{
    [Flags]
    public enum CalculateFlags
    {
        Matrix = 1 << 0,
        Doppler = 1 << 1,
        LpfDirect = 1 << 2,
        LpfReverb = 1 << 3
    }

    public struct DspSettings
    {
        public float[] MatrixCoefficients;
        public float DopplerFactor;
    }

    public struct VoiceDetails
    {
        public int InputChannelCount;
        public int InputSampleRate;
    }

    internal static class AudioMath
    {
        public static float PitchRatioToSemitones(float ratio)
        {
            if (ratio <= 0f)
                return 0f;
            return (float)(12.0 * Math.Log(ratio, 2.0));
        }

        public static float SemitonesToPitchRatio(float semitones)
        {
            return (float)Math.Pow(2.0, semitones / 12.0);
        }
    }
}
