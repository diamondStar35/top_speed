using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.InteropServices;
using MiniAudioEx.Core.AdvancedAPI;
using MiniAudioEx.Native;

namespace TS.Audio
{
    public sealed class AudioOutput : IDisposable
    {
        private readonly AudioOutputConfig _config;
        private readonly AudioSystemConfig _systemConfig;
        private readonly bool _trueStereoHrtf;
        private readonly HrtfDownmixMode _downmixMode;
        private readonly MaContext _context;
        private readonly MaDevice _device;
        private readonly MaResourceManager _resourceManager;
        private readonly MaEngine _engine;
        private readonly MaEngineListener _listener;
        private readonly ma_device_data_proc _deviceDataProc;
        private readonly List<AudioSourceHandle> _sources;
        private readonly List<OggStreamHandle> _streams;
        private readonly SteamAudioContext? _steamAudio;

        public string Name => _config.Name;
        public int SampleRate => (int)_config.SampleRate;
        public int Channels => (int)_config.Channels;
        public uint PeriodSizeInFrames => _config.PeriodSizeInFrames;
        public MaEngine Engine => _engine;
        public SteamAudioContext? SteamAudio => _steamAudio;
        public bool TrueStereoHrtf => _trueStereoHrtf;
        public HrtfDownmixMode DownmixMode => _downmixMode;
        public bool IsHrtfActive => _steamAudio != null;

        private Vector3 _listenerPosition;
        private Vector3 _listenerVelocity;

        public AudioOutput(AudioOutputConfig config, AudioSystemConfig systemConfig)
        {
            _config = config;
            _systemConfig = systemConfig;
            _sources = new List<AudioSourceHandle>();
            _streams = new List<OggStreamHandle>();
            _trueStereoHrtf = _systemConfig.HrtfMode == HrtfMode.Stereo;
            _downmixMode = _systemConfig.HrtfDownmixMode;

            _context = new MaContext();
            _context.Initialize();

            var autoChannels = _config.Channels == 0 || _systemConfig.Channels == 0;
            var autoSampleRate = _config.SampleRate == 0 || _systemConfig.SampleRate == 0;
            if (_systemConfig.UseHrtf)
            {
                _config.Channels = 2;
            }
            else if (autoChannels)
            {
                var resolved = ResolveAutoChannels();
                _config.Channels = resolved > 0 ? resolved : 2u;
            }

            _resourceManager = new MaResourceManager();
            var resourceConfig = _resourceManager.GetConfig();
            try
            {
                var vorbis = MiniAudioNative.ma_libvorbis_get_decoding_backend_ptr();
                if (vorbis.pointer != IntPtr.Zero)
                    resourceConfig.SetCustomDecodingBackendVTables(new[] { vorbis });
            }
            catch (EntryPointNotFoundException)
            {
                // Ignore if the native build does not expose the Vorbis backend.
            }

            var resourceInit = _resourceManager.Initialize(resourceConfig);
            resourceConfig.FreeCustomDecodingBackendVTables();
            if (resourceInit != ma_result.success)
            {
                throw new InvalidOperationException("Failed to initialize audio resource manager: " + resourceInit);
            }

            _device = new MaDevice();
            var deviceConfig = _device.GetConfig(ma_device_type.playback);
            deviceConfig.sampleRate = autoSampleRate ? 0u : _config.SampleRate;
            deviceConfig.periodSizeInFrames = _config.PeriodSizeInFrames;
            deviceConfig.playback.format = ma_format.f32;
            deviceConfig.playback.channels = _config.Channels;

            if (_config.DeviceIndex.HasValue)
            {
                if (_context.GetDevices(out var playbackDevices, out _))
                {
                    int idx = _config.DeviceIndex.Value;
                    if (playbackDevices != null && idx >= 0 && idx < playbackDevices.Length)
                    {
                        deviceConfig.playback.pDeviceID = playbackDevices[idx].pDeviceId;
                    }
                }
            }

            _deviceDataProc = OnDeviceData;
            deviceConfig.SetDataCallback(_deviceDataProc);

            var deviceInit = _device.Initialize(_context, deviceConfig);
            if (deviceInit != ma_result.success)
            {
                throw new InvalidOperationException("Failed to initialize audio device: " + deviceInit);
            }

            if (autoSampleRate)
            {
                var resolved = GetDeviceSampleRate();
                if (resolved == 0)
                    resolved = ResolveAutoSampleRate();
                if (resolved == 0)
                    resolved = 44100;
                _config.SampleRate = resolved;
            }

            _engine = new MaEngine();
            var engineConfig = _engine.GetConfig();
            engineConfig.pDevice = _device.Handle;
            engineConfig.pResourceManager = _resourceManager.Handle;
            var engineInit = _engine.Initialize(engineConfig);
            if (engineInit != ma_result.success)
            {
                throw new InvalidOperationException("Failed to initialize audio engine: " + engineInit);
            }

            unsafe
            {
                _device.Handle.Get()->pUserData = _engine.Handle.pointer;
            }

            _listener = new MaEngineListener();
            _listener.Initialize(_engine, 0);

            _steamAudio = _systemConfig.UseHrtf
                ? new SteamAudioContext((int)_config.SampleRate, (int)_config.PeriodSizeInFrames, _systemConfig.HrtfSofaPath)
                : null;

            _device.Start();
        }

