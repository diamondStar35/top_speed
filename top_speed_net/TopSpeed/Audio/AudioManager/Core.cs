using System;
using System.IO;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Threading;
using TopSpeed.Input;
using TS.Audio;

namespace TopSpeed.Audio
{
    internal sealed partial class AudioManager : IGameAudio, IAudioSources
    {
        private readonly AudioEngine _engine;
        private readonly ManualResetEventSlim _updateWake = new ManualResetEventSlim(false);
        private Thread? _updateThread;
        private volatile bool _updateRunning;
        private bool _disposed;

        public bool IsHrtfActive => _engine.System.IsHrtfActive;
        public int OutputChannels => _engine.PrimaryOutput.Channels;
        public int OutputSampleRate => _engine.PrimaryOutput.SampleRate;

        public AudioManager(
            bool useHrtf = false,
            bool autoDetectDeviceFormat = true)
        {
            var config = new AudioSystemConfig
            {
                UseHrtf = useHrtf
            };

            var outputConfig = new AudioOutputConfig
            {
                Name = "main"
            };
            var speechOutputConfig = new AudioOutputConfig
            {
                Name = "speech"
            };

            if (autoDetectDeviceFormat)
            {
                config.Channels = 0;
                config.SampleRate = 0;
                outputConfig.Channels = 0;
                outputConfig.SampleRate = 0;
                speechOutputConfig.Channels = 0;
                speechOutputConfig.SampleRate = 0;
            }
            else
            {
                outputConfig.Channels = config.Channels;
                outputConfig.SampleRate = config.SampleRate;
                speechOutputConfig.Channels = config.Channels;
                speechOutputConfig.SampleRate = config.SampleRate;
            }

            ApplyAndroidOutputPolicy(outputConfig);
            ApplyAndroidOutputPolicy(speechOutputConfig);

            var engineOptions = new AudioEngineOptions
            {
                SystemConfig = config,
                PrimaryOutput = outputConfig,
                SpeechOutput = speechOutputConfig,
                UseDedicatedSpeechOutput = !IsAndroid()
            };

            _engine = new AudioEngine(engineOptions);
        }

        private static void ApplyAndroidOutputPolicy(AudioOutputConfig config)
        {
            if (!IsAndroid())
                return;

            config.PeriodSizeInFrames = 1024;
        }

        private static bool IsAndroid()
        {
#if NET5_0_OR_GREATER
            if (OperatingSystem.IsAndroid())
                return true;
#endif
            return RuntimeInformation.IsOSPlatform(OSPlatform.Create("ANDROID"));
        }

        public SoundAsset LoadAsset(string path, bool streamFromDisk = true)
        {
            if (string.IsNullOrWhiteSpace(path))
                throw new ArgumentException("Audio path is required.", nameof(path));

            var fullPath = Path.GetFullPath(path);
            if (!File.Exists(fullPath))
                throw new FileNotFoundException("Audio file not found.", fullPath);

            return _engine.LoadClip(fullPath, streamFromDisk, cache: true);
        }

        public StreamAsset LoadStream(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                throw new ArgumentException("Audio path is required.", nameof(path));

            var fullPath = Path.GetFullPath(path);
            if (!File.Exists(fullPath))
                throw new FileNotFoundException("Audio file not found.", fullPath);

            return _engine.LoadStream(fullPath, cache: true);
        }

        public Source CreateSource(SoundAsset asset, string busName, bool? useHrtf = null)
        {
            if (asset == null)
                throw new ArgumentNullException(nameof(asset));

            return _engine.CreateSource(asset, busName, spatialize: false, useHrtf: useHrtf);
        }

        public Source CreateLoopingSource(SoundAsset asset, string busName, bool? useHrtf = null)
        {
            return CreateSource(asset, busName, useHrtf);
        }

        public Source CreateSpatialSource(SoundAsset asset, string busName, bool? allowHrtf = null)
        {
            if (asset == null)
                throw new ArgumentNullException(nameof(asset));

            return _engine.CreateSpatialSource(asset, busName, allowHrtf);
        }

        public Source CreateLoopingSpatialSource(SoundAsset asset, string busName, bool? allowHrtf = null)
        {
            return CreateSpatialSource(asset, busName, allowHrtf);
        }

        public Source CreateProceduralSource(
            ProceduralAudioCallback callback,
            uint channels = 1,
            uint sampleRate = 44100,
            string? busName = null,
            bool? spatialize = null,
            bool? useHrtf = null)
        {
            if (callback == null)
                throw new ArgumentNullException(nameof(callback));

            return _engine.CreateProceduralSource(callback, channels, sampleRate, busName, spatialize, useHrtf);
        }

        public void PlayOneShot(SoundAsset asset, string busName, Action<Source>? configure = null, bool? useHrtf = null)
        {
            _engine.PlayOneShot(asset, busName, configure, spatialize: false, useHrtf: useHrtf);
        }

        public void PlayOneShotSpatial(SoundAsset asset, string busName, Action<Source>? configure = null, bool? allowHrtf = null)
        {
            _engine.PlayOneShotSpatial(asset, busName, configure, allowHrtf);
        }

        public void Update()
        {
            _engine.Update();
        }

        public void SetMasterVolume(float volume)
        {
            _engine.PrimaryOutput.SetMasterVolume(volume);
        }

        public void UpdateListener(Vector3 position, Vector3 forward, Vector3 up, Vector3 velocity)
        {
            _engine.SetListener(position, forward, up, velocity);
        }

        public void SetRoomAcoustics(RoomAcoustics acoustics)
        {
            _engine.PrimaryOutput.SetRoomAcoustics(acoustics);
        }
    }
}

