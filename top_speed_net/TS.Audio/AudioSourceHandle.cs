using System;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Threading;
using MiniAudioEx.Core.AdvancedAPI;
using MiniAudioEx.Native;

namespace TS.Audio
{
    internal sealed class AudioSourceSpatialParams
    {
        public float PosX;
        public float PosY;
        public float PosZ;
        public float VelX;
        public float VelY;
        public float VelZ;
        public float RefDistance = 1.0f;
        public float MaxDistance = 10000.0f;
        public float RollOff = 1.0f;
        public float Occlusion = 0f;
        public DistanceModel DistanceModel = DistanceModel.Inverse;
    }

    public sealed class AudioSourceHandle : IDisposable
    {
        private const float MaxDistanceInfinite = 1000000000f;
        private readonly AudioOutput _output;
        private readonly MaSound _sound;
        private MaEffectNode? _effectNode;
        private readonly AudioSourceSpatialParams _spatial;
        private SteamAudioSpatializer? _spatializer;
        private bool _useHrtf;
        private readonly bool _spatialize;
        private readonly bool _trueStereoHrtf;
        private float _basePitch = 1.0f;
        private bool _ownsSound;
        private IDisposable? _userData;
        private int _channels = 2;
        private int _sampleRate = 44100;
        private ma_sound_end_proc? _endCallback;
        private GCHandle _endHandle;
        private Action? _onEnd;

        public AudioSourceHandle(AudioOutput output, string filePath, bool streamFromDisk, bool useHrtf = true)
            : this(output, filePath, streamFromDisk, spatialize: useHrtf, useHrtf: useHrtf)
        {
        }

        public AudioSourceHandle(AudioOutput output, string filePath, bool streamFromDisk, bool spatialize, bool useHrtf)
        {
            _output = output;
            _sound = new MaSound();
            _spatial = new AudioSourceSpatialParams();
            _ownsSound = true;
            _trueStereoHrtf = output.TrueStereoHrtf;
            _spatialize = spatialize;

            ma_sound_flags flags = streamFromDisk ? ma_sound_flags.stream : 0;
            var init = _sound.InitializeFromFile(_output.Engine, filePath, flags, null!);
            if (init != ma_result.success)
                throw new InvalidOperationException("Failed to init sound: " + init);

            CacheFormat();
            _spatializer = _output.SteamAudio != null ? new SteamAudioSpatializer(_output.SteamAudio, _output.PeriodSizeInFrames, _trueStereoHrtf, _output.DownmixMode) : null;
            _useHrtf = _spatialize && useHrtf && _output.SteamAudio != null;

            if (_useHrtf)
            {
                _effectNode = new MaEffectNode();
                _effectNode.Initialize(_output.Engine, (uint)_output.SampleRate, (uint)_output.Channels);
                _effectNode.Process += OnHrtfProcess;

                _sound.SetSpatializationEnabled(false);
                _sound.DetachAllOutputBuses();
                _sound.AttachOutputBus(0, _effectNode.NodeHandle, 0);
                _effectNode.AttachOutputBus(0, _output.Engine.GetEndPoint(), 0);
            }
            else if (_spatialize)
            {
                _sound.SetSpatializationEnabled(true);
            }
            else
            {
                _sound.SetSpatializationEnabled(false);
            }
        }

        internal AudioSourceHandle(AudioOutput output, MaSound sound, bool ownsSound, bool useHrtf = true, IDisposable? userData = null)
            : this(output, sound, ownsSound, spatialize: useHrtf, useHrtf: useHrtf, userData: userData)
        {
        }

