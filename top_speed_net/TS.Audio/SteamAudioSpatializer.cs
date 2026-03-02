using System;
using System.Threading;
using MiniAudioEx.Native;
using SteamAudio;

namespace TS.Audio
{
    internal sealed class SteamAudioSpatializer : IDisposable
    {
        private readonly SteamAudioContext _ctx;
        private readonly bool _trueStereo;
        private readonly HrtfDownmixMode _downmixMode;
        private IPL.BinauralEffect _binauralLeft;
        private IPL.BinauralEffect _binauralRight;
        private IPL.AmbisonicsBinauralEffect _ambisonicsBinauralParametric;
        private IPL.AmbisonicsRotationEffect _ambisonicsRotationParametric;
        private IPL.DirectEffect _directLeft;
        private IPL.DirectEffect _directRight;
        private IPL.ReflectionEffect _reflectionParametric;
        private readonly float[] _mono;
        private readonly float[] _outL;
        private readonly float[] _outR;
        private readonly float[] _inLeft;
        private readonly float[] _inRight;
        private readonly float[] _directLeftSamples;
        private readonly float[] _directRightSamples;
        private readonly float[] _outLeftL;
        private readonly float[] _outLeftR;
        private readonly float[] _outRightL;
        private readonly float[] _outRightR;
        private readonly float[] _reverbAmbi;
        private readonly float[] _reverbAmbiRotated;
        private readonly float[] _reverbWetL;
        private readonly float[] _reverbWetR;
        private float _reflectionWet;
        private readonly int _frameSize;
        private readonly int _reflectionOrder;
        private readonly int _reflectionChannels;

        public SteamAudioSpatializer(SteamAudioContext context, uint frameSize, bool trueStereo, HrtfDownmixMode downmixMode)
        {
            _ctx = context;
            _trueStereo = trueStereo;
            _downmixMode = downmixMode;
            _frameSize = (int)frameSize;
            _reflectionOrder = Math.Max(0, _ctx.ReflectionOrder);
            _reflectionChannels = Math.Max(1, _ctx.ReflectionChannels);

            var audioSettings = new IPL.AudioSettings
            {
                SamplingRate = _ctx.SampleRate,
                FrameSize = _ctx.FrameSize
            };

            var binauralSettings = new IPL.BinauralEffectSettings
            {
                Hrtf = _ctx.Hrtf
            };

            var error = IPL.BinauralEffectCreate(_ctx.Context, in audioSettings, in binauralSettings, out _binauralLeft);
            if (error != IPL.Error.Success)
                throw new InvalidOperationException("Failed to create binaural effect: " + error);

            error = IPL.BinauralEffectCreate(_ctx.Context, in audioSettings, in binauralSettings, out _binauralRight);
            if (error != IPL.Error.Success)
                throw new InvalidOperationException("Failed to create binaural effect: " + error);

            var ambiSettings = new IPL.AmbisonicsBinauralEffectSettings
            {
                Hrtf = _ctx.Hrtf,
                MaxOrder = _reflectionOrder
            };

            error = IPL.AmbisonicsBinauralEffectCreate(_ctx.Context, in audioSettings, in ambiSettings, out _ambisonicsBinauralParametric);
            if (error != IPL.Error.Success)
                throw new InvalidOperationException("Failed to create parametric ambisonics binaural effect: " + error);

            var rotationSettings = new IPL.AmbisonicsRotationEffectSettings
            {
                MaxOrder = _reflectionOrder
            };

            error = IPL.AmbisonicsRotationEffectCreate(_ctx.Context, in audioSettings, in rotationSettings, out _ambisonicsRotationParametric);
            if (error != IPL.Error.Success)
                throw new InvalidOperationException("Failed to create parametric ambisonics rotation effect: " + error);

            var directSettingsMono = new IPL.DirectEffectSettings { NumChannels = 1 };
            error = IPL.DirectEffectCreate(_ctx.Context, in audioSettings, in directSettingsMono, out _directLeft);
            if (error != IPL.Error.Success)
                throw new InvalidOperationException("Failed to create direct effect: " + error);

            error = IPL.DirectEffectCreate(_ctx.Context, in audioSettings, in directSettingsMono, out _directRight);
            if (error != IPL.Error.Success)
                throw new InvalidOperationException("Failed to create direct effect: " + error);

            _reflectionParametric = CreateReflectionEffect(
                in audioSettings,
                IPL.ReflectionEffectType.Parametric,
                1,
                1,
                "parametric reflection effect");

            _mono = new float[_frameSize];
            _outL = new float[_frameSize];
            _outR = new float[_frameSize];
            _inLeft = new float[_frameSize];
            _inRight = new float[_frameSize];
            _directLeftSamples = new float[_frameSize];
            _directRightSamples = new float[_frameSize];
            _outLeftL = new float[_frameSize];
            _outLeftR = new float[_frameSize];
            _outRightL = new float[_frameSize];
            _outRightR = new float[_frameSize];
            _reverbAmbi = new float[_frameSize * _reflectionChannels];
            _reverbAmbiRotated = new float[_frameSize * _reflectionChannels];
            _reverbWetL = new float[_frameSize];
            _reverbWetR = new float[_frameSize];
        }

