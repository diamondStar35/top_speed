using System;
using System.IO;
using TS.Audio;

namespace TopSpeed.Audio
{
    internal sealed class AudioManager : IDisposable
    {
        private readonly AudioSystem _system;
        private readonly AudioOutput _output;
        private readonly bool _useHrtf;

        public AudioManager(bool useHrtf = false)
        {
            _useHrtf = useHrtf;
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
            return _output.CreateSource(path, streamFromDisk, useHrtf && _useHrtf);
        }

        public void Update()
        {
            _system.Update();
        }

        public void Dispose()
        {
            _output.Dispose();
            _system.Dispose();
        }
    }
}