        internal AudioSourceHandle(AudioOutput output, MaSound sound, bool ownsSound, bool spatialize, bool useHrtf, IDisposable? userData = null)
        {
            _output = output;
            _sound = sound ?? throw new ArgumentNullException(nameof(sound));
            _spatial = new AudioSourceSpatialParams();
            _ownsSound = ownsSound;
            _userData = userData;
            _trueStereoHrtf = output.TrueStereoHrtf;
            _spatialize = spatialize;

            CacheFormat();
            _spatializer = _output.SteamAudio != null ? new SteamAudioSpatializer(_output.SteamAudio, _output.PeriodSizeInFrames, _trueStereoHrtf, _output.DownmixMode) : null;
            _useHrtf = _spatialize && useHrtf && _output.SteamAudio != null;

            if (_useHrtf)
            {
                _effectNode = new MaEffectNode();
                _effectNode.Initialize(_output.Engine, (uint)_output.SampleRate, (uint)_output.Channels);
                _effectNode.Process += OnHrtfProcess;

                _sound.SetSpatializationEnabled(false);
                _sound.DetachAllOutputBuses();
                _sound.AttachOutputBus(0, _effectNode.NodeHandle, 0);
                _effectNode.AttachOutputBus(0, _output.Engine.GetEndPoint(), 0);
            }
            else if (_spatialize)
            {
                _sound.SetSpatializationEnabled(true);
            }
            else
            {
                _sound.SetSpatializationEnabled(false);
            }
        }

        public void Play(bool loop)
        {
            _sound.SetLooping(loop);
            _sound.Start();
        }

        public void Stop()
        {
            _sound.Stop();
        }

        public bool IsPlaying => _sound.IsPlaying();

        public void SetVolume(float volume)
        {
            _sound.SetVolume(volume);
        }

        public float GetVolume()
        {
            return _sound.GetVolume();
        }

        public void SetPitch(float pitch)
        {
            _basePitch = pitch;
            _sound.SetPitch(pitch);
        }

        public float GetPitch()
        {
            return _sound.GetPitch();
        }

        public void SetPan(float pan)
        {
            if (_spatialize)
                return;

            _sound.SetPan(pan);
        }

        public void SetPosition(Vector3 position)
        {
            if (!_spatialize)
                return;

            Volatile.Write(ref _spatial.PosX, position.X);
            Volatile.Write(ref _spatial.PosY, position.Y);
            Volatile.Write(ref _spatial.PosZ, position.Z);

            if (!_useHrtf)
            {
                _sound.SetPosition(new ma_vec3f { x = position.X, y = position.Y, z = position.Z });
            }
        }

        public void SetVelocity(Vector3 velocity)
        {
            if (!_spatialize)
                return;

            Volatile.Write(ref _spatial.VelX, velocity.X);
            Volatile.Write(ref _spatial.VelY, velocity.Y);
            Volatile.Write(ref _spatial.VelZ, velocity.Z);

            if (!_useHrtf)
            {
                _sound.SetVelocity(new ma_vec3f { x = velocity.X, y = velocity.Y, z = velocity.Z });
            }
        }

        public void SetDistanceModel(DistanceModel model, float refDistance, float maxDistance, float rolloff)
        {
            if (!_spatialize)
                return;

            if (refDistance <= 0f)
                refDistance = 0.0001f;
            if (maxDistance <= 0f)
                maxDistance = MaxDistanceInfinite;
            if (maxDistance < refDistance)
                maxDistance = refDistance;

            Volatile.Write(ref _spatial.RefDistance, refDistance);
            Volatile.Write(ref _spatial.MaxDistance, maxDistance);
            Volatile.Write(ref _spatial.RollOff, rolloff);
            _spatial.DistanceModel = model;

            if (!_useHrtf)
            {
                MiniAudioNative.ma_sound_set_min_distance(_sound.Handle, refDistance);
                MiniAudioNative.ma_sound_set_max_distance(_sound.Handle, maxDistance);
                MiniAudioNative.ma_sound_set_rolloff(_sound.Handle, rolloff);
                _sound.SetAttenuationModel(ToMaAttenuationModel(model));
            }
        }

        public void ApplyCurveDistanceScaler(float curveDistanceScaler)
        {
            if (!_spatialize)
                return;

            if (curveDistanceScaler <= 0f)
                curveDistanceScaler = 0.0001f;

            SetDistanceModel(DistanceModel.Inverse, curveDistanceScaler, MaxDistanceInfinite, 1.0f);
        }

        public void SetDopplerFactor(float dopplerFactor)
        {
            if (!_spatialize)
                return;

            if (!_useHrtf)
            {
                _sound.SetDopplerFactor(dopplerFactor);
            }
        }

        public void SetLooping(bool loop)
        {
            _sound.SetLooping(loop);
        }

        public void SeekToStart()
        {
            _sound.SeekToPCMFrame(0);
        }

