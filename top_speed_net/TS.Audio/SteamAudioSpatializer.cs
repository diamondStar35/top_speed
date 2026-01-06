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
        private IPL.DirectEffect _directLeft;
        private IPL.DirectEffect _directRight;
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
        private readonly int _frameSize;

        public SteamAudioSpatializer(SteamAudioContext context, uint frameSize, bool trueStereo, HrtfDownmixMode downmixMode)
        {
            _ctx = context;
            _trueStereo = trueStereo;
            _downmixMode = downmixMode;
            _frameSize = (int)frameSize;

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

            var directSettingsMono = new IPL.DirectEffectSettings { NumChannels = 1 };
            error = IPL.DirectEffectCreate(_ctx.Context, in audioSettings, in directSettingsMono, out _directLeft);
            if (error != IPL.Error.Success)
                throw new InvalidOperationException("Failed to create direct effect: " + error);

            error = IPL.DirectEffectCreate(_ctx.Context, in audioSettings, in directSettingsMono, out _directRight);
            if (error != IPL.Error.Success)
                throw new InvalidOperationException("Failed to create direct effect: " + error);

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
            {
                for (int i = 0; i < frames; i++)
                {
                    int idx = i * (int)channels;
                    pInL[i] = framesIn[idx];
                    pInR[i] = framesIn[idx + 1];
                }

                var attenuation = GetAttenuationAndDirection(spatial, out var direction);

                var directParams = new IPL.DirectEffectParams
                {
                    Flags = IPL.DirectEffectFlags.ApplyDistanceAttenuation,
                    TransmissionType = IPL.TransmissionType.FrequencyIndependent,
                    DistanceAttenuation = attenuation,
                    Directivity = 1.0f,
                    Occlusion = Volatile.Read(ref spatial.Occlusion)
                };

                directParams.AirAbsorption[0] = 1.0f;
                directParams.AirAbsorption[1] = 1.0f;
                directParams.AirAbsorption[2] = 1.0f;
                directParams.Transmission[0] = 0.0f;
                directParams.Transmission[1] = 0.0f;
                directParams.Transmission[2] = 0.0f;

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

                frameCountOut = (UInt32)frames;
            }
        }

        public void Dispose()
        {
            if (_binauralLeft.Handle != IntPtr.Zero)
                IPL.BinauralEffectRelease(ref _binauralLeft);
            if (_binauralRight.Handle != IntPtr.Zero)
                IPL.BinauralEffectRelease(ref _binauralRight);
            if (_directLeft.Handle != IntPtr.Zero)
                IPL.DirectEffectRelease(ref _directLeft);
            if (_directRight.Handle != IntPtr.Zero)
                IPL.DirectEffectRelease(ref _directRight);
        }

        private unsafe void ProcessMono(NativeArray<float> framesIn, UInt32 frameCountIn, NativeArray<float> framesOut, ref UInt32 frameCountOut, UInt32 channels, AudioSourceSpatialParams spatial)
        {
            int frames = (int)Math.Min(frameCountIn, (uint)_frameSize);

            fixed (float* pMono = _mono)
            fixed (float* pOutL = _outL)
            fixed (float* pOutR = _outR)
            {
                int chCount = (int)channels;
                for (int i = 0; i < frames; i++)
                {
                    int idx = i * chCount;
                    pMono[i] = DownmixSample(framesIn, idx, chCount);
                }

                var attenuation = GetAttenuationAndDirection(spatial, out var direction);

                var directParams = new IPL.DirectEffectParams
                {
                    Flags = IPL.DirectEffectFlags.ApplyDistanceAttenuation,
                    TransmissionType = IPL.TransmissionType.FrequencyIndependent,
                    DistanceAttenuation = attenuation,
                    Directivity = 1.0f,
                    Occlusion = Volatile.Read(ref spatial.Occlusion)
                };

                directParams.AirAbsorption[0] = 1.0f;
                directParams.AirAbsorption[1] = 1.0f;
                directParams.AirAbsorption[2] = 1.0f;
                directParams.Transmission[0] = 0.0f;
                directParams.Transmission[1] = 0.0f;
                directParams.Transmission[2] = 0.0f;

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

                for (int i = 0; i < frames; i++)
                {
                    int idx = i * (int)channels;
                    framesOut[idx] = pOutL[i];
                    if (channels > 1)
                        framesOut[idx + 1] = pOutR[i];
                    for (int ch = 2; ch < channels; ch++)
                        framesOut[idx + ch] = 0f;
                }

                frameCountOut = (UInt32)frames;
            }
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
