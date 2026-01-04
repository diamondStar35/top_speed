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

            if (_systemConfig.UseHrtf && _config.Channels < 2)
            {
                _config.Channels = 2;
            }

            _context = new MaContext();
            _context.Initialize();

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
            deviceConfig.sampleRate = _config.SampleRate;
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

            var pos = new ma_vec3f { x = position.X, y = position.Y, z = position.Z };
            var dir = new ma_vec3f { x = forward.X, y = forward.Y, z = forward.Z };
            var vel = new ma_vec3f { x = velocity.X, y = velocity.Y, z = velocity.Z };
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