        public unsafe void Process(NativeArray<float> framesIn, UInt32 frameCountIn, NativeArray<float> framesOut, ref UInt32 frameCountOut, UInt32 channels, AudioSourceSpatialParams spatial)
        {
            if (!_trueStereo || channels < 2)
            {
                ProcessMono(framesIn, frameCountIn, framesOut, ref frameCountOut, channels, spatial);
                return;
            }

            int frames = (int)Math.Min(frameCountIn, (uint)_frameSize);

            fixed (float* pInL = _inLeft)
            fixed (float* pInR = _inRight)
            fixed (float* pDirL = _directLeftSamples)
            fixed (float* pDirR = _directRightSamples)
            fixed (float* pOutLL = _outLeftL)
            fixed (float* pOutLR = _outLeftR)
            fixed (float* pOutRL = _outRightL)
            fixed (float* pOutRR = _outRightR)
            fixed (float* pMono = _mono)
            fixed (float* pReverbL = _reverbWetL)
            fixed (float* pReverbR = _reverbWetR)
            {
                for (int i = 0; i < frames; i++)
                {
                    int idx = i * (int)channels;
                    pInL[i] = framesIn[idx];
                    pInR[i] = framesIn[idx + 1];
                    pMono[i] = 0.5f * (pInL[i] + pInR[i]);
                    pReverbL[i] = 0f;
                    pReverbR[i] = 0f;
                }

                var attenuation = GetAttenuationAndDirection(spatial, out var direction);

                var directParams = new IPL.DirectEffectParams
                {
                    Flags = IPL.DirectEffectFlags.ApplyDistanceAttenuation,
                    TransmissionType = IPL.TransmissionType.FrequencyDependent,
                    DistanceAttenuation = attenuation,
                    Directivity = 1.0f
                };

                var simFlags = Volatile.Read(ref spatial.SimulationFlags);
                if ((simFlags & AudioSourceSpatialParams.SimAirAbsorption) != 0)
                {
                    directParams.Flags |= IPL.DirectEffectFlags.ApplyAirAbsorption;
                    directParams.AirAbsorption[0] = Volatile.Read(ref spatial.AirAbsLow);
                    directParams.AirAbsorption[1] = Volatile.Read(ref spatial.AirAbsMid);
                    directParams.AirAbsorption[2] = Volatile.Read(ref spatial.AirAbsHigh);
                }
                else
                {
                    directParams.AirAbsorption[0] = 1.0f;
                    directParams.AirAbsorption[1] = 1.0f;
                    directParams.AirAbsorption[2] = 1.0f;
                }

                if ((simFlags & AudioSourceSpatialParams.SimOcclusion) != 0)
                {
                    directParams.Flags |= IPL.DirectEffectFlags.ApplyOcclusion;
                    directParams.Occlusion = Volatile.Read(ref spatial.Occlusion);
                }

                if ((simFlags & AudioSourceSpatialParams.SimTransmission) != 0)
                {
                    directParams.Flags |= IPL.DirectEffectFlags.ApplyTransmission;
                    directParams.Transmission[0] = Volatile.Read(ref spatial.TransLow);
                    directParams.Transmission[1] = Volatile.Read(ref spatial.TransMid);
                    directParams.Transmission[2] = Volatile.Read(ref spatial.TransHigh);
                }
                else
                {
                    directParams.Transmission[0] = 0.0f;
                    directParams.Transmission[1] = 0.0f;
                    directParams.Transmission[2] = 0.0f;
                }

                var binauralParams = new IPL.BinauralEffectParams
                {
                    Direction = direction,
                    Interpolation = IPL.HrtfInterpolation.Bilinear,
                    SpatialBlend = 1.0f,
                    Hrtf = _ctx.Hrtf,
                    PeakDelays = IntPtr.Zero
                };

                var inPtrL = stackalloc IntPtr[1];
                var dirPtrL = stackalloc IntPtr[1];
                var outPtrL = stackalloc IntPtr[2];
                inPtrL[0] = (IntPtr)pInL;
                dirPtrL[0] = (IntPtr)pDirL;
                outPtrL[0] = (IntPtr)pOutLL;
                outPtrL[1] = (IntPtr)pOutLR;

                var inBufferL = new IPL.AudioBuffer { NumChannels = 1, NumSamples = frames, Data = (IntPtr)inPtrL };
                var dirBufferL = new IPL.AudioBuffer { NumChannels = 1, NumSamples = frames, Data = (IntPtr)dirPtrL };
                var outBufferL = new IPL.AudioBuffer { NumChannels = 2, NumSamples = frames, Data = (IntPtr)outPtrL };

                IPL.DirectEffectApply(_directLeft, ref directParams, ref inBufferL, ref dirBufferL);
                IPL.BinauralEffectApply(_binauralLeft, ref binauralParams, ref dirBufferL, ref outBufferL);

                var inPtrR = stackalloc IntPtr[1];
                var dirPtrR = stackalloc IntPtr[1];
                var outPtrR = stackalloc IntPtr[2];
                inPtrR[0] = (IntPtr)pInR;
                dirPtrR[0] = (IntPtr)pDirR;
                outPtrR[0] = (IntPtr)pOutRL;
                outPtrR[1] = (IntPtr)pOutRR;

                var inBufferR = new IPL.AudioBuffer { NumChannels = 1, NumSamples = frames, Data = (IntPtr)inPtrR };
                var dirBufferR = new IPL.AudioBuffer { NumChannels = 1, NumSamples = frames, Data = (IntPtr)dirPtrR };
                var outBufferR = new IPL.AudioBuffer { NumChannels = 2, NumSamples = frames, Data = (IntPtr)outPtrR };

                IPL.DirectEffectApply(_directRight, ref directParams, ref inBufferR, ref dirBufferR);
                IPL.BinauralEffectApply(_binauralRight, ref binauralParams, ref dirBufferR, ref outBufferR);

                const float mixScale = 0.5f;

                if (spatial.StereoWidening)
                {
                    // Apply channel-specific attenuation based on the sound's horizontal direction (X-axis).
                    // A buffer is included to ensure complete silence is only reached at the maximum extent of the arc.
                    float x = direction.X;
                    float leftGain = x > 0f ? Math.Max(0f, 1.0f - (x / 0.90f)) : 1.0f;
                    float rightGain = x < 0f ? Math.Max(0f, 1.0f + (x / 0.90f)) : 1.0f;

                    for (int i = 0; i < frames; i++)
                    {
                        int idx = i * (int)channels;
                        framesOut[idx] = (pOutLL[i] + pOutRL[i]) * mixScale * leftGain;
                        framesOut[idx + 1] = (pOutLR[i] + pOutRR[i]) * mixScale * rightGain;
                        for (int ch = 2; ch < channels; ch++)
                            framesOut[idx + ch] = 0f;
                    }
                }
                else
                {
                    // EXACT Vanilla Path
                    for (int i = 0; i < frames; i++)
                    {
                        int idx = i * (int)channels;
                        float left = (pOutLL[i] + pOutRL[i]) * mixScale;
                        float right = (pOutLR[i] + pOutRR[i]) * mixScale;
                        framesOut[idx] = left;
                        framesOut[idx + 1] = right;
                        for (int ch = 2; ch < channels; ch++)
                            framesOut[idx + ch] = 0f;
                    }
                }

                if ((simFlags & AudioSourceSpatialParams.SimReflections) != 0)
                {
                    ApplyReflections(frames, spatial, pReverbL, pReverbR);
                    var wetScale = GetReflectionWetScale(spatial);
                    for (int i = 0; i < frames; i++)
                    {
                        int idx = i * (int)channels;
                        _reflectionWet += (wetScale - _reflectionWet) * 0.05f;
                        framesOut[idx] += pReverbL[i] * _reflectionWet;
                        framesOut[idx + 1] += pReverbR[i] * _reflectionWet;
                    }
                }
                else
                {
                    _reflectionWet *= 0.90f;
                }

                frameCountOut = (UInt32)frames;
            }
        }