        public AudioSourceHandle CreateSource(string filePath, bool streamFromDisk = true, bool useHrtf = true)
        {
            return CreateSource(filePath, streamFromDisk, spatialize: useHrtf, useHrtf: useHrtf);
        }

        public AudioSourceHandle CreateSpatialSource(string filePath, bool streamFromDisk = true, bool allowHrtf = true)
        {
            return CreateSource(filePath, streamFromDisk, spatialize: true, useHrtf: allowHrtf);
        }

        internal AudioSourceHandle CreateSource(string filePath, bool streamFromDisk, bool spatialize, bool useHrtf)
        {
            var source = new AudioSourceHandle(this, filePath, streamFromDisk, spatialize, useHrtf);
            if (_systemConfig.UseCurveDistanceScaler)
                source.ApplyCurveDistanceScaler(_systemConfig.CurveDistanceScaler);
            else
                source.SetDistanceModel(_systemConfig.DistanceModel, _systemConfig.MinDistance, _systemConfig.MaxDistance, _systemConfig.RollOff);
            source.SetDopplerFactor(_systemConfig.DopplerFactor);
            _sources.Add(source);
            return source;
        }

        public AudioSourceHandle CreateProceduralSource(ProceduralAudioCallback callback, uint channels = 1, uint sampleRate = 44100, bool useHrtf = true)
        {
            if (callback == null)
                throw new ArgumentNullException(nameof(callback));

            var generator = new ProceduralAudioGenerator(callback);
            var sound = new MaSound();
            var result = sound.InitializeFromCallback(_engine, channels, sampleRate, generator.Proc, generator.UserData, null!);
            if (result != ma_result.success)
            {
                generator.Dispose();
                sound.Dispose();
                throw new InvalidOperationException("Failed to init procedural sound: " + result);
            }

            var source = new AudioSourceHandle(this, sound, true, useHrtf, generator);
            if (_systemConfig.UseCurveDistanceScaler)
                source.ApplyCurveDistanceScaler(_systemConfig.CurveDistanceScaler);
            else
                source.SetDistanceModel(_systemConfig.DistanceModel, _systemConfig.MinDistance, _systemConfig.MaxDistance, _systemConfig.RollOff);
            source.SetDopplerFactor(_systemConfig.DopplerFactor);
            _sources.Add(source);
            return source;
        }

        public OggStreamHandle CreateOggStream(params string[] filePaths)
        {
            var stream = new OggStreamHandle(this, filePaths);
            _streams.Add(stream);
            return stream;
        }

        public void RemoveSource(AudioSourceHandle source)
        {
            _sources.Remove(source);
        }

        internal void RemoveStream(OggStreamHandle stream)
        {
            _streams.Remove(stream);
        }

        public void UpdateListener(Vector3 position, Vector3 forward, Vector3 up, Vector3 velocity)
        {
            _listenerPosition = position;
            _listenerVelocity = velocity;

            var pos = ToMaVec3(position);
            var dir = ToMaVec3(forward);
            var vel = ToMaVec3(velocity);
            var upVec = new ma_vec3f { x = up.X, y = up.Y, z = up.Z };

            _listener.SetPosition(pos);
            _listener.SetDirection(dir);
            _listener.SetVelocity(vel);
            _listener.SetWorldUp(upVec);

            if (_steamAudio != null)
            {
                _steamAudio.UpdateListener(position, forward, up);
            }
        }

