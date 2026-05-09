using System;
using SoundFlow.Abstracts;
using SoundFlow.Codecs.FFMpeg;
using SoundFlow.Enums;
using SoundFlow.Providers;
using TopSpeed.Protocol;
using SfAudioFormat = SoundFlow.Structs.AudioFormat;

namespace TopSpeed.Network.Live
{
    internal sealed class Source : IDisposable
    {
        private static readonly Lazy<AudioEngine> DecoderEngine = new Lazy<AudioEngine>(CreateEngine);
        private SoundFlow.Interfaces.ISoundDataProvider _provider;
        private readonly int _channels;
        private readonly int _framesPerPacket;
        private readonly float[] _floatBuffer;
        private readonly short[] _sampleBuffer;
        private readonly string _filePath;
        private readonly SfAudioFormat _format;

        private Source(
            SoundFlow.Interfaces.ISoundDataProvider provider,
            int channels,
            int framesPerPacket,
            string filePath,
            SfAudioFormat format)
        {
            _provider = provider;
            _channels = channels;
            _framesPerPacket = framesPerPacket;
            _floatBuffer = new float[_channels * _framesPerPacket];
            _sampleBuffer = new short[_channels * _framesPerPacket];
            _filePath = filePath;
            _format = format;
        }

        public static bool TryOpen(string filePath, out Source? source)
        {
            source = null;
            if (string.IsNullOrWhiteSpace(filePath))
                return false;

            if (!System.IO.File.Exists(filePath))
                return false;

            var format = new SfAudioFormat
            {
                Format = SampleFormat.F32,
                Channels = ProtocolConstants.LiveChannelsMax,
                Layout = SfAudioFormat.GetLayoutFromChannels(ProtocolConstants.LiveChannelsMax),
                SampleRate = ProtocolConstants.LiveSampleRate
            };

            if (!TryOpenProvider(filePath, format, out var provider) || provider == null)
                return false;

            var frameCount = ProtocolConstants.LiveSampleRate * ProtocolConstants.LiveFrameMs / 1000;
            source = new Source(provider, ProtocolConstants.LiveChannelsMax, frameCount, filePath, format);
            return true;
        }

        public bool TryRead(out short[] samples)
        {
            samples = _sampleBuffer;
            var targetFrames = (ulong)_framesPerPacket;
            ulong writtenFrames = 0;
            var wraps = 0;
            var stalledReads = 0;

            while (writtenFrames < targetFrames)
            {
                var sampleOffset = (int)(writtenFrames * (ulong)_channels);
                var samplesToRead = (int)((targetFrames - writtenFrames) * (ulong)_channels);

                int readSamples;
                try
                {
                    readSamples = _provider.ReadBytes(_floatBuffer.AsSpan(sampleOffset, samplesToRead));
                }
                catch
                {
                    // Some native decoders (FFmpeg in particular) can leave their internal
                    // demuxer state in an unrecoverable condition once they reach end of
                    // stream, which causes the next decode call to throw even though the
                    // underlying file stream is healthy. Fall through to the rewind path
                    // so the source is reopened from scratch instead of crashing the
                    // multiplayer loop.
                    readSamples = 0;
                }

                if (readSamples > 0)
                {
                    writtenFrames += (ulong)(readSamples / _channels);
                    stalledReads = 0;
                    continue;
                }

                if (!TryRewind())
                    return false;

                wraps++;
                if (wraps > _framesPerPacket)
                    return false;

                stalledReads++;
                if (stalledReads > 2)
                    return false;
            }

            ConvertToPcm16(_floatBuffer, _sampleBuffer);
            return true;
        }

        public void Dispose()
        {
            _provider.Dispose();
        }

        private bool TryRewind()
        {
            try
            {
                _provider.Dispose();
            }
            catch
            {
                // best effort; the replacement provider below is what we depend on.
            }

            if (!TryOpenProvider(_filePath, _format, out var provider) || provider == null)
                return false;

            _provider = provider;
            return true;
        }

        private static bool TryOpenProvider(string filePath, SfAudioFormat format, out SoundFlow.Interfaces.ISoundDataProvider? provider)
        {
            provider = null;
            if (!System.IO.File.Exists(filePath))
                return false;

            SoundFlow.Interfaces.ISoundDataProvider opened;
            try
            {
                opened = new StreamDataProvider(
                    DecoderEngine.Value,
                    format,
                    new System.IO.FileStream(filePath, System.IO.FileMode.Open, System.IO.FileAccess.Read, System.IO.FileShare.Read));
            }
            catch
            {
                return false;
            }

            if (opened.Length == 0)
            {
                opened.Dispose();
                return false;
            }

            provider = opened;
            return true;
        }

        private static AudioEngine CreateEngine()
        {
            var engine = new SoundFlow.Backends.MiniAudio.MiniAudioEngine();
            engine.RegisterCodecFactory(new FFmpegCodecFactory());
            return engine;
        }

        private static void ConvertToPcm16(float[] source, short[] destination)
        {
            var count = Math.Min(source.Length, destination.Length);
            for (var i = 0; i < count; i++)
            {
                var sample = source[i];
                if (sample < -1f)
                    sample = -1f;
                else if (sample > 1f)
                    sample = 1f;
                destination[i] = (short)Math.Round(sample * short.MaxValue);
            }
        }
    }
}
