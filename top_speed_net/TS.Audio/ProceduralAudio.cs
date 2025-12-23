using System;
using System.Runtime.InteropServices;
using MiniAudioEx.Native;

namespace TS.Audio
{
    public delegate void ProceduralAudioCallback(float[] buffer, int frames, int channels, ref ulong frameIndex);

    internal sealed class ProceduralAudioGenerator : IDisposable
    {
        private readonly ProceduralAudioCallback _callback;
        private readonly ma_procedural_data_source_proc _proc;
        private GCHandle _handle;
        private float[] _buffer;
        private ulong _frameIndex;

        public ma_procedural_data_source_proc Proc => _proc;
        public IntPtr UserData => GCHandle.ToIntPtr(_handle);

        public ProceduralAudioGenerator(ProceduralAudioCallback callback)
        {
            _callback = callback ?? throw new ArgumentNullException(nameof(callback));
            _proc = OnProcess;
            _handle = GCHandle.Alloc(this);
            _buffer = new float[0];
        }

        public void Dispose()
        {
            if (_handle.IsAllocated)
                _handle.Free();
        }

        private static void OnProcess(IntPtr pUserData, IntPtr pFramesOut, ulong frameCount, uint channels)
        {
            var handle = GCHandle.FromIntPtr(pUserData);
            var generator = handle.Target as ProceduralAudioGenerator;
            if (generator == null)
                return;

            int frameCountInt = (int)frameCount;
            int channelsInt = (int)channels;
            int sampleCount = frameCountInt * channelsInt;
            if (sampleCount <= 0)
                return;

            if (generator._buffer.Length < sampleCount)
                generator._buffer = new float[sampleCount];

            for (int i = 0; i < sampleCount; i++)
                generator._buffer[i] = 0f;

            ulong frameIndex = generator._frameIndex;
            try
            {
                generator._callback(generator._buffer, frameCountInt, channelsInt, ref frameIndex);
            }
            catch
            {
                for (int i = 0; i < sampleCount; i++)
                    generator._buffer[i] = 0f;
            }

            if (frameIndex == generator._frameIndex)
                generator._frameIndex += (ulong)frameCountInt;
            else
                generator._frameIndex = frameIndex;

            Marshal.Copy(generator._buffer, 0, pFramesOut, sampleCount);
        }
    }
}
