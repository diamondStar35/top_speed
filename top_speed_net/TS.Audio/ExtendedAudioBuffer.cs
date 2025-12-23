using System;
using System.Numerics;

namespace TS.Audio
{
    public class ExtendedAudioBuffer : IDisposable
    {
        public enum State
        {
            playing,
            stopped
        }

        private readonly AudioSourceHandle _source;
        private bool _hasNeverPlayed = true;
        private bool _isStopped;
        private bool _looping;
        private bool _isInitializingPlayback;
        private Action? _onEnd;

        internal ExtendedAudioBuffer(AudioSourceHandle source)
        {
            _source = source ?? throw new ArgumentNullException(nameof(source));
        }

        public State state
        {
            get
            {
                if (_isInitializingPlayback)
                    return State.playing;

                if (_looping)
                    return _isStopped ? State.stopped : State.playing;

                return _source.IsPlaying ? State.playing : State.stopped;
            }
        }

        public void setOnEnd(Action onEnd)
        {
            _onEnd = onEnd;
            _source.SetOnEnd(onEnd);
        }

        public void play(bool stop, bool loop)
        {
            _looping = loop;
            _isInitializingPlayback = true;
            _source.SetLooping(loop);

            if (stop || _hasNeverPlayed)
            {
                _hasNeverPlayed = false;
                _source.Stop();
                _source.SeekToStart();
            }

            if (!_source.IsPlaying)
            {
                _source.Play(loop);
            }
            _isStopped = false;
            _isInitializingPlayback = false;
        }

        public void stop()
        {
            _source.Stop();
            _isStopped = true;
        }

        public void apply3D(DspSettings settings, int sourceChannels, int destinationChannels, CalculateFlags flags)
        {
            if ((flags & CalculateFlags.Doppler) == CalculateFlags.Doppler)
            {
                // Doppler is already handled by the audio system's listener/source state.
            }
        }

        public float getFrequency()
        {
            return AudioMath.PitchRatioToSemitones(_source.GetPitch());
        }

        public void setFrequency(float f)
        {
            _source.SetPitch(AudioMath.SemitonesToPitchRatio(f));
        }

        public float getVolume()
        {
            return _source.GetVolume();
        }

        public void setVolume(float v)
        {
            _source.SetVolume(v);
        }

        public VoiceDetails getVoiceDetails()
        {
            return new VoiceDetails
            {
                InputChannelCount = _source.InputChannels,
                InputSampleRate = _source.InputSampleRate
            };
        }

        public void setOutputMatrix(int sourceChannels, int destinationChannels, float[] levelMatrixRef)
        {
            // Matrix routing is handled internally by the audio engine.
        }

        public void Dispose()
        {
            _source.Dispose();
        }

        internal void SetPosition(Vector3 position)
        {
            _source.SetPosition(position);
        }

        internal void SetVelocity(Vector3 velocity)
        {
            _source.SetVelocity(velocity);
        }

        internal void SetPan(float pan)
        {
            _source.SetPan(pan);
        }

        internal void ApplyCurveDistanceScaler(float scaler)
        {
            _source.ApplyCurveDistanceScaler(scaler);
        }
    }
}
