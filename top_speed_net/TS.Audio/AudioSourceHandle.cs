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
        public float AirAbsLow = 1.0f;
        public float AirAbsMid = 1.0f;
        public float AirAbsHigh = 1.0f;
        public float TransLow = 0.0f;
        public float TransMid = 0.0f;
        public float TransHigh = 0.0f;
        public bool StereoWidening = false;
        public int SimulationFlags = 0;
        public float ReverbTimeLow = 0.0f;
        public float ReverbTimeMid = 0.0f;
        public float ReverbTimeHigh = 0.0f;
        public float ReverbEqLow = 1.0f;
        public float ReverbEqMid = 1.0f;
        public float ReverbEqHigh = 1.0f;
        public int ReverbDelay = 0;
        public int RoomFlags = 0;
        public float RoomReverbTimeSeconds = 0f;
        public float RoomReverbGain = 0f;
        public float RoomHfDecayRatio = 1f;
        public float RoomLateReverbGain = 0f;
        public float RoomDiffusion = 0f;
        public float RoomAirAbsorptionScale = 1f;
        public float RoomOcclusionScale = 1f;
        public float RoomTransmissionScale = 1f;
        public float RoomOcclusionOverride = float.NaN;
        public float RoomTransmissionOverrideLow = float.NaN;
        public float RoomTransmissionOverrideMid = float.NaN;
        public float RoomTransmissionOverrideHigh = float.NaN;
        public float RoomAirAbsorptionOverrideLow = float.NaN;
        public float RoomAirAbsorptionOverrideMid = float.NaN;
        public float RoomAirAbsorptionOverrideHigh = float.NaN;
        public DistanceModel DistanceModel = DistanceModel.Inverse;

        public const int SimOcclusion = 1 << 0;
        public const int SimTransmission = 1 << 1;
        public const int SimAirAbsorption = 1 << 2;
        public const int SimReflections = 1 << 3;
        public const int RoomHasProfile = 1 << 8;
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
        private float _dopplerFactor = 1.0f;
        private bool _ownsSound;
        private IDisposable? _userData;
        private int _channels = 2;
        private int _sampleRate = 44100;
        private ma_sound_end_proc? _endCallback;
        private GCHandle _endHandle;
        private Action? _onEnd;
        private float _userVolume = 1.0f;
        private float _currentVolume = 1.0f;
        private float _fadeDuration;
        private float _fadeRemaining;
        private float _fadeStartVolume;
        private float _fadeTargetVolume;
        private bool _stopAfterFade;

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
            InitializeVolumeState();
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
            InitializeVolumeState();
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
            Play(loop, 0f);
        }

        public void Play(bool loop, float fadeInSeconds)
        {
            _sound.SetLooping(loop);
            if (fadeInSeconds <= 0f)
            {
                CancelFade();
                _currentVolume = _userVolume;
                _sound.SetVolume(_currentVolume);
                _sound.Start();
                return;
            }

            if (!_sound.IsPlaying())
            {
                _currentVolume = 0f;
                _sound.SetVolume(0f);
                _sound.Start();
            }

            BeginFade(_userVolume, fadeInSeconds, stopAfter: false);
        }

        public void Stop()
        {
            Stop(0f);
        }

        public void Stop(float fadeOutSeconds)
        {
            if (fadeOutSeconds <= 0f || !_sound.IsPlaying())
            {
                CancelFade();
                _sound.Stop();
                return;
            }

            BeginFade(0f, fadeOutSeconds, stopAfter: true);
        }

        public void FadeIn(float seconds)
        {
            if (seconds <= 0f)
            {
                CancelFade();
                _currentVolume = _userVolume;
                _sound.SetVolume(_currentVolume);
                if (!_sound.IsPlaying())
                    _sound.Start();
                return;
            }

            if (!_sound.IsPlaying())
            {
                _currentVolume = 0f;
                _sound.SetVolume(0f);
                _sound.Start();
            }

            BeginFade(_userVolume, seconds, stopAfter: false);
        }

        public void FadeOut(float seconds)
        {
            if (seconds <= 0f)
            {
                Stop();
                return;
            }

            BeginFade(0f, seconds, stopAfter: true);
        }

        public bool IsPlaying => _sound.IsPlaying();

        public void SetVolume(float volume)
        {
            _userVolume = volume;
            if (_fadeRemaining > 0f && !_stopAfterFade)
            {
                _fadeTargetVolume = _userVolume;
                return;
            }

            _currentVolume = _userVolume;
            _sound.SetVolume(_currentVolume);
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

        public void SetStereoWidening(bool enabled)
        {
            _spatial.StereoWidening = enabled;
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
                _sound.SetPosition(ToMaVec3(position));
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
                _sound.SetVelocity(ToMaVec3(velocity));
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

        internal void ApplyDirectSimulation(float occlusion, float airLow, float airMid, float airHigh, float transLow, float transMid, float transHigh)
        {
            Volatile.Write(ref _spatial.Occlusion, occlusion);
            Volatile.Write(ref _spatial.AirAbsLow, airLow);
            Volatile.Write(ref _spatial.AirAbsMid, airMid);
            Volatile.Write(ref _spatial.AirAbsHigh, airHigh);
            Volatile.Write(ref _spatial.TransLow, transLow);
            Volatile.Write(ref _spatial.TransMid, transMid);
            Volatile.Write(ref _spatial.TransHigh, transHigh);
            Volatile.Write(ref _spatial.SimulationFlags,
                AudioSourceSpatialParams.SimOcclusion |
                AudioSourceSpatialParams.SimTransmission |
                AudioSourceSpatialParams.SimAirAbsorption);
        }

        internal void ApplyReverbSimulation(float timeLow, float timeMid, float timeHigh, float eqLow, float eqMid, float eqHigh, int delay)
        {
            Volatile.Write(ref _spatial.ReverbTimeLow, timeLow);
            Volatile.Write(ref _spatial.ReverbTimeMid, timeMid);
            Volatile.Write(ref _spatial.ReverbTimeHigh, timeHigh);
            Volatile.Write(ref _spatial.ReverbEqLow, eqLow);
            Volatile.Write(ref _spatial.ReverbEqMid, eqMid);
            Volatile.Write(ref _spatial.ReverbEqHigh, eqHigh);
            Volatile.Write(ref _spatial.ReverbDelay, delay);
            Volatile.Write(ref _spatial.SimulationFlags, _spatial.SimulationFlags | AudioSourceSpatialParams.SimReflections);
        }

        public void SetRoomAcoustics(RoomAcoustics acoustics)
        {
            Volatile.Write(ref _spatial.RoomFlags, acoustics.HasRoom ? AudioSourceSpatialParams.RoomHasProfile : 0);
            Volatile.Write(ref _spatial.RoomReverbTimeSeconds, acoustics.ReverbTimeSeconds);
            Volatile.Write(ref _spatial.RoomReverbGain, acoustics.ReverbGain);
            Volatile.Write(ref _spatial.RoomHfDecayRatio, acoustics.HfDecayRatio);
            Volatile.Write(ref _spatial.RoomLateReverbGain, acoustics.LateReverbGain);
            Volatile.Write(ref _spatial.RoomDiffusion, acoustics.Diffusion);
            Volatile.Write(ref _spatial.RoomAirAbsorptionScale, acoustics.AirAbsorptionScale);
            Volatile.Write(ref _spatial.RoomOcclusionScale, acoustics.OcclusionScale);
            Volatile.Write(ref _spatial.RoomTransmissionScale, acoustics.TransmissionScale);

            Volatile.Write(ref _spatial.RoomOcclusionOverride, acoustics.OcclusionOverride ?? float.NaN);
            Volatile.Write(ref _spatial.RoomTransmissionOverrideLow, acoustics.TransmissionOverrideLow ?? float.NaN);
            Volatile.Write(ref _spatial.RoomTransmissionOverrideMid, acoustics.TransmissionOverrideMid ?? float.NaN);
            Volatile.Write(ref _spatial.RoomTransmissionOverrideHigh, acoustics.TransmissionOverrideHigh ?? float.NaN);
            Volatile.Write(ref _spatial.RoomAirAbsorptionOverrideLow, acoustics.AirAbsorptionOverrideLow ?? float.NaN);
            Volatile.Write(ref _spatial.RoomAirAbsorptionOverrideMid, acoustics.AirAbsorptionOverrideMid ?? float.NaN);
            Volatile.Write(ref _spatial.RoomAirAbsorptionOverrideHigh, acoustics.AirAbsorptionOverrideHigh ?? float.NaN);
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

            _dopplerFactor = Math.Max(0f, dopplerFactor);
            if (!_useHrtf)
            {
                _sound.SetDopplerFactor(_dopplerFactor);
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

        private static ma_vec3f ToMaVec3(Vector3 value)
        {
            // MiniAudio defaults to right-handed with forward = -Z.
            return new ma_vec3f { x = value.X, y = value.Y, z = -value.Z };
        }


        public int InputChannels => _channels;
        public int InputSampleRate => _sampleRate;
        internal bool UsesSteamAudio => _useHrtf;
        internal bool IsSpatialized => _spatialize;
        internal AudioSourceSpatialParams SpatialParams => _spatial;

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
            var dopplerFactor = config.DopplerFactor * _dopplerFactor;
            if (dopplerFactor <= 0f)
            {
                _sound.SetPitch(_basePitch);
                return;
            }

            float doppler = (c + dopplerFactor * vL) / (c + dopplerFactor * vS);
            if (doppler < 0.5f) doppler = 0.5f;
            if (doppler > 2.0f) doppler = 2.0f;

            _sound.SetPitch(_basePitch * doppler);
        }

        internal void UpdateFade(double deltaTime)
        {
            if (_fadeRemaining <= 0f)
                return;

            var step = (float)deltaTime;
            if (step <= 0f)
                return;

            _fadeRemaining -= step;
            var t = _fadeDuration <= 0f ? 1f : 1f - (_fadeRemaining / _fadeDuration);
            if (t < 0f) t = 0f;
            if (t > 1f) t = 1f;

            _currentVolume = _fadeStartVolume + (_fadeTargetVolume - _fadeStartVolume) * t;
            _sound.SetVolume(_currentVolume);

            if (t >= 1f)
            {
                _fadeRemaining = 0f;
                _currentVolume = _fadeTargetVolume;
                if (_stopAfterFade && _currentVolume <= 0.0001f)
                    _sound.Stop();
                _stopAfterFade = false;
            }
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

        private void InitializeVolumeState()
        {
            _userVolume = _sound.GetVolume();
            _currentVolume = _userVolume;
            _fadeDuration = 0f;
            _fadeRemaining = 0f;
            _fadeStartVolume = _currentVolume;
            _fadeTargetVolume = _currentVolume;
            _stopAfterFade = false;
        }

        private void BeginFade(float targetVolume, float durationSeconds, bool stopAfter)
        {
            _fadeDuration = Math.Max(0.0001f, durationSeconds);
            _fadeRemaining = _fadeDuration;
            _fadeStartVolume = _currentVolume;
            _fadeTargetVolume = targetVolume;
            _stopAfterFade = stopAfter;
        }

        private void CancelFade()
        {
            _fadeDuration = 0f;
            _fadeRemaining = 0f;
            _fadeStartVolume = _currentVolume;
            _fadeTargetVolume = _currentVolume;
            _stopAfterFade = false;
        }
    }
}
