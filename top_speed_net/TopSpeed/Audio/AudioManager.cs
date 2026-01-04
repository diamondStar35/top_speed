using System;
using System.IO;
using System.Numerics;
using TS.Audio;

namespace TopSpeed.Audio
{
    internal sealed class AudioManager : IDisposable
    {
        private readonly AudioSystem _system;
        private readonly AudioOutput _output;
        public AudioManager(bool useHrtf = false)
        {
            var config = new AudioSystemConfig
            {
                UseHrtf = useHrtf
            };
            _system = new AudioSystem(config);
            _output = _system.CreateOutput(new AudioOutputConfig { Name = "main" });
        }

        public AudioSourceHandle CreateSource(string path, bool streamFromDisk = true, bool useHrtf = false)
        {
            if (!File.Exists(path))
                throw new FileNotFoundException("Audio file not found.", path);
            return _output.CreateSource(path, streamFromDisk, useHrtf);
        }

        public AudioSourceHandle CreateLoopingSource(string path, bool useHrtf = false)
        {
            return CreateSource(path, streamFromDisk: false, useHrtf: useHrtf);
        }

        public AudioSourceHandle CreateSpatialSource(string path, bool streamFromDisk = true, bool allowHrtf = true)
        {
            if (!File.Exists(path))
                throw new FileNotFoundException("Audio file not found.", path);

            return _output.CreateSpatialSource(path, streamFromDisk, allowHrtf);
        }

        public AudioSourceHandle CreateLoopingSpatialSource(string path, bool allowHrtf = true)
        {
            return CreateSpatialSource(path, streamFromDisk: false, allowHrtf: allowHrtf);
        }

        public void Update()
        {
            _system.Update();
        }

        public void UpdateListener(Vector3 position, Vector3 forward, Vector3 up, Vector3 velocity)
        {
            _system.UpdateListenerAll(position, forward, up, velocity);
        }

        public void Dispose()
        {
            _output.Dispose();
            _system.Dispose();
        }
    }
}
