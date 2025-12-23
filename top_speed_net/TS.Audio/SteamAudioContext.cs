using System;
using System.Numerics;
using SteamAudio;

namespace TS.Audio
{
    public sealed class SteamAudioContext : IDisposable
    {
        public IPL.Context Context;
        public IPL.Hrtf Hrtf;
        public readonly int SampleRate;
        public readonly int FrameSize;
        public IPL.CoordinateSpace3 ListenerTransform;

        public SteamAudioContext(int sampleRate, int frameSize, string? hrtfSofaPath)
        {
            SampleRate = sampleRate;
            FrameSize = frameSize;
            ListenerTransform = CreateIdentityTransform();

            var contextSettings = new IPL.ContextSettings
            {
                Version = IPL.Version,
                LogCallback = null,
                AllocateCallback = null,
                FreeCallback = null,
                SimdLevel = IPL.SimdLevel.Avx2,
                Flags = 0
            };

            var error = IPL.ContextCreate(in contextSettings, out Context);
            if (error != IPL.Error.Success)
            {
                throw new InvalidOperationException("Failed to create SteamAudio context: " + error);
            }

            var hrtfSettings = new IPL.HrtfSettings
            {
                Type = string.IsNullOrWhiteSpace(hrtfSofaPath) ? IPL.HrtfType.Default : IPL.HrtfType.Sofa,
                SofaFileName = string.IsNullOrWhiteSpace(hrtfSofaPath) ? null : hrtfSofaPath,
                SofaData = IntPtr.Zero,
                SofaDataSize = 0,
                Volume = 1.0f,
                NormType = IPL.HrtfNormType.None
            };

            var audioSettings = new IPL.AudioSettings
            {
                SamplingRate = sampleRate,
                FrameSize = frameSize
            };

            error = IPL.HrtfCreate(Context, in audioSettings, in hrtfSettings, out Hrtf);
            if (error != IPL.Error.Success)
            {
                IPL.ContextRelease(ref Context);
                Context = default;
                throw new InvalidOperationException("Failed to create SteamAudio HRTF: " + error);
            }
        }

        public void UpdateListener(Vector3 position, Vector3 forward, Vector3 up)
        {
            if (Context.Handle == IntPtr.Zero)
                return;

            var normForward = Vector3.Normalize(forward);
            var normUp = Vector3.Normalize(up);
            var right = Vector3.Normalize(Vector3.Cross(normForward, normUp));

            ListenerTransform = new IPL.CoordinateSpace3
            {
                Right = ToIpl(right),
                Up = ToIpl(normUp),
                Ahead = new IPL.Vector3 { X = -normForward.X, Y = -normForward.Y, Z = -normForward.Z },
                Origin = ToIpl(position)
            };
        }

        public void Dispose()
        {
            if (Hrtf.Handle != IntPtr.Zero)
            {
                IPL.HrtfRelease(ref Hrtf);
                Hrtf = default;
            }

            if (Context.Handle != IntPtr.Zero)
            {
                IPL.ContextRelease(ref Context);
                Context = default;
            }
        }

        public static IPL.Vector3 ToIpl(Vector3 v)
        {
            return new IPL.Vector3 { X = v.X, Y = v.Y, Z = v.Z };
        }

        private static IPL.CoordinateSpace3 CreateIdentityTransform()
        {
            return new IPL.CoordinateSpace3
            {
                Right = new IPL.Vector3 { X = 1, Y = 0, Z = 0 },
                Up = new IPL.Vector3 { X = 0, Y = 1, Z = 0 },
                Ahead = new IPL.Vector3 { X = 0, Y = 0, Z = -1 },
                Origin = new IPL.Vector3 { X = 0, Y = 0, Z = 0 }
            };
        }
    }
}