        public void Dispose()
        {
            if (_binauralLeft.Handle != IntPtr.Zero)
                IPL.BinauralEffectRelease(ref _binauralLeft);
            if (_binauralRight.Handle != IntPtr.Zero)
                IPL.BinauralEffectRelease(ref _binauralRight);
            if (_ambisonicsBinauralParametric.Handle != IntPtr.Zero)
                IPL.AmbisonicsBinauralEffectRelease(ref _ambisonicsBinauralParametric);
            if (_ambisonicsRotationParametric.Handle != IntPtr.Zero)
                IPL.AmbisonicsRotationEffectRelease(ref _ambisonicsRotationParametric);
            if (_directLeft.Handle != IntPtr.Zero)
                IPL.DirectEffectRelease(ref _directLeft);
            if (_directRight.Handle != IntPtr.Zero)
                IPL.DirectEffectRelease(ref _directRight);
            if (_reflectionParametric.Handle != IntPtr.Zero)
                IPL.ReflectionEffectRelease(ref _reflectionParametric);
        }

        private unsafe void ProcessMono(NativeArray<float> framesIn, UInt32 frameCountIn, NativeArray<float> framesOut, ref UInt32 frameCountOut, UInt32 channels, AudioSourceSpatialParams spatial)
        {
            int frames = (int)Math.Min(frameCountIn, (uint)_frameSize);

            fixed (float* pMono = _mono)
            fixed (float* pOutL = _outL)
            fixed (float* pOutR = _outR)
            fixed (float* pReverbL = _reverbWetL)
            fixed (float* pReverbR = _reverbWetR)
            {
                int chCount = (int)channels;
                for (int i = 0; i < frames; i++)
                {
                    int idx = i * chCount;
                    pMono[i] = DownmixSample(framesIn, idx, chCount);
                    pReverbL[i] = 0f;
                    pReverbR[i] = 0f;
                }

                var attenuation = GetAttenuationAndDirection(spatial, out var direction);

                var directParams = new IPL.DirectEffectParams
                {
                    Flags = IPL.DirectEffectFlags.ApplyDistanceAttenuation,
                    TransmissionType = IPL.TransmissionType.FrequencyDependent,
                    DistanceAttenuation = attenuation,
                    Directivity = 1.0f
                };

                var simFlags = Volatile.Read(ref spatial.SimulationFlags);
                if ((simFlags & AudioSourceSpatialParams.SimAirAbsorption) != 0)
                {
                    directParams.Flags |= IPL.DirectEffectFlags.ApplyAirAbsorption;
                    directParams.AirAbsorption[0] = Volatile.Read(ref spatial.AirAbsLow);
                    directParams.AirAbsorption[1] = Volatile.Read(ref spatial.AirAbsMid);
                    directParams.AirAbsorption[2] = Volatile.Read(ref spatial.AirAbsHigh);
                }
                else
                {
                    directParams.AirAbsorption[0] = 1.0f;
                    directParams.AirAbsorption[1] = 1.0f;
                    directParams.AirAbsorption[2] = 1.0f;
                }

                if ((simFlags & AudioSourceSpatialParams.SimOcclusion) != 0)
                {
                    directParams.Flags |= IPL.DirectEffectFlags.ApplyOcclusion;
                    directParams.Occlusion = Volatile.Read(ref spatial.Occlusion);
                }

                if ((simFlags & AudioSourceSpatialParams.SimTransmission) != 0)
                {
                    directParams.Flags |= IPL.DirectEffectFlags.ApplyTransmission;
                    directParams.Transmission[0] = Volatile.Read(ref spatial.TransLow);
                    directParams.Transmission[1] = Volatile.Read(ref spatial.TransMid);
                    directParams.Transmission[2] = Volatile.Read(ref spatial.TransHigh);
                }
                else
                {
                    directParams.Transmission[0] = 0.0f;
                    directParams.Transmission[1] = 0.0f;
                    directParams.Transmission[2] = 0.0f;
                }

                var binauralParams = new IPL.BinauralEffectParams
                {
                    Direction = direction,
                    Interpolation = IPL.HrtfInterpolation.Bilinear,
                    SpatialBlend = 1.0f,
                    Hrtf = _ctx.Hrtf,
                    PeakDelays = IntPtr.Zero
                };

                var inputPtr = stackalloc IntPtr[1];
                var outputPtr = stackalloc IntPtr[2];
                inputPtr[0] = (IntPtr)pMono;
                outputPtr[0] = (IntPtr)pOutL;
                outputPtr[1] = (IntPtr)pOutR;

                var inputBuffer = new IPL.AudioBuffer
                {
                    NumChannels = 1,
                    NumSamples = frames,
                    Data = (IntPtr)inputPtr
                };

                var outputBuffer = new IPL.AudioBuffer
                {
                    NumChannels = 2,
                    NumSamples = frames,
                    Data = (IntPtr)outputPtr
                };

                IPL.DirectEffectApply(_directLeft, ref directParams, ref inputBuffer, ref inputBuffer);
                IPL.BinauralEffectApply(_binauralLeft, ref binauralParams, ref inputBuffer, ref outputBuffer);

                if (spatial.StereoWidening)
                {
                    // Apply channel-specific attenuation based on the sound's horizontal direction (X-axis).
                    // A buffer is included to ensure complete silence is only reached at the maximum extent of the arc.
                    float x = direction.X;
                    float leftGain = x > 0f ? Math.Max(0f, 1.0f - (x / 0.90f)) : 1.0f;
                    float rightGain = x < 0f ? Math.Max(0f, 1.0f + (x / 0.90f)) : 1.0f;

                    for (int i = 0; i < frames; i++)
                    {
                        int idx = i * (int)channels;
                        framesOut[idx] = pOutL[i] * leftGain;
                        if (channels > 1)
                            framesOut[idx + 1] = pOutR[i] * rightGain;
                        for (int ch = 2; ch < channels; ch++)
                            framesOut[idx + ch] = 0f;
                    }
                }
                else
                {
                    // EXACT Vanilla Path
                    for (int i = 0; i < frames; i++)
                    {
                        int idx = i * (int)channels;
                        framesOut[idx] = pOutL[i];
                        if (channels > 1)
                            framesOut[idx + 1] = pOutR[i];
                        for (int ch = 2; ch < channels; ch++)
                            framesOut[idx + ch] = 0f;
                    }
                }

                if ((simFlags & AudioSourceSpatialParams.SimReflections) != 0)
                {
                    ApplyReflections(frames, spatial, pReverbL, pReverbR);
                    var wetScale = GetReflectionWetScale(spatial);
                    for (int i = 0; i < frames; i++)
                    {
                        int idx = i * (int)channels;
                        _reflectionWet += (wetScale - _reflectionWet) * 0.05f;
                        framesOut[idx] += pReverbL[i] * _reflectionWet;
                        if (channels > 1)
                        {
                            framesOut[idx + 1] += pReverbR[i] * _reflectionWet;
                        }
                    }
                }
                else
                {
                    _reflectionWet *= 0.90f;
                }

                frameCountOut = (UInt32)frames;
            }
        }