        private static ma_vec3f ToMaVec3(Vector3 value)
        {
            // MiniAudio defaults to right-handed with forward = -Z.
            return new ma_vec3f { x = value.X, y = value.Y, z = -value.Z };
        }

        public void Update(double deltaTime)
        {
            for (int i = _streams.Count - 1; i >= 0; i--)
            {
                _streams[i].Update();
            }

            if (!_systemConfig.UseHrtf)
                return;

            for (int i = 0; i < _sources.Count; i++)
            {
                _sources[i].UpdateDoppler(_listenerPosition, _listenerVelocity, _systemConfig);
            }
        }

        public void Dispose()
        {
            for (int i = _sources.Count - 1; i >= 0; i--)
            {
                _sources[i].Dispose();
            }
            _sources.Clear();

            for (int i = _streams.Count - 1; i >= 0; i--)
            {
                _streams[i].Dispose();
            }
            _streams.Clear();

            _steamAudio?.Dispose();
            _listener.Dispose();
            _engine.Dispose();
            _device.Dispose();
            _resourceManager.Dispose();
            _context.Dispose();
        }

        private uint ResolveAutoChannels()
        {
            if (!TryGetPlaybackDevice(out var device))
                return 0;
            return PickBestChannelCount(device.deviceInfo);
        }

        private uint ResolveAutoSampleRate()
        {
            if (!TryGetPlaybackDevice(out var device))
                return 0;
            return PickBestSampleRate(device.deviceInfo);
        }

        private bool TryGetPlaybackDevice(out MaDeviceInfo device)
        {
            device = default;
            if (!_context.GetDevices(out var playbackDevices, out _))
                return false;
            if (playbackDevices == null || playbackDevices.Length == 0)
                return false;

            if (_config.DeviceIndex.HasValue)
            {
                var idx = _config.DeviceIndex.Value;
                if (idx >= 0 && idx < playbackDevices.Length)
                {
                    device = playbackDevices[idx];
                    return true;
                }
            }

            for (int i = 0; i < playbackDevices.Length; i++)
            {
                if (playbackDevices[i].deviceInfo.isDefault > 0)
                {
                    device = playbackDevices[i];
                    return true;
                }
            }

            device = playbackDevices[0];
            return true;
        }

        private static uint PickBestChannelCount(ma_device_info info)
        {
            var count = (int)info.nativeDataFormatCount;
            if (count <= 0)
                return 0;

            uint best = 0;
            unsafe
            {
                var formats = info.nativeDataFormats;
                var max = Math.Min(count, 64);
                for (int i = 0; i < max; i++)
                {
                    var channels = formats[i].channels;
                    if (channels == 0)
                        continue;
                    if (channels > best)
                        best = channels;
                }
            }

            return best;
        }

        private static uint PickBestSampleRate(ma_device_info info)
        {
            var count = (int)info.nativeDataFormatCount;
            if (count <= 0)
                return 0;

            uint best = 0;
            bool has44100 = false;
            bool has48000 = false;
            unsafe
            {
                var formats = info.nativeDataFormats;
                var max = Math.Min(count, 64);
                for (int i = 0; i < max; i++)
                {
                    var sampleRate = formats[i].sampleRate;
                    if (sampleRate == 0)
                        continue;
                    if (sampleRate == 44100)
                        has44100 = true;
                    else if (sampleRate == 48000)
                        has48000 = true;
                    if (sampleRate > best)
                        best = sampleRate;
                }
            }

            if (has48000)
                return 48000;
            if (has44100)
                return 44100;
            return best;
        }

        private uint GetDeviceSampleRate()
        {
            unsafe
            {
                ma_device* device = _device.Handle.Get();
                if (device == null)
                    return 0;
                return device->sampleRate;
            }
        }

        private static void OnDeviceData(ma_device_ptr pDevice, IntPtr pOutput, IntPtr pInput, uint frameCount)
        {
            unsafe
            {
                ma_device* device = pDevice.Get();
                if (device == null)
                    return;

                if (device->pUserData == IntPtr.Zero)
                    return;

                var enginePtr = new ma_engine_ptr(device->pUserData);
                MiniAudioNative.ma_engine_read_pcm_frames(enginePtr, pOutput, frameCount);
            }
        }
    }
}
