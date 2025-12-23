using System;
using System.Collections.Generic;
using System.Numerics;

namespace TS.Audio
{
    public sealed class AudioSystem : IDisposable
    {
        private readonly AudioSystemConfig _config;
        private readonly Dictionary<string, AudioOutput> _outputs;
        private DateTime _lastUpdate;

        public IReadOnlyDictionary<string, AudioOutput> Outputs => _outputs;
        public bool IsInitialized => _outputs.Count > 0;
        public bool IsHrtfActive
        {
            get
            {
                foreach (var output in _outputs.Values)
                {
                    if (output.IsHrtfActive)
                        return true;
                }
                return false;
            }
        }

        public AudioSystem(AudioSystemConfig config)
        {
            _config = config ?? new AudioSystemConfig();
            _outputs = new Dictionary<string, AudioOutput>(StringComparer.OrdinalIgnoreCase);
            _lastUpdate = DateTime.Now;
        }

        public AudioOutput CreateOutput(AudioOutputConfig outputConfig)
        {
            if (outputConfig == null)
                throw new ArgumentNullException(nameof(outputConfig));

            if (_outputs.ContainsKey(outputConfig.Name))
                throw new InvalidOperationException("Output already exists: " + outputConfig.Name);

            if (outputConfig.SampleRate == 0)
                outputConfig.SampleRate = _config.SampleRate;
            if (outputConfig.Channels == 0)
                outputConfig.Channels = _config.Channels;
            if (outputConfig.PeriodSizeInFrames == 0)
                outputConfig.PeriodSizeInFrames = _config.PeriodSizeInFrames;

            var output = new AudioOutput(outputConfig, _config);
            _outputs.Add(outputConfig.Name, output);
            return output;
        }

        public AudioOutput GetOutput(string name)
        {
            if (!_outputs.TryGetValue(name, out var output))
                throw new KeyNotFoundException("Output not found: " + name);
            return output;
        }

        public void Update()
        {
            var now = DateTime.Now;
            var delta = now - _lastUpdate;
            double dt = delta.TotalSeconds;

            foreach (var output in _outputs.Values)
            {
                output.Update(dt);
            }

            _lastUpdate = now;
        }

        public void UpdateListenerAll(Vector3 position, Vector3 forward, Vector3 up, Vector3 velocity)
        {
            foreach (var output in _outputs.Values)
            {
                output.UpdateListener(position, forward, up, velocity);
            }
        }

        public void UpdateListenerFromTdv(float x, float y, float z, float vx, float vy, float vz, bool upright)
        {
            var pos = CoordinateMapper.ToAudioPosition(x, y, z);
            var vel = CoordinateMapper.ToAudioVelocity(vx, vy, vz, _config.UseVerticalVelocity);
            var forward = CoordinateMapper.ToAudioForward(vx, vy, vz);
            var up = new Vector3(0f, upright ? 1f : -1f, 0f);

            UpdateListenerAll(pos, forward, up, vel);
        }

        public void Dispose()
        {
            foreach (var output in _outputs.Values)
            {
                output.Dispose();
            }
            _outputs.Clear();
        }
    }
}