        private unsafe void ApplyReflections(int frames, AudioSourceSpatialParams spatial, float* outL, float* outR)
        {
            var timeLow = Volatile.Read(ref spatial.ReverbTimeLow);
            var timeMid = Volatile.Read(ref spatial.ReverbTimeMid);
            var timeHigh = Volatile.Read(ref spatial.ReverbTimeHigh);
            var eqLow = Volatile.Read(ref spatial.ReverbEqLow);
            var eqMid = Volatile.Read(ref spatial.ReverbEqMid);
            var eqHigh = Volatile.Read(ref spatial.ReverbEqHigh);
            var delay = Volatile.Read(ref spatial.ReverbDelay);
            if (RenderReflectionsPass(
                frames,
                IPL.ReflectionEffectType.Parametric,
                0,
                0,
                0,
                delay,
                timeLow,
                timeMid,
                timeHigh,
                eqLow,
                eqMid,
                eqHigh,
                outL,
                outR))
            {
                return;
            }

            for (int i = 0; i < frames; i++)
            {
                outL[i] = 0f;
                outR[i] = 0f;
            }
        }

        private unsafe bool RenderReflectionsPass(
            int frames,
            IPL.ReflectionEffectType effectType,
            long irPtrValue,
            int irSize,
            int irChannels,
            int delay,
            float timeLow,
            float timeMid,
            float timeHigh,
            float eqLow,
            float eqMid,
            float eqHigh,
            float* outL,
            float* outR)
        {
            if (effectType != IPL.ReflectionEffectType.Parametric)
                return false;

            var reflection = _reflectionParametric;
            if (reflection.Handle == IntPtr.Zero)
                return false;

            const int channelsToRender = 1;
            const int channelsToProcess = 1;
            const int irSamples = 0;
            const int order = 0;

            var reflectionParams = new IPL.ReflectionEffectParams
            {
                Type = effectType,
                NumChannels = channelsToProcess,
                IrSize = irSamples,
                Ir = default,
                Delay = delay
            };

            reflectionParams.ReverbTimes[0] = timeLow;
            reflectionParams.ReverbTimes[1] = timeMid;
            reflectionParams.ReverbTimes[2] = timeHigh;
            reflectionParams.Eq[0] = eqLow;
            reflectionParams.Eq[1] = eqMid;
            reflectionParams.Eq[2] = eqHigh;

            fixed (float* pIn = _mono)
            fixed (float* pAmbi = _reverbAmbi)
            fixed (float* pAmbiRot = _reverbAmbiRotated)
            {
                var rotationEffect = _ambisonicsRotationParametric;
                var binauralEffect = _ambisonicsBinauralParametric;

                for (int ch = 0; ch < channelsToRender; ch++)
                {
                    float* chPtr = pAmbi + ch * _frameSize;
                    for (int i = 0; i < frames; i++)
                        chPtr[i] = 0f;
                }
                for (int i = 0; i < frames; i++)
                {
                    outL[i] = 0f;
                    outR[i] = 0f;
                }

                var inPtr = stackalloc IntPtr[1];
                inPtr[0] = (IntPtr)pIn;
                var outPtr = stackalloc IntPtr[channelsToRender];
                for (int ch = 0; ch < channelsToRender; ch++)
                    outPtr[ch] = (IntPtr)(pAmbi + ch * _frameSize);

                var inBuffer = new IPL.AudioBuffer { NumChannels = 1, NumSamples = frames, Data = (IntPtr)inPtr };
                var outBuffer = new IPL.AudioBuffer { NumChannels = channelsToRender, NumSamples = frames, Data = (IntPtr)outPtr };

                IPL.ReflectionEffectApply(reflection, ref reflectionParams, ref inBuffer, ref outBuffer, default);

                var rotOutPtr = stackalloc IntPtr[channelsToRender];
                for (int ch = 0; ch < channelsToRender; ch++)
                    rotOutPtr[ch] = (IntPtr)(pAmbiRot + ch * _frameSize);

                var rotOutBuffer = new IPL.AudioBuffer { NumChannels = channelsToRender, NumSamples = frames, Data = (IntPtr)rotOutPtr };
                var listener = _ctx.ListenerSnapshot;
                var rotationParams = new IPL.AmbisonicsRotationEffectParams
                {
                    Orientation = new IPL.CoordinateSpace3
                    {
                        Right = listener.Right,
                        Up = listener.Up,
                        Ahead = listener.Ahead,
                        Origin = listener.Origin
                    },
                    Order = order
                };

                IPL.AmbisonicsRotationEffectApply(rotationEffect, ref rotationParams, ref outBuffer, ref rotOutBuffer);

                var ambiParams = new IPL.AmbisonicsBinauralEffectParams
                {
                    Hrtf = _ctx.Hrtf,
                    Order = order
                };

                var ambiOutPtr = stackalloc IntPtr[2];
                ambiOutPtr[0] = (IntPtr)outL;
                ambiOutPtr[1] = (IntPtr)outR;
                var ambiOutBuffer = new IPL.AudioBuffer { NumChannels = 2, NumSamples = frames, Data = (IntPtr)ambiOutPtr };

                IPL.AmbisonicsBinauralEffectApply(binauralEffect, ref ambiParams, ref rotOutBuffer, ref ambiOutBuffer);
            }

            return true;
        }