        public int InputChannels => _channels;
        public int InputSampleRate => _sampleRate;

        public float GetLengthSeconds()
        {
            if (_sound.Handle.pointer == IntPtr.Zero)
                return 0f;
            if (MiniAudioNative.ma_sound_get_length_in_seconds(_sound.Handle, out var seconds) != ma_result.success)
                seconds = 0f;
            if (seconds > 0f)
                return seconds;

            if (MiniAudioNative.ma_sound_get_length_in_pcm_frames(_sound.Handle, out var frames) != ma_result.success)
                return 0f;
            if (frames == 0 || _sampleRate <= 0)
                return 0f;
            return (float)(frames / (double)_sampleRate);
        }

        public void SetOnEnd(Action onEnd)
        {
            _onEnd = onEnd;
            if (_endCallback == null)
            {
                _endCallback = OnSoundEnd;
                _endHandle = GCHandle.Alloc(this);
            }
            _sound.SetEndCallback(_endCallback, GCHandle.ToIntPtr(_endHandle));
        }

        public void UpdateDoppler(Vector3 listenerPos, Vector3 listenerVel, AudioSystemConfig config)
        {
            if (!_useHrtf)
                return;

            var srcPos = new Vector3(
                Volatile.Read(ref _spatial.PosX),
                Volatile.Read(ref _spatial.PosY),
                Volatile.Read(ref _spatial.PosZ));

            var srcVel = new Vector3(
                Volatile.Read(ref _spatial.VelX),
                Volatile.Read(ref _spatial.VelY),
                Volatile.Read(ref _spatial.VelZ));

            var rel = srcPos - listenerPos;
            var distance = rel.Length();
            if (distance <= 0.0001f)
            {
                _sound.SetPitch(_basePitch);
                return;
            }

            var dir = rel / distance;
            float vL = Vector3.Dot(listenerVel, dir);
            float vS = Vector3.Dot(srcVel, dir);

            float c = config.SpeedOfSound;
            float doppler = (c + config.DopplerFactor * vL) / (c + config.DopplerFactor * vS);
            if (doppler < 0.5f) doppler = 0.5f;
            if (doppler > 2.0f) doppler = 2.0f;

            _sound.SetPitch(_basePitch * doppler);
        }

        public void Dispose()
        {
            _output.RemoveSource(this);
            _effectNode?.Dispose();
            _spatializer?.Dispose();
            if (_endHandle.IsAllocated)
                _endHandle.Free();
            if (_ownsSound)
                _sound.Dispose();
            _userData?.Dispose();
        }

        private void OnHrtfProcess(MaEffectNode sender, NativeArray<float> framesIn, UInt32 frameCountIn, NativeArray<float> framesOut, ref UInt32 frameCountOut, UInt32 channels)
        {
            if (_spatializer == null)
            {
                framesIn.CopyTo(framesOut);
                return;
            }

            _spatializer.Process(framesIn, frameCountIn, framesOut, ref frameCountOut, channels, _spatial);
        }

        private static ma_attenuation_model ToMaAttenuationModel(DistanceModel model)
        {
            switch (model)
            {
                case DistanceModel.Linear:
                    return ma_attenuation_model.linear;
                case DistanceModel.Exponential:
                    return ma_attenuation_model.exponential;
                case DistanceModel.Inverse:
                default:
                    return ma_attenuation_model.inverse;
            }
        }

        private void CacheFormat()
        {
            if (_sound.Handle.pointer == IntPtr.Zero)
                return;

            ma_format fmt;
            uint channels;
            uint sampleRate;
            var res = MiniAudioNative.ma_sound_get_data_format(_sound.Handle, out fmt, out channels, out sampleRate, 0, 0);
            if (res == ma_result.success)
            {
                _channels = (int)channels;
                _sampleRate = (int)sampleRate;
            }
        }

        private static void OnSoundEnd(IntPtr pUserData, ma_sound_ptr pSound)
        {
            var handle = GCHandle.FromIntPtr(pUserData);
            var source = handle.Target as AudioSourceHandle;
            if (source == null)
                return;

            var onEnd = source._onEnd;
            if (onEnd != null)
                ThreadPool.QueueUserWorkItem(_ => onEnd());
        }
    }
}
