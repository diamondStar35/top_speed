using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Threading;
using TS.Audio;

namespace TopSpeed.Audio
{
    internal sealed class AudioManager : IDisposable
    {
        private readonly struct AudioCacheKey
        {
            public readonly string Path;
            public readonly bool StreamFromDisk;
            public readonly bool UseHrtf;

            public AudioCacheKey(string path, bool streamFromDisk, bool useHrtf)
            {
                Path = path;
                StreamFromDisk = streamFromDisk;
                UseHrtf = useHrtf;
            }
        }

        private sealed class AudioCacheKeyComparer : IEqualityComparer<AudioCacheKey>
        {
            public bool Equals(AudioCacheKey x, AudioCacheKey y)
            {
                return x.StreamFromDisk == y.StreamFromDisk
                    && x.UseHrtf == y.UseHrtf
                    && string.Equals(x.Path, y.Path, StringComparison.OrdinalIgnoreCase);
            }

            public int GetHashCode(AudioCacheKey obj)
            {
                var hash = StringComparer.OrdinalIgnoreCase.GetHashCode(obj.Path);
                hash = (hash * 397) ^ obj.StreamFromDisk.GetHashCode();
                hash = (hash * 397) ^ obj.UseHrtf.GetHashCode();
                return hash;
            }
        }

        private sealed class CachedSource
        {
            public AudioSourceHandle Handle { get; }
            public int RefCount { get; set; }

            public CachedSource(AudioSourceHandle handle)
            {
                Handle = handle;
                RefCount = 1;
            }
        }

        private readonly AudioSystem _system;
        private readonly AudioOutput _output;
        private readonly object _cacheLock = new object();
        private readonly Dictionary<AudioCacheKey, CachedSource> _sourceCache =
            new Dictionary<AudioCacheKey, CachedSource>(new AudioCacheKeyComparer());
        private readonly Dictionary<AudioSourceHandle, AudioCacheKey> _handleCache =
            new Dictionary<AudioSourceHandle, AudioCacheKey>();
        private readonly object _pathCacheLock = new object();
        private readonly Dictionary<string, bool> _pathExistsCache =
            new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
        private Thread? _updateThread;
        private volatile bool _updateRunning;
        public bool IsHrtfActive => _system.IsHrtfActive;
        public int OutputChannels => _output.Channels;
        public int OutputSampleRate => _output.SampleRate;
        public AudioManager(bool useHrtf = false, bool autoDetectDeviceFormat = true)
        {
            var config = new AudioSystemConfig
            {
                UseHrtf = useHrtf
            };
            if (autoDetectDeviceFormat)
            {
                config.Channels = 0;
                config.SampleRate = 0;
            }
            _system = new AudioSystem(config);
            _output = _system.CreateOutput(new AudioOutputConfig { Name = "main", Channels = 0, SampleRate = 0 });
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

        public bool TryResolvePath(string path, out string fullPath)
        {
            fullPath = string.Empty;
            if (string.IsNullOrWhiteSpace(path))
                return false;

            fullPath = Path.GetFullPath(path);
            lock (_pathCacheLock)
            {
                if (_pathExistsCache.TryGetValue(fullPath, out var exists))
                    return exists;
                exists = File.Exists(fullPath);
                _pathExistsCache[fullPath] = exists;
                return exists;
            }
        }

        public AudioSourceHandle AcquireCachedSource(string path, bool streamFromDisk = true, bool useHrtf = false)
        {
            if (string.IsNullOrWhiteSpace(path))
                throw new ArgumentException("Audio path is required.", nameof(path));

            var fullPath = Path.GetFullPath(path);
            var key = new AudioCacheKey(fullPath, streamFromDisk, useHrtf);
            lock (_cacheLock)
            {
                if (_sourceCache.TryGetValue(key, out var cached))
                {
                    cached.RefCount++;
                    return cached.Handle;
                }

                var handle = CreateSource(fullPath, streamFromDisk, useHrtf);
                _sourceCache[key] = new CachedSource(handle);
                _handleCache[handle] = key;
                return handle;
            }
        }

        public void ReleaseCachedSource(AudioSourceHandle? handle)
        {
            if (handle == null)
                return;
            lock (_cacheLock)
            {
                if (!_handleCache.TryGetValue(handle, out var key))
                    return;

                if (!_sourceCache.TryGetValue(key, out var cached))
                {
                    _handleCache.Remove(handle);
                    return;
                }

                cached.RefCount--;
                if (cached.RefCount > 0)
                    return;

                _sourceCache.Remove(key);
                _handleCache.Remove(handle);
                cached.Handle.Dispose();
            }
        }

        public void Update()
        {
            _system.Update();
        }

        public void StartUpdateThread(int intervalMs = 8)
        {
            if (_updateRunning)
                return;
            _updateRunning = true;
            _updateThread = new Thread(() => UpdateLoop(intervalMs))
            {
                IsBackground = true,
                Name = "AudioUpdate"
            };
            _updateThread.Start();
        }

        public void StopUpdateThread()
        {
            _updateRunning = false;
            if (_updateThread == null)
                return;
            if (_updateThread.IsAlive)
                _updateThread.Join(200);
            _updateThread = null;
        }

        private void UpdateLoop(int intervalMs)
        {
            while (_updateRunning)
            {
                _system.Update();
                Thread.Sleep(intervalMs);
            }
        }

        public void UpdateListener(Vector3 position, Vector3 forward, Vector3 up, Vector3 velocity)
        {
            _system.UpdateListenerAll(position, forward, up, velocity);
        }

        public void Dispose()
        {
            StopUpdateThread();
            ClearCachedSources();
            _output.Dispose();
            _system.Dispose();
        }

        private void ClearCachedSources()
        {
            lock (_cacheLock)
            {
                foreach (var cached in _sourceCache.Values)
                    cached.Handle.Dispose();
                _sourceCache.Clear();
                _handleCache.Clear();
            }

            lock (_pathCacheLock)
            {
                _pathExistsCache.Clear();
            }
        }
    }
}