        private IPL.ReflectionEffect CreateReflectionEffect(
            in IPL.AudioSettings audioSettings,
            IPL.ReflectionEffectType type,
            int numChannels,
            int irSize,
            string label)
        {
            var settings = new IPL.ReflectionEffectSettings
            {
                Type = type,
                NumChannels = Math.Max(1, numChannels),
                IrSize = Math.Max(1, irSize)
            };

            var error = IPL.ReflectionEffectCreate(_ctx.Context, in audioSettings, in settings, out var effect);
            if (error != IPL.Error.Success)
                throw new InvalidOperationException($"Failed to create {label}: {error}");

            return effect;
        }

        private static float GetReflectionWetScale(AudioSourceSpatialParams spatial)
        {
            const float defaultWetScale = 0.35f;
            var wetScale = defaultWetScale;
            var roomFlags = Volatile.Read(ref spatial.RoomFlags);
            if ((roomFlags & AudioSourceSpatialParams.RoomHasProfile) != 0)
            {
                wetScale *= Clamp01(Volatile.Read(ref spatial.RoomReverbGain));
            }

            return wetScale;
        }

        private float GetAttenuationAndDirection(AudioSourceSpatialParams spatial, out IPL.Vector3 direction)
        {
            var sourcePos = new IPL.Vector3
            {
                X = Volatile.Read(ref spatial.PosX),
                Y = Volatile.Read(ref spatial.PosY),
                Z = Volatile.Read(ref spatial.PosZ)
            };

            var listener = _ctx.ListenerSnapshot;

            var worldDir = new IPL.Vector3
            {
                X = sourcePos.X - listener.Origin.X,
                Y = sourcePos.Y - listener.Origin.Y,
                Z = sourcePos.Z - listener.Origin.Z
            };

            float distance = (float)Math.Sqrt(worldDir.X * worldDir.X + worldDir.Y * worldDir.Y + worldDir.Z * worldDir.Z);
            if (distance > 0.0001f)
            {
                float inv = 1.0f / distance;
                worldDir.X *= inv;
                worldDir.Y *= inv;
                worldDir.Z *= inv;

                direction = new IPL.Vector3
                {
                    X = worldDir.X * listener.Right.X + worldDir.Y * listener.Right.Y + worldDir.Z * listener.Right.Z,
                    Y = worldDir.X * listener.Up.X + worldDir.Y * listener.Up.Y + worldDir.Z * listener.Up.Z,
                    Z = worldDir.X * listener.Ahead.X + worldDir.Y * listener.Ahead.Y + worldDir.Z * listener.Ahead.Z
                };
            }
            else
            {
                direction = new IPL.Vector3 { X = 0, Y = 0, Z = -1 };
            }

            float refDist = Volatile.Read(ref spatial.RefDistance);
            float maxDist = Volatile.Read(ref spatial.MaxDistance);
            float rolloff = Volatile.Read(ref spatial.RollOff);

            // Manual calculation for Inverse Distance to avoid potential IPL issues
            float attenuation = 1.0f;
            if (distance < refDist)
            {
                attenuation = 1.0f;
            }
            else
            {
                attenuation = refDist / distance;
            }
            
            // IPL.DistanceAttenuationCalculate does this:
            /*
            var distModel = new IPL.DistanceAttenuationModel
            {
                Type = IPL.DistanceAttenuationModelType.InverseDistance,
                MinDistance = refDist,
                Callback = null,
                UserData = IntPtr.Zero,
                Dirty = false
            };
            float attenuation = IPL.DistanceAttenuationCalculate(_ctx.Context, sourcePos, listener.Origin, in distModel);
            */

            return ApplyDistanceModel(distance, refDist, maxDist, rolloff, attenuation, spatial.DistanceModel);
        }

