using System;
using TS.Audio;

namespace TopSpeed.Audio
{
    internal static class AudioHelpers
    {
        public static void SetVolumePercent(this AudioSourceHandle handle, int percent)
        {
            var clamped = Math.Max(0, Math.Min(100, percent));
            handle.SetVolume(clamped / 100f);
        }

        public static void SetPanPercent(this AudioSourceHandle handle, int pan)
        {
            var clamped = Math.Max(-100, Math.Min(100, pan));
            handle.SetPan(clamped / 100f);
        }

        public static void SetFrequency(this AudioSourceHandle handle, int frequency)
        {
            if (frequency <= 0)
            {
                handle.SetPitch(0.001f);
                return;
            }

            var sampleRate = handle.InputSampleRate > 0 ? handle.InputSampleRate : 44100;
            var pitch = frequency / (float)sampleRate;
            if (pitch < 0.001f)
                pitch = 0.001f;
            handle.SetPitch(pitch);
        }

        public static void Restart(this AudioSourceHandle handle, bool loop)
        {
            handle.Stop();
            handle.SeekToStart();
            handle.Play(loop);
        }
    }
}