        private static float ApplyDistanceModel(float distance, float refDistance, float maxDistance, float rolloff, float steamAudioAttenuation, DistanceModel model)
        {
            if (model == DistanceModel.Inverse)
            {
                if (distance < refDistance)
                    distance = refDistance;
                if (maxDistance > refDistance && maxDistance < 100000000f && distance > maxDistance)
                    distance = maxDistance;

                return Clamp(steamAudioAttenuation, 0f, 1f);
            }

            if (maxDistance <= refDistance)
                maxDistance = refDistance + 0.0001f;

            distance = Clamp(distance, refDistance, maxDistance);
            float attenuation;

            switch (model)
            {
                case DistanceModel.Linear:
                    attenuation = 1f - rolloff * (distance - refDistance) / (maxDistance - refDistance);
                    break;
                case DistanceModel.Exponential:
                    attenuation = (float)Math.Pow(distance / refDistance, -rolloff);
                    break;
                default:
                    attenuation = steamAudioAttenuation;
                    break;
            }

            return Clamp(attenuation, 0f, 1f);
        }

        private static float Clamp(float value, float min, float max)
        {
            if (value < min) return min;
            if (value > max) return max;
            return value;
        }

        private static float Clamp01(float value)
        {
            return Clamp(value, 0f, 1f);
        }

        private static float Lerp(float from, float to, float t)
        {
            return from + ((to - from) * t);
        }

        private float DownmixSample(NativeArray<float> framesIn, int offset, int channels)
        {
            if (channels <= 1)
                return framesIn[offset];

            switch (_downmixMode)
            {
                case HrtfDownmixMode.Left:
                    return framesIn[offset];
                case HrtfDownmixMode.Right:
                    return framesIn[offset + 1];
                case HrtfDownmixMode.Sum:
                {
                    float sum = 0f;
                    for (int ch = 0; ch < channels; ch++)
                        sum += framesIn[offset + ch];
                    return sum;
                }
                case HrtfDownmixMode.Average:
                default:
                {
                    float sum = 0f;
                    for (int ch = 0; ch < channels; ch++)
                        sum += framesIn[offset + ch];
                    return sum / Math.Max(1, channels);
                }
            }
        }
    }
}
